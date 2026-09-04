using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Quét tệp bằng ClamAV qua giao thức <c>INSTREAM</c> của <c>clamd</c> (mục 6.4). Không dùng thư
/// viện ngoài: giao thức chỉ gồm gửi lệnh <c>zINSTREAM</c>, rồi từng khối dữ liệu có tiền tố độ dài
/// 4 byte big-endian, kết thúc bằng khối độ dài 0; máy chủ trả một dòng chữ.
///
/// Tắt (mặc định) thì trả "sạch" ngay, không mở kết nối nào — thư viện không muốn dựng thêm một
/// container chỉ để quét vẫn chạy được. Bật mà không nối được clamd thì <b>từ chối tệp</b> chứ không
/// im lặng cho qua: đã bật nghĩa là bên vận hành yêu cầu mọi tệp phải được quét.
/// </summary>
public class ClamAvScanner : IVirusScanner
{
    private const int ChunkSize = 64 * 1024;

    private readonly ClamAvOptions _options;
    private readonly ILogger<ClamAvScanner> _logger;

    public ClamAvScanner(IOptions<ClamAvOptions> options, ILogger<ClamAvScanner> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool Enabled => _options.Enabled;

    public async Task<VirusScanResult> ScanAsync(byte[] content, string fileName, CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            return VirusScanResult.Clean;
        }

        string reply;

        try
        {
            reply = await AskClamAvAsync(content, ct);
        }
        catch (Exception ex) when (ex is SocketException or IOException or OperationCanceledException)
        {
            _logger.LogError(ex,
                "Không quét được tệp {FileName}: không kết nối được clamd tại {Host}:{Port}",
                fileName, _options.Host, _options.Port);

            throw new InvalidOperationException(
                "Không kết nối được máy chủ quét virus. Tệp chưa được nhận; hãy báo quản trị hệ thống.", ex);
        }

        // "stream: OK" hoặc "stream: <tên mẫu> FOUND".
        if (reply.EndsWith("OK", StringComparison.Ordinal))
        {
            return VirusScanResult.Clean;
        }

        if (reply.EndsWith("FOUND", StringComparison.Ordinal))
        {
            var signature = reply[..^"FOUND".Length].Trim();
            var colon = signature.IndexOf(':');
            signature = colon >= 0 ? signature[(colon + 1)..].Trim() : signature;

            _logger.LogWarning("Từ chối tệp {FileName}: phát hiện {Signature}", fileName, signature);
            return VirusScanResult.Infected(signature);
        }

        _logger.LogError("clamd trả lời lạ khi quét {FileName}: {Reply}", fileName, reply);
        throw new InvalidOperationException("Máy chủ quét virus trả lời không hiểu được: " + reply);
    }

    private async Task<string> AskClamAvAsync(byte[] content, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        using var client = new TcpClient();
        await client.ConnectAsync(_options.Host, _options.Port, timeout.Token);

        await using var stream = client.GetStream();

        // Lệnh dạng z: kết thúc bằng NUL, để tên mẫu chứa xuống dòng cũng không làm lệch khung.
        await stream.WriteAsync(Encoding.ASCII.GetBytes("zINSTREAM\0"), timeout.Token);

        var header = new byte[4];

        for (var offset = 0; offset < content.Length; offset += ChunkSize)
        {
            var length = Math.Min(ChunkSize, content.Length - offset);
            BinaryPrimitives.WriteInt32BigEndian(header, length);

            await stream.WriteAsync(header, timeout.Token);
            await stream.WriteAsync(content.AsMemory(offset, length), timeout.Token);
        }

        BinaryPrimitives.WriteInt32BigEndian(header, 0);
        await stream.WriteAsync(header, timeout.Token);
        await stream.FlushAsync(timeout.Token);

        using var buffer = new MemoryStream();
        var chunk = new byte[256];
        int read;

        while ((read = await stream.ReadAsync(chunk, timeout.Token)) > 0)
        {
            buffer.Write(chunk, 0, read);
        }

        return Encoding.UTF8.GetString(buffer.ToArray()).TrimEnd('\0', '\n', '\r').Trim();
    }
}
