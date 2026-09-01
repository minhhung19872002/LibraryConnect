using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Catalogs;
using LibraryConnect.Application.Features.Readers;
using LibraryConnect.Domain.Entities.Cir;
using LibraryConnect.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Phân hệ VI — Bạn đọc, chạy thật qua HTTP: hồ sơ, thẻ, gia hạn và cấp lại thẻ, chuyển trạng thái
/// ra trường, in thẻ, nhập xuất dữ liệu, đồng bộ từ hệ thống đào tạo và báo cáo.
/// </summary>
[Collection(ApiCollection.Name)]
public class ReaderTests
{
    private readonly LibraryConnectFactory _factory;

    public ReaderTests(LibraryConnectFactory factory) => _factory = factory;

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

    /// <summary>Thông báo lỗi đã giải mã, để so sánh được với tiếng Việt có dấu.</summary>
    private static async Task<string> ErrorTextAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse>(LibraryConnectFactory.JsonOptions);

        return string.Join(" | ",
            new[] { payload?.Message }
                .Concat(payload?.Errors?.Select(error => error.Message) ?? Array.Empty<string>())
                .Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    private static async Task<Guid> ReaderTypeAsync(HttpClient client)
    {
        var items = await ReadAsync<PagedResult<CatalogItemDto>>(
            await client.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));

        return items.Items.First().Id;
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private static async Task<Guid> NewReaderAsync(
        HttpClient client,
        string fullName,
        Guid readerTypeId,
        string? className = null,
        string? courseYear = null,
        string? studentCode = null,
        DateOnly? expire = null)
    {
        var response = await client.PostAsJsonAsync("/api/readers", new
        {
            fullName,
            studentCode = studentCode ?? $"SV{Unique()}",
            gender = "Nam",
            dateOfBirth = "2005-09-05",
            email = $"{Unique()}@sinhvien.edu.vn",
            phone = "0901234567",
            readerTypeId,
            className,
            courseYear,
            cardExpireDate = expire?.ToString("yyyy-MM-dd")
        });

        return await ReadAsync<Guid>(response);
    }

    // -----------------------------------------------------------------------------------------
    // VI.1 — Hồ sơ
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task A_new_reader_gets_a_generated_card_number_and_a_card_row()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);

        var id = await NewReaderAsync(client, "Nguyễn Văn An", typeId);
        var reader = await ReadAsync<ReaderDetailDto>(await client.GetAsync($"/api/readers/{id}"));

        reader.CardNumber.Should().NotBeNullOrWhiteSpace();
        reader.Status.Should().Be(ReaderStatus.Active);

        // Hạn thẻ tính từ hạn thẻ của loại bạn đọc, không phải để trống chờ cán bộ tự điền.
        reader.CardExpireDate.Should().BeAfter(reader.CardIssueDate);

