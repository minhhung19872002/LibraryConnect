using LibraryConnect.Application.Features.InterLibrary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryConnect.Api.Controllers;

/// <summary>
/// Hai giao thức mở ra cho thư viện khác dùng: SRU để tra cứu, OAI-PMH để thu hoạch metadata.
///
/// Không nằm dưới tiền tố /api vì địa chỉ của chúng là một phần của chuẩn — thư viện bạn khai vào
/// phần mềm của họ đúng chuỗi này. Cũng không đòi đăng nhập: cả hai chuẩn sinh ra để phục vụ tra
/// cứu công khai, còn muốn siết thì chặn ở tầng máy chủ web.
/// </summary>
[ApiController]
[AllowAnonymous]
[Tags("Giao thức liên thư viện")]
public class ProtocolController : ControllerBase
{
    private readonly MediatR.ISender _mediator;

    public ProtocolController(MediatR.ISender mediator) => _mediator = mediator;

    /// <summary>
    /// SRU 1.2 — tra cứu qua HTTP (mục 3.3).
    ///
    /// Ví dụ: <c>/sru?operation=searchRetrieve&amp;version=1.2&amp;query=dc.title="cơ sở dữ
    /// liệu"&amp;recordSchema=marcxml</c>. Gọi trần trụi không tham số thì trả về bản tự khai.
    /// </summary>
    [HttpGet("/sru")]
    [Produces("application/xml")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Sru(
        [FromQuery] string? operation,
        [FromQuery] string? version,
        [FromQuery] string? query,
        [FromQuery] int startRecord,
        [FromQuery] int maximumRecords,
        [FromQuery] string? recordSchema,
        [FromQuery] string? recordPacking,
        CancellationToken ct)
    {
        var request = new SruRequest
        {
            Operation = operation,
            Version = version,
            Query = query,
            StartRecord = startRecord <= 0 ? 1 : startRecord,
            MaximumRecords = maximumRecords <= 0 ? 10 : maximumRecords,
            RecordSchema = recordSchema,
            RecordPacking = recordPacking,
        };

        var xml = await _mediator.Send(new HandleSruRequestQuery(request, BaseUrl("/sru")), ct);

        return Content(xml, "application/xml; charset=utf-8");
    }

    /// <summary>
    /// OAI-PMH 2.0 — sáu verb theo chuẩn (mục 3.4).
    ///
    /// Ví dụ: <c>/oai?verb=ListRecords&amp;metadataPrefix=oai_dc</c>.
    /// </summary>
    [HttpGet("/oai")]
    [Produces("application/xml")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> OaiGet(
        [FromQuery] string? verb,
        [FromQuery] string? identifier,
        [FromQuery] string? metadataPrefix,
        [FromQuery] string? set,
        [FromQuery] string? from,
        [FromQuery] string? until,
        [FromQuery] string? resumptionToken,
        CancellationToken ct) =>
        HandleOaiAsync(
            new OaiRequest
            {
                Verb = verb,
                Identifier = identifier,
                MetadataPrefix = metadataPrefix,
                Set = set,
                From = from,
                Until = until,
                ResumptionToken = resumptionToken,
            },
            ct);

    /// <summary>Chuẩn OAI-PMH bắt buộc hỗ trợ cả POST dạng biểu mẫu, không chỉ GET.</summary>
    [HttpPost("/oai")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/xml")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> OaiPost([FromForm] OaiRequest request, CancellationToken ct) =>
        HandleOaiAsync(request, ct);

    private async Task<IActionResult> HandleOaiAsync(OaiRequest request, CancellationToken ct)
    {
        var xml = await _mediator.Send(new HandleOaiRequestQuery(request, BaseUrl("/oai")), ct);

        return Content(xml, "application/xml; charset=utf-8");
    }

    /// <summary>
    /// Địa chỉ cơ sở mà máy chủ tự khai trong phần trả lời.
    ///
    /// Đứng sau Nginx thì địa chỉ thật nằm ở tiêu đề chuyển tiếp; lấy sai chỗ này là nơi thu hoạch
    /// ghi lại một địa chỉ nội bộ mà từ ngoài không gọi được.
    /// </summary>
    private string BaseUrl(string path)
    {
        var scheme = Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? Request.Scheme;
        var host = Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? Request.Host.Value;

        return $"{scheme}://{host}{path}";
    }
}
