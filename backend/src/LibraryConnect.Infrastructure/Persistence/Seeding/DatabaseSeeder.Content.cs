using LibraryConnect.Application.Features.Cms;
using LibraryConnect.Domain.Entities.Web;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Infrastructure.Persistence.Seeding;

public partial class DatabaseSeeder
{
    /// <summary>
    /// Nội dung ban đầu của trang tra cứu (Phân hệ VIII).
    ///
    /// Năm trang tĩnh, thanh điều hướng và vài liên kết ngoài — vừa đủ để trang tra cứu mở lên là
    /// đã ra hình một website thư viện thật, chưa cần ai vào soạn gì. Nội dung viết theo lệ chung
    /// của thư viện đại học Việt Nam và không nhắc tên trường nào: chỗ nào cần tên thư viện thì để
    /// giao diện chèn từ tham số hệ thống.
    ///
    /// Không nạp sẵn banner: banner là ảnh, mà nạp một dòng trỏ tới tấm ảnh không tồn tại thì trang
    /// chủ hiện ô ảnh vỡ — tệ hơn là không có banner nào.
    /// </summary>
    private async Task SeedContentAsync(CancellationToken ct)
    {
        await SeedSiteSettingsAsync(ct);
        await SeedStaticPagesAsync(ct);
        await SeedNewsCategoriesAsync(ct);
        await SeedMenusAsync(ct);
        await SeedExternalLinksAsync(ct);
    }

