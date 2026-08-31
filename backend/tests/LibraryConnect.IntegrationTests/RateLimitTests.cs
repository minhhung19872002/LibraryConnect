using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Chặn tần suất gọi API (yêu cầu 6.4).
///
/// Bộ kiểm thử chung nâng hạn mức đăng nhập lên rất cao để hàng trăm lượt đăng nhập của các lớp kiểm
/// thử khác không đụng trần. Lớp này dựng một máy chủ riêng với hạn mức thấp để thử đúng cơ chế đó,
/// nên nó không dùng chung máy chủ với các lớp khác và cũng không làm hỏng hạn mức của họ.
/// </summary>
[Collection(ApiCollection.Name)]
public class RateLimitTests
{
    /// <summary>Số lượt cho phép mỗi phút trong bài kiểm thử này.</summary>
    private const int Limit = 5;

    private const string LimitVariable = "LC_RateLimit__LoginPerMinute";

    // Nhận máy chủ dùng chung không phải để gọi, mà để chắc chắn cơ sở dữ liệu và các biến môi
    // trường đã sẵn sàng trước khi lớp này dựng máy chủ riêng của mình.
    public RateLimitTests(LibraryConnectFactory shared) => _ = shared;

    [Fact]
    public async Task Go_sai_mat_khau_lien_tuc_thi_bi_chan_va_duoc_bao_cho_biet_phai_cho_bao_lau()
    {
        // Máy chủ đọc hạn mức ngay lúc dựng, trước khi WebApplicationFactory kịp chèn cấu hình của
        // mình, nên phải đặt bằng biến môi trường — đúng cách mà máy chủ dùng chung cũng đang làm.
        // Máy chủ dùng chung đã đọc xong giá trị của nó từ trước, đổi ở đây không ảnh hưởng tới nó.
        var previous = Environment.GetEnvironmentVariable(LimitVariable);
        Environment.SetEnvironmentVariable(LimitVariable, Limit.ToString());

        try
        {
            await BiChanSauKhiVuotHanMucAsync();
        }
        finally
        {
            Environment.SetEnvironmentVariable(LimitVariable, previous);
        }
    }

    private static async Task BiChanSauKhiVuotHanMucAsync()
    {
        using var factory = new LowLimitFactory();
        var client = factory.CreateClient();
        HttpResponseMessage? blocked = null;

        // Gõ sai mật khẩu là chuyện thường; gõ sai hàng chục lần trong một phút thì không còn là
        // người dùng thật nữa. Đây chính là kịch bản dò mật khẩu mà mục 6.4 yêu cầu chặn.
        for (var attempt = 0; attempt < Limit + 3 && blocked is null; attempt++)
        {
            // Gõ vào một tên đăng nhập không có thật: cần thử cơ chế chặn theo địa chỉ IP, mà gõ
            // sai vào tài khoản thật thì tài khoản ấy bị khóa tạm và các bài kiểm thử khác dùng
            // chung tài khoản đó sẽ hỏng theo.
            var response = await client.PostAsJsonAsync(
                "/api/auth/login", new { username = "khong-co-tai-khoan-nay", password = "sai-mat-khau" });

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                blocked = response;
            }
        }

        blocked.Should().NotBeNull("gọi quá hạn mức phải bị chặn chứ không được cho qua");

        // Máy khách (trang tra cứu và ứng dụng di động) đều thử lại tự động. Không nói rõ phải chờ
        // bao lâu thì chúng thử lại ngay, và cửa sổ chặn cứ thế kéo dài.
        blocked!.Headers.RetryAfter.Should().NotBeNull("phải trả đầu đề Retry-After");

        var body = await blocked.Content.ReadAsStringAsync();
        body.Should().Contain("quá nhiều yêu cầu", "thông báo phải bằng tiếng Việt và dễ hiểu");
    }

    /// <summary>Máy chủ riêng cho lớp này, hạ hạn mức đăng nhập xuống mức thử được.</summary>
    public class LowLimitFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Development);

            // Đặt qua cấu hình trong bộ nhớ chứ không qua biến môi trường: biến môi trường là của
            // cả tiến trình, đổi ở đây sẽ ảnh hưởng tới máy chủ dùng chung của các lớp khác.
            // Không nạp lại dữ liệu minh họa và không chạy migration lần nữa: bài kiểm thử này chỉ
            // cần đường ống HTTP, cơ sở dữ liệu đã do máy chủ dùng chung dựng sẵn.
            builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["SEED_DEMO"] = "false",
                    ["Database:AutoMigrate"] = "false"
                }));
        }
    }
}
