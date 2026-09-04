using LibraryConnect.Application.Common.Interfaces;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Bộ quét virus giả cho kiểm thử: mặc định tắt (mọi tệp sạch, đúng như bản cài không dựng ClamAV).
/// Bật <see cref="RejectAll"/> để mọi tệp bị coi là nhiễm — thử đường từ chối mà không cần một
/// container ClamAV thật và không cần tệp EICAR nào trong kho mã.
/// </summary>
public sealed class StubVirusScanner : IVirusScanner
{
    public bool Enabled { get; set; }

    /// <summary>Tên mẫu trả về khi <see cref="RejectAll"/> bật.</summary>
    public string Signature { get; set; } = "Eicar-Test-Signature";

    public bool RejectAll { get; set; }

    /// <summary>Số lượt quét đã chạy — chứng minh cổng vào thật sự gọi bộ quét.</summary>
    public int Scans { get; private set; }

    public Task<VirusScanResult> ScanAsync(byte[] content, string fileName, CancellationToken ct = default)
    {
        Scans++;
        return Task.FromResult(RejectAll ? VirusScanResult.Infected(Signature) : VirusScanResult.Clean);
    }
}
