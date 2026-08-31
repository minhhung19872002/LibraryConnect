using System.Globalization;
using System.Text.Json;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Marc;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Cataloging;

/// <summary>Khung biểu ghi mới trả về cho trình soạn thảo.</summary>
public class NewBibRecordDto
{
    public string MarcJson { get; set; } = string.Empty;
    public Guid? TemplateId { get; set; }
    public string? TemplateName { get; set; }
    public Guid? DocumentTypeId { get; set; }
    /// <summary>Số trường đã được điền sẵn từ bảng giá trị ngầm định.</summary>
    public int AppliedDefaults { get; set; }
}

/// <summary>
/// Dựng khung biểu ghi mới theo mẫu biên mục và bảng giá trị ngầm định (II.1, II.2).
/// </summary>
public record GetNewBibRecordQuery(Guid? DocumentTypeId = null, Guid? TemplateId = null)
    : IRequest<NewBibRecordDto>;

public class GetNewBibRecordQueryHandler : IRequestHandler<GetNewBibRecordQuery, NewBibRecordDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ISystemParameterService _parameters;
    private readonly IDateTimeProvider _clock;

    public GetNewBibRecordQueryHandler(
        IApplicationDbContext db,
        ISystemParameterService parameters,
        IDateTimeProvider clock)
    {
        _db = db;
        _parameters = parameters;
        _clock = clock;
    }

    public async Task<NewBibRecordDto> Handle(GetNewBibRecordQuery query, CancellationToken ct)
    {
        var template = await FindTemplateAsync(query, ct);
        var documentType = query.DocumentTypeId is null
            ? null
            : await _db.DocumentTypes.AsNoTracking()
                .FirstOrDefaultAsync(type => type.Id == query.DocumentTypeId, ct);

        var record = template is null ? new MarcRecord() : BuildFromTemplate(template);

        // The document type carries the two leader positions that decide how every other system
        // interprets the record, so they are set before anything else can be layered on.
        if (!string.IsNullOrEmpty(documentType?.MarcTypeOfRecord))
        {
            record.Leader.RecordType = documentType.MarcTypeOfRecord[0];
        }

        if (!string.IsNullOrEmpty(documentType?.MarcBibLevel))
        {
            record.Leader.BibliographicLevel = documentType.MarcBibLevel[0];
        }

        record.Leader.RecordStatus = 'n';

        await EnsureFixedFieldAsync(record, ct);
        var applied = await ApplyDefaultsAsync(record, query.DocumentTypeId, ct);

        EnsureTitleField(record);

        // Fields added by the default-value rules land at the end; a cataloguer reads the record top
        // to bottom and expects 040 above 245. The sort is stable, so repeated fields keep the order
        // the template put them in.
        record.DataFields = record.DataFields
            .OrderBy(field => field.Tag, StringComparer.Ordinal)
            .ToList();

        return new NewBibRecordDto
        {
            MarcJson = MarcJson.Serialize(record),
            TemplateId = template?.Id,
            TemplateName = template?.Name,
            DocumentTypeId = query.DocumentTypeId,
            AppliedDefaults = applied
        };
    }

    private async Task<MarcTemplate?> FindTemplateAsync(GetNewBibRecordQuery query, CancellationToken ct)
    {
        if (query.TemplateId is not null)
        {
            return await _db.MarcTemplates.AsNoTracking()
                .FirstOrDefaultAsync(template => template.Id == query.TemplateId, ct);
        }

        var candidates = _db.MarcTemplates.AsNoTracking()
            .Where(template => template.IsActive && template.IsDefault);

        if (query.DocumentTypeId is null)
        {
            // No document type chosen yet: hand back the library's default template rather than an
            // empty skeleton, so a cataloguer who just wants to start typing sees the usual fields.
            return await candidates
                .OrderBy(template => template.DocumentTypeId == null ? 0 : 1)
                .ThenBy(template => template.Name)
                .FirstOrDefaultAsync(ct);
        }

        // With one chosen: its own default template, else the general one, else no template.
        return await candidates
            .Where(template => template.DocumentTypeId == query.DocumentTypeId || template.DocumentTypeId == null)
            .OrderByDescending(template => template.DocumentTypeId == query.DocumentTypeId)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Cấu hình đọc khung mẫu.
    ///
    /// Case-insensitive on purpose: the template JSON is written by the editor, by the seeder and by
    /// hand, and a template silently producing an empty skeleton because a key was capitalised
    /// differently is a failure a cataloguer cannot diagnose.
    /// </summary>
    private static readonly JsonSerializerOptions TemplateOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Đọc khung trường của mẫu biên mục. Mẫu lưu dạng JSON có hình dạng giống danh sách trường dữ
    /// liệu của một biểu ghi, chỉ khác là các giá trị để trống.
    /// </summary>
    private static MarcRecord BuildFromTemplate(MarcTemplate template)
    {
        var record = new MarcRecord();

        if (string.IsNullOrWhiteSpace(template.Fields))
        {
            return record;
        }

        try
        {
            var fields = JsonSerializer.Deserialize<List<TemplateField>>(template.Fields, TemplateOptions)
                         ?? new List<TemplateField>();

            foreach (var field in fields.Where(item => !string.IsNullOrWhiteSpace(item.Tag)))
            {
                if (MarcConstants.IsControlFieldTag(field.Tag))
                {
                    record.SetControlField(field.Tag, field.Value ?? string.Empty);
                    continue;
                }

                var data = record.AddField(
                    field.Tag,
                    string.IsNullOrEmpty(field.Ind1) ? ' ' : field.Ind1[0],
                    string.IsNullOrEmpty(field.Ind2) ? ' ' : field.Ind2[0]);

                foreach (var subfield in field.Subfields.Where(item => !string.IsNullOrWhiteSpace(item.Code)))
                {
                    data.AddSubfield(subfield.Code[0], subfield.Value ?? string.Empty);
                }

                if (data.Subfields.Count == 0)
                {
                    data.AddSubfield('a', string.Empty);
                }
            }
        }
        catch (JsonException)
        {
            // A template whose JSON has been corrupted should not stop a cataloguer from starting a
            // record; they get the plain skeleton and the template can be repaired separately.
            return new MarcRecord();
        }

        return record;
    }

    /// <summary>
    /// Bảo đảm biểu ghi có trường 008 đủ 40 ký tự, đã điền ngày tạo, năm, mã nước và mã ngôn ngữ
    /// theo tham số hệ thống.
    /// </summary>
    private Task EnsureFixedFieldAsync(MarcRecord record, CancellationToken ct) =>
        Marc008Builder.EnsureAsync(record, _parameters, _clock.Today, ct: ct);

    /// <summary>
    /// Áp bảng giá trị ngầm định lên biểu ghi. Giá trị của dạng tài liệu cụ thể ghi đè lên giá trị
    /// chung, và một trường đã có nội dung từ mẫu biên mục thì không bị ghi đè.
    /// </summary>
    private async Task<int> ApplyDefaultsAsync(MarcRecord record, Guid? documentTypeId, CancellationToken ct)
    {
        var defaults = await _db.MarcFieldDefaults
            .AsNoTracking()
            .Where(item => item.IsActive && (item.DocumentTypeId == null || item.DocumentTypeId == documentTypeId))
            // General rules first so the ones for this document type can overwrite them.
            .OrderBy(item => item.DocumentTypeId == null ? 0 : 1)
            .ThenBy(item => item.SortOrder)
            .ToListAsync(ct);

        var applied = 0;

        foreach (var rule in defaults)
        {
            var value = await ResolveValueAsync(rule, ct);

            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            if (MarcConstants.IsControlFieldTag(rule.Tag))
            {
                ApplyToControlField(record, rule, value);
                applied++;
                continue;
            }

            if (string.IsNullOrEmpty(rule.Subfield))
            {
                continue;
            }

            ApplyToDataField(record, rule, value);
            applied++;
        }

        return applied;
    }

    private async Task<string?> ResolveValueAsync(MarcFieldDefault rule, CancellationToken ct) =>
        string.IsNullOrWhiteSpace(rule.ParameterKey)
            ? rule.DefaultValue
            : await _parameters.GetAsync(rule.ParameterKey, rule.DefaultValue ?? string.Empty, ct);

    private static void ApplyToControlField(MarcRecord record, MarcFieldDefault rule, string value)
    {
        var position = rule.Position ?? 0;
        var length = rule.Length ?? value.Length;
        var current = (record.GetControlField(rule.Tag) ?? string.Empty)
            .PadRight(position + length, ' ')
            .ToCharArray();

        var text = value.PadRight(length, ' ')[..length];

        for (var index = 0; index < length; index++)
        {
            current[position + index] = text[index];
        }

        record.SetControlField(rule.Tag, new string(current));
    }

    private static void ApplyToDataField(MarcRecord record, MarcFieldDefault rule, string value)
    {
        var code = rule.Subfield![0];
        var field = record.GetFields(rule.Tag).FirstOrDefault();

        if (field is null)
        {
            field = record.AddField(
                rule.Tag,
                string.IsNullOrEmpty(rule.Ind1) ? ' ' : rule.Ind1[0],
                string.IsNullOrEmpty(rule.Ind2) ? ' ' : rule.Ind2[0]);
        }
        else
        {
            if (!string.IsNullOrEmpty(rule.Ind1))
            {
                field.Indicator1 = rule.Ind1[0];
            }

            if (!string.IsNullOrEmpty(rule.Ind2))
            {
                field.Indicator2 = rule.Ind2[0];
            }
        }

        var subfield = field.Subfields.FirstOrDefault(item => item.Code == code);

        if (subfield is null)
        {
            field.AddSubfield(code, value);
            return;
        }

        // A value typed into the template stays; the default only fills an empty box.
        if (string.IsNullOrWhiteSpace(subfield.Value))
        {
            subfield.Value = value;
        }
    }

    /// <summary>Mọi biểu ghi đều phải có trường 245, nên trình soạn thảo luôn mở sẵn nó.</summary>
    private static void EnsureTitleField(MarcRecord record)
    {
        if (record.GetField("245") is null)
        {
            record.AddField("245", '1', '0').AddSubfield('a', string.Empty);
        }
    }

    /// <summary>Hình dạng một trường trong JSON của mẫu biên mục.</summary>
    private class TemplateField
    {
        public string Tag { get; set; } = string.Empty;
        public string? Ind1 { get; set; }
        public string? Ind2 { get; set; }
        public string? Value { get; set; }
        public List<TemplateSubfield> Subfields { get; set; } = new();
    }

    private class TemplateSubfield
    {
        public string Code { get; set; } = string.Empty;
        public string? Value { get; set; }
    }
}
