using System.Net.Sockets;
using System.Text;

namespace LibraryConnect.Marc.Z3950;

/// <summary>Thông số kết nối tới một máy chủ Z39.50.</summary>
public class Z3950ConnectionOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 210;
    public string DatabaseName { get; set; } = "Default";
    public string? Username { get; set; }
    public string? Password { get; set; }

    /// <summary>UTF-8 | MARC-8 | ISO-8859-1 — cách máy chủ đích mã hóa nội dung biểu ghi.</summary>
    public string Charset { get; set; } = "UTF-8";

    /// <summary>USMARC | MARC21 | UNIMARC | XML.</summary>
    public string RecordSyntax { get; set; } = "USMARC";

    public int TimeoutSeconds { get; set; } = 20;

    /// <summary>Tên phần mềm tự khai khi bắt tay, theo đúng mục 0.1 của đặc tả.</summary>
    public string ImplementationName { get; set; } = "LibraryConnect";

    public string ImplementationVersion { get; set; } = "1.0";
}

/// <summary>Kết quả một lần tra cứu.</summary>
public class Z3950SearchResult
{
    public int TotalHits { get; init; }
    public List<MarcRecord> Records { get; init; } = new();
    public List<Z3950Diagnostic> Diagnostics { get; init; } = new();

    /// <summary>Những biểu ghi lấy về nhưng không đọc được thành MARC, giữ lại để soi khi cần.</summary>
    public List<Z3950Record> RawRecords { get; init; } = new();
}

