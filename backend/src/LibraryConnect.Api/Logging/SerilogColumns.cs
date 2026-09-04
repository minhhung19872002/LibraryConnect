using NpgsqlTypes;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL;

namespace LibraryConnect.Api.Logging;

/// <summary>
/// Cột của bảng <c>sys.serilog_logs</c> — nhật ký kỹ thuật ghi song song với tệp.
///
/// Giữ đúng những cột đọc được bằng mắt khi mở bảng: thời điểm, mức, câu thông báo, ngoại lệ và phần
/// thuộc tính có cấu trúc.
///
/// Khác hẳn <c>sys.audit_logs</c>: bảng ấy là nhật ký **nghiệp vụ** — ai sửa gì, giá trị cũ và mới,
/// theo mục 6.2 — còn bảng này là nhật ký **kỹ thuật**: ngoại lệ, lỗi hạ tầng, cảnh báo của khung
/// nền. Hai thứ không thay nhau được, và bảng kỹ thuật chỉ nhận từ mức Cảnh báo trở lên.
/// </summary>
public static class SerilogColumns
{
    public static readonly IDictionary<string, ColumnWriterBase> Definition =
        new Dictionary<string, ColumnWriterBase>
        {
            ["message"] = new RenderedMessageColumnWriter(NpgsqlDbType.Text),
            ["message_template"] = new MessageTemplateColumnWriter(NpgsqlDbType.Text),
            ["level"] = new LevelColumnWriter(true, NpgsqlDbType.Varchar),
            ["timestamp"] = new UtcTimestampColumnWriter(),
            ["exception"] = new ExceptionColumnWriter(NpgsqlDbType.Text),
            ["properties"] = new LogEventSerializedColumnWriter(NpgsqlDbType.Jsonb),
        };
}

/// <summary>
/// Ghi thời điểm ở dạng UTC.
///
/// Bộ ghi thời gian sẵn có của sink đưa thẳng <see cref="DateTimeOffset"/> theo giờ máy chủ xuống
/// Npgsql, mà Npgsql 8 chỉ nhận độ lệch 0 cho kiểu <c>timestamp with time zone</c>. Máy chủ chạy ở
/// múi giờ +07 nên mỗi lô đều ném — và sink **nuốt lỗi**, nên biểu hiện duy nhất là bảng rỗng mãi.
/// Chỉ nhìn ra khi bật SelfLog của Serilog.
/// </summary>
public class UtcTimestampColumnWriter : ColumnWriterBase
{
    public UtcTimestampColumnWriter() : base(NpgsqlDbType.TimestampTz)
    {
    }

    public override object GetValue(LogEvent logEvent, IFormatProvider? formatProvider = null) =>
        logEvent.Timestamp.UtcDateTime;
}
