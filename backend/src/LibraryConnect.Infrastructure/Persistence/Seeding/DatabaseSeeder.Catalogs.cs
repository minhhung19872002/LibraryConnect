using LibraryConnect.Domain.Entities.Cat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Persistence.Seeding;

public partial class DatabaseSeeder
{
    /// <summary>
    /// Nạp các danh mục chuẩn quốc tế và danh mục nghiệp vụ tối thiểu (mục 8 của đặc tả).
    ///
    /// Only standard reference data goes in here — language codes, country codes, the DDC summary,
    /// carrier types. Anything specific to a customer (their faculties, their suppliers) is left for
    /// the library to enter or import, because guessing it would put wrong data in front of them on
    /// day one.
    /// </summary>
    private async Task SeedCatalogsAsync(CancellationToken ct)
    {
        await SeedAsync(_db.Languages, LanguageSeeds, ct, "ngôn ngữ");
        await SeedAsync(_db.Countries, CountrySeeds, ct, "nước xuất bản");
        await SeedAsync(_db.DocumentTypes, DocumentTypeSeeds, ct, "dạng tài liệu");
        await SeedAsync(_db.CarrierTypes, CarrierTypeSeeds, ct, "vật mang tin");
        await SeedAsync(_db.ReaderTypes, ReaderTypeSeeds, ct, "loại bạn đọc");
        await SeedAsync(_db.ViolationTypes, ViolationTypeSeeds, ct, "loại vi phạm");
        await SeedClassificationsAsync(ct);
    }

    /// <summary>
    /// Inserts the values that are missing, keyed by code. Existing rows are left untouched so an
    /// upgrade never overwrites a name the library has adjusted.
    /// </summary>
    private async Task SeedAsync<TEntity>(
        DbSet<TEntity> set, IReadOnlyList<TEntity> seeds, CancellationToken ct, string label)
        where TEntity : Domain.Common.CatalogEntity
    {
        var existing = await set.Select(entity => entity.Code).ToListAsync(ct);
        var known = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = seeds.Where(seed => !known.Contains(seed.Code)).ToList();

        if (missing.Count == 0)
        {
            return;
        }

        foreach (var entity in missing)
        {
            entity.CreatedAt = _clock.Now;
        }

        set.AddRange(missing);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Đã bổ sung {Count} giá trị danh mục {Label}", missing.Count, label);
    }

    // ---------------- Ngôn ngữ (ISO 639-2/B) ----------------

    private static readonly IReadOnlyList<Language> LanguageSeeds = new List<Language>
    {
        Language("vie", "vi", "Tiếng Việt", "Vietnamese", 1),
        Language("eng", "en", "Tiếng Anh", "English", 2),
        Language("fre", "fr", "Tiếng Pháp", "French", 3),
        Language("rus", "ru", "Tiếng Nga", "Russian", 4),
        Language("chi", "zh", "Tiếng Trung Quốc", "Chinese", 5),
        Language("jpn", "ja", "Tiếng Nhật", "Japanese", 6),
        Language("kor", "ko", "Tiếng Hàn", "Korean", 7),
        Language("ger", "de", "Tiếng Đức", "German", 8),
        Language("spa", "es", "Tiếng Tây Ban Nha", "Spanish", 9),
        Language("ita", "it", "Tiếng Ý", "Italian", 10),
        Language("por", "pt", "Tiếng Bồ Đào Nha", "Portuguese", 11),
        Language("tha", "th", "Tiếng Thái", "Thai", 12),
        Language("lao", "lo", "Tiếng Lào", "Lao", 13),
        Language("khm", "km", "Tiếng Khmer", "Khmer", 14),
        Language("ind", "id", "Tiếng Indonesia", "Indonesian", 15),
        Language("may", "ms", "Tiếng Mã Lai", "Malay", 16),
        Language("ara", "ar", "Tiếng Ả Rập", "Arabic", 17),
        Language("hin", "hi", "Tiếng Hindi", "Hindi", 18),
        Language("lat", "la", "Tiếng Latinh", "Latin", 19),
        Language("mul", null, "Nhiều ngôn ngữ", "Multiple languages", 20),
        Language("und", null, "Không xác định", "Undetermined", 21)
    };

    private static Language Language(string code, string? iso1, string name, string nameEn, int order) => new()
    {
        Code = code,
        Iso6391 = iso1,
        Name = name,
        NameEn = nameEn,
        SortOrder = order,
        IsActive = true
    };

