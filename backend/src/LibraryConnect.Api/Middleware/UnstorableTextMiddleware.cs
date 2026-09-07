using System.Text.Json;
using System.Text.Json.Serialization;
using LibraryConnect.Application.Common.Text;
using Microsoft.AspNetCore.Http.Extensions;

namespace LibraryConnect.Api.Middleware;

/// <summary>
/// Bỏ ký tự điều khiển mà PostgreSQL không lưu được ra khỏi **mọi** chuỗi đi vào hệ thống.
///
/// Cột kiểu text của PostgreSQL không chứa được ký tự rỗng U+0000: máy chủ trả về
/// <c>22021: invalid byte sequence for encoding "UTF8": 0x00</c> ngay ở tầng trình điều khiển, tức là
/// sau khi câu truy vấn đã gửi đi — không bộ kiểm tra nào của tầng nghiệp vụ chặn kịp, và người dùng
/// nhận 500 "Đã xảy ra lỗi hệ thống".
///
/// Ngày 07/09/2026, một ký tự rỗng trong từ khóa làm **bảy** endpoint đổ, trong đó có ba lối công
/// khai không cần đăng nhập: <c>/api/search</c>, <c>/api/search/suggest</c>, <c>/api/search/facets</c>
/// và <c>/api/public/news</c>. <see cref="PlainText.RemoveUnstorableCharacters"/> đã có sẵn từ trước
/// và làm đúng việc ấy — nhưng chỉ được gọi ở một đường duy nhất, lớp chữ rút từ tệp PDF.
///
/// Hệ thống chỉ có hai cửa cho chuỗi của người dùng, nên lọc ở đúng hai chỗ này là đủ cho mọi
/// endpoint, kể cả endpoint viết sau này:
///   1. Chuỗi truy vấn trên địa chỉ — phần lớp trung gian này lo.
///   2. Chuỗi trong thân JSON — <see cref="UnstorableTextJsonConverter"/> lo, gắn vào bộ đọc JSON.
///
/// Không từ chối bằng 400: ký tự rỗng không mang nghĩa gì trong một từ khóa hay một cái tên, nên bỏ
/// nó đi là làm đúng thứ máy khách định nói. Ký tự xuống dòng và tab được giữ vì chúng là bố cục thật.
/// </summary>
public class UnstorableTextMiddleware
{
    private readonly RequestDelegate _next;

    public UnstorableTextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var query = context.Request.Query;

        if (query.Count > 0 && CanBan(query))
        {
            var sach = new QueryBuilder();

            foreach (var cap in query)
            {
                foreach (var giaTri in cap.Value)
                {
                    sach.Add(
                        PlainText.RemoveUnstorableCharacters(cap.Key),
                        PlainText.RemoveUnstorableCharacters(giaTri));
                }
            }

            context.Request.QueryString = sach.ToQueryString();
        }

        await _next(context);
    }

    /// <summary>Đọc lướt trước: gần như mọi yêu cầu đều sạch, không dựng lại chuỗi truy vấn vô ích.</summary>
    private static bool CanBan(IQueryCollection query)
    {
        foreach (var cap in query)
        {
            if (Ban(cap.Key))
            {
                return true;
            }

            foreach (var giaTri in cap.Value)
            {
                if (Ban(giaTri))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool Ban(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        foreach (var character in value)
        {
            if (char.IsControl(character) && character is not ('\n' or '\r' or '\t'))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Cùng bộ lọc ấy, đặt ở bộ đọc JSON: mọi thuộc tính kiểu chuỗi của mọi thân yêu cầu đi qua đây.
///
/// Chỉ lọc lúc **đọc**. Lúc ghi ra phản hồi thì trả nguyên văn — dữ liệu đã nằm trong cơ sở dữ liệu
/// thì không còn ký tự cấm nào để bỏ, và can thiệp vào đường ghi là tự thêm một chỗ dễ sai.
/// </summary>
public class UnstorableTextJsonConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();

        return value is null ? null : PlainText.RemoveUnstorableCharacters(value);
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}
