using System.Text;

namespace LibraryConnect.Marc.Z3950;

/// <summary>Một mệnh đề tìm kiếm mà máy chủ đã hiểu ra từ cây RPN nhận được.</summary>
public record Z3950SearchClause(Bib1Use Use, string Term, Bib1Relation Relation);

/// <summary>
/// Truy vấn đã giải mã: danh sách mệnh đề cùng toán tử nối chúng.
///
/// Giữ ở dạng phẳng vì mọi máy khách trên thực tế chỉ gửi tổ hợp AND hoặc OR đơn giản; nhánh lồng
/// nhiều tầng vẫn duyệt được nhưng sẽ nối bằng cùng một toán tử.
/// </summary>
public class Z3950ParsedQuery
{
    public List<Z3950SearchClause> Clauses { get; } = new();

    public RpnOperator Operator { get; set; } = RpnOperator.And;
}

/// <summary>Biểu ghi máy chủ trả về, kèm số kiểm soát để máy khách đối chiếu.</summary>
public record Z3950ServerRecord(string ControlNumber, byte[] Iso2709);

/// <summary>Nguồn dữ liệu mà máy chủ Z39.50 tra vào — do tầng ứng dụng cài đặt.</summary>
public interface IZ3950Catalog
{
    /// <summary>Tên cơ sở dữ liệu mà máy chủ này phục vụ, ví dụ "LibraryConnect".</summary>
    string DatabaseName { get; }

    /// <summary>Đếm số biểu ghi khớp truy vấn.</summary>
    Task<int> CountAsync(Z3950ParsedQuery query, CancellationToken ct);

    /// <summary>Lấy một khoảng biểu ghi, đánh số từ 1.</summary>
    Task<IReadOnlyList<Z3950ServerRecord>> FetchAsync(
        Z3950ParsedQuery query, int start, int count, CancellationToken ct);
}

/// <summary>
/// Xử lý một phiên Z39.50 phía máy chủ (mục 3.3b): Init, Search, Present, Close.
///
/// Tách khỏi phần lắng nghe TCP để kiểm thử được bằng cách bơm thẳng byte vào, không cần mở cổng.
/// Tập kết quả giữ trong phiên chứ không lưu ra ngoài: phiên đóng là quên, đúng như cách các máy
/// chủ Z39.50 khác vẫn làm.
/// </summary>
public class Z3950ServerSession
{
    private readonly IZ3950Catalog _catalog;
    private readonly string _implementationName;
    private readonly Dictionary<string, Z3950ParsedQuery> _resultSets = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _resultCounts = new(StringComparer.Ordinal);

    public Z3950ServerSession(IZ3950Catalog catalog, string implementationName = "LibraryConnect")
    {
        _catalog = catalog;
        _implementationName = implementationName;
    }

    /// <summary>Phiên đã nhận lệnh Close chưa — phía lắng nghe dựa vào đây để ngắt kết nối.</summary>
    public bool Closed { get; private set; }

    /// <summary>Nhận một APDU, trả về APDU đáp lại, hoặc null nếu không cần đáp.</summary>
    public async Task<BerElement?> HandleAsync(BerElement request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.TagNumber switch
        {
            Z3950Constants.InitRequest => HandleInit(request),
            Z3950Constants.SearchRequest => await HandleSearchAsync(request, ct),
            Z3950Constants.PresentRequest => await HandlePresentAsync(request, ct),
            Z3950Constants.Close => HandleClose(request),
            _ => Diagnostic(request, 108, $"Không hỗ trợ APDU {request.TagNumber}."),
        };
    }

    private BerElement HandleInit(BerElement request)
    {
        var reference = request.Child(2);

        var children = new List<BerElement>();

        if (reference is not null)
        {
            children.Add(reference);
        }

        children.AddRange(new[]
        {
            BerElement.Primitive(BerTagClass.Context, 3, new byte[] { 0x05, 0x60 }),
            BerElement.Primitive(BerTagClass.Context, 4, new byte[] { 0x01, 0x36 }),
            BerElement.Integer(BerTagClass.Context, 5, 1024 * 1024),
            BerElement.Integer(BerTagClass.Context, 6, 1024 * 1024),
            BerElement.Boolean(BerTagClass.Context, 12, true),
            BerElement.String(BerTagClass.Context, 110, _implementationName),
            BerElement.String(BerTagClass.Context, 111, "1.0"),
        });

        return BerElement.Constructed(BerTagClass.Context, Z3950Constants.InitResponse, children);
    }

