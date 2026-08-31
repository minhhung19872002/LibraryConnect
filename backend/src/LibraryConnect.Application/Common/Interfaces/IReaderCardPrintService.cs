using LibraryConnect.Application.Features.Readers;

namespace LibraryConnect.Application.Common.Interfaces;

/// <summary>In thẻ bạn đọc ra PDF (VI.2).</summary>
public interface IReaderCardPrintService
{
    /// <summary>
    /// Kết xuất thẻ bạn đọc.
    ///
    /// <paramref name="multiplePerPage"/> quyết định cách in: tắt thì mỗi thẻ một trang đúng khổ
    /// CR80 để đưa vào máy in thẻ nhựa; bật thì xếp nhiều thẻ trên tờ A4 để in thử hoặc để cắt.
    ///
    /// Khi mẫu thẻ có mặt sau, các trang mặt sau in xen ngay sau trang mặt trước tương ứng và được
    /// lật ngược thứ tự cột trên tờ A4, để lật giấy theo cạnh dài là mặt sau khớp đúng mặt trước.
    /// </summary>
    byte[] Render(
        ReaderCardTemplateDto template,
        IReadOnlyList<ReaderCardDataDto> cards,
        CardLibraryInfo library,
        bool multiplePerPage);
}
