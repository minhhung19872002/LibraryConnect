using System.Text.Json;
using FluentAssertions;
using LibraryConnect.Marc;

namespace LibraryConnect.UnitTests.Marc;

/// <summary>
/// Dạng JSON là dạng lưu trong cột <c>marc_json</c>, nên hình dạng của nó là một cam kết với cơ sở
/// dữ liệu chứ không phải chi tiết nội bộ.
/// </summary>
public class MarcJsonTests
{
    [Fact]
    public void The_json_shape_matches_the_one_the_database_stores()
    {
        var record = new MarcRecord();
        record.SetControlField("001", "VNU001");
        record.AddField("245", '1', '0').AddSubfield('a', "Giáo trình cơ sở dữ liệu");

        using var document = JsonDocument.Parse(MarcJson.Serialize(record));
        var root = document.RootElement;

        root.GetProperty("leader").GetString().Should().HaveLength(24);
        root.GetProperty("controlFields")[0].GetProperty("tag").GetString().Should().Be("001");
        root.GetProperty("controlFields")[0].GetProperty("value").GetString().Should().Be("VNU001");

        var field = root.GetProperty("dataFields")[0];
        field.GetProperty("tag").GetString().Should().Be("245");
        field.GetProperty("ind1").GetString().Should().Be("1");
        field.GetProperty("ind2").GetString().Should().Be("0");
        field.GetProperty("subfields")[0].GetProperty("code").GetString().Should().Be("a");
        field.GetProperty("subfields")[0].GetProperty("value").GetString().Should().Be("Giáo trình cơ sở dữ liệu");
    }

    [Fact]
    public void Vietnamese_text_is_stored_readable_rather_than_escaped()
    {
        var record = new MarcRecord();
        record.AddField("245", '1', '0').AddSubfield('a', "Lịch sử Việt Nam");

        // Escaped text would make the stored jsonb impossible to read or search by eye in psql.
        MarcJson.Serialize(record).Should().Contain("Lịch sử Việt Nam");
    }

    [Fact]
    public void Round_trip_through_json_preserves_the_record()
    {
        var record = new MarcRecord();
        record.Leader.RecordType = 'a';
        record.SetControlField("008", "240115s2023    vm a     b    000 0 vie d");
        record.AddField("100", '1').AddSubfield('a', "Nguyễn Văn Ánh").AddSubfield('e', "chủ biên");
        record.AddField("650", ' ', '4').AddSubfield('a', "Cơ sở dữ liệu");

        var parsed = MarcJson.Deserialize(MarcJson.Serialize(record));

        parsed.Leader.ToString().Should().Be(record.Leader.ToString());
        parsed.ControlFields.Should().BeEquivalentTo(record.ControlFields, options => options.WithStrictOrdering());
        parsed.DataFields.Should().BeEquivalentTo(record.DataFields, options => options.WithStrictOrdering());
    }

    [Fact]
    public void A_blank_indicator_survives_the_round_trip()
    {
        var record = new MarcRecord();
        record.AddField("500").AddSubfield('a', "Phụ chú");

        var parsed = MarcJson.Deserialize(MarcJson.Serialize(record));

        parsed.GetField("500")!.Indicator1.Should().Be(' ');
    }

    [Fact]
    public void Invalid_json_reports_a_readable_error()
    {
        var act = () => MarcJson.Deserialize("{ khong phai json }");

        act.Should().Throw<MarcException>().WithMessage("*JSON*");
    }
}

/// <summary>
/// Kiểm tra biểu ghi: cái gì là lỗi chặn lưu, cái gì chỉ là cảnh báo.
/// </summary>
public class MarcValidatorTests
{
    private static MarcValidator Validator() => new(new[]
    {
        new MarcFieldRule { Tag = "001", Name = "Số kiểm soát biểu ghi", IsControl = true, IsRequired = true },
        new MarcFieldRule { Tag = "008", Name = "Yếu tố dữ liệu có độ dài cố định", IsControl = true, IsRequired = true },
        new MarcFieldRule
        {
            Tag = "082",
            Name = "Chỉ số phân loại DDC",
            IsRepeatable = true,
            IsRecommended = true,
            Subfields = new[] { new MarcSubfieldRule { Code = 'a', Name = "Chỉ số phân loại", IsRequired = true } }
        },
        new MarcFieldRule
        {
            Tag = "245",
            Name = "Nhan đề và thông tin trách nhiệm",
            IsRequired = true,
            Indicators = new[]
            {
                new MarcIndicatorRule
                {
                    Position = 1,
                    Name = "Tạo tiêu đề bổ sung",
                    Values = new[] { new MarcIndicatorValue("0", "Không tạo"), new MarcIndicatorValue("1", "Có tạo") }
                }
            },
            Subfields = new[]
            {
                new MarcSubfieldRule { Code = 'a', Name = "Nhan đề chính", IsRequired = true },
                new MarcSubfieldRule { Code = 'b', Name = "Phần còn lại của nhan đề" },
                new MarcSubfieldRule { Code = 'c', Name = "Thông tin trách nhiệm" }
            }
        },
        new MarcFieldRule
        {
            Tag = "650",
            Name = "Đề mục chủ đề",
            IsRepeatable = true,
            Subfields = new[]
            {
                new MarcSubfieldRule { Code = 'a', Name = "Đề mục chủ đề", IsRequired = true },
                new MarcSubfieldRule { Code = 'x', Name = "Phụ đề chung", IsRepeatable = true }
            }
        }
    });