    // ---------------- Nước xuất bản (MARC Country Codes) ----------------

    private static readonly IReadOnlyList<Country> CountrySeeds = new List<Country>
    {
        Country("vm", "Việt Nam", "Vietnam", 1),
        Country("cc", "Trung Quốc", "China", 2),
        Country("ja", "Nhật Bản", "Japan", 3),
        Country("ko", "Hàn Quốc", "Korea (South)", 4),
        Country("th", "Thái Lan", "Thailand", 5),
        Country("ls", "Lào", "Laos", 6),
        Country("cb", "Campuchia", "Cambodia", 7),
        Country("io", "Indonesia", "Indonesia", 8),
        Country("my", "Malaysia", "Malaysia", 9),
        Country("si", "Singapore", "Singapore", 10),
        Country("ph", "Philippines", "Philippines", 11),
        Country("xxu", "Hoa Kỳ", "United States", 12),
        Country("enk", "Anh", "England", 13),
        Country("fr", "Pháp", "France", 14),
        Country("gw", "Đức", "Germany", 15),
        Country("ru", "Liên bang Nga", "Russia", 16),
        Country("it", "Ý", "Italy", 17),
        Country("sp", "Tây Ban Nha", "Spain", 18),
        Country("ne", "Hà Lan", "Netherlands", 19),
        Country("sz", "Thụy Sĩ", "Switzerland", 20),
        Country("at", "Úc", "Australia", 21),
        Country("onc", "Canada", "Canada", 22),
        Country("ii", "Ấn Độ", "India", 23),
        Country("xx", "Không xác định", "Unknown", 99)
    };

    private static Country Country(string code, string name, string nameEn, int order) => new()
    {
        Code = code,
        Name = name,
        NameEn = nameEn,
        SortOrder = order,
        IsActive = true
    };

    // ---------------- Dạng tài liệu ----------------

    private static readonly IReadOnlyList<DocumentType> DocumentTypeSeeds = new List<DocumentType>
    {
        DocumentType("SACH", "Sách", "Book", "a", "m", false, 1),
        DocumentType("GIAOTRINH", "Giáo trình", "Textbook", "a", "m", false, 2),
        DocumentType("BAO", "Báo", "Newspaper", "a", "s", true, 3),
        DocumentType("TAPCHI", "Tạp chí", "Journal", "a", "s", true, 4),
        DocumentType("LUANVAN", "Luận văn", "Master thesis", "a", "m", false, 5),
        DocumentType("LUANAN", "Luận án", "Doctoral dissertation", "a", "m", false, 6),
        DocumentType("DETAI", "Đề tài nghiên cứu khoa học", "Research report", "a", "m", false, 7),
        DocumentType("KYYEU", "Kỷ yếu hội thảo", "Conference proceedings", "a", "m", false, 8),
        DocumentType("BAITRICH", "Bài trích", "Analytic article", "a", "a", false, 9),
        DocumentType("TUDIEN", "Từ điển", "Dictionary", "a", "m", false, 10),
        DocumentType("BANDO", "Bản đồ", "Map", "e", "m", false, 11),
        DocumentType("BAIGIANG", "Bài giảng điện tử", "Courseware", "m", "m", false, 12),
        DocumentType("AUDIO", "Tài liệu nghe", "Sound recording", "i", "m", false, 13),
        DocumentType("VIDEO", "Tài liệu nhìn", "Video recording", "g", "m", false, 14)
    };

    private static DocumentType DocumentType(
        string code, string name, string nameEn, string typeOfRecord, string bibLevel, bool isSerial, int order) => new()
    {
        Code = code,
        Name = name,
        NameEn = nameEn,
        MarcTypeOfRecord = typeOfRecord,
        MarcBibLevel = bibLevel,
        IsSerial = isSerial,
        SortOrder = order,
        IsActive = true
    };

    // ---------------- Vật mang tin ----------------

    private static readonly IReadOnlyList<CarrierType> CarrierTypeSeeds = new List<CarrierType>
    {
        Carrier("GIAY", "Giấy", "Paper", 1),
        Carrier("CD", "Đĩa CD", "CD", 2),
        Carrier("DVD", "Đĩa DVD", "DVD", 3),
        Carrier("FILE", "Tệp số", "Digital file", 4),
        Carrier("VIPHIM", "Vi phim", "Microfilm", 5),
        Carrier("VIPHIEU", "Vi phiếu", "Microfiche", 6),
        Carrier("BANGTU", "Băng từ", "Magnetic tape", 7),
        Carrier("ONLINE", "Tài nguyên trực tuyến", "Online resource", 8)
    };