    private async Task SeedSiteSettingsAsync(CancellationToken ct)
    {
        var existing = await _db.CmsSettings.Select(setting => setting.Key).ToListAsync(ct);

        var defaults = new Dictionary<string, string>
        {
            [CmsSettingCatalog.Slogan] = "Tri thức mở — Học tập suốt đời",
            [CmsSettingCatalog.OpeningHours] =
                "Thứ 2 – Thứ 6: 7h30 – 17h00" + Environment.NewLine
                + "Thứ 7: 7h30 – 11h30" + Environment.NewLine
                + "Chủ nhật và ngày lễ: nghỉ",
            [CmsSettingCatalog.ContactNote] =
                "Bạn đọc cần hỗ trợ tra cứu hoặc mượn tài liệu xin liên hệ quầy phục vụ trong giờ mở cửa."
        };

        var added = false;

        foreach (var definition in CmsSettingCatalog.All)
        {
            if (existing.Contains(definition.Key))
            {
                continue;
            }

            _db.CmsSettings.Add(new CmsSetting
            {
                Key = definition.Key,
                Value = defaults.TryGetValue(definition.Key, out var value)
                    ? value
                    : definition.DefaultValue,
                GroupCode = definition.GroupCode,
                Name = definition.Name,
                Description = definition.Description,
                DataType = definition.DataType,
                SortOrder = definition.SortOrder
            });

            added = true;
        }

        if (added)
        {
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task SeedStaticPagesAsync(CancellationToken ct)
    {
        if (await _db.CmsPages.AnyAsync(ct))
        {
            return;
        }

        var now = _clock.Now;

        void Page(string slug, string title, string description, string content, int order) =>
            _db.CmsPages.Add(new CmsPage
            {
                Slug = slug,
                Title = title,
                MetaDescription = description,
                Content = content,
                IsPublished = true,
                PublishedAt = now,
                SortOrder = order
            });

        Page("gioi-thieu", "Giới thiệu",
            "Giới thiệu về thư viện, chức năng nhiệm vụ và các dịch vụ phục vụ bạn đọc.",
            """
            <h2>Chức năng, nhiệm vụ</h2>
            <p>Thư viện là đơn vị phục vụ hoạt động đào tạo và nghiên cứu khoa học, có nhiệm vụ thu thập,
            xử lý, lưu trữ và tổ chức khai thác nguồn tài liệu phục vụ giảng viên, sinh viên, học viên
            và cán bộ trong toàn đơn vị.</p>
            <h2>Nguồn lực thông tin</h2>
            <p>Vốn tài liệu gồm giáo trình, sách tham khảo, luận văn, luận án, đề tài nghiên cứu khoa học,
            báo và tạp chí chuyên ngành, cùng bộ sưu tập tài liệu số được xây dựng và cập nhật thường xuyên.</p>
            <h2>Dịch vụ phục vụ bạn đọc</h2>
            <ul>
              <li>Đọc tại chỗ và mượn tài liệu về nhà</li>
              <li>Tra cứu trực tuyến qua trang thông tin của thư viện</li>
              <li>Khai thác tài liệu số và cơ sở dữ liệu điện tử</li>
              <li>Hướng dẫn tra cứu, hỗ trợ tìm tin theo chuyên đề</li>
              <li>Mượn liên thư viện với các thư viện có kết nối</li>
            </ul>
            """, 10);

        Page("noi-quy", "Nội quy thư viện",
            "Nội quy sử dụng thư viện, quy định mượn trả tài liệu và xử lý vi phạm.",
            """
            <h2>Quy định chung</h2>
            <ol>
              <li>Xuất trình thẻ thư viện khi vào thư viện và khi mượn tài liệu. Thẻ chỉ có giá trị với
              người đứng tên trên thẻ, không cho người khác mượn thẻ.</li>
              <li>Giữ trật tự, không sử dụng điện thoại gây ồn trong phòng đọc.</li>
              <li>Không mang đồ ăn, thức uống có màu vào khu vực kho và phòng đọc.</li>
              <li>Gửi túi xách, cặp tại tủ gửi đồ trước khi vào kho mở.</li>
            </ol>
            <h2>Mượn và trả tài liệu</h2>
            <ol>
              <li>Số lượng tài liệu và thời hạn mượn thực hiện theo chính sách lưu thông áp dụng cho từng
              loại bạn đọc; bạn đọc xem hạn trả của mình trong mục Tài khoản trên trang tra cứu.</li>
              <li>Được gia hạn tài liệu nếu chưa quá hạn và tài liệu không có người đặt giữ.</li>
              <li>Trả tài liệu đúng hạn. Quá hạn phải nộp phí theo quy định.</li>
              <li>Làm mất hoặc hư hỏng tài liệu phải bồi thường bằng bản tương đương hoặc bằng tiền theo
              quy định của thư viện.</li>
            </ol>
            <h2>Xử lý vi phạm</h2>
            <p>Tùy mức độ, bạn đọc vi phạm nội quy có thể bị nhắc nhở, tạm khóa thẻ hoặc khóa thẻ và
            thông báo về đơn vị quản lý.</p>
            """, 20);

        Page("huong-dan-su-dung", "Hướng dẫn sử dụng",
            "Hướng dẫn tra cứu tài liệu, mượn trả và sử dụng tài liệu số.",
            """
            <h2>Tra cứu tài liệu</h2>
            <p>Nhập từ khóa vào ô tìm kiếm ở trang chủ. Có thể gõ không dấu — hệ thống vẫn tìm được tài
            liệu có dấu. Chọn phạm vi tìm (Nhan đề, Tác giả, Chủ đề, ISBN…) để thu hẹp kết quả.</p>
            <p>Dùng <b>Tìm kiếm nâng cao</b> khi cần kết hợp nhiều điều kiện bằng VÀ / HOẶC / KHÔNG, hoặc
            lọc theo năm xuất bản, ngôn ngữ, dạng tài liệu và kho.</p>
            <h2>Xem tình trạng tài liệu</h2>
            <p>Trong trang chi tiết, phần <i>Bản in trong kho</i> cho biết mỗi bản đang ở kho nào, ký hiệu
            xếp giá là gì và có sẵn sàng hay không. Bản đang có người mượn hiển thị kèm hạn trả.</p>
            <h2>Đặt giữ chỗ</h2>
            <p>Đăng nhập bằng số thẻ và mật khẩu, mở tài liệu cần mượn rồi bấm <b>Đặt giữ chỗ</b>. Khi có
            bản sẵn sàng, thư viện sẽ thông báo và giữ tài liệu trong thời hạn quy định.</p>
            <h2>Tài liệu số</h2>
            <p>Tài liệu công khai đọc được ngay. Tài liệu nội bộ yêu cầu đăng nhập. Tài liệu hạn chế cần
            gửi yêu cầu kèm lý do sử dụng và chờ thư viện duyệt.</p>
            """, 30);

        Page("lien-he", "Liên hệ",
            "Địa chỉ, điện thoại, email và giờ mở cửa của thư viện.",
            """
            <p>Bạn đọc cần hỗ trợ vui lòng liên hệ thư viện theo thông tin hiển thị ở cuối trang, hoặc đến
            trực tiếp quầy phục vụ trong giờ mở cửa.</p>
            <p>Với các đề nghị bổ sung tài liệu, xin gửi kèm nhan đề, tác giả, nhà xuất bản, năm xuất bản
            và số bản đề nghị mua để thư viện xem xét.</p>
            """, 40);

        Page("hoi-dap", "Hỏi đáp",
            "Những câu hỏi bạn đọc thường gặp khi sử dụng thư viện.",
            """
            <h3>Tôi quên mật khẩu tài khoản tra cứu thì làm thế nào?</h3>
            <p>Liên hệ quầy phục vụ kèm thẻ thư viện để được đặt lại mật khẩu.</p>
            <h3>Thẻ thư viện của tôi hết hạn, gia hạn ở đâu?</h3>
            <p>Đăng nhập trang tra cứu, vào mục Tài khoản và gửi yêu cầu gia hạn thẻ; hoặc đến trực tiếp
            quầy phục vụ.</p>
            <h3>Tôi tìm được tài liệu nhưng thư viện báo hết bản rảnh?</h3>
            <p>Bấm Đặt giữ chỗ để vào hàng đợi. Khi có người trả tài liệu, hệ thống sẽ giữ bản đó cho bạn
            và gửi thông báo.</p>
            <h3>Vì sao tôi không tải được tệp tài liệu số?</h3>
            <p>Một số tài liệu chỉ cho đọc trực tuyến, không cho tải về, theo thỏa thuận bản quyền với tác
            giả hoặc nhà xuất bản. Tài liệu hạn chế thì cần gửi yêu cầu và được duyệt trước.</p>
            <h3>Thư viện có tra cứu được tài liệu của thư viện khác không?</h3>
            <p>Có. Mục <i>Tìm ở thư viện khác</i> tra song song sang các thư viện đã kết nối theo chuẩn
            Z39.50 và SRU.</p>
            """, 50);

        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedNewsCategoriesAsync(CancellationToken ct)
    {
        if (await _db.CmsNewsCategories.AnyAsync(ct))
        {
            return;
        }

        _db.CmsNewsCategories.AddRange(
            new CmsNewsCategory { Code = "THONG_BAO", Name = "Thông báo", SortOrder = 10 },
            new CmsNewsCategory { Code = "SU_KIEN", Name = "Sự kiện", SortOrder = 20 },
            new CmsNewsCategory { Code = "TAI_LIEU_MOI", Name = "Tài liệu mới", SortOrder = 30 },
            new CmsNewsCategory { Code = "HUONG_DAN", Name = "Hướng dẫn bạn đọc", SortOrder = 40 });

        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedMenusAsync(CancellationToken ct)
    {
        if (await _db.CmsMenus.AnyAsync(ct))
        {
            return;
        }

        var about = new CmsMenu { Name = "Giới thiệu", Url = "/trang/gioi-thieu", SortOrder = 50 };

        _db.CmsMenus.AddRange(
            new CmsMenu { Name = "Trang chủ", Url = "/", SortOrder = 10 },
            new CmsMenu { Name = "Tra cứu", Url = "/tra-cuu", SortOrder = 20 },
            new CmsMenu { Name = "Tài liệu số", Url = "/tai-lieu-so", SortOrder = 30 },
            new CmsMenu { Name = "Tin tức", Url = "/tin-tuc", SortOrder = 40 },
            about);

        await _db.SaveChangesAsync(ct);

        _db.CmsMenus.AddRange(
            new CmsMenu
            {
                Name = "Nội quy thư viện", Url = "/trang/noi-quy",
                ParentId = about.Id, SortOrder = 10
            },
            new CmsMenu
            {
                Name = "Hướng dẫn sử dụng", Url = "/trang/huong-dan-su-dung",
                ParentId = about.Id, SortOrder = 20
            },
            new CmsMenu
            {
                Name = "Hỏi đáp", Url = "/trang/hoi-dap",
                ParentId = about.Id, SortOrder = 30
            },
            new CmsMenu { Name = "Liên hệ", Url = "/trang/lien-he", SortOrder = 60 });

        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedExternalLinksAsync(CancellationToken ct)
    {
        if (await _db.CmsExternalLinks.AnyAsync(ct))
        {
            return;
        }

        _db.CmsExternalLinks.AddRange(
            new CmsExternalLink
            {
                Name = "Thư viện Quốc gia Việt Nam",
                Url = "https://nlv.gov.vn",
                GroupName = "Thư viện bạn",
                SortOrder = 10
            },
            new CmsExternalLink
            {
                Name = "Thư viện Quốc hội Mỹ",
                Url = "https://catalog.loc.gov",
                GroupName = "Thư viện bạn",
                SortOrder = 20
            },
            new CmsExternalLink
            {
                Name = "Google Scholar",
                Url = "https://scholar.google.com",
                Description = "Tìm bài báo và công trình nghiên cứu khoa học.",
                GroupName = "Cơ sở dữ liệu trực tuyến",
                SortOrder = 30
            },
            new CmsExternalLink
            {
                Name = "Cổng thông tin Sở hữu trí tuệ Việt Nam",
                Url = "https://ipvietnam.gov.vn",
                Description = "Tra cứu thông tin sáng chế và sở hữu công nghiệp.",
                GroupName = "Cơ sở dữ liệu trực tuyến",
                SortOrder = 40
            });

        await _db.SaveChangesAsync(ct);
    }
}
