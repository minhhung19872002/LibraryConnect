using System.Globalization;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Marc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Persistence.Seeding;

public partial class DatabaseSeeder
{
    /// <summary>Tắt bộ dữ liệu minh họa khi bàn giao cho thư viện thật: đặt LC_SEED_DEMO=false.</summary>
    public const string DemoSwitch = "SEED_DEMO";

    /// <summary>Mật khẩu của mọi tài khoản bạn đọc minh họa — chỉ dùng để demo và kiểm thử.</summary>
    public const string DemoReaderPassword = "BanDoc@2025";

    /// <summary>
    /// Bộ dữ liệu minh họa: 200 biểu ghi, 500 ĐKCB, 50 bạn đọc, 100 lượt mượn trả (mục 8).
    ///
    /// Hệ thống mới cài mà kho rỗng thì không demo được gì: tra cứu ra danh sách trắng, báo cáo toàn
    /// số 0, màn hình ghi mượn không có mã vạch nào để quét. Bộ dữ liệu này lấp đúng chỗ đó.
    ///
    /// Ba nguyên tắc khi sinh:
    /// - Đi qua đúng những đường mà cán bộ đi: biểu ghi qua <see cref="IBibRecordWriter"/> nên có đủ
    ///   MARC, cột phẳng và liên kết tác giả; ĐKCB qua <c>ItemCreator</c> nên số mã vạch, số ĐKCB và
    ///   ký hiệu xếp giá theo đúng quy tắc đang khai; hạn trả và tiền phạt tính bằng chính
    ///   <see cref="CirculationRules"/> mà quầy dùng.
    /// - Sinh cố định, không ngẫu nhiên: hai lần cài trên hai máy phải ra cùng một bộ dữ liệu, để
    ///   kịch bản nghiệm thu viết một lần dùng được ở mọi nơi.
    /// - Chỉ chạy khi kho còn trống. Thư viện đã nhập dữ liệu thật rồi thì một lần nâng cấp không
    ///   được phép trộn 200 cuốn sách bịa vào giữa kho của họ.
    /// </summary>
    private async Task SeedDemoDataAsync(CancellationToken ct)
    {
        if (!_configuration.GetValue(DemoSwitch, true))
        {
            _logger.LogInformation("Bỏ qua dữ liệu minh họa theo cấu hình {Switch}", DemoSwitch);
            return;
        }

        if (await _db.BibRecords.AnyAsync(ct))
        {
            return;
        }

        await EnsureFundingSourcesAsync(ct);
        await EnsureSuppliersAsync(ct);

        var bibs = await SeedDemoBibsAsync(ct);
        var items = await SeedDemoItemsAsync(bibs, ct);
        var readers = await SeedDemoReadersAsync(ct);

        await SeedDemoCirculationAsync(readers, items, ct);
        await SeedDemoCourseDocumentsAsync(bibs, ct);

        // Đếm lại ĐKCB từ kho: danh sách trả về ở trên chỉ gồm bản sẵn sàng cho mượn, còn con số
        // cần báo là tổng số bản đã tạo.
        var itemCount = await _db.Items.CountAsync(ct);

        _logger.LogInformation(
            "Đã nạp dữ liệu minh họa: {Bibs} biểu ghi, {Items} ĐKCB, {Readers} bạn đọc",
            bibs.Count, itemCount, readers.Count);
    }

    // ---------------------------------------------------------------------------------------------
    // Kho tư liệu để dựng nhan đề
    // ---------------------------------------------------------------------------------------------

    /// <summary>Một môn khoa học: tên tiếng Việt, tên tiếng Anh, ký hiệu DDC và đề mục chủ đề.</summary>
    private sealed record DemoSubject(string Name, string English, string Ddc, string Subject);

