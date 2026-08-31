using System.Net;
using System.Net.Http.Json;
using System.Xml.Linq;
using FluentAssertions;
using LibraryConnect.Application.Common.Models;
using LibraryConnect.Application.Features.Acquisition;
using LibraryConnect.Application.Features.InterLibrary;
using LibraryConnect.Application.Features.Locations;
using LibraryConnect.Marc;
using LibraryConnect.Marc.Oai;
using LibraryConnect.Marc.Z3950;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Phân hệ liên thư viện chạy thật qua HTTP: SRU, OAI-PMH sáu verb, khai báo máy chủ đích, và
/// vòng khép kín thu hoạch chính kho của mình về lại — cách kiểm chứng cả hai chiều bằng một phép
/// thử duy nhất mà không phụ thuộc máy chủ nào ngoài Internet.
/// </summary>
[Collection(ApiCollection.Name)]
public class InterLibraryTests
{
    private static readonly XNamespace Srw = "http://www.loc.gov/zing/srw/";
    private static readonly XNamespace Oai = "http://www.openarchives.org/OAI/2.0/";
    private static readonly XNamespace Diag = "http://www.loc.gov/zing/srw/diagnostic/";
    private static readonly XNamespace Marc21Slim = "http://www.loc.gov/MARC21/slim";

    private readonly LibraryConnectFactory _factory;

    public InterLibraryTests(LibraryConnectFactory factory) => _factory = factory;

    private Task<HttpClient> ClientAsync() =>
        _factory.CreateAuthenticatedClientAsync(
            LibraryConnectFactory.AdminUsername, LibraryConnectFactory.AdminPassword);

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(
            "máy chủ trả về {0}: {1}", response.StatusCode, await response.Content.ReadAsStringAsync());

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(LibraryConnectFactory.JsonOptions);

        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue(payload.Message);

