using System.Text.Json;
using LibraryConnect.Domain.Entities.Acq;
using LibraryConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Infrastructure.Persistence.Seeding;

public partial class DatabaseSeeder
{
    /// <summary>
    /// Nạp mẫu tem mã vạch và mẫu nhãn gáy mặc định (III.2).
    ///
    /// A library should be able to print a barcode on day one without designing a label first, so one
    /// working template of each kind ships with the system. Both are sized for the label sheets sold
    /// in Vietnam — 50×25 mm barcode labels, 5 across on A4, and 35×45 mm spine labels — and the
    /// librarian edits or replaces them in Bổ sung → Mẫu tem và nhãn.
    /// </summary>
    private async Task SeedLabelTemplatesAsync(CancellationToken ct)
    {
        var seeded = 0;

        if (!await _db.BarcodeTemplates.AnyAsync(ct))
        {
            _db.BarcodeTemplates.Add(new BarcodeTemplate
            {
                Code = "TEM50X25",
                Name = "Tem mã vạch 50×25 mm (4 cột × 10 hàng trên A4)",
                WidthMm = 50,
                HeightMm = 25,
                BarcodeType = BarcodeType.Code128,
                ColumnsPerPage = 4,
                RowsPerPage = 10,
                MarginTopMm = 12,
                MarginLeftMm = 5,
                IsDefault = true,
                IsActive = true,
                CreatedAt = _clock.Now,
                Layout = Json(new
                {
                    padding = 1.5,
                    showBorder = false,
                    barcode = new
                    {
                        x = 3.0,
                        y = 6.0,
                        width = 41.0,
                        height = 11.0,
                        showText = true,
                        fontSize = 6.5,
                        source = "barcode"
                    },
                    boxes = new object[]
                    {
                        new
                        {
                            source = "libraryName",
                            x = 1.0, y = 0.5, width = 45.0, height = 4.0,
                            fontSize = 6.5, bold = true, italic = false, align = "center", border = false
                        },
                        new
                        {
                            source = "callNumber",
                            x = 1.0, y = 20.0, width = 45.0, height = 4.0,
                            fontSize = 6.5, bold = false, italic = false, align = "center", border = false
                        }
                    }
                })
            });

            seeded++;
        }

        if (!await _db.LabelTemplates.AnyAsync(ct))
        {
            _db.LabelTemplates.Add(new LabelTemplate
            {
                Code = "NHANGAY35X45",
                Name = "Nhãn gáy 35×45 mm (5 cột × 6 hàng trên A4)",
                WidthMm = 35,
                HeightMm = 45,
                ColumnsPerPage = 5,
                RowsPerPage = 6,
                MarginTopMm = 12,
                MarginLeftMm = 8,
                IsDefault = true,
                IsActive = true,
                CreatedAt = _clock.Now,
                Layout = Json(new
                {
                    padding = 2.0,
                    showBorder = true,
                    // Nhãn gáy không mang mã vạch: nó nằm ở gáy sách để người đi dọc giá đọc được ký
                    // hiệu xếp giá, còn mã vạch dán trong bìa để máy quét ở quầy đọc.
                    boxes = new object[]
                    {
                        new
                        {
                            source = "libraryName",
                            x = 0.0, y = 0.0, width = 31.0, height = 5.0,
                            fontSize = 6.0, bold = false, italic = false, align = "center", border = false
                        },
                        new
                        {
                            source = "callNumberLine1",
                            x = 0.0, y = 8.0, width = 31.0, height = 7.0,
                            fontSize = 13.0, bold = true, italic = false, align = "center", border = false
                        },
                        new
                        {
                            source = "callNumberLine2",
                            x = 0.0, y = 17.0, width = 31.0, height = 7.0,
                            fontSize = 13.0, bold = true, italic = false, align = "center", border = false
                        },
                        new
                        {
                            source = "callNumberLine3",
                            x = 0.0, y = 26.0, width = 31.0, height = 6.0,
                            fontSize = 11.0, bold = false, italic = false, align = "center", border = false
                        },
                        new
                        {
                            source = "registerNumber",
                            x = 0.0, y = 35.0, width = 31.0, height = 5.0,
                            fontSize = 6.5, bold = false, italic = false, align = "center", border = false
                        }
                    }
                })
            });

            seeded++;
        }

        if (seeded == 0)
        {
            return;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Đã nạp {Count} mẫu tem / nhãn mặc định", seeded);
    }


    /// <summary>
    /// Nạp mẫu biểu in mặc định cho các chứng từ của phân hệ bổ sung (III.6).
    ///
    /// Each template follows the layout of a Vietnamese administrative document — organisation block
    /// on the left, national heading on the right, the document name in capitals, the detail table,
    /// then the signature boxes — so a librarian can print, sign and file it on the first day. The
    /// designer screen edits every part of them.
    /// </summary>
    private async Task SeedFormTemplatesAsync(CancellationToken ct)
    {
        if (await _db.FormTemplates.AnyAsync(ct))
        {
            return;
        }

        var organisation = new[] { "{libraryName}" };
        var noIntro = Array.Empty<string>();

        _db.FormTemplates.AddRange(
            FormTemplate("BB-BANGIAO", "Biên bản bàn giao tài liệu", "HANDOVER", Json(new
            {
                showLogo = true,
                organisationLines = organisation,
                showNationalHeading = true,
                title = "Biên bản bàn giao tài liệu",
                subtitle = "Số: {code}",
                introLines = new[]
                {
                    "Hôm nay, ngày {day} tháng {month} năm {year}, tại {libraryName}, chúng tôi gồm:"
                },
                fields = new object[]
                {
                    new { label = "Bên giao", key = "partyA", fullWidth = true },
                    new { label = "Bên nhận", key = "partyB", fullWidth = true },
                    new { label = "Theo đơn đặt số", key = "orderCode", fullWidth = false },
                    new { label = "Số hợp đồng", key = "contractNo", fullWidth = false },
                    new { label = "Nội dung", key = "content", fullWidth = true }
                },
                columns = new object[]
                {
                    new { header = "TT", key = "index", width = 0.5, align = "center", sum = false },
                    new { header = "Nhan đề", key = "title", width = 4.0, align = "left", sum = false },
                    new { header = "Tác giả", key = "author", width = 2.0, align = "left", sum = false },
                    new { header = "ISBN", key = "isbn", width = 1.5, align = "left", sum = false },
                    new { header = "Số lượng", key = "quantity", width = 1.0, align = "right", sum = true },
                    new { header = "Đơn giá", key = "unitPrice", width = 1.2, align = "right", sum = false },
                    new { header = "Thành tiền", key = "amount", width = 1.4, align = "right", sum = true },
                    // III.1 đòi biên bản ghi cả tình trạng từng dòng, không chỉ số lượng.
                    new { header = "Tình trạng", key = "condition", width = 1.6, align = "left", sum = false }
                },
                showTotals = true,
                closingLines = new[]
                {
                    "Tổng số: {totalItems} bản, tổng giá trị: {totalAmount} đồng.",
                    "Biên bản được lập thành 02 bản có giá trị như nhau, mỗi bên giữ 01 bản."
                },
                signatures = new object[]
                {
                    new { role = "Đại diện bên giao", note = "(Ký, ghi rõ họ tên)" },
                    new { role = "Đại diện bên nhận", note = "(Ký, ghi rõ họ tên)" }
                },
                fontSize = 10.0
            })),

            FormTemplate("DH-DATHANG", "Đơn đặt hàng gửi nhà cung cấp", "ORDER", Json(new
            {
                showLogo = true,
                organisationLines = organisation,
                showNationalHeading = true,
                title = "Đơn đặt hàng",
                subtitle = "Số: {code} — Ngày {orderDate}",
                introLines = new[] { "Kính gửi: {supplierName}" },
                fields = new object[]
                {
                    new { label = "Địa chỉ", key = "supplierAddress", fullWidth = true },
                    new { label = "Điện thoại", key = "supplierPhone", fullWidth = false },
                    new { label = "Mã số thuế", key = "supplierTaxCode", fullWidth = false },
                    new { label = "Số hợp đồng", key = "contractNo", fullWidth = false },
                    new { label = "Ngày dự kiến giao", key = "expectedDate", fullWidth = false },
                    new { label = "Nguồn kinh phí", key = "fundingSourceName", fullWidth = true }
                },
                columns = new object[]
                {
                    new { header = "TT", key = "index", width = 0.5, align = "center", sum = false },
                    new { header = "Nhan đề", key = "title", width = 4.0, align = "left", sum = false },
                    new { header = "Tác giả", key = "author", width = 2.0, align = "left", sum = false },
                    new { header = "ISBN", key = "isbn", width = 1.5, align = "left", sum = false },
                    new { header = "Số lượng", key = "quantity", width = 1.0, align = "right", sum = true },
                    new { header = "Đơn giá", key = "unitPrice", width = 1.2, align = "right", sum = false },
                    new { header = "Thành tiền", key = "amount", width = 1.4, align = "right", sum = true }
                },
                showTotals = true,
                closingLines = new[]
                {
                    "Tổng giá trị đơn hàng: {totalAmount} đồng.",
                    "Đề nghị Quý đơn vị giao hàng đúng thời hạn và đúng danh mục nêu trên."
                },
                signatures = new object[]
                {
                    new { role = "Người lập đơn", note = "(Ký, ghi rõ họ tên)" },
                    new { role = "Thủ trưởng đơn vị", note = "(Ký tên, đóng dấu)" }
                },
                fontSize = 10.0
            })),

            FormTemplate("PN-NHAPKHO", "Phiếu nhập kho", "RECEIPT", Json(new
            {
                showLogo = false,
                organisationLines = organisation,
                showNationalHeading = true,
                title = "Phiếu nhập kho",
                subtitle = "Ngày {receiptDate}",
                introLines = noIntro,
                fields = new object[]
                {
                    new { label = "Theo đơn đặt số", key = "orderCode", fullWidth = false },
                    new { label = "Nhà cung cấp", key = "supplierName", fullWidth = false },
                    new { label = "Nhập vào kho", key = "warehouseName", fullWidth = false },
                    new { label = "Nguồn kinh phí", key = "fundingSourceName", fullWidth = false }
                },
                columns = new object[]
                {
                    new { header = "TT", key = "index", width = 0.5, align = "center", sum = false },
                    new { header = "Mã vạch", key = "barcode", width = 1.5, align = "left", sum = false },
                    new { header = "Số ĐKCB", key = "registerNumber", width = 1.5, align = "left", sum = false },
                    new { header = "Nhan đề", key = "title", width = 4.0, align = "left", sum = false },
                    new { header = "Ký hiệu xếp giá", key = "callNumber", width = 1.5, align = "left", sum = false },
                    new { header = "Giá", key = "price", width = 1.2, align = "right", sum = true }
                },
                showTotals = true,
                closingLines = new[] { "Tổng số: {totalItems} bản, tổng giá trị: {totalAmount} đồng." },
                signatures = new object[]
                {
                    new { role = "Người giao", note = "(Ký, ghi rõ họ tên)" },
                    new { role = "Thủ kho", note = "(Ký, ghi rõ họ tên)" },
                    new { role = "Thủ trưởng đơn vị", note = "(Ký tên, đóng dấu)" }
                },
                fontSize = 9.5
            })),

            FormTemplate("PCK-CHUYENKHO", "Phiếu chuyển kho", "TRANSFER", Json(new
            {
                showLogo = false,
                organisationLines = organisation,
                showNationalHeading = true,
                title = "Phiếu chuyển kho",
                subtitle = "Số: {code} — Ngày {movementDate}",
                introLines = noIntro,
                fields = new object[]
                {
                    new { label = "Chuyển từ kho", key = "fromWarehouse", fullWidth = false },
                    new { label = "Đến kho", key = "toWarehouse", fullWidth = false },
                    new { label = "Số quyết định", key = "decisionNo", fullWidth = false },
                    new { label = "Người thực hiện", key = "performedBy", fullWidth = false },
                    new { label = "Lý do chuyển", key = "reason", fullWidth = true }
                },
                columns = new object[]
                {
                    new { header = "TT", key = "index", width = 0.5, align = "center", sum = false },
                    new { header = "Mã vạch", key = "barcode", width = 1.5, align = "left", sum = false },
                    new { header = "Số ĐKCB", key = "registerNumber", width = 1.5, align = "left", sum = false },
                    new { header = "Nhan đề", key = "title", width = 4.0, align = "left", sum = false },
                    new { header = "Ký hiệu xếp giá", key = "callNumber", width = 1.5, align = "left", sum = false },
                    new { header = "Tình trạng", key = "condition", width = 1.2, align = "left", sum = false },
                    new { header = "Giá", key = "price", width = 1.2, align = "right", sum = true }
                },
                showTotals = true,
                closingLines = new[] { "Tổng số: {totalItems} bản, tổng giá trị: {totalAmount} đồng." },
                signatures = new object[]
                {
                    new { role = "Thủ kho giao", note = "(Ký, ghi rõ họ tên)" },
                    new { role = "Thủ kho nhận", note = "(Ký, ghi rõ họ tên)" },
                    new { role = "Thủ trưởng đơn vị", note = "(Ký tên, đóng dấu)" }
                },
                fontSize = 9.5
            })),

            FormTemplate("QD-THANHLY", "Danh mục tài liệu thanh lý", "DISPOSAL", Json(new
            {
                showLogo = false,
                organisationLines = organisation,
                showNationalHeading = true,
                title = "Danh mục tài liệu thanh lý",
                subtitle = "Kèm theo quyết định số: {decisionNo} ngày {disposalDate}",
                introLines = noIntro,
                fields = new object[]
                {
                    new { label = "Hình thức", key = "disposalType", fullWidth = false },
                    new { label = "Người duyệt", key = "approvedBy", fullWidth = false },
                    new { label = "Lý do", key = "reason", fullWidth = true }
                },
                columns = new object[]
                {
                    new { header = "TT", key = "index", width = 0.5, align = "center", sum = false },
                    new { header = "Mã vạch", key = "barcode", width = 1.5, align = "left", sum = false },
                    new { header = "Số ĐKCB", key = "registerNumber", width = 1.5, align = "left", sum = false },
                    new { header = "Nhan đề", key = "title", width = 4.0, align = "left", sum = false },
                    new { header = "Kho", key = "warehouseName", width = 1.5, align = "left", sum = false },
                    new { header = "Giá trị", key = "price", width = 1.2, align = "right", sum = true }
                },
                showTotals = true,
                closingLines = new[] { "Tổng số: {totalItems} bản, tổng giá trị: {totalAmount} đồng." },
                signatures = new object[]
                {
                    new { role = "Người lập", note = "(Ký, ghi rõ họ tên)" },
                    new { role = "Hội đồng thanh lý", note = "(Ký, ghi rõ họ tên)" },
                    new { role = "Thủ trưởng đơn vị", note = "(Ký tên, đóng dấu)" }
                },
                fontSize = 9.5
            })),

            FormTemplate("BB-KIEMKE", "Biên bản kiểm kê kho", "INVENTORY", Json(new
            {
                showLogo = false,
                organisationLines = organisation,
                showNationalHeading = true,
                title = "Biên bản kiểm kê kho tài liệu",
                subtitle = "Kỳ kiểm kê: {code} — {name}",
                introLines = new[]
                {
                    "Hôm nay, ngày {day} tháng {month} năm {year}, hội đồng kiểm kê tiến hành kiểm kê kho {warehouseName}."
                },
                fields = new object[]
                {
                    new { label = "Thời gian kiểm kê", key = "startDate", fullWidth = false },
                    new { label = "Đến ngày", key = "endDate", fullWidth = false },
                    new { label = "Cán bộ kiểm kê", key = "assignedStaff", fullWidth = true },
                    new { label = "Số bản theo sổ", key = "expectedCount", fullWidth = false },
                    new { label = "Số bản khớp", key = "matchCount", fullWidth = false },
                    new { label = "Số bản thiếu", key = "missingCount", fullWidth = false },
                    new { label = "Số bản thừa", key = "unexpectedCount", fullWidth = false },
                    new { label = "Số bản sai kho", key = "wrongWarehouseCount", fullWidth = false }
                },
                columns = new object[]
                {
                    new { header = "TT", key = "index", width = 0.5, align = "center", sum = false },
                    new { header = "Mã vạch", key = "barcode", width = 1.5, align = "left", sum = false },
                    new { header = "Số ĐKCB", key = "registerNumber", width = 1.5, align = "left", sum = false },
                    new { header = "Nhan đề", key = "title", width = 4.0, align = "left", sum = false },
                    new { header = "Kết quả", key = "result", width = 1.2, align = "center", sum = false },
                    new { header = "Giá", key = "price", width = 1.2, align = "right", sum = true }
                },
                showTotals = true,
                closingLines = new[]
                {
                    "Bảng trên liệt kê các bản không khớp; các bản khớp không liệt kê chi tiết.",
                    "Biên bản được lập thành 02 bản, đơn vị giữ 01 bản, hội đồng kiểm kê giữ 01 bản."
                },
                signatures = new object[]
                {
                    new { role = "Thủ kho", note = "(Ký, ghi rõ họ tên)" },
                    new { role = "Thành viên hội đồng", note = "(Ký, ghi rõ họ tên)" },
                    new { role = "Chủ tịch hội đồng", note = "(Ký tên, đóng dấu)" }
                },
                fontSize = 9.5
            })));

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Đã nạp 6 mẫu biểu in mặc định của phân hệ bổ sung");
    }

    private FormTemplate FormTemplate(string code, string name, string formType, string layout) => new()
    {
        Code = code,
        Name = name,
        FormType = formType,
        PaperSize = "A4",
        IsLandscape = false,
        IsDefault = true,
        IsActive = true,
        Layout = layout,
        CreatedAt = _clock.Now
    };

    private static string Json(object value) => JsonSerializer.Serialize(value, JsonOptions);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
