using System.Net;
using System.Text;
using FluentAssertions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Domain.Entities.Ill;
using LibraryConnect.Domain.Enums;
using LibraryConnect.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Thu hoạch OAI-PMH đứt giữa chừng thì lần sau chạy tiếp từ chỗ dừng (lỗi A6).
///
/// Chuyện đã xảy ra thật với kho dspace.ctu.edu.vn: máy chủ nguồn thiếu chứng thư trung gian nên
/// phiên TLS đứt ở giữa, cả lượt thu hoạch hỏng, và vì thẻ đọc tiếp không được ghi lại nên chạy lại
/// là lấy lại từ trang đầu — một kho vài chục nghìn biểu ghi thì không bao giờ về tới đích.
///
/// Máy chủ nguồn ở đây là bản giả dựng trong bộ nhớ: chỉ có dựng được thì mới ép nó đứt đúng ở
/// trang thứ hai, còn kho thật ngoài Internet thì không.
/// </summary>
[Collection(ApiCollection.Name)]
public class OaiHarvestResumeTests
{
    private readonly LibraryConnectFactory _factory;

    public OaiHarvestResumeTests(LibraryConnectFactory factory) => _factory = factory;

    private const string DiaChiKho = "https://kho-gia.test/oai";

    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Dut_giua_chung_thi_ghi_lai_the_doc_tiep_va_lan_sau_chay_tiep_tu_do()
    {
        var repositoryId = await TaoKhoAsync("Kho đứt giữa chừng");

        // Lượt một: trang đầu lấy được hai biểu ghi rồi phiên TLS đứt ở trang hai.
        var lanMot = new MayChuGia(uri =>
            uri.Query.Contains("resumptionToken=THE-TRANG-2")
                ? throw LoiChungThu()
                : Tra(Trang(new[] { "oai:gia:1", "oai:gia:2" }, "THE-TRANG-2")));

        var mot = await ChayAsync(repositoryId, lanMot);

        mot.Status.Should().Be(JobStatus.Failed);
        mot.RecordsImported.Should().Be(2, "hai biểu ghi của trang đầu đã lấy được thì phải giữ lại");

        (await TheDocTiepAsync(repositoryId)).Should().Be("THE-TRANG-2",
            "không ghi lại thẻ đọc tiếp thì lần sau phải lấy lại từ trang đầu");

        // Lượt hai: phải mở đầu bằng chính thẻ ấy, không được hỏi lại trang đầu.
        var lanHai = new MayChuGia(_ => Tra(Trang(new[] { "oai:gia:3", "oai:gia:4" }, null)));

        var hai = await ChayAsync(repositoryId, lanHai);

        hai.Status.Should().Be(JobStatus.Completed, hai.Errors ?? "không có lỗi");
        hai.RecordsImported.Should().Be(2);

        lanHai.DaGoi.Should().HaveCount(1);
        lanHai.DaGoi[0].Query.Should().Contain("resumptionToken=THE-TRANG-2",
            "lượt sau phải nối tiếp chỗ dừng chứ không quét lại từ đầu");

        (await TheDocTiepAsync(repositoryId)).Should().BeNull(
            "thu hoạch xong thì không còn chỗ dở dang nào để nối tiếp");
    }

    [Fact]
    public async Task The_doc_tiep_da_het_han_thi_quay_ve_quet_lai_tu_dau_chu_khong_ket_vinh_vien()
    {
        var repositoryId = await TaoKhoAsync("Kho thẻ hết hạn");

        var lanMot = new MayChuGia(uri =>
            uri.Query.Contains("resumptionToken=THE-CU")
                ? throw new HttpRequestException("Kết nối bị ngắt.")
                : Tra(Trang(new[] { "oai:hethan:1" }, "THE-CU")));

        await ChayAsync(repositoryId, lanMot);
        (await TheDocTiepAsync(repositoryId)).Should().Be("THE-CU");

        // Kho nào cũng cho thẻ đọc tiếp một tuổi thọ ngắn. Quay lại sau khi thẻ hết hạn mà không xử
        // lý thì kho ấy kẹt vĩnh viễn: lần nào cũng gửi thẻ chết và lần nào cũng hỏng.
        var lanHai = new MayChuGia(uri =>
            uri.Query.Contains("resumptionToken=THE-CU")
                ? Tra(Loi("badResumptionToken", "Thẻ đọc tiếp đã hết hạn."))
                : Tra(Trang(new[] { "oai:hethan:2" }, null)));

        var hai = await ChayAsync(repositoryId, lanHai);

        hai.Status.Should().Be(JobStatus.Completed, hai.Errors ?? "không có lỗi");
        hai.RecordsImported.Should().Be(1);
        lanHai.DaGoi.Should().HaveCount(2, "gửi thẻ chết rồi quét lại từ đầu");
        lanHai.DaGoi[1].Query.Should().Contain("verb=ListRecords");
        lanHai.DaGoi[1].Query.Should().NotContain("resumptionToken");
    }