    private static CarrierType Carrier(string code, string name, string nameEn, int order) => new()
    {
        Code = code, Name = name, NameEn = nameEn, SortOrder = order, IsActive = true
    };

    // ---------------- Loại bạn đọc ----------------

    private static readonly IReadOnlyList<ReaderType> ReaderTypeSeeds = new List<ReaderType>
    {
        Reader("SV", "Sinh viên", "Undergraduate student", 54, 1),
        Reader("HV", "Học viên cao học", "Master student", 30, 2),
        Reader("NCS", "Nghiên cứu sinh", "PhD candidate", 48, 3),
        Reader("GV", "Giảng viên", "Lecturer", 60, 4),
        Reader("CBNV", "Cán bộ, nhân viên", "Staff", 60, 5),
        Reader("KHACH", "Bạn đọc khách", "Guest", 12, 6)
    };

    private static ReaderType Reader(string code, string name, string nameEn, int validMonths, int order) => new()
    {
        Code = code,
        Name = name,
        NameEn = nameEn,
        CardValidMonths = validMonths,
        SortOrder = order,
        IsActive = true
    };

    // ---------------- Loại vi phạm ----------------

    private static readonly IReadOnlyList<ViolationType> ViolationTypeSeeds = new List<ViolationType>
    {
        Violation("QUAHAN", "Trả tài liệu quá hạn", 1),
        Violation("MAT", "Làm mất tài liệu", 2),
        Violation("HONG", "Làm hỏng tài liệu", 3),
        Violation("VIETBAN", "Viết, vẽ, gạch xóa vào tài liệu", 4),
        Violation("ONAO", "Gây mất trật tự trong thư viện", 5),
        Violation("MUONHO", "Cho mượn thẻ, dùng thẻ của người khác", 6)
    };

    private static ViolationType Violation(string code, string name, int order) => new()
    {
        Code = code, Name = name, SortOrder = order, IsActive = true
    };

    // ---------------- Khung phân loại DDC ----------------

