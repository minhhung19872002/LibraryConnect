using System.Globalization;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Marc;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>
/// Ghi một biểu ghi thư mục: kiểm tra, sinh số kiểm soát, cập nhật cột phẳng và liên kết danh mục,
/// lưu lịch sử phiên bản.
/// </summary>
public interface IBibRecordWriter
{
    /// <summary>
    /// Cấp số kiểm soát và mã cơ quan cho biểu ghi nếu chưa có.
    ///
    /// This runs before the record is validated, because 001 is a mandatory field that the system
    /// itself supplies: validating first would reject every new record for missing the very field
    /// the save is about to write.
    /// </summary>
    Task PrepareAsync(BibRecord entity, MarcRecord marc, CancellationToken ct = default);

    /// <summary>
    /// Áp nội dung MARC lên một biểu ghi. Với biểu ghi đã có, phiên bản trước được lưu lại trước
    /// khi ghi đè.
    /// </summary>
    Task ApplyAsync(
        BibRecord entity,
        MarcRecord marc,
        bool isNew,
        string? changeNote,
        CancellationToken ct = default);
}

/// <summary>
/// Mọi đường ghi biểu ghi — biên mục chi tiết, biên mục sơ lược, nhập ISO 2709, nhập Excel, nhập
/// từ Z39.50 — đều đi qua đây.
///
/// Having one place that writes a record is what keeps the invariants true: the control number is
/// issued exactly once, field 005 always reflects the last save, the flat columns always match the
/// MARC beside them, the authority links always match the names in the record, and no edit is ever
/// lost because the previous version is snapshotted before the new one overwrites it.
/// </summary>
public class BibRecordWriter : IBibRecordWriter
{
    private readonly IApplicationDbContext _db;
    private readonly IBibAuthorityLinker _linker;
    private readonly ICodeGenerator _codes;
    private readonly ISystemParameterService _parameters;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUser _currentUser;

    public BibRecordWriter(
        IApplicationDbContext db,
        IBibAuthorityLinker linker,
        ICodeGenerator codes,
        ISystemParameterService parameters,
        IDateTimeProvider clock,
        ICurrentUser currentUser)
    {
        _db = db;
        _linker = linker;
        _codes = codes;
        _parameters = parameters;
        _clock = clock;
        _currentUser = currentUser;
    }

    public Task PrepareAsync(BibRecord entity, MarcRecord marc, CancellationToken ct = default) =>
        EnsureControlNumberAsync(entity, marc, ct);

    public async Task ApplyAsync(
        BibRecord entity,
        MarcRecord marc,
        bool isNew,
        string? changeNote,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(marc);

        if (!isNew && !string.IsNullOrEmpty(entity.MarcData))
        {
            await SnapshotAsync(entity, changeNote, ct);
        }

        // Idempotent: the save path calls this before validating, so by now it usually has nothing
        // left to do.
        await EnsureControlNumberAsync(entity, marc, ct);
        await EnsureOrganisationCodeAsync(marc, ct);
        StampTransactionTime(marc);
        MarkRecordStatus(marc, isNew);

        var projection = MarcProjection.Project(marc);
        var links = await _linker.LinkAsync(entity, projection, ct);

        entity.MarcData = MarcJson.Serialize(marc);
        entity.ControlNumber = marc.ControlNumber ?? entity.ControlNumber;

        entity.Title = projection.Title;
        entity.Subtitle = projection.Subtitle;
        entity.StatementOfResponsibility = projection.StatementOfResponsibility;
        entity.AuthorMain = projection.AuthorMain;
        entity.UniformTitle = projection.UniformTitle;
        entity.Isbn = projection.Isbn;
        entity.Issn = projection.Issn;
        entity.PublisherName = projection.PublisherName;
        entity.PublishPlace = projection.PublishPlace;
        entity.PublishYear = projection.PublishYear;
        entity.Edition = projection.Edition;
        entity.Pages = projection.Pages;
        entity.Dimensions = projection.Dimensions;
        entity.Ddc = projection.Ddc;
        entity.Abstract = projection.Abstract;
        entity.SeriesVolume = projection.SeriesVolume;

        entity.PublisherId = links.PublisherId;
        entity.LanguageId = links.LanguageId;
        entity.CountryId = links.CountryId;
        entity.SeriesId = links.SeriesId;
    }

