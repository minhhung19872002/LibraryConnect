using FluentAssertions;
using LibraryConnect.Marc.Z3950;

namespace LibraryConnect.UnitTests.Marc;

/// <summary>
/// K20 (06/09/2026): trên máy chủ thật, truy vấn SRU `(dc.title="a" and` trả về **12.060 biểu ghi** —
/// toàn bộ kho — thay vì báo lỗi cú pháp. Bộ phân tích CQL gặp thứ không hiểu thì coi cả cụm là một
/// từ khóa trần, nên câu hỏi hỏng trở thành câu hỏi rộng.
///
/// <para>Thư viện bạn nối vào qua SRU không có cách nào biết mình gõ sai: nhận đủ kết quả, tưởng đúng.
/// Một giao thức trao đổi mà im lặng khi sai còn tệ hơn không có.</para>
/// </summary>
public class CqlSyntaxTests
{
    [Theory]
    [InlineData("(dc.title=\"a\" and", "ngoặc mở không đóng")]
    [InlineData("dc.title=a and", "toán tử đứng cuối, thiếu vế sau")]
    [InlineData("(dc.title=a or dc.author=b) and dc.subject=c", "nhóm ngoặc đổi nghĩa câu hỏi")]
    [InlineData("dc.title=a and dc.author=b or dc.subject=c", "hai toán tử khác nhau")]
    [InlineData("dc.title=a)", "thừa dấu ngoặc đóng")]
    [InlineData("dc.date > 2020", "quan hệ so sánh chưa hỗ trợ — nhận vào rồi coi như dấu bằng là trả kết quả sai")]
    public void Truy_van_sai_cu_phap_phai_bao_loi_chu_khong_thanh_tim_tat_ca(string query, string vi)
    {
        var hanh_dong = () => CqlParser.Parse(query);

        hanh_dong.Should().Throw<CqlException>(
            "câu hỏi {0} không được âm thầm biến thành một phép tìm rộng", vi);
    }

    [Theory]
    [InlineData("dc.title=\"cơ sở dữ liệu\"")]
    [InlineData("dc.author=Nguyễn")]
    [InlineData("cql.serverChoice=lập trình")]
    [InlineData("lập trình")]
    [InlineData("dc.title=a and dc.author=b")]
    [InlineData("(dc.title=a or dc.author=b)")]
    public void Truy_van_dung_cu_phap_van_phan_tich_duoc(string query)
    {
        var ket_qua = CqlParser.Parse(query);

        ket_qua.Clauses.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("dc.title", true)]
    [InlineData("title", true)]
    [InlineData("cql.serverChoice", true)]
    [InlineData("dc.identifier", true)]
    [InlineData("dc.khongco", false)]
    [InlineData("bath.tenLa", false)]
    public void Chi_muc_la_phai_bi_nhan_ra_de_tra_chan_doan_16(string index, bool hoTro)
    {
        CqlParser.TryMapIndex(index, out _).Should().Be(hoTro);
    }
}
