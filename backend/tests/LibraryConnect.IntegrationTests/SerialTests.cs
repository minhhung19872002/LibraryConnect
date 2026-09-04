using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Locations;
using LibraryConnect.Application.Features.Serials;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Phân hệ IV — Ấn phẩm định kỳ, chạy thật qua HTTP: khai kỳ hạn, sinh số dự kiến, ghi nhận số đến,
/// khiếu nại số thiếu, mục lục bài trích, đóng tập và báo cáo.
/// </summary>
[Collection(ApiCollection.Name)]
public class SerialTests
{
    private readonly LibraryConnectFactory _factory;

    public SerialTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> ClientAsync() =>
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

    private static async Task<Guid> WarehouseAsync(HttpClient client)
    {
        var warehouses = await ReadAsync<IReadOnlyList<WarehouseDto>>(
            await client.GetAsync("/api/locations/warehouses"));

        return warehouses[0].Id;
    }

    /// <summary>Khai một đầu báo mới với kỳ hạn truyền vào.</summary>
    private static Task<Guid> NewSerialAsync(
        HttpClient client,
        string title,
        SerialFrequency frequency,
        object pattern,
        Guid? warehouseId = null,
        decimal price = 30000) =>
        client.PostAsJsonAsync("/api/serials", new
        {
            title,
            issn = $"1234-{Math.Abs(title.GetHashCode()) % 10000:0000}",
            frequency,
            pattern,
            warehouseId,
            pricePerIssue = price,
            copiesPerIssue = 1,
            subscriptionStart = "2026-01-01",
            subscriptionEnd = "2026-12-31",
            isActive = true,
            ddc = "070.4"
        }).ContinueWith(task => ReadAsync<Guid>(task.Result)).Unwrap();

