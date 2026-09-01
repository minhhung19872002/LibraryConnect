namespace LibraryConnect.Marc.Oai;

/// <summary>
/// Mã nhận dạng kho nguồn, dùng cho trường 035$a và 040$a của biểu ghi thu hoạch về.
///
/// MARC 21 quy định 035$a viết theo dạng <c>(mã cơ quan)số kiểm soát</c>. Bản trước ghi
/// <c>(OAI)oai:localhost:DHTL/623</c>, sai cả hai vế: <c>OAI</c> là tên giao thức chứ không phải
/// tên cơ quan nào, còn <c>localhost</c> là do kho nguồn khai sai địa chỉ của chính họ trong định
/// danh. Nhìn vào biểu ghi không biết nó từ thư viện nào — mất hẳn khả năng truy vết, mà truy vết
/// lại chính là lý do trường 035 tồn tại.
///
/// Thư viện Việt Nam phần lớn chưa đăng ký mã cơ quan MARC với Thư viện Quốc hội Mỹ, nên không có
/// mã dạng <c>VN-XXXXX</c> để dùng. Tên máy của kho (<c>tailieuso.tlu.edu.vn</c>) là thứ nhận dạng
/// bền và tra ngược được — đúng tinh thần của trường này.
/// </summary>
public static class OaiSourceCode
{
    /// <summary>Mã nhận dạng của một kho OAI-PMH, suy từ địa chỉ của kho.</summary>
    /// <param name="baseUrl">Địa chỉ endpoint OAI-PMH của kho nguồn.</param>
    /// <param name="fallbackName">Tên kho do cán bộ khai, dùng khi địa chỉ không đọc được.</param>
    public static string ForRepository(string? baseUrl, string? fallbackName = null)
    {
        if (!string.IsNullOrWhiteSpace(baseUrl)
            && Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var uri)
            && !string.IsNullOrWhiteSpace(uri.Host))
        {
            return uri.Host.ToLowerInvariant();
        }

        return string.IsNullOrWhiteSpace(fallbackName) ? "khong-ro-nguon" : fallbackName.Trim();
    }

    /// <summary>
    /// Giá trị của 035$a: <c>(mã kho nguồn)định danh gốc</c>.
    ///
    /// Định danh OAI-PMH có dạng <c>oai:tên-máy:mã-bản-ghi</c>. Phần tên máy trong đó do kho nguồn
    /// tự khai và nhiều kho khai sai (<c>localhost</c>), nên bỏ đi và thay bằng tên máy thật lấy từ
    /// địa chỉ mình đang gọi tới. Phần mã bản ghi giữ nguyên — đó mới là thứ tra ngược được bên kho
    /// nguồn, và nó vẫn được lưu đủ cả ở cột <c>source_ref</c> để lần thu hoạch sau nhận ra biểu
    /// ghi đã lấy rồi.
    /// </summary>
    public static string SystemControlNumber(
        string? baseUrl, string identifier, string? fallbackName = null)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        return $"({ForRepository(baseUrl, fallbackName)}){MaBanGhi(identifier)}";
    }

    /// <summary>Phần mã bản ghi trong một định danh OAI-PMH.</summary>
    public static string MaBanGhi(string identifier)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        var value = identifier.Trim();

        if (!value.StartsWith("oai:", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        // oai:tên-máy:mã-bản-ghi — lấy phần sau dấu hai chấm thứ hai. Mã bản ghi của một số kho có
        // chứa dấu hai chấm nữa, nên chỉ cắt đúng hai lần.
        var parts = value.Split(':', 3);

        return parts.Length == 3 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2] : value;
    }
}