    private static MarcRecord ValidRecord()
    {
        var record = new MarcRecord();
        record.Leader.RecordStatus = 'n';
        record.Leader.RecordType = 'a';
        record.Leader.BibliographicLevel = 'm';
        record.SetControlField("001", "VNU001");
        record.SetControlField("008", "240115s2023    vm a     b    000 0 vie d");
        record.AddField("245", '1', '0').AddSubfield('a', "Giáo trình cơ sở dữ liệu");
        record.AddField("082", '0', '4').AddSubfield('a', "005.74");

        return record;
    }

    [Fact]
    public void A_complete_record_produces_no_errors()
    {
        var issues = Validator().Validate(ValidRecord());

        issues.Should().NotContain(issue => issue.Severity == MarcIssueSeverity.Error);
        MarcValidator.IsValid(issues).Should().BeTrue();
    }

    [Fact]
    public void A_record_without_a_title_cannot_be_saved()
    {
        var record = ValidRecord();
        record.DataFields.RemoveAll(field => field.Tag == "245");

        var issues = Validator().Validate(record);

        issues.Should().Contain(issue =>
            issue.Severity == MarcIssueSeverity.Error && issue.Tag == "245");
        MarcValidator.IsValid(issues).Should().BeFalse();
    }

    [Fact]
    public void A_record_without_a_classification_number_is_only_warned_about()
    {
        var record = ValidRecord();
        record.DataFields.RemoveAll(field => field.Tag == "082");

        var issues = Validator().Validate(record);

        issues.Should().Contain(issue =>
            issue.Severity == MarcIssueSeverity.Warning && issue.Tag == "082");
        MarcValidator.IsValid(issues).Should().BeTrue("thiếu chỉ số phân loại thì vẫn lưu được biểu ghi");
    }

    [Fact]
    public void A_missing_required_subfield_is_an_error_naming_the_subfield()
    {
        var record = ValidRecord();
        record.GetField("245")!.Subfields.Clear();
        record.GetField("245")!.AddSubfield('c', "Nguyễn Văn Ánh");

        var issues = Validator().Validate(record);

        issues.Should().Contain(issue =>
            issue.Severity == MarcIssueSeverity.Error &&
            issue.Tag == "245" &&
            issue.SubfieldCode == 'a');
    }

    [Fact]
    public void Repeating_a_field_that_cannot_repeat_is_an_error()
    {
        var record = ValidRecord();
        record.AddField("245", '1', '0').AddSubfield('a', "Nhan đề thứ hai");

        var issues = Validator().Validate(record);

        issues.Should().Contain(issue =>
            issue.Severity == MarcIssueSeverity.Error && issue.Message.Contains("không được lặp lại"));
    }

    [Fact]
    public void Repeating_a_field_that_may_repeat_is_accepted()
    {
        var record = ValidRecord();
        record.AddField("650", ' ', '4').AddSubfield('a', "Cơ sở dữ liệu");
        record.AddField("650", ' ', '4').AddSubfield('a', "Tin học");

        MarcValidator.IsValid(Validator().Validate(record)).Should().BeTrue();
    }

    [Fact]
    public void Repeating_a_subfield_that_cannot_repeat_is_an_error()
    {
        var record = ValidRecord();
        record.GetField("245")!.AddSubfield('a', "Nhan đề thứ hai");

        var issues = Validator().Validate(record);

        issues.Should().Contain(issue =>
            issue.Severity == MarcIssueSeverity.Error && issue.SubfieldCode == 'a' && issue.Tag == "245");
    }

    [Fact]
    public void An_indicator_outside_the_defined_values_is_a_warning_that_lists_them()
    {
        var record = ValidRecord();
        record.GetField("245")!.Indicator1 = '9';

        var issues = Validator().Validate(record);

        issues.Should().Contain(issue =>
            issue.Severity == MarcIssueSeverity.Warning &&
            issue.Tag == "245" &&
            issue.Message.Contains("Không tạo"));
    }

    [Fact]
    public void An_unknown_field_is_a_warning_that_points_at_the_definition_screen()
    {
        var record = ValidRecord();
        record.AddField("999").AddSubfield('a', "Trường riêng");

        var issues = Validator().Validate(record);

        issues.Should().Contain(issue =>
            issue.Severity == MarcIssueSeverity.Warning &&
            issue.Tag == "999" &&
            issue.Message.Contains("Định nghĩa trường MARC"));
    }

    [Fact]
    public void An_008_that_is_not_forty_characters_is_a_warning()
    {
        var record = ValidRecord();
        record.SetControlField("008", "240115s2023");

        var issues = Validator().Validate(record);

        issues.Should().Contain(issue => issue.Tag == "008" && issue.Message.Contains("40 ký tự"));
    }

    [Fact]
    public void A_field_too_long_for_the_exchange_format_is_caught_while_cataloguing()
    {
        // The cataloguer finds out now, not when the export to a partner library fails.
        var record = ValidRecord();
        record.AddField("650", ' ', '4').AddSubfield('a', new string('ế', 3_400));

        var issues = Validator().Validate(record);

        issues.Should().Contain(issue =>
            issue.Severity == MarcIssueSeverity.Error &&
            issue.Tag == "650" &&
            issue.Message.Contains("byte"));
    }

    [Fact]
    public void An_empty_subfield_value_is_a_warning()
    {
        var record = ValidRecord();
        record.GetField("245")!.AddSubfield('b', "   ");

        var issues = Validator().Validate(record);

        issues.Should().Contain(issue =>
            issue.Severity == MarcIssueSeverity.Warning && issue.SubfieldCode == 'b');
    }
}
