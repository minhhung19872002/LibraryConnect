using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Ill;
using LibraryConnect.Marc;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.InterLibrary;

// ---------------------------------------------------------------------------------------------
// II.7 — Nhập biểu ghi từ thư viện khác: tra song song nhiều máy chủ, đối chiếu với kho của mình,
// rồi mở trình soạn MARC để hiệu đính trước khi lưu.
// ---------------------------------------------------------------------------------------------

/// <summary>Tra cứu ở một hoặc nhiều máy chủ đích cùng lúc.</summary>
public class RemoteSearchCommand : IRequest<RemoteSearchResultDto>
{
    /// <summary>Bỏ trống thì tra ở mọi máy chủ đang bật.</summary>
    public List<Guid> TargetIds { get; set; } = new();

    public RemoteSearchField Field { get; set; } = RemoteSearchField.Any;

    public string Term { get; set; } = string.Empty;

    public int MaxRecords { get; set; } = 20;
}

public class RemoteSearchCommandValidator : AbstractValidator<RemoteSearchCommand>
{
    public RemoteSearchCommandValidator()
    {
        RuleFor(command => command.Term)
            .NotEmpty().WithMessage("Chưa nhập từ khóa tra cứu.")
            .MaximumLength(300).WithMessage("Từ khóa tối đa 300 ký tự.");

        RuleFor(command => command.MaxRecords)
            .InclusiveBetween(1, 100).WithMessage("Số biểu ghi lấy về nằm trong khoảng 1 đến 100.");
    }
}

