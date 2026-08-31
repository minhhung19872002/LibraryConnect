using LibraryConnect.Application.Features.Cataloging;

namespace LibraryConnect.Application.Common.Interfaces;

/// <summary>Một phích đã dựng xong nội dung, sẵn sàng để in.</summary>
public record CardToPrint(CardContent Content, LibraryConnect.Marc.MarcRecord Record);

/// <summary>
/// In phích thư mục ra PDF theo mẫu đã thiết kế (II.10).
/// </summary>
public interface ICardPrintService
{
    /// <summary>
    /// Kết xuất các phích thành một tệp PDF.
    ///
    /// <paramref name="multiplePerPage"/> quyết định cách xếp giấy: bật thì xếp nhiều phích vừa khổ
    /// trên một trang A4 để cắt rời, tắt thì mỗi phích một trang đúng khổ phích để in trực tiếp lên
    /// bìa phích in sẵn.
    /// </summary>
    byte[] Render(
        CardTemplateDto template,
        IReadOnlyList<CardToPrint> cards,
        bool multiplePerPage);
}