    // -----------------------------------------------------------------------------------------
    // IV.1 và IV.4 — Khai báo và sinh số
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Phieu_khieu_nai_in_ra_duoc_thanh_van_ban_gui_nha_cung_cap()
    {
        // IV.3 nói "tạo phiếu khiếu nại gửi nhà cung cấp". Trước 04/09/2026 chỉ có bản ghi trên màn
        // hình, không có tờ giấy nào để ký và gửi đi.
        var client = await ClientAsync();

        // Tự dựng một phiếu khiếu nại: bài này phải luôn đi qua đường in, không đỗ suông khi kho
        // kiểm thử tình cờ chưa có khiếu nại nào.
        var serialId = await NewSerialAsync(
            client, "Tạp chí In Khiếu Nại", SerialFrequency.Monthly, new { dayOfMonth = 5 });

        await ReadAsync<GenerateIssuesResultDto>(await client.PostAsJsonAsync(
            "/api/serials/issues/generate",
            new { serialIds = new[] { serialId }, from = "2026-01-01", to = "2026-03-31" }));

        var issues = await ReadAsync<PagedResult<SerialIssueDto>>(
            await client.GetAsync($"/api/serials/issues?serialId={serialId}&pageSize=50"));

        var missing = issues.Items.Take(2).Select(issue => issue.Id).ToArray();

        await ReadAsync<int>(await client.PostAsJsonAsync(
            "/api/serials/issues/mark-missing", new { issueIds = missing, note = "Chưa thấy về" }));

        var created = await ReadAsync<CreateClaimsResultDto>(await client.PostAsJsonAsync(
            "/api/serials/claims", new { issueIds = missing }));

        created.ClaimNumbers.Should().NotBeEmpty();
        var claimNo = created.ClaimNumbers[0];
        var response = await client.GetAsync($"/api/acquisition/forms/print/SERIAL_CLAIM/{claimNo}");

        response.IsSuccessStatusCode.Should().BeTrue(
            "in phiếu khiếu nại phải chạy được, máy chủ trả: " + await response.Content.ReadAsStringAsync());
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        (await response.Content.ReadAsByteArrayAsync()).Length.Should().BeGreaterThan(500);

        // Số phiếu không có thật thì báo không tìm thấy, không dựng ra một tờ giấy trống.
        (await client.GetAsync("/api/acquisition/forms/print/SERIAL_CLAIM/KN-KHONG-CO"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_new_title_gets_its_own_marc_record_marked_as_a_serial()
    {
        var client = await ClientAsync();

        var id = await NewSerialAsync(
            client, "Tạp chí Thư viện Việt Nam", SerialFrequency.Monthly, new { dayOfMonth = 15 });

        var serial = await ReadAsync<SerialDetailDto>(await client.GetAsync($"/api/serials/{id}"));

        serial.Title.Should().Be("Tạp chí Thư viện Việt Nam");
        serial.ControlNumber.Should().NotBeNullOrWhiteSpace();
        serial.Pattern.DayOfMonth.Should().Be(15);

        // Biểu ghi phải nhận ra được là ấn phẩm nhiều kỳ: leader vị trí 07 = 's'.
        var marc = await ReadAsync<Application.Features.Cataloging.BibDetailDto>(
            await client.GetAsync($"/api/cataloging/bibs/{serial.BibId}"));

        var record = LibraryConnect.Marc.MarcJson.Deserialize(marc.MarcJson);

        record.Leader.BibliographicLevel.Should().Be('s');
        record.GetSubfield("022", 'a').Should().NotBeNullOrWhiteSpace();
        record.GetSubfield("310", 'a').Should().Be("Tháng");
    }

    [Fact]
    public async Task Preview_predicts_issues_without_writing_anything()
    {
        var client = await ClientAsync();

        var id = await NewSerialAsync(
            client, "Báo Thư viện Tuần", SerialFrequency.Weekly, new { dayOfWeek = 5 });

        var preview = await ReadAsync<IReadOnlyList<IssuePreviewDto>>(
            await client.GetAsync($"/api/serials/{id}/issues/preview?from=2026-01-01&to=2026-01-31"));

        preview.Should().HaveCount(5);
        preview.Should().OnlyContain(issue => issue.ExpectedDate.DayOfWeek == DayOfWeek.Friday);

        // Xem trước không được để lại gì trong cơ sở dữ liệu.
        var issues = await ReadAsync<PagedResult<SerialIssueDto>>(
            await client.GetAsync($"/api/serials/issues?serialId={id}&pageSize=100"));

        issues.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Generating_twice_over_the_same_range_does_not_duplicate_issues()
    {
        var client = await ClientAsync();

        var id = await NewSerialAsync(
            client, "Tạp chí Sinh số Hai Lần", SerialFrequency.Monthly, new { dayOfMonth = 1 });

        var first = await ReadAsync<GenerateIssuesResultDto>(await client.PostAsJsonAsync(
            "/api/serials/issues/generate",
            new { serialIds = new[] { id }, from = "2026-01-01", to = "2026-06-30" }));

        first.Created.Should().Be(6);

        // Lần hai trên đúng khoảng đó không sinh thêm số nào.
        var again = await client.PostAsJsonAsync(
            "/api/serials/issues/generate",
            new { serialIds = new[] { id }, from = "2026-01-01", to = "2026-06-30" });

        again.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Mở rộng khoảng thì chỉ sinh phần còn thiếu.
        var extended = await ReadAsync<GenerateIssuesResultDto>(await client.PostAsJsonAsync(
            "/api/serials/issues/generate",
            new { serialIds = new[] { id }, from = "2026-01-01", to = "2026-12-31" }));

        extended.Created.Should().Be(6);
        extended.Skipped.Should().Be(6);
    }

    [Fact]
    public async Task Several_titles_can_be_generated_in_one_go()
    {
        var client = await ClientAsync();

        var monthly = await NewSerialAsync(
            client, "Tạp chí Tổng thể A", SerialFrequency.Monthly, new { dayOfMonth = 1 });
        var quarterly = await NewSerialAsync(
            client, "Tạp chí Tổng thể B", SerialFrequency.Quarterly, new { dayOfMonth = 1 });

        var result = await ReadAsync<GenerateIssuesResultDto>(await client.PostAsJsonAsync(
            "/api/serials/issues/generate",
            new { serialIds = new[] { monthly, quarterly }, from = "2026-01-01", to = "2026-12-31" }));

        // 12 số hàng tháng cộng 4 số hàng quý.
        result.Created.Should().Be(16);
    }

    [Fact]
    public async Task A_hand_edited_preview_is_what_gets_saved()
    {
        var client = await ClientAsync();

        var id = await NewSerialAsync(
            client, "Tạp chí Sửa Tay", SerialFrequency.Quarterly, new { dayOfMonth = 1 });

        var result = await ReadAsync<GenerateIssuesResultDto>(await client.PostAsJsonAsync(
            "/api/serials/issues/generate",
            new
            {
                serialIds = new[] { id },
                issues = new[]
                {
                    new { issueNo = "Đặc biệt", volume = (string?)null, year = 2026,
                          expectedDate = "2026-04-30", caption = "Số đặc biệt (2026)" }
                }
            }));

        result.Created.Should().Be(1);

        var issues = await ReadAsync<PagedResult<SerialIssueDto>>(
            await client.GetAsync($"/api/serials/issues?serialId={id}&pageSize=50"));

        issues.Items.Should().ContainSingle();
        issues.Items[0].IssueNo.Should().Be("Đặc biệt");
        issues.Items[0].ExpectedDate.Should().Be(new DateOnly(2026, 4, 30));
    }

    // -----------------------------------------------------------------------------------------
    // IV.3 và IV.4 — Ghi nhận, thiếu, khiếu nại
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Receiving_an_issue_creates_a_real_copy_in_the_stack()
    {
        var client = await ClientAsync();
        var warehouseId = await WarehouseAsync(client);

        var id = await NewSerialAsync(
            client, "Tạp chí Ghi Nhận", SerialFrequency.Monthly, new { dayOfMonth = 1 }, warehouseId);

        await ReadAsync<GenerateIssuesResultDto>(await client.PostAsJsonAsync(
            "/api/serials/issues/generate",
            new { serialIds = new[] { id }, from = "2026-01-01", to = "2026-03-31" }));

        var issues = await ReadAsync<PagedResult<SerialIssueDto>>(
            await client.GetAsync($"/api/serials/issues?serialId={id}&pageSize=50"));

        issues.Items.Should().HaveCount(3);
        issues.Items.Should().OnlyContain(issue => issue.Status == SerialIssueStatus.Expected);

        var received = await ReadAsync<ReceiveIssuesResultDto>(await client.PostAsJsonAsync(
            "/api/serials/issues/receive",
            new
            {
                issues = new[]
                {
                    new { issueId = issues.Items[0].Id, quantity = 2, receivedDate = "2026-01-03",
                          note = (string?)null },
                },
                createItems = true,
                warehouseId
            }));

        received.Received.Should().Be(1);
        received.CreatedItems.Should().Be(2);
        received.Barcodes.Should().HaveCount(2);

        // Bản nhận về là ấn phẩm thật trong kho, cho mượn được ngay chứ không chờ kiểm nhận.
        var stock = await ReadAsync<PagedResult<Application.Features.Acquisition.StockItemDto>>(
            await client.PostAsJsonAsync("/api/stock/items/search", new
            {
                page = 1,
                pageSize = 20,
                filter = new { keyword = received.Barcodes[0] }
            }));

        stock.Items.Should().ContainSingle();
        stock.Items[0].Status.Should().Be(ItemStatus.InStock);
        stock.Items[0].IsLocked.Should().BeFalse();

        // Ghi nhận lại số đã nhận thì bị chặn.
        var again = await client.PostAsJsonAsync("/api/serials/issues/receive", new
        {
            issues = new[]
            {
                new { issueId = issues.Items[0].Id, quantity = 1, receivedDate = (string?)null,
                      note = (string?)null },
            },
            createItems = false
        });

        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Missing_issues_turn_into_a_claim_and_back_again_when_it_is_cancelled()
    {
        var client = await ClientAsync();

        var id = await NewSerialAsync(
            client, "Tạp chí Khiếu Nại", SerialFrequency.Monthly, new { dayOfMonth = 1 });

        await ReadAsync<GenerateIssuesResultDto>(await client.PostAsJsonAsync(
            "/api/serials/issues/generate",
            new { serialIds = new[] { id }, from = "2026-01-01", to = "2026-02-28" }));

        var issues = await ReadAsync<PagedResult<SerialIssueDto>>(
            await client.GetAsync($"/api/serials/issues?serialId={id}&pageSize=50"));

        var affected = await ReadAsync<int>(await client.PostAsJsonAsync(
            "/api/serials/issues/mark-missing",
            new { issueIds = new[] { issues.Items[0].Id }, note = "Chưa thấy về" }));

        affected.Should().Be(1);

        var claims = await ReadAsync<CreateClaimsResultDto>(await client.PostAsJsonAsync(
            "/api/serials/claims", new { issueIds = new[] { issues.Items[0].Id } }));

        claims.Created.Should().Be(1);
        claims.ClaimNumbers.Should().ContainSingle();

        var afterClaim = await ReadAsync<PagedResult<SerialIssueDto>>(
            await client.GetAsync($"/api/serials/issues?serialId={id}&pageSize=50"));

        afterClaim.Items[0].Status.Should().Be(SerialIssueStatus.Claimed);
        afterClaim.Items[0].HasOpenClaim.Should().BeTrue();

        // Khiếu nại lần hai trên số đang có phiếu mở thì bị chặn.
        var duplicate = await client.PostAsJsonAsync(
            "/api/serials/claims", new { issueIds = new[] { issues.Items[0].Id } });

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var list = await ReadAsync<IReadOnlyList<SerialClaimDto>>(
            await client.GetAsync($"/api/serials/claims?serialId={id}"));

        list.Should().ContainSingle();
        list[0].Content.Should().Contain("Tạp chí Khiếu Nại");

        // Hủy khiếu nại thì số quay lại trạng thái thiếu để vẫn nằm trong danh sách theo dõi.
        var responded = await client.PostAsJsonAsync(
            $"/api/serials/claims/{list[0].Id}/respond",
            new { response = "Nhà cung cấp báo không còn số này", status = SerialClaimStatus.Cancelled });

        responded.IsSuccessStatusCode.Should().BeTrue();

        var afterCancel = await ReadAsync<PagedResult<SerialIssueDto>>(
            await client.GetAsync($"/api/serials/issues?serialId={id}&pageSize=50"));

        afterCancel.Items[0].Status.Should().Be(SerialIssueStatus.Missing);
    }

    [Fact]
    public async Task Batch_receiving_lists_due_issues_of_every_title_and_reconciles_the_missing_ones()
    {
        var client = await ClientAsync();
        var warehouseId = await WarehouseAsync(client);

        // Hai đầu báo khác kỳ hạn, cùng có số đến hạn từ đầu năm — màn hình bổ sung tổng thể phải
        // nhìn thấy cả hai mà không phải mở từng đầu báo.
        var monthly = await NewSerialAsync(
            client, "Tạp chí Tổng Thể Tháng", SerialFrequency.Monthly, new { dayOfMonth = 1 }, warehouseId);
        var quarterly = await NewSerialAsync(
            client, "Tạp chí Tổng Thể Quý", SerialFrequency.Quarterly, new { dayOfMonth = 1 }, warehouseId);

        await ReadAsync<GenerateIssuesResultDto>(await client.PostAsJsonAsync(
            "/api/serials/issues/generate",
            new { serialIds = new[] { monthly, quarterly }, from = "2026-01-01", to = "2026-03-31" }));

        var due = await ReadAsync<PagedResult<SerialIssueDto>>(
            await client.GetAsync("/api/serials/issues?dueOnly=true&pageSize=500"));

        due.Items.Should().Contain(issue => issue.SerialId == monthly);
        due.Items.Should().Contain(issue => issue.SerialId == quarterly);

        var monthlyIssues = due.Items.Where(issue => issue.SerialId == monthly)
            .OrderBy(issue => issue.ExpectedDate).ToList();
        var quarterlyIssue = due.Items.First(issue => issue.SerialId == quarterly);

        // Nhận hàng loạt: mỗi dòng một số lượng và một ngày nhận riêng, trải trên hai đầu báo.
        var received = await ReadAsync<ReceiveIssuesResultDto>(await client.PostAsJsonAsync(
            "/api/serials/issues/receive", new
            {
                issues = new object[]
                {
                    new { issueId = monthlyIssues[0].Id, quantity = 2, receivedDate = "2026-01-05", note = (string?)null },
                    new { issueId = quarterlyIssue.Id, quantity = 1, receivedDate = "2026-01-09", note = (string?)null }
                },
                createItems = true
            }));

        received.Received.Should().Be(2);
        received.CreatedItems.Should().Be(3);

        // Số tháng 2 ghi thiếu, số tháng 3 vẫn dự kiến nhưng đã quá hạn: cả hai phải nằm trong danh
        // sách đối chiếu, còn số đã nhận thì không.
        await ReadAsync<int>(await client.PostAsJsonAsync(
            "/api/serials/issues/mark-missing", new { issueIds = new[] { monthlyIssues[1].Id } }));

        var unresolved = await ReadAsync<PagedResult<SerialIssueDto>>(
            await client.GetAsync("/api/serials/issues?unresolvedOnly=true&pageSize=500"));

        unresolved.Items.Should().Contain(issue => issue.Id == monthlyIssues[1].Id && issue.Status == SerialIssueStatus.Missing);
        unresolved.Items.Should().Contain(issue => issue.Id == monthlyIssues[2].Id && issue.IsOverdue);
        unresolved.Items.Should().NotContain(issue => issue.Id == monthlyIssues[0].Id);
        unresolved.Items.Should().NotContain(issue => issue.Id == quarterlyIssue.Id);

        // Một phiếu khiếu nại cho mỗi số, dù các số thuộc đầu báo khác nhau.
        var claims = await ReadAsync<CreateClaimsResultDto>(await client.PostAsJsonAsync(
            "/api/serials/claims",
            new { issueIds = new[] { monthlyIssues[1].Id, monthlyIssues[2].Id } }));

        claims.Created.Should().Be(2);

        var afterClaim = await ReadAsync<PagedResult<SerialIssueDto>>(
            await client.GetAsync("/api/serials/issues?unresolvedOnly=true&pageSize=500"));

        // Đang khiếu nại vẫn là "chưa về": danh sách đối chiếu giữ chúng lại để theo dõi phản hồi.
        afterClaim.Items.Should().Contain(issue => issue.Id == monthlyIssues[1].Id && issue.HasOpenClaim);
    }

    [Fact]
    public async Task The_issue_grid_groups_by_year_and_counts_each_state()
    {
        var client = await ClientAsync();
        var warehouseId = await WarehouseAsync(client);

        var id = await NewSerialAsync(
            client, "Tạp chí Lưới Nhận Số", SerialFrequency.Quarterly, new { dayOfMonth = 1 }, warehouseId);

        await ReadAsync<GenerateIssuesResultDto>(await client.PostAsJsonAsync(
            "/api/serials/issues/generate",
            new { serialIds = new[] { id }, from = "2026-01-01", to = "2026-12-31" }));

        var issues = await ReadAsync<PagedResult<SerialIssueDto>>(
            await client.GetAsync($"/api/serials/issues?serialId={id}&pageSize=50"));

        await ReadAsync<ReceiveIssuesResultDto>(await client.PostAsJsonAsync(
            "/api/serials/issues/receive",
            new
            {
                issues = issues.Items.Take(2).Select(issue => new
                {
                    issueId = issue.Id,
                    quantity = 1,
                    receivedDate = (string?)null,
                    note = (string?)null
                }),
                createItems = true,
                warehouseId
            }));

        var grid = await ReadAsync<IReadOnlyList<IssueGridYearDto>>(
            await client.GetAsync($"/api/serials/{id}/grid"));

        grid.Should().ContainSingle();
        grid[0].Year.Should().Be(2026);
        grid[0].Received.Should().Be(2);
        grid[0].Expected.Should().Be(2);
        grid[0].Cells.Should().HaveCount(4);

        var summary = await ReadAsync<IReadOnlyList<SerialSummaryRowDto>>(
            await client.GetAsync($"/api/serials/{id}/summary"));

        summary.Should().ContainSingle();
        summary[0].Planned.Should().Be(4);
        summary[0].Received.Should().Be(2);
        summary[0].ReceivedPercent.Should().Be(50);
    }

    // -----------------------------------------------------------------------------------------
    // IV.2 — Bài trích
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Articles_become_analytic_records_linked_to_the_host_by_field_773()
    {
        var client = await ClientAsync();

        var id = await NewSerialAsync(
            client, "Tạp chí Bài Trích", SerialFrequency.Monthly, new { dayOfMonth = 1 });

        await ReadAsync<GenerateIssuesResultDto>(await client.PostAsJsonAsync(
            "/api/serials/issues/generate",
            new { serialIds = new[] { id }, from = "2026-01-01", to = "2026-01-31" }));

        var issues = await ReadAsync<PagedResult<SerialIssueDto>>(
            await client.GetAsync($"/api/serials/issues?serialId={id}&pageSize=10"));

        var issueId = issues.Items[0].Id;

        await ReadAsync<int>(await client.PutAsJsonAsync(
            $"/api/serials/issues/{issueId}/articles",
            new
            {
                articles = new[]
                {
                    new
                    {
                        title = "Ứng dụng học máy trong thư viện số",
                        authors = "Nguyễn Văn An; Trần Thị Bình",
                        pageFrom = 15,
                        pageTo = 23,
                        @abstract = "Bài viết trình bày cách áp dụng học máy vào tra cứu.",
                        keywords = "học máy; thư viện số"
                    }
                }
            }));

        var articles = await ReadAsync<IReadOnlyList<SerialArticleDto>>(
            await client.GetAsync($"/api/serials/issues/{issueId}/articles"));

        articles.Should().ContainSingle();
        articles[0].BibId.Should().BeNull();

        var generated = await ReadAsync<GenerateArticleRecordsResultDto>(
            await client.PostAsJsonAsync(
                $"/api/serials/issues/{issueId}/articles/generate-records",
                new { articleIds = Array.Empty<Guid>() }));

        generated.Created.Should().Be(1);

        var withRecord = await ReadAsync<IReadOnlyList<SerialArticleDto>>(
            await client.GetAsync($"/api/serials/issues/{issueId}/articles"));

        withRecord[0].BibId.Should().NotBeNull();

        var marc = await ReadAsync<Application.Features.Cataloging.BibDetailDto>(
            await client.GetAsync($"/api/cataloging/bibs/{withRecord[0].BibId}"));

        var record = LibraryConnect.Marc.MarcJson.Deserialize(marc.MarcJson);

        // Mức thư mục 'a' = bộ phận của một tài liệu nhiều kỳ.
        record.Leader.BibliographicLevel.Should().Be('a');
        record.GetSubfield("245", 'a').Should().Be("Ứng dụng học máy trong thư viện số");
        record.GetSubfield("100", 'a').Should().Be("Nguyễn Văn An");
        record.GetSubfield("700", 'a').Should().Be("Trần Thị Bình");
        record.GetSubfield("300", 'a').Should().Be("tr. 15-23");
        record.GetSubfield("773", 't').Should().Be("Tạp chí Bài Trích");
        record.GetSubfield("773", 'g').Should().Contain("tr. 15-23");

        // Sinh lần hai không tạo biểu ghi trùng.
        var second = await client.PostAsJsonAsync(
            $"/api/serials/issues/{issueId}/articles/generate-records",
            new { articleIds = Array.Empty<Guid>() });

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Và bài đã có biểu ghi thì không xóa khỏi mục lục được.
        var removal = await client.PutAsJsonAsync(
            $"/api/serials/issues/{issueId}/articles",
            new { articles = Array.Empty<object>() });

        removal.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Article_catalogue_can_be_imported_from_excel()
    {
        var client = await ClientAsync();

        var id = await NewSerialAsync(
            client, "Tạp chí Nhập Mục Lục", SerialFrequency.Monthly, new { dayOfMonth = 1 });

        await ReadAsync<GenerateIssuesResultDto>(await client.PostAsJsonAsync(
            "/api/serials/issues/generate",
            new { serialIds = new[] { id }, from = "2026-01-01", to = "2026-01-31" }));

        var issues = await ReadAsync<PagedResult<SerialIssueDto>>(
            await client.GetAsync($"/api/serials/issues?serialId={id}&pageSize=10"));

        var template = await client.GetAsync("/api/serials/articles/excel-template");
        template.IsSuccessStatusCode.Should().BeTrue();

        var file = ExcelFixtures.BuildArticleSheet(new[]
        {
            new[] { "Chuyển đổi số trong thư viện đại học", "Lê Văn Cường", "5", "12",
                    "Tóm tắt bài", "chuyển đổi số" },
            new[] { "", "Thiếu nhan đề", "1", "2", "", "" },
            new[] { "Trang ngược", "Phạm Thị Dung", "30", "20", "", "" }
        });

        using var content = new MultipartFormDataContent();
        using var upload = new ByteArrayContent(file);
        upload.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(upload, "file", "muc-luc.xlsx");

        var result = await ReadAsync<ImportArticlesResultDto>(
            await client.PostAsync($"/api/serials/issues/{issues.Items[0].Id}/articles/import", content));

        result.Imported.Should().Be(1);
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(error => error.Message.Contains("nhan đề"));
        result.Errors.Should().Contain(error => error.Message.Contains("Trang kết thúc"));
    }

    // -----------------------------------------------------------------------------------------
    // IV.4 — Đóng tập
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Binding_a_run_of_issues_produces_one_new_copy_and_retires_the_singles()
    {
        var client = await ClientAsync();
        var warehouseId = await WarehouseAsync(client);

        var id = await NewSerialAsync(
            client, "Tạp chí Đóng Tập", SerialFrequency.Quarterly, new { dayOfMonth = 1 },
            warehouseId, price: 50000);

        await ReadAsync<GenerateIssuesResultDto>(await client.PostAsJsonAsync(
            "/api/serials/issues/generate",
            new { serialIds = new[] { id }, from = "2026-01-01", to = "2026-12-31" }));

        var issues = await ReadAsync<PagedResult<SerialIssueDto>>(
            await client.GetAsync($"/api/serials/issues?serialId={id}&pageSize=50"));

        // Chưa nhận số nào thì không đóng tập được — tập rỗng ruột là tập vô nghĩa.
        var tooEarly = await client.PostAsJsonAsync(
            "/api/serials/bindings", new { serialId = id, year = 2026 });

        tooEarly.StatusCode.Should().Be(HttpStatusCode.Conflict);

        await ReadAsync<ReceiveIssuesResultDto>(await client.PostAsJsonAsync(
            "/api/serials/issues/receive",
            new
            {
                issues = issues.Items.Select(issue => new
                {
                    issueId = issue.Id,
                    quantity = 1,
                    receivedDate = (string?)null,
                    note = (string?)null
                }),
                createItems = true,
                warehouseId
            }));

        var binding = await ReadAsync<SerialBindingDto>(await client.PostAsJsonAsync(
            "/api/serials/bindings",
            new { serialId = id, year = 2026, callNumber = "070.4 TAP 2026", price = 200000m }));

        binding.IssueCount.Should().Be(4);
        binding.FromIssue.Should().Be("1");
        binding.ToIssue.Should().Be("4");
        binding.Barcode.Should().NotBeNullOrWhiteSpace();

        // Tập đóng là một ấn phẩm thật trong kho.
        var stock = await ReadAsync<PagedResult<Application.Features.Acquisition.StockItemDto>>(
            await client.PostAsJsonAsync("/api/stock/items/search", new
            {
                page = 1,
                pageSize = 20,
                filter = new { keyword = binding.Barcode }
            }));

        stock.Items.Should().ContainSingle();
        stock.Items[0].CallNumber.Should().Be("070.4 TAP 2026");
        stock.Items[0].Price.Should().Be(200000m);

        // Các số lẻ chuyển sang "đã đóng tập" nhưng vẫn còn trong sổ nhận số.
        var afterBinding = await ReadAsync<PagedResult<SerialIssueDto>>(
            await client.GetAsync($"/api/serials/issues?serialId={id}&pageSize=50"));

        afterBinding.Items.Should().HaveCount(4);
        afterBinding.Items.Should().OnlyContain(issue => issue.Status == SerialIssueStatus.Bound);
        afterBinding.Items.Should().OnlyContain(issue => issue.BindingId == binding.Id);

        var bindings = await ReadAsync<IReadOnlyList<SerialBindingDto>>(
            await client.GetAsync($"/api/serials/bindings?serialId={id}"));

        bindings.Should().ContainSingle();
    }

    [Fact]
    public async Task Binding_by_issue_range_takes_only_that_run_and_prints_a_spine_label()
    {
        var client = await ClientAsync();
        var warehouseId = await WarehouseAsync(client);

        var id = await NewSerialAsync(
            client, "Tạp chí Đóng Nửa Năm", SerialFrequency.Quarterly, new { dayOfMonth = 1 },
            warehouseId, price: 40000);

        await ReadAsync<GenerateIssuesResultDto>(await client.PostAsJsonAsync(
            "/api/serials/issues/generate",
            new { serialIds = new[] { id }, from = "2026-01-01", to = "2026-12-31" }));

        var issues = await ReadAsync<PagedResult<SerialIssueDto>>(
            await client.GetAsync($"/api/serials/issues?serialId={id}&pageSize=50"));

        await ReadAsync<ReceiveIssuesResultDto>(await client.PostAsJsonAsync(
            "/api/serials/issues/receive",
            new
            {
                issues = issues.Items.Select(issue => new
                {
                    issueId = issue.Id, quantity = 1, receivedDate = (string?)null, note = (string?)null
                }),
                createItems = true,
                warehouseId
            }));

        // Chọn khoảng số 1–2: chỉ hai số ấy vào tập, số 3–4 vẫn là số lẻ đã nhận.
        var first = await ReadAsync<SerialBindingDto>(await client.PostAsJsonAsync(
            "/api/serials/bindings",
            new { serialId = id, year = 2026, fromIssue = "1", toIssue = "2" }));

        first.IssueCount.Should().Be(2);
        first.FromIssue.Should().Be("1");
        first.ToIssue.Should().Be("2");
        first.ItemId.Should().NotBeNull();

        var afterFirst = await ReadAsync<PagedResult<SerialIssueDto>>(
            await client.GetAsync($"/api/serials/issues?serialId={id}&pageSize=50"));

        afterFirst.Items.Count(issue => issue.Status == SerialIssueStatus.Bound).Should().Be(2);
        afterFirst.Items.Count(issue => issue.Status == SerialIssueStatus.Received).Should().Be(2);

        // Số không có trong năm thì báo rõ, không âm thầm đóng cả năm.
        var unknown = await client.PostAsJsonAsync(
            "/api/serials/bindings",
            new { serialId = id, year = 2026, fromIssue = "3", toIssue = "9" });

        unknown.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var second = await ReadAsync<SerialBindingDto>(await client.PostAsJsonAsync(
            "/api/serials/bindings",
            new { serialId = id, year = 2026, fromIssue = "3", toIssue = "4" }));

        second.IssueCount.Should().Be(2);

        // Nhãn gáy của tập in bằng đúng dịch vụ in nhãn của Phân hệ III, theo ĐKCB của tập.
        var label = await client.PostAsJsonAsync("/api/stock/print/labels",
            new { itemIds = new[] { first.ItemId!.Value }, copies = 1 });

        label.IsSuccessStatusCode.Should().BeTrue(await label.Content.ReadAsStringAsync());
        (await label.Content.ReadAsByteArrayAsync()).Take(5)
            .Should().Equal((byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-');
    }

    [Fact]
    public async Task A_title_that_has_received_issues_cannot_be_deleted()
    {
        var client = await ClientAsync();
        var warehouseId = await WarehouseAsync(client);

        var id = await NewSerialAsync(
            client, "Tạp chí Không Xóa Được", SerialFrequency.Monthly, new { dayOfMonth = 1 }, warehouseId);

        await ReadAsync<GenerateIssuesResultDto>(await client.PostAsJsonAsync(
            "/api/serials/issues/generate",
            new { serialIds = new[] { id }, from = "2026-01-01", to = "2026-02-28" }));

        var issues = await ReadAsync<PagedResult<SerialIssueDto>>(
            await client.GetAsync($"/api/serials/issues?serialId={id}&pageSize=10"));

        await ReadAsync<ReceiveIssuesResultDto>(await client.PostAsJsonAsync(
            "/api/serials/issues/receive",
            new
            {
                issues = new[]
                {
                    new { issueId = issues.Items[0].Id, quantity = 1, receivedDate = (string?)null,
                          note = (string?)null }
                },
                createItems = false
            }));

        var response = await client.DeleteAsync($"/api/serials/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task A_title_with_only_predicted_issues_can_still_be_deleted()
    {
        var client = await ClientAsync();

        var id = await NewSerialAsync(
            client, "Tạp chí Xóa Được", SerialFrequency.Monthly, new { dayOfMonth = 1 });

        await ReadAsync<GenerateIssuesResultDto>(await client.PostAsJsonAsync(
            "/api/serials/issues/generate",
            new { serialIds = new[] { id }, from = "2026-01-01", to = "2026-03-31" }));

        var response = await client.DeleteAsync($"/api/serials/{id}");
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    // -----------------------------------------------------------------------------------------
    // IV.5 — Báo cáo
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Statistics_cover_every_dimension_the_specification_asks_for()
    {
        var client = await ClientAsync();
        var warehouseId = await WarehouseAsync(client);

        var id = await NewSerialAsync(
            client, "Tạp chí Báo Cáo", SerialFrequency.Monthly, new { dayOfMonth = 1 }, warehouseId);

        await ReadAsync<GenerateIssuesResultDto>(await client.PostAsJsonAsync(
            "/api/serials/issues/generate",
            new { serialIds = new[] { id }, from = "2026-01-01", to = "2026-03-31" }));

        var issues = await ReadAsync<PagedResult<SerialIssueDto>>(
            await client.GetAsync($"/api/serials/issues?serialId={id}&pageSize=10"));

        await ReadAsync<ReceiveIssuesResultDto>(await client.PostAsJsonAsync(
            "/api/serials/issues/receive",
            new
            {
                issues = issues.Items.Select(issue => new
                {
                    issueId = issue.Id,
                    quantity = 2,
                    receivedDate = (string?)null,
                    note = (string?)null
                }),
                createItems = false
            }));

        var dimensions = await ReadAsync<IReadOnlyDictionary<string, string>>(
            await client.GetAsync("/api/serials/reports/dimensions"));

        dimensions.Should().ContainKey(SerialDimensions.Overall);
        dimensions.Should().ContainKey(SerialDimensions.Classification);
        dimensions.Should().ContainKey(SerialDimensions.Frequency);
        dimensions.Should().ContainKey(SerialDimensions.Language);

        foreach (var dimension in dimensions.Keys)
        {
            var report = await ReadAsync<SerialStatReportDto>(await client.PostAsJsonAsync(
                $"/api/serials/reports/statistics?dimension={dimension}", new SerialReportFilter()));

            report.TotalTitles.Should().BeGreaterThan(0, "chiều {0} phải đếm được đầu báo", dimension);
            report.Rows.Sum(row => row.ReceivedIssues).Should().Be(report.TotalReceivedIssues);
            report.Rows.Should().OnlyContain(row => !string.IsNullOrWhiteSpace(row.Label));
        }

        // Chiều môn loại gộp theo lớp trăm của DDC, không liệt kê từng chỉ số lẻ.
        var byClass = await ReadAsync<SerialStatReportDto>(await client.PostAsJsonAsync(
            $"/api/serials/reports/statistics?dimension={SerialDimensions.Classification}",
            new SerialReportFilter()));

        byClass.Rows.Should().Contain(row => row.Label.StartsWith("000 —"));

        var invalid = await client.PostAsJsonAsync(
            "/api/serials/reports/statistics?dimension=KHONG-CO", new SerialReportFilter());

        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Serial_reports_export_to_excel_and_pdf()
    {
        var client = await ClientAsync();

        var excel = await client.PostAsJsonAsync(
            $"/api/serials/reports/export?dimension={SerialDimensions.Frequency}&format=Excel",
            new SerialReportFilter());

        excel.IsSuccessStatusCode.Should().BeTrue();
        (await excel.Content.ReadAsByteArrayAsync()).Take(2).Should().Equal((byte)'P', (byte)'K');

        var pdf = await client.PostAsJsonAsync(
            $"/api/serials/reports/export?dimension={SerialDimensions.Overall}&format=Pdf",
            new SerialReportFilter());

        pdf.IsSuccessStatusCode.Should().BeTrue();
        (await pdf.Content.ReadAsByteArrayAsync()).Take(5)
            .Should().Equal((byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-');
    }
}
