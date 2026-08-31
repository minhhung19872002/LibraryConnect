using System.Net;
using System.Net.Sockets;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Application.Features.InterLibrary;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Marc;
using LibraryConnect.Marc.Z3950;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Nguồn dữ liệu cho máy chủ Z39.50: tra thẳng vào kho thư mục của thư viện mình.
///
/// Cùng một cách tra không dấu như mọi màn hình khác, nên thư viện bạn gõ "co so du lieu" vẫn ra
/// đúng sách tiếng Việt.
/// </summary>
public class BibZ3950Catalog : IZ3950Catalog
{
    private readonly IApplicationDbContext _db;

    public BibZ3950Catalog(IApplicationDbContext db) => _db = db;

    public string DatabaseName { get; set; } = "LibraryConnect";

    public Task<int> CountAsync(Z3950ParsedQuery query, CancellationToken ct) =>
        Apply(query).CountAsync(ct);

    public async Task<IReadOnlyList<Z3950ServerRecord>> FetchAsync(
        Z3950ParsedQuery query, int start, int count, CancellationToken ct)
    {
        var records = await Apply(query)
            .OrderBy(bib => bib.Title)
            .Skip(Math.Max(0, start - 1))
            .Take(Math.Clamp(count, 0, 100))
            .ToListAsync(ct);

        return records
            .Select(bib =>
            {
                var marc = BibMarcReader.Read(bib);

                return new Z3950ServerRecord(
                    bib.ControlNumber ?? bib.Id.ToString("N"), Iso2709Writer.Write(marc));
            })
            .ToList();
    }

    private IQueryable<BibRecord> Apply(Z3950ParsedQuery query)
    {
        var source = _db.BibRecords.AsNoTracking().AsQueryable();

        foreach (var clause in query.Clauses)
        {
            var term = VietnameseText.RemoveDiacritics(clause.Term.Trim()).ToLowerInvariant();
            var raw = clause.Term.Trim();

            source = clause.Use switch
            {
                Bib1Use.Title => source.Where(bib =>
                    DatabaseFunctions.Unaccent(bib.Title).Contains(term)),
                Bib1Use.PersonalName or Bib1Use.CorporateName => source.Where(bib =>
                    bib.AuthorMain != null
                    && DatabaseFunctions.Unaccent(bib.AuthorMain).Contains(term)),
                Bib1Use.Isbn => source.Where(bib => bib.Isbn != null && bib.Isbn.Contains(raw)),
                Bib1Use.Issn => source.Where(bib => bib.Issn != null && bib.Issn.Contains(raw)),
                Bib1Use.Publisher => source.Where(bib =>
                    bib.PublisherName != null
                    && DatabaseFunctions.Unaccent(bib.PublisherName).Contains(term)),
                Bib1Use.Subject => source.Where(bib =>
                    bib.Abstract != null && DatabaseFunctions.Unaccent(bib.Abstract).Contains(term)),
                _ => source.Where(bib =>
                    DatabaseFunctions.Unaccent(bib.Title).Contains(term)
                    || (bib.AuthorMain != null
                        && DatabaseFunctions.Unaccent(bib.AuthorMain).Contains(term))
                    || (bib.Isbn != null && bib.Isbn.Contains(raw))),
            };
        }

        return source;
    }
}

/// <summary>
/// Máy chủ Z39.50 của thư viện mình (mục 3.3b) — lắng nghe TCP để thư viện khác tra sang.
///
/// Bật tắt và giới hạn dải IP bằng tham số hệ thống, vì đây là cổng duy nhất của sản phẩm mở ra
/// bên ngoài mà không có mật khẩu: chuẩn Z39.50 vốn dùng cho tra cứu công khai.
/// </summary>
public class Z3950ServerHost : BackgroundService
{
    public const string EnabledParameter = "ILL.Z3950_SERVER_ENABLED";
    public const string PortParameter = "ILL.Z3950_SERVER_PORT";
    public const string DatabaseParameter = "ILL.Z3950_DATABASE_NAME";
    public const string AllowedIpsParameter = "ILL.Z3950_ALLOWED_IPS";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Z3950ServerHost> _logger;
    private TcpListener? _listener;

