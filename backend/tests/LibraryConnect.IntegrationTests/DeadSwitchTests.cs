using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Api.Controllers;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Application.Features.Circulation;
using LibraryConnect.Infrastructure.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Bốn công tắc trên màn hình Tham số hệ thống lưu được mà không nơi nào đọc, cộng một lịch chạy
/// nền chỉ đọc lúc khởi động (đợt rà thứ hai mươi hai, 08/09/2026).
///
/// Mỗi phép thử ở đây đổi đúng một tham số rồi hỏi hệ thống có làm theo không — phép thử quét
/// <c>SystemParameterReadersTests</c> chỉ nói được "có chỗ đọc", còn đây mới nói "đọc rồi có tác
/// dụng". Tham số được trả về giá trị cũ sau mỗi phép thử: các phép thử khác dùng chung một máy chủ.
/// </summary>
[Collection(ApiCollection.Name)]
public class DeadSwitchTests
{
    private readonly LibraryConnectFactory _factory;

    public DeadSwitchTests(LibraryConnectFactory factory) => _factory = factory;

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            "máy chủ trả về {0}: {1}", response.StatusCode, await response.Content.ReadAsStringAsync());

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(LibraryConnectFactory.JsonOptions);

        payload!.Success.Should().BeTrue(payload.Message);

        return payload.Data!;
    }

    /// <summary>Đặt tham số rồi trả về nguyên trạng khi xong.</summary>
    private async Task VoiThamSoAsync(string khoa, string? giaTri, Func<Task> than)
    {
        using var scope = _factory.Services.CreateScope();
        var parameters = scope.ServiceProvider.GetRequiredService<ISystemParameterService>();
        var cu = await parameters.GetAsync(khoa);

        await parameters.SetAsync(khoa, giaTri);

        try
        {
            await than();
        }
        finally
        {
            await parameters.SetAsync(khoa, cu);
        }
    }

    [Fact]
    public async Task Tat_kho_OAI_thi_khong_ai_thu_hoach_duoc_nua()
    {
        var anonymous = _factory.CreateClient();

        // Bật (mặc định) thì kho trả lời đúng chuẩn.
        var mo = await anonymous.GetAsync("/oai?verb=Identify");
        mo.StatusCode.Should().Be(HttpStatusCode.OK);

        await VoiThamSoAsync(ProtocolController.OaiEnabledParameter, "false", async () =>
        {
            foreach (var verb in new[] { "Identify", "ListRecords&metadataPrefix=oai_dc", "ListIdentifiers&metadataPrefix=oai_dc" })
            {
                var dong = await anonymous.GetAsync($"/oai?verb={verb}");

                dong.StatusCode.Should().Be(HttpStatusCode.NotFound,
                    "tắt công tắc \"Mở kho OAI-PMH của mình\" mà verb {0} vẫn phục vụ thì thư viện "
                    + "khác cứ thu hoạch tiếp toàn bộ kho", verb);
            }

            // POST cũng là một lối vào của chuẩn — bịt một lối mà quên lối kia là không bịt gì cả.
            var post = await anonymous.PostAsync("/oai",
                new FormUrlEncodedContent(new Dictionary<string, string> { ["verb"] = "Identify" }));

            post.StatusCode.Should().Be(HttpStatusCode.NotFound);
        });

        // Bật lại thì kho sống lại ngay, không cần khởi động máy chủ.
        var lai = await anonymous.GetAsync("/oai?verb=Identify");
        lai.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Tat_canh_bao_trung_thi_ca_hai_loi_deu_thoi_canh_bao()
    {
        var client = await _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

        // Dựng lấy bối cảnh: một tài liệu thư viện đã có, để lượt tra trùng có gì mà trúng.
        var them = await client.PostAsJsonAsync("/api/acquisition/quick-catalog", new
        {
            title = "Giáo trình kiểm thử công tắc cấu hình",
            author = "Nguyễn Văn Kiểm",
            isbn = "978-604-77-9911-3",
            price = 100000m,
            itemQuantity = 0,
        });

        them.IsSuccessStatusCode.Should().BeTrue(
            "phải biên mục sơ lược được thì mới có cái để tra trùng: {0}",
            await them.Content.ReadAsStringAsync());

        const string doDuong = "/api/acquisition/requests/duplicate-check?isbn=978-604-77-9911-3";

        var bat = await ReadAsync<PurchaseDuplicateDto?>(await client.GetAsync(doDuong));

        bat.Should().NotBeNull("bối cảnh của phép thử phải có một tài liệu trùng thật");

        await VoiThamSoAsync(PurchaseDuplicateFinder.EnabledParameter, "false", async () =>
        {
            var tat = await ReadAsync<PurchaseDuplicateDto?>(await client.GetAsync(doDuong));

            tat.Should().BeNull(
                "tắt \"Cảnh báo khi tài liệu đã có trong thư viện\" mà màn hình vẫn cảnh báo thì "
                + "công tắc ấy không có nghĩa gì");

            // Lối thứ hai hỏi đúng câu ấy: lượt tự tra khi lưu yêu cầu đặt mua. Tắt một lối mà quên
            // lối kia thì người quản trị vẫn thấy cảnh báo ở chỗ họ không ngờ tới (bài học 42).
            var yeuCau = await client.PostAsJsonAsync("/api/acquisition/requests", new
            {
                type = "MONOGRAPH",
                requesterName = "Nguyễn Thị Đề Nghị",
                department = "Khoa Kiểm thử",
                reason = "Kiểm công tắc cảnh báo trùng",
                items = new[]
                {
                    new
                    {
                        title = "Giáo trình kiểm thử công tắc cấu hình",
                        isbn = "978-604-77-9911-3",
                        quantity = 1,
                        unitPrice = 100000m,
                    },
                },
            });

            yeuCau.IsSuccessStatusCode.Should().BeTrue(await yeuCau.Content.ReadAsStringAsync());

            var id = await ReadAsync<Guid>(yeuCau);
            var chiTiet = await ReadAsync<PurchaseRequestDetailDto>(
                await client.GetAsync($"/api/acquisition/requests/{id}"));

            chiTiet.Items[0].IsDuplicate.Should().BeFalse(
                "lượt tự tra khi lưu phải theo cùng một công tắc với nút tra nhanh");
        });
    }

    [Fact]
    public async Task Doi_lich_thu_hoach_thi_viec_nen_nhan_lich_moi_ngay_lap_tuc()
    {
        using var scope = _factory.Services.CreateScope();
        var jobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
        var parameters = scope.ServiceProvider.GetRequiredService<ISystemParameterService>();
        var cu = await parameters.GetAsync(BackupScheduleRefresher.HarvestCronParameter);

        var client = await _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

        try
        {
            var luu = await client.PutAsJsonAsync("/api/admin/parameters", new
            {
                parameters = new[]
                {
                    new { key = BackupScheduleRefresher.HarvestCronParameter, value = "15 5 * * *" },
                },
            });

            luu.IsSuccessStatusCode.Should().BeTrue();

            jobs.GetRecurringCron(RecurringJobRegistrar.OaiHarvestJobId).Should().Be("15 5 * * *",
                "màn hình hiện lịch mới mà bộ chạy nền giữ lịch cũ tới lần khởi động lại thì người "
                + "quản trị không có cách nào biết");
        }
        finally
        {
            await client.PutAsJsonAsync("/api/admin/parameters", new
            {
                parameters = new[]
                {
                    new { key = BackupScheduleRefresher.HarvestCronParameter, value = cu ?? "0 2 * * *" },
                },
            });
        }
    }
}