/// <summary>
/// Máy khách Z39.50 (mục 3.3a) — mở kết nối TCP tới thư viện khác, bắt tay, tra cứu và lấy biểu ghi.
///
/// Vòng đời một phiên đúng như đặc tả: Init để thỏa thuận, Search để hỏi có bao nhiêu kết quả,
/// Present để lấy về từng biểu ghi, Close để đóng lịch sự. Mỗi APDU là một phần tử BER gửi thẳng
/// trên luồng TCP, không có khung bao ngoài, nên phía nhận phải đọc đủ độ dài mà thẻ BER khai.
/// </summary>
public class Z3950Client : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Số biểu ghi xin mỗi lượt.
    ///
    /// Máy chủ thật giới hạn kích thước bản tin, và mỗi nơi một mức. Năm biểu ghi là mức mọi máy
    /// chủ thử nghiệm đều chịu được; xin nhiều hơn thì có nơi từ chối cả lô bằng một mã lỗi trơ.
    /// </summary>
    private const int RecordBatchSize = 5;

    private readonly Z3950ConnectionOptions _options;
    private TcpClient? _tcp;
    private NetworkStream? _stream;
    private int _referenceId;

    public Z3950Client(Z3950ConnectionOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>Tên và phiên bản phần mềm phía máy chủ, biết được sau khi bắt tay.</summary>
    public string? ServerImplementationName { get; private set; }

    public string? ServerImplementationVersion { get; private set; }

    /// <summary>Kích thước bản tin tối đa máy chủ chấp nhận.</summary>
    public int NegotiatedMaximumRecordSize { get; private set; } = 1024 * 1024;

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        _tcp = new TcpClient
        {
            ReceiveTimeout = _options.TimeoutSeconds * 1000,
            SendTimeout = _options.TimeoutSeconds * 1000,
        };

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        try
        {
            await _tcp.ConnectAsync(_options.Host, _options.Port, timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new Z3950Exception(
                $"Không kết nối được tới {_options.Host}:{_options.Port} trong {_options.TimeoutSeconds} giây.");
        }
        catch (SocketException ex)
        {
            throw new Z3950Exception(
                $"Không kết nối được tới {_options.Host}:{_options.Port}: {ex.Message}", ex);
        }

        _stream = _tcp.GetStream();

        await InitializeAsync(ct);
    }

    private async Task InitializeAsync(CancellationToken ct)
    {
        var children = new List<BerElement>
        {
            BerElement.Integer(BerTagClass.Context, 2, ++_referenceId),

            // protocolVersion và options đều là BIT STRING: byte đầu đếm số bit thừa ở cuối, các
            // byte sau mang bit theo thứ tự từ trái sang.
            //
            // protocolVersion: bật bit 1 và 2 nghĩa là nói được cả phiên bản 2 lẫn 3.
            BerElement.Primitive(BerTagClass.Context, 3, new byte[] { 0x05, 0x60 }),

            // options: phải bật đúng những việc mình định làm. Bit 0 là search, bit 1 là present,
            // bit 14 là namedResultSets. Khai thiếu search và present thì máy chủ hiểu là máy khách
            // này không định tra cứu gì, và đóng kết nối ngay sau khi bắt tay.
            BerElement.Primitive(BerTagClass.Context, 4, new byte[] { 0x01, 0xC0, 0x02 }),
            BerElement.Integer(BerTagClass.Context, 5, NegotiatedMaximumRecordSize),
            BerElement.Integer(BerTagClass.Context, 6, NegotiatedMaximumRecordSize),
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            // IdAuthentication ::= CHOICE { open [0] VisibleString, idPass [1] SEQUENCE {...} }
            children.Add(BerElement.Constructed(
                BerTagClass.Context, 7,
                BerElement.Constructed(
                    BerTagClass.Context, 1,
                    BerElement.String(BerTagClass.Context, 0, _options.Username),
                    BerElement.String(BerTagClass.Context, 2, _options.Password ?? string.Empty))));
        }

        children.Add(BerElement.String(BerTagClass.Context, 110, _options.ImplementationName));
        children.Add(BerElement.String(BerTagClass.Context, 111, _options.ImplementationVersion));

        var request = BerElement.Constructed(
            BerTagClass.Context, Z3950Constants.InitRequest, children);

        var response = await ExchangeAsync(request, ct);

        if (response.TagNumber != Z3950Constants.InitResponse)
        {
            throw new Z3950Exception(
                $"Máy chủ trả về APDU {response.TagNumber} thay vì InitResponse.");
        }

        ServerImplementationName = response.Child(110)?.AsString();
        ServerImplementationVersion = response.Child(111)?.AsString();

        var size = response.Child(6);

        if (size is not null)
        {
            NegotiatedMaximumRecordSize = (int)Math.Min(size.AsInteger(), int.MaxValue);
        }

        var accepted = response.Child(12);

        if (accepted is not null && !accepted.AsBoolean())
        {
            var reason = response.Child(103)?.AsString();

            throw new Z3950Exception(
                $"Máy chủ từ chối phiên làm việc{(reason is null ? "." : $": {reason}")}");
        }
    }

    /// <summary>Tra cứu rồi lấy về tối đa <paramref name="maxRecords"/> biểu ghi đầu tiên.</summary>
    public async Task<Z3950SearchResult> SearchAsync(
        RpnQuery query, int maxRecords = 20, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        EnsureConnected();

        var resultSetName = "default";
        var wanted = Math.Max(0, maxRecords);

        // Ba con số dưới đây nói với máy chủ rằng đừng gửi kèm biểu ghi nào trong bản trả lời tra
        // cứu: ngưỡng "tập lớn" đặt bằng 1 nên mọi kết quả đều rơi vào diện lấy riêng. Thử cho máy
        // chủ gửi kèm thì nhiều nơi từ chối cả lô bằng một mã lỗi trơ, còn lấy riêng theo lô thì
        // nơi nào cũng chịu.
        var request = BerElement.Constructed(
            BerTagClass.Context, Z3950Constants.SearchRequest,
            BerElement.Integer(BerTagClass.Context, 2, ++_referenceId),
            BerElement.Integer(BerTagClass.Context, 13, 0),
            BerElement.Integer(BerTagClass.Context, 14, 1),
            BerElement.Integer(BerTagClass.Context, 15, 0),
            // [16] là replaceIndicator (đồng ý ghi đè tập kết quả cùng tên), [17] mới là tên tập
            // kết quả. Nhét tên vào ô 16 thì máy chủ đọc ra một giá trị luận lý vô nghĩa và ngắt.
            BerElement.Boolean(BerTagClass.Context, 16, true),
            BerElement.Primitive(BerTagClass.Context, 17, Encoding.ASCII.GetBytes(resultSetName)),
            // databaseNames ::= [18] IMPLICIT SEQUENCE OF DatabaseName, mà
            // DatabaseName ::= [105] IMPLICIT InternationalString — từng tên mang thẻ riêng chứ
            // không phải chuỗi trơn.
            BerElement.Constructed(
                BerTagClass.Context, 18,
                BerElement.Primitive(
                    BerTagClass.Context, 105, Encoding.UTF8.GetBytes(_options.DatabaseName))),
            BerElement.ObjectIdentifier(BerTagClass.Context, 104, SyntaxOid()),
            query.ToBer());

        var response = await ExchangeAsync(request, ct);

        if (response.TagNumber != Z3950Constants.SearchResponse)
        {
            throw new Z3950Exception($"Máy chủ trả về APDU {response.TagNumber} thay vì SearchResponse.");
        }

        var hits = (int)(response.Child(23)?.AsInteger() ?? 0);

        var result = new Z3950SearchResult { TotalHits = hits };
        result.Diagnostics.AddRange(CollectDiagnostics(response.Child(130)));

        ReadRecords(response.Child(28), result);

        var target = Math.Min(hits, wanted);

        // Xin nốt phần còn thiếu theo từng lô nhỏ. Máy chủ thật đều có giới hạn số biểu ghi mỗi bản
        // tin — xin một lượt hai chục cái là bị từ chối cả lô, mà lời từ chối không nói vì sao.
        while (result.Records.Count < target)
        {
            var batch = Math.Min(RecordBatchSize, target - result.Records.Count);
            var present = await PresentAsync(resultSetName, result.Records.Count + 1, batch, ct);

            result.RawRecords.AddRange(present.RawRecords);

            if (present.Records.Count == 0)
            {
                // Hết biểu ghi lấy được, hoặc máy chủ từ chối: giữ những gì đã có kèm lời chẩn đoán
                // thay vì trả về tay không.
                result.Diagnostics.AddRange(present.Diagnostics);
                break;
            }

            result.Records.AddRange(present.Records);
        }

        return new Z3950SearchResult
        {
            TotalHits = hits,
            Records = result.Records,
            RawRecords = result.RawRecords,
            Diagnostics = result.Diagnostics,
        };
    }

    /// <summary>Lấy về một khoảng biểu ghi trong tập kết quả đã có trên máy chủ.</summary>
    public async Task<Z3950SearchResult> PresentAsync(
        string resultSetName, int start, int count, CancellationToken ct = default)
    {
        EnsureConnected();

        var request = BerElement.Constructed(
            BerTagClass.Context, Z3950Constants.PresentRequest,
            BerElement.Integer(BerTagClass.Context, 2, ++_referenceId),
            BerElement.Primitive(BerTagClass.Context, 31, Encoding.ASCII.GetBytes(resultSetName)),
            BerElement.Integer(BerTagClass.Context, 30, start),
            BerElement.Integer(BerTagClass.Context, 29, count),
            // recordComposition ::= CHOICE { simple [19] ElementSetNames, ... } và
            // ElementSetNames ::= CHOICE { genericElementSetName [0] InternationalString, ... }
            BerElement.Constructed(
                BerTagClass.Context, 19,
                BerElement.Primitive(
                    BerTagClass.Context, 0,
                    Encoding.ASCII.GetBytes(Z3950Constants.ElementSetFull))),
            BerElement.ObjectIdentifier(BerTagClass.Context, 104, SyntaxOid()));

        var response = await ExchangeAsync(request, ct);

        if (response.TagNumber != Z3950Constants.PresentResponse)
        {
            throw new Z3950Exception($"Máy chủ trả về APDU {response.TagNumber} thay vì PresentResponse.");
        }

        var result = new Z3950SearchResult
        {
            TotalHits = (int)(response.Child(24)?.AsInteger() ?? 0),
        };

        ReadRecords(response.Child(28), result);

        if (result.Records.Count == 0 && result.Diagnostics.Count == 0)
        {
            result.Diagnostics.AddRange(CollectDiagnostics(response.Child(130)));
        }

        return result;
    }

    /// <summary>
    /// Đọc danh sách biểu ghi trong một bản trả lời.
    ///
    /// Records ::= CHOICE { responseRecords [0] SEQUENCE OF NamePlusRecord,
    ///                      nonSurrogateDiagnostic [1], multipleNonSurDiagnostics [2] }
    /// </summary>
    private void ReadRecords(BerElement? records, Z3950SearchResult result)
    {
        if (records is null)
        {
            return;
        }

        if (records.Children.Count >= 1 && records.Children[0].TagNumber is 1 or 2
            && records.Children[0].TagClass == BerTagClass.Context)
        {
            result.Diagnostics.AddRange(CollectDiagnostics(records.Children[0]));
            return;
        }

        foreach (var namePlusRecord in records.Children.SelectMany(Flatten))
        {
            ReadRecord(namePlusRecord, result);
        }
    }

    /// <summary>Đóng phiên một cách lịch sự để máy chủ giải phóng tập kết quả.</summary>
    public async Task CloseAsync(CancellationToken ct = default)
    {
        if (_stream is null)
        {
            return;
        }

        try
        {
            var request = BerElement.Constructed(
                BerTagClass.Context, Z3950Constants.Close,
                BerElement.Integer(BerTagClass.Context, 2, ++_referenceId),
                BerElement.Integer(BerTagClass.Context, 211, 0),
                BerElement.String(BerTagClass.Context, 3, "Kết thúc phiên."));

            var bytes = request.ToBytes();
            await _stream.WriteAsync(bytes, ct);
            await _stream.FlushAsync(ct);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or SocketException)
        {
            // Máy chủ đóng trước thì thôi; đây chỉ là phép lịch sự cuối phiên.
        }
    }

    // ---------------------------------------------------------------------------------------------

    private static IEnumerable<BerElement> Flatten(BerElement element) =>
        // Tùy máy chủ, danh sách biểu ghi có thể nằm thêm một lớp SEQUENCE nữa.
        element.TagNumber == 0 && element.TagClass == BerTagClass.Context
            ? element.Children
            : new[] { element };

    private void ReadRecord(BerElement namePlusRecord, Z3950SearchResult result)
    {
        // NamePlusRecord ::= SEQUENCE { name [0] optional, record [1] CHOICE {
        //     retrievalRecord [1] EXTERNAL, surrogateDiagnostic [2] } }
        var wrapper = namePlusRecord.Child(1) ?? namePlusRecord;
        var diagnostic = wrapper.Child(2);

        if (diagnostic is not null)
        {
            result.Diagnostics.AddRange(CollectDiagnostics(diagnostic));
            return;
        }

        var external = wrapper.Child(1) ?? wrapper;

        // EXTERNAL ::= [UNIVERSAL 8] SEQUENCE { direct-reference OID, encoding CHOICE {
        //     single-ASN1-type [0], octet-aligned [1] IMPLICIT OCTET STRING, ... } }
        var payload = FindOctets(external);

        if (payload is null || payload.Length == 0)
        {
            return;
        }

        var syntax = external.Children
            .FirstOrDefault(child => child is { TagClass: BerTagClass.Universal, TagNumber: 6 })
            ?.AsOid() ?? _options.RecordSyntax;

        result.RawRecords.Add(new Z3950Record(syntax, payload));

        try
        {
            // Nhiều thư viện Mỹ vẫn phát biểu ghi theo bảng mã MARC-8; bộ đọc ISO 2709 đã biết
            // chuyển sang Unicode nên chỉ cần nói cho nó biết máy chủ này dùng bảng mã nào.
            var charset = _options.Charset.ToUpperInvariant() switch
            {
                "MARC-8" or "MARC8" => MarcCharset.Marc8,
                "UTF-8" or "UTF8" => MarcCharset.Utf8,
                _ => MarcCharset.Auto,
            };

            result.Records.Add(Iso2709Reader.Read(payload, charset));
        }
        catch (Exception ex) when (ex is MarcException or BerException or ArgumentException)
        {
            result.Diagnostics.Add(new Z3950Diagnostic(
                0, $"Biểu ghi lấy về không đọc được theo ISO 2709: {ex.Message}"));
        }
    }

    /// <summary>Tìm chuỗi byte thật của biểu ghi trong lớp vỏ EXTERNAL, dù nó lồng mấy tầng.</summary>
    private static byte[]? FindOctets(BerElement element)
    {
        if (!element.IsConstructed)
        {
            return element.Content.Length > 0 ? element.Content : null;
        }

        foreach (var child in element.Children)
        {
            if (child is { TagClass: BerTagClass.Universal, TagNumber: 6 })
            {
                continue;
            }

            var found = FindOctets(child);

            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static List<Z3950Diagnostic> CollectDiagnostics(BerElement? element)
    {
        var diagnostics = new List<Z3950Diagnostic>();

        if (element is null)
        {
            return diagnostics;
        }

        foreach (var record in Walk(element))
        {
            var code = record.Children
                .FirstOrDefault(child => child is { TagClass: BerTagClass.Universal, TagNumber: 2 });

            if (code is null)
            {
                continue;
            }

            var message = record.Children
                .FirstOrDefault(child => child.TagClass == BerTagClass.Context && child.TagNumber is 2 or 3)
                ?.AsString();

            diagnostics.Add(new Z3950Diagnostic((int)code.AsInteger(), message));
        }

        return diagnostics;
    }

    private static IEnumerable<BerElement> Walk(BerElement element)
    {
        yield return element;

        foreach (var child in element.Children.SelectMany(Walk))
        {
            yield return child;
        }
    }

    private string SyntaxOid() => _options.RecordSyntax.ToUpperInvariant() switch
    {
        "UNIMARC" => Z3950Constants.UnimarcOid,
        "XML" or "MARCXML" => Z3950Constants.XmlOid,
        "SUTRS" => Z3950Constants.SutrsOid,
        _ => Z3950Constants.UsmarcOid,
    };

    private void EnsureConnected()
    {
        if (_stream is null)
        {
            throw new Z3950Exception("Chưa mở kết nối tới máy chủ Z39.50.");
        }
    }

    private async Task<BerElement> ExchangeAsync(BerElement request, CancellationToken ct)
    {
        EnsureConnected();

        var bytes = request.ToBytes();

        await _stream!.WriteAsync(bytes, ct);
        await _stream.FlushAsync(ct);

        return await Z3950Framing.ReadApduAsync(_stream, _options.TimeoutSeconds, ct);
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _tcp?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();

        if (_stream is not null)
        {
            await _stream.DisposeAsync();
        }

        _tcp?.Dispose();
        GC.SuppressFinalize(this);
    }
}

public class Z3950Exception : Exception
{
    public Z3950Exception(string message) : base(message) { }

    public Z3950Exception(string message, Exception inner) : base(message, inner) { }
}