    public Z3950ServerHost(IServiceScopeFactory scopeFactory, ILogger<Z3950ServerHost> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Chờ một nhịp cho cơ sở dữ liệu chạy migration xong rồi mới đọc tham số.
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        bool enabled;
        int port;
        string database;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var parameters = scope.ServiceProvider.GetRequiredService<ISystemParameterService>();

            enabled = await parameters.GetAsync(EnabledParameter, false, stoppingToken);
            port = await parameters.GetAsync(PortParameter, 210, stoppingToken);
            database = await parameters.GetAsync(DatabaseParameter, "LibraryConnect", stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Không đọc được tham số máy chủ Z39.50, tạm thời không bật.");
            return;
        }

        if (!enabled)
        {
            _logger.LogInformation(
                "Máy chủ Z39.50 đang tắt. Bật bằng tham số {Parameter}.", EnabledParameter);
            return;
        }

        try
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
        }
        catch (SocketException ex)
        {
            // Cổng 210 dưới 1024 nên trên Linux cần quyền riêng; nói rõ ra thay vì chết lặng.
            _logger.LogError(ex,
                "Không mở được cổng {Port} cho máy chủ Z39.50. Cổng dưới 1024 cần quyền đặc biệt; "
                + "hãy đổi {Parameter} sang cổng lớn hơn.", port, PortParameter);
            return;
        }

        _logger.LogInformation(
            "Máy chủ Z39.50 đang lắng nghe cổng {Port}, cơ sở dữ liệu '{Database}'.", port, database);

        while (!stoppingToken.IsCancellationRequested)
        {
            TcpClient client;

            try
            {
                client = await _listener.AcceptTcpClientAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException ex)
            {
                _logger.LogWarning(ex, "Lỗi khi nhận kết nối Z39.50.");
                continue;
            }

            _ = HandleClientAsync(client, database, stoppingToken);
        }

        _listener.Stop();
    }

    private async Task HandleClientAsync(TcpClient client, string database, CancellationToken ct)
    {
        var remote = (client.Client.RemoteEndPoint as IPEndPoint)?.Address;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var parameters = scope.ServiceProvider.GetRequiredService<ISystemParameterService>();

            var allowed = await parameters.GetAsync(AllowedIpsParameter, string.Empty, ct);

            if (!IsAllowed(remote, allowed))
            {
                _logger.LogWarning("Từ chối kết nối Z39.50 từ {Address}: ngoài dải cho phép.", remote);
                client.Close();
                return;
            }

            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var catalog = new BibZ3950Catalog(db) { DatabaseName = database };
            var session = new Z3950ServerSession(catalog);

            await using var stream = client.GetStream();

            while (!ct.IsCancellationRequested && !session.Closed)
            {
                BerElement request;

                try
                {
                    request = await Z3950Framing.ReadApduAsync(stream, 120, ct);
                }
                catch (Z3950Exception)
                {
                    // Máy khách ngắt hoặc im lặng quá lâu: đóng phiên, không phải lỗi hệ thống.
                    break;
                }

                var response = await session.HandleAsync(request, ct);

                if (response is null)
                {
                    continue;
                }

                var bytes = response.ToBytes();
                await stream.WriteAsync(bytes, ct);
                await stream.FlushAsync(ct);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Phiên Z39.50 từ {Address} kết thúc vì lỗi.", remote);
        }
        finally
        {
            client.Dispose();
        }
    }

    /// <summary>
    /// Kiểm tra địa chỉ có nằm trong dải cho phép không.
    ///
    /// Danh sách để trống nghĩa là mở cho mọi nơi — đúng tinh thần tra cứu công khai của Z39.50,
    /// nhưng thư viện muốn siết thì khai từng địa chỉ hoặc tiền tố, ví dụ "203.113." .
    /// </summary>
    internal static bool IsAllowed(IPAddress? address, string allowedList)
    {
        if (string.IsNullOrWhiteSpace(allowedList))
        {
            return true;
        }

        if (address is null)
        {
            return false;
        }

        var text = address.IsIPv4MappedToIPv6 ? address.MapToIPv4().ToString() : address.ToString();

        return allowedList
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(entry => text.Equals(entry, StringComparison.OrdinalIgnoreCase)
                || text.StartsWith(entry, StringComparison.OrdinalIgnoreCase));
    }

    public override void Dispose()
    {
        _listener?.Stop();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
