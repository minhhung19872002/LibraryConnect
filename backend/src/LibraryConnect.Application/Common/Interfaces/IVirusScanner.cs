namespace LibraryConnect.Application.Common.Interfaces;

/// <summary>Kết quả quét: sạch, hoặc nhiễm kèm tên mẫu mà bộ quét nhận ra.</summary>
public record VirusScanResult(bool IsClean, string? Signature)
{
    public static readonly VirusScanResult Clean = new(true, null);

    public static VirusScanResult Infected(string signature) => new(false, signature);
}

/// <summary>
/// Quét tệp người dùng tải lên trước khi ghi vào kho đối tượng (mục 6.4 — "quét virus, ClamAV tùy
/// chọn"). Tắt thì mọi tệp coi như sạch; bật thì tệp nhiễm bị từ chối ngay ở cổng vào, không bao giờ
/// nằm trong MinIO.
/// </summary>
public interface IVirusScanner
{
    /// <summary>Bộ quét có đang bật không — dùng để báo trạng thái, không phải để bỏ qua lượt quét.</summary>
    bool Enabled { get; }

    Task<VirusScanResult> ScanAsync(byte[] content, string fileName, CancellationToken ct = default);
}
