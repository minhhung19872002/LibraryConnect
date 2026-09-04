using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Entities.Cir;
using Microsoft.EntityFrameworkCore;

namespace LibraryConnect.Application.Features.Circulation;

/// <summary>
/// Chính sách áp dụng cho một lượt mượn cụ thể, sau khi đã chọn ra ô đúng của ma trận (VII.1).
/// </summary>
public class EffectivePolicy
{
    public Guid? PolicyId { get; set; }
    public string Name { get; set; } = "Chính sách mặc định";
    public int MaxItems { get; set; }
    public int LoanDays { get; set; }
    public int MaxRenewals { get; set; }
    public int RenewalDays { get; set; }
    public decimal FinePerDay { get; set; }
    public int GraceDays { get; set; }
    public int MaxHolds { get; set; }
    public int HoldExpireDays { get; set; }
    public bool AllowLoan { get; set; } = true;
    public bool AllowRenew { get; set; } = true;
    public bool AllowHold { get; set; } = true;
    public bool AllowTakeHome { get; set; } = true;
    public bool RequireRenewalApproval { get; set; }
}

/// <summary>
/// Chọn chính sách lưu thông cho một cặp bạn đọc × tài liệu × kho.
///
/// Ma trận cho phép để trống từng chiều với nghĩa "áp dụng cho mọi giá trị", nên nhiều ô có thể cùng
/// khớp. Khi đó ô có độ ưu tiên lớn hơn thắng; bằng nhau thì ô khai cụ thể hơn thắng — một chính sách
/// viết riêng cho "Sinh viên × Luận văn × Kho đóng" bao giờ cũng phải mạnh hơn chính sách chung.
/// </summary>
public interface ICirculationPolicyResolver
{
    Task<EffectivePolicy> ResolveAsync(
        Guid? readerTypeId, Guid? documentTypeId, Guid? warehouseId, CancellationToken ct = default);
}

public class CirculationPolicyResolver : ICirculationPolicyResolver
{
    public const string DefaultLoanDaysParameter = "CIRCULATION.DEFAULT_LOAN_DAYS";
    public const string DefaultMaxItemsParameter = "CIRCULATION.DEFAULT_MAX_ITEMS";
    public const string DefaultFinePerDayParameter = "CIRCULATION.FINE_PER_DAY";

    private readonly IApplicationDbContext _db;
    private readonly ISystemParameterService _parameters;

    public CirculationPolicyResolver(IApplicationDbContext db, ISystemParameterService parameters)
    {
        _db = db;
        _parameters = parameters;
    }

    public async Task<EffectivePolicy> ResolveAsync(
        Guid? readerTypeId, Guid? documentTypeId, Guid? warehouseId, CancellationToken ct = default)
    {
        var candidates = await _db.CirculationPolicies
            .AsNoTracking()
            .Where(policy => policy.IsActive
                             && (policy.ReaderTypeId == null || policy.ReaderTypeId == readerTypeId)
                             && (policy.DocumentTypeId == null || policy.DocumentTypeId == documentTypeId)
                             && (policy.WarehouseId == null || policy.WarehouseId == warehouseId))
            .ToListAsync(ct);

        var best = Pick(candidates);

        if (best is null && readerTypeId is not null)
        {
            // Không ô nào khớp thì tới lượt chính sách mặc định gắn trên loại bạn đọc (VI.3) — thư
            // viện chỉ cần khai một chính sách chung cho loại mới, không phải thêm ô cho từng kho.
            best = await _db.ReaderTypes
                .AsNoTracking()
                .Where(type => type.Id == readerTypeId && type.DefaultPolicyId != null)
                .Select(type => type.DefaultPolicy)
                .FirstOrDefaultAsync(ct);

            if (best is not null && !best.IsActive)
            {
                best = null;
            }
        }

        if (best is null)
        {
            // Chưa khai chính sách nào thì vẫn phải cho mượn được: lấy giá trị mặc định trong Tham số
            // hệ thống, để thư viện mới cài đặt dùng được ngay từ ngày đầu.
            return new EffectivePolicy
            {
                MaxItems = await _parameters.GetAsync(DefaultMaxItemsParameter, 5, ct),
                LoanDays = await _parameters.GetAsync(DefaultLoanDaysParameter, 14, ct),
                MaxRenewals = 2,
                RenewalDays = await _parameters.GetAsync(DefaultLoanDaysParameter, 14, ct),
                FinePerDay = await _parameters.GetAsync(DefaultFinePerDayParameter, 2000m, ct),
                GraceDays = 0,
                MaxHolds = 3,
                HoldExpireDays = 3
            };
        }

        return Map(best);
    }

    /// <summary>Ô thắng cuộc: ưu tiên cao hơn trước, rồi tới ô khai cụ thể hơn.</summary>
    public static CirculationPolicy? Pick(IReadOnlyList<CirculationPolicy> candidates) =>
        candidates
            .OrderByDescending(policy => policy.Priority)
            .ThenByDescending(Specificity)
            .ThenBy(policy => policy.Name, StringComparer.CurrentCulture)
            .FirstOrDefault();

    private static int Specificity(CirculationPolicy policy) =>
        (policy.ReaderTypeId is null ? 0 : 1)
        + (policy.DocumentTypeId is null ? 0 : 1)
        + (policy.WarehouseId is null ? 0 : 1);

    public static EffectivePolicy Map(CirculationPolicy policy) => new()
    {
        PolicyId = policy.Id,
        Name = policy.Name,
        MaxItems = policy.MaxItems,
        LoanDays = policy.LoanDays,
        MaxRenewals = policy.MaxRenewals,
        RenewalDays = policy.RenewalDays,
        FinePerDay = policy.FinePerDay,
        GraceDays = policy.GraceDays,
        MaxHolds = policy.MaxHolds,
        HoldExpireDays = policy.HoldExpireDays,
        AllowLoan = policy.AllowLoan,
        AllowRenew = policy.AllowRenew,
        AllowHold = policy.AllowHold,
        AllowTakeHome = policy.AllowTakeHome,
        RequireRenewalApproval = policy.RequireRenewalApproval
    };
}
