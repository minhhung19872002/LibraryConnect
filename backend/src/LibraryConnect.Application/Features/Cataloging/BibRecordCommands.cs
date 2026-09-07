using FluentValidation;
using LibraryConnect.Application.Common.Exceptions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Marc;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Marc;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>Kết quả lưu biểu ghi: định danh, số kiểm soát và các cảnh báo còn lại.</summary>
public class SaveBibResultDto
{
    public Guid Id { get; set; }
    public string ControlNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public MarcValidationResultDto Validation { get; set; } = new();
    /// <summary>Số giá trị danh mục được tạo tự động từ nội dung biểu ghi.</summary>
    public int CreatedAuthorities { get; set; }
}

/// <summary>
/// Thêm mới hoặc cập nhật một biểu ghi (II.2, II.3).
/// </summary>
public class SaveBibRecordCommand : IRequest<SaveBibResultDto>
{
    /// <summary>Bỏ trống nghĩa là tạo biểu ghi mới.</summary>
    public Guid? Id { get; set; }

    /// <summary>Biểu ghi MARC dạng JSON, đúng dạng trình soạn thảo gửi lên.</summary>
    public string MarcJson { get; set; } = string.Empty;

    public Guid? DocumentTypeId { get; set; }
    public Guid? CarrierTypeId { get; set; }
    public List<Guid> CollectionIds { get; set; } = new();
    public string? CoverImageUrl { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Published;

    /// <summary>Ghi chú kèm phiên bản cũ khi sửa biểu ghi.</summary>
    public string? ChangeNote { get; set; }

    /// <summary>Nguồn của biểu ghi; mặc định là nhập tay.</summary>
    public BibSource Source { get; set; } = BibSource.Manual;
    public string? SourceRef { get; set; }
}

public class SaveBibRecordCommandValidator : AbstractValidator<SaveBibRecordCommand>
{
    public SaveBibRecordCommandValidator()
    {
        RuleFor(command => command.MarcJson)
            .NotEmpty().WithMessage("Biểu ghi MARC rỗng.");

        RuleFor(command => command.ChangeNote)
            .MaximumLength(500).WithMessage("Ghi chú thay đổi tối đa 500 ký tự.");
    }
}

public class SaveBibRecordCommandHandler : IRequestHandler<SaveBibRecordCommand, SaveBibResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IBibRecordWriter _writer;
    private readonly IMarcRuleProvider _rules;

    public SaveBibRecordCommandHandler(
        IApplicationDbContext db,
        IBibRecordWriter writer,
        IMarcRuleProvider rules)
    {
        _db = db;
        _writer = writer;
        _rules = rules;
    }

    public async Task<SaveBibResultDto> Handle(SaveBibRecordCommand request, CancellationToken ct)
    {
        MarcRecord marc;

        try
        {
            marc = MarcJson.Deserialize(request.MarcJson);
        }
        catch (MarcException exception)
        {
            throw new Common.Exceptions.ValidationException("MarcJson", exception.Message);
        }

        var isNew = request.Id is null;

        var entity = isNew
            ? new BibRecord { Id = Guid.NewGuid() }
            : await _db.BibRecords
                  .Include(record => record.Authors)
                  .Include(record => record.Subjects)
                  .Include(record => record.Keywords)
                  .Include(record => record.Classifications)
                  .Include(record => record.Collections)
                  .FirstOrDefaultAsync(record => record.Id == request.Id, ct)
              ?? throw new NotFoundException("Không tìm thấy biểu ghi cần sửa.");

        // The control number is issued before validation, because 001 is mandatory and the system is
        // what supplies it. Nothing is written to the database until the very end, so a record that
        // fails validation still leaves no row behind.
        await _writer.PrepareAsync(entity, marc, ct);

        // A record that fails validation is not saved at all: the flat columns and the authority
        // links are derived from it, and deriving them from a record with no title produces a row
        // no one can find again.
        var validator = await _rules.GetValidatorAsync(ct);
        var issues = validator.Validate(marc);

        if (!MarcValidator.IsValid(issues))
        {
            var errors = issues
                .Where(issue => issue.Severity == MarcIssueSeverity.Error)
                .Select(issue => new Common.Models.ApiError(issue.Tag ?? "MarcJson", issue.Message))
                .ToList();

            throw new Common.Exceptions.ValidationException(errors);
        }

        if (isNew)
        {
            entity.Source = request.Source;
            entity.SourceRef = request.SourceRef;
            _db.BibRecords.Add(entity);
        }

        entity.DocumentTypeId = request.DocumentTypeId;
        entity.CarrierTypeId = request.CarrierTypeId;
        entity.Status = request.Status;

        if (request.CoverImageUrl is not null)
        {
            entity.CoverImageUrl = string.IsNullOrWhiteSpace(request.CoverImageUrl)
                ? null
                : request.CoverImageUrl.Trim();
        }

        await _writer.ApplyAsync(entity, marc, isNew, request.ChangeNote, ct);
        SyncCollections(entity, request.CollectionIds);

        await _db.SaveChangesAsync(ct);

        return new SaveBibResultDto
        {
            Id = entity.Id,
            ControlNumber = entity.ControlNumber,
            Title = entity.Title,
            Validation = ValidateMarcRecordCommandHandler.Describe(issues)
        };
    }

