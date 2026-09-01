using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Domain.Entities.Acq;
using LibraryConnect.Domain.Entities.Cir;
using LibraryConnect.Domain.Entities.Rdr;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Persistence.Seeding;

public partial class DatabaseSeeder
{
    /// <summary>
    /// Bộ dữ liệu trình diễn lớn: đặt <c>LC_SEED_DEMO=rich</c>.
    ///
    /// Bộ minh họa mặc định (200 biểu ghi, 500 ĐKCB, 50 bạn đọc) đủ để chứng minh chức năng chạy,
    /// nhưng không đủ để thấy hệ thống cư xử thế nào khi kho lớn. Nhiều lỗi chỉ lộ ra ở quy mô thật:
    /// trang duyệt theo tác giả từng chỉ hiện đúng một người vì lấy 500 tên đầu bảng chữ cái rồi mới
    /// lọc, cột nhan đề từng bị bóp còn 66 px vì dữ liệu mẫu toàn nhan đề ngắn.
    ///
    /// Bộ này khác bộ mặc định ở chỗ **chạy được trên kho đã có dữ liệu**: nó sinh ĐKCB cho chính
    /// những biểu ghi thu hoạch thật đang nằm trong kho, chứ không dựng biểu ghi giả. Vì vậy nó
    /// không dùng điều kiện "kho còn trống" mà dùng một dấu riêng, chạy đúng một lần.
    /// </summary>
    private const string DemoRichMarker = "SYS.DEMO_RICH_APPLIED";

    /// <summary>Tỉ lệ biểu ghi được sinh ĐKCB — thư viện nào cũng có phần chỉ có bản số.</summary>
    private const int TiLeCoBanIn = 60;

    private const int SoBanDoc = 300;
    private const int SoLuotMuon = 1_500;
    private const int SoDatGiu = 50;

    /// <summary>
    /// Bộ trình diễn lớn chia làm hai phần, khác nhau ở chỗ chạy lại được hay không.
    ///
    /// **Phần chạy một lần** — bạn đọc, lượt mượn trả, đặt giữ, tên thư viện — có dấu riêng, vì chạy
    /// lại là nhân đôi số bạn đọc và số lượt mượn.
    ///
    /// **Phần chạy lại được** — sinh ĐKCB cho biểu ghi chưa có bản in — không cần dấu nào cả: nó chỉ
    /// chọn biểu ghi *chưa có bản in nào*, nên chạy hai lần cũng ra đúng một kết quả. Nhờ vậy nạp
    /// thêm sách về sau — ví dụ 3.600 cuốn từ Open Library — rồi khởi động lại là chúng có bản in
    /// ngay, không phải xoá dấu hay dựng lại cả bộ dữ liệu.
    /// </summary>
    private async Task SeedDemoRichAsync(CancellationToken ct)
    {
        if (CheDoMinhHoa() != CheDoDuLieuMinhHoa.Lon)
        {
            return;
        }

        var daNapPhanMotLan = await _parameters.GetAsync(DemoRichMarker, false, ct);

        if (!daNapPhanMotLan)
        {
            _logger.LogInformation("Bắt đầu nạp bộ dữ liệu trình diễn lớn (LC_SEED_DEMO=rich)…");

            // Đặt tên thư viện và mã cơ quan nếu tham số còn để mặc định: bản trình diễn mà đầu
            // trang vẫn hiện đúng chữ "Thư viện" thì người xem nghĩ là phần mềm chưa cài xong.
            await SeedDemoLibraryIdentityAsync(ct);
        }

        var moi = await SinhDkcbChoKhoThatAsync(ct);

        if (!daNapPhanMotLan)
        {
            var sanSang = await _db.Items
                .Where(item => item.Status == ItemStatus.InStock && !item.IsLocked)
                .OrderBy(item => item.Barcode)
                .ToListAsync(ct);

            var readers = await SeedDemoReadersAsync(ct, SoBanDoc);

            await SeedDemoCirculationAsync(readers, sanSang, ct, SoLuotMuon, soThangTraiDeu: 18);
            await SinhDatGiuAsync(readers, ct);

            await _parameters.SetAsync(DemoRichMarker, "true", ct);
            await _parameters.InvalidateAsync(ct);

            _logger.LogInformation(
                "Đã nạp bộ dữ liệu trình diễn lớn: {Items} ĐKCB, {Readers} bạn đọc, {Loans} lượt mượn.",
                sanSang.Count, readers.Count, SoLuotMuon);

            return;
        }

        if (moi.Count == 0)
        {
            return;
        }

        // Sách mới nạp về mà nằm im trên giá thì màn hình lưu thông không thấy chúng bao giờ. Cho
        // một phần đi vào lưu thông, tỉ lệ giữ đúng như phần lượt mượn đã sinh lúc đầu.
        var soLuot = Math.Max(1, moi.Count * SoLuotMuon / Math.Max(1, SoBanDocTrenKho()));

        await SinhLuotMuonChoBanMoiAsync(moi, soLuot, ct);

        _logger.LogInformation(
            "Đã sinh thêm {Items} ĐKCB cho biểu ghi mới và {Loans} lượt mượn trên số bản ấy.",
            moi.Count, soLuot);
    }