    private async Task<BerElement> HandleSearchAsync(BerElement request, CancellationToken ct)
    {
        var reference = request.Child(2);
        var resultSetName = ReadString(request.Child(17)) ?? "default";

        var database = request.Child(18)?.Children.FirstOrDefault();
        var databaseName = ReadString(database);

        if (databaseName is not null
            && !databaseName.Equals(_catalog.DatabaseName, StringComparison.OrdinalIgnoreCase)
            && !databaseName.Equals("Default", StringComparison.OrdinalIgnoreCase))
        {
            return SearchFailure(reference, 109, $"Không có cơ sở dữ liệu '{databaseName}'.");
        }

        Z3950ParsedQuery query;

        try
        {
            query = ParseQuery(request.Child(21))
                ?? throw new BerException("Thiếu phần truy vấn trong SearchRequest.");
        }
        catch (BerException ex)
        {
            return SearchFailure(reference, 3, ex.Message);
        }

        if (query.Clauses.Count == 0)
        {
            return SearchFailure(reference, 3, "Truy vấn không có mệnh đề nào hiểu được.");
        }

        var count = await _catalog.CountAsync(query, ct);

        _resultSets[resultSetName] = query;
        _resultCounts[resultSetName] = count;

        var children = new List<BerElement>();

        if (reference is not null)
        {
            children.Add(reference);
        }

        // Thứ tự và số hiệu theo đúng đặc tả: [23] số kết quả, [24] số biểu ghi trả kèm,
        // [25] vị trí tiếp theo, [22] tra cứu thành công hay không.
        children.AddRange(new[]
        {
            BerElement.Integer(BerTagClass.Context, 23, count),
            BerElement.Integer(BerTagClass.Context, 24, 0),
            BerElement.Integer(BerTagClass.Context, 25, 1),
            BerElement.Boolean(BerTagClass.Context, 22, true),
        });

        return BerElement.Constructed(BerTagClass.Context, Z3950Constants.SearchResponse, children);
    }

    private async Task<BerElement> HandlePresentAsync(BerElement request, CancellationToken ct)
    {
        var reference = request.Child(2);
        var resultSetName = ReadString(request.Child(31)) ?? "default";
        var start = (int)(request.Child(30)?.AsInteger() ?? 1);
        var count = (int)(request.Child(29)?.AsInteger() ?? 1);

        if (!_resultSets.TryGetValue(resultSetName, out var query))
        {
            return PresentFailure(reference, 30, $"Không có tập kết quả tên '{resultSetName}'.");
        }

        var total = _resultCounts.GetValueOrDefault(resultSetName);

        if (start < 1 || (total > 0 && start > total))
        {
            return PresentFailure(reference, 13, "Vị trí bắt đầu nằm ngoài tập kết quả.");
        }

        var records = await _catalog.FetchAsync(query, start, Math.Max(0, count), ct);

        var namePlusRecords = records.Select(record =>
            BerElement.Constructed(
                BerTagClass.Universal, 16,
                BerElement.Primitive(
                    BerTagClass.Context, 0,
                    System.Text.Encoding.UTF8.GetBytes(_catalog.DatabaseName)),
                BerElement.Constructed(
                    BerTagClass.Context, 1,
                    BerElement.Constructed(
                        BerTagClass.Universal, 8,
                        BerElement.ObjectIdentifier(BerTagClass.Universal, 6, Z3950Constants.UsmarcOid),
                        BerElement.Primitive(BerTagClass.Context, 1, record.Iso2709)))));

        var children = new List<BerElement>();

        if (reference is not null)
        {
            children.Add(reference);
        }

        children.Add(BerElement.Integer(BerTagClass.Context, 24, records.Count));
        children.Add(BerElement.Integer(BerTagClass.Context, 25, start + records.Count));
        // [27] presentStatus: 0 nghĩa là trả đủ, 2 nghĩa là không có biểu ghi nào.
        children.Add(BerElement.Integer(BerTagClass.Context, 27, records.Count == 0 ? 2 : 0));
        children.Add(BerElement.Constructed(
            BerTagClass.Context, 28,
            BerElement.Constructed(BerTagClass.Context, 0, namePlusRecords)));

        return BerElement.Constructed(BerTagClass.Context, Z3950Constants.PresentResponse, children);
    }

    private BerElement? HandleClose(BerElement request)
    {
        Closed = true;

        var reference = request.Child(2);
        var children = new List<BerElement>();

        if (reference is not null)
        {
            children.Add(reference);
        }

        children.Add(BerElement.Integer(BerTagClass.Context, 211, 0));

        return BerElement.Constructed(BerTagClass.Context, Z3950Constants.Close, children);
    }

    // ---------------------------------------------------------------------------------------------

    /// <summary>Giải mã cây RPN thành danh sách mệnh đề mà tầng dữ liệu hiểu được.</summary>
    public static Z3950ParsedQuery? ParseQuery(BerElement? query)
    {
        if (query is null)
        {
            return null;
        }

        // Query [21] → type-1 [1] → SEQUENCE { attributeSet OID, rpn }
        var typeOne = query.Child(1) ?? query;
        var parsed = new Z3950ParsedQuery();

        foreach (var child in typeOne.Children)
        {
            if (child is { TagClass: BerTagClass.Universal, TagNumber: 6 })
            {
                continue;
            }

            Walk(child, parsed);
        }

        return parsed;
    }

