using System.Text.RegularExpressions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Entities.Cat;
using LibraryConnect.Domain.Entities.Rdr;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Readers;

/// <summary>Kết quả xử lý một tệp nhập bạn đọc.</summary>
public class ReaderImportOutcome
{
    public int TotalRows { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public List<ReaderImportErrorDto> Errors { get; } = new();
    public List<ReaderImportRowDto> Rows { get; } = new();

    public int ValidRows => Created + Updated;
    public int ErrorRows => Errors.Select(error => error.Row).Distinct().Count();
}

/// <summary>
/// Đọc, kiểm tra và ghi dữ liệu bạn đọc từ một sheet Excel (VI.4).
///
/// Một lớp duy nhất phục vụ cả bước kiểm tra lẫn bước nhập thật: nếu tách làm hai đường code thì sớm
/// muộn cũng có luật kiểm tra chỉ tồn tại ở một bên, và cán bộ sẽ thấy tệp "không lỗi" ở bước xem
/// trước nhưng lại đổ lỗi ở bước nhập.
/// </summary>
public class ReaderImportProcessor
{
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"^[0-9+().\s-]{6,20}$", RegexOptions.Compiled);

    /// <summary>Số dòng lỗi giữ lại; nhiều hơn thế thì tệp cần sửa lại chứ không cần đọc tiếp.</summary>
    public const int MaxRecordedErrors = 500;

    private readonly IApplicationDbContext _db;
    private readonly ICodeGenerator _codes;
    private readonly IPasswordHasher _hasher;
    private readonly IDateTimeProvider _clock;

