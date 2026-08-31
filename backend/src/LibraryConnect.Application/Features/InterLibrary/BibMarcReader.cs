using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Marc;

namespace LibraryConnect.Application.Features.InterLibrary;

/// <summary>
/// Đọc biểu ghi MARC của một bản ghi thư mục để phát ra ngoài (SRU, OAI-PMH, Z39.50).
///
/// Biểu ghi được lưu hai lần: bản MARC đầy đủ ở cột jsonb và vài cột phẳng để tra cứu nhanh. Bình
/// thường thì đọc bản đầy đủ; bản đó hỏng — dữ liệu chuyển đổi từ hệ thống cũ vẫn có trường hợp
/// như vậy — thì dựng lại một biểu ghi tối thiểu từ cột phẳng, để thư viện bạn vẫn nhận được thứ
/// đọc được thay vì một lỗi.
/// </summary>
public static class BibMarcReader
{
    public static MarcRecord Read(BibRecord bib)
    {
        ArgumentNullException.ThrowIfNull(bib);

        if (!string.IsNullOrWhiteSpace(bib.MarcData))
        {
            try
            {
                var record = MarcJson.Deserialize(bib.MarcData);
                record.ControlNumber = bib.ControlNumber;

                return record;
            }
            catch (Exception ex) when (ex is MarcException or System.Text.Json.JsonException)
            {
                // Rơi xuống nhánh dựng từ cột phẳng bên dưới.
            }
        }

        var fallback = new MarcRecord { ControlNumber = bib.ControlNumber };
        fallback.AddField("245", '1', '0').AddSubfield('a', bib.Title);

        if (!string.IsNullOrWhiteSpace(bib.AuthorMain))
        {
            fallback.AddField("100", '1').AddSubfield('a', bib.AuthorMain);
        }

        if (!string.IsNullOrWhiteSpace(bib.Isbn))
        {
            fallback.AddField("020").AddSubfield('a', bib.Isbn);
        }

        return fallback;
    }
}
