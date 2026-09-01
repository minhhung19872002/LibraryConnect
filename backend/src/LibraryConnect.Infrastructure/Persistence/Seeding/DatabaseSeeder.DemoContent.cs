using Microsoft.Extensions.Logging;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Domain.Entities.Web;
using LibraryConnect.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Infrastructure.Persistence.Seeding;

public partial class DatabaseSeeder
{
    /// <summary>
    /// Thông tin thư viện và tin tức cho bản trình diễn.
    ///
    /// Bộ gieo dữ liệu nền cố tình để tên thư viện là chữ "Thư viện" trống trơn — sản phẩm không
    /// được gắn cứng tên của khách nào. Nhưng bản trình diễn để nguyên như vậy thì trang chủ hiện
    /// đúng hai chữ "Thư viện", mục tin tức báo "Chưa có bản tin nào được đăng", và người xem nghĩ
    /// là phần mềm chưa cài xong chứ không nghĩ là dữ liệu chưa nhập.
    ///
    /// Vì thế phần này chỉ chạy cùng bộ dữ liệu minh họa, và đặt một tên trường rõ ràng là tên mẫu.
    /// Khách hàng đổi lại bằng một tham số ở màn hình Tham số hệ thống.
    /// </summary>
    private async Task SeedDemoContentAsync(CancellationToken ct)
    {
        await SeedDemoLibraryIdentityAsync(ct);
        await SeedDemoNewsAsync(ct);
    }

    private async Task SeedDemoLibraryIdentityAsync(CancellationToken ct)
    {
        var giaTri = new Dictionary<string, string>
        {
            [ParameterKeys.LibraryName] = "Thư viện Trường Đại học Mẫu",
            [ParameterKeys.LibraryNameEn] = "Sample University Library",
            [ParameterKeys.LibraryAddress] = "Số 1 đường Nguyễn Trãi, Quận 1, TP. Hồ Chí Minh",
            [ParameterKeys.LibraryPhone] = "(028) 3822 0000",
            [ParameterKeys.LibraryEmail] = "thuvien@example.edu.vn",
            [ParameterKeys.LibraryWebsite] = "https://thuvien.example.edu.vn",
            [ParameterKeys.MarcCatalogingSource] = "Thư viện Trường Đại học Mẫu"
        };

        var thamSo = await _db.SystemParameters
            .Where(row => giaTri.Keys.Contains(row.Key))
            .ToListAsync(ct);

        var doi = 0;

        foreach (var row in thamSo)
        {
            // Chỉ điền chỗ còn trống hoặc còn nguyên giá trị mặc định: thư viện đã đặt tên của mình
            // rồi thì một lần chạy lại bộ gieo dữ liệu không được ghi đè lên.
            var trong = string.IsNullOrWhiteSpace(row.Value)
                        || row.Value == "Thư viện"
                        || row.Value == "Library";

            if (!trong)
            {
                continue;
            }

            row.Value = giaTri[row.Key];
            doi++;
        }

        if (doi > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Đã đặt {Count} tham số nhận diện thư viện cho bản trình diễn", doi);
        }
    }