        return payload.Data!;
    }

    private static async Task<string> ErrorTextAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse>(LibraryConnectFactory.JsonOptions);

        return string.Join(" | ",
            new[] { payload?.Message }
                .Concat(payload?.Errors?.Select(error => error.Message) ?? Array.Empty<string>())
                .Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>Tạo một biểu ghi thư mục có nhan đề duy nhất để tra cứu chắc chắn tìm ra nó.</summary>
    private static async Task<(Guid BibId, string Title)> NewBibAsync(
        HttpClient client, string prefix, string? documentTypeCode = null)
    {
        var warehouses = await ReadAsync<IReadOnlyList<WarehouseDto>>(
            await client.GetAsync("/api/locations/warehouses"));

        Guid? documentTypeId = null;

        if (documentTypeCode is not null)
        {
            var types = await ReadAsync<PagedResult<Application.Features.Catalogs.CatalogItemDto>>(
                await client.GetAsync("/api/catalogs/document-types/items?pageSize=50"));

            documentTypeId = types.Items.First(item => item.Code == documentTypeCode).Id;
        }

        var title = $"{prefix} {Unique()}";

        var quick = await ReadAsync<QuickCatalogResultDto>(await client.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title,
                author = "Nguyễn Văn Liên Thư Viện",
                price = 100000m,
                ddc = "020",
                documentTypeId,
                isbn = $"978604{Random.Shared.Next(1000000, 9999999)}",
                itemQuantity = 1,
                warehouseId = warehouses[0].Id
            }));

        return (quick.BibId, title);
    }

    private static async Task<XDocument> GetXmlAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);

        response.IsSuccessStatusCode.Should().BeTrue(
            "gọi {0} trả về {1}", url, response.StatusCode);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/xml");

        return XDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    // -----------------------------------------------------------------------------------------
    // SRU
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task SRU_khong_tham_so_tra_ve_ban_tu_khai()
    {
        var client = _factory.CreateClient();
        var document = await GetXmlAsync(client, "/sru");

        document.Root!.Name.Should().Be(Srw + "explainResponse");
        document.Descendants().Should().Contain(element => element.Name.LocalName == "indexInfo");

        // Bản tự khai phải liệt kê đủ chỉ mục để thư viện bạn biết tra được những gì.
        var indexes = document.Descendants()
            .Where(element => element.Name.LocalName == "name")
            .Select(element => element.Value)
            .ToList();

        indexes.Should().Contain(new[] { "title", "creator", "isbn" });
    }

    [Fact]
    public async Task SRU_tra_cuu_tra_ve_MARCXML_dung_khong_gian_ten()
    {
        var client = await ClientAsync();
        var (_, title) = await NewBibAsync(client, "Giáo trình liên thư viện");

        var anonymous = _factory.CreateClient();
        var url = $"/sru?operation=searchRetrieve&version=1.2"
            + $"&query={Uri.EscapeDataString($"dc.title=\"{title}\"")}&recordSchema=marcxml";

        var document = await GetXmlAsync(anonymous, url);

        document.Root!.Name.Should().Be(Srw + "searchRetrieveResponse");
        document.Descendants(Srw + "numberOfRecords").First().Value.Should().Be("1");

        var record = document.Descendants(Marc21Slim + "record").FirstOrDefault();

        record.Should().NotBeNull("biểu ghi phải nằm trong không gian tên MARC21 slim");

        // Đọc ngược lại bằng chính bộ đọc MARCXML của sản phẩm: nếu XML sai thì bước này hỏng.
        var marc = MarcXml.FromXml(record!);

        marc.GetSubfield("245", 'a').Should().Contain(title);
    }

    [Fact]
    public async Task SRU_tra_cuu_go_khong_dau_van_ra_ket_qua()
    {
        var client = await ClientAsync();
        var suffix = Unique();

        await ReadAsync<QuickCatalogResultDto>(await client.PostAsJsonAsync(
            "/api/acquisition/quick-catalog", new
            {
                title = $"Cơ sở dữ liệu phân tán {suffix}",
                author = "Trần Thị B",
                price = 100000m,
                itemQuantity = 0
            }));

        var anonymous = _factory.CreateClient();
        var url = "/sru?operation=searchRetrieve&version=1.2"
            + $"&query={Uri.EscapeDataString($"dc.title=\"co so du lieu phan tan {suffix}\"")}";

        var document = await GetXmlAsync(anonymous, url);

        document.Descendants(Srw + "numberOfRecords").First().Value.Should().Be("1",
            "thư viện bạn gõ không dấu vẫn phải tra được sách tiếng Việt");
    }

    [Fact]
    public async Task SRU_tra_ve_duoc_ca_Dublin_Core()
    {
        var client = await ClientAsync();
        var (_, title) = await NewBibAsync(client, "Sách Dublin Core");

        var anonymous = _factory.CreateClient();
        var url = "/sru?operation=searchRetrieve&version=1.2"
            + $"&query={Uri.EscapeDataString($"dc.title=\"{title}\"")}&recordSchema=dc";

        var document = await GetXmlAsync(anonymous, url);

        document.Descendants(DublinCore.Dc + "title").First().Value.Should().Contain(title);
    }

    [Fact]
    public async Task SRU_truy_van_sai_cu_phap_tra_ve_chan_doan_chu_khong_phai_loi_500()
    {
        var anonymous = _factory.CreateClient();

        var document = await GetXmlAsync(
            anonymous,
            "/sru?operation=searchRetrieve&version=1.2"
            + $"&query={Uri.EscapeDataString("dc.title=\"chưa đóng")}");

        document.Descendants(Diag + "message").Should().NotBeEmpty();
    }

    [Fact]
    public async Task SRU_khong_ho_tro_luoc_do_la_thi_bao_ro()
    {
        var anonymous = _factory.CreateClient();

        var document = await GetXmlAsync(
            anonymous,
            "/sru?operation=searchRetrieve&version=1.2&query=test&recordSchema=luocdola");

        document.Descendants(Diag + "message").First().Value.Should().Contain("luocdola");
    }

    [Fact]
    public async Task SRU_phan_trang_bang_startRecord()
    {
        var client = await ClientAsync();
        var suffix = Unique();

        for (var index = 1; index <= 3; index++)
        {
            await ReadAsync<QuickCatalogResultDto>(await client.PostAsJsonAsync(
                "/api/acquisition/quick-catalog", new
                {
                    title = $"Bộ sách phân trang {suffix} tập {index}",
                    author = "Tác giả",
                    price = 50000m,
                    itemQuantity = 0
                }));
        }

        var anonymous = _factory.CreateClient();
        var query = Uri.EscapeDataString($"dc.title=\"Bộ sách phân trang {suffix}\"");

        var first = await GetXmlAsync(
            anonymous, $"/sru?operation=searchRetrieve&query={query}&maximumRecords=2");

        first.Descendants(Srw + "numberOfRecords").First().Value.Should().Be("3");
        first.Descendants(Srw + "record").Should().HaveCount(2);
        first.Descendants(Srw + "nextRecordPosition").First().Value.Should().Be("3");

        var second = await GetXmlAsync(
            anonymous, $"/sru?operation=searchRetrieve&query={query}&startRecord=3&maximumRecords=2");

        second.Descendants(Srw + "record").Should().HaveCount(1);
        second.Descendants(Srw + "nextRecordPosition").Should().BeEmpty("đã hết biểu ghi");
    }

    // -----------------------------------------------------------------------------------------
    // OAI-PMH: sáu verb
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task OAI_Identify_tra_ve_thong_tin_kho()
    {
        var anonymous = _factory.CreateClient();
        var document = await GetXmlAsync(anonymous, "/oai?verb=Identify");

        var identify = document.Descendants(Oai + "Identify").First();

        identify.Element(Oai + "protocolVersion")!.Value.Should().Be("2.0");
        identify.Element(Oai + "repositoryName")!.Value.Should().NotBeNullOrWhiteSpace();
        identify.Element(Oai + "deletedRecord")!.Value.Should().Be("persistent");
        identify.Element(Oai + "granularity")!.Value.Should().Be("YYYY-MM-DDThh:mm:ssZ");
    }

    [Fact]
    public async Task OAI_ListMetadataFormats_co_du_oai_dc_va_marc21()
    {
        var anonymous = _factory.CreateClient();
        var document = await GetXmlAsync(anonymous, "/oai?verb=ListMetadataFormats");

        var prefixes = document.Descendants(Oai + "metadataPrefix")
            .Select(element => element.Value)
            .ToList();

        prefixes.Should().Contain(new[] { "oai_dc", "marc21" });
    }

    [Fact]
    public async Task OAI_ListSets_liet_ke_dang_tai_lieu()
    {
        var anonymous = _factory.CreateClient();
        var document = await GetXmlAsync(anonymous, "/oai?verb=ListSets");

        var sets = document.Descendants(Oai + "setSpec").Select(element => element.Value).ToList();

        sets.Should().NotBeEmpty();
        sets.Should().OnlyContain(spec => spec.StartsWith("doctype:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task OAI_ListRecords_tra_ve_bieu_ghi_va_the_doc_tiep()
    {
        var client = await ClientAsync();
        await NewBibAsync(client, "Sách cho OAI");

        var anonymous = _factory.CreateClient();
        var document = await GetXmlAsync(anonymous, "/oai?verb=ListRecords&metadataPrefix=oai_dc");

        document.Descendants(Oai + "record").Should().NotBeEmpty();
        document.Descendants(DublinCore.Dc + "title").Should().NotBeEmpty();

        // Kho nhiều hơn một trang thì phải cấp thẻ đọc tiếp, và thẻ đó phải dùng lại được.
        var token = document.Descendants(Oai + "resumptionToken").FirstOrDefault()?.Value;

        if (!string.IsNullOrWhiteSpace(token))
        {
            var next = await GetXmlAsync(
                anonymous, $"/oai?verb=ListRecords&resumptionToken={Uri.EscapeDataString(token)}");

            next.Descendants(Oai + "error").Should().BeEmpty("thẻ do chính máy chủ cấp phải dùng được");
            next.Descendants(Oai + "record").Should().NotBeEmpty();
        }
    }

    [Fact]
    public async Task OAI_ListRecords_theo_marc21_tra_ve_MARCXML()
    {
        var client = await ClientAsync();
        await NewBibAsync(client, "Sách MARC qua OAI");

        var anonymous = _factory.CreateClient();
        var document = await GetXmlAsync(anonymous, "/oai?verb=ListRecords&metadataPrefix=marc21");

        var record = document.Descendants(Marc21Slim + "record").FirstOrDefault();

        record.Should().NotBeNull();
        MarcXml.FromXml(record!).GetSubfield("245", 'a').Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task OAI_ListIdentifiers_chi_tra_phan_dau_khong_kem_noi_dung()
    {
        var client = await ClientAsync();
        await NewBibAsync(client, "Sách cho ListIdentifiers");

        var anonymous = _factory.CreateClient();
        var document = await GetXmlAsync(anonymous, "/oai?verb=ListIdentifiers&metadataPrefix=oai_dc");

        document.Descendants(Oai + "header").Should().NotBeEmpty();
        document.Descendants(Oai + "metadata").Should().BeEmpty();
    }

    [Fact]
    public async Task OAI_GetRecord_lay_dung_mot_bieu_ghi()
    {
        var client = await ClientAsync();
        var (bibId, title) = await NewBibAsync(client, "Sách lấy riêng");

        var anonymous = _factory.CreateClient();

        // Định danh OAI dựng theo đúng dạng chuẩn oai:tên-miền:mã, nên đoán trước được từ mã biểu
        // ghi — cũng chính là phép thử cho quy ước đặt định danh.
        var host = _factory.Server.BaseAddress!.Host;
        var identifier = $"oai:{host}:{bibId:D}";

        var document = await GetXmlAsync(
            anonymous,
            $"/oai?verb=GetRecord&metadataPrefix=oai_dc&identifier={Uri.EscapeDataString(identifier)}");

        document.Descendants(Oai + "error").Should().BeEmpty(
            "định danh dựng theo đúng quy ước phải tra ra biểu ghi");
        document.Descendants(Oai + "identifier").First().Value.Should().Be(identifier);

        document.Descendants(Oai + "record").Should().ContainSingle();
        document.Descendants(DublinCore.Dc + "title").First().Value.Should().Contain(title);
    }

    [Theory]
    [InlineData("/oai", "badVerb")]
    [InlineData("/oai?verb=KhongCoVerbNay", "badVerb")]
    [InlineData("/oai?verb=GetRecord&metadataPrefix=oai_dc", "badArgument")]
    [InlineData("/oai?verb=ListRecords&metadataPrefix=khonghotro", "cannotDisseminateFormat")]
    [InlineData("/oai?verb=ListRecords&resumptionToken=the-gia-mao", "badResumptionToken")]
    public async Task OAI_tham_so_sai_tra_ve_dung_ma_loi_cua_chuan(string url, string expectedCode)
    {
        var anonymous = _factory.CreateClient();
        var document = await GetXmlAsync(anonymous, url);

        document.Descendants(Oai + "error").First().Attribute("code")!.Value
            .Should().Be(expectedCode);
    }

    [Fact]
    public async Task OAI_loc_theo_khoang_thoi_gian()
    {
        var anonymous = _factory.CreateClient();

        // Khoảng thời gian trong quá khứ xa thì không có biểu ghi nào — đúng ra phải là noRecordsMatch
        // chứ không phải danh sách rỗng, vì chuẩn phân biệt hai thứ đó.
        var document = await GetXmlAsync(
            anonymous,
            "/oai?verb=ListRecords&metadataPrefix=oai_dc&from=1990-01-01T00:00:00Z"
            + "&until=1990-12-31T23:59:59Z");

        document.Descendants(Oai + "error").First().Attribute("code")!.Value
            .Should().Be("noRecordsMatch");
    }

    [Fact]
    public async Task OAI_nhan_ca_POST_theo_dung_chuan()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsync("/oai", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["verb"] = "Identify" }));

        response.IsSuccessStatusCode.Should().BeTrue();

        var document = XDocument.Parse(await response.Content.ReadAsStringAsync());

        document.Descendants(Oai + "Identify").Should().ContainSingle();
    }

    // -----------------------------------------------------------------------------------------
    // Máy chủ đích và thu hoạch
    // -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Ba_may_chu_cong_khai_duoc_nap_san_khi_cai_dat()
    {
        var client = await ClientAsync();

        var targets = await ReadAsync<IReadOnlyList<Z3950TargetDto>>(
            await client.GetAsync("/api/interlibrary/targets?includeInactive=true"));

        targets.Should().HaveCountGreaterThanOrEqualTo(3);
        targets.Should().Contain(target => target.Host == "lx2.loc.gov");
        targets.Should().Contain(target => target.UseSru, "phải có ít nhất một đường SRU dự phòng");
    }

    [Fact]
    public async Task Khai_bao_may_chu_thieu_thong_tin_thi_bi_chan()
    {
        var client = await ClientAsync();

        var missingHost = await client.PostAsJsonAsync("/api/interlibrary/targets", new
        {
            name = "Thiếu địa chỉ",
            port = 210,
            databaseName = "DB"
        });

        missingHost.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorTextAsync(missingHost)).Should().Contain("địa chỉ máy chủ");

        var missingSru = await client.PostAsJsonAsync("/api/interlibrary/targets", new
        {
            name = "SRU thiếu địa chỉ",
            useSru = true
        });

        missingSru.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorTextAsync(missingSru)).Should().Contain("SRU");
    }

    [Fact]
    public async Task Sua_may_chu_bo_trong_mat_khau_thi_giu_nguyen_mat_khau_cu()
    {
        var client = await ClientAsync();

        var id = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/interlibrary/targets", new
        {
            name = $"Máy chủ thử {Unique()}",
            host = "z3950.example.org",
            port = 210,
            databaseName = "DB",
            username = "user",
            password = "matkhaucu"
        }));

        await ReadAsync<Guid>(await client.PutAsJsonAsync($"/api/interlibrary/targets/{id}", new
        {
            name = "Máy chủ đã đổi tên",
            host = "z3950.example.org",
            port = 210,
            databaseName = "DB",
            username = "user"
        }));

        var targets = await ReadAsync<IReadOnlyList<Z3950TargetDto>>(
            await client.GetAsync("/api/interlibrary/targets?includeInactive=true"));

        var target = targets.First(row => row.Id == id);

        target.Name.Should().Be("Máy chủ đã đổi tên");
        target.Username.Should().Be("user");
    }

    [Fact]
    public async Task Kiem_tra_may_chu_khong_ton_tai_thi_bao_loi_ro_chu_khong_treo()
    {
        var client = await ClientAsync();

        var id = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/interlibrary/targets", new
        {
            name = $"Máy chủ không có thật {Unique()}",
            host = "khong-co-may-chu-nay.invalid",
            port = 210,
            databaseName = "DB",
            timeoutSeconds = 3
        }));

        var result = await ReadAsync<Z3950CheckResultDto>(
            await client.PostAsync($"/api/interlibrary/targets/{id}/check", null));

        result.Success.Should().BeFalse();
        result.Message.Should().NotBeNullOrWhiteSpace();

        // Kết quả kiểm tra phải được ghi lại để cán bộ nhìn danh sách là biết máy chủ nào hỏng.
        var targets = await ReadAsync<IReadOnlyList<Z3950TargetDto>>(
            await client.GetAsync("/api/interlibrary/targets?includeInactive=true"));

        var target = targets.First(row => row.Id == id);

        target.LastCheckOk.Should().BeFalse();
        target.LastCheckedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Tra_cuu_khi_khong_co_may_chu_nao_duoc_chon_thi_bao_ro()
    {
        var client = await ClientAsync();

        var response = await client.PostAsJsonAsync("/api/interlibrary/search", new
        {
            targetIds = new[] { Guid.NewGuid() },
            field = "Title",
            term = "test"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ErrorTextAsync(response)).Should().Contain("máy chủ");
    }

    [Fact]
    public async Task Chuan_bi_bieu_ghi_lay_ve_thi_ghi_nguon_va_bo_so_kiem_soat_cu()
    {
        var client = await ClientAsync();

        var id = await ReadAsync<Guid>(await client.PostAsJsonAsync("/api/interlibrary/targets", new
        {
            name = $"Thư viện bạn {Unique()}",
            host = "z3950.bantoi.org",
            port = 210,
            databaseName = "CATALOG"
        }));

        var remote = new MarcRecord { ControlNumber = "REMOTE-12345" };
        remote.AddField("245", '1', '0').AddSubfield('a', "Sách của thư viện bạn");

        var prepared = await ReadAsync<string>(await client.PostAsJsonAsync(
            $"/api/interlibrary/targets/{id}/prepare",
            new { marcJson = MarcJson.Serialize(remote) }));

        var record = MarcJson.Deserialize(prepared);

        record.ControlNumber.Should().BeNull(
            "số kiểm soát của thư viện bạn không dùng lại được, kho mình cấp số riêng");
        record.GetSubfield("035", 'a').Should().Contain("REMOTE-12345");
        record.GetSubfield("035", 'a').Should().Contain("z3950.bantoi.org");
    }

    /// <summary>
    /// Vòng khép kín: khai chính địa chỉ /oai của mình làm nguồn thu hoạch rồi chạy thu hoạch.
    ///
    /// Đây là phép thử mạnh nhất của cả phân hệ vì nó chạy qua đủ cả hai chiều: provider phát ra
    /// đúng chuẩn thì harvester mới đọc được, và ngược lại. Không phụ thuộc máy chủ nào ngoài
    /// Internet nên chạy được cả khi máy không có mạng.
    /// </summary>
    [Fact]
    public async Task Thu_hoach_chinh_kho_cua_minh_ve_lai_thanh_cong()
    {
        var client = await ClientAsync();
        await NewBibAsync(client, "Sách để thu hoạch", "SACH");

        var baseUrl = new Uri(_factory.Server.BaseAddress!, "/oai").ToString();

        var identify = await ReadAsync<OaiIdentifyDto>(await client.GetAsync(
            $"/api/interlibrary/oai/identify?baseUrl={Uri.EscapeDataString(baseUrl)}"));

        identify.ProtocolVersion.Should().Be("2.0");
        identify.MetadataPrefixes.Should().Contain("oai_dc");
        identify.Sets.Should().NotBeEmpty();

        // Lọc theo bộ "sách": biểu ghi thu về chưa được gán dạng tài liệu nên không thuộc bộ nào,
        // nhờ vậy lần chạy sau không tự thu lại chính thứ vừa tạo ra — đồng thời đây cũng là phép
        // thử cho phần lọc theo bộ của chuẩn.
        var repositoryId = await ReadAsync<Guid>(await client.PostAsJsonAsync(
            "/api/interlibrary/oai/repositories", new
            {
                name = $"Chính kho của mình {Unique()}",
                baseUrl,
                metadataPrefix = "oai_dc",
                setSpec = "doctype:SACH"
            }));

        var log = await ReadAsync<OaiHarvestLogDto>(await client.PostAsync(
            $"/api/interlibrary/oai/repositories/{repositoryId}/harvest?fullReload=true", null));

        log.Status.Should().Be("Completed", log.Errors ?? "không có lỗi");
        log.RecordsFetched.Should().BeGreaterThan(0);
        log.RecordsImported.Should().BeGreaterThan(0, log.Errors ?? "khong co loi ghi lai");

        // Chạy lại lần nữa: biểu ghi đã thu về không được nhân đôi.
        var again = await ReadAsync<OaiHarvestLogDto>(await client.PostAsync(
            $"/api/interlibrary/oai/repositories/{repositoryId}/harvest?fullReload=true", null));

        again.RecordsImported.Should().Be(0,
            "biểu ghi đã thu về lần trước thì phải bỏ qua chứ không tạo bản trùng");
        again.RecordsSkipped.Should().Be(again.RecordsFetched);

        var logs = await ReadAsync<PagedResult<OaiHarvestLogDto>>(await client.GetAsync(
            $"/api/interlibrary/oai/harvest-logs?repositoryId={repositoryId}&page=1&pageSize=10"));

        logs.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Bieu_ghi_thu_hoach_ve_vao_hang_doi_bien_muc_va_ghi_nguon()
    {
        var client = await ClientAsync();
        var (_, title) = await NewBibAsync(client, "Sách kiểm nguồn thu hoạch", "SACH");

        var baseUrl = new Uri(_factory.Server.BaseAddress!, "/oai").ToString();

        var repositoryId = await ReadAsync<Guid>(await client.PostAsJsonAsync(
            "/api/interlibrary/oai/repositories", new
            {
                name = $"Nguồn kiểm tra {Unique()}",
                baseUrl,
                metadataPrefix = "oai_dc",
                setSpec = "doctype:SACH"
            }));

        await ReadAsync<OaiHarvestLogDto>(await client.PostAsync(
            $"/api/interlibrary/oai/repositories/{repositoryId}/harvest?fullReload=true", null));

        var found = await ReadAsync<PagedResult<Application.Features.Cataloging.BibListItemDto>>(
            await client.GetAsync(
                $"/api/cataloging/bibs?page=1&pageSize=50&keyword={Uri.EscapeDataString(title)}"));

        // Biểu ghi gốc và bản thu hoạch về đứng cạnh nhau.
        found.Items.Should().HaveCountGreaterThanOrEqualTo(2,
            "thu hoạch tạo thêm một bản dựng từ Dublin Core bên cạnh bản gốc");

        // Bản thu hoạch phải nằm ở hàng đợi biên mục, vì Dublin Core còn nghèo, cán bộ phải hiệu đính.
        found.Items.Should().Contain(item => item.Status == Domain.Enums.RecordStatus.Queued);
    }

    [Fact]
    public async Task Kho_OAI_khai_dia_chi_khong_hop_le_thi_bi_chan()
    {
        var client = await ClientAsync();

        var response = await client.PostAsJsonAsync("/api/interlibrary/oai/repositories", new
        {
            name = "Địa chỉ sai",
            baseUrl = "khong-phai-dia-chi"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorTextAsync(response)).Should().Contain("HTTP");
    }

    [Fact]
    public async Task Phan_quyen_bao_ve_phan_khai_bao_may_chu()
    {
        var reader = _factory.CreateClient();

        var response = await reader.GetAsync("/api/interlibrary/targets");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
