namespace LibraryConnect.Marc.Z3950;

/// <summary>
/// Cắt luồng TCP thành từng APDU.
///
/// Z39.50 gửi thẳng phần tử BER lên socket, không có khung bao ngoài và không có ký tự kết thúc.
/// Muốn biết một bản tin đã về đủ chưa thì phải đọc phần thẻ và độ dài trước, rồi mới biết còn phải
/// chờ bao nhiêu byte. Đọc thiếu một byte là cả phần tử hỏng, đọc thừa là lệch sang bản tin sau —
/// nên cả máy khách lẫn máy chủ đều dùng chung đúng đoạn mã này.
/// </summary>
public static class Z3950Framing
{
    /// <summary>Đọc trọn một APDU từ luồng, chờ tối đa <paramref name="timeoutSeconds"/> giây.</summary>
    public static async Task<BerElement> ReadApduAsync(
        Stream stream, int timeoutSeconds, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        var buffer = new List<byte>();
        var chunk = new byte[8192];

        while (true)
        {
            int read;

            try
            {
                read = await stream.ReadAsync(chunk, timeout.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new Z3950Exception($"Không nhận được bản tin trong {timeoutSeconds} giây.");
            }

            if (read == 0)
            {
                throw new Z3950Exception("Phía bên kia đóng kết nối giữa chừng.");
            }

            buffer.AddRange(chunk.Take(read));

            var data = buffer.ToArray();

            // Bản tin khai độ dài rõ ràng thì đếm byte là biết đã đủ chưa — đường nhanh, không phải
            // phân tích lại mỗi lần nhận thêm.
            if (TryPeekLength(data, out var total))
            {
                if (data.Length < total)
                {
                    continue;
                }

                var offset = 0;
                return BerElement.Read(data, ref offset);
            }

            // Còn bản tin dùng độ dài không xác định — YAZ và nhiều máy chủ thật gửi kiểu này — thì
            // không có cách nào biết đã đủ ngoài việc thử phân tích: hai byte 0x00 kết thúc trông
            // hệt như dữ liệu bên trong một phần tử con, dò byte là cắt nhầm giữa bản tin.
            try
            {
                var offset = 0;
                return BerElement.Read(data, ref offset);
            }
            catch (BerException)
            {
                // Chưa đủ byte để dựng lại cây; đọc tiếp cho tới khi đủ hoặc hết thời gian chờ.
            }
        }
    }

    /// <summary>
    /// Đọc thử thẻ và độ dài để biết cả APDU dài bao nhiêu byte.
    ///
    /// Trả về false khi dữ liệu nhận được còn chưa đủ để biết độ dài — khi đó cứ đọc tiếp.
    /// </summary>
    public static bool TryPeekLength(byte[] data, out int totalLength)
    {
        ArgumentNullException.ThrowIfNull(data);

        totalLength = 0;

        if (data.Length < 2)
        {
            return false;
        }

        var offset = 0;
        var identifier = data[offset++];

        if ((identifier & 0x1F) == 0x1F)
        {
            while (offset < data.Length && (data[offset] & 0x80) != 0)
            {
                offset++;
            }

            if (offset >= data.Length)
            {
                return false;
            }

            offset++;
        }

        if (offset >= data.Length)
        {
            return false;
        }

        var first = data[offset++];

        if ((first & 0x80) == 0)
        {
            totalLength = offset + first;
            return true;
        }

        var count = first & 0x7F;

        if (count == 0)
        {
            // Độ dài không xác định: không đếm trước được, phía gọi phải thử phân tích.
            return false;
        }

        if (offset + count > data.Length)
        {
            return false;
        }

        var length = 0;

        for (var index = 0; index < count; index++)
        {
            length = (length << 8) | data[offset++];
        }

        totalLength = offset + length;
        return true;
    }
}
