using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Domain.Entities.Cat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Ba lỗi của đợt rà thứ ba, tìm ra khi sửa một biểu ghi bằng chuột trên trình soạn MARC thật.
///
/// Cả ba đều qua được bộ kiểm thử cũ vì bộ ấy chỉ tạo mới rồi sửa nhan đề — chưa bao giờ <i>thêm</i>
/// một điểm truy cập vào biểu ghi đã có, và chưa bao giờ có hơn hai trăm tác giả cùng họ trong kho.
/// </summary>
[Collection(ApiCollection.Name)]
public class BibEditReviewTests
{
    private readonly LibraryConnectFactory _factory;

    public BibEditReviewTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> ClientAsync() =>
        _factory.CreateAuthenticatedClientAsync(LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    /// <summary>
    /// H4 — Thêm tác giả vào biểu ghi đã có phải lưu được.
    ///
    /// Trước khi sửa: 409 "Không lưu được dữ liệu", vì liên kết mới thêm qua navigation của một
    /// biểu ghi đang Unchanged được Entity Framework theo dõi ở trạng thái Modified, phát UPDATE cho
    /// một dòng chưa từng tồn tại, và 0 dòng bị ảnh hưởng thành DbUpdateConcurrencyException.
    /// </summary>
    [Fact]
    public async Task Them_diem_truy_cap_moi_vao_bieu_ghi_da_co_thi_luu_duoc()
    {
        var client = await ClientAsync();
        var marker = Guid.NewGuid().ToString("N")[..8];

        var id = await SaveAsync(client, $"Biểu ghi rà thứ ba {marker}", author: null, keyword: null);

        var marc = await MarcAsync(client, id);
        marc.AddField("100", '1', ' ').AddSubfield('a', $"Người Mới Toanh {marker}");
        marc.AddField("653", ' ', ' ').AddSubfield('a', $"từ khóa mới {marker}");

        var response = await client.PutAsJsonAsync($"/api/cataloging/bibs/{id}", new
        {
            marcJson = LibraryConnect.Marc.MarcJson.Serialize(marc),
            status = "Published",
            changeNote = "Thêm tác giả và từ khóa"
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(
            HttpStatusCode.OK, "máy chủ trả về: " + await response.Content.ReadAsStringAsync());

        var detail = await client.GetFromJsonAsync<ApiResponse<BibDetailDto>>(
            $"/api/cataloging/bibs/{id}", LibraryConnectFactory.JsonOptions);

        detail!.Data!.AuthorMain.Should().Be($"Người Mới Toanh {marker}");
    }

    /// <summary>
    /// H5 — Tên đã có trong hồ sơ thẩm quyền phải được tìm ra dù có hàng trăm tên cùng họ.
    ///
    /// Bộ đối chiếu cũ lọc sơ bộ theo từ đầu tiên rồi chỉ lấy 200 ứng viên: "Nguyễn" khớp 3.060 tác
    /// giả trên kho thật, tên cần tìm nằm ngoài 200 dòng đầu, và lần lưu thứ hai của <i>cùng một</i>
    /// biểu ghi tạo thêm "NGUYEN_VAN_KIEM_2". Đo được 485 nhóm tác giả trùng trong kho.
    /// </summary>
    [Fact]
    public async Task Ten_da_co_khong_bi_tao_trung_du_co_hang_tram_ten_cung_ho()
    {
        var client = await ClientAsync();
        var marker = Guid.NewGuid().ToString("N")[..8];
        var ho = $"Nguyenra{marker}";
        var ten = $"{ho} Văn Kiểm";

        // Dựng đúng bối cảnh của kho thật: hơn hai trăm tác giả cùng họ, tạo trước tên cần tìm để
        // nó nằm ngoài "200 dòng đầu" của bộ lọc sơ bộ cũ.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            for (var index = 0; index < 220; index++)
            {
                db.Authors.Add(new Author
                {
                    Id = Guid.NewGuid(),
                    Code = $"{ho.ToUpperInvariant()}_{index:D3}",
                    Name = $"{ho} Thị {index:D3}",
                    FullName = $"{ho} Thị {index:D3}",
                    IsActive = true
                });
            }

            await db.SaveChangesAsync();
        }

        var first = await SaveAsync(client, $"Sách thứ nhất {marker}", author: ten, keyword: null);
        var second = await SaveAsync(client, $"Sách thứ hai {marker}", author: ten, keyword: null);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var soTacGia = await db.Authors.CountAsync(author => author.Name == ten);
            soTacGia.Should().Be(1, "hai biểu ghi cùng một tác giả chỉ được tạo một mục thẩm quyền");

            var lienKet = await db.BibAuthors
                .Where(link => link.BibId == first || link.BibId == second)
                .Select(link => link.AuthorId)
                .Distinct()
                .CountAsync();
            lienKet.Should().Be(1, "cả hai biểu ghi phải trỏ về cùng một tác giả");
        }
    }

    /// <summary>
    /// H2 — Trường con để trống của khung mẫu không được ghi vào biểu ghi.
    ///
    /// Khung mẫu điền sẵn hai chục trường; cán bộ điền ba rồi Ctrl+S. Trước khi sửa, biểu ghi lưu
    /// nguyên 24 trường con rỗng, xuất ISO 2709 mang cả "020 ## $a $c" sang thư viện khác, và trang
    /// "Xem MARC" của bạn đọc hiện chúng ra.
    /// </summary>
    [Fact]
    public async Task Truong_con_de_trong_cua_khung_mau_khong_duoc_luu_vao_bieu_ghi()
    {
        var client = await ClientAsync();
        var documentTypeId = await DocumentTypeIdAsync(client, "SACH");

        var blank = await client.GetFromJsonAsync<ApiResponse<NewBibRecordDto>>(
            $"/api/cataloging/bibs/new?documentTypeId={documentTypeId}", LibraryConnectFactory.JsonOptions);

        var marc = LibraryConnect.Marc.MarcJson.Deserialize(blank!.Data!.MarcJson);

        // Khung mẫu phải thật sự mang trường con rỗng, nếu không phép thử này không kiểm gì cả.
        marc.DataFields.SelectMany(field => field.Subfields)
            .Should().Contain(subfield => string.IsNullOrWhiteSpace(subfield.Value),
                "khung mẫu điền sẵn các trường con để cán bộ điền vào");

        var title = marc.GetField("245") ?? marc.AddField("245", '1', '0');
        var titleA = title.Subfields.FirstOrDefault(subfield => subfield.Code == 'a');
        if (titleA is null) title.AddSubfield('a', "Kiểm thử khung mẫu rỗng");
        else titleA.Value = "Kiểm thử khung mẫu rỗng";

        var response = await client.PostAsJsonAsync("/api/cataloging/bibs", new
        {
            marcJson = LibraryConnect.Marc.MarcJson.Serialize(marc),
            documentTypeId,
            status = "Published"
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var saved = await response.Content.ReadFromJsonAsync<ApiResponse<SaveBibResultDto>>(
            LibraryConnectFactory.JsonOptions);

        var stored = await MarcAsync(client, saved!.Data!.Id);

        stored.DataFields.SelectMany(field => field.Subfields)
            .Where(subfield => string.IsNullOrWhiteSpace(subfield.Value))
            .Select(subfield => "$" + subfield.Code)
            .Should().BeEmpty("trường con rỗng không mang thông tin gì, chỉ mang rác sang kho khác");

        stored.DataFields.Should().OnlyContain(field => field.Subfields.Count > 0,
            "trường không còn trường con nào thì phải bỏ luôn");

        stored.GetField("245")!.GetSubfields('a').Should().ContainSingle()
            .Which.Should().Be("Kiểm thử khung mẫu rỗng");
        stored.GetField("040").Should().NotBeNull("giá trị ngầm định có nội dung thì giữ nguyên");
    }

    private static async Task<LibraryConnect.Marc.MarcRecord> MarcAsync(HttpClient client, Guid id)
    {
        var detail = await client.GetFromJsonAsync<ApiResponse<BibDetailDto>>(
            $"/api/cataloging/bibs/{id}", LibraryConnectFactory.JsonOptions);

        return LibraryConnect.Marc.MarcJson.Deserialize(detail!.Data!.MarcJson);
    }

    private static async Task<Guid> DocumentTypeIdAsync(HttpClient client, string code)
    {
        var types = await client.GetFromJsonAsync<ApiResponse<PagedResult<Application.Features.Catalogs.CatalogItemDto>>>(
            "/api/catalogs/document-types/items?pageSize=100", LibraryConnectFactory.JsonOptions);

        return types!.Data!.Items.Single(item => item.Code == code).Id;
    }

    private static async Task<Guid> SaveAsync(HttpClient client, string title, string? author, string? keyword)
    {
        var fields = new List<object>
        {
            new
            {
                tag = "245",
                ind1 = "1",
                ind2 = "0",
                subfields = new[] { new { code = "a", value = title } }
            }
        };

        if (author is not null)
        {
            fields.Insert(0, new
            {
                tag = "100",
                ind1 = "1",
                ind2 = " ",
                subfields = new[] { new { code = "a", value = author } }
            });
        }

        if (keyword is not null)
        {
            fields.Add(new
            {
                tag = "653",
                ind1 = " ",
                ind2 = " ",
                subfields = new[] { new { code = "a", value = keyword } }
            });
        }

        var marc = JsonSerializer.Serialize(new
        {
            leader = "00000nam a2200000 a 4500",
            controlFields = new[] { new { tag = "008", value = "260101s2024    vm a     b    000 0 vie d" } },
            dataFields = fields
        });

        var response = await client.PostAsJsonAsync("/api/cataloging/bibs", new { marcJson = marc, status = "Published" });

        response.IsSuccessStatusCode.Should().BeTrue(
            "lưu biểu ghi phải thành công, máy chủ trả về: " + await response.Content.ReadAsStringAsync());

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<SaveBibResultDto>>(
            LibraryConnectFactory.JsonOptions);

        return payload!.Data!.Id;
    }
}