    /// <summary>
    /// Bộ sưu tập là lựa chọn của thư viện chứ không rút được từ MARC, nên nó đến từ biểu mẫu và
    /// được đồng bộ riêng.
    /// </summary>
    private static void SyncCollections(BibRecord entity, List<Guid> collectionIds)
    {
        foreach (var link in entity.Collections.Where(link => !collectionIds.Contains(link.CollectionId)).ToList())
        {
            entity.Collections.Remove(link);
        }

        foreach (var id in collectionIds.Where(id => entity.Collections.All(link => link.CollectionId != id)))
        {
            entity.Collections.Add(new BibCollection
            {
                Id = Guid.NewGuid(),
                BibId = entity.Id,
                CollectionId = id
            });
        }
    }
}

/// <summary>
/// Xóa mềm một biểu ghi (II.3). Biểu ghi còn đăng ký cá biệt thì không xóa được.
/// </summary>
public record DeleteBibRecordCommand(Guid Id, string Reason) : IRequest;

public class DeleteBibRecordCommandValidator : AbstractValidator<DeleteBibRecordCommand>
{
    public DeleteBibRecordCommandValidator()
    {
        RuleFor(command => command.Reason)
            .NotEmpty().WithMessage("Phải nhập lý do xóa biểu ghi.")
            .MaximumLength(500).WithMessage("Lý do xóa tối đa 500 ký tự.");
    }
}

public class DeleteBibRecordCommandHandler : IRequestHandler<DeleteBibRecordCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteBibRecordCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(DeleteBibRecordCommand request, CancellationToken ct)
    {
        var bib = await _db.BibRecords.FirstOrDefaultAsync(record => record.Id == request.Id, ct)
                  ?? throw new NotFoundException("Không tìm thấy biểu ghi cần xóa.");

        var items = await _db.Items.CountAsync(item => item.BibId == bib.Id, ct);

        if (items > 0)
        {
            throw new ConflictException(
                $"Biểu ghi này còn {items:N0} đăng ký cá biệt. Hãy thanh lý hoặc chuyển các bản đó sang biểu ghi " +
                "khác trước khi xóa biểu ghi.");
        }

        var digital = await _db.DigitalDocuments.CountAsync(document => document.BibId == bib.Id, ct);

        if (digital > 0)
        {
            throw new ConflictException(
                $"Biểu ghi này còn {digital:N0} tài liệu số đính kèm. Hãy xóa các tài liệu số đó trước.");
        }

        // Xóa biểu ghi thì việc biên mục của nó cũng hết lý do tồn tại. Trước 07/09/2026 dòng việc
        // nằm lại trong hàng đợi: trên máy chủ thật có 45 việc "Chờ xử lý" trỏ tới biểu ghi đã xóa,
        // và vì màn hình hàng đợi đọc bảng công việc chứ không đọc cột trạng thái của biểu ghi
        // (mục A.3 số 2), cán bộ nhìn thấy đủ 45 việc ấy trong bộ đếm mà mở ra thì không có gì.
        var congViec = await _db.CatalogQueue
            .Where(task => task.BibId == bib.Id)
            .ToListAsync(ct);

        if (congViec.Count > 0)
        {
            _db.CatalogQueue.RemoveRange(congViec);
        }

        bib.DeleteReason = request.Reason.Trim();
        _db.BibRecords.Remove(bib);

        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Khôi phục biểu ghi về một phiên bản cũ (II.3).</summary>
public record RestoreBibVersionCommand(Guid BibId, Guid VersionId) : IRequest<SaveBibResultDto>;

public class RestoreBibVersionCommandHandler : IRequestHandler<RestoreBibVersionCommand, SaveBibResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IBibRecordWriter _writer;
    private readonly IMarcRuleProvider _rules;

    public RestoreBibVersionCommandHandler(
        IApplicationDbContext db,
        IBibRecordWriter writer,
        IMarcRuleProvider rules)
    {
        _db = db;
        _writer = writer;
        _rules = rules;
    }

    public async Task<SaveBibResultDto> Handle(RestoreBibVersionCommand request, CancellationToken ct)
    {
        var version = await _db.BibRecordVersions
                          .FirstOrDefaultAsync(item => item.Id == request.VersionId && item.BibId == request.BibId, ct)
                      ?? throw new NotFoundException("Không tìm thấy phiên bản cần khôi phục.");

        var bib = await _db.BibRecords
                      .Include(record => record.Authors)
                      .Include(record => record.Subjects)
                      .Include(record => record.Keywords)
                      .Include(record => record.Classifications)
                      .Include(record => record.Collections)
                      .FirstOrDefaultAsync(record => record.Id == request.BibId, ct)
                  ?? throw new NotFoundException("Không tìm thấy biểu ghi.");

        var marc = MarcJson.Deserialize(version.MarcData);

        // Restoring is an ordinary edit: the version being replaced is snapshotted first, so the
        // restore itself can be undone.
        await _writer.ApplyAsync(
            bib,
            marc,
            isNew: false,
            $"Khôi phục về phiên bản {version.VersionNumber}",
            ct);

        await _db.SaveChangesAsync(ct);

        var validator = await _rules.GetValidatorAsync(ct);

        return new SaveBibResultDto
        {
            Id = bib.Id,
            ControlNumber = bib.ControlNumber,
            Title = bib.Title,
            Validation = ValidateMarcRecordCommandHandler.Describe(validator.Validate(marc))
        };
    }
}
