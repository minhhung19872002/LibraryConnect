using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Catalogs;
using LibraryConnect.Application.Features.Admin.UserGroups;
using LibraryConnect.Application.Features.Admin.Users;
using LibraryConnect.Application.Features.Auth;
using LibraryConnect.Application.Features.Circulation;
using Microsoft.EntityFrameworkCore;
using LibraryConnect.Application.Features.Readers;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Khóa thẻ bạn đọc hay khóa tài khoản cán bộ phải có tác dụng **ngay**, kể cả với phiên đang mở.
///
/// Thẻ đăng nhập là JWT nên máy chủ không giữ trạng thái của nó: trước 07/09/2026, khóa thẻ một bạn
/// đọc trên máy chủ thật xong thì phiên đang mở của họ vẫn làm được **9 trong 11 việc** — xem thẻ
/// điện tử, xem công nợ, sửa thông tin liên hệ, gửi yêu cầu gia hạn thẻ, đọc tài liệu số nội bộ, và
/// tự cấp cho mình một **gói đọc ngoại tuyến dùng được thêm bảy ngày** — rồi làm mới thẻ đăng nhập
/// vô thời hạn, nên phiên ấy không bao giờ kết thúc. Chỉ hai lối tự hỏi trạng thái thẻ (đặt giữ và
/// mượn tự phục vụ) là chặn được. Tài khoản cán bộ khá hơn — thẻ làm mới bị thu hồi — nhưng thẻ
/// đang cầm vẫn sống nốt một giờ.
///
/// Lệnh "tạm khóa thẻ" của VI.1 sinh ra để dừng bạn đọc lại ngay lúc thư viện bấm nút; canh ở từng
/// bộ xử lý thì chỗ thứ mười lại quên, nên câu hỏi này thuộc tầng xác thực.
/// </summary>
[Collection(ApiCollection.Name)]
public class SessionRevocationTests
{
    private readonly LibraryConnectFactory _factory;

