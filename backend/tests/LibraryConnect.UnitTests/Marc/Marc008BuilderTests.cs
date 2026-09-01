using FluentAssertions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Marc;

namespace LibraryConnect.UnitTests.Marc;

/// <summary>
/// Trường 008 — trường bắt buộc của MARC 21 bibliographic.
///
/// Đo trên kho thật trước khi sửa: **0 trên 7.466** biểu ghi thu hoạch về có trường 008. Thiếu nó
/// thì biểu ghi không hợp lệ và phần mềm thư viện khác từ chối nhận khi nhập ISO 2709 — đúng thứ
/// mục 2.4 của E-HSMT đem ra kiểm.
///
/// Vị trí nào không suy ra được thì điền ký tự `|` của chuẩn, nghĩa là "không mã hóa". Để khoảng
/// trắng là một lời khai khác hẳn: khoảng trắng ở nhiều vị trí mang nghĩa "không có", tức là mình
/// đang khẳng định một điều mình không biết.
/// </summary>
public class Marc008BuilderTests
{
    private static readonly DateOnly HomNay = new(2026, 9, 1);

    private sealed class ThamSoGia : ISystemParameterService
    {
        private readonly Dictionary<string, string?> _values;

        public ThamSoGia(Dictionary<string, string?>? values = null) => _values = values ?? new();

        public Task<string?> GetAsync(string key, CancellationToken ct = default) =>
            Task.FromResult(_values.GetValueOrDefault(key));

        public Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken ct = default) =>
            Task.FromResult(_values.TryGetValue(key, out var value) && value is not null
                ? (T)Convert.ChangeType(value, typeof(T))
                : defaultValue);

        public Task SetAsync(string key, string? value, CancellationToken ct = default)
        {
            _values[key] = value;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyDictionary<string, string?>> GetGroupAsync(
            string groupCode, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<string, string?>>(_values);

        public Task InvalidateAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private static async Task<string> DungAsync(
        MarcRecord? record = null, int? namXuatBan = null, Dictionary<string, string?>? thamSo = null)
    {
        record ??= new MarcRecord();
        await Marc008Builder.EnsureAsync(record, new ThamSoGia(thamSo), HomNay, namXuatBan);

        return record.GetControlField("008")!;
    }

    [Fact]
    public async Task Dai_dung_40_ky_tu()
    {
        (await DungAsync()).Should().HaveLength(40);
    }

    [Fact]
    public async Task Sau_ky_tu_dau_la_ngay_tao_bieu_ghi_dang_YYMMDD()
    {
        (await DungAsync())[..6].Should().Be("260901");
    }

    [Fact]
    public async Task Vi_tri_06_la_s_va_07_10_la_nam_xuat_ban()
    {
        var value = await DungAsync(namXuatBan: 2018);

        value[6].Should().Be('s', "'s' nghĩa là một năm xuất bản duy nhất");
        value[7..11].Should().Be("2018");
    }

    [Fact]
    public async Task Nuoc_va_ngon_ngu_lay_tu_tham_so_he_thong()
    {
        var value = await DungAsync(thamSo: new Dictionary<string, string?>
        {
            ["CATALOG.DEFAULT_COUNTRY"] = "vm",
            ["CATALOG.DEFAULT_LANGUAGE"] = "vie",
        });

        value[15..18].Should().Be("vm ", "mã nước MARC của Việt Nam là 'vm', đệm khoảng trắng cho đủ 3");
        value[35..38].Should().Be("vie");
    }

    [Fact]
    public async Task Ngon_ngu_lay_theo_truong_041_cua_chinh_bieu_ghi_neu_co()
    {
        var record = new MarcRecord();
        record.AddField("041").AddSubfield('a', "eng");

        var value = await DungAsync(record, thamSo: new Dictionary<string, string?>
        {
            ["CATALOG.DEFAULT_LANGUAGE"] = "vie",
        });

        value[35..38].Should().Be("eng",
            "008 vị trí 35-37 phải khớp với 041$a, nếu không hai chỗ trong cùng một biểu ghi nói "
            + "hai thứ khác nhau");
    }

    [Fact]
    public async Task Vi_tri_khong_suy_ra_duoc_dien_ky_tu_gach_dung()
    {
        var value = await DungAsync();

        // 18-34 là nhóm vị trí riêng của sách: minh họa, đối tượng đọc, dạng vật mang, nội dung,
        // xuất bản phẩm nhà nước, kỷ yếu hội nghị, sách kỷ niệm, chỉ mục, thể loại văn học, tiểu sử.
        // Dublin Core không có thông tin nào trong số này.
        foreach (var index in Enumerable.Range(18, 14).Concat(Enumerable.Range(33, 2)))
        {
            value[index].Should().Be('|', $"vị trí {index} không suy ra được từ nguồn");
        }
    }

    [Fact]
    public async Task Vi_tri_khong_dinh_nghia_va_vi_tri_sua_doi_phai_de_trong()
    {
        var value = await DungAsync();

        value[32].Should().Be(' ', "vị trí 32 chưa được chuẩn định nghĩa, phải để trống");
        value[38].Should().Be(' ', "biểu ghi chưa bị cắt bớt vì hạn chế bảng mã");
    }

    [Fact]
    public async Task Vi_tri_39_ghi_nguon_bien_muc_la_co_quan_khac()
    {
        (await DungAsync())[39].Should().Be('d',
            "để trống ở vị trí 39 là tự nhận mình là cơ quan thư mục quốc gia");
    }

    [Fact]
    public async Task Khong_ghi_de_len_nhung_vi_tri_can_bo_da_dien()
    {
        var record = new MarcRecord();
        record.SetControlField("008", "230115s2015    vm a     b    000 0 vie d");

        var value = await DungAsync(record, namXuatBan: 2020);

        value.Should().StartWith("230115s2015", "cán bộ đã biên mục tay thì giữ nguyên");
    }
}