        // Sổ cấp thẻ có đúng một thẻ hiện hành, trùng số với hồ sơ.
        reader.Cards.Should().ContainSingle();
        reader.Cards[0].IsCurrent.Should().BeTrue();
        reader.Cards[0].CardNumber.Should().Be(reader.CardNumber);
    }

    [Fact]
    public async Task Two_readers_cannot_share_a_student_code()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var studentCode = $"SV{Unique()}";

        await NewReaderAsync(client, "Trần Thị Bình", typeId, studentCode: studentCode);

        var response = await client.PostAsJsonAsync("/api/readers", new
        {
            fullName = "Người trùng mã",
            studentCode,
            readerTypeId = typeId
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorTextAsync(response)).Should().Contain("đã có trong hệ thống");
    }

    [Fact]
    public async Task A_reader_is_found_by_a_name_typed_without_diacritics()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var name = $"Lê Thị Hồng Nhung {Unique()}";

        var id = await NewReaderAsync(client, name, typeId);

        var result = await ReadAsync<PagedResult<ReaderDto>>(
            await client.GetAsync("/api/readers?keyword=le thi hong nhung&pageSize=50"));

        result.Items.Should().Contain(row => row.Id == id);
    }

    [Fact]
    public async Task A_reader_is_found_by_card_number_and_by_student_code()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var studentCode = $"SV{Unique()}";

        var id = await NewReaderAsync(client, "Phạm Quốc Cường", typeId, studentCode: studentCode);
        var reader = await ReadAsync<ReaderDetailDto>(await client.GetAsync($"/api/readers/{id}"));

        var byCard = await ReadAsync<PagedResult<ReaderDto>>(
            await client.GetAsync($"/api/readers?keyword={reader.CardNumber}"));
        byCard.Items.Should().ContainSingle().Which.Id.Should().Be(id);

        var byStudent = await ReadAsync<PagedResult<ReaderDto>>(
            await client.GetAsync($"/api/readers?keyword={studentCode}"));
        byStudent.Items.Should().ContainSingle().Which.Id.Should().Be(id);
    }

    [Fact]
    public async Task Editing_the_card_expiry_keeps_the_card_register_in_step()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);

        var id = await NewReaderAsync(client, "Đỗ Minh Dũng", typeId);
        var newExpiry = DateOnly.FromDateTime(DateTime.Today).AddYears(3);

        await ReadAsync<Guid>(await client.PutAsJsonAsync($"/api/readers/{id}", new
        {
            fullName = "Đỗ Minh Dũng",
            readerTypeId = typeId,
            cardExpireDate = newExpiry.ToString("yyyy-MM-dd")
        }));

        var reader = await ReadAsync<ReaderDetailDto>(await client.GetAsync($"/api/readers/{id}"));

        reader.CardExpireDate.Should().Be(newExpiry);
        reader.Cards.Single(card => card.IsCurrent).ExpireDate.Should().Be(newExpiry);
    }

    [Fact]
    public async Task An_expired_card_is_reported_as_expired_even_though_the_status_still_says_active()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);

        var id = await NewReaderAsync(client, "Bạn đọc thẻ cũ", typeId,
            expire: DateOnly.FromDateTime(DateTime.Today).AddDays(-3));

        var reader = await ReadAsync<ReaderDetailDto>(await client.GetAsync($"/api/readers/{id}"));

        reader.IsExpired.Should().BeTrue();
        reader.CanBorrow.Should().BeFalse();
    }

    // -----------------------------------------------------------------------------------------
    // VI.1 — Gia hạn, khóa, cấp lại thẻ
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Extending_an_expired_card_counts_from_today_not_from_the_old_expiry()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var id = await NewReaderAsync(client, "Bạn đọc gia hạn", typeId, expire: today.AddDays(-30));

        var result = await ReadAsync<BulkResultDto>(await client.PostAsJsonAsync("/api/readers/cards/extend", new
        {
            selection = new { readerIds = new[] { id } },
            months = 12
        }));

        result.Succeeded.Should().Be(1);

        var reader = await ReadAsync<ReaderDetailDto>(await client.GetAsync($"/api/readers/{id}"));

        reader.CardExpireDate.Should().Be(today.AddMonths(12));
        reader.Cards.Single(card => card.IsCurrent).ExpireDate.Should().Be(today.AddMonths(12));
    }

    [Fact]
    public async Task Extending_a_card_that_is_still_valid_adds_on_top_of_the_remaining_time()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var expiry = today.AddMonths(6);

        var id = await NewReaderAsync(client, "Bạn đọc còn hạn", typeId, expire: expiry);

        await ReadAsync<BulkResultDto>(await client.PostAsJsonAsync("/api/readers/cards/extend", new
        {
            selection = new { readerIds = new[] { id } },
            months = 12
        }));

        var reader = await ReadAsync<ReaderDetailDto>(await client.GetAsync($"/api/readers/{id}"));

        // Phần thời gian bạn đọc chưa dùng đến không được mất khi gia hạn sớm.
        reader.CardExpireDate.Should().Be(expiry.AddMonths(12));
    }

    [Fact]
    public async Task A_whole_cohort_is_extended_from_the_filter_shown_on_screen()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var cohort = $"K{Unique()}";
        var today = DateOnly.FromDateTime(DateTime.Today);

        for (var index = 0; index < 3; index++)
        {
            await NewReaderAsync(client, $"Sinh viên khóa {index}", typeId,
                courseYear: cohort, expire: today.AddDays(10));
        }

        var result = await ReadAsync<BulkResultDto>(await client.PostAsJsonAsync("/api/readers/cards/extend", new
        {
            selection = new { useFilter = true, filter = new { courseYear = cohort } },
            months = 24
        }));

        result.Total.Should().Be(3);
        result.Succeeded.Should().Be(3);

        var readers = await ReadAsync<PagedResult<ReaderDto>>(
            await client.GetAsync($"/api/readers?courseYear={cohort}&pageSize=50"));

        readers.Items.Should().OnlyContain(row => row.CardExpireDate > today.AddYears(1));
    }

    [Fact]
    public async Task Locking_a_card_demands_a_reason()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var id = await NewReaderAsync(client, "Bạn đọc bị khóa", typeId);

        var refused = await client.PostAsJsonAsync("/api/readers/lock", new
        {
            selection = new { readerIds = new[] { id } },
            locked = true
        });

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorTextAsync(refused)).Should().Contain("lý do");

        await ReadAsync<BulkResultDto>(await client.PostAsJsonAsync("/api/readers/lock", new
        {
            selection = new { readerIds = new[] { id } },
            locked = true,
            reason = "Làm hỏng tài liệu mượn về"
        }));

        var reader = await ReadAsync<ReaderDetailDto>(await client.GetAsync($"/api/readers/{id}"));

        reader.Status.Should().Be(ReaderStatus.Suspended);
        reader.StatusReason.Should().Contain("hỏng tài liệu");
        reader.CanBorrow.Should().BeFalse();
    }

    [Fact]
    public async Task Unlocking_returns_the_card_to_active_and_lets_the_reader_borrow_again()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var id = await NewReaderAsync(client, "Bạn đọc mở khóa", typeId);

        await ReadAsync<BulkResultDto>(await client.PostAsJsonAsync("/api/readers/lock", new
        {
            selection = new { readerIds = new[] { id } },
            locked = true,
            reason = "Chờ xác minh"
        }));

        await ReadAsync<BulkResultDto>(await client.PostAsJsonAsync("/api/readers/lock", new
        {
            selection = new { readerIds = new[] { id } },
            locked = false
        }));

        var reader = await ReadAsync<ReaderDetailDto>(await client.GetAsync($"/api/readers/{id}"));

        reader.Status.Should().Be(ReaderStatus.Active);
        reader.CanBorrow.Should().BeTrue();
    }

    [Fact]
    public async Task Reissuing_a_card_keeps_the_old_one_in_the_history()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var id = await NewReaderAsync(client, "Bạn đọc mất thẻ", typeId);

        var before = await ReadAsync<ReaderDetailDto>(await client.GetAsync($"/api/readers/{id}"));

        var newCard = await ReadAsync<ReaderCardDto>(
            await client.PostAsJsonAsync($"/api/readers/{id}/cards/reissue", new
            {
                reason = "Bạn đọc báo mất thẻ"
            }));

        newCard.CardNumber.Should().NotBe(before.CardNumber);

        var after = await ReadAsync<ReaderDetailDto>(await client.GetAsync($"/api/readers/{id}"));

        after.CardNumber.Should().Be(newCard.CardNumber);
        after.Cards.Should().HaveCount(2);
        after.Cards.Count(card => card.IsCurrent).Should().Be(1);

        // Sổ mượn trả cũ ghi theo số thẻ cũ nên dòng thẻ cũ phải còn nguyên.
        after.Cards.Should().Contain(card => card.CardNumber == before.CardNumber && !card.IsCurrent);
    }

    [Fact]
    public async Task Reissuing_a_damaged_card_can_keep_the_same_number()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var id = await NewReaderAsync(client, "Bạn đọc thẻ hỏng", typeId);

        var before = await ReadAsync<ReaderDetailDto>(await client.GetAsync($"/api/readers/{id}"));

        var newCard = await ReadAsync<ReaderCardDto>(
            await client.PostAsJsonAsync($"/api/readers/{id}/cards/reissue", new
            {
                reason = "Thẻ bong lớp phủ, không quét được",
                keepCardNumber = true
            }));

        newCard.CardNumber.Should().Be(before.CardNumber);
    }

    [Fact]
    public async Task Reissuing_a_card_demands_a_reason()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var id = await NewReaderAsync(client, "Bạn đọc thiếu lý do", typeId);

        var response = await client.PostAsJsonAsync($"/api/readers/{id}/cards/reissue", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorTextAsync(response)).Should().Contain("lý do");
    }

    // -----------------------------------------------------------------------------------------
    // VI.1 — Ra trường và công nợ
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task A_reader_with_nothing_outstanding_graduates()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var id = await NewReaderAsync(client, "Sinh viên tốt nghiệp", typeId);

        var clearance = await ReadAsync<ReaderClearanceDto>(
            await client.GetAsync($"/api/readers/{id}/clearance"));

        clearance.Cleared.Should().BeTrue();

        var result = await ReadAsync<BulkResultDto>(await client.PostAsJsonAsync("/api/readers/graduate", new
        {
            selection = new { readerIds = new[] { id } }
        }));

        result.Succeeded.Should().Be(1);

        var reader = await ReadAsync<ReaderDetailDto>(await client.GetAsync($"/api/readers/{id}"));
        reader.Status.Should().Be(ReaderStatus.Graduated);
    }

    [Fact]
    public async Task A_reader_who_still_owes_a_fine_is_held_back_from_graduating()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var id = await NewReaderAsync(client, "Sinh viên còn nợ phí", typeId);

        // Tiền phạt do phân hệ Lưu thông sinh ra ở đợt sau; ở đây ghi thẳng một khoản nợ thật vào sổ
        // phạt để kiểm chứng đúng luật chặn ra trường của Phân hệ VI.
        await AddFineAsync(id, 25000);

        var clearance = await ReadAsync<ReaderClearanceDto>(
            await client.GetAsync($"/api/readers/{id}/clearance"));

        clearance.Cleared.Should().BeFalse();
        clearance.OutstandingFines.Should().Be(25000);

        var result = await ReadAsync<BulkResultDto>(await client.PostAsJsonAsync("/api/readers/graduate", new
        {
            selection = new { readerIds = new[] { id } }
        }));

        result.Succeeded.Should().Be(0);
        result.Skipped.Should().Be(1);
        result.Skips.Should().ContainSingle().Which.Reason.Should().Contain("nợ");

        var reader = await ReadAsync<ReaderDetailDto>(await client.GetAsync($"/api/readers/{id}"));
        reader.Status.Should().NotBe(ReaderStatus.Graduated);
    }

    [Fact]
    public async Task A_reader_who_still_owes_a_fine_cannot_be_deleted_either()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var id = await NewReaderAsync(client, "Bạn đọc nợ phí", typeId);

        await AddFineAsync(id, 12000);

        var response = await client.DeleteAsync($"/api/readers/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorTextAsync(response)).Should().Contain("nợ");
    }

    /// <summary>Ghi một khoản phạt thật vào sổ phạt của bạn đọc.</summary>
    private async Task AddFineAsync(Guid readerId, decimal amount)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        db.Fines.Add(new Fine
        {
            ReaderId = readerId,
            Code = $"PH{Unique()}",
            Type = FineType.Overdue,
            Amount = amount,
            PaidAmount = 0
        });

        await db.SaveChangesAsync(CancellationToken.None);
    }

    // -----------------------------------------------------------------------------------------
    // VI.1 — Vi phạm và lịch sử
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task A_violation_takes_the_default_fine_of_its_type()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var id = await NewReaderAsync(client, "Bạn đọc vi phạm", typeId);

        var violationTypeId = await ReadAsync<Guid>(await client.PostAsJsonAsync(
            "/api/catalogs/violation-types/items", new
            {
                code = $"VP{Unique()}",
                name = "Làm rách trang sách",
                isActive = true,
                extras = new Dictionary<string, string> { ["defaultFine"] = "50000" }
            }));

        await ReadAsync<Guid>(await client.PostAsJsonAsync($"/api/readers/{id}/violations", new
        {
            violationTypeId,
            description = "Rách 2 trang cuốn Giáo trình cơ sở dữ liệu"
        }));

        var violations = await ReadAsync<PagedResult<ReaderViolationDto>>(
            await client.GetAsync($"/api/readers/{id}/violations"));

        violations.Items.Should().ContainSingle();
        violations.Items[0].FineAmount.Should().Be(50000);
        violations.Items[0].ViolationTypeName.Should().Be("Làm rách trang sách");
    }

    [Fact]
    public async Task The_history_tabs_answer_even_when_the_reader_has_no_activity_yet()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var id = await NewReaderAsync(client, "Bạn đọc mới tinh", typeId);

        (await ReadAsync<PagedResult<ReaderLoanDto>>(
            await client.GetAsync($"/api/readers/{id}/loans?currentOnly=true"))).TotalCount.Should().Be(0);

        (await ReadAsync<PagedResult<ReaderFineDto>>(
            await client.GetAsync($"/api/readers/{id}/fines"))).TotalCount.Should().Be(0);

        (await ReadAsync<PagedResult<ReaderVisitDto>>(
            await client.GetAsync($"/api/readers/{id}/visits"))).TotalCount.Should().Be(0);

        (await ReadAsync<PagedResult<ReaderDigitalAccessDto>>(
            await client.GetAsync($"/api/readers/{id}/digital-access"))).TotalCount.Should().Be(0);
    }

    // -----------------------------------------------------------------------------------------
    // VI.1 — Ảnh chân dung
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task A_photo_upload_is_checked_by_its_binary_signature_not_by_its_name()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var id = await NewReaderAsync(client, "Bạn đọc có ảnh", typeId);

        var fake = new MultipartFormDataContent();
        var fakeContent = new ByteArrayContent(new byte[] { 0x4D, 0x5A, 0x90, 0x00 });
        fakeContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        fake.Add(fakeContent, "file", "anh-that.jpg");

        var refused = await client.PostAsync($"/api/readers/{id}/photo", fake);

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorTextAsync(refused)).Should().Contain("không phải ảnh");

        var real = new MultipartFormDataContent();
        var realContent = new ByteArrayContent(PngBytes());
        realContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        real.Add(realContent, "file", "anh.png");

        await ReadAsync<string>(await client.PostAsync($"/api/readers/{id}/photo", real));

        var photo = await client.GetAsync($"/api/readers/{id}/photo");
        photo.StatusCode.Should().Be(HttpStatusCode.OK);
        (await photo.Content.ReadAsByteArrayAsync()).Should().Equal(PngBytes());
    }

    /// <summary>Một ảnh PNG 1×1 hợp lệ.</summary>
    private static byte[] PngBytes() => Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    // -----------------------------------------------------------------------------------------
    // VI.2 — In thẻ
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task The_system_ships_with_a_working_cr80_card_template()
    {
        var client = await ClientAsync();

        var templates = await ReadAsync<IReadOnlyList<ReaderCardTemplateDto>>(
            await client.GetAsync("/api/readers/card-templates"));

        var standard = templates.Should().ContainSingle(template => template.IsDefault).Subject;

        standard.WidthMm.Should().BeApproximately(85.6, 0.01);
        standard.HeightMm.Should().BeApproximately(54, 0.01);
        standard.Front.Boxes.Should().NotBeEmpty();
        standard.Front.Barcode.Should().NotBeNull();
        standard.Front.Images.Should().Contain(image => image.Kind == ReaderCardImageKinds.Photo);
    }

    [Fact]
    public async Task Printing_a_card_produces_a_pdf_and_counts_the_print()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var id = await NewReaderAsync(client, "Bạn đọc in thẻ", typeId, className: "DH21TH1", courseYear: "K21");

        var response = await client.PostAsJsonAsync("/api/readers/cards/print", new
        {
            selection = new { readerIds = new[] { id } },
            multiplePerPage = false
        });

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

        var pdf = await response.Content.ReadAsByteArrayAsync();
        pdf.Length.Should().BeGreaterThan(1000);
        System.Text.Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");

        var reader = await ReadAsync<ReaderDetailDto>(await client.GetAsync($"/api/readers/{id}"));
        reader.Cards.Single(card => card.IsCurrent).PrintCount.Should().Be(1);
    }

    [Fact]
    public async Task Previewing_a_card_does_not_count_as_a_print()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var id = await NewReaderAsync(client, "Bạn đọc xem trước", typeId);

        var response = await client.PostAsJsonAsync("/api/readers/cards/print", new
        {
            selection = new { readerIds = new[] { id } },
            preview = true
        });

        response.IsSuccessStatusCode.Should().BeTrue();

        var reader = await ReadAsync<ReaderDetailDto>(await client.GetAsync($"/api/readers/{id}"));
        reader.Cards.Single(card => card.IsCurrent).PrintCount.Should().Be(0);
    }

    [Fact]
    public async Task A_whole_class_is_printed_from_the_filter_on_one_a4_sheet_layout()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var className = $"DH{Unique()}";

        for (var index = 0; index < 4; index++)
        {
            await NewReaderAsync(client, $"Sinh viên in thẻ {index}", typeId, className: className);
        }

        var response = await client.PostAsJsonAsync("/api/readers/cards/print", new
        {
            selection = new { useFilter = true, filter = new { className } },
            multiplePerPage = true
        });

        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        (await response.Content.ReadAsByteArrayAsync()).Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task A_card_template_with_content_outside_the_card_is_refused()
    {
        var client = await ClientAsync();

        var response = await client.PostAsJsonAsync("/api/readers/card-templates", new
        {
            code = $"THE{Unique()}",
            name = "Mẫu thẻ sai khổ",
            widthMm = 85.6,
            heightMm = 54,
            front = new
            {
                boxes = new[]
                {
                    new { x = 70.0, y = 4.0, width = 30.0, height = 6.0, source = "fullName" }
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorTextAsync(response)).Should().Contain("ngoài khổ thẻ");
    }

    // -----------------------------------------------------------------------------------------
    // VI.4 — Nhập, xuất và đồng bộ
    // -----------------------------------------------------------------------------------------

    private static MultipartFormDataContent ExcelUpload(byte[] content, string? options = null)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);

        file.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        form.Add(file, "file", "ban-doc.xlsx");

        if (options is not null)
        {
            form.Add(new StringContent(options), "options");
        }

        return form;
    }

    [Fact]
    public async Task The_import_template_can_be_downloaded_and_read_back()
    {
        var client = await ClientAsync();

        var response = await client.GetAsync("/api/readers/import/template");

        response.IsSuccessStatusCode.Should().BeTrue();
        (await response.Content.ReadAsByteArrayAsync()).Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task Validating_a_file_reports_the_bad_rows_and_writes_nothing()
    {
        var client = await ClientAsync();
        var studentCode = $"SV{Unique()}";

        var sheet = ExcelFixtures.BuildReaderSheet(new[]
        {
            new[] { "", studentCode, "Nguyễn Văn Hợp", "Nam", "05/09/2005", "", "hop@sv.edu.vn",
                    "0901234567", "", "Sinh viên" },
            new[] { "", $"SV{Unique()}", "", "Nữ", "05/09/2005", "", "", "", "", "Sinh viên" },
            new[] { "", $"SV{Unique()}", "Trần Thị Sai Ngày", "Nữ", "ngày nào đó", "", "", "", "", "Sinh viên" },
            new[] { "", $"SV{Unique()}", "Lê Văn Sai Email", "Nam", "01/01/2004", "", "khong-phai-email",
                    "", "", "Sinh viên" }
        });

        var preview = await ReadAsync<ReaderImportPreviewDto>(
            await client.PostAsync("/api/readers/import/validate", ExcelUpload(sheet)));

        preview.TotalRows.Should().Be(4);
        preview.ErrorRows.Should().Be(3);
        preview.Errors.Should().Contain(error => error.Message.Contains("họ và tên"));
        preview.Errors.Should().Contain(error => error.Message.Contains("Ngày không đọc được"));
        preview.Errors.Should().Contain(error => error.Message.Contains("email không hợp lệ"));

        // Bước kiểm tra không được ghi gì: mã sinh viên hợp lệ ở dòng đầu vẫn phải chưa tồn tại.
        var search = await ReadAsync<PagedResult<ReaderDto>>(
            await client.GetAsync($"/api/readers?keyword={studentCode}"));

        search.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Duplicate_student_codes_inside_the_file_are_reported_against_the_second_row()
    {
        var client = await ClientAsync();
        var duplicated = $"SV{Unique()}";

        var sheet = ExcelFixtures.BuildReaderSheet(new[]
        {
            new[] { "", duplicated, "Người thứ nhất", "", "", "", "", "", "", "Sinh viên" },
            new[] { "", duplicated, "Người thứ hai", "", "", "", "", "", "", "Sinh viên" }
        });

        var preview = await ReadAsync<ReaderImportPreviewDto>(
            await client.PostAsync("/api/readers/import/validate", ExcelUpload(sheet)));

        preview.Errors.Should().Contain(error =>
            error.Row == 3 && error.Message.Contains("lặp trong chính tệp"));
    }

    [Fact]
    public async Task Importing_creates_the_readers_and_registers_the_missing_catalogue_values()
    {
        var client = await ClientAsync();
        var cohort = $"K{Unique()}";
        var className = $"DH{Unique()}";
        var facultyName = $"Khoa Thử nghiệm {Unique()}";

        var sheet = ExcelFixtures.BuildReaderSheet(new[]
        {
            new[]
            {
                "", $"SV{Unique()}", "Nguyễn Thị Nhập", "Nữ", "12/03/2005", "", $"{Unique()}@sv.edu.vn",
                "0912345678", "12 Nguyễn Huệ", "Sinh viên", facultyName, "Kỹ thuật phần mềm",
                className, cohort
            },
            new[]
            {
                "", $"SV{Unique()}", "Trần Văn Nhập", "Nam", "20/07/2004", "", $"{Unique()}@sv.edu.vn",
                "0912345679", "", "Sinh viên", facultyName, "Kỹ thuật phần mềm", className, cohort
            }
        });

        var batchId = await ReadAsync<Guid>(
            await client.PostAsync("/api/readers/import", ExcelUpload(sheet)));

        var batch = await WaitForBatchAsync(client, batchId);

        batch.Status.Should().Be(JobStatus.Completed);
        batch.TotalRows.Should().Be(2);
        batch.SuccessRows.Should().Be(2);
        batch.ErrorRows.Should().Be(0);

        var readers = await ReadAsync<PagedResult<ReaderDto>>(
            await client.GetAsync($"/api/readers?className={className}&pageSize=20"));

        readers.TotalCount.Should().Be(2);
        readers.Items.Should().OnlyContain(row => row.CourseYear == cohort);

        // Số thẻ để trống trong tệp thì hệ thống phải tự sinh, không được để rỗng.
        readers.Items.Should().OnlyContain(row => !string.IsNullOrWhiteSpace(row.CardNumber));

        // Khoa, lớp và khóa chưa có được ghi vào danh mục để lần sau lọc được.
        var cohorts = await ReadAsync<PagedResult<CatalogItemDto>>(
            await client.GetAsync($"/api/catalogs/cohorts/items?keyword={cohort}"));
        cohorts.Items.Should().ContainSingle(item => item.Code == cohort);

        var classes = await ReadAsync<PagedResult<CatalogItemDto>>(
            await client.GetAsync($"/api/catalogs/student-classes/items?keyword={className}"));
        classes.Items.Should().ContainSingle(item => item.Code == className);
    }

    [Fact]
    public async Task Re_importing_the_same_students_updates_them_instead_of_duplicating()
    {
        var client = await ClientAsync();
        var studentCode = $"SV{Unique()}";

        var first = ExcelFixtures.BuildReaderSheet(new[]
        {
            new[] { "", studentCode, "Phạm Văn Cũ", "Nam", "01/01/2005", "", "", "", "", "Sinh viên" }
        });

        var firstBatch = await WaitForBatchAsync(client,
            await ReadAsync<Guid>(await client.PostAsync("/api/readers/import", ExcelUpload(first))));

        firstBatch.SuccessRows.Should().Be(1);

        var second = ExcelFixtures.BuildReaderSheet(new[]
        {
            new[] { "", studentCode, "Phạm Văn Mới", "Nam", "01/01/2005", "", "", "", "", "Sinh viên" }
        });

        var options = """{"onDuplicate":2}""";

        var secondBatch = await WaitForBatchAsync(client,
            await ReadAsync<Guid>(await client.PostAsync("/api/readers/import", ExcelUpload(second, options))));

        secondBatch.SuccessRows.Should().Be(1);

        var readers = await ReadAsync<PagedResult<ReaderDto>>(
            await client.GetAsync($"/api/readers?keyword={studentCode}"));

        readers.TotalCount.Should().Be(1);
        readers.Items[0].FullName.Should().Be("Phạm Văn Mới");
    }

    [Fact]
    public async Task Importing_a_student_who_already_exists_is_refused_by_default()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var studentCode = $"SV{Unique()}";

        await NewReaderAsync(client, "Người đã có hồ sơ", typeId, studentCode: studentCode);

        var sheet = ExcelFixtures.BuildReaderSheet(new[]
        {
            new[] { "", studentCode, "Người trùng", "Nam", "", "", "", "", "", "Sinh viên" }
        });

        var preview = await ReadAsync<ReaderImportPreviewDto>(
            await client.PostAsync("/api/readers/import/validate", ExcelUpload(sheet)));

        preview.ErrorRows.Should().Be(1);
        preview.Errors.Should().Contain(error => error.Message.Contains("đã có trong hệ thống"));
    }

    private async Task<ReaderImportBatchDto> WaitForBatchAsync(HttpClient client, Guid batchId)
    {
        for (var attempt = 0; attempt < 60; attempt++)
        {
            var batch = await ReadAsync<ReaderImportBatchDto>(
                await client.GetAsync($"/api/readers/import/batches/{batchId}"));

            if (batch.Status is JobStatus.Completed or JobStatus.Failed)
            {
                return batch;
            }

            await Task.Delay(500);
        }

        throw new InvalidOperationException($"Đợt nhập {batchId} không kết thúc trong thời gian chờ.");
    }

    [Fact]
    public async Task The_reader_list_exports_to_excel_by_the_filter_on_screen()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var className = $"DH{Unique()}";

        await NewReaderAsync(client, "Bạn đọc xuất Excel", typeId, className: className);

        var response = await client.GetAsync($"/api/readers/export?className={className}");

        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType!.MediaType.Should()
            .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        (await response.Content.ReadAsByteArrayAsync()).Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task The_training_system_syncs_students_through_its_own_field_names()
    {
        var client = await ClientAsync();
        var studentCode = $"SV{Unique()}";
        var className = $"DH{Unique()}";

        await ReadAsync<object>(await client.PutAsJsonAsync("/api/readers/sync/mapping",
            new Dictionary<string, string>
            {
                ["studentCode"] = "MaSinhVien",
                ["fullName"] = "HoTen",
                ["className"] = "MaLop",
                ["readerType"] = "DoiTuong"
            }));

        var result = await ReadAsync<ReaderSyncResultDto>(
            await client.PostAsJsonAsync("/api/readers/sync", new
            {
                items = new[]
                {
                    new Dictionary<string, string>
                    {
                        ["MaSinhVien"] = studentCode,
                        ["HoTen"] = "Vũ Thị Đồng Bộ",
                        ["MaLop"] = className,
                        ["DoiTuong"] = "Sinh viên"
                    }
                }
            }));

        result.Created.Should().Be(1);
        result.ErrorItems.Should().Be(0);

        var readers = await ReadAsync<PagedResult<ReaderDto>>(
            await client.GetAsync($"/api/readers?keyword={studentCode}"));

        readers.Items.Should().ContainSingle().Which.FullName.Should().Be("Vũ Thị Đồng Bộ");

        // Gọi lại lần hai với tên đã đổi: đồng bộ phải cập nhật chứ không tạo thêm hồ sơ.
        var again = await ReadAsync<ReaderSyncResultDto>(
            await client.PostAsJsonAsync("/api/readers/sync", new
            {
                items = new[]
                {
                    new Dictionary<string, string>
                    {
                        ["MaSinhVien"] = studentCode,
                        ["HoTen"] = "Vũ Thị Đồng Bộ Lần Hai",
                        ["MaLop"] = className,
                        ["DoiTuong"] = "Sinh viên"
                    }
                }
            }));

        again.Updated.Should().Be(1);
        again.Created.Should().Be(0);

        var after = await ReadAsync<PagedResult<ReaderDto>>(
            await client.GetAsync($"/api/readers?keyword={studentCode}"));

        after.TotalCount.Should().Be(1);
        after.Items[0].FullName.Should().Be("Vũ Thị Đồng Bộ Lần Hai");
    }

    [Fact]
    public async Task A_dry_run_sync_reports_what_would_happen_without_writing()
    {
        var client = await ClientAsync();
        var studentCode = $"SV{Unique()}";

        var result = await ReadAsync<ReaderSyncResultDto>(
            await client.PostAsJsonAsync("/api/readers/sync", new
            {
                dryRun = true,
                mapping = new Dictionary<string, string>
                {
                    ["studentCode"] = "studentCode",
                    ["fullName"] = "fullName",
                    ["readerType"] = "readerType"
                },
                items = new[]
                {
                    new Dictionary<string, string>
                    {
                        ["studentCode"] = studentCode,
                        ["fullName"] = "Người chỉ thử",
                        ["readerType"] = "Sinh viên"
                    }
                }
            }));

        result.DryRun.Should().BeTrue();
        result.Created.Should().Be(1);

        var readers = await ReadAsync<PagedResult<ReaderDto>>(
            await client.GetAsync($"/api/readers?keyword={studentCode}"));

        readers.TotalCount.Should().Be(0);
    }

    // -----------------------------------------------------------------------------------------
    // VI.5 — Báo cáo
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task The_count_report_totals_match_the_reader_list()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var cohort = $"K{Unique()}";

        for (var index = 0; index < 3; index++)
        {
            await NewReaderAsync(client, $"Bạn đọc báo cáo {index}", typeId, courseYear: cohort);
        }

        var rows = await ReadAsync<IReadOnlyList<ReaderReportRowDto>>(
            await client.GetAsync($"/api/readers/reports/count?dimension=Cohort&courseYear={cohort}"));

        rows.Should().ContainSingle();
        rows[0].Label.Should().Be(cohort);
        rows[0].Total.Should().Be(3);
        rows[0].Percentage.Should().Be(100);

        var list = await ReadAsync<PagedResult<ReaderDto>>(
            await client.GetAsync($"/api/readers?courseYear={cohort}"));

        // Con số trên báo cáo phải khớp với con số trên danh sách — đây là điểm kiểm thử 2.8.
        rows[0].Total.Should().Be(list.TotalCount);
    }

    [Fact]
    public async Task Expiring_cards_are_listed_with_the_days_left_and_the_expired_ones_are_negative()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var cohort = $"K{Unique()}";

        await NewReaderAsync(client, "Thẻ sắp hết hạn", typeId, courseYear: cohort, expire: today.AddDays(10));
        await NewReaderAsync(client, "Thẻ đã hết hạn", typeId, courseYear: cohort, expire: today.AddDays(-5));
        await NewReaderAsync(client, "Thẻ còn dài", typeId, courseYear: cohort, expire: today.AddYears(2));

        var report = await ReadAsync<ExpiringCardsReportDto>(
            await client.GetAsync($"/api/readers/reports/expiring-cards?withinDays=30&courseYear={cohort}"));

        report.ExpiringCount.Should().Be(1);
        report.ExpiredCount.Should().Be(1);
        report.ValidCount.Should().Be(1);

        report.Rows.Should().HaveCount(2);
        report.Rows.Should().Contain(row => row.DaysLeft == 10);
        report.Rows.Should().Contain(row => row.DaysLeft == -5);
    }

    [Fact]
    public async Task Graduated_readers_are_kept_out_of_the_card_renewal_reminders()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var cohort = $"K{Unique()}";

        var id = await NewReaderAsync(client, "Cựu sinh viên", typeId,
            courseYear: cohort, expire: today.AddDays(-1));

        await ReadAsync<BulkResultDto>(await client.PostAsJsonAsync("/api/readers/graduate", new
        {
            selection = new { readerIds = new[] { id } }
        }));

        var report = await ReadAsync<ExpiringCardsReportDto>(
            await client.GetAsync($"/api/readers/reports/expiring-cards?withinDays=30&courseYear={cohort}"));

        // Thẻ của người đã ra trường hết hạn là đúng quy trình, không phải việc cần nhắc gia hạn.
        report.Rows.Should().NotContain(row => row.ReaderId == id);
    }

    [Fact]
    public async Task Readers_who_never_borrowed_are_listed_for_outreach()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var cohort = $"K{Unique()}";

        var id = await NewReaderAsync(client, "Bạn đọc chưa mượn bao giờ", typeId, courseYear: cohort);

        var rows = await ReadAsync<IReadOnlyList<ReaderActivityRowDto>>(
            await client.GetAsync($"/api/readers/reports/activity?neverBorrowed=true&top=50&courseYear={cohort}"));

        rows.Should().ContainSingle().Which.ReaderId.Should().Be(id);
        rows[0].LoanCount.Should().Be(0);
    }

    [Fact]
    public async Task Registrations_are_grouped_by_month()
    {
        var client = await ClientAsync();
        var typeId = await ReaderTypeAsync(client);
        var cohort = $"K{Unique()}";

        await NewReaderAsync(client, "Bạn đọc đăng ký", typeId, courseYear: cohort);

        var rows = await ReadAsync<IReadOnlyList<ReaderTimeRowDto>>(
            await client.GetAsync($"/api/readers/reports/registrations?grouping=Month&courseYear={cohort}"));

        rows.Should().ContainSingle();
        rows[0].Period.Should().StartWith("Tháng ");
        rows[0].NewReaders.Should().Be(1);
        rows[0].Cumulative.Should().Be(1);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(2, false)]
    [InlineData(0, true)]
    public async Task Every_reader_report_exports_to_excel_and_pdf(int kind, bool asPdf)
    {
        var client = await ClientAsync();

        var response = await client.PostAsJsonAsync("/api/readers/reports/export", new
        {
            kind,
            asPdf,
            dimension = "ReaderType",
            withinDays = 30,
            top = 20,
            filter = new { }
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
    // Phân quyền (điểm kiểm thử 2.3)
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Cataloguing_staff_cannot_reach_the_reader_module()
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
                fullName = "Cán bộ biên mục kiểm thử quyền bạn đọc",
                isActive = true,
                groupIds = new[] { group.Id },
                dataScopes = Array.Empty<object>()
            }
        });

        var payload = await created.Content.ReadFromJsonAsync<ApiResponse<CreatedUserPayload>>(
            LibraryConnectFactory.JsonOptions);

        var client = await _factory.CreateAuthenticatedClientAsync(username, payload!.Data!.TemporaryPassword);

        (await client.GetAsync("/api/readers")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/readers/reports/count?dimension=ReaderType")).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed class CreatedUserPayload
    {
        public string TemporaryPassword { get; set; } = string.Empty;
    }
    /// <summary>
    /// Ngày sinh gõ nhầm phải bị chặn ngay.
    ///
    /// 2099 thay vì 1999 là lỗi gõ phổ biến nhất trên bàn phím số. Không chặn thì hồ sơ sai nằm im
    /// trong kho, và mọi báo cáo theo độ tuổi đều lệch theo mà không ai biết vì sao.
    /// </summary>
    [Theory]
    [InlineData("2099-01-01")]
    [InlineData("1830-05-20")]
    public async Task Ngay_sinh_vo_ly_thi_bi_chan(string ngaySinh)
    {
        var client = await ClientAsync();
        var types = await ReadAsync<PagedResult<Application.Features.Catalogs.CatalogItemDto>>(
            await client.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));

        var response = await client.PostAsJsonAsync("/api/readers", new
        {
            fullName = "Bạn đọc ngày sinh vô lý",
            studentCode = $"SV{Guid.NewGuid():N}"[..12],
            readerTypeId = types.Items.First().Id,
            dateOfBirth = ngaySinh
        }, LibraryConnectFactory.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse>(
            LibraryConnectFactory.JsonOptions);

        payload!.Errors.Should().Contain(error => error.Message.Contains("Ngày sinh"));
    }
}