    /// <summary>
    /// 25 môn phủ các lớp DDC mà thư viện đại học đa ngành hay có, để mọi bộ lọc trên trang tra cứu
    /// đều có dữ liệu: tin học, toán, vật lý, môi trường, kinh tế, luật.
    /// </summary>
    private static readonly DemoSubject[] DemoSubjects =
    {
        new("cơ sở dữ liệu", "Database Systems", "005.74", "Cơ sở dữ liệu"),
        new("lập trình hướng đối tượng", "Object-Oriented Programming", "005.117", "Lập trình máy tính"),
        new("mạng máy tính", "Computer Networks", "004.6", "Mạng máy tính"),
        new("trí tuệ nhân tạo", "Artificial Intelligence", "006.3", "Trí tuệ nhân tạo"),
        new("kỹ thuật phần mềm", "Software Engineering", "005.1", "Công nghệ phần mềm"),
        new("cấu trúc dữ liệu và giải thuật", "Data Structures and Algorithms", "005.73", "Giải thuật"),
        new("hệ điều hành", "Operating Systems", "005.43", "Hệ điều hành"),
        new("an toàn thông tin", "Information Security", "005.8", "An toàn thông tin"),
        new("tin học đại cương", "Introduction to Informatics", "004", "Tin học"),
        new("xác suất thống kê", "Probability and Statistics", "519.2", "Xác suất thống kê"),
        new("giải tích", "Mathematical Analysis", "515", "Giải tích toán học"),
        new("đại số tuyến tính", "Linear Algebra", "512.5", "Đại số"),
        new("vật lý đại cương", "General Physics", "530", "Vật lý"),
        new("hóa học môi trường", "Environmental Chemistry", "628", "Hóa học môi trường"),
        new("quản lý tài nguyên nước", "Water Resources Management", "333.91", "Tài nguyên nước"),
        new("biến đổi khí hậu", "Climate Change", "363.738", "Biến đổi khí hậu"),
        new("trắc địa", "Geodesy", "526.9", "Trắc địa"),
        new("hệ thống thông tin địa lý", "Geographic Information Systems", "910.285", "Hệ thống thông tin địa lý"),
        new("quản trị học", "Principles of Management", "658", "Quản trị kinh doanh"),
        new("marketing căn bản", "Principles of Marketing", "658.8", "Marketing"),
        new("kinh tế vi mô", "Microeconomics", "338.5", "Kinh tế học"),
        new("kế toán tài chính", "Financial Accounting", "657", "Kế toán"),
        new("quản trị nhân lực", "Human Resource Management", "658.3", "Quản trị nhân lực"),
        new("luật dân sự", "Civil Law", "346.597", "Luật dân sự"),
        new("luật sở hữu trí tuệ", "Intellectual Property Law", "346.048", "Sở hữu trí tuệ")
    };

    private static readonly string[] DemoAuthors =
    {
        "Nguyễn Văn An", "Trần Thị Bình", "Lê Hoàng Cường", "Phạm Thị Dung", "Hoàng Minh Đức",
        "Vũ Thị Hà", "Đặng Quốc Hùng", "Bùi Thị Lan", "Đỗ Văn Mạnh", "Ngô Thị Nga",
        "Dương Thanh Phong", "Lý Thị Quyên", "Trịnh Văn Sơn", "Mai Thị Thu", "Phan Anh Tuấn",
        "Chu Thị Vân", "Tạ Quang Vinh", "Đinh Thị Xuân", "Hồ Sỹ Yên", "Lương Bá Khôi"
    };

    private static readonly string[] DemoForeignAuthors =
    {
        "Silberschatz, Abraham", "Tanenbaum, Andrew S.", "Russell, Stuart", "Sommerville, Ian",
        "Cormen, Thomas H.", "Stallings, William", "Kotler, Philip", "Mankiw, N. Gregory"
    };

    private static readonly string[] DemoPublishers =
    {
        "Nhà xuất bản Giáo dục", "Nhà xuất bản Khoa học và Kỹ thuật",
        "Nhà xuất bản Đại học Quốc gia Hà Nội", "Nhà xuất bản Xây dựng", "Nhà xuất bản Thống kê"
    };

    private static readonly string[] DemoForeignPublishers = { "Pearson", "Springer", "Elsevier", "Wiley" };

    private static readonly string[] DemoPlaces = { "Hà Nội", "TP. Hồ Chí Minh", "Đà Nẵng" };

    // ---------------------------------------------------------------------------------------------
    // Biểu ghi thư mục
    // ---------------------------------------------------------------------------------------------

    /// <summary>Một biểu ghi minh họa đã lưu, giữ lại những gì bước sau cần.</summary>
    private sealed record DemoBib(BibRecord Record, string Ddc, int Variant);