    public SessionRevocationTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> AdminAsync() =>
        _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            "máy chủ trả về {0}: {1}", response.StatusCode, await response.Content.ReadAsStringAsync());

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(LibraryConnectFactory.JsonOptions);

        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue(payload.Message);

        return payload.Data!;
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private const string MatKhau = "BanDocThu@2026";

    [Fact]
    public async Task Khoa_the_ban_doc_thi_phien_dang_mo_dung_lai_ngay()
    {
        var admin = await AdminAsync();
        var (readerId, doc) = await NewReaderSessionAsync(admin);

        // Phiên đang chạy bình thường.
        var truoc = await doc.GetAsync("/api/reader/card");
        truoc.StatusCode.Should().Be(HttpStatusCode.OK);

        await ReadAsync<BulkResultDto>(await admin.PostAsJsonAsync("/api/readers/lock", new
        {
            selection = new { readerIds = new[] { readerId } },
            locked = true,
            reason = $"Báo mất thẻ {Unique()}"
        }));

        // Bảy lối mà phiên cũ trước đây vẫn đi được.
        foreach (var path in new[]
                 {
                     "/api/reader/card",
                     "/api/reader/profile",
                     "/api/reader/loans/current",
                     "/api/reader/fines",
                     "/api/reader/notifications",
                     "/api/reader/saved-searches",
                     "/api/reader/digital/requests"
                 })
        {
            var response = await doc.GetAsync(path);

            response.StatusCode.Should().Be(
                HttpStatusCode.Unauthorized,
                "thẻ đã khóa mà {0} vẫn trả lời thì lệnh khóa của thư viện không có tác dụng", path);
        }

        // Và lối ghi cũng vậy.
        var sua = await doc.PutAsJsonAsync("/api/reader/profile", new { phone = "0900000009" });
        sua.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Khoa_the_ban_doc_thi_khong_lam_moi_duoc_phien()
    {
        var admin = await AdminAsync();
        var (readerId, _, lamMoi) = await NewReaderSessionWithRefreshAsync(admin);

        await ReadAsync<BulkResultDto>(await admin.PostAsJsonAsync("/api/readers/lock", new
        {
            selection = new { readerIds = new[] { readerId } },
            locked = true,
            reason = $"Báo mất thẻ {Unique()}"
        }));

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/reader/auth/refresh", new { refreshToken = lamMoi });

        response.IsSuccessStatusCode.Should().BeFalse(
            "làm mới được thẻ là phiên của người vừa bị khóa kéo dài vô thời hạn");
    }

    [Fact]
    public async Task Mo_khoa_thi_ban_doc_dang_nhap_lai_va_lam_viec_binh_thuong()
    {
        var admin = await AdminAsync();
        var (readerId, card) = await NewReaderCardAsync(admin);

        await ReadAsync<BulkResultDto>(await admin.PostAsJsonAsync("/api/readers/lock", new
        {
            selection = new { readerIds = new[] { readerId } },
            locked = true,
            reason = $"Kiểm mở khóa {Unique()}"
        }));

        await ReadAsync<BulkResultDto>(await admin.PostAsJsonAsync("/api/readers/lock", new
        {
            selection = new { readerIds = new[] { readerId } },
            locked = false,
            reason = "Đã trả lại thẻ"
        }));

        var doc = await ReaderClientAsync(card);
        var response = await doc.GetAsync("/api/reader/card");

        response.StatusCode.Should().Be(
            HttpStatusCode.OK, "mở khóa xong thì bạn đọc phải dùng được thẻ lại ngay");
    }

    [Fact]
    public async Task Khoa_tai_khoan_can_bo_thi_the_dang_cam_het_gia_tri_ngay()
    {
        var admin = await AdminAsync();

        var groups = await ReadAsync<PagedResult<UserGroupListItemDto>>(
            await admin.GetAsync("/api/admin/user-groups?pageSize=50"));

        var group = groups.Items.First(item => item.Code == "CIRCULATION");
        var username = $"kt{Unique()}";

        var created = await ReadAsync<CreateUserResult>(await admin.PostAsJsonAsync("/api/admin/users", new
        {
            username,
            profile = new
            {
                fullName = $"Cán bộ kiểm khóa {Unique()}",
                email = $"{username}@thu.example.vn",
                groupIds = new[] { group.Id },
                isActive = true
            }
        }));

        var can_bo = _factory.CreateClient();
        var dangNhap = await ReadAsync<AuthResultDto>(await can_bo.PostAsJsonAsync(
            "/api/auth/login", new { username, password = created.TemporaryPassword }));

        can_bo.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", dangNhap.AccessToken);

        await can_bo.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = created.TemporaryPassword,
            newPassword = "CanBoThu@2026",
            confirmPassword = "CanBoThu@2026"
        });

        var lai = await ReadAsync<AuthResultDto>(await can_bo.PostAsJsonAsync(
            "/api/auth/login", new { username, password = "CanBoThu@2026" }));

        can_bo.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lai.AccessToken);

        (await can_bo.GetAsync("/api/circulation/loans?pageSize=1")).StatusCode
            .Should().Be(HttpStatusCode.OK, "cán bộ đang làm việc bình thường");

        var khoa = await admin.PostAsJsonAsync(
            $"/api/admin/users/{created.Id}/lock", new { locked = true, reason = "Nghỉ việc" });
        khoa.IsSuccessStatusCode.Should().BeTrue();

        (await can_bo.GetAsync("/api/circulation/loans?pageSize=1")).StatusCode
            .Should().Be(
                HttpStatusCode.Unauthorized,
                "thẻ đang cầm còn sống thêm một giờ là một giờ người vừa nghỉ việc vẫn vào được hệ thống");
    }


    /// <summary>
    /// Công nợ trên hồ sơ phải theo kịp khoản phạt vừa lập.
    ///
    /// Cột `debt_amount` là bản chép sẵn của tổng phạt chưa thu; trang cá nhân của bạn đọc trên ứng
    /// dụng di động đọc đúng cột ấy, còn quầy thì cộng thẳng từ bảng phạt. Trước 07/09/2026 cột này
    /// chỉ được đồng bộ khi **thu** và khi **miễn** phạt: quầy nói "còn nợ 12.000 đ" mà bạn đọc mở
    /// ứng dụng ra thấy "còn nợ 0 đ".
    /// </summary>
    [Fact]
    public async Task Lap_khoan_phat_thi_cong_no_tren_ho_so_theo_kip()
    {
        var admin = await AdminAsync();
        var (readerId, _) = await NewReaderCardAsync(admin);

        await ReadAsync<FineRowDto>(await admin.PostAsJsonAsync("/api/circulation/fines", new
        {
            readerId,
            type = "Other",
            amount = 12_000m,
            note = $"Kiểm công nợ {Unique()}"
        }));

        var reader = await ReadAsync<ReaderDetailDto>(await admin.GetAsync($"/api/readers/{readerId}"));

        reader.DebtAmount.Should().Be(
            12_000m,
            "quầy cộng thẳng từ bảng phạt còn ứng dụng di động đọc cột chép sẵn; hai con số lệch "
            + "nhau thì bạn đọc tin con số nào cũng sai");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<Application.Common.Interfaces.IApplicationDbContext>();

        var luuTrongKho = await db.Readers
            .Where(entity => entity.Id == readerId)
            .Select(entity => entity.DebtAmount)
            .FirstAsync();

        luuTrongKho.Should().Be(12_000m, "cột chép sẵn trong kho mới là thứ ứng dụng di động đọc");
    }

    // ---------------------------------------------------------------------------------------

    private async Task<(Guid ReaderId, string CardNumber)> NewReaderCardAsync(HttpClient admin)
    {
        var types = await ReadAsync<PagedResult<CatalogItemDto>>(
            await admin.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));

        var readerId = await ReadAsync<Guid>(await admin.PostAsJsonAsync("/api/readers", new
        {
            fullName = $"Bạn đọc kiểm khóa {Unique()}",
            studentCode = $"SV{Unique()}",
            readerTypeId = types.Items.First(item => item.Code == "SV").Id
        }));

        var reader = await ReadAsync<ReaderDetailDto>(await admin.GetAsync($"/api/readers/{readerId}"));

        return (readerId, reader.CardNumber);
    }

    private async Task<HttpClient> ReaderClientAsync(string cardNumber)
    {
        var admin = await AdminAsync();
        var readers = await ReadAsync<PagedResult<ReaderDto>>(
            await admin.GetAsync($"/api/readers?keyword={cardNumber}&pageSize=5"));

        var readerId = readers.Items.First(item => item.CardNumber == cardNumber).Id;

        var tam = await ReadAsync<string>(
            await admin.PostAsJsonAsync($"/api/readers/{readerId}/reset-password", new { }));

        var client = _factory.CreateClient();
        var login = await ReadAsync<AuthResultDto>(await client.PostAsJsonAsync(
            "/api/reader/auth/login", new { cardNumber, password = tam }));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        await client.PostAsJsonAsync("/api/reader/auth/change-password", new
        {
            currentPassword = tam,
            newPassword = MatKhau,
            confirmPassword = MatKhau
        });

        var lai = await ReadAsync<AuthResultDto>(await client.PostAsJsonAsync(
            "/api/reader/auth/login", new { cardNumber, password = MatKhau }));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", lai.AccessToken);

        return client;
    }

    private async Task<(Guid ReaderId, HttpClient Client)> NewReaderSessionAsync(HttpClient admin)
    {
        var (readerId, card) = await NewReaderCardAsync(admin);
        return (readerId, await ReaderClientAsync(card));
    }

    private async Task<(Guid ReaderId, HttpClient Client, string RefreshToken)>
        NewReaderSessionWithRefreshAsync(HttpClient admin)
    {
        var (readerId, card) = await NewReaderCardAsync(admin);
        var client = _factory.CreateClient();

        var readers = await ReadAsync<PagedResult<ReaderDto>>(
            await admin.GetAsync($"/api/readers?keyword={card}&pageSize=5"));

        var tam = await ReadAsync<string>(
            await admin.PostAsJsonAsync($"/api/readers/{readers.Items[0].Id}/reset-password", new { }));

        var login = await ReadAsync<AuthResultDto>(await client.PostAsJsonAsync(
            "/api/reader/auth/login", new { cardNumber = card, password = tam }));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        return (readerId, client, login.RefreshToken);
    }
}
