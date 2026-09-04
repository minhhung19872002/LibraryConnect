using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Domain.Entities.Acq;
using LibraryConnect.Domain.Entities.Cat;
using LibraryConnect.Domain.Entities.Dig;
using LibraryConnect.Domain.Entities.Web;

namespace LibraryConnect.Application.Features.Catalogs;

/// <summary>
/// Every business lookup list of the product, declared once.
///
/// The generic catalogue screen, the Excel import/export and the merge function all read this
/// registry, so adding a new list to the system is a matter of adding one entry here rather than
/// writing another CRUD screen. Section 4.2 of the specification is the source for this list.
/// </summary>
public static class CatalogRegistry
{
    public static IReadOnlyList<CatalogDefinition> All { get; } = new List<CatalogDefinition>
    {
        // ---------------- Biên mục ----------------
        new CatalogDefinition<DocumentType>("document-types", "dạng tài liệu", "Dạng tài liệu")
        {
            Description = "Sách, báo, tạp chí, luận văn, luận án, đề tài nghiên cứu, bản đồ…",
            Fields = new CatalogField[]
            {
                CatalogFields.Text<DocumentType>("marcTypeOfRecord", "Leader/06 — Loại biểu ghi",
                    e => e.MarcTypeOfRecord, (e, v) => e.MarcTypeOfRecord = v,
                    "Ký tự điền vào vị trí 06 của Leader khi tạo biểu ghi mới, ví dụ 'a' cho tài liệu chữ.",
                    showInList: false),
                CatalogFields.Text<DocumentType>("marcBibLevel", "Leader/07 — Cấp thư mục",
                    e => e.MarcBibLevel, (e, v) => e.MarcBibLevel = v,
                    "Ví dụ 'm' cho chuyên khảo, 's' cho xuất bản phẩm nhiều kỳ.",
                    showInList: false),
                CatalogFields.Boolean<DocumentType>("isSerial", "Là ấn phẩm định kỳ",
                    e => e.IsSerial, (e, v) => e.IsSerial = v,
                    "Bật để dạng tài liệu này xuất hiện trong phân hệ Ấn phẩm định kỳ.")
            }
        },

        new CatalogDefinition<CarrierType>("carrier-types", "vật mang tin", "Vật mang tin")
        {
            Description = "Giấy, CD/DVD, tệp số, vi phim, băng từ…"
        },

        new CatalogDefinition<Language>("languages", "ngôn ngữ", "Ngôn ngữ")
        {
            Description = "Mã theo ISO 639-2, dùng cho trường MARC 041$a và vị trí 35–37 của trường 008.",
            Fields = new CatalogField[]
            {
                CatalogFields.Text<Language>("iso6391", "Mã ISO 639-1",
                    e => e.Iso6391, (e, v) => e.Iso6391 = v, "Mã hai chữ cái, ví dụ 'vi'.")
            }
        },

        new CatalogDefinition<Country>("countries", "nước xuất bản", "Nước xuất bản")
        {
            Description = "Mã theo MARC Country Code List, dùng cho trường 008/15-17 và 044$a."
        },

        new CatalogDefinition<Publisher>("publishers", "nhà xuất bản", "Nhà xuất bản")
        {
            SupportsMerge = true,
            Fields = new CatalogField[]
            {
                CatalogFields.Text<Publisher>("address", "Địa chỉ", e => e.Address, (e, v) => e.Address = v),
                CatalogFields.Text<Publisher>("phone", "Điện thoại", e => e.Phone, (e, v) => e.Phone = v),
                CatalogFields.Text<Publisher>("email", "Email", e => e.Email, (e, v) => e.Email = v, showInList: false),
                CatalogFields.Text<Publisher>("website", "Website", e => e.Website, (e, v) => e.Website = v, showInList: false)
            }
        },

        new CatalogDefinition<Author>("authors", "tác giả", "Tác giả")
        {
            Description = "Hồ sơ tác giả (authority file) dùng cho các trường MARC 100, 110, 700 và 710.",
            SupportsMerge = true,
            Fields = new CatalogField[]
            {
                CatalogFields.Text<Author>("fullName", "Họ và tên đầy đủ",
                    e => e.FullName, (e, v) => e.FullName = v ?? string.Empty, required: true),
                CatalogFields.Text<Author>("sortName", "Dạng sắp xếp",
                    e => e.SortName, (e, v) => e.SortName = v,
                    "Dạng ghi vào MARC 100$a, ví dụ 'Nguyễn, Văn A'."),
                CatalogFields.Text<Author>("birthYear", "Năm sinh", e => e.BirthYear, (e, v) => e.BirthYear = v),
                CatalogFields.Text<Author>("deathYear", "Năm mất", e => e.DeathYear, (e, v) => e.DeathYear = v),
                CatalogFields.Text<Author>("nationality", "Quốc tịch",
                    e => e.Nationality, (e, v) => e.Nationality = v, showInList: false),
                CatalogFields.Text<Author>("role", "Vai trò",
                    e => e.Role, (e, v) => e.Role = v, "Chủ biên, dịch giả, hiệu đính…", showInList: false),
                CatalogFields.Boolean<Author>("isCorporate", "Tác giả tập thể",
                    e => e.IsCorporate, (e, v) => e.IsCorporate = v,
                    "Bật cho cơ quan, tổ chức (MARC 110/710)."),
                CatalogFields.LongText<Author>("otherNames", "Tên khác",
                    e => e.OtherNames, (e, v) => e.OtherNames = v, "Mỗi dạng tên một dòng (MARC 400)."),
                CatalogFields.LongText<Author>("biography", "Tiểu sử", e => e.Biography, (e, v) => e.Biography = v)
            }
        },

        new CatalogDefinition<Subject>("subjects", "đề mục chủ đề", "Đề mục chủ đề")
        {
            Description = "Đề mục chủ đề phân cấp, dùng cho trường MARC 650.",
            IsHierarchical = true,
            SupportsMerge = true,
            Fields = new CatalogField[]
            {
                CatalogFields.Text<Subject>("scheme", "Bộ đề mục",
                    e => e.Scheme, (e, v) => e.Scheme = v, "Giá trị ghi vào 650$2."),
                CatalogFields.LongText<Subject>("scopeNote", "Phạm vi sử dụng",
                    e => e.ScopeNote, (e, v) => e.ScopeNote = v)
            }
        },

        new CatalogDefinition<Keyword>("keywords", "từ khóa", "Từ khóa")
        {
            Description = "Từ khóa tự do, dùng cho trường MARC 653.",
            ShowCode = false,
            SupportsMerge = true
        },

        new CatalogDefinition<Classification>("classifications", "ký hiệu phân loại", "Khung phân loại")
        {
            Description = "DDC, BBK, LCC, UDC. Mã ký hiệu là duy nhất trong phạm vi từng khung.",
            IsHierarchical = true,
            Fields = new CatalogField[]
            {
                CatalogFields.Select<Classification>("scheme", "Khung phân loại",
                    e => e.Scheme, (e, v) => e.Scheme = string.IsNullOrWhiteSpace(v) ? "DDC" : v,
                    new List<CatalogOption>
                    {
                        new("DDC", "DDC — Dewey"),
                        new("BBK", "BBK"),
                        new("LCC", "LCC — Library of Congress"),
                        new("UDC", "UDC")
                    }),
                CatalogFields.Text<Classification>("edition", "Ấn bản",
                    e => e.Edition, (e, v) => e.Edition = v, "Ghi vào 082$2, ví dụ '23'.", showInList: false)
            }
        },

        new CatalogDefinition<Series>("series", "tùng thư", "Tùng thư")
        {
            Description = "Tùng thư, dùng cho trường MARC 490 và 830.",
            Fields = new CatalogField[]
            {
                CatalogFields.Text<Series>("issn", "ISSN", e => e.Issn, (e, v) => e.Issn = v)
            }
        },

        new CatalogDefinition<Collection>("collections", "bộ sưu tập", "Bộ sưu tập")
        {
            Description = "Nhóm biểu ghi để trưng bày trên trang tra cứu.",
            IsHierarchical = true,
            Fields = new CatalogField[]
            {
                CatalogFields.Text<Collection>("imageUrl", "Ảnh đại diện",
                    e => e.ImageUrl, (e, v) => e.ImageUrl = v, showInList: false),
                CatalogFields.Boolean<Collection>("showOnOpac", "Hiển thị trên OPAC",
                    e => e.ShowOnOpac, (e, v) => e.ShowOnOpac = v)
            }
        },

        // ---------------- Bạn đọc và đào tạo ----------------
        new CatalogDefinition<ReaderType>("reader-types", "loại bạn đọc", "Loại bạn đọc")
        {
            Description = "Sinh viên, học viên, nghiên cứu sinh, giảng viên, cán bộ, khách…",
            Fields = new CatalogField[]
            {
                CatalogFields.Number<ReaderType>("cardValidMonths", "Hạn thẻ (tháng)",
                    e => e.CardValidMonths, (e, v) => e.CardValidMonths = v ?? 48,
                    "Dùng để tính ngày hết hạn khi cấp thẻ mới."),
                CatalogFields.Decimal<ReaderType>("cardFee", "Phí làm thẻ (VNĐ)",
                    e => e.CardFee, (e, v) => e.CardFee = v ?? 0),
                CatalogFields.Decimal<ReaderType>("depositAmount", "Tiền đặt cọc (VNĐ)",
                    e => e.DepositAmount, (e, v) => e.DepositAmount = v ?? 0),
                // "circulation-policies" không phải một danh mục của registry: giao diện nạp danh sách
                // này từ /api/circulation/policies. Ở máy chủ, tên danh mục tham chiếu chỉ là siêu dữ liệu.
                CatalogFields.Reference<ReaderType>("defaultPolicyId", "Chính sách lưu thông mặc định",
                    e => e.DefaultPolicyId, (e, v) => e.DefaultPolicyId = v, "circulation-policies",
                    "Áp dụng khi không ô nào của ma trận chính sách khớp với loại bạn đọc này.")
            }
        },

        new CatalogDefinition<Faculty>("faculties", "khoa", "Khoa")
        {
            Fields = new CatalogField[]
            {
                CatalogFields.Text<Faculty>("dean", "Trưởng khoa", e => e.Dean, (e, v) => e.Dean = v),
                CatalogFields.Text<Faculty>("phone", "Điện thoại", e => e.Phone, (e, v) => e.Phone = v, showInList: false),
                CatalogFields.Text<Faculty>("email", "Email", e => e.Email, (e, v) => e.Email = v, showInList: false)
            }
        },

        new CatalogDefinition<Major>("majors", "ngành đào tạo", "Ngành đào tạo")
        {
            Fields = new CatalogField[]
            {
                CatalogFields.Reference<Major>("facultyId", "Khoa quản lý",
                    e => e.FacultyId, (e, v) => e.FacultyId = v, "faculties"),
                CatalogFields.Select<Major>("trainingLevel", "Bậc đào tạo",
                    e => e.TrainingLevel, (e, v) => e.TrainingLevel = v,
                    new List<CatalogOption>
                    {
                        new("DH", "Đại học"),
                        new("THS", "Thạc sĩ"),
                        new("TS", "Tiến sĩ"),
                        new("CD", "Cao đẳng")
                    })
            }
        },

        new CatalogDefinition<Course>("courses", "môn học", "Môn học")
        {
            Fields = new CatalogField[]
            {
                CatalogFields.Number<Course>("credits", "Số tín chỉ",
                    e => e.Credits, (e, v) => e.Credits = v ?? 0),
                CatalogFields.Text<Course>("semester", "Học kỳ", e => e.Semester, (e, v) => e.Semester = v),
                CatalogFields.Text<Course>("lecturer", "Giảng viên phụ trách",
                    e => e.Lecturer, (e, v) => e.Lecturer = v, showInList: false)
            }
        },

        new CatalogDefinition<Cohort>("cohorts", "khóa học", "Khóa học")
        {
            Description = "Niên khóa tuyển sinh, ví dụ K45 — dùng để lọc bạn đọc và chuyển trạng thái ra trường theo khóa.",
            Fields = new CatalogField[]
            {
                CatalogFields.Number<Cohort>("startYear", "Năm nhập học",
                    e => e.StartYear, (e, v) => e.StartYear = v),
                CatalogFields.Number<Cohort>("endYear", "Năm tốt nghiệp dự kiến",
                    e => e.EndYear, (e, v) => e.EndYear = v)
            }
        },

        new CatalogDefinition<StudentClass>("student-classes", "lớp", "Lớp")
        {
            Description = "Lớp sinh viên, ví dụ DH19TH1.",
            Fields = new CatalogField[]
            {
                CatalogFields.Text<StudentClass>("cohortCode", "Mã khóa",
                    e => e.CohortCode, (e, v) => e.CohortCode = v,
                    "Khớp với mã trong danh mục Khóa học, ví dụ K45."),
                CatalogFields.Text<StudentClass>("advisor", "Cố vấn học tập",
                    e => e.Advisor, (e, v) => e.Advisor = v, showInList: false)
            }
        },

        new CatalogDefinition<ViolationType>("violation-types", "loại vi phạm", "Loại vi phạm")
        {
            Fields = new CatalogField[]
            {
                CatalogFields.Decimal<ViolationType>("defaultFine", "Mức phạt mặc định (VNĐ)",
                    e => e.DefaultFine, (e, v) => e.DefaultFine = v ?? 0)
            }
        },

        // ---------------- Bổ sung ----------------
        new CatalogDefinition<Supplier>("suppliers", "nhà cung cấp", "Nhà cung cấp")
        {
            Fields = new CatalogField[]
            {
                CatalogFields.Text<Supplier>("taxCode", "Mã số thuế", e => e.TaxCode, (e, v) => e.TaxCode = v),
                CatalogFields.Text<Supplier>("contactPerson", "Người liên hệ",
                    e => e.ContactPerson, (e, v) => e.ContactPerson = v),
                CatalogFields.Text<Supplier>("phone", "Điện thoại", e => e.Phone, (e, v) => e.Phone = v),
                CatalogFields.Text<Supplier>("email", "Email", e => e.Email, (e, v) => e.Email = v, showInList: false),
                CatalogFields.Text<Supplier>("address", "Địa chỉ", e => e.Address, (e, v) => e.Address = v, showInList: false),
                CatalogFields.Text<Supplier>("bankAccount", "Số tài khoản",
                    e => e.BankAccount, (e, v) => e.BankAccount = v, showInList: false),
                CatalogFields.Text<Supplier>("bankName", "Ngân hàng",
                    e => e.BankName, (e, v) => e.BankName = v, showInList: false),
                // Đánh giá nhà cung cấp (III.1): 0 là chưa đánh giá, 1–5 sao. Cột ở thực thể có từ
                // phase 6 nhưng chưa bao giờ được khai ở đây nên không sửa được từ giao diện.
                CatalogFields.Number<Supplier>("rating", "Đánh giá (1–5 sao)",
                    e => e.Rating, (e, v) => e.Rating = Math.Clamp(v ?? 0, 0, 5),
                    description: "Bỏ trống hoặc 0 là chưa đánh giá; hiện kèm lịch sử giao dịch ở báo cáo bổ sung.")
            }
        },

        new CatalogDefinition<FundingSource>("funding-sources", "nguồn kinh phí", "Nguồn kinh phí")
        {
            Fields = new CatalogField[]
            {
                CatalogFields.Number<FundingSource>("year", "Năm", e => e.Year, (e, v) => e.Year = v),
                CatalogFields.Decimal<FundingSource>("budget", "Dự toán (VNĐ)",
                    e => e.Budget, (e, v) => e.Budget = v)
            }
        },

        // ---------------- Tài liệu số và nội dung ----------------
        new CatalogDefinition<DigitalCollection>("digital-collections", "bộ sưu tập số", "Bộ sưu tập tài liệu số")
        {
            Description = "Cây bộ sưu tập của kho tài liệu số: giáo trình, luận văn, đề tài nghiên cứu…",
            IsHierarchical = true
        },

        new CatalogDefinition<CmsNewsCategory>("news-categories", "chuyên mục tin", "Chuyên mục tin tức")
    };

    private static readonly Dictionary<string, CatalogDefinition> ByCode =
        All.ToDictionary(definition => definition.Code, StringComparer.OrdinalIgnoreCase);

    public static CatalogDefinition Require(string code) =>
        ByCode.TryGetValue(code, out var definition)
            ? definition
            : throw new NotFoundException($"Không tìm thấy danh mục '{code}'.");

    public static bool Exists(string code) => ByCode.ContainsKey(code);
}