    /// <summary>
    /// Lưu lại nội dung MARC trước khi ghi đè.
    ///
    /// Requirement II.3 asks for every version to be kept, so this is an insert and never an update:
    /// history is append-only and a librarian can always go back to what a record looked like before
    /// any particular edit.
    /// </summary>
    private async Task SnapshotAsync(BibRecord entity, string? changeNote, CancellationToken ct)
    {
        var lastVersion = await _db.BibRecordVersions
            .Where(version => version.BibId == entity.Id)
            .MaxAsync(version => (int?)version.VersionNumber, ct) ?? 0;

        _db.BibRecordVersions.Add(new BibRecordVersion
        {
            Id = Guid.NewGuid(),
            BibId = entity.Id,
            VersionNumber = lastVersion + 1,
            MarcData = entity.MarcData,
            ChangeNote = changeNote,
            ChangedBy = _currentUser.UserId,
            ChangedByName = _currentUser.FullName,
            ChangedAt = _clock.Now
        });
    }

    /// <summary>
    /// Cấp số kiểm soát cho biểu ghi mới nếu trường 001 còn trống.
    ///
    /// A record imported from another library arrives with a control number of its own; that number
    /// is kept, because it is what the source system will quote when the two catalogues are compared.
    /// The number this library issues goes to records it creates itself.
    ///
    /// A number consumed by a save that then fails validation leaves a gap in the sequence. That is
    /// how sequences behave everywhere in the product and is preferable to handing the same number to
    /// two records that are being catalogued at the same moment.
    /// </summary>
    private async Task EnsureControlNumberAsync(BibRecord entity, MarcRecord marc, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(marc.ControlNumber))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(entity.ControlNumber))
        {
            marc.ControlNumber = entity.ControlNumber;
            return;
        }

        var prefix = await _parameters.GetAsync(
            "CATALOG.CONTROL_NUMBER_PREFIX", "LC", ct);

        var number = await _codes.NextAsync("CONTROL", ct);

        // The generator already applies the CODE.CONTROL_* parameters; the cataloguing prefix is
        // what distinguishes this library's numbers in a shared catalogue.
        marc.ControlNumber = number.StartsWith(prefix, StringComparison.Ordinal) ? number : prefix + number;
    }

    /// <summary>
    /// Trường 003 ghi cơ quan đã cấp số kiểm soát ở trường 001.
    ///
    /// It is stamped on every save rather than only when a number is issued: a client that posts a
    /// record it assembled itself — the cataloguing form, an import, a Z39.50 capture — would
    /// otherwise drop the field, and a control number with no owning agency is ambiguous the moment
    /// the record reaches another catalogue.
    /// </summary>
    private async Task EnsureOrganisationCodeAsync(MarcRecord marc, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(marc.GetControlField("003")))
        {
            return;
        }

        var organisation = await _parameters.GetAsync("CATALOG.MARC_040A", string.Empty, ct);

        if (!string.IsNullOrWhiteSpace(organisation))
        {
            marc.SetControlField("003", organisation);
        }
    }

    /// <summary>Trường 005 ghi thời điểm lưu gần nhất, định dạng yyyyMMddHHmmss.f.</summary>
    private void StampTransactionTime(MarcRecord marc) =>
        marc.SetControlField("005", _clock.Now.UtcDateTime.ToString("yyyyMMddHHmmss.f", CultureInfo.InvariantCulture));

    /// <summary>Đầu biểu vị trí 05: "n" cho biểu ghi mới, "c" cho biểu ghi đã sửa.</summary>
    private static void MarkRecordStatus(MarcRecord marc, bool isNew)
    {
        if (isNew)
        {
            if (marc.Leader.RecordStatus is not ('n' or 'a' or 'p'))
            {
                marc.Leader.RecordStatus = 'n';
            }

            return;
        }

        if (marc.Leader.RecordStatus == 'n')
        {
            marc.Leader.RecordStatus = 'c';
        }
    }
}
