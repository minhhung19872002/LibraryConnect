using LibraryConnect.Domain.Entities.Rdr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Persistence.Seeding;

public partial class DatabaseSeeder
{
    /// <summary>
    /// Nạp mẫu thẻ bạn đọc mặc định (VI.2).
    ///
    /// A library must be able to print a working card on day one rather than design one first, so a
    /// complete CR80 layout ships with the system: 85,6 × 54 mm — the size of every plastic card
    /// printer and every card holder — with the library name band, the photo slot, the identity
    /// fields and a Code 128 barcode of the card number that the circulation desk and the entrance
    /// gate both scan. The librarian edits it in Bạn đọc → Mẫu thẻ.
    /// </summary>
    private async Task SeedReaderCardTemplateAsync(CancellationToken ct)
    {
        if (await _db.ReaderCardTemplates.AnyAsync(ct))
        {
            return;
        }

        _db.ReaderCardTemplates.Add(new ReaderCardTemplate
        {
            Code = "THE-CR80",
            Name = "Thẻ bạn đọc CR80 (85,6 × 54 mm)",
            WidthMm = 85.6,
            HeightMm = 54,
            CardsPerPage = 10,
            IsDefault = true,
            IsActive = true,
            PrintBack = true,
            CreatedAt = _clock.Now,
            FrontLayout = Json(new
            {
                backgroundColor = "#FFFFFF",
                headerBandHeight = 9.0,
                headerBandColor = "#0D47A1",
                images = new object[]
                {
                    new { kind = "photo", x = 4.0, y = 13.0, width = 22.0, height = 28.0, border = true },
                    new { kind = "logo", x = 2.0, y = 1.5, width = 6.0, height = 6.0, border = false }
                },
                barcode = new
                {
                    x = 30.0, y = 41.5, width = 40.0, height = 8.0,
                    type = "Code128", showText = true, fontSize = 5.5
                },
                boxes = new object[]
                {
                    new
                    {
                        source = "libraryName",
                        x = 9.0, y = 2.0, width = 74.0, height = 5.0,
                        fontSize = 8.0, bold = true, italic = false, uppercase = true,
                        align = "center", color = "#FFFFFF", border = false
                    },
                    new
                    {
                        source = "\"THẺ BẠN ĐỌC\"",
                        x = 30.0, y = 10.5, width = 52.0, height = 5.0,
                        fontSize = 9.0, bold = true, italic = false, uppercase = false,
                        align = "center", color = "#0D47A1", border = false
                    },
                    new
                    {
                        source = "fullName",
                        x = 30.0, y = 17.0, width = 52.0, height = 5.5,
                        fontSize = 10.0, bold = true, italic = false, uppercase = true,
                        align = "left", border = false
                    },
                    new
                    {
                        source = "studentCode", prefix = "Mã số: ",
                        x = 30.0, y = 23.0, width = 52.0, height = 4.5,
                        fontSize = 7.5, bold = false, italic = false, align = "left", border = false
                    },
                    new
                    {
                        source = "readerTypeName", prefix = "Đối tượng: ",
                        x = 30.0, y = 27.0, width = 52.0, height = 4.5,
                        fontSize = 7.5, bold = false, italic = false, align = "left", border = false
                    },
                    new
                    {
                        source = "className", prefix = "Lớp: ",
                        x = 30.0, y = 31.0, width = 25.0, height = 4.5,
                        fontSize = 7.5, bold = false, italic = false, align = "left", border = false
                    },
                    new
                    {
                        source = "courseYear", prefix = "Khóa: ",
                        x = 56.0, y = 31.0, width = 26.0, height = 4.5,
                        fontSize = 7.5, bold = false, italic = false, align = "left", border = false
                    },
                    new
                    {
                        source = "cardExpireDate", prefix = "Hạn dùng: ",
                        x = 30.0, y = 35.0, width = 52.0, height = 4.5,
                        fontSize = 7.5, bold = true, italic = false, align = "left", border = false
                    },
                    new
                    {
                        source = "facultyName",
                        x = 3.0, y = 42.0, width = 25.0, height = 8.0,
                        fontSize = 6.0, bold = false, italic = true, align = "center", border = false
                    }
                }
            }),
            BackLayout = Json(new
            {
                backgroundColor = "#FFFFFF",
                headerBandHeight = 0.0,
                images = System.Array.Empty<object>(),
                boxes = new object[]
                {
                    new
                    {
                        source = "\"QUY ĐỊNH SỬ DỤNG THẺ\"",
                        x = 4.0, y = 4.0, width = 77.0, height = 5.0,
                        fontSize = 8.0, bold = true, italic = false, uppercase = false,
                        align = "center", color = "#0D47A1", border = false
                    },
                    new
                    {
                        source = "\"1. Thẻ chỉ có giá trị với người được cấp, không cho mượn lại.\"",
                        x = 4.0, y = 11.0, width = 77.0, height = 4.0,
                        fontSize = 6.5, bold = false, italic = false, align = "left", border = false
                    },
                    new
                    {
                        source = "\"2. Xuất trình thẻ khi vào thư viện và khi mượn, trả tài liệu.\"",
                        x = 4.0, y = 15.0, width = 77.0, height = 4.0,
                        fontSize = 6.5, bold = false, italic = false, align = "left", border = false
                    },
                    new
                    {
                        source = "\"3. Mất thẻ phải báo ngay cho thư viện để khóa và cấp lại.\"",
                        x = 4.0, y = 19.0, width = 77.0, height = 4.0,
                        fontSize = 6.5, bold = false, italic = false, align = "left", border = false
                    },
                    new
                    {
                        source = "libraryAddress", prefix = "Địa chỉ: ",
                        x = 4.0, y = 27.0, width = 77.0, height = 4.5,
                        fontSize = 6.5, bold = false, italic = false, align = "left", border = false
                    },
                    new
                    {
                        source = "libraryPhone", prefix = "Điện thoại: ",
                        x = 4.0, y = 31.0, width = 77.0, height = 4.5,
                        fontSize = 6.5, bold = false, italic = false, align = "left", border = false
                    },
                    new
                    {
                        source = "cardNumber", prefix = "Số thẻ: ",
                        x = 4.0, y = 44.0, width = 40.0, height = 5.0,
                        fontSize = 7.5, bold = true, italic = false, align = "left", border = false
                    }
                }
            })
        });

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Đã nạp mẫu thẻ bạn đọc mặc định");
    }
}
