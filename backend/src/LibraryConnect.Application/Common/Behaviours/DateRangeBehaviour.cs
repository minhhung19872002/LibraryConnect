using System.Reflection;
using LibraryConnect.Application.Common.Models;
using MediatR;

namespace LibraryConnect.Application.Common.Behaviours;

/// <summary>
/// Chặn khoảng thời gian ngược ở mọi lệnh và mọi truy vấn có hai ô ngày.
///
/// Chọn nhầm hai ô ngày là chuyện thường — hai ô lịch cạnh nhau, bấm nhanh là đảo. Trước
/// 07/09/2026 tám màn hình nhận khoảng ngày ngược rồi trả **bảng rỗng, mã 200, không một lời nào**:
/// cán bộ đọc ra "kỳ này thư viện không có dữ liệu" và đi báo cáo đúng như thế.
///
/// Đặt ở đường ống thay vì ở từng bộ xử lý vì đây là một **lớp** lỗi: mười sáu bộ lọc có cặp ô
/// ngày hôm nay, và mỗi bộ lọc thêm vào ngày mai là một cơ hội quên (bài học 30, 69). Chỉ soi
/// những cặp tên đã biết, trên chính đối tượng yêu cầu và trên thuộc tính <c>Filter</c> của nó —
/// đủ phủ mọi chỗ đang có mà không đoán mò.
/// </summary>
public class DateRangeBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>Những cặp ô ngày mà sản phẩm đang dùng, theo đúng tên thuộc tính.</summary>
    private static readonly (string Dau, string Cuoi)[] Cap =
    {
        ("FromDate", "ToDate"),
        ("From", "To"),
        ("CreatedFrom", "CreatedTo"),
        ("AcquiredFrom", "AcquiredTo"),
        ("UploadedFrom", "UploadedTo"),
        ("ExpectedFrom", "ExpectedTo"),
        ("SubscriptionFrom", "SubscriptionTo"),
    };

    public Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        Soi(request);

        var filter = request.GetType()
            .GetProperty("Filter", BindingFlags.Public | BindingFlags.Instance)?
            .GetValue(request);

        if (filter is not null)
        {
            Soi(filter);
        }

        var nested = request.GetType()
            .GetProperty("Request", BindingFlags.Public | BindingFlags.Instance)?
            .GetValue(request);

        if (nested is not null)
        {
            Soi(nested);

            var nestedFilter = nested.GetType()
                .GetProperty("Filter", BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(nested);

            if (nestedFilter is not null)
            {
                Soi(nestedFilter);
            }
        }

        return next();
    }

    private static void Soi(object target)
    {
        var type = target.GetType();

        foreach (var (tenDau, tenCuoi) in Cap)
        {
            var dau = type.GetProperty(tenDau, BindingFlags.Public | BindingFlags.Instance);
            var cuoi = type.GetProperty(tenCuoi, BindingFlags.Public | BindingFlags.Instance);

            if (dau is null || cuoi is null)
            {
                continue;
            }

            var giaTriDau = dau.GetValue(target);
            var giaTriCuoi = cuoi.GetValue(target);

            var ten = char.ToLowerInvariant(tenDau[0]) + tenDau[1..];

            if (giaTriDau is DateOnly a && giaTriCuoi is DateOnly b)
            {
                DateRangeRules.EnsureOrdered(a, b, ten);
            }
            else if (giaTriDau is DateTimeOffset c && giaTriCuoi is DateTimeOffset d)
            {
                DateRangeRules.EnsureOrdered(c, d, ten);
            }
        }
    }
}
