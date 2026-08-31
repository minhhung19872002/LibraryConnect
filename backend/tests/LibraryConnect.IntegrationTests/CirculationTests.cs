using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Application.Features.Auth;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Application.Features.Catalogs;
using LibraryConnect.Application.Features.Circulation;
using LibraryConnect.Application.Features.Locations;
using LibraryConnect.Application.Features.Readers;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Phân hệ VII — Lưu thông, chạy thật qua HTTP: chính sách, ghi mượn, ghi trả, gia hạn, đặt giữ,
/// tiền phạt, tủ gửi đồ, ra vào thư viện, bảy báo cáo và nhóm endpoint dành cho bạn đọc.
/// </summary>
[Collection(ApiCollection.Name)]
public class CirculationTests
{
    private readonly LibraryConnectFactory _factory;

    public CirculationTests(LibraryConnectFactory factory) => _factory = factory;

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

    private static async Task<string> ErrorTextAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse>(LibraryConnectFactory.JsonOptions);

        return string.Join(" | ",
            new[] { payload?.Message }
                .Concat(payload?.Errors?.Select(error => error.Message) ?? Array.Empty<string>())
                .Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    // -----------------------------------------------------------------------------------------
    // Dựng dữ liệu: bạn đọc và ấn phẩm sẵn sàng cho mượn
    // -----------------------------------------------------------------------------------------

    private static async Task<Guid> NewReaderAsync(HttpClient client, string fullName, string? typeCode = null)
    {
        var types = await ReadAsync<PagedResult<CatalogItemDto>>(
            await client.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));

        var type = typeCode is null
            ? types.Items.First(item => item.Code == "SV")
            : types.Items.First(item => item.Code == typeCode);

        return await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/readers", new
        {
            fullName,
            studentCode = $"SV{Unique()}",
            readerTypeId = type.Id,
            className = "DH21TH1",
            courseYear = "K21"
        }));
    }

    /// <summary>Tạo một biểu ghi kèm số bản đã kiểm nhận, sẵn sàng lưu thông.</summary>
    private static async Task<List<string>> NewCirculatableItemsAsync(
        HttpClient client, string title, int quantity = 1)
    {
        var warehouses = await ReadAsync<IReadOnlyList<WarehouseDto>>(
            await client.GetAsync("/api/locations/warehouses"));

        // Nhan đề kèm hậu tố duy nhất: biên mục sơ lược gộp theo nhan đề, nên hai kịch bản dùng chung
        // một tên sách sẽ dùng chung luôn cả biểu ghi và số bản của nhau.
        var quick = await ReadAsync<QuickCatalogResultDto>(await client.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title = $"{title} {Unique()}",
                author = "Nguyễn Văn Tác Giả",
                price = 90000m,
                ddc = "005",
                itemQuantity = quantity,
                warehouseId = warehouses[0].Id
            }));

        var page = await ReadAsync<PagedResult<StockItemDto>>(await client.PostAsJsonAsync(
            "/api/stock/items/search",
            new { page = 1, pageSize = 50, filter = new { bibId = quick.BibId } }));

        var ids = page.Items.Select(item => item.Id).ToList();

        // Ấn phẩm mới nhập bị khóa tới khi kiểm nhận — đó là luật của Phân hệ III.
        await ReadAsync<BulkItemResultDto>(await client.PostAsJsonAsync(
            "/api/stock/items/inspect", new { itemIds = ids, condition = "Tốt" }));

        return page.Items.Select(item => item.Barcode).ToList();
    }

    private static async Task<Guid> BibIdOfAsync(HttpClient client, string barcode)
    {
        var page = await ReadAsync<PagedResult<StockItemDto>>(await client.PostAsJsonAsync(
            "/api/stock/items/search",
            new { page = 1, pageSize = 5, filter = new { keyword = barcode } }));

        return page.Items.First(item => item.Barcode == barcode).BibId;
    }

    /// <summary>
    /// Đặt hạn trả của một lượt mượn cách hôm nay đúng số ngày cho trước; số âm nghĩa là quá hạn.
    ///
    /// Ghi thẳng vào cơ sở dữ liệu vì không có cách nào khác để dựng tình huống quá hạn thật mà
    /// không phải chờ hết kỳ hạn mượn.
    /// </summary>
    private async Task ShiftDueDateAsync(Guid loanId, int daysFromToday)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var loan = await db.Loans.FirstAsync(entity => entity.Id == loanId);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var shift = loan.DueDate.DayNumber - today.AddDays(daysFromToday).DayNumber;

        loan.DueDate = today.AddDays(daysFromToday);
        loan.LoanDate = loan.LoanDate.AddDays(-shift);

        await db.SaveChangesAsync(CancellationToken.None);
    }

    // -----------------------------------------------------------------------------------------
    // VII.1 — Chính sách và lịch nghỉ
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task The_system_ships_with_a_policy_for_every_seeded_reader_type()
    {
        var client = await ClientAsync();

        var policies = await ReadAsync<IReadOnlyList<CirculationPolicyDto>>(
            await client.GetAsync("/api/circulation/policies"));

        policies.Should().HaveCountGreaterThanOrEqualTo(6);
        policies.Should().Contain(policy => policy.Name.Contains("Sinh viên"));

        // Bạn đọc khách chỉ đọc tại chỗ — đó là lý do có cột riêng cho việc này.
        policies.Should().Contain(policy => policy.Name.Contains("khách") && !policy.AllowTakeHome);
    }

    [Fact]
    public async Task Two_cells_of_the_matrix_cannot_share_the_same_coordinates_and_priority()
    {
        var client = await ClientAsync();

        var types = await ReadAsync<PagedResult<CatalogItemDto>>(
            await client.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));

        var readerTypeId = types.Items.First(item => item.Code == "NCS").Id;

        await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/circulation/policies", new
        {
            name = $"Thử nghiệm {Unique()}",
            readerTypeId,
            priority = 777,
            maxItems = 4,
            loanDays = 20,
            maxRenewals = 1,
            renewalDays = 10
        }));

        var duplicate = await client.PostAsJsonAsync("/api/circulation/policies", new
        {
            name = $"Trùng ô {Unique()}",
            readerTypeId,
            priority = 777,
            maxItems = 9,
            loanDays = 30,
            maxRenewals = 1,
            renewalDays = 10
        });

        duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorTextAsync(duplicate)).Should().Contain("độ ưu tiên");
    }

    [Fact]
    public async Task The_matrix_preview_answers_which_cell_wins()
    {
        var client = await ClientAsync();

        var types = await ReadAsync<PagedResult<CatalogItemDto>>(
            await client.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));

        var student = types.Items.First(item => item.Code == "SV").Id;

        var policy = await ReadAsync<EffectivePolicy>(
            await client.GetAsync($"/api/circulation/policies/preview?readerTypeId={student}"));

        policy.Name.Should().Contain("Sinh viên");
        policy.LoanDays.Should().BeGreaterThan(0);
        policy.MaxItems.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task A_due_date_landing_on_a_public_holiday_is_pushed_to_the_next_working_day()
    {
        var client = await ClientAsync();

        // 02/09 là Quốc khánh và được nạp sẵn khi cài đặt; mượn ngày 01/09 với hạn 1 ngày.
        var year = DateTime.Today.Year;

        var preview = await ReadAsync<DueDatePreviewDto>(await client.GetAsync(
            $"/api/circulation/holidays/preview-due-date?loanDate={year}-09-01&loanDays=1"));

        preview.RawDueDate.Should().Be(new DateOnly(year, 9, 2));
        preview.Shifted.Should().BeTrue();
        preview.DueDate.Should().BeAfter(new DateOnly(year, 9, 3));
        preview.Explanation.Should().Contain("đóng cửa");
    }

    [Fact]
    public async Task A_holiday_can_be_declared_and_removed()
    {
        var client = await ClientAsync();

        var id = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/circulation/holidays", new
        {
            name = $"Nghỉ thử nghiệm {Unique()}",
            fromDate = "2030-03-10",
            toDate = "2030-03-12",
            isRecurringYearly = false,
            isActive = true
        }));

        var holidays = await ReadAsync<IReadOnlyList<HolidayDto>>(
            await client.GetAsync("/api/circulation/holidays?year=2030"));

        holidays.Should().Contain(holiday => holiday.Id == id);

        (await client.DeleteAsync($"/api/circulation/holidays/{id}")).IsSuccessStatusCode.Should().BeTrue();
    }

    // -----------------------------------------------------------------------------------------
    // VII.2 — Ghi mượn
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Scanning_a_card_shows_the_reader_with_the_quota_from_the_policy()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Nguyễn Văn Quầy");

        var reader = await ReadAsync<ReaderDetailDto>(await client.GetAsync($"/api/readers/{readerId}"));

        var desk = await ReadAsync<DeskReaderDto>(
            await client.GetAsync($"/api/circulation/desk/reader?cardNumber={reader.CardNumber}"));

        desk.Id.Should().Be(readerId);
        desk.CanBorrow.Should().BeTrue();
        desk.MaxItems.Should().Be(3);
        desk.RemainingQuota.Should().Be(3);
        desk.Warnings.Should().NotContain(warning => warning.Blocking);
    }

    [Fact]
    public async Task Scanning_a_barcode_returns_the_due_date_computed_by_the_server()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Trần Thị Quét");
        var barcodes = await NewCirculatableItemsAsync(client, "Giáo trình mạng máy tính");

        var scan = await ReadAsync<ScanForLoanDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/scan", new { readerId, barcode = barcodes[0] }));

        scan.Allowed.Should().BeTrue();
        scan.Title.Should().StartWith("Giáo trình mạng máy tính");
        scan.DueDate.Should().NotBeNull();
        scan.DueDate!.Value.Should().BeAfter(DateOnly.FromDateTime(DateTime.Today));
        scan.PolicyName.Should().Contain("Sinh viên");
    }

    [Fact]
    public async Task Scanning_the_same_barcode_twice_in_one_session_is_refused()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Lê Văn Trùng");
        var barcodes = await NewCirculatableItemsAsync(client, "Cơ sở dữ liệu nâng cao");

        var scan = await ReadAsync<ScanForLoanDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/scan",
            new { readerId, barcode = barcodes[0], pending = new[] { barcodes[0] } }));

        scan.Allowed.Should().BeFalse();
        scan.Warnings.Should().Contain(warning => warning.Code == CirculationWarnings.AlreadyInList);
    }

    [Fact]
    public async Task Checking_out_moves_the_copy_to_on_loan_and_fills_the_slip()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Phạm Thị Mượn");
        var barcodes = await NewCirculatableItemsAsync(client, "Nhập môn trí tuệ nhân tạo", 2);

        var result = await ReadAsync<CheckoutResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId, barcodes }));

        result.Loans.Should().HaveCount(2);
        result.Failures.Should().BeEmpty();
        result.SlipCode.Should().NotBeNullOrWhiteSpace();
        result.Loans.Should().OnlyContain(loan => loan.Status == LoanStatus.Active);

        var stock = await ReadAsync<PagedResult<StockItemDto>>(await client.PostAsJsonAsync(
            "/api/stock/items/search",
            new { page = 1, pageSize = 10, filter = new { keyword = barcodes[0] } }));

        stock.Items.First(item => item.Barcode == barcodes[0]).Status.Should().Be(ItemStatus.OnLoan);

        var desk = await ReadAsync<DeskReaderDto>(
            await client.GetAsync($"/api/circulation/desk/reader/{readerId}"));

        desk.CurrentLoanCount.Should().Be(2);
        desk.RemainingQuota.Should().Be(1);
    }

    [Fact]
    public async Task Ghi_muon_lam_tang_so_luot_muon_cua_bieu_ghi()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Lê Thị Đếm Lượt");
        var barcodes = await NewCirculatableItemsAsync(client, $"Sách đếm lượt mượn {Unique()}", 2);
        var bibId = await BibIdOfAsync(client, barcodes[0]);

        var before = await ReadAsync<BibDetailDto>(await client.GetAsync($"/api/cataloging/bibs/{bibId}"));

        await ReadAsync<CheckoutResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId, barcodes = new[] { barcodes[0] } }));

        var after = await ReadAsync<BibDetailDto>(await client.GetAsync($"/api/cataloging/bibs/{bibId}"));

        // Trang tra cứu xếp "sách được mượn nhiều" và tính độ liên quan theo con số này. Chỉ đếm ở
        // từng bản in thì khối ấy trống mãi dù thư viện cho mượn hàng nghìn lượt.
        after.LoanCount.Should().Be(before.LoanCount + 1);
    }

    [Fact]
    public async Task A_copy_already_on_loan_cannot_be_lent_to_someone_else()
    {
        var client = await ClientAsync();
        var first = await NewReaderAsync(client, "Bạn đọc thứ nhất");
        var second = await NewReaderAsync(client, "Bạn đọc thứ hai");
        var barcodes = await NewCirculatableItemsAsync(client, "Kỹ thuật lập trình");

        await ReadAsync<CheckoutResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId = first, barcodes }));

        var scan = await ReadAsync<ScanForLoanDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/scan", new { readerId = second, barcode = barcodes[0] }));

        scan.Allowed.Should().BeFalse();
        scan.Warnings.Should().Contain(warning => warning.Code == CirculationWarnings.ItemOnLoan);

        var refused = await client.PostAsJsonAsync("/api/circulation/desk/checkout",
            new { readerId = second, barcodes });

        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task The_policy_quota_stops_the_fourth_book_for_a_student()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Sinh viên đủ hạn mức");
        var barcodes = await NewCirculatableItemsAsync(client, "Bộ sách bốn tập", 4);

        var result = await ReadAsync<CheckoutResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId, barcodes }));

        // Chính sách sinh viên cho mượn 3 quyển; quyển thứ tư bị giữ lại kèm lý do rõ ràng.
        result.Loans.Should().HaveCount(3);
        result.Failures.Should().ContainSingle();
        result.Failures[0].Message.Should().Contain("mượn đủ 3 tài liệu");
    }

    [Fact]
    public async Task A_locked_copy_is_refused_at_the_desk()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Bạn đọc gặp sách khóa");
        var barcodes = await NewCirculatableItemsAsync(client, "Sách đang số hóa");

        var stock = await ReadAsync<PagedResult<StockItemDto>>(await client.PostAsJsonAsync(
            "/api/stock/items/search",
            new { page = 1, pageSize = 5, filter = new { keyword = barcodes[0] } }));

        await ReadAsync<BulkItemResultDto>(await client.PostAsJsonAsync("/api/stock/items/lock", new
        {
            itemIds = new[] { stock.Items[0].Id },
            isLocked = true,
            reason = "Đang số hóa"
        }));

        var scan = await ReadAsync<ScanForLoanDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/scan", new { readerId, barcode = barcodes[0] }));

        scan.Allowed.Should().BeFalse();
        scan.Warnings.Should().Contain(warning =>
            warning.Code == CirculationWarnings.ItemLocked && warning.Message.Contains("số hóa"));
    }

    [Fact]
    public async Task A_locked_reader_card_blocks_the_whole_checkout()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Bạn đọc bị khóa thẻ");
        var barcodes = await NewCirculatableItemsAsync(client, "Sách cho người bị khóa thẻ");

        await ReadAsync<object>(await client.PostAsJsonAsync("/api/readers/lock", new
        {
            selection = new { readerIds = new[] { readerId } },
            locked = true,
            reason = "Chưa bồi thường tài liệu"
        }));

        var desk = await ReadAsync<DeskReaderDto>(
            await client.GetAsync($"/api/circulation/desk/reader/{readerId}"));

        desk.CanBorrow.Should().BeFalse();
        desk.Warnings.Should().Contain(warning =>
            warning.Code == CirculationWarnings.ReaderLocked && warning.Blocking);

        var refused = await client.PostAsJsonAsync("/api/circulation/desk/checkout",
            new { readerId, barcodes });

        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorTextAsync(refused)).Should().Contain("khóa");
    }

    // -----------------------------------------------------------------------------------------
    // VII.2 — Ghi trả và tiền phạt
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Returning_on_time_costs_nothing_and_puts_the_copy_back_in_stock()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Bạn đọc trả đúng hạn");
        var barcodes = await NewCirculatableItemsAsync(client, "Sách trả đúng hạn");

        await ReadAsync<CheckoutResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId, barcodes }));

        var result = await ReadAsync<ReturnResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/return", new { barcodes }));

        result.Items.Should().ContainSingle();
        result.TotalFine.Should().Be(0);
        result.Items[0].Fine.Should().Be(0);
        result.Items[0].HoldWaiting.Should().BeFalse();

        var stock = await ReadAsync<PagedResult<StockItemDto>>(await client.PostAsJsonAsync(
            "/api/stock/items/search",
            new { page = 1, pageSize = 5, filter = new { keyword = barcodes[0] } }));

        stock.Items[0].Status.Should().Be(ItemStatus.InStock);
    }

    [Fact]
    public async Task Returning_late_creates_a_fine_the_reader_owes()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Bạn đọc trả muộn");
        var barcodes = await NewCirculatableItemsAsync(client, "Sách trả muộn");

        var checkout = await ReadAsync<CheckoutResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId, barcodes }));

        // Hạn trả rơi vào 10 ngày trước để tạo tình huống quá hạn thật.
        await ShiftDueDateAsync(checkout.Loans[0].Id, -10);

        var result = await ReadAsync<ReturnResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/return", new { barcodes }));

        result.Items[0].OverdueDays.Should().BeGreaterThan(0);
        result.TotalFine.Should().BeGreaterThan(0);
        result.Items[0].FineCode.Should().NotBeNullOrWhiteSpace();

        var fines = await ReadAsync<ReaderFineSummaryDto>(
            await client.GetAsync($"/api/circulation/fines/reader/{readerId}"));

        fines.TotalOutstanding.Should().Be(result.TotalFine);
    }

    [Fact]
    public async Task Grace_days_mean_one_day_late_is_free_under_the_seeded_student_policy()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Bạn đọc trễ một ngày");
        var barcodes = await NewCirculatableItemsAsync(client, "Sách trễ một ngày");

        var checkout = await ReadAsync<CheckoutResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId, barcodes }));

        await ShiftDueDateAsync(checkout.Loans[0].Id, -1);

        var result = await ReadAsync<ReturnResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/return", new { barcodes }));

        // Chính sách mặc định có một ngày ân hạn.
        result.TotalFine.Should().Be(0);
    }

    [Fact]
    public async Task Returning_a_copy_nobody_borrowed_is_reported_not_swallowed()
    {
        var client = await ClientAsync();
        var barcodes = await NewCirculatableItemsAsync(client, "Sách chưa ai mượn");

        var refused = await client.PostAsJsonAsync("/api/circulation/desk/return", new { barcodes });

        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorTextAsync(refused)).Should().Contain("không có lượt mượn");
    }

    [Fact]
    public async Task A_fine_can_be_paid_in_two_instalments()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Bạn đọc trả góp tiền phạt");

        var fine = await ReadAsync<FineRowDto>(await client.PostAsJsonAsync("/api/circulation/fines", new
        {
            readerId,
            type = FineType.Other,
            amount = 30000m,
            note = "Làm mất thẻ"
        }));

        var partial = await ReadAsync<FineRowDto>(await client.PostAsJsonAsync(
            $"/api/circulation/fines/{fine.Id}/pay", new { amount = 10000m }));

        partial.PaidAmount.Should().Be(10000);
        partial.Outstanding.Should().Be(20000);

        var full = await ReadAsync<FineRowDto>(await client.PostAsJsonAsync(
            $"/api/circulation/fines/{fine.Id}/pay", new { }));

        full.Outstanding.Should().Be(0);

        var again = await client.PostAsJsonAsync($"/api/circulation/fines/{fine.Id}/pay", new { });
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Paying_more_than_the_debt_is_refused()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Bạn đọc trả thừa");

        var fine = await ReadAsync<FineRowDto>(await client.PostAsJsonAsync("/api/circulation/fines", new
        {
            readerId,
            type = FineType.Other,
            amount = 20000m
        }));

        var refused = await client.PostAsJsonAsync(
            $"/api/circulation/fines/{fine.Id}/pay", new { amount = 50000m });

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorTextAsync(refused)).Should().Contain("vượt quá");
    }

    [Fact]
    public async Task Waiving_a_fine_demands_a_reason_and_clears_the_debt()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Bạn đọc được miễn phạt");

        var fine = await ReadAsync<FineRowDto>(await client.PostAsJsonAsync("/api/circulation/fines", new
        {
            readerId,
            type = FineType.Other,
            amount = 15000m
        }));

        var noReason = await client.PostAsJsonAsync(
            $"/api/circulation/fines/{fine.Id}/waive", new { });

        noReason.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorTextAsync(noReason)).Should().Contain("lý do");

        var waived = await ReadAsync<FineRowDto>(await client.PostAsJsonAsync(
            $"/api/circulation/fines/{fine.Id}/waive", new { reason = "Hoàn cảnh khó khăn" }));

        waived.Waived.Should().BeTrue();
        waived.Outstanding.Should().Be(0);

        var summary = await ReadAsync<ReaderFineSummaryDto>(
            await client.GetAsync($"/api/circulation/fines/reader/{readerId}"));

        summary.TotalOutstanding.Should().Be(0);
    }

    [Fact]
    public async Task A_lost_book_closes_the_loan_and_bills_the_reader()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Bạn đọc làm mất sách");
        var barcodes = await NewCirculatableItemsAsync(client, "Sách bị mất");

        var checkout = await ReadAsync<CheckoutResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId, barcodes }));

        var fine = await ReadAsync<FineRowDto>(await client.PostAsJsonAsync(
            $"/api/circulation/loans/{checkout.Loans[0].Id}/close-as-lost",
            new { damaged = false, note = "Bạn đọc báo mất" }));

        // Hệ số bồi thường mặc định là 2 lần giá ghi trên ĐKCB (90.000 đ).
        fine.Amount.Should().Be(180000);
        fine.Type.Should().Be(FineType.Lost);

        var stock = await ReadAsync<PagedResult<StockItemDto>>(await client.PostAsJsonAsync(
            "/api/stock/items/search",
            new { page = 1, pageSize = 5, filter = new { keyword = barcodes[0] } }));

        stock.Items[0].Status.Should().Be(ItemStatus.Lost);
        stock.Items[0].IsLocked.Should().BeTrue();
    }

    // -----------------------------------------------------------------------------------------
    // VII.2 — Gia hạn
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Renewing_extends_the_due_date_and_counts_the_renewal()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Bạn đọc gia hạn");
        var barcodes = await NewCirculatableItemsAsync(client, "Sách được gia hạn");

        var checkout = await ReadAsync<CheckoutResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId, barcodes }));

        var loan = checkout.Loans[0];

        // Hạn còn ba ngày: gia hạn mới thực sự kéo dài thêm được.
        await ShiftDueDateAsync(loan.Id, 3);

        var renewed = await ReadAsync<LoanRowDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/renew-by-barcode", new { barcode = barcodes[0] }));

        renewed.RenewedCount.Should().Be(1);
        renewed.DueDate.Should().BeAfter(DateOnly.FromDateTime(DateTime.Today));

        var detail = await ReadAsync<LoanDetailDto>(
            await client.GetAsync($"/api/circulation/loans/{loan.Id}"));

        detail.Renewals.Should().ContainSingle();
        detail.Renewals[0].NewDueDate.Should().Be(renewed.DueDate);
    }

    [Fact]
    public async Task Renewing_is_refused_once_the_book_is_overdue()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Bạn đọc quá hạn xin gia hạn");
        var barcodes = await NewCirculatableItemsAsync(client, "Sách quá hạn xin gia hạn");

        var checkout = await ReadAsync<CheckoutResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId, barcodes }));

        await ShiftDueDateAsync(checkout.Loans[0].Id, -30);

        var refused = await client.PostAsJsonAsync(
            $"/api/circulation/loans/{checkout.Loans[0].Id}/renew", new { });

        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorTextAsync(refused)).Should().Contain("quá hạn");
    }

    [Fact]
    public async Task Renewing_is_refused_while_another_reader_is_waiting()
    {
        var client = await ClientAsync();
        var borrower = await NewReaderAsync(client, "Người đang giữ sách");
        var waiter = await NewReaderAsync(client, "Người đang đợi");
        var barcodes = await NewCirculatableItemsAsync(client, "Sách có người đợi");

        var checkout = await ReadAsync<CheckoutResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId = borrower, barcodes }));

        var bibId = await BibIdOfAsync(client, barcodes[0]);

        await ReadAsync<HoldRowDto>(await client.PostAsJsonAsync("/api/circulation/holds", new
        {
            readerId = waiter,
            bibId
        }));

        var refused = await client.PostAsJsonAsync(
            $"/api/circulation/loans/{checkout.Loans[0].Id}/renew", new { });

        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorTextAsync(refused)).Should().Contain("đặt giữ");
    }

    // -----------------------------------------------------------------------------------------
    // VII.2 — Đặt giữ chỗ
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task A_returned_copy_is_kept_at_the_desk_for_the_first_reader_in_the_queue()
    {
        var client = await ClientAsync();
        var borrower = await NewReaderAsync(client, "Người mượn trước");
        var waiter = await NewReaderAsync(client, "Người đặt giữ");
        var barcodes = await NewCirculatableItemsAsync(client, "Sách được đặt giữ");

        await ReadAsync<CheckoutResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId = borrower, barcodes }));

        var bibId = await BibIdOfAsync(client, barcodes[0]);

        var hold = await ReadAsync<HoldRowDto>(await client.PostAsJsonAsync("/api/circulation/holds", new
        {
            readerId = waiter,
            bibId
        }));

        hold.Status.Should().Be(HoldStatus.Waiting);
        hold.QueuePosition.Should().Be(1);

        var returned = await ReadAsync<ReturnResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/return", new { barcodes }));

        returned.Items[0].HoldWaiting.Should().BeTrue();
        returned.Items[0].HoldForReaderName.Should().Be("Người đặt giữ");

        var stock = await ReadAsync<PagedResult<StockItemDto>>(await client.PostAsJsonAsync(
            "/api/stock/items/search",
            new { page = 1, pageSize = 5, filter = new { keyword = barcodes[0] } }));

        stock.Items[0].Status.Should().Be(ItemStatus.OnHoldShelf);

        // Người khác không lấy được bản đang giữ cho người đã đặt.
        var third = await NewReaderAsync(client, "Người đến sau");

        var scan = await ReadAsync<ScanForLoanDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/scan", new { readerId = third, barcode = barcodes[0] }));

        scan.Allowed.Should().BeFalse();
        scan.Warnings.Should().Contain(warning => warning.Code == CirculationWarnings.ItemHeldForOther);

        // Còn người đã đặt thì mượn được, và phiếu đặt giữ khép lại.
        var checkout = await ReadAsync<CheckoutResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId = waiter, barcodes }));

        checkout.Loans.Should().ContainSingle();

        var holds = await ReadAsync<PagedResult<HoldRowDto>>(
            await client.GetAsync($"/api/circulation/holds?readerId={waiter}"));

        holds.Items.Should().ContainSingle().Which.Status.Should().Be(HoldStatus.Fulfilled);
    }

    [Fact]
    public async Task The_hold_queue_is_numbered_in_arrival_order()
    {
        var client = await ClientAsync();
        var borrower = await NewReaderAsync(client, "Người giữ sách hiếm");
        var first = await NewReaderAsync(client, "Người xếp hàng thứ nhất");
        var second = await NewReaderAsync(client, "Người xếp hàng thứ hai");
        var barcodes = await NewCirculatableItemsAsync(client, "Sách hiếm nhiều người đợi");

        await ReadAsync<CheckoutResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId = borrower, barcodes }));

        var bibId = await BibIdOfAsync(client, barcodes[0]);

        var one = await ReadAsync<HoldRowDto>(await client.PostAsJsonAsync(
            "/api/circulation/holds", new { readerId = first, bibId }));

        var two = await ReadAsync<HoldRowDto>(await client.PostAsJsonAsync(
            "/api/circulation/holds", new { readerId = second, bibId }));

        one.QueuePosition.Should().Be(1);
        two.QueuePosition.Should().Be(2);

        var queue = await ReadAsync<IReadOnlyList<HoldRowDto>>(
            await client.GetAsync($"/api/circulation/holds/queue/{bibId}"));

        queue.Should().HaveCount(2);
        queue[0].ReaderName.Should().Be("Người xếp hàng thứ nhất");

        // Người đầu hàng bỏ cuộc thì người thứ hai lên số 1.
        (await client.DeleteAsync($"/api/circulation/holds/{one.Id}?reason=Không cần nữa"))
            .IsSuccessStatusCode.Should().BeTrue();

        queue = await ReadAsync<IReadOnlyList<HoldRowDto>>(
            await client.GetAsync($"/api/circulation/holds/queue/{bibId}"));

        queue.Should().ContainSingle();
        queue[0].QueuePosition.Should().Be(1);
    }

    [Fact]
    public async Task A_reader_cannot_hold_the_same_title_twice()
    {
        var client = await ClientAsync();
        var borrower = await NewReaderAsync(client, "Người giữ sách");
        var waiter = await NewReaderAsync(client, "Người đặt hai lần");
        var barcodes = await NewCirculatableItemsAsync(client, "Sách đặt hai lần");

        await ReadAsync<CheckoutResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId = borrower, barcodes }));

        var bibId = await BibIdOfAsync(client, barcodes[0]);

        await ReadAsync<HoldRowDto>(await client.PostAsJsonAsync(
            "/api/circulation/holds", new { readerId = waiter, bibId }));

        var again = await client.PostAsJsonAsync("/api/circulation/holds", new { readerId = waiter, bibId });

        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorTextAsync(again)).Should().Contain("đã đặt giữ");
    }

    // -----------------------------------------------------------------------------------------
    // VII.2 — Ra vào thư viện
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task One_scanner_at_the_gate_handles_both_entering_and_leaving()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Bạn đọc vào thư viện");

        var reader = await ReadAsync<ReaderDetailDto>(await client.GetAsync($"/api/readers/{readerId}"));

        var entering = await ReadAsync<GateScanResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/gate/scan", new { cardNumber = reader.CardNumber, gate = "Cổng chính" }));

        entering.CheckedIn.Should().BeTrue();
        entering.Message.Should().Contain("vào thư viện");

        var leaving = await ReadAsync<GateScanResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/gate/scan", new { cardNumber = reader.CardNumber, gate = "Cổng chính" }));

        leaving.CheckedIn.Should().BeFalse();
        leaving.Visit.CheckoutAt.Should().NotBeNull();

        var visits = await ReadAsync<PagedResult<VisitRowDto>>(
            await client.GetAsync($"/api/circulation/visits?readerId={readerId}"));

        visits.TotalCount.Should().Be(1);
        visits.Items[0].Minutes.Should().NotBeNull();
    }

    // -----------------------------------------------------------------------------------------
    // VII.3 — Tủ gửi đồ
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task The_locker_map_ships_with_twenty_lockers_and_tracks_who_holds_them()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Bạn đọc gửi đồ");
        var reader = await ReadAsync<ReaderDetailDto>(await client.GetAsync($"/api/readers/{readerId}"));

        var map = await ReadAsync<LockerMapDto>(await client.GetAsync("/api/circulation/lockers"));

        map.Lockers.Should().HaveCountGreaterThanOrEqualTo(20);
        map.Areas.Should().Contain("Dãy A");

        var free = map.Lockers.First(locker => locker.Status == LockerStatus.Free);

        var assigned = await ReadAsync<LockerRowDto>(await client.PostAsJsonAsync(
            $"/api/circulation/lockers/{free.Id}/assign",
            new { cardNumber = reader.CardNumber, keyNumber = "K01" }));

        assigned.Status.Should().Be(LockerStatus.InUse);
        assigned.ReaderName.Should().Be("Bạn đọc gửi đồ");

        // Một bạn đọc chỉ giữ một tủ.
        var another = map.Lockers.Last(locker => locker.Status == LockerStatus.Free);

        var refused = await client.PostAsJsonAsync(
            $"/api/circulation/lockers/{another.Id}/assign", new { cardNumber = reader.CardNumber });

        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorTextAsync(refused)).Should().Contain("đang giữ tủ");

        var released = await ReadAsync<LockerUsageRowDto>(await client.PostAsJsonAsync(
            "/api/circulation/lockers/release", new { cardNumber = reader.CardNumber }));

        released.CheckoutAt.Should().NotBeNull();

        var after = await ReadAsync<LockerMapDto>(await client.GetAsync("/api/circulation/lockers"));
        after.Lockers.First(locker => locker.Id == free.Id).Status.Should().Be(LockerStatus.Free);
    }

    // -----------------------------------------------------------------------------------------
    // VII.5 — Bảy báo cáo
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task The_visit_report_has_a_bucket_for_every_hour_of_the_day()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Bạn đọc cho báo cáo ra vào");
        var reader = await ReadAsync<ReaderDetailDto>(await client.GetAsync($"/api/readers/{readerId}"));

        await ReadAsync<GateScanResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/gate/scan", new { cardNumber = reader.CardNumber }));

        var report = await ReadAsync<VisitReportDto>(
            await client.GetAsync($"/api/circulation/reports/visits?readerId={readerId}"));

        report.TotalVisits.Should().Be(1);
        report.ByHour.Should().HaveCount(24);
        report.ByReaderType.Should().ContainSingle();
    }

    [Fact]
    public async Task The_overdue_report_shows_the_days_late_and_the_fine_to_come()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Bạn đọc trong báo cáo quá hạn");
        var barcodes = await NewCirculatableItemsAsync(client, "Sách trong báo cáo quá hạn");

        var checkout = await ReadAsync<CheckoutResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId, barcodes }));

        await ShiftDueDateAsync(checkout.Loans[0].Id, -20);

        var report = await ReadAsync<OverdueReportDto>(
            await client.GetAsync($"/api/circulation/reports/overdue?readerId={readerId}"));

        report.TotalOverdue.Should().Be(1);
        report.Rows[0].OverdueDays.Should().BeGreaterThan(0);
        report.EstimatedFine.Should().BeGreaterThan(0);
        report.ByRange.Should().HaveCount(4);

        // Nhắc hạn gửi tới bạn đọc, gộp một thư cho mỗi người.
        var sent = await ReadAsync<int>(await client.PostAsJsonAsync(
            "/api/circulation/reports/overdue/remind",
            new { filter = new { readerId }, loanIds = Array.Empty<Guid>() }));

        sent.Should().Be(1);
    }

    [Fact]
    public async Task The_current_loans_report_lists_what_is_out_right_now()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Bạn đọc đang giữ sách");
        var barcodes = await NewCirculatableItemsAsync(client, "Sách đang được giữ", 2);

        await ReadAsync<CheckoutResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId, barcodes }));

        var rows = await ReadAsync<IReadOnlyList<LoanRowDto>>(
            await client.GetAsync($"/api/circulation/reports/current-loans?readerId={readerId}"));

        rows.Should().HaveCount(2);
        rows.Should().OnlyContain(row => row.ReturnDate == null);
    }

    [Fact]
    public async Task The_top_reader_and_top_item_reports_count_the_same_loans()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Bạn đọc mượn nhiều");
        var barcodes = await NewCirculatableItemsAsync(client, "Sách được mượn nhiều", 3);

        await ReadAsync<CheckoutResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId, barcodes }));

        var readers = await ReadAsync<IReadOnlyList<TopReaderRowDto>>(
            await client.GetAsync($"/api/circulation/reports/top-readers?readerId={readerId}&top=10"));

        readers.Should().ContainSingle();
        readers[0].LoanCount.Should().Be(3);

        var items = await ReadAsync<IReadOnlyList<TopItemRowDto>>(
            await client.GetAsync($"/api/circulation/reports/top-items?readerId={readerId}&top=10"));

        items.Should().ContainSingle();
        items[0].LoanCount.Should().Be(3);
        items[0].CopyCount.Should().Be(3);
    }

    [Fact]
    public async Task The_history_report_totals_match_its_own_rows()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Bạn đọc có lịch sử");
        var barcodes = await NewCirculatableItemsAsync(client, "Sách có lịch sử mượn", 2);

        await ReadAsync<CheckoutResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId, barcodes }));

        await ReadAsync<ReturnResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/return", new { barcodes = new[] { barcodes[0] } }));

        var report = await ReadAsync<LoanHistoryReportDto>(
            await client.GetAsync($"/api/circulation/reports/history?readerId={readerId}"));

        report.TotalLoans.Should().Be(2);
        report.Returned.Should().Be(1);
        report.StillOut.Should().Be(1);
        report.Rows.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(5, false)]
    [InlineData(6, false)]
    public async Task Every_circulation_report_exports_to_excel_and_pdf(int kind, bool asPdf)
    {
        var client = await ClientAsync();

        var response = await client.PostAsJsonAsync("/api/circulation/reports/export", new
        {
            kind,
            asPdf,
            filter = new { top = 20 }
        });

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());

        var content = await response.Content.ReadAsByteArrayAsync();
        content.Length.Should().BeGreaterThan(500);

        if (asPdf)
        {
            System.Text.Encoding.ASCII.GetString(content, 0, 5).Should().Be("%PDF-");
        }
    }

    // -----------------------------------------------------------------------------------------
    // VII.4 — Biểu mẫu in ở quầy
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task The_loan_slip_and_the_return_slip_print_from_the_shared_form_designer()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Bạn đọc in phiếu");
        var barcodes = await NewCirculatableItemsAsync(client, "Sách in phiếu mượn", 2);

        var checkout = await ReadAsync<CheckoutResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId, barcodes }));

        var slip = await client.GetAsync(
            $"/api/acquisition/forms/print/LOAN_SLIP/{checkout.SlipCode}");

        slip.IsSuccessStatusCode.Should().BeTrue(await slip.Content.ReadAsStringAsync());
        (await slip.Content.ReadAsByteArrayAsync()).Length.Should().BeGreaterThan(1000);

        var returned = await ReadAsync<ReturnResultDto>(await client.PostAsJsonAsync(
            "/api/circulation/desk/return", new { barcodes }));

        var returnSlip = await client.GetAsync(
            $"/api/acquisition/forms/print/RETURN_SLIP/{returned.SlipCode}");

        returnSlip.IsSuccessStatusCode.Should().BeTrue(await returnSlip.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task The_clearance_certificate_states_whether_anything_is_still_outstanding()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Sinh viên xin giấy xác nhận");
        var reader = await ReadAsync<ReaderDetailDto>(await client.GetAsync($"/api/readers/{readerId}"));

        var certificate = await client.GetAsync(
            $"/api/acquisition/forms/print/CLEARANCE/{reader.CardNumber}");

        certificate.IsSuccessStatusCode.Should().BeTrue(await certificate.Content.ReadAsStringAsync());
        (await certificate.Content.ReadAsByteArrayAsync()).Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task A_fine_receipt_prints_with_the_amount_spelled_out()
    {
        var client = await ClientAsync();
        var readerId = await NewReaderAsync(client, "Bạn đọc nhận biên lai");

        var fine = await ReadAsync<FineRowDto>(await client.PostAsJsonAsync("/api/circulation/fines", new
        {
            readerId,
            type = FineType.Other,
            amount = 25000m,
            note = "Vi phạm nội quy phòng đọc"
        }));

        await ReadAsync<FineRowDto>(await client.PostAsJsonAsync(
            $"/api/circulation/fines/{fine.Id}/pay", new { }));

        var receipt = await client.GetAsync($"/api/acquisition/forms/print/FINE_RECEIPT/{fine.Code}");

        receipt.IsSuccessStatusCode.Should().BeTrue(await receipt.Content.ReadAsStringAsync());
        (await receipt.Content.ReadAsByteArrayAsync()).Length.Should().BeGreaterThan(1000);
    }

    // -----------------------------------------------------------------------------------------
    // Nhóm /api/reader/* (mục XI.4)
    // -----------------------------------------------------------------------------------------

    /// <summary>Tạo bạn đọc có mật khẩu và trả về client đã đăng nhập bằng tài khoản bạn đọc.</summary>
    private async Task<(HttpClient Client, Guid ReaderId, string CardNumber)> ReaderClientAsync(
        HttpClient staff, string fullName)
    {
        var readerId = await NewReaderAsync(staff, fullName);
        var reader = await ReadAsync<ReaderDetailDto>(await staff.GetAsync($"/api/readers/{readerId}"));

        var password = await ReadAsync<string>(await staff.PostAsJsonAsync(
            $"/api/readers/{readerId}/reset-password", new { }));

        var client = await _factory.CreateReaderClientAsync(reader.CardNumber, password);

        return (client, readerId, reader.CardNumber);
    }

    [Fact]
    public async Task A_reader_signs_in_with_the_card_number_and_sees_only_their_own_loans()
    {
        var staff = await ClientAsync();
        var (client, readerId, cardNumber) = await ReaderClientAsync(staff, "Bạn đọc đăng nhập");
        var barcodes = await NewCirculatableItemsAsync(staff, "Sách của bạn đọc đăng nhập");

        await ReadAsync<CheckoutResultDto>(await staff.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId, barcodes }));

        var card = await ReadAsync<ReaderCardInfoDto>(await client.GetAsync("/api/reader/card"));

        card.CardNumber.Should().Be(cardNumber);
        card.BarcodeValue.Should().Be(cardNumber);
        card.CurrentLoanCount.Should().Be(1);

        var loans = await ReadAsync<PagedResult<LoanRowDto>>(
            await client.GetAsync("/api/reader/loans/current"));

        loans.Items.Should().ContainSingle();
        loans.Items[0].ReaderId.Should().Be(readerId);
    }

    [Fact]
    public async Task A_wrong_password_is_refused_without_saying_which_part_was_wrong()
    {
        var staff = await ClientAsync();
        var readerId = await NewReaderAsync(staff, "Bạn đọc gõ sai mật khẩu");
        var reader = await ReadAsync<ReaderDetailDto>(await staff.GetAsync($"/api/readers/{readerId}"));

        await ReadAsync<string>(await staff.PostAsJsonAsync(
            $"/api/readers/{readerId}/reset-password", new { }));

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/reader/auth/login", new
        {
            cardNumber = reader.CardNumber,
            password = "SaiMatKhau@123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ErrorTextAsync(response)).Should().Contain("Số thẻ hoặc mật khẩu không đúng");
    }

    [Fact]
    public async Task A_reader_renews_and_holds_through_the_reader_endpoints()
    {
        var staff = await ClientAsync();
        var (client, readerId, _) = await ReaderClientAsync(staff, "Bạn đọc tự gia hạn");
        var barcodes = await NewCirculatableItemsAsync(staff, "Sách bạn đọc tự gia hạn");

        var checkout = await ReadAsync<CheckoutResultDto>(await staff.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId, barcodes }));

        await ShiftDueDateAsync(checkout.Loans[0].Id, 3);

        var renewed = await ReadAsync<LoanRowDto>(await client.PostAsJsonAsync(
            $"/api/reader/loans/{checkout.Loans[0].Id}/renew", new { }));

        renewed.RenewedCount.Should().Be(1);

        // Cột kênh của lượt mượn ghi cách bạn đọc mượn lúc đầu (ở quầy); kênh gia hạn nằm trong sổ
        // gia hạn, vì một lượt mượn có thể được gia hạn nhiều lần qua nhiều kênh khác nhau.
        var detail = await ReadAsync<LoanDetailDto>(
            await staff.GetAsync($"/api/circulation/loans/{checkout.Loans[0].Id}"));

        detail.Renewals.Should().ContainSingle();
        detail.Renewals[0].Channel.Should().Be(LoanChannel.Opac);

        // Đặt giữ một tài liệu khác đang có người mượn.
        var other = await NewReaderAsync(staff, "Người giữ sách khác");
        var otherBarcodes = await NewCirculatableItemsAsync(staff, "Sách bạn đọc đặt giữ từ xa");

        await ReadAsync<CheckoutResultDto>(await staff.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId = other, barcodes = otherBarcodes }));

        var bibId = await BibIdOfAsync(staff, otherBarcodes[0]);

        var hold = await ReadAsync<HoldRowDto>(await client.PostAsJsonAsync(
            "/api/reader/holds", new { bibId }));

        hold.ReaderId.Should().Be(readerId);
        hold.Channel.Should().Be(LoanChannel.Opac);

        var mine = await ReadAsync<PagedResult<HoldRowDto>>(await client.GetAsync("/api/reader/holds"));
        mine.Items.Should().ContainSingle();

        (await client.DeleteAsync($"/api/reader/holds/{hold.Id}")).IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task A_reader_cannot_renew_someone_elses_loan()
    {
        var staff = await ClientAsync();
        var (client, _, _) = await ReaderClientAsync(staff, "Bạn đọc tò mò");

        var victim = await NewReaderAsync(staff, "Bạn đọc bị dòm ngó");
        var barcodes = await NewCirculatableItemsAsync(staff, "Sách của người khác");

        var checkout = await ReadAsync<CheckoutResultDto>(await staff.PostAsJsonAsync(
            "/api/circulation/desk/checkout", new { readerId = victim, barcodes }));

        var response = await client.PostAsJsonAsync(
            $"/api/reader/loans/{checkout.Loans[0].Id}/renew", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Self_checkout_stays_closed_until_the_library_switches_it_on()
    {
        var staff = await ClientAsync();
        var (client, _, _) = await ReaderClientAsync(staff, "Bạn đọc thử tự mượn");
        var barcodes = await NewCirculatableItemsAsync(staff, "Sách cho mượn tự phục vụ");

        var response = await client.PostAsJsonAsync("/api/reader/loans/self-checkout", new { barcodes });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorTextAsync(response)).Should().Contain("chưa mở");
    }

    [Fact]
    public async Task A_reader_sees_their_own_fines()
    {
        var staff = await ClientAsync();
        var (client, readerId, _) = await ReaderClientAsync(staff, "Bạn đọc xem tiền phạt");

        await ReadAsync<FineRowDto>(await staff.PostAsJsonAsync("/api/circulation/fines", new
        {
            readerId,
            type = FineType.Other,
            amount = 12000m,
            note = "Trả sách muộn"
        }));

        var fines = await ReadAsync<ReaderFineSummaryDto>(await client.GetAsync("/api/reader/fines"));

        fines.TotalOutstanding.Should().Be(12000);
        fines.Fines.Should().ContainSingle();
    }

    // -----------------------------------------------------------------------------------------
    // Phân quyền (điểm kiểm thử 2.3)
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Cataloguing_staff_cannot_reach_the_circulation_desk()
    {
        var admin = await ClientAsync();

        var groups = await ReadAsync<PagedResult<Application.Features.Admin.UserGroups.UserGroupListItemDto>>(
            await admin.GetAsync("/api/admin/user-groups?pageSize=50"));

        var group = groups.Items.Single(item => item.Code == "CATALOGER");
        var username = $"bienmuc{Unique()}";

        var created = await admin.PostAsJsonAsync("/api/admin/users", new
        {
            username,
            profile = new
            {
                fullName = "Cán bộ biên mục kiểm thử quyền lưu thông",
                isActive = true,
                groupIds = new[] { group.Id },
                dataScopes = Array.Empty<object>()
            }
        });

        var payload = await created.Content.ReadFromJsonAsync<ApiResponse<CreatedUserPayload>>(
            LibraryConnectFactory.JsonOptions);

        var client = await _factory.CreateAuthenticatedClientAsync(username, payload!.Data!.TemporaryPassword);

        (await client.GetAsync("/api/circulation/loans")).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/circulation/reports/overdue")).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed class CreatedUserPayload
    {
        public string TemporaryPassword { get; set; } = string.Empty;
    }
}