    private static void Walk(BerElement node, Z3950ParsedQuery parsed)
    {
        if (node.TagClass != BerTagClass.Context)
        {
            foreach (var child in node.Children)
            {
                Walk(child, parsed);
            }

            return;
        }

        switch (node.TagNumber)
        {
            case 0:
                ReadOperand(node, parsed);
                break;

            case 1:
                foreach (var child in node.Children)
                {
                    if (child is { TagClass: BerTagClass.Context, TagNumber: 46 })
                    {
                        var op = child.Children.FirstOrDefault();

                        parsed.Operator = op?.TagNumber switch
                        {
                            1 => RpnOperator.Or,
                            2 => RpnOperator.AndNot,
                            _ => RpnOperator.And,
                        };

                        continue;
                    }

                    Walk(child, parsed);
                }

                break;

            default:
                foreach (var child in node.Children)
                {
                    Walk(child, parsed);
                }

                break;
        }
    }

    private static void ReadOperand(BerElement operand, Z3950ParsedQuery parsed)
    {
        var attributesPlusTerm = operand.Child(102) ?? operand;

        var use = Bib1Use.Any;
        var relation = Bib1Relation.Equal;
        string? term = null;

        foreach (var child in attributesPlusTerm.Children)
        {
            if (child is { TagClass: BerTagClass.Context, TagNumber: 45 })
            {
                term = Encoding.UTF8.GetString(child.Content);
                continue;
            }

            // AttributeList là SEQUENCE OF AttributeElement.
            foreach (var attribute in child.Children)
            {
                var type = attribute.Child(120)?.AsInteger();
                var value = attribute.Child(121)?.AsInteger();

                if (type is null || value is null)
                {
                    continue;
                }

                if (type == 1)
                {
                    use = (Bib1Use)value.Value;
                }
                else if (type == 2)
                {
                    relation = (Bib1Relation)value.Value;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            parsed.Clauses.Add(new Z3950SearchClause(use, term, relation));
        }
    }

    private static string? ReadString(BerElement? element) =>
        element is null ? null : Encoding.UTF8.GetString(element.Content);

    private static BerElement SearchFailure(BerElement? reference, int code, string message)
    {
        var children = new List<BerElement>();

        if (reference is not null)
        {
            children.Add(reference);
        }

        children.AddRange(new[]
        {
            BerElement.Integer(BerTagClass.Context, 23, 0),
            BerElement.Integer(BerTagClass.Context, 24, 0),
            BerElement.Integer(BerTagClass.Context, 25, 1),
            // searchStatus false: tra cứu không thành công, lý do nằm ở phần chẩn đoán.
            BerElement.Boolean(BerTagClass.Context, 22, false),
            DiagnosticRecords(code, message),
        });

        return BerElement.Constructed(BerTagClass.Context, Z3950Constants.SearchResponse, children);
    }

    private static BerElement PresentFailure(BerElement? reference, int code, string message)
    {
        var children = new List<BerElement>();

        if (reference is not null)
        {
            children.Add(reference);
        }

        children.AddRange(new[]
        {
            BerElement.Integer(BerTagClass.Context, 24, 0),
            BerElement.Integer(BerTagClass.Context, 25, 0),
            // [27] presentStatus 5: hỏng, lý do nằm ở phần chẩn đoán.
            BerElement.Integer(BerTagClass.Context, 27, 5),
            DiagnosticRecords(code, message),
        });

        return BerElement.Constructed(BerTagClass.Context, Z3950Constants.PresentResponse, children);
    }

    private static BerElement Diagnostic(BerElement request, int code, string message)
    {
        var reference = request.Child(2);
        return SearchFailure(reference, code, message);
    }

    /// <summary>Records [28] → nonSurrogateDiagnostic [1] → DefaultDiagFormat.</summary>
    private static BerElement DiagnosticRecords(int code, string message) =>
        BerElement.Constructed(
            BerTagClass.Context, 28,
            BerElement.Constructed(
                BerTagClass.Context, 1,
                BerElement.Constructed(
                    BerTagClass.Universal, 8,
                    BerElement.ObjectIdentifier(
                        BerTagClass.Universal, 6, Z3950Constants.Bib1AttributeSetOid),
                    BerElement.Constructed(
                        BerTagClass.Context, 0,
                        BerElement.Constructed(
                            BerTagClass.Universal, 16,
                            BerElement.ObjectIdentifier(
                                BerTagClass.Universal, 6, Z3950Constants.Bib1AttributeSetOid),
                            BerElement.Integer(BerTagClass.Universal, 2, code),
                            BerElement.String(BerTagClass.Context, 2, message))))));
}