    private async Task<IReadOnlyList<DemoBib>> SeedDemoBibsAsync(CancellationToken ct)
    {
        var documentTypes = await _db.DocumentTypes
            .ToDictionaryAsync(type => type.Code, type => type.Id, ct);

        var carrierTypes = await _db.CarrierTypes
            .ToDictionaryAsync(type => type.Code, type => type.Id, ct);

        var collections = await _db.Collections.OrderBy(entity => entity.SortOrder).ToListAsync(ct);

        var results = new List<DemoBib>();
        var today = _clock.Today;

        for (var subjectIndex = 0; subjectIndex < DemoSubjects.Length; subjectIndex++)
        {
            var subject = DemoSubjects[subjectIndex];

            for (var variant = 0; variant < 8; variant++)
            {
                var isForeign = variant == 3;
                var year = 2016 + ((subjectIndex + variant) % 10);

                var title = variant switch
                {
                    0 => $"Giáo trình {subject.Name}",
                    1 => $"Nhập môn {subject.Name}",
                    2 => $"Bài tập {subject.Name}",
                    3 => $"{subject.English}: theory and practice",
                    4 => $"{Capitalise(subject.Name)} — lý thuyết và bài tập",
                    5 => $"Nghiên cứu ứng dụng {subject.Name} trong đào tạo đại học",
                    6 => $"Nghiên cứu phát triển phương pháp giảng dạy {subject.Name}",
                    _ => $"Ứng dụng {subject.Name} phục vụ công tác đào tạo"
                };

                var documentTypeCode = variant switch
                {
                    0 => "GIAOTRINH",
                    5 => "LUANVAN",
                    6 => "LUANAN",
                    7 => "DETAI",
                    _ => "SACH"
                };

                var author = isForeign
                    ? DemoForeignAuthors[(subjectIndex + variant) % DemoForeignAuthors.Length]
                    : DemoAuthors[(subjectIndex * 3 + variant) % DemoAuthors.Length];

                var publisher = isForeign
                    ? DemoForeignPublishers[(subjectIndex + variant) % DemoForeignPublishers.Length]
                    : DemoPublishers[(subjectIndex + variant) % DemoPublishers.Length];

                var marc = BuildDemoMarc(
                    subject, title, author, publisher, year, isForeign, variant, subjectIndex);

                await Marc008Builder.EnsureAsync(marc, _parameters, today, year, ct);

                if (isForeign)
                {
                    // 008 vị trí 35–37 là ngôn ngữ của chính tài liệu, không phải ngôn ngữ mặc định
                    // của thư viện; để nguyên "vie" thì bộ lọc ngôn ngữ trên trang tra cứu nói sai.
                    var fixedField = marc.GetControlField("008")!.ToCharArray();
                    "eng".CopyTo(0, fixedField, 35, 3);
                    marc.SetControlField("008", new string(fixedField));
                }

                var record = new BibRecord
                {
                    Id = Guid.NewGuid(),
                    Status = RecordStatus.Published,
                    Source = BibSource.Manual,
                    DocumentTypeId = documentTypes.TryGetValue(documentTypeCode, out var typeId)
                        ? typeId
                        : null,
                    CarrierTypeId = carrierTypes.TryGetValue("GIAY", out var carrierId) ? carrierId : null,
                    CreatedAt = _clock.Now
                };

                await _bibWriter.PrepareAsync(record, marc, ct);
                _db.BibRecords.Add(record);
                await _bibWriter.ApplyAsync(record, marc, isNew: true, changeNote: null, ct);

                if (collections.Count > 0)
                {
                    // Bộ sưu tập là quan hệ nhiều-nhiều: một cuốn nằm được trong cả "Giáo trình" lẫn
                    // "Tài liệu nội sinh", nên gắn qua bảng liên kết chứ không phải một cột.
                    _db.BibCollections.Add(new BibCollection
                    {
                        BibId = record.Id,
                        CollectionId = collections[(subjectIndex + variant) % collections.Count].Id
                    });
                }

                // Lưu ngay từng biểu ghi, đúng như đường nhập ISO 2709 vẫn làm: bộ liên kết tác giả
                // và chủ đề dò trùng bằng câu truy vấn xuống cơ sở dữ liệu, nên hai biểu ghi cùng
                // một ký hiệu phân loại mà chưa lưu sẽ cùng tạo ra một dòng phân loại mới và va vào
                // ràng buộc duy nhất.
                await _db.SaveChangesAsync(ct);

                results.Add(new DemoBib(record, subject.Ddc, variant));
            }
        }

        return results;
    }

