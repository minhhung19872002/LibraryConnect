using FluentAssertions;
using LibraryConnect.Application.Features.InterLibrary;
using LibraryConnect.Domain.Entities.Ill;
using LibraryConnect.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace LibraryConnect.UnitTests.Infrastructure;

/// <summary>
/// Lối SRU dự phòng khi máy chủ Z39.50 nhận truy vấn nhưng từ chối trả biểu ghi (lỗi A4).
///
/// Chuyện có thật với Thư viện Quốc hội Mỹ: tra "Nhan đề = Vietnam" báo 11.528 kết quả rồi bước
/// Present trả về tay không, trong khi cùng truy vấn ấy đi lối SRU của chính thư viện đó vẫn lấy
/// được biểu ghi. Cán bộ nhìn thấy "11.528 kết quả" cạnh một danh sách rỗng và kết luận là phần
/// mềm hỏng.
/// </summary>
public class RemoteCatalogSearcherTests
{
    private static Z3950Target Dich(string? sruBaseUrl) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Thư viện Quốc hội Mỹ (Z39.50)",
        Host = "lx2.loc.gov",
        Port = 210,
        DatabaseName = "LCDB",
        UseSru = false,
        SruBaseUrl = sruBaseUrl,
        TimeoutSeconds = 30,
    };

    private static RemoteRecordDto BieuGhi(Z3950Target dich, int viTri) => new(
        dich.Id, dich.Name, viTri, $"LC{viTri}", "Vietnam: a history", null, null, null, null,
        null, null, "{}", null, null);

    [Fact]
    public async Task Z3950_bao_co_ket_qua_nhung_khong_tra_bieu_ghi_thi_lay_qua_loi_SRU()
    {
        var dich = Dich("http://lx2.loc.gov:210/lcdb");

        var searcher = new SearcherGia(
            z3950: ket => ket with { TotalHits = 11528, Records = Array.Empty<RemoteRecordDto>() },
            sru: ket => ket with
            {
                TotalHits = 5,
                Records = Enumerable.Range(1, 5).Select(i => BieuGhi(dich, i)).ToList(),
            });

        var ket = await searcher.SearchAsync(dich, RemoteSearchField.Title, "Vietnam", 20, default);

        ket.Records.Should().HaveCount(5,
            "cùng thư viện ấy còn một lối vào nữa đang lấy được biểu ghi");
        ket.Success.Should().BeTrue();
        ket.Message.Should().Contain("SRU", "phải nói rõ cho cán bộ biết đã lấy qua lối nào");
        searcher.SoLanGoiSru.Should().Be(1);
    }

    [Fact]
    public async Task Z3950_tra_duoc_bieu_ghi_thi_khong_dung_toi_loi_SRU()
    {
        var dich = Dich("http://lx2.loc.gov:210/lcdb");

        var searcher = new SearcherGia(
            z3950: ket => ket with
            {
                TotalHits = 3,
                Records = Enumerable.Range(1, 3).Select(i => BieuGhi(dich, i)).ToList(),
            },
            sru: ket => throw new InvalidOperationException("không được gọi tới lối SRU"));

        var ket = await searcher.SearchAsync(dich, RemoteSearchField.Title, "Vietnam", 20, default);

        ket.Records.Should().HaveCount(3);
        searcher.SoLanGoiSru.Should().Be(0);
    }

    [Fact]
    public async Task Khong_khai_dia_chi_SRU_du_phong_thi_giu_nguyen_ket_qua_cua_Z3950()
    {
        var dich = Dich(null);

        var searcher = new SearcherGia(
            z3950: ket => ket with { TotalHits = 11528, Records = Array.Empty<RemoteRecordDto>() },
            sru: ket => throw new InvalidOperationException("không có lối SRU để gọi"));

        var ket = await searcher.SearchAsync(dich, RemoteSearchField.Title, "Vietnam", 20, default);

        ket.TotalHits.Should().Be(11528);
        ket.Records.Should().BeEmpty();
        searcher.SoLanGoiSru.Should().Be(0);
    }

    [Fact]
    public async Task Loi_SRU_cung_hong_thi_van_tra_ve_ket_qua_goc_chu_khong_bao_that_bai()
    {
        var dich = Dich("http://lx2.loc.gov:210/lcdb");

        var searcher = new SearcherGia(
            z3950: ket => ket with { TotalHits = 11528, Records = Array.Empty<RemoteRecordDto>() },
            sru: _ => throw new InvalidOperationException("Máy chủ SRU trả về mã 503."));

        var ket = await searcher.SearchAsync(dich, RemoteSearchField.Title, "Vietnam", 20, default);

        ket.TotalHits.Should().Be(11528);
        ket.Records.Should().BeEmpty();
        ket.Message.Should().Contain("503", "cán bộ cần biết lối dự phòng hỏng vì lý do gì");
    }

    /// <summary>
    /// Bản giả của bộ tra cứu: thay hai lối đi ra mạng bằng kết quả dựng sẵn, giữ nguyên phần quyết
    /// định chuyển lối — chính là phần đang được kiểm.
    /// </summary>
    private sealed class SearcherGia : RemoteCatalogSearcher
    {
        private readonly Func<RemoteSearchTargetResultDto, RemoteSearchTargetResultDto> _z3950;
        private readonly Func<RemoteSearchTargetResultDto, RemoteSearchTargetResultDto> _sru;

        public SearcherGia(
            Func<RemoteSearchTargetResultDto, RemoteSearchTargetResultDto> z3950,
            Func<RemoteSearchTargetResultDto, RemoteSearchTargetResultDto> sru)
            : base(new HttpClientFactoryGia(), NullLogger<RemoteCatalogSearcher>.Instance)
        {
            _z3950 = z3950;
            _sru = sru;
        }

        public int SoLanGoiSru { get; private set; }

        protected override Task<RemoteSearchTargetResultDto> SearchZ3950Async(
            Z3950Target target, RemoteSearchField field, string term, int maxRecords,
            CancellationToken ct) =>
            Task.FromResult(_z3950(Rong(target)));

        protected override Task<RemoteSearchTargetResultDto> SearchSruAsync(
            Z3950Target target, RemoteSearchField field, string term, int maxRecords,
            CancellationToken ct)
        {
            SoLanGoiSru++;
            return Task.FromResult(_sru(Rong(target)));
        }

        private static RemoteSearchTargetResultDto Rong(Z3950Target target) =>
            new(target.Id, target.Name, true, null, 0, 0, Array.Empty<RemoteRecordDto>());
    }

    private sealed class HttpClientFactoryGia : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