    [Fact]
    public async Task Loi_chung_thu_TLS_duoc_noi_ro_nguyen_nhan_bang_tieng_Viet()
    {
        var repositoryId = await TaoKhoAsync("Kho thiếu chứng thư");

        var log = await ChayAsync(repositoryId, new MayChuGia(_ => throw LoiChungThu()));

        log.Status.Should().Be(JobStatus.Failed);
        log.Errors.Should().NotBeNull();
        log.Errors!.Should().Contain("chứng thư",
            "cán bộ đọc nhật ký cần biết là lỗi chứng thư của máy chủ nguồn, không phải mạng hỏng");
    }

    // -----------------------------------------------------------------------------------------

    /// <summary>Đúng dạng lỗi .NET ném ra khi máy chủ nguồn thiếu chứng thư trung gian.</summary>
    private static HttpRequestException LoiChungThu() => new(
        "The SSL connection could not be established, see inner exception.",
        new System.Security.Authentication.AuthenticationException(
            "The remote certificate is invalid according to the validation procedure."));

    private async Task<Guid> TaoKhoAsync(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var repository = new OaiRepository
        {
            Id = Guid.NewGuid(),
            Name = $"{name} {Guid.NewGuid():N}"[..30],
            BaseUrl = DiaChiKho,
            MetadataPrefix = "oai_dc",
            IsActive = true,
        };

        db.OaiRepositories.Add(repository);
        await db.SaveChangesAsync(default);

        return repository.Id;
    }

    private async Task<string?> TheDocTiepAsync(Guid repositoryId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        return await db.OaiRepositories
            .AsNoTracking()
            .Where(row => row.Id == repositoryId)
            .Select(row => row.ResumptionToken)
            .FirstAsync(default);
    }

    /// <summary>Chạy trọn một lượt thu hoạch đồng bộ, trả về dòng nhật ký đã chốt.</summary>
    private async Task<OaiHarvestLog> ChayAsync(Guid repositoryId, MayChuGia mayChu)
    {
        Guid logId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

            var log = new OaiHarvestLog
            {
                Id = Guid.NewGuid(),
                RepositoryId = repositoryId,
                StartedAt = clock.Now,
                Status = JobStatus.Running,
                FullReload = false,
            };

            db.OaiHarvestLogs.Add(log);
            await db.SaveChangesAsync(default);
            logId = log.Id;
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var sp = scope.ServiceProvider;

            var harvester = new OaiHarvester(
                sp.GetRequiredService<IApplicationDbContext>(),
                new BoTaoHttpGia(mayChu),
                sp.GetRequiredService<IBibRecordWriter>(),
                sp.GetRequiredService<IDateTimeProvider>(),
                sp.GetRequiredService<ISystemParameterService>(),
                sp.GetRequiredService<IBackgroundJobService>(),
                NullLogger<OaiHarvester>.Instance);

            await harvester.RunAsync(logId, default);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            return await db.OaiHarvestLogs.AsNoTracking().FirstAsync(row => row.Id == logId, default);
        }
    }

    // -----------------------------------------------------------------------------------------

    private static HttpResponseMessage Tra(string xml) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(xml, Encoding.UTF8, "text/xml"),
    };

    private static string Trang(IEnumerable<string> identifiers, string? token)
    {
        var records = string.Concat(identifiers.Select(id =>
            "<record>"
            + $"<header><identifier>{id}</identifier><datestamp>2026-01-01</datestamp></header>"
            + "<metadata>"
            + "<oai_dc:dc xmlns:oai_dc=\"http://www.openarchives.org/OAI/2.0/oai_dc/\" "
            + "xmlns:dc=\"http://purl.org/dc/elements/1.1/\">"
            + $"<dc:title>Biểu ghi thử {id}</dc:title>"
            + "<dc:creator>Nguyễn Văn Thu Hoạch</dc:creator>"
            + "<dc:date>2023</dc:date>"
            + "<dc:language>vie</dc:language>"
            + "</oai_dc:dc>"
            + "</metadata>"
            + "</record>"));

        var resumption = token is null
            ? "<resumptionToken/>"
            : $"<resumptionToken>{token}</resumptionToken>";

        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
            + "<OAI-PMH xmlns=\"http://www.openarchives.org/OAI/2.0/\">"
            + "<responseDate>2026-01-01T00:00:00Z</responseDate>"
            + $"<ListRecords>{records}{resumption}</ListRecords>"
            + "</OAI-PMH>";
    }

    private static string Loi(string code, string message) =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
        + "<OAI-PMH xmlns=\"http://www.openarchives.org/OAI/2.0/\">"
        + "<responseDate>2026-01-01T00:00:00Z</responseDate>"
        + $"<error code=\"{code}\">{message}</error>"
        + "</OAI-PMH>";

    private sealed class MayChuGia : HttpMessageHandler
    {
        private readonly Func<Uri, HttpResponseMessage> _tra;

        public MayChuGia(Func<Uri, HttpResponseMessage> tra) => _tra = tra;

        public List<Uri> DaGoi { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            DaGoi.Add(request.RequestUri!);
            return Task.FromResult(_tra(request.RequestUri!));
        }
    }

    private sealed class BoTaoHttpGia : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public BoTaoHttpGia(HttpMessageHandler handler) => _handler = handler;

        public HttpClient CreateClient(string name) =>
            new(_handler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(30) };
    }
}