    private MarcRecord BuildDemoMarc(
        DemoSubject subject,
        string title,
        string author,
        string publisher,
        int year,
        bool isForeign,
        int variant,
        int subjectIndex)
    {
        var marc = new MarcRecord();

        marc.AddField("020").AddSubfield('a', DemoIsbn(subjectIndex, variant));

        marc.AddField("040")
            .AddSubfield('a', "VN-HCM-LC")
            .AddSubfield('b', "vie")
            .AddSubfield('c', "VN-HCM-LC");

        marc.AddField("041").AddSubfield('a', isForeign ? "eng" : "vie");
        marc.AddField("044").AddSubfield('a', isForeign ? "xxu" : "vm");

        marc.AddField("082", '0', '4')
            .AddSubfield('a', subject.Ddc)
            .AddSubfield('2', "23");

        // Chỉ thị 1 = 1: có mục từ tên riêng; chỉ thị 2 = 0: nhan đề không có mạo từ đầu.
        marc.AddField("100", '1').AddSubfield('a', author).AddSubfield('e', "tác giả");

        marc.AddField("245", '1', '0')
            .AddSubfield('a', title + " /")
            .AddSubfield('c', author);

        if (variant == 4)
        {
            marc.AddField("250").AddSubfield('a', "Tái bản lần thứ 2, có sửa chữa và bổ sung");
        }

        marc.AddField("260")
            .AddSubfield('a', (isForeign ? "London" : DemoPlaces[subjectIndex % DemoPlaces.Length]) + " :")
            .AddSubfield('b', publisher + ",")
            .AddSubfield('c', year.ToString(CultureInfo.InvariantCulture));

        marc.AddField("300")
            .AddSubfield('a', $"{180 + (subjectIndex * 7 + variant * 13) % 240} tr. :")
            .AddSubfield('b', "hình vẽ, bảng ;")
            .AddSubfield('c', "24 cm");

        marc.AddField("336").AddSubfield('a', "văn bản").AddSubfield('b', "txt").AddSubfield('2', "rdacontent");
        marc.AddField("337").AddSubfield('a', "không cần thiết bị").AddSubfield('b', "n").AddSubfield('2', "rdamedia");
        marc.AddField("338").AddSubfield('a', "quyển").AddSubfield('b', "nc").AddSubfield('2', "rdacarrier");

        marc.AddField("520").AddSubfield('a', DemoAbstract(subject, variant));

        marc.AddField("650", ' ', '4').AddSubfield('a', subject.Subject);
        marc.AddField("653").AddSubfield('a', subject.Name);

        if (variant is 2 or 4)
        {
            marc.AddField("700", '1')
                .AddSubfield('a', DemoAuthors[(subjectIndex + variant + 7) % DemoAuthors.Length])
                .AddSubfield('e', "biên soạn");
        }

        if (variant is 5 or 6)
        {
            // Luận văn và luận án luôn có phụ chú về cấp học và nơi bảo vệ; thiếu nó thì biểu ghi
            // nhìn không ra là luận văn.
            marc.AddField("502").AddSubfield('a',
                variant == 5
                    ? "Luận văn Thạc sĩ — Trường Đại học, " + year
                    : "Luận án Tiến sĩ — Trường Đại học, " + year);
        }

        return marc;
    }

    private static string DemoAbstract(DemoSubject subject, int variant) => variant switch
    {
        0 => $"Giáo trình trình bày hệ thống các khái niệm cơ bản của {subject.Name}, kèm ví dụ và "
             + "bài tập cuối mỗi chương, dùng cho sinh viên năm thứ hai và thứ ba.",
        2 => $"Tuyển chọn bài tập {subject.Name} theo từng chủ đề, có lời giải chi tiết và hướng dẫn "
             + "cách trình bày bài thi.",
        5 => $"Luận văn khảo sát thực trạng giảng dạy {subject.Name} tại một số trường đại học và đề "
             + "xuất giải pháp cải tiến chương trình.",
        6 => $"Luận án đề xuất và kiểm chứng phương pháp giảng dạy {subject.Name} theo hướng phát "
             + "triển năng lực người học.",
        7 => $"Báo cáo kết quả đề tài nghiên cứu về ứng dụng {subject.Name} trong hoạt động đào tạo "
             + "và nghiên cứu của nhà trường.",
        _ => $"Tài liệu giới thiệu các nội dung cốt lõi của {subject.Name} và hướng dẫn vận dụng vào "
             + "bài toán thực tế."
    };

    /// <summary>
    /// Sinh mã ISBN 13 chữ số có số kiểm tra đúng.
    ///
    /// Số kiểm tra sai thì chức năng tra trùng theo ISBN và phần nhập từ Excel đều báo lỗi ngay trên
    /// chính dữ liệu mẫu của hệ thống.
    /// </summary>
    private static string DemoIsbn(int subjectIndex, int variant)
    {
        var body = "978604" + (100000 + subjectIndex * 8 + variant).ToString(CultureInfo.InvariantCulture);
        var sum = 0;

        for (var index = 0; index < 12; index++)
        {
            sum += (body[index] - '0') * (index % 2 == 0 ? 1 : 3);
        }

        var check = (10 - (sum % 10)) % 10;
        return body + check.ToString(CultureInfo.InvariantCulture);
    }

    private static string Capitalise(string value) =>
        string.IsNullOrEmpty(value) ? value : char.ToUpper(value[0], new CultureInfo("vi-VN")) + value[1..];
}
