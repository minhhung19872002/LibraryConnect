using LibraryConnect.Application.Common.Text;
using LibraryConnect.Application.Features.Circulation;
using LibraryConnect.Domain.Entities.Cir;

namespace LibraryConnect.UnitTests.Circulation;

/// <summary>Chọn ô chính sách trong ma trận lưu thông (VII.1).</summary>
public class PolicyResolverTests
{
    private static CirculationPolicy Policy(
        string name, Guid? readerType = null, Guid? documentType = null, Guid? warehouse = null,
        int priority = 100) => new()
    {
        Name = name,
        ReaderTypeId = readerType,
        DocumentTypeId = documentType,
        WarehouseId = warehouse,
        Priority = priority,
        IsActive = true
    };

    [Fact]
    public void The_higher_priority_cell_wins()
    {
        var chosen = CirculationPolicyResolver.Pick(new[]
        {
            Policy("Chung", priority: 100),
            Policy("Đợt cao điểm", priority: 500)
        });

        chosen!.Name.Should().Be("Đợt cao điểm");
    }

    [Fact]
    public void At_equal_priority_the_more_specific_cell_wins()
    {
        var readerType = Guid.NewGuid();
        var documentType = Guid.NewGuid();

        var chosen = CirculationPolicyResolver.Pick(new[]
        {
            Policy("Áp dụng cho mọi bạn đọc"),
            Policy("Sinh viên", readerType),
            Policy("Sinh viên × Luận văn", readerType, documentType)
        });

        // Chính sách viết riêng cho một cặp cụ thể phải mạnh hơn chính sách chung, nếu không thì
        // khai riêng chẳng có tác dụng gì.
        chosen!.Name.Should().Be("Sinh viên × Luận văn");
    }

    [Fact]
    public void No_candidate_means_no_policy()
    {
        CirculationPolicyResolver.Pick(Array.Empty<CirculationPolicy>()).Should().BeNull();
    }

    [Fact]
    public void Mapping_a_policy_keeps_every_limit_the_desk_needs()
    {
        var policy = Policy("Sinh viên");
        policy.MaxItems = 3;
        policy.LoanDays = 14;
        policy.MaxRenewals = 2;
        policy.RenewalDays = 7;
        policy.FinePerDay = 2000;
        policy.GraceDays = 1;
        policy.AllowTakeHome = false;

        var mapped = CirculationPolicyResolver.Map(policy);

        mapped.PolicyId.Should().Be(policy.Id);
        mapped.MaxItems.Should().Be(3);
        mapped.LoanDays.Should().Be(14);
        mapped.MaxRenewals.Should().Be(2);
        mapped.RenewalDays.Should().Be(7);
        mapped.FinePerDay.Should().Be(2000);
        mapped.GraceDays.Should().Be(1);
        mapped.AllowTakeHome.Should().BeFalse();
    }
}

/// <summary>
/// Đọc số tiền thành chữ cho biên lai (VII.4).
///
/// Biên lai thu tiền ở Việt Nam bắt buộc có dòng "bằng chữ", nên sai ở đây là sai chứng từ.
/// </summary>
public class VietnameseMoneyTests
{
    [Theory]
    [InlineData(0, "Không đồng")]
    [InlineData(1, "Một đồng")]
    [InlineData(5, "Năm đồng")]
    [InlineData(10, "Mười đồng")]
    [InlineData(15, "Mười lăm đồng")]
    [InlineData(21, "Hai mươi mốt đồng")]
    [InlineData(24, "Hai mươi tư đồng")]
    [InlineData(25, "Hai mươi lăm đồng")]
    [InlineData(100, "Một trăm đồng")]
    [InlineData(105, "Một trăm lẻ năm đồng")]
    [InlineData(1000, "Một nghìn đồng")]
    [InlineData(1005, "Một nghìn không trăm lẻ năm đồng")]
    [InlineData(10000, "Mười nghìn đồng")]
    [InlineData(50000, "Năm mươi nghìn đồng")]
    [InlineData(123456, "Một trăm hai mươi ba nghìn bốn trăm năm mươi sáu đồng")]
    [InlineData(2000000, "Hai triệu đồng")]
    public void Reads_amounts_the_way_a_receipt_is_written(decimal amount, string expected)
    {
        VietnameseMoney.InWords(amount).Should().Be(expected);
    }

    [Fact]
    public void Rounds_to_whole_dong_because_receipts_have_no_smaller_unit()
    {
        VietnameseMoney.InWords(1500.6m).Should().Be("Một nghìn năm trăm lẻ một đồng");
    }

    [Fact]
    public void Falls_back_to_digits_rather_than_reading_an_absurd_number_wrong()
    {
        var text = VietnameseMoney.InWords(9_999_999_999_999m);

        text.Should().Contain("9.999.999.999.999");
    }
}
