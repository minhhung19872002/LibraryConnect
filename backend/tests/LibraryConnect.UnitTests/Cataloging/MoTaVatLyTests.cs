using FluentAssertions;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Marc;

namespace LibraryConnect.UnitTests.Cataloging;

/// <summary>
/// Cột phẳng "mô tả vật lý" không được nhận kiểu tệp.
///
/// Đã hỏng thật, và hỏng ở chỗ khó thấy nhất. Bộ ánh xạ Dublin Core cũ đổ `dc:format` vào trường
/// 300; sửa bộ ánh xạ rồi thì trường MARC sạch, nhưng **cột phẳng** `pages` — thứ mà trang tra cứu
/// công khai thật sự đọc — vẫn giữ nguyên chuỗi cũ ở 4.624 biểu ghi. Bạn đọc mở một cuốn sách ra
/// thấy dòng "Mô tả vật lý: application/pdf". Không phép thử nào bắt được, chỉ có mở trang lên nhìn.
///
/// Migration <c>20260902160000_DonKieuTepLotVaoMoTaVatLy</c> dọn số cũ. Phép thử này chặn đường
/// quay lại, và chặn ở **tầng chiếu** chứ không ở bộ ánh xạ Dublin Core — vì Dublin Core không phải
/// lối vào duy nhất: biểu ghi còn vào kho qua ISO 2709, qua Z39.50, qua Excel và qua trình soạn MARC
/// gõ tay. Chặn ở tầng chiếu là chặn cả sáu lối cùng lúc.
/// </summary>
public class MoTaVatLyTests
{
    private static MarcRecord BieuGhi(string moTaVatLy)
    {
        var record = new MarcRecord();
        record.AddField("245", '1', '0').AddSubfield('a', "Nhan đề thử");
        record.AddField("300").AddSubfield('a', moTaVatLy);

        return record;
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("application/msword")]
    [InlineData("text/plain")]
    [InlineData("image/jpeg")]
    [InlineData("video/mp4")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public void Kieu_tep_khong_duoc_coi_la_mo_ta_vat_ly(string kieuTep)
    {
        MarcProjection.Project(BieuGhi(kieuTep)).Pages
            .Should().BeNull($"'{kieuTep}' là kiểu tệp, không phải mô tả vật lý — thà bỏ trống còn "
                             + "hơn để trang tra cứu công khai hiện một chuỗi vô nghĩa cho bạn đọc");
    }

    [Theory]
    [InlineData("180 tr.")]
    [InlineData("xii, 260 tr. : minh hoạ")]
    [InlineData("1 tập bản đồ (32 tr.)")]
    // Có dấu gạch chéo mà vẫn là mô tả vật lý thật: khoảng trắng phân biệt được hai loại.
    [InlineData("2 tập / 480 tr.")]
    [InlineData("1 đĩa CD-ROM")]
    public void Mo_ta_vat_ly_that_giu_nguyen(string moTa)
    {
        MarcProjection.Project(BieuGhi(moTa)).Pages.Should().Be(moTa);
    }
}
