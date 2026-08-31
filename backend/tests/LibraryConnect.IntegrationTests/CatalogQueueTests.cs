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
}