    public ReaderImportProcessor(
        IApplicationDbContext db,
        ICodeGenerator codes,
        IPasswordHasher hasher,
        IDateTimeProvider clock)
    {
        _db = db;
        _codes = codes;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task<ReaderImportOutcome> ProcessAsync(
        ExcelSheet sheet, ReaderImportOptions options, bool dryRun, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(options);

        var outcome = new ReaderImportOutcome { TotalRows = sheet.Rows.Count };
        var today = _clock.Today;

        var nameHeader = options.HeaderOf(ReaderImportFields.FullName);

        if (!sheet.Headers.Contains(nameHeader, StringComparer.OrdinalIgnoreCase))
        {
            outcome.Errors.Add(new ReaderImportErrorDto(1, nameHeader, null,
                $"Tệp không có cột '{nameHeader}'. Hãy tải lại tệp mẫu hoặc ánh xạ lại cột."));
            return outcome;
        }

        var readerTypes = await _db.ReaderTypes.ToListAsync(ct);
        var faculties = await _db.Faculties.ToListAsync(ct);
        var majors = await _db.Majors.ToListAsync(ct);
        var cohorts = await _db.Cohorts.ToListAsync(ct);
        var classes = await _db.StudentClasses.ToListAsync(ct);

        var existingCards = await _db.Readers
            .Select(reader => new { reader.Id, reader.CardNumber, reader.StudentCode })
            .ToListAsync(ct);

        var readerIdByCard = existingCards
            .GroupBy(row => row.CardNumber, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.OrdinalIgnoreCase);

        var readerIdByStudentCode = existingCards
            .Where(row => !string.IsNullOrWhiteSpace(row.StudentCode))
            .GroupBy(row => row.StudentCode!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.OrdinalIgnoreCase);

        var seenCards = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenStudentCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in sheet.Rows)
        {
            ct.ThrowIfCancellationRequested();

            var errorsBefore = outcome.Errors.Count;

            var fullName = row.Get(nameHeader).Trim();
            var cardNumber = Value(row, options, ReaderImportFields.CardNumber);
            var studentCode = Value(row, options, ReaderImportFields.StudentCode);
            var email = Value(row, options, ReaderImportFields.Email)?.ToLowerInvariant();
            var phone = Value(row, options, ReaderImportFields.Phone);

            var parsed = new ReaderImportRowDto
            {
                Row = row.RowNumber,
                CardNumber = cardNumber,
                StudentCode = studentCode,
                FullName = fullName,
                Gender = NormalizeGender(Value(row, options, ReaderImportFields.Gender)),
                Email = email,
                Phone = phone,
                ReaderType = Value(row, options, ReaderImportFields.ReaderType),
                Faculty = Value(row, options, ReaderImportFields.Faculty),
                Major = Value(row, options, ReaderImportFields.Major),
                ClassName = Value(row, options, ReaderImportFields.ClassName),
                CourseYear = Value(row, options, ReaderImportFields.CourseYear)
            };

            if (string.IsNullOrWhiteSpace(fullName))
            {
                AddError(outcome, row.RowNumber, nameHeader, fullName, "Chưa nhập họ và tên.");
            }

            if (!string.IsNullOrWhiteSpace(email) && !EmailPattern.IsMatch(email))
            {
                AddError(outcome, row.RowNumber, options.HeaderOf(ReaderImportFields.Email), email,
                    "Địa chỉ email không hợp lệ.");
            }

            if (!string.IsNullOrWhiteSpace(phone) && !PhonePattern.IsMatch(phone))
            {
                AddError(outcome, row.RowNumber, options.HeaderOf(ReaderImportFields.Phone), phone,
                    "Số điện thoại chỉ gồm chữ số và các ký tự + ( ) . -");
            }

            var dateOfBirth = ReadDate(outcome, row, options, ReaderImportFields.DateOfBirth);
            var issueDate = ReadDate(outcome, row, options, ReaderImportFields.CardIssueDate);
            var expireDate = ReadDate(outcome, row, options, ReaderImportFields.CardExpireDate);
            parsed.DateOfBirth = dateOfBirth;

            // Trùng ngay trong tệp: hai dòng cùng mã sinh viên là lỗi của tệp, không phải của hệ thống.
            if (!string.IsNullOrWhiteSpace(studentCode) && !seenStudentCodes.Add(studentCode))
            {
                AddError(outcome, row.RowNumber, options.HeaderOf(ReaderImportFields.StudentCode), studentCode,
                    "Mã sinh viên bị lặp trong chính tệp này.");
            }

            if (!string.IsNullOrWhiteSpace(cardNumber) && !seenCards.Add(cardNumber))
            {
                AddError(outcome, row.RowNumber, options.HeaderOf(ReaderImportFields.CardNumber), cardNumber,
                    "Số thẻ bị lặp trong chính tệp này.");
            }

            var readerType = ResolveReaderType(readerTypes, parsed.ReaderType, options.DefaultReaderTypeId);

            if (readerType is null)
            {
                AddError(outcome, row.RowNumber, options.HeaderOf(ReaderImportFields.ReaderType),
                    parsed.ReaderType,
                    string.IsNullOrWhiteSpace(parsed.ReaderType)
                        ? "Chưa có loại bạn đọc và cũng chưa chọn loại mặc định cho đợt nhập."
                        : $"Không tìm thấy loại bạn đọc '{parsed.ReaderType}' trong danh mục.");
            }

            // Xác định hồ sơ đã có: ưu tiên mã sinh viên vì đó là mã do nhà trường cấp, số thẻ chỉ là
            // mã nội bộ của thư viện.
            Guid? existingId = null;

            if (!string.IsNullOrWhiteSpace(studentCode)
                && readerIdByStudentCode.TryGetValue(studentCode, out var byStudent))
            {
                existingId = byStudent;
            }
            else if (!string.IsNullOrWhiteSpace(cardNumber)
                     && readerIdByCard.TryGetValue(cardNumber, out var byCard))
            {
                existingId = byCard;
            }

            parsed.IsExisting = existingId is not null;

            if (existingId is not null && options.OnDuplicate == ReaderImportDuplicateAction.Reject)
            {
                AddError(outcome, row.RowNumber, options.HeaderOf(ReaderImportFields.StudentCode),
                    studentCode ?? cardNumber,
                    "Bạn đọc đã có trong hệ thống. Chọn cách xử lý trùng là Cập nhật hoặc Bỏ qua nếu muốn nhập tiếp.");
            }

            parsed.HasError = outcome.Errors.Count > errorsBefore;

            if (outcome.Rows.Count < 50)
            {
                outcome.Rows.Add(parsed);
            }

            if (parsed.HasError)
            {
                continue;
            }

            if (existingId is not null && options.OnDuplicate == ReaderImportDuplicateAction.Skip)
            {
                outcome.Skipped++;
                continue;
            }

            var facultyId = await ResolveCatalogAsync(faculties, parsed.Faculty, options, dryRun,
                name => new Faculty { Code = MakeCode(name), Name = name }, ct);

            var majorId = await ResolveCatalogAsync(majors, parsed.Major, options, dryRun,
                name => new Major { Code = MakeCode(name), Name = name, FacultyId = facultyId }, ct);

            // Lớp và khóa lưu trên hồ sơ dạng chuỗi vì danh sách từ phòng đào tạo bao giờ cũng là
            // chuỗi; danh mục chỉ để chuẩn hóa cách viết và để lọc, nên chỉ cần bảo đảm có mặt.
            await EnsureCohortAsync(cohorts, parsed.CourseYear, options, dryRun, ct);
            await EnsureClassAsync(classes, parsed.ClassName, parsed.CourseYear, facultyId, majorId,
                options, dryRun, ct);

            if (dryRun)
            {
                if (existingId is not null)
                {
                    outcome.Updated++;
                }
                else
                {
                    outcome.Created++;
                }

                continue;
            }

            Reader reader;

            if (existingId is not null)
            {
                reader = await _db.Readers.FirstAsync(entity => entity.Id == existingId, ct);
                outcome.Updated++;
            }
            else
            {
                var issued = issueDate ?? today;

                reader = new Reader
                {
                    CardNumber = string.IsNullOrWhiteSpace(cardNumber)
                        ? await _codes.NextAsync("CARD", ct)
                        : cardNumber,
                    CardIssueDate = issued,
                    CardExpireDate = expireDate
                                     ?? issued.AddMonths(Math.Max(1, readerType!.CardValidMonths)),
                    Status = ReaderStatus.Active
                };

                _db.Readers.Add(reader);

                _db.ReaderCards.Add(new ReaderCard
                {
                    ReaderId = reader.Id,
                    CardNumber = reader.CardNumber,
                    IssueDate = reader.CardIssueDate,
                    ExpireDate = reader.CardExpireDate,
                    IsCurrent = true
                });

                readerIdByCard[reader.CardNumber] = reader.Id;
                outcome.Created++;
            }

            reader.FullName = fullName;
            reader.StudentCode = studentCode;
            reader.Gender = parsed.Gender;
            reader.DateOfBirth = dateOfBirth;
            reader.IdCardNumber = Value(row, options, ReaderImportFields.IdCardNumber);
            reader.Email = email;
            reader.Phone = phone;
            reader.Address = Value(row, options, ReaderImportFields.Address);
            reader.ReaderTypeId = readerType!.Id;
            reader.FacultyId = facultyId;
            reader.MajorId = majorId;
            reader.ClassName = parsed.ClassName;
            reader.CourseYear = parsed.CourseYear;
            reader.Note = Value(row, options, ReaderImportFields.Note);

            if (issueDate is not null)
            {
                reader.CardIssueDate = issueDate.Value;
            }

            if (expireDate is not null)
            {
                reader.CardExpireDate = expireDate.Value;
            }

            if (!string.IsNullOrWhiteSpace(studentCode))
            {
                readerIdByStudentCode[studentCode] = reader.Id;
            }

            // Mật khẩu tra cứu ban đầu đặt bằng ngày sinh — cách các trường vẫn làm khi phát thẻ cho
            // cả khóa. Bạn đọc buộc phải đổi ở lần đăng nhập đầu tiên.
            if (options.SetInitialPassword && dateOfBirth is not null
                && string.IsNullOrEmpty(reader.PasswordHash))
            {
                reader.PasswordHash = _hasher.Hash(dateOfBirth.Value.ToString("ddMMyyyy"));
                reader.MustChangePassword = true;
            }
        }

        return outcome;
    }

