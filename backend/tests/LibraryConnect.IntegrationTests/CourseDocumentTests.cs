using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClosedXML.Excel;
using FluentAssertions;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Application.Features.Catalogs;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Application.Features.Courses;
using LibraryConnect.Application.Features.Locations;
using LibraryConnect.Application.Features.Opac;
using LibraryConnect.Application.Features.Readers;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Phân hệ X — Tài liệu môn học, chạy thật qua HTTP.
///
/// Phần đáng kiểm nhất không phải CRUD mà là ba chỗ nghiệp vụ dễ sai: quan hệ nhiều-nhiều giữa môn
/// và ngành, việc gán lại một tài liệu đã gán, và bộ đọc Excel do khoa gửi sang với đủ kiểu viết.
/// </summary>
[Collection(ApiCollection.Name)]
public class CourseDocumentTests
{
    private readonly LibraryConnectFactory _factory;

    public CourseDocumentTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> StaffAsync() =>
        _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            "máy chủ trả về {0}: {1}", response.StatusCode, await response.Content.ReadAsStringAsync());

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(
            LibraryConnectFactory.JsonOptions);

        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue(payload.Message);

        return payload.Data!;
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private static async Task<Guid> NewCourseAsync(HttpClient staff, string code, string name)
    {
        return await ReadAsync<Guid>(await staff.PostAsJsonAsync("/api/catalogs/courses/items", new
        {
            code,
            name,
            sortOrder = 0,
            isActive = true,
            extras = new Dictionary<string, string> { ["credits"] = "3" }
        }));
    }

    private static async Task<Guid> NewMajorAsync(HttpClient staff, string code, string name)
    {
        return await ReadAsync<Guid>(await staff.PostAsJsonAsync("/api/catalogs/majors/items", new
        {
            code,
            name,
            sortOrder = 0,
            isActive = true
        }));
    }

    private static async Task<(Guid BibId, string Isbn)> NewBibAsync(HttpClient staff, string title)
    {
        var warehouses = await ReadAsync<IReadOnlyList<WarehouseDto>>(
            await staff.GetAsync("/api/locations/warehouses"));

        var isbn = $"978604{Random.Shared.Next(1000000, 9999999)}";

        var quick = await ReadAsync<QuickCatalogResultDto>(await staff.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title,
                author = "Nguyễn Văn Môn Học",
                price = 100000m,
                ddc = "005.1",
                isbn,
                itemQuantity = 1,
                warehouseId = warehouses[0].Id,
                reuseDuplicate = false
            }));

        return (quick.BibId, isbn);
    }

    // -----------------------------------------------------------------------------------------
    // X.2 — Môn học và ngành đào tạo
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Danh_muc_mon_hoc_va_nganh_duoc_nap_san_khi_cai_dat()
    {
        var staff = await StaffAsync();

        var courses = await ReadAsync<PagedResult<CourseRowDto>>(
            await staff.GetAsync("/api/courses?pageSize=200"));

        // Mục 8 yêu cầu có dữ liệu để demo ngay; danh mục môn học mẫu phải gồm cả những môn nhiều
        // ngành cùng học, vì đó mới là chỗ quan hệ nhiều-nhiều có ý nghĩa.
        courses.TotalCount.Should().BeGreaterThan(0);
        courses.Items.Should().Contain(course => course.Majors.Count > 1);
    }

    [Fact]
    public async Task Gan_mon_hoc_vao_nhieu_nganh_va_bo_bot_duoc()
    {
        var staff = await StaffAsync();
        var marker = Unique();

        var courseId = await NewCourseAsync(staff, $"MH{marker}", $"Môn thử {marker}");
        var first = await NewMajorAsync(staff, $"NG1{marker}", $"Ngành một {marker}");
        var second = await NewMajorAsync(staff, $"NG2{marker}", $"Ngành hai {marker}");

        var linked = await staff.PutAsJsonAsync($"/api/courses/{courseId}/majors",
            new { majorIds = new[] { first, second } });

        linked.IsSuccessStatusCode.Should().BeTrue(
            "máy chủ trả về {0}: {1}",
            linked.StatusCode, await linked.Content.ReadAsStringAsync());

        var both = await ReadAsync<PagedResult<CourseRowDto>>(
            await staff.GetAsync($"/api/courses?keyword={marker}"));

        both.Items.Single().Majors.Should().HaveCount(2);

        (await staff.PutAsJsonAsync($"/api/courses/{courseId}/majors",
            new { majorIds = new[] { second } })).IsSuccessStatusCode.Should().BeTrue();

        var one = await ReadAsync<PagedResult<CourseRowDto>>(
            await staff.GetAsync($"/api/courses?keyword={marker}"));

        one.Items.Single().Majors.Should().ContainSingle()
            .Which.Id.Should().Be(second);
    }

    [Fact]
    public async Task Gan_nganh_khong_ton_tai_bi_tu_choi()
    {
        var staff = await StaffAsync();
        var courseId = await NewCourseAsync(staff, $"MH{Unique()}", "Môn thử ngành lạ");

        var response = await staff.PutAsJsonAsync($"/api/courses/{courseId}/majors",
            new { majorIds = new[] { Guid.NewGuid() } });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -----------------------------------------------------------------------------------------
    // X.3 — Liên kết tài liệu
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Gan_tai_lieu_cho_mon_hoc_va_doc_lai_duoc()
    {
        var staff = await StaffAsync();
        var marker = Unique();

        var courseId = await NewCourseAsync(staff, $"MH{marker}", $"Môn có tài liệu {marker}");
        var (bibId, _) = await NewBibAsync(staff, $"Giáo trình môn học {marker}");

        var added = await ReadAsync<int>(await staff.PostAsJsonAsync(
            $"/api/courses/{courseId}/documents",
            new { bibIds = new[] { bibId }, relationType = "MainTextbook", note = "Đọc chương 1" }));

        added.Should().Be(1);

        var documents = await ReadAsync<IReadOnlyList<CourseDocumentDto>>(
            await staff.GetAsync($"/api/courses/{courseId}/documents"));

        documents.Should().ContainSingle();
        documents[0].RelationLabel.Should().Be("Giáo trình chính");
        documents[0].Note.Should().Be("Đọc chương 1");
    }

    [Fact]
    public async Task Gan_lai_cung_tai_lieu_thi_cap_nhat_chu_khong_tao_dong_thu_hai()
    {
        var staff = await StaffAsync();
        var marker = Unique();

        var courseId = await NewCourseAsync(staff, $"MH{marker}", $"Môn gán lại {marker}");
        var (bibId, _) = await NewBibAsync(staff, $"Sách gán lại {marker}");

        await ReadAsync<int>(await staff.PostAsJsonAsync($"/api/courses/{courseId}/documents",
            new { bibIds = new[] { bibId }, relationType = "AdditionalReference" }));

        var addedAgain = await ReadAsync<int>(await staff.PostAsJsonAsync(
            $"/api/courses/{courseId}/documents",
            new { bibIds = new[] { bibId }, relationType = "MainTextbook" }));

        addedAgain.Should().Be(0, "cuốn này đã có, chỉ đổi mức độ");

        var documents = await ReadAsync<IReadOnlyList<CourseDocumentDto>>(
            await staff.GetAsync($"/api/courses/{courseId}/documents"));

        // Hai dòng cho cùng một cuốn sẽ hiện hai lần trên trang tra cứu của bạn đọc.
        documents.Should().ContainSingle();
        documents[0].RelationLabel.Should().Be("Giáo trình chính");
    }

    [Fact]
    public async Task Sua_muc_do_va_ghi_chu_ngay_tren_dong_da_gan()
    {
        var staff = await StaffAsync();
        var marker = Unique();

        var courseId = await NewCourseAsync(staff, $"MH{marker}", $"Môn sửa mức độ {marker}");
        var (bibId, _) = await NewBibAsync(staff, $"Sách sửa mức độ {marker}");

        await ReadAsync<int>(await staff.PostAsJsonAsync($"/api/courses/{courseId}/documents",
            new { bibIds = new[] { bibId }, relationType = "AdditionalReference" }));

        var documents = await ReadAsync<IReadOnlyList<CourseDocumentDto>>(
            await staff.GetAsync($"/api/courses/{courseId}/documents"));

        // Cán bộ đổi mức độ ngay trên dòng chứ không bỏ tài liệu ra rồi gán lại — thao tác kia làm
        // mất ghi chú đã nhập và làm trang tra cứu của bạn đọc trống một khoảng.
        (await staff.PutAsJsonAsync($"/api/courses/documents/{documents[0].Id}",
                new { relationType = "MainTextbook", note = "Đọc chương 1–4" }))
            .IsSuccessStatusCode.Should().BeTrue();

        var after = await ReadAsync<IReadOnlyList<CourseDocumentDto>>(
            await staff.GetAsync($"/api/courses/{courseId}/documents"));

        after.Should().ContainSingle();
        after[0].Id.Should().Be(documents[0].Id, "vẫn là dòng cũ chứ không phải dòng mới");
        after[0].RelationLabel.Should().Be("Giáo trình chính");
        after[0].Note.Should().Be("Đọc chương 1–4");
    }

    [Fact]
    public async Task Bo_tai_lieu_khoi_mon_hoc()
    {
        var staff = await StaffAsync();
        var marker = Unique();

        var courseId = await NewCourseAsync(staff, $"MH{marker}", $"Môn bỏ tài liệu {marker}");
        var (bibId, _) = await NewBibAsync(staff, $"Sách sẽ bỏ {marker}");

        await ReadAsync<int>(await staff.PostAsJsonAsync($"/api/courses/{courseId}/documents",
            new { bibIds = new[] { bibId }, relationType = "RequiredReference" }));

        var documents = await ReadAsync<IReadOnlyList<CourseDocumentDto>>(
            await staff.GetAsync($"/api/courses/{courseId}/documents"));

        (await staff.DeleteAsync($"/api/courses/documents/{documents[0].Id}"))
            .IsSuccessStatusCode.Should().BeTrue();

        var after = await ReadAsync<IReadOnlyList<CourseDocumentDto>>(
            await staff.GetAsync($"/api/courses/{courseId}/documents"));

        after.Should().BeEmpty();
    }

    [Fact]
    public async Task Loc_mon_hoc_chua_co_tai_lieu()
    {
        var staff = await StaffAsync();
        var marker = Unique();

        var emptyCourse = await NewCourseAsync(staff, $"MHE{marker}", $"Môn rỗng {marker}");
        var filledCourse = await NewCourseAsync(staff, $"MHF{marker}", $"Môn đủ {marker}");
        var (bibId, _) = await NewBibAsync(staff, $"Sách của môn đủ {marker}");

        await ReadAsync<int>(await staff.PostAsJsonAsync($"/api/courses/{filledCourse}/documents",
            new { bibIds = new[] { bibId }, relationType = "MainTextbook" }));

        var withoutDocuments = await ReadAsync<PagedResult<CourseRowDto>>(
            await staff.GetAsync($"/api/courses?keyword={marker}&withoutDocuments=true"));

        withoutDocuments.Items.Should().ContainSingle()
            .Which.Id.Should().Be(emptyCourse);
    }

    // -----------------------------------------------------------------------------------------
    // Nhập từ Excel
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Nhap_danh_muc_tai_lieu_mon_hoc_tu_Excel()
    {
        var staff = await StaffAsync();
        var marker = Unique();

        var courseCode = $"MHX{marker}";
        var courseId = await NewCourseAsync(staff, courseCode, $"Môn nhập Excel {marker}");
        var (_, isbn) = await NewBibAsync(staff, $"Sách nhập Excel {marker}");

        var file = BuildSheet(new[]
        {
            (courseCode, isbn, "Giáo trình chính", "Đọc chương 1–4"),
            (courseCode, "khong-ton-tai", "Tham khảo", string.Empty),
            ("MON-KHONG-CO", isbn, "Tham khảo", string.Empty)
        });

        // Bước một: kiểm tra thử, chưa ghi gì.
        var dryRun = await UploadAsync(staff, file, dryRun: true);

        dryRun.TotalRows.Should().Be(3);
        dryRun.SuccessRows.Should().Be(1);
        dryRun.FailedRows.Should().Be(2);
        dryRun.Rows.Should().Contain(row =>
            row.Message != null && row.Message.Contains("Không tìm thấy tài liệu"));
        dryRun.Rows.Should().Contain(row =>
            row.Message != null && row.Message.Contains("Không có môn học"));

        var beforeImport = await ReadAsync<IReadOnlyList<CourseDocumentDto>>(
            await staff.GetAsync($"/api/courses/{courseId}/documents"));

        beforeImport.Should().BeEmpty("bước kiểm tra thử không được ghi gì vào kho");

        // Bước hai: nhập thật. Dòng hỏng không làm hỏng dòng lành.
        var real = await UploadAsync(staff, file, dryRun: false);

        real.SuccessRows.Should().Be(1);

        var afterImport = await ReadAsync<IReadOnlyList<CourseDocumentDto>>(
            await staff.GetAsync($"/api/courses/{courseId}/documents"));

        afterImport.Should().ContainSingle();
        afterImport[0].RelationLabel.Should().Be("Giáo trình chính");
        afterImport[0].Note.Should().Be("Đọc chương 1–4");
    }

    [Fact]
    public async Task Tep_mau_Excel_tai_ve_duoc_va_doc_lai_duoc()
    {
        var staff = await StaffAsync();
        var response = await staff.GetAsync("/api/courses/documents/import/template");

        response.IsSuccessStatusCode.Should().BeTrue();

        using var stream = new MemoryStream(await response.Content.ReadAsByteArrayAsync());
        using var workbook = new XLWorkbook(stream);

        var sheet = workbook.Worksheet(CourseDocumentImportColumns.SheetName);
        var headers = sheet.Row(1).CellsUsed().Select(cell => cell.GetString()).ToList();

        headers.Should().Contain(CourseDocumentImportColumns.CourseCode);
        headers.Should().Contain(CourseDocumentImportColumns.BibKey);
    }

    // -----------------------------------------------------------------------------------------
    // Báo cáo và trang tra cứu
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Bao_cao_dem_dung_mon_chua_co_tai_lieu_va_ty_le_dap_ung()
    {
        var staff = await StaffAsync();
        var marker = Unique();

        var majorId = await NewMajorAsync(staff, $"NGB{marker}", $"Ngành báo cáo {marker}");
        var covered = await NewCourseAsync(staff, $"MHC{marker}", $"Môn có sách {marker}");
        var empty = await NewCourseAsync(staff, $"MHK{marker}", $"Môn không sách {marker}");

        foreach (var courseId in new[] { covered, empty })
        {
            (await staff.PutAsJsonAsync($"/api/courses/{courseId}/majors",
                new { majorIds = new[] { majorId } })).IsSuccessStatusCode.Should().BeTrue();
        }

        var (bibId, _) = await NewBibAsync(staff, $"Sách cho báo cáo {marker}");

        await ReadAsync<int>(await staff.PostAsJsonAsync($"/api/courses/{covered}/documents",
            new { bibIds = new[] { bibId }, relationType = "MainTextbook" }));

        var report = await ReadAsync<CourseReportDto>(
            await staff.GetAsync($"/api/courses/reports?majorId={majorId}"));

        report.TotalCourses.Should().Be(2);
        report.CoveredCourses.Should().Be(1);
        report.WithoutDocuments.Should().ContainSingle().Which.Code.Should().Be($"MHK{marker}");

        var coverage = report.Coverage.Should().ContainSingle().Subject;

        coverage.CourseCount.Should().Be(2);
        coverage.CoveredCourseCount.Should().Be(1);
        coverage.CoveragePercent.Should().Be(50);
    }

    [Fact]
    public async Task Bao_cao_chi_ra_tai_lieu_dung_chung_nhieu_mon()
    {
        var staff = await StaffAsync();
        var marker = Unique();

        var first = await NewCourseAsync(staff, $"MHS1{marker}", $"Môn chung một {marker}");
        var second = await NewCourseAsync(staff, $"MHS2{marker}", $"Môn chung hai {marker}");
        var (bibId, _) = await NewBibAsync(staff, $"Sách dùng chung {marker}");

        foreach (var courseId in new[] { first, second })
        {
            await ReadAsync<int>(await staff.PostAsJsonAsync($"/api/courses/{courseId}/documents",
                new { bibIds = new[] { bibId }, relationType = "RequiredReference" }));
        }

        var report = await ReadAsync<CourseReportDto>(
            await staff.GetAsync("/api/courses/reports?top=200"));

        var shared = report.SharedDocuments.FirstOrDefault(row => row.BibId == bibId);

        shared.Should().NotBeNull();
        shared!.CourseCount.Should().Be(2);
        shared.Courses.Should().Contain("Môn chung một");
    }

    [Fact]
    public async Task Xuat_bao_cao_ra_Excel_va_PDF()
    {
        var staff = await StaffAsync();

        var excel = await staff.GetAsync("/api/courses/reports/export?format=excel");
        var pdf = await staff.GetAsync("/api/courses/reports/export?format=pdf");

        excel.IsSuccessStatusCode.Should().BeTrue();
        pdf.IsSuccessStatusCode.Should().BeTrue();

        (await excel.Content.ReadAsByteArrayAsync()).Should().StartWith(new byte[] { 0x50, 0x4B });
        (await pdf.Content.ReadAsByteArrayAsync()).Should().StartWith("%PDF"u8.ToArray());
    }

    [Fact]
    public async Task Ban_doc_duyet_theo_nganh_roi_mon_hoc_thay_dung_tai_lieu()
    {
        var staff = await StaffAsync();
        var marker = Unique();

        var majorId = await NewMajorAsync(staff, $"NGO{marker}", $"Ngành tra cứu {marker}");
        var courseId = await NewCourseAsync(staff, $"MHO{marker}", $"Môn tra cứu {marker}");

        (await staff.PutAsJsonAsync($"/api/courses/{courseId}/majors",
            new { majorIds = new[] { majorId } })).IsSuccessStatusCode.Should().BeTrue();

        var (bibId, _) = await NewBibAsync(staff, $"Giáo trình tra cứu {marker}");

        // Trang tra cứu chỉ hiện biểu ghi đã xuất bản.
        var detail = await ReadAsync<BibDetailDto>(
            await staff.GetAsync($"/api/cataloging/bibs/{bibId}"));

        await ReadAsync<SaveBibResultDto>(await staff.PutAsJsonAsync(
            $"/api/cataloging/bibs/{bibId}",
            new { marcJson = detail.MarcJson, status = "Published", changeNote = "Xuất bản" }));

        await ReadAsync<int>(await staff.PostAsJsonAsync($"/api/courses/{courseId}/documents",
            new { bibIds = new[] { bibId }, relationType = "MainTextbook" }));

        var anonymous = _factory.CreateClient();

        var majors = await ReadAsync<IReadOnlyList<OpacBrowseEntryDto>>(
            await anonymous.GetAsync("/api/browse/majors"));

        majors.Should().Contain(entry => entry.Id == majorId);

        var courses = await ReadAsync<IReadOnlyList<OpacBrowseEntryDto>>(
            await anonymous.GetAsync($"/api/browse/courses?majorId={majorId}"));

        courses.Should().Contain(entry => entry.Id == courseId);

        var documents = await ReadAsync<PagedResult<OpacCourseDocumentDto>>(
            await anonymous.GetAsync(
                $"/api/browse/majors/{majorId}/courses/{courseId}/documents"));

        documents.Items.Should().ContainSingle();
        documents.Items[0].RelationLabel.Should().Be("Giáo trình chính");
        documents.Items[0].Bib.Id.Should().Be(bibId);
    }

    [Fact]
    public async Task Phan_quyen_tai_lieu_mon_hoc()
    {
        var staff = await StaffAsync();

        var types = await ReadAsync<PagedResult<CatalogItemDto>>(
            await staff.GetAsync("/api/catalogs/reader-types/items?pageSize=50"));

        var readerId = await ReadAsync<Guid>(await staff.PostAsJsonAsync("/api/readers", new
        {
            fullName = "Nguyễn Văn Không Quyền",
            studentCode = $"SV{Unique()}",
            readerTypeId = types.Items.First(item => item.Code == "SV").Id
        }));

        var reader = await ReadAsync<ReaderDetailDto>(await staff.GetAsync($"/api/readers/{readerId}"));

        const string password = "BanDoc@2026";

        (await staff.PostAsJsonAsync(
                $"/api/readers/{readerId}/reset-password", new { newPassword = password }))
            .IsSuccessStatusCode.Should().BeTrue();

        var client = _factory.CreateClient();

        var login = await ReadAsync<Application.Features.Auth.AuthResultDto>(
            await client.PostAsJsonAsync(
                "/api/reader/auth/login", new { cardNumber = reader.CardNumber, password }));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.AccessToken);

        (await client.GetAsync("/api/courses")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/courses/reports")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // -----------------------------------------------------------------------------------------

    private static byte[] BuildSheet(
        IReadOnlyList<(string CourseCode, string BibKey, string Relation, string Note)> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet(CourseDocumentImportColumns.SheetName);

        sheet.Cell(1, 1).Value = CourseDocumentImportColumns.CourseCode;
        sheet.Cell(1, 2).Value = CourseDocumentImportColumns.BibKey;
        sheet.Cell(1, 3).Value = CourseDocumentImportColumns.RelationType;
        sheet.Cell(1, 4).Value = CourseDocumentImportColumns.Note;

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];

            sheet.Cell(index + 2, 1).Value = row.CourseCode;
            sheet.Cell(index + 2, 2).Value = row.BibKey;
            sheet.Cell(index + 2, 3).Value = row.Relation;
            sheet.Cell(index + 2, 4).Value = row.Note;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    private static async Task<CourseDocumentImportResultDto> UploadAsync(
        HttpClient staff, byte[] file, bool dryRun)
    {
        using var content = new MultipartFormDataContent();
        var bytes = new ByteArrayContent(file);

        bytes.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        content.Add(bytes, "file", "tai-lieu-mon-hoc.xlsx");

        return await ReadAsync<CourseDocumentImportResultDto>(
            await staff.PostAsync($"/api/courses/documents/import?dryRun={dryRun}", content));
    }
}
