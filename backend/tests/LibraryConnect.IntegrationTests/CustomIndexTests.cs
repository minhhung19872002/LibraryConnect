using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Marc;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Danh mục tự tạo từ trường MARC 21 (II.9, mục DM.9 của bảng đáp ứng).
///
/// The feature is only worth anything if three things hold: the scan finds the values that are
/// actually in the records, the values then work as a search filter, and a merge the librarian makes
/// survives the next scan. All three are checked here against real records in PostgreSQL.
/// </summary>
[Collection(ApiCollection.Name)]
public class CustomIndexTests
{
    private readonly LibraryConnectFactory _factory;

    public CustomIndexTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> ClientAsync() =>
        _factory.CreateAuthenticatedClientAsync(LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    private static async Task CreateRecordAsync(HttpClient client, string title, string place)
    {
        var record = new MarcRecord();
        record.Leader.RecordType = 'a';
        record.Leader.BibliographicLevel = 'm';
        record.SetControlField("008", "240115s2023    vm a     b    000 0 vie d");
        record.AddField("040").AddSubfield('a', "VN-TEST");
        record.AddField("245", '1', '0').AddSubfield('a', title);
        record.AddField("260").AddSubfield('a', place).AddSubfield('c', "2023");

        var response = await client.PostAsJsonAsync("/api/cataloging/bibs", new
        {
            marcJson = MarcJson.Serialize(record),
            status = "Published"
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<Guid> CreateIndexAsync(HttpClient client, string name, string tag, string subfield)
    {
        var response = await client.PostAsJsonAsync("/api/cataloging/custom-indexes", new
        {
            name,
            marcTag = tag,
            marcSubfield = subfield,
            showAsFacet = true
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>(LibraryConnectFactory.JsonOptions);

        return payload!.Data;
    }

    private static async Task<List<CustomIndexValueDto>> HarvestAsync(HttpClient client, Guid indexId)
    {
        var harvest = await client.PostAsync($"/api/cataloging/custom-indexes/{indexId}/harvest", null);
        harvest.StatusCode.Should().Be(HttpStatusCode.OK);

        var values = await client.GetFromJsonAsync<ApiResponse<List<CustomIndexValueDto>>>(
            $"/api/cataloging/custom-indexes/{indexId}/values", LibraryConnectFactory.JsonOptions);

        return values!.Data!;
    }

    [Fact]
    public async Task Harvesting_finds_the_distinct_values_and_counts_the_records_using_each()
    {
        var client = await ClientAsync();
        var marker = Guid.NewGuid().ToString("N")[..6];

        await CreateRecordAsync(client, $"Sách {marker} A", $"Hải Phòng {marker} :");
        await CreateRecordAsync(client, $"Sách {marker} B", $"Hải Phòng {marker}");
        await CreateRecordAsync(client, $"Sách {marker} C", $"Cần Thơ {marker} :");

        var indexId = await CreateIndexAsync(client, $"Nơi xuất bản {marker}", "260", "a");
        var values = await HarvestAsync(client, indexId);

        // The trailing ISBD colon is stripped before grouping, so the two spellings of the same
        // place are one value rather than two.
        var haiPhong = values.Should().Contain(value => value.Name == $"Hải Phòng {marker}")
            .And.Subject.First(value => value.Name == $"Hải Phòng {marker}");

        haiPhong.RecordCount.Should().Be(2);
        values.Should().Contain(value => value.Name == $"Cần Thơ {marker}" && value.RecordCount == 1);
    }

    [Fact]
    public async Task A_value_written_without_diacritics_lands_on_the_same_entry()
    {
        var client = await ClientAsync();
        var marker = Guid.NewGuid().ToString("N")[..6];

        await CreateRecordAsync(client, $"Sách {marker} A", $"Đà Nẵng {marker} :");
        await CreateRecordAsync(client, $"Sách {marker} B", $"Da Nang {marker} :");

        var indexId = await CreateIndexAsync(client, $"Nơi xuất bản {marker}", "260", "a");
        var values = await HarvestAsync(client, indexId);

        var matching = values.Where(value => value.Name.Contains(marker)).ToList();

        matching.Should().ContainSingle("hai cách viết có dấu và không dấu phải về cùng một giá trị");
        matching[0].RecordCount.Should().Be(2);
    }

    [Fact]
    public async Task A_harvested_value_works_as_a_search_filter()
    {
        var client = await ClientAsync();
        var marker = Guid.NewGuid().ToString("N")[..6];

        await CreateRecordAsync(client, $"Sách {marker} A", $"Huế {marker} :");
        await CreateRecordAsync(client, $"Sách {marker} B", $"Huế {marker} :");
        await CreateRecordAsync(client, $"Sách {marker} C", $"Vinh {marker} :");

        var indexId = await CreateIndexAsync(client, $"Nơi xuất bản {marker}", "260", "a");
        var values = await HarvestAsync(client, indexId);
        var hue = values.First(value => value.Name == $"Huế {marker}");

        var filtered = await client.GetFromJsonAsync<ApiResponse<PagedResult<BibListItemDto>>>(
            $"/api/cataloging/bibs?customIndexValueId={hue.Id}", LibraryConnectFactory.JsonOptions);

        filtered!.Data!.TotalCount.Should().Be(2);
        filtered.Data.Items.Should().OnlyContain(item => item.Title.Contains(marker));
        filtered.Data.Items.Should().NotContain(item => item.Title.EndsWith("C"));
    }

    [Fact]
    public async Task Merging_two_spellings_survives_the_next_harvest()
    {
        // Without remembering the merged spelling, the next scan would read the records again, see
        // it, and recreate it — undoing the librarian's work in silence.
        var client = await ClientAsync();
        var marker = Guid.NewGuid().ToString("N")[..6];

        await CreateRecordAsync(client, $"Sách {marker} A", $"Thành phố Hồ Chí Minh {marker} :");
        await CreateRecordAsync(client, $"Sách {marker} B", $"TP. HCM {marker} :");

        var indexId = await CreateIndexAsync(client, $"Nơi xuất bản {marker}", "260", "a");
        var values = await HarvestAsync(client, indexId);

        var mine = values.Where(value => value.Name.Contains(marker)).ToList();
        mine.Should().HaveCount(2);

        var keep = mine.First(value => value.Name.StartsWith("Thành phố"));
        var merge = mine.First(value => value.Id != keep.Id);

        var response = await client.PostAsJsonAsync($"/api/cataloging/custom-indexes/{indexId}/merge", new
        {
            keepId = keep.Id,
            mergeIds = new[] { merge.Id }
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterMerge = await client.GetFromJsonAsync<ApiResponse<List<CustomIndexValueDto>>>(
            $"/api/cataloging/custom-indexes/{indexId}/values", LibraryConnectFactory.JsonOptions);

        afterMerge!.Data!.Where(value => value.Name.Contains(marker)).Should().ContainSingle();

        // Harvest again: the merged spelling must not come back.
        var afterHarvest = await HarvestAsync(client, indexId);
        var survivors = afterHarvest.Where(value => value.Name.Contains(marker)).ToList();

        survivors.Should().ContainSingle("giá trị đã gộp không được lần quét sau tạo lại");
        survivors[0].Name.Should().Be(keep.Name);
        survivors[0].RecordCount.Should().Be(2, "cả hai biểu ghi đều thuộc giá trị đã gộp");

        var filtered = await client.GetFromJsonAsync<ApiResponse<PagedResult<BibListItemDto>>>(
            $"/api/cataloging/bibs?customIndexValueId={keep.Id}", LibraryConnectFactory.JsonOptions);

        filtered!.Data!.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Changing_the_source_field_clears_the_values_harvested_from_the_old_one()
    {
        var client = await ClientAsync();
        var marker = Guid.NewGuid().ToString("N")[..6];

        await CreateRecordAsync(client, $"Sách {marker}", $"Nha Trang {marker} :");

        var indexId = await CreateIndexAsync(client, $"Nơi xuất bản {marker}", "260", "a");
        (await HarvestAsync(client, indexId)).Should().NotBeEmpty();

        var response = await client.PutAsJsonAsync($"/api/cataloging/custom-indexes/{indexId}", new
        {
            name = $"Năm xuất bản {marker}",
            marcTag = "260",
            marcSubfield = "c",
            showAsFacet = true
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var values = await client.GetFromJsonAsync<ApiResponse<List<CustomIndexValueDto>>>(
            $"/api/cataloging/custom-indexes/{indexId}/values", LibraryConnectFactory.JsonOptions);

        values!.Data!.Should().BeEmpty("giá trị cũ rút từ trường khác không còn mô tả đúng danh mục này");
    }

    [Fact]
    public async Task A_control_field_cannot_be_used_as_the_source()
    {
        var client = await ClientAsync();

        var response = await client.PostAsJsonAsync("/api/cataloging/custom-indexes", new
        {
            name = "Sai nguồn",
            marcTag = "008",
            marcSubfield = "a"
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