    private static void AddError(
        ReaderImportOutcome outcome, int row, string? column, string? value, string message)
    {
        if (outcome.Errors.Count < MaxRecordedErrors)
        {
            outcome.Errors.Add(new ReaderImportErrorDto(row, column, value, message));
        }
    }

    private static string? Value(ExcelRow row, ReaderImportOptions options, string field)
    {
        var text = row.Get(options.HeaderOf(field)).Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static DateOnly? ReadDate(
        ReaderImportOutcome outcome, ExcelRow row, ReaderImportOptions options, string field)
    {
        var header = options.HeaderOf(field);
        var text = row.Get(header).Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (ReaderDateParser.TryParse(text, out var date))
        {
            return date;
        }

        AddError(outcome, row.RowNumber, header, text, "Ngày không đọc được. Hãy ghi dạng ngày/tháng/năm.");
        return null;
    }

    private static string? NormalizeGender(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = VietnameseText.RemoveDiacritics(value.Trim()).ToLowerInvariant();

        return text switch
        {
            "nam" or "m" or "male" or "1" => "Nam",
            "nu" or "f" or "female" or "0" or "2" => "Nữ",
            _ => value.Trim()
        };
    }

    private static ReaderType? ResolveReaderType(
        IReadOnlyList<ReaderType> types, string? value, Guid? defaultId)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            var needle = VietnameseText.RemoveDiacritics(value.Trim()).ToLowerInvariant();

            var match = types.FirstOrDefault(type =>
                string.Equals(type.Code, value.Trim(), StringComparison.OrdinalIgnoreCase)
                || VietnameseText.RemoveDiacritics(type.Name).ToLowerInvariant() == needle);

            if (match is not null)
            {
                return match;
            }
        }

