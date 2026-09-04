using System.Diagnostics;
using System.Globalization;
using System.Text;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Application.Common.Text;
using Microsoft.Extensions.Logging;
using PDFtoImage;
using SkiaSharp;
using UglyToad.PdfPig;

namespace LibraryConnect.Infrastructure.Services;

/// <summary>
/// Xử lý tệp tài liệu số bằng ba công cụ, mỗi công cụ làm đúng phần nó giỏi:
///
/// <list type="bullet">
/// <item>PdfPig đọc lớp chữ có sẵn trong PDF — nhanh, chạy hoàn toàn trong tiến trình.</item>
/// <item>PDFium kết xuất từng trang thành ảnh để đọc trực tuyến và để nhận dạng khi cần.</item>
/// <item>Tesseract nhận dạng ký tự tiếng Việt cho những bản quét không có lớp chữ.</item>
/// </list>
///
/// Nhận dạng ký tự chạy trong tiến trình con vì bộ dữ liệu ngôn ngữ nằm ngoài ứng dụng; máy chủ
/// chưa cài Tesseract thì tài liệu vẫn dùng được, chỉ mất phần tìm kiếm toàn văn của bản quét, và
/// hệ thống nói rõ điều đó thay vì im lặng.
/// </summary>
public class DocumentProcessor : IDocumentProcessor
{
    private static readonly TimeSpan OcrTimeout = TimeSpan.FromMinutes(5);

    /// <summary>Bao nhiêu ký tự trên một trang thì coi là tệp có lớp chữ thật sự.</summary>
    private const int MinimumCharactersPerPage = 60;

    private readonly ISystemParameterService _parameters;
    private readonly ILogger<DocumentProcessor> _logger;

    public DocumentProcessor(ISystemParameterService parameters, ILogger<DocumentProcessor> logger)
    {
        _parameters = parameters;
        _logger = logger;
    }

    public bool CanRenderPages(string mimeType) =>
        mimeType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);

    public async Task<bool> IsOcrAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var command = await GetOcrCommandAsync(ct);
            var (exitCode, _, _) = await RunAsync(command, new[] { "--version" }, null, ct);
            return exitCode == 0;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return false;
        }
    }

    public Task<IReadOnlyList<string>> ExtractPageTextsAsync(
        byte[] content, string mimeType, CancellationToken ct = default)
    {
        if (!CanRenderPages(mimeType))
        {
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        ct.ThrowIfCancellationRequested();

        using var stream = new MemoryStream(content, writable: false);
        using var pdf = PdfDocument.Open(stream);
        var pages = new List<string>(pdf.NumberOfPages);

        foreach (var page in pdf.GetPages())
        {
            ct.ThrowIfCancellationRequested();
            pages.Add(page.Text);
        }

        return Task.FromResult<IReadOnlyList<string>>(pages);
    }

    public Task<IReadOnlyList<DocumentOutlineEntry>> ExtractOutlineAsync(
        byte[] content, string mimeType, CancellationToken ct = default)
    {
        if (!CanRenderPages(mimeType))
        {
            return Task.FromResult<IReadOnlyList<DocumentOutlineEntry>>(Array.Empty<DocumentOutlineEntry>());
        }

        ct.ThrowIfCancellationRequested();

        using var stream = new MemoryStream(content, writable: false);
        using var pdf = PdfDocument.Open(stream);

        if (!pdf.TryGetBookmarks(out var bookmarks))
        {
            return Task.FromResult<IReadOnlyList<DocumentOutlineEntry>>(Array.Empty<DocumentOutlineEntry>());
        }

        // Làm phẳng cây bookmark theo thứ tự đọc (cha rồi tới con), giữ độ sâu để ứng dụng thụt lề.
        var entries = new List<DocumentOutlineEntry>();

        void Walk(IEnumerable<UglyToad.PdfPig.Outline.BookmarkNode> nodes, int level)
        {
            foreach (var node in nodes)
            {
                ct.ThrowIfCancellationRequested();

                var title = node.Title?.Trim();
                if (string.IsNullOrEmpty(title))
                {
                    title = "…";
                }

                int? page = node is UglyToad.PdfPig.Outline.DocumentBookmarkNode inDocument
                    ? inDocument.PageNumber
                    : null;

                entries.Add(new DocumentOutlineEntry(level, title, page));
                Walk(node.Children, level + 1);
            }
        }

        Walk(bookmarks.Roots, 0);

        return Task.FromResult<IReadOnlyList<DocumentOutlineEntry>>(entries);
    }

    public Task<DocumentInspection> InspectAsync(
        byte[] content, string mimeType, CancellationToken ct = default)
    {
        if (!CanRenderPages(mimeType))
        {
            // Định dạng khác PDF (video, âm thanh, ảnh, EPUB) không có lớp chữ để rút; số trang
            // cũng không có nghĩa. Trả về rỗng chứ không đoán bừa.
            return Task.FromResult(new DocumentInspection(null, string.Empty, false));
        }

        ct.ThrowIfCancellationRequested();

        using var stream = new MemoryStream(content, writable: false);
        using var pdf = PdfDocument.Open(stream);

        var builder = new StringBuilder();

        foreach (var page in pdf.GetPages())
        {
            ct.ThrowIfCancellationRequested();
            builder.AppendLine(page.Text);
        }

        var text = PlainText.RemoveUnstorableCharacters(builder.ToString());
        var pageCount = pdf.NumberOfPages;
        var needsOcr = pageCount > 0 && text.Length < pageCount * MinimumCharactersPerPage;

        return Task.FromResult(new DocumentInspection(pageCount, text, needsOcr));
    }

    public Task<byte[]> RenderPageAsync(
        byte[] content, int pageNumber, int dpi, WatermarkOptions? watermark, CancellationToken ct = default)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Số trang bắt đầu từ 1.");
        }

        ct.ThrowIfCancellationRequested();

        // PDFium chạy trên Windows, Linux và macOS — đúng những nền tảng sản phẩm hỗ trợ theo mục 1
        // của đặc tả, nên cảnh báo tương thích nền tảng ở đây không nói lên điều gì.
