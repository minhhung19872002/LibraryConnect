using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LibraryConnect.Marc;

/// <summary>
/// Chuyển biểu ghi MARC sang và về JSON — đây là dạng lưu trong cột <c>marc_json</c> kiểu jsonb.
///
/// The shape is the one the specification fixes: leader as a string, controlFields as tag/value
/// pairs, dataFields with ind1/ind2 and an ordered list of subfields. Keeping it stable matters
/// because the database indexes and the search triggers read this JSON directly.
/// </summary>
public static class MarcJson
{
    /// <summary>
    /// Cấu hình dùng chung. Không thoát ký tự Unicode để tiếng Việt trong cơ sở dữ liệu đọc được
    /// bằng mắt thường khi cần kiểm tra bằng psql.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false
    };

    public static string Serialize(MarcRecord record) => JsonSerializer.Serialize(record, Options);

    public static MarcRecord Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new MarcException("Chuỗi JSON của biểu ghi rỗng.");
        }

        try
        {
            return JsonSerializer.Deserialize<MarcRecord>(json, Options)
                   ?? throw new MarcException("Chuỗi JSON của biểu ghi không hợp lệ.");
        }
        catch (JsonException exception)
        {
            throw new MarcException($"Không đọc được JSON của biểu ghi: {exception.Message}", exception);
        }
    }
}

/// <summary>
/// Đọc/ghi <see cref="MarcLeader"/> như một chuỗi 24 ký tự thay vì một đối tượng.
/// </summary>
public class MarcLeaderJsonConverter : JsonConverter<MarcLeader>
{
    public override MarcLeader Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetString() ?? MarcLeader.Default);

    public override void Write(Utf8JsonWriter writer, MarcLeader value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