public class RemoteSearchCommandHandler : IRequestHandler<RemoteSearchCommand, RemoteSearchResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IRemoteCatalogSearcher _searcher;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public RemoteSearchCommandHandler(
        IApplicationDbContext db,
        IRemoteCatalogSearcher searcher,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _searcher = searcher;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<RemoteSearchResultDto> Handle(
        RemoteSearchCommand command, CancellationToken ct)
    {
        var targets = await _db.Z3950Targets
            .AsNoTracking()
            .Where(target => target.IsActive
                && (command.TargetIds.Count == 0 || command.TargetIds.Contains(target.Id)))
            .OrderBy(target => target.SortOrder)
            .ToListAsync(ct);

        if (targets.Count == 0)
        {
            throw new ConflictException(
                "Chưa có máy chủ nào đang bật để tra cứu. Hãy khai báo trong Liên thư viện.");
        }

        // Tra song song: một máy chủ ở nước ngoài chậm cả chục giây, hỏi tuần tự thì cán bộ phải
        // ngồi chờ bằng tổng thời gian của mọi máy chủ.
        //
        // Nhưng chỉ song song giữa các nơi khác nhau. Nhiều thư viện khai hai lối vào cùng một máy
        // chủ (một Z39.50, một SRU); mở hai phiên cùng lúc thì máy chủ bỏ tập kết quả của phiên
        // sau và lối đó về tay không. Nên trong cùng một địa chỉ thì hỏi lần lượt.
        var groups = targets
            .GroupBy(HostOf)
            .Select(async group =>
            {
                var groupResults = new List<RemoteSearchTargetResultDto>();

                foreach (var target in group)
                {
                    groupResults.Add(await _searcher.SearchAsync(
                        target, command.Field, command.Term.Trim(), command.MaxRecords, ct));
                }

                return groupResults;
            });

        var results = (await Task.WhenAll(groups)).SelectMany(group => group).ToList();

        var enriched = new List<RemoteSearchTargetResultDto>();

        foreach (var result in results)
        {
            enriched.Add(result with { Records = await MatchExistingAsync(result.Records, ct) });
        }

        foreach (var result in enriched)
        {
            _db.Z3950SearchLogs.Add(new Z3950SearchLog
            {
                TargetId = result.TargetId,
                Query = $"{RemoteSearchFields.Describe(command.Field)} = {command.Term}",
                ResultCount = result.TotalHits,
                DurationMs = result.DurationMs,
                Success = result.Success,
                Message = result.Message,
                UserId = _currentUser.UserId,
                OccurredAt = _clock.Now,
            });
        }

        await _db.SaveChangesAsync(ct);

        return new RemoteSearchResultDto(
            enriched,
            enriched.Sum(result => result.TotalHits),
            enriched.Sum(result => result.Records.Count));
    }

    /// <summary>
    /// Đánh dấu biểu ghi nào thư viện mình đã có.
    ///
    /// Đây là thứ giữ cho kho khỏi sinh bản trùng: cán bộ nhìn thấy ngay "đã có trong kho" trước khi
    /// bấm nhập. So theo ISBN trước vì nó chắc chắn nhất, rồi mới tới nhan đề.
    /// </summary>
    private async Task<IReadOnlyList<RemoteRecordDto>> MatchExistingAsync(
        IReadOnlyList<RemoteRecordDto> records, CancellationToken ct)
    {
        if (records.Count == 0)
        {
            return records;
        }

        var isbns = records
            .Select(record => Normalize(record.Isbn))
            .Where(isbn => isbn is not null)
            .Select(isbn => isbn!)
            .Distinct()
            .ToList();

        var titles = records
            .Select(record => record.Title?.Trim().ToLowerInvariant())
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .Select(title => title!)
            .Distinct()
            .ToList();

        var byIsbn = isbns.Count == 0
            ? new Dictionary<string, (Guid Id, string Title)>()
            : (await _db.BibRecords
                .AsNoTracking()
                .Where(bib => bib.Isbn != null)
                .Select(bib => new { bib.Id, bib.Title, bib.Isbn })
                .ToListAsync(ct))
                .Where(bib => Normalize(bib.Isbn) is { } value && isbns.Contains(value))
                .GroupBy(bib => Normalize(bib.Isbn)!)
                .ToDictionary(group => group.Key, group => (group.First().Id, group.First().Title));

        var byTitle = titles.Count == 0
            ? new Dictionary<string, (Guid Id, string Title)>()
            : (await _db.BibRecords
                .AsNoTracking()
                .Where(bib => titles.Contains(bib.Title.ToLower()))
                .Select(bib => new { bib.Id, bib.Title })
                .ToListAsync(ct))
                .GroupBy(bib => bib.Title.ToLowerInvariant())
                .ToDictionary(group => group.Key, group => (group.First().Id, group.First().Title));

        return records.Select(record =>
        {
            var isbn = Normalize(record.Isbn);

            if (isbn is not null && byIsbn.TryGetValue(isbn, out var match))
            {
                return record with { ExistingBibId = match.Id, ExistingBibTitle = match.Title };
            }

            var title = record.Title?.Trim().ToLowerInvariant();

            if (title is not null && byTitle.TryGetValue(title, out var titleMatch))
            {
                return record with
                {
                    ExistingBibId = titleMatch.Id,
                    ExistingBibTitle = titleMatch.Title,
                };
            }

            return record;
        }).ToList();
    }

    /// <summary>
    /// Tên máy chủ thật của một lối vào, dùng để gom những lối cùng trỏ về một nơi.
    ///
    /// Một thư viện thường khai hai lối: Z39.50 ghi thẳng tên máy, còn SRU ghi cả địa chỉ HTTP —
    /// phải bóc tên máy ra mới nhận ra chúng là cùng một chỗ.
    /// </summary>
    private static string HostOf(Z3950Target target)
    {
        if (target.UseSru
            && Uri.TryCreate(target.SruBaseUrl, UriKind.Absolute, out var uri))
        {
            return uri.Host.ToLowerInvariant();
        }

        return target.Host.ToLowerInvariant();
    }

    /// <summary>Bỏ gạch nối và khoảng trắng để so ISBN của hai nơi ghi khác kiểu.</summary>
    private static string? Normalize(string? isbn) =>
        string.IsNullOrWhiteSpace(isbn)
            ? null
            : new string(isbn.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
}

/// <summary>
/// Lấy một biểu ghi từ thư viện khác về dưới dạng biểu ghi MARC đầy đủ, sẵn sàng mở trong trình
/// soạn MARC (II.7).
///
/// Ghi lại nguồn vào trường 040$d và số kiểm soát gốc vào 035$a — đây là cách giới thư viện ghi
/// nhận biểu ghi lấy từ đâu, để sau này còn đối chiếu được.
/// </summary>
public record PrepareRemoteRecordCommand(Guid TargetId, string MarcJson) : IRequest<string>;

public class PrepareRemoteRecordCommandHandler : IRequestHandler<PrepareRemoteRecordCommand, string>
{
    private readonly IApplicationDbContext _db;
    private readonly ISystemParameterService _parameters;

    public PrepareRemoteRecordCommandHandler(
        IApplicationDbContext db, ISystemParameterService parameters)
    {
        _db = db;
        _parameters = parameters;
    }

    public async Task<string> Handle(PrepareRemoteRecordCommand command, CancellationToken ct)
    {
        var target = await _db.Z3950Targets
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.Id == command.TargetId, ct)
            ?? throw new NotFoundException("máy chủ Z39.50", command.TargetId);

        MarcRecord record;

        try
        {
            record = MarcJson.Deserialize(command.MarcJson);
        }
        catch (Exception ex) when (ex is MarcException or System.Text.Json.JsonException)
        {
            throw new Common.Exceptions.ValidationException(
                "marcJson", $"Biểu ghi gửi lên không đọc được: {ex.Message}");
        }

        var original = record.ControlNumber;

        if (!string.IsNullOrWhiteSpace(original))
        {
            // 035$a giữ số kiểm soát của nơi phát, viết theo dạng (nguồn)số như quy ước MARC 21.
            var host = target.UseSru ? target.SruBaseUrl ?? target.Name : target.Host;
            record.AddField("035").AddSubfield('a', $"({host}){original}");
        }

        // Số kiểm soát của thư viện mình sẽ do hệ thống cấp khi lưu, nên xóa số của nơi khác đi.
        record.SetControlField("001", null);

        var source = await _parameters.GetAsync("CATALOG.MARC_040A", string.Empty, ct);
        var field040 = record.GetField("040") ?? record.AddField("040");

        // 040$d là "nơi hiệu đính": biểu ghi gốc của thư viện bạn, mình sửa lại nên ghi tên mình vào.
        if (!string.IsNullOrWhiteSpace(source))
        {
            field040.AddSubfield('d', source);
        }

        return MarcJson.Serialize(record);
    }
}