    /// <summary>Sáu bản tin trải trên các chuyên mục, để mục Tin tức – sự kiện demo được ngay.</summary>
    private async Task SeedDemoNewsAsync(CancellationToken ct)
    {
        if (await _db.CmsNews.AnyAsync(ct))
        {
            return;
        }

        var chuyenMuc = await _db.CmsNewsCategories
            .OrderBy(row => row.SortOrder)
            .ToListAsync(ct);

        if (chuyenMuc.Count == 0)
        {
            return;
        }

        var tin = new (string Title, string Summary, string Body, int NgayTruoc, bool NoiBat)[]
        {
            ("Thư viện mở cửa trở lại toàn bộ phòng đọc từ đầu học kỳ mới",
             "Từ ngày khai giảng, cả bốn phòng đọc và kho mở phục vụ liên tục từ 7h30 đến 17h00 các ngày trong tuần.",
             "<p>Sau đợt sắp xếp lại kho, Thư viện mở cửa trở lại toàn bộ phòng đọc và kho mở. "
             + "Bạn đọc mang theo thẻ thư viện hoặc thẻ điện tử trên điện thoại để quét tại cổng.</p>"
             + "<p>Giờ phục vụ: thứ Hai đến thứ Sáu 7h30 – 17h00, thứ Bảy 7h30 – 11h30.</p>", 3, true),

            ("Bổ sung hơn 1.200 tài liệu chuyên ngành tài nguyên và môi trường",
             "Đợt bổ sung quý này tập trung vào tài nguyên nước, biến đổi khí hậu và kỹ thuật môi trường.",
             "<p>Thư viện vừa hoàn tất kiểm nhận đợt bổ sung quý này với hơn 1.200 tài liệu, "
             + "trong đó có giáo trình, sách tham khảo và luận văn thuộc các ngành tài nguyên nước, "
             + "biến đổi khí hậu, kỹ thuật môi trường.</p>"
             + "<p>Bạn đọc tra cứu trên trang tra cứu trực tuyến hoặc hỏi cán bộ tại quầy phục vụ.</p>", 9, true),

            ("Hướng dẫn tra cứu tài liệu số dành cho sinh viên năm nhất",
             "Buổi hướng dẫn kéo dài 90 phút, giới thiệu cách tra cứu, đặt giữ chỗ và đọc tài liệu số trực tuyến.",
             "<p>Thư viện tổ chức buổi hướng dẫn sử dụng dành cho sinh viên khóa mới: cách tra cứu "
             + "theo nhan đề, tác giả và chủ đề; cách đặt giữ chỗ khi tài liệu đang có người mượn; "
             + "cách xin quyền đọc tài liệu hạn chế.</p>"
             + "<p>Sinh viên đăng ký tại quầy phục vụ hoặc qua trang tra cứu.</p>", 16, false),

            ("Danh mục tạp chí khoa học đặt mua năm nay",
             "Thư viện tiếp tục đặt 12 đầu tạp chí chuyên ngành, trong đó có bốn đầu tạp chí quốc tế.",
             "<p>Danh mục tạp chí năm nay giữ nguyên các đầu chuyên ngành thủy lợi, môi trường và "
             + "kinh tế, bổ sung thêm hai đầu về công nghệ thông tin.</p>"
             + "<p>Số mới nhận được trưng bày tại phòng đọc báo – tạp chí trong hai tuần trước khi "
             + "chuyển vào kho.</p>", 24, false),

            ("Nhắc bạn đọc trả tài liệu trước kỳ nghỉ",
             "Tài liệu đến hạn trong kỳ nghỉ được gia hạn tự động tới ngày làm việc đầu tiên sau nghỉ.",
             "<p>Bạn đọc kiểm tra mục \"Sách đang mượn\" trong tài khoản của mình để biết hạn trả. "
             + "Tài liệu quá hạn tính phí theo quy định hiện hành; ngày nghỉ lễ không tính vào số "
             + "ngày quá hạn.</p>", 35, false),

            ("Kết nối tra cứu liên thư viện với Thư viện Quốc hội Mỹ",
             "Bạn đọc tra cứu song song sang thư viện bạn ngay trên trang tra cứu, ở mục \"Tìm ở thư viện khác\".",
             "<p>Thư viện đã kết nối theo chuẩn Z39.50 và SRU tới một số thư viện lớn. Kết quả tra "
             + "cứu ghi rõ nguồn; tài liệu nào thư viện mình đã có sẽ hiện liên kết mở thẳng sang "
             + "trang chi tiết.</p>", 48, false)
        };

        for (var index = 0; index < tin.Length; index++)
        {
            var (title, summary, body, ngayTruoc, noiBat) = tin[index];
            var ngayDang = _clock.Now.AddDays(-ngayTruoc);

            _db.CmsNews.Add(new CmsNews
            {
                Id = Guid.NewGuid(),
                Title = title,
                Slug = VietnameseText.UrlSlug(title),
                Summary = summary,
                Content = body,
                CategoryId = chuyenMuc[index % chuyenMuc.Count].Id,
                Author = "Ban Thông tin – Thư viện",
                IsFeatured = noiBat,
                IsPublished = true,
                PublishedAt = ngayDang,
                ViewCount = 40 + index * 17,
                CreatedAt = ngayDang
            });
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Đã nạp {Count} bản tin minh họa", tin.Length);
    }
}