    /// <summary>Số ĐKCB của lần nạp đầu, dùng để giữ đúng tỉ lệ lượt mượn khi sinh thêm.</summary>
    private static int SoBanDocTrenKho() => 9_500;

    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Sinh ĐKCB cho khoảng 60% biểu ghi trong kho chưa có bản in nào.
    ///
    /// Đi qua đúng <see cref="ItemCreator"/> mà cán bộ bổ sung dùng, nên mã vạch, số đăng ký cá biệt
    /// và ký hiệu xếp giá theo đúng quy tắc đang khai trong tham số — không có đường tắt nào riêng
    /// cho dữ liệu trình diễn.
    ///
    /// Chọn biểu ghi theo phép chia lấy dư trên số thứ tự chứ không chọn ngẫu nhiên: hai lần nạp
    /// trên hai máy phải ra cùng một bộ dữ liệu thì kịch bản nghiệm thu mới viết một lần dùng được
    /// ở mọi nơi.
    /// </summary>
    private async Task<IReadOnlyList<Item>> SinhDkcbChoKhoThatAsync(CancellationToken ct)
    {
        var warehouses = await _db.Warehouses.OrderBy(kho => kho.Code).ToListAsync(ct);

        if (warehouses.Count == 0)
        {
            return Array.Empty<Item>();
        }

        var shelves = await _db.Shelves.OrderBy(gia => gia.Code).ToListAsync(ct);

        var funding = await _db.FundingSources
            .OrderBy(nguon => nguon.SortOrder).Select(nguon => nguon.Id).ToListAsync(ct);

        var suppliers = await _db.Suppliers
            .OrderBy(ncc => ncc.SortOrder).Select(ncc => ncc.Id).ToListAsync(ct);

        var chuaCoBan = await _db.BibRecords
            .Where(bib => bib.DeletedAt == null && bib.ItemCount == 0
                          && bib.Status == RecordStatus.Published)
            .OrderBy(bib => bib.ControlNumber)
            .ToListAsync(ct);

        var today = _clock.Today;
        var daTao = 0;

        for (var index = 0; index < chuaCoBan.Count; index++)
        {
            if (index % 100 >= TiLeCoBanIn)
            {
                continue;
            }

            var bib = chuaCoBan[index];
            var warehouse = warehouses[index % warehouses.Count];

            var giaCuaKho = shelves.Where(gia => gia.WarehouseId == warehouse.Id).ToList();

            var shelf = giaCuaKho.Count > 0
                ? (Guid?)giaCuaKho[index % giaCuaKho.Count].Id
                : null;

            // Luận văn, luận án, đề tài thường chỉ có một bản; giáo trình và sách thì nhiều bản.
            var quantity = (index % 7) switch
            {
                0 or 1 => 3,
                2 or 3 or 4 => 2,
                _ => 1,
            };

            // Cứ 40 đầu thì một đầu để chờ kiểm nhận, đủ để màn hình kiểm nhận có việc thật.
            var choKiemNhan = index % 40 == 39;

            await ItemCreator.CreateAsync(_db, _codes, _parameters, _clock, new ItemCreator.Request(
                Bib: bib,
                Quantity: quantity,
                WarehouseId: warehouse.Id,
                ShelfId: shelf,
                Price: 65_000 + (index % 18) * 12_000,
                FundingSourceId: funding.Count > 0 ? funding[index % funding.Count] : null,
                AcquisitionType: (index % 13) switch
                {
                    0 => AcquisitionType.Donation,
                    7 => AcquisitionType.LegalDeposit,
                    _ => AcquisitionType.Purchase,
                },
                OrderId: null,
                Note: null,
                AcquisitionDate: today.AddDays(-(30 + index * 3 % 900)),
                SupplierId: suppliers.Count > 0 ? suppliers[index % suppliers.Count] : null,
                UnlockImmediately: !choKiemNhan), ct);

            daTao++;

            // Ghi theo lô: bảy nghìn biểu ghi mà lưu từng cái một thì lượt nạp kéo dài hàng chục phút.
            if (daTao % 200 == 0)
            {
                _logger.LogInformation("Đã sinh ĐKCB cho {Count} biểu ghi…", daTao);
            }
        }

        if (daTao == 0)
        {
            return Array.Empty<Item>();
        }

        await DanhDauMatHongThanhLyAsync(ct);

        // Chỉ trả về những bản vừa tạo trong lượt này: phía trên cần biết đúng phần mới để sinh
        // lượt mượn cho chúng, chứ không phải cả kho.
        var cuaBieuGhiMoi = chuaCoBan.Select(bib => bib.Id).ToHashSet();

        return await _db.Items
            .Where(item => cuaBieuGhiMoi.Contains(item.BibId)
                           && item.Status == ItemStatus.InStock && !item.IsLocked)
            .OrderBy(item => item.Barcode)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Sinh lượt mượn trên những bản in vừa tạo, dùng đúng bạn đọc đã có trong kho.
    ///
    /// Không dựng bạn đọc mới: kho đã có 300 người, thêm nữa chỉ làm loãng. Lượt mượn đi qua đúng
    /// <see cref="CirculationRules"/> mà quầy dùng, nên hạn trả và tiền phạt tính cùng một quy tắc.
    /// </summary>
    private async Task SinhLuotMuonChoBanMoiAsync(
        IReadOnlyList<Item> items, int soLuot, CancellationToken ct)
    {
        var readers = await _db.Readers
            .Where(reader => reader.DeletedAt == null && reader.Status == ReaderStatus.Active)
            .OrderBy(reader => reader.CardNumber)
            .ToListAsync(ct);

        if (readers.Count == 0 || items.Count == 0)
        {
            return;
        }

        await SeedDemoCirculationAsync(
            readers, items, ct, Math.Min(soLuot, items.Count), soThangTraiDeu: 12);
    }

    /// <summary>
    /// Đánh dấu một phần nhỏ ĐKCB là mất, hỏng và đã thanh lý.
    ///
    /// Không có những trạng thái này thì báo cáo kho chỉ có đúng một cột, màn hình thanh lý rỗng, và
    /// kết quả kiểm kê không bao giờ ra dòng "thiếu" nào — ba màn hình không demo được.
    /// </summary>
    private async Task DanhDauMatHongThanhLyAsync(CancellationToken ct)
    {
        var items = await _db.Items
            .Where(item => item.Status == ItemStatus.InStock && !item.IsLocked)
            .OrderBy(item => item.Barcode)
            .ToListAsync(ct);

        for (var index = 0; index < items.Count; index++)
        {
            items[index].Status = (index % 250) switch
            {
                7 => ItemStatus.Lost,
                97 => ItemStatus.Damaged,
                197 => ItemStatus.Discarded,
                _ => items[index].Status,
            };

            if (items[index].Status == ItemStatus.Discarded)
            {
                items[index].Note = "Thanh lý theo quyết định số 41/QĐ-TV";
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Đặt giữ chỗ ở đủ các trạng thái: đang chờ, sách đã về sẵn sàng nhận, đã nhận, hết hạn.
    ///
    /// Chỉ đặt giữ trên biểu ghi mà mọi bản đều đang có người mượn — đúng tình huống bạn đọc đặt giữ
    /// thật, và nhờ vậy hàng đợi trên màn hình chi tiết tài liệu có nghĩa.
    /// </summary>
    private async Task SinhDatGiuAsync(IReadOnlyList<Reader> readers, CancellationToken ct)
    {
        var borrowers = readers.Where(reader => reader.Status == ReaderStatus.Active).ToList();

        if (borrowers.Count == 0)
        {
            return;
        }

        var dangMuon = await _db.Loans
            .Where(loan => loan.ReturnDate == null && loan.BibId != null)
            .Select(loan => new { BibId = loan.BibId!.Value, loan.ReaderId })
            .Distinct()
            .Take(SoDatGiu * 2)
            .ToListAsync(ct);

        if (dangMuon.Count == 0)
        {
            return;
        }

        var pickup = await _db.Warehouses.OrderBy(kho => kho.Code)
            .Select(kho => (Guid?)kho.Id).FirstOrDefaultAsync(ct);

        var holds = new List<Hold>();

        for (var index = 0; index < Math.Min(SoDatGiu, dangMuon.Count); index++)
        {
            var muon = dangMuon[index];

            // Không để bạn đọc tự đặt giữ chính cuốn mình đang mượn — chuyện ấy không xảy ra thật.
            var reader = borrowers[(index * 3 + 1) % borrowers.Count].Id == muon.ReaderId
                ? borrowers[(index * 3 + 2) % borrowers.Count]
                : borrowers[(index * 3 + 1) % borrowers.Count];

            var status = (index % 10) switch
            {
                < 6 => HoldStatus.Waiting,
                6 or 7 => HoldStatus.Ready,
                8 => HoldStatus.Fulfilled,
                _ => HoldStatus.Expired,
            };

            holds.Add(new Hold
            {
                Id = Guid.NewGuid(),
                ReaderId = reader.Id,
                BibId = muon.BibId,
                HoldDate = _clock.Now.AddDays(-(index % 25) - 1),
                ExpireDate = status == HoldStatus.Expired
                    ? _clock.Now.AddDays(-3)
                    : _clock.Now.AddDays(7),
                PickupWarehouseId = pickup,
                Status = status,
                QueuePosition = 1 + index % 3,
                NotifiedAt = status is HoldStatus.Ready or HoldStatus.Fulfilled
                    ? _clock.Now.AddDays(-1)
                    : null,
                CreatedAt = _clock.Now,
            });
        }

        _db.Holds.AddRange(holds);
        await _db.SaveChangesAsync(ct);
    }
}