        return defaultId is null ? null : types.FirstOrDefault(type => type.Id == defaultId);
    }

    /// <summary>Tìm mục danh mục theo mã hoặc tên; chưa có thì tạo mới nếu cán bộ cho phép.</summary>
    private async Task<Guid?> ResolveCatalogAsync<TEntity>(
        List<TEntity> cache,
        string? value,
        ReaderImportOptions options,
        bool dryRun,
        Func<string, TEntity> factory,
        CancellationToken ct)
        where TEntity : CatalogEntity
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.Trim();
        var needle = VietnameseText.RemoveDiacritics(text).ToLowerInvariant();

        var match = cache.FirstOrDefault(entity =>
            string.Equals(entity.Code, text, StringComparison.OrdinalIgnoreCase)
            || VietnameseText.RemoveDiacritics(entity.Name).ToLowerInvariant() == needle);

        if (match is not null)
        {
            return match.Id;
        }

        if (!options.CreateMissingCatalogs)
        {
            return null;
        }

        var created = factory(text);
        cache.Add(created);

        if (!dryRun)
        {
            _db.Set<TEntity>().Add(created);
            await _db.SaveChangesAsync(ct);
        }

        return created.Id;
    }

    private async Task EnsureCohortAsync(
        List<Cohort> cache, string? value, ReaderImportOptions options, bool dryRun, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(value) || !options.CreateMissingCatalogs)
        {
            return;
        }

        var text = value.Trim();

        if (cache.Any(cohort => string.Equals(cohort.Code, text, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var created = new Cohort { Code = text, Name = $"Khóa {text}" };
        cache.Add(created);

        if (!dryRun)
        {
            _db.Cohorts.Add(created);
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task EnsureClassAsync(
        List<StudentClass> cache,
        string? value,
        string? cohortCode,
        Guid? facultyId,
        Guid? majorId,
        ReaderImportOptions options,
        bool dryRun,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(value) || !options.CreateMissingCatalogs)
        {
            return;
        }

        var text = value.Trim();

        if (cache.Any(entity => string.Equals(entity.Code, text, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var created = new StudentClass
        {
            Code = text,
            Name = text,
            CohortCode = cohortCode,
            FacultyId = facultyId,
            MajorId = majorId
        };

        cache.Add(created);

        if (!dryRun)
        {
            _db.StudentClasses.Add(created);
            await _db.SaveChangesAsync(ct);
        }
    }

    /// <summary>Sinh mã danh mục từ tên khi tạo mới: bỏ dấu, viết hoa, cắt còn 30 ký tự.</summary>
    private static string MakeCode(string name)
    {
        var stripped = VietnameseText.RemoveDiacritics(name).ToUpperInvariant();
        var code = new string(stripped.Where(character => char.IsLetterOrDigit(character)).ToArray());

        if (code.Length == 0)
        {
            code = "DM";
        }

        return code.Length > 30 ? code[..30] : code;
    }
}