#pragma warning disable CA1416
        using var bitmap = Conversion.ToImage(
            content, new Index(pageNumber - 1), password: null, options: new RenderOptions(Dpi: dpi));
#pragma warning restore CA1416

        if (watermark is not null && watermark.Lines.Count > 0)
        {
            DrawWatermark(bitmap, watermark.Lines);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);

        return Task.FromResult(data.ToArray());
    }

    public async Task<string> RecognizeTextAsync(byte[] pageImagePng, CancellationToken ct = default)
    {
        var command = await GetOcrCommandAsync(ct);
        var language = await _parameters.GetAsync("DIGITAL.OCR_LANGUAGE", "vie", ct);

        var workDirectory = Path.Combine(Path.GetTempPath(), "lc-ocr", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDirectory);

        var imagePath = Path.Combine(workDirectory, "page.png");

        try
        {
            await File.WriteAllBytesAsync(imagePath, pageImagePng, ct);

            var (exitCode, output, error) = await RunAsync(
                command, new[] { imagePath, "stdout", "-l", language }, workDirectory, ct);

            if (exitCode != 0)
            {
                _logger.LogWarning("Nhận dạng ký tự thất bại (mã {ExitCode}): {Error}", exitCode, error);
                return string.Empty;
            }

            return PlainText.RemoveUnstorableCharacters(output);
        }
        finally
        {
            TryDeleteDirectory(workDirectory);
        }
    }

    // ---------------------------------------------------------------------------------------------

    private async Task<string> GetOcrCommandAsync(CancellationToken ct) =>
        await _parameters.GetAsync("DIGITAL.OCR_COMMAND", "tesseract", ct);

    /// <summary>
    /// Đóng chữ chìm chéo trang, lặp lại theo chiều dọc để cắt một khúc ảnh vẫn còn dấu vết.
    /// </summary>
    private static void DrawWatermark(SKBitmap bitmap, IReadOnlyList<string> lines)
    {
        var fontSize = Math.Max(16f, bitmap.Width / 26f);

        using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint
        {
            Color = new SKColor(190, 30, 30, 46),
            TextSize = fontSize,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("DejaVu Sans", SKFontStyle.Bold)
                ?? SKTypeface.CreateDefault(),
        };

        var lineHeight = fontSize * 1.25f;
        var blockHeight = lineHeight * lines.Count;
        var blockWidth = lines.Max(line => paint.MeasureText(line));

        var stepY = blockHeight + fontSize * 2.6f;
        var stepX = blockWidth + fontSize * 3f;

        canvas.Save();
        canvas.RotateDegrees(-32, bitmap.Width / 2f, bitmap.Height / 2f);

        // Lát kín cả hai chiều: xoay xong thì bốn góc trang nằm ngoài khung vẽ, nên phải vẽ tràn ra
        // quá mép mới không để lại khoảng trắng ở góc. Vẽ thiếu thì cắt một khúc ảnh là mất dấu vết.
        var margin = Math.Max(bitmap.Width, bitmap.Height) * 0.45f;

        for (var y = -margin; y < bitmap.Height + margin; y += stepY)
        {
            for (var x = -margin; x < bitmap.Width + margin; x += stepX)
            {
                for (var index = 0; index < lines.Count; index++)
                {
                    canvas.DrawText(lines[index], x, y + index * lineHeight, paint);
                }
            }
        }

        canvas.Restore();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunAsync(
        string fileName, IReadOnlyList<string> arguments, string? workingDirectory, CancellationToken ct)
    {
        var info = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        if (workingDirectory is not null)
        {
            info.WorkingDirectory = workingDirectory;
        }

        using var process = new Process { StartInfo = info };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(ct);
        var errorTask = process.StandardError.ReadToEndAsync(ct);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(OcrTimeout);

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            TryKill(process);
            throw new TimeoutException(
                $"Công cụ {fileName} chạy quá {OcrTimeout.TotalMinutes.ToString(CultureInfo.InvariantCulture)} phút.");
        }

        return (process.ExitCode, await outputTask, await errorTask);
    }

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Tiến trình đã tự thoát giữa lúc chờ và lúc gọi kill.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
            // Thư mục tạm để lại thì lần dọn rác của hệ điều hành sẽ xóa; không đáng làm hỏng yêu cầu.
        }
    }
}