    /// <summary>
    /// Nạp bảng tóm tắt DDC: 10 lớp chính và 100 phân lớp.
    ///
    /// This is the published DDC summary, which is what a Vietnamese academic library classifies to
    /// in practice. The full three-digit section table (1000 entries) is deliberately not invented
    /// here: a library that needs it imports its own authoritative translation through
    /// Danh mục → Khung phân loại → Nhập từ Excel, and those rows then sit under these divisions.
    /// </summary>
    private async Task SeedClassificationsAsync(CancellationToken ct)
    {
        if (await _db.Classifications.AnyAsync(ct))
        {
            return;
        }

        var mainClasses = new (string Code, string Name)[]
        {
            ("000", "Tin học, thông tin và tác phẩm tổng quát"),
            ("100", "Triết học và tâm lý học"),
            ("200", "Tôn giáo"),
            ("300", "Khoa học xã hội"),
            ("400", "Ngôn ngữ"),
            ("500", "Khoa học tự nhiên và toán học"),
            ("600", "Công nghệ và khoa học ứng dụng"),
            ("700", "Nghệ thuật và giải trí"),
            ("800", "Văn học"),
            ("900", "Lịch sử và địa lý")
        };

        var divisions = new (string Code, string Name)[]
        {
            ("010", "Thư mục học"), ("020", "Thư viện học và khoa học thông tin"),
            ("030", "Bách khoa toàn thư tổng quát"), ("050", "Xuất bản phẩm nhiều kỳ tổng quát"),
            ("060", "Tổ chức và bảo tàng học"), ("070", "Báo chí, xuất bản"),
            ("080", "Tuyển tập tổng quát"), ("090", "Bản thảo và sách quý hiếm"),
            ("110", "Siêu hình học"), ("120", "Nhận thức luận"), ("130", "Hiện tượng huyền bí"),
            ("140", "Các trường phái triết học"), ("150", "Tâm lý học"), ("160", "Logic học"),
            ("170", "Đạo đức học"), ("180", "Triết học cổ đại, trung đại và phương Đông"),
            ("190", "Triết học phương Tây hiện đại"),
            ("210", "Triết học và lý thuyết tôn giáo"), ("220", "Kinh Thánh"),
            ("230", "Thần học Kitô giáo"), ("260", "Thần học xã hội Kitô giáo"),
            ("270", "Lịch sử Kitô giáo"), ("280", "Các giáo phái Kitô giáo"),
            ("290", "Các tôn giáo khác"),
            ("310", "Thống kê học"), ("320", "Khoa học chính trị"), ("330", "Kinh tế học"),
            ("340", "Luật học"), ("350", "Hành chính công và khoa học quân sự"),
            ("360", "Vấn đề và dịch vụ xã hội"), ("370", "Giáo dục học"),
            ("380", "Thương mại, truyền thông và giao thông"), ("390", "Phong tục, tập quán và văn hóa dân gian"),
            ("410", "Ngôn ngữ học"), ("420", "Tiếng Anh"), ("430", "Tiếng Đức"),
            ("440", "Tiếng Pháp"), ("450", "Tiếng Ý"), ("460", "Tiếng Tây Ban Nha và Bồ Đào Nha"),
            ("470", "Tiếng Latinh"), ("480", "Tiếng Hy Lạp"), ("490", "Các ngôn ngữ khác"),
            ("510", "Toán học"), ("520", "Thiên văn học"), ("530", "Vật lý học"),
            ("540", "Hóa học"), ("550", "Khoa học Trái Đất"), ("560", "Cổ sinh vật học"),
            ("570", "Sinh học"), ("580", "Thực vật học"), ("590", "Động vật học"),
            ("610", "Y học và sức khỏe"), ("620", "Kỹ thuật"), ("630", "Nông nghiệp"),
            ("640", "Kinh tế gia đình"), ("650", "Quản trị kinh doanh"),
            ("660", "Công nghệ hóa học"), ("670", "Sản xuất công nghiệp"),
            ("680", "Sản xuất sản phẩm chuyên dụng"), ("690", "Xây dựng"),
            ("710", "Quy hoạch đô thị và kiến trúc cảnh quan"), ("720", "Kiến trúc"),
            ("730", "Điêu khắc, gốm sứ và kim hoàn"), ("740", "Đồ họa và trang trí"),
            ("750", "Hội họa"), ("760", "Đồ họa in ấn"), ("770", "Nhiếp ảnh"),
            ("780", "Âm nhạc"), ("790", "Thể thao, trò chơi và giải trí"),
            ("810", "Văn học Mỹ"), ("820", "Văn học Anh"), ("830", "Văn học Đức"),
            ("840", "Văn học Pháp"), ("850", "Văn học Ý"), ("860", "Văn học Tây Ban Nha"),
            ("870", "Văn học Latinh"), ("880", "Văn học Hy Lạp"), ("890", "Văn học các nước khác"),
            ("895", "Văn học châu Á"),
            ("910", "Địa lý và du hành"), ("920", "Tiểu sử và phả hệ"),
            ("930", "Lịch sử thế giới cổ đại"), ("940", "Lịch sử châu Âu"),
            ("950", "Lịch sử châu Á"), ("959", "Lịch sử Việt Nam và Đông Nam Á"),
            ("960", "Lịch sử châu Phi"), ("970", "Lịch sử Bắc Mỹ"),
            ("980", "Lịch sử Nam Mỹ"), ("990", "Lịch sử các vùng khác")
        };

        var roots = new Dictionary<string, Classification>();

        foreach (var (code, name) in mainClasses)
        {
            var entity = new Classification
            {
                Scheme = "DDC",
                Edition = "23",
                Code = code,
                Name = name,
                SortOrder = int.Parse(code),
                Level = 0,
                IsActive = true,
                CreatedAt = _clock.Now
            };

            entity.Path = entity.Id.ToString();
            roots[code] = entity;
        }

        _db.Classifications.AddRange(roots.Values);

        foreach (var (code, name) in divisions)
        {
            // A division belongs under the main class sharing its first digit: 530 under 500.
            var parent = roots[$"{code[0]}00"];

            var division = new Classification
            {
                Scheme = "DDC",
                Edition = "23",
                Code = code,
                Name = name,
                SortOrder = int.Parse(code),
                ParentId = parent.Id,
                Level = 1,
                IsActive = true,
                CreatedAt = _clock.Now
            };

            // The materialised path ends with the node's own id, so a prefix match selects the whole
            // branch including the node itself.
            division.Path = $"{parent.Path}/{division.Id}";

            _db.Classifications.Add(division);
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Đã nạp bảng tóm tắt DDC: {Classes} lớp chính và {Divisions} phân lớp",
            mainClasses.Length, divisions.Length);
    }
}
