using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Marc;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Hàng đợi biên mục chi tiết (II.4).
///
/// The queue is what makes quick cataloguing and bulk import safe to use: a record captured in a
/// hurry is not lost, it is parked here until somebody catalogues it properly. These tests follow a
/// job through the states a real one goes through, including being sent back.
/// </summary>
[Collection(ApiCollection.Name)]
public class CatalogQueueTests
{
    private readonly LibraryConnectFactory _factory;

    public CatalogQueueTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> ClientAsync() =>
        _factory.CreateAuthenticatedClientAsync(LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    private static async Task<Guid> CreateRecordAsync(HttpClient client, string title)
    {
        var record = new MarcRecord();
        record.Leader.RecordType = 'a';
        record.Leader.BibliographicLevel = 'm';
        record.SetControlField("008", "240115s2023    vm a     b    000 0 vie d");
        record.AddField("040").AddSubfield('a', "VN-TEST");
        record.AddField("245", '1', '0').AddSubfield('a', title);

        var response = await client.PostAsJsonAsync("/api/cataloging/bibs", new
        {
            marcJson = MarcJson.Serialize(record),
            status = "Draft"
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<SaveBibResultDto>>(
            LibraryConnectFactory.JsonOptions);

        return payload!.Data!.Id;
    }

    private static async Task<Guid> EnqueueAsync(HttpClient client, Guid bibId, int priority = 3)
    {
        var response = await client.PostAsJsonAsync("/api/cataloging/queue", new
        {
            bibId,
            priority,
            note = "Cần biên mục chi tiết"
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>(LibraryConnectFactory.JsonOptions);

        return payload!.Data;
    }

    private static async Task<CatalogQueueItemDto> FindAsync(HttpClient client, Guid queueId, string status)
    {
        var page = await client.GetFromJsonAsync<ApiResponse<PagedResult<CatalogQueueItemDto>>>(
            $"/api/cataloging/queue?status={status}&pageSize=200", LibraryConnectFactory.JsonOptions);

        return page!.Data!.Items.Single(item => item.Id == queueId);
    }

    private static Task ChangeStatusAsync(HttpClient client, Guid queueId, string status, string? reason = null) =>
        client.PostAsJsonAsync($"/api/cataloging/queue/{queueId}/status", new { status, reason },
            LibraryConnectFactory.JsonOptions);

    [Fact]
    public async Task A_record_put_in_the_queue_waits_unassigned()
    {
        var client = await ClientAsync();
        var bibId = await CreateRecordAsync(client, $"Biểu ghi chờ biên mục {Guid.NewGuid():N}");
        var queueId = await EnqueueAsync(client, bibId);

        var item = await FindAsync(client, queueId, "Pending");

        item.BibId.Should().Be(bibId);
        item.AssignedTo.Should().BeNull();
        item.Priority.Should().Be(3);
        item.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public async Task The_same_record_cannot_be_queued_twice_while_it_is_still_open()
    {
        var client = await ClientAsync();
        var bibId = await CreateRecordAsync(client, $"Biểu ghi trùng hàng đợi {Guid.NewGuid():N}");
        await EnqueueAsync(client, bibId);

        var second = await client.PostAsJsonAsync("/api/cataloging/queue", new { bibId },
            LibraryConnectFactory.JsonOptions);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Assigning_a_job_moves_it_into_the_in_progress_column()
    {
        var client = await ClientAsync();
        var bibId = await CreateRecordAsync(client, $"Biểu ghi phân công {Guid.NewGuid():N}");
        var queueId = await EnqueueAsync(client, bibId);

        var me = await client.GetFromJsonAsync<ApiResponse<CurrentUserProbe>>(
            "/api/auth/me", LibraryConnectFactory.JsonOptions);

        var response = await client.PostAsJsonAsync("/api/cataloging/queue/assign", new
        {
            ids = new[] { queueId },
            assignedTo = me!.Data!.Id,
            priority = 1,
            deadline = DateOnly.FromDateTime(DateTime.Today.AddDays(7))
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var item = await FindAsync(client, queueId, "InProgress");

        item.AssignedTo.Should().Be(me.Data.Id);
        item.AssignedToName.Should().NotBeNullOrEmpty();
        item.Priority.Should().Be(1);
        item.Deadline.Should().NotBeNull();
    }

    [Fact]
    public async Task A_job_walks_from_taken_through_review_to_finished()
    {
        var client = await ClientAsync();
        var bibId = await CreateRecordAsync(client, $"Biểu ghi đi hết quy trình {Guid.NewGuid():N}");
        var queueId = await EnqueueAsync(client, bibId);

        await ChangeStatusAsync(client, queueId, "InProgress");
        var taken = await FindAsync(client, queueId, "InProgress");
        taken.AssignedToName.Should().NotBeNullOrEmpty("nhận việc thì việc đó thuộc về người nhận");
        taken.StartedAt.Should().NotBeNull();

        await ChangeStatusAsync(client, queueId, "WaitingApproval");
        await FindAsync(client, queueId, "WaitingApproval");

        await ChangeStatusAsync(client, queueId, "Completed");
        var done = await FindAsync(client, queueId, "Completed");
        done.CompletedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Duyệt xong việc thì biểu ghi phải lên OPAC.
    ///
    /// Cả hàng đợi sinh ra là để đưa một biểu ghi thô thành biểu ghi tra cứu được. Nếu trạng thái
    /// việc chạy hết vòng mà biểu ghi vẫn nằm ở "Chờ biên mục" thì bạn đọc không bao giờ thấy nó,
    /// và cán bộ không có chỗ nào khác để xuất bản biểu ghi — cả quy trình thành ngõ cụt.
    /// </summary>
    [Fact]
    public async Task Duyet_xong_thi_bieu_ghi_len_OPAC()
    {
        var client = await ClientAsync();
        var title = $"Biểu ghi duyệt xong lên OPAC {Guid.NewGuid():N}";
        var bibId = await CreateRecordAsync(client, title);
        var queueId = await EnqueueAsync(client, bibId);

        var anonymous = _factory.CreateClient();
        anonymous.DefaultRequestHeaders.Authorization = null;

        var before = await SearchAsync(anonymous, title);
        before.Should().Be(0, "biểu ghi chưa biên mục xong thì chưa được lên trang tra cứu");

        await ChangeStatusAsync(client, queueId, "InProgress");
        await ChangeStatusAsync(client, queueId, "WaitingApproval");
        await ChangeStatusAsync(client, queueId, "Completed");

        var detail = await client.GetFromJsonAsync<ApiResponse<BibDetailDto>>(
            $"/api/cataloging/bibs/{bibId}", LibraryConnectFactory.JsonOptions);

        detail!.Data!.Status.Should().Be(Domain.Enums.RecordStatus.Published,
            "duyệt xong việc biên mục nghĩa là biểu ghi đã dùng được");

        var after = await SearchAsync(anonymous, title);
        after.Should().Be(1, "bạn đọc phải tra ra biểu ghi vừa được duyệt");
    }

    /// <summary>
    /// Trả việc lại thì biểu ghi phải rút khỏi OPAC.
    ///
    /// Một biểu ghi đã duyệt rồi bị phát hiện sai, cán bộ trả lại để sửa: nếu nó vẫn nằm trên trang
    /// tra cứu thì bạn đọc vẫn thấy bản sai suốt thời gian sửa.
    /// </summary>
    [Fact]
    public async Task Tra_lai_viec_thi_bieu_ghi_rut_khoi_OPAC()
    {
        var client = await ClientAsync();
        var title = $"Biểu ghi bị rút khỏi OPAC {Guid.NewGuid():N}";
        var bibId = await CreateRecordAsync(client, title);
        var queueId = await EnqueueAsync(client, bibId);

        var anonymous = _factory.CreateClient();
        anonymous.DefaultRequestHeaders.Authorization = null;

        await ChangeStatusAsync(client, queueId, "InProgress");
        await ChangeStatusAsync(client, queueId, "Completed");
        (await SearchAsync(anonymous, title)).Should().Be(1);

        await ChangeStatusAsync(client, queueId, "Returned", "Sai chỉ số phân loại");

        (await SearchAsync(anonymous, title)).Should().Be(0,
            "biểu ghi đang phải sửa lại thì không để bạn đọc tra ra bản sai");
    }

    private static async Task<int> SearchAsync(HttpClient anonymous, string title)
    {
        var page = await anonymous.GetFromJsonAsync<ApiResponse<PagedResult<Application.Features.Opac.OpacResultDto>>>(
            $"/api/search?keyword={Uri.EscapeDataString(title)}&page=1&pageSize=5",
            LibraryConnectFactory.JsonOptions);

        return page!.Data!.TotalCount;
    }

    [Fact]
    public async Task Sending_a_job_back_requires_a_reason_and_the_reason_is_shown_to_the_cataloguer()
    {
        var client = await ClientAsync();
        var bibId = await CreateRecordAsync(client, $"Biểu ghi bị trả lại {Guid.NewGuid():N}");
        var queueId = await EnqueueAsync(client, bibId);

        await ChangeStatusAsync(client, queueId, "InProgress");
        await ChangeStatusAsync(client, queueId, "WaitingApproval");

        var refused = await client.PostAsJsonAsync($"/api/cataloging/queue/{queueId}/status",
            new { status = "Returned" }, LibraryConnectFactory.JsonOptions);

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var accepted = await client.PostAsJsonAsync($"/api/cataloging/queue/{queueId}/status",
            new { status = "Returned", reason = "Thiếu chỉ số phân loại và đề mục chủ đề" },
            LibraryConnectFactory.JsonOptions);

        accepted.StatusCode.Should().Be(HttpStatusCode.OK);

        var item = await FindAsync(client, queueId, "Returned");
        item.ReturnReason.Should().Be("Thiếu chỉ số phân loại và đề mục chủ đề");
    }

    [Fact]
    public async Task The_summary_counts_the_jobs_in_each_column()
    {
        var client = await ClientAsync();
        var bibId = await CreateRecordAsync(client, $"Biểu ghi đếm cột {Guid.NewGuid():N}");
        await EnqueueAsync(client, bibId);

        var summary = await client.GetFromJsonAsync<ApiResponse<CatalogQueueSummaryDto>>(
            "/api/cataloging/queue/summary", LibraryConnectFactory.JsonOptions);

        summary!.Data!.Pending.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Productivity_reports_what_each_cataloguer_finished()
    {
        var client = await ClientAsync();
        var bibId = await CreateRecordAsync(client, $"Biểu ghi năng suất {Guid.NewGuid():N}");
        var queueId = await EnqueueAsync(client, bibId);

        await ChangeStatusAsync(client, queueId, "InProgress");
        await ChangeStatusAsync(client, queueId, "Completed");

        var productivity = await client.GetFromJsonAsync<ApiResponse<List<CatalogProductivityDto>>>(
            "/api/cataloging/queue/productivity", LibraryConnectFactory.JsonOptions);

        productivity!.Data!.Should().NotBeEmpty();
        productivity.Data!.Sum(item => item.Completed).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Removing_a_job_from_the_queue_leaves_the_record_alone()
    {
        var client = await ClientAsync();
        var bibId = await CreateRecordAsync(client, $"Biểu ghi bỏ khỏi hàng đợi {Guid.NewGuid():N}");
        var queueId = await EnqueueAsync(client, bibId);

        var response = await client.DeleteAsync($"/api/cataloging/queue/{queueId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var record = await client.GetAsync($"/api/cataloging/bibs/{bibId}");
        record.StatusCode.Should().Be(HttpStatusCode.OK, "bỏ việc khỏi hàng đợi không đụng đến biểu ghi");
    }

    /// <summary>Chỉ cần định danh người dùng hiện tại để phân công việc cho chính mình.</summary>
    private class CurrentUserProbe
    {
        public Guid Id { get; set; }
    }
    /// <summary>
    /// Duyệt hàng loạt.
    ///
    /// Một lượt thu hoạch từ thư viện bạn đưa về hàng nghìn biểu ghi cùng một nguồn, cùng một chất
    /// lượng. Bắt cán bộ bấm duyệt từng biểu ghi một là chức năng có cũng như không — họ sẽ tìm cách
    /// khác, hoặc bỏ luôn cả kho ấy ngoài trang tra cứu.
    /// </summary>
    [Fact]
    public async Task Duyet_hang_loat_dua_ca_nhom_bieu_ghi_len_OPAC()
    {
        var client = await ClientAsync();
        var marker = Guid.NewGuid().ToString("N")[..8];

        var queueIds = new List<Guid>();

        for (var index = 0; index < 3; index++)
        {
            var bibId = await CreateRecordAsync(client, $"Biểu ghi duyệt hàng loạt {marker} {index}");
            queueIds.Add(await EnqueueAsync(client, bibId));
        }

        var anonymous = _factory.CreateClient();
        anonymous.DefaultRequestHeaders.Authorization = null;

        (await SearchAsync(anonymous, marker)).Should().Be(0);

        var response = await client.PostAsJsonAsync("/api/cataloging/queue/status", new
        {
            ids = queueIds,
            status = "Completed"
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<int>>(
            LibraryConnectFactory.JsonOptions);

        payload!.Data.Should().Be(3);

        (await SearchAsync(anonymous, marker)).Should().Be(3,
            "cả nhóm vừa duyệt phải tra ra được ngay");
    }

    [Fact]
    public async Task Tra_lai_hang_loat_van_bat_buoc_neu_ly_do()
    {
        var client = await ClientAsync();
        var bibId = await CreateRecordAsync(client, $"Biểu ghi trả hàng loạt {Guid.NewGuid():N}");
        var queueId = await EnqueueAsync(client, bibId);

        var thieuLyDo = await client.PostAsJsonAsync("/api/cataloging/queue/status", new
        {
            ids = new[] { queueId },
            status = "Returned"
        }, LibraryConnectFactory.JsonOptions);

        thieuLyDo.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "trả việc lại mà không nói vì sao thì cán bộ biên mục không biết sửa gì");
    }

    /// <summary>Ô ghi chú của hộp thoại phân công (II.4) phải đi tới việc được giao.</summary>
    [Fact]
    public async Task Phan_cong_kem_ghi_chu_thi_ghi_chu_duoc_luu()
    {
        var client = await ClientAsync();
        var bibId = await CreateRecordAsync(client, $"Sách ghi chú {Guid.NewGuid():N}");
        var queueId = await EnqueueAsync(client, bibId);

        var me = await client.GetFromJsonAsync<ApiResponse<CurrentUserProbe>>(
            "/api/auth/me", LibraryConnectFactory.JsonOptions);

        var response = await client.PostAsJsonAsync("/api/cataloging/queue/assign", new
        {
            ids = new[] { queueId },
            assignedTo = me!.Data!.Id,
            note = "  Ưu tiên bổ sung đề mục chủ đề  "
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var item = await FindAsync(client, queueId, "InProgress");

        item.Note.Should().Be("Ưu tiên bổ sung đề mục chủ đề");
    }

    /// <summary>
    /// Xóa biểu ghi thì việc biên mục của nó phải biến khỏi hàng đợi — cả trong danh sách lẫn trong
    /// bộ đếm.
    ///
    /// Trước 07/09/2026, dòng việc nằm lại: màn hình hàng đợi trên máy chủ thật báo 981 việc "Chờ xử
    /// lý" mà một trang 200 dòng chỉ trả về 155, thiếu đúng 45 việc của biểu ghi đã xóa, và trang
    /// cuối rỗng trơn. Phần đếm chạy thẳng trên bảng công việc, còn phần lấy dòng phải nối sang biểu
    /// ghi để lấy nhan đề nên bị bộ lọc xóa mềm gạt bớt — hai con số không bao giờ gặp nhau.
    /// </summary>
    [Fact]
    public async Task Xoa_bieu_ghi_thi_viec_bien_muc_cua_no_ra_khoi_ca_danh_sach_lan_bo_dem()
    {
        var client = await ClientAsync();
        var dau = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        var bibId = await CreateRecordAsync(client, $"Sách rồi sẽ xóa {dau}");
        var queueId = await EnqueueAsync(client, bibId);

        var truoc = await client.GetFromJsonAsync<ApiResponse<CatalogQueueSummaryDto>>(
            "/api/cataloging/queue/summary", LibraryConnectFactory.JsonOptions);

        var danhSachTruoc = await client.GetFromJsonAsync<ApiResponse<PagedResult<CatalogQueueItemDto>>>(
            "/api/cataloging/queue?status=Pending&pageSize=200", LibraryConnectFactory.JsonOptions);

        danhSachTruoc!.Data!.Items.Should().Contain(item => item.Id == queueId,
            "việc vừa tạo phải nằm trong danh sách");

        var xoa = await client.SendAsync(new HttpRequestMessage(
            HttpMethod.Delete, $"/api/cataloging/bibs/{bibId}")
        {
            Content = JsonContent.Create(new { reason = "Nhập trùng, xóa đi" })
        });

        xoa.IsSuccessStatusCode.Should().BeTrue(await xoa.Content.ReadAsStringAsync());

        var sau = await client.GetFromJsonAsync<ApiResponse<CatalogQueueSummaryDto>>(
            "/api/cataloging/queue/summary", LibraryConnectFactory.JsonOptions);

        sau!.Data!.Pending.Should().Be(truoc!.Data!.Pending - 1,
            "bộ đếm phải giảm đúng một việc khi biểu ghi của nó bị xóa");

        var danhSachSau = await client.GetFromJsonAsync<ApiResponse<PagedResult<CatalogQueueItemDto>>>(
            "/api/cataloging/queue?status=Pending&pageSize=200", LibraryConnectFactory.JsonOptions);

        danhSachSau!.Data!.Items.Should().NotContain(item => item.Id == queueId);

        // Điều quan trọng nhất: con số bộ đếm và số dòng lấy về phải nói cùng một chuyện.
        danhSachSau.Data.Items.Count.Should().Be(
            Math.Min(danhSachSau.Data.TotalCount, 200),
            "một trang phải trả về đủ số dòng mà bộ đếm hứa; thiếu là có việc đếm được mà không hiện được");
    }
}
