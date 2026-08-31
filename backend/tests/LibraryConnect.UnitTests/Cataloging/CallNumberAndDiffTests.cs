using FluentAssertions;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Marc;

namespace LibraryConnect.UnitTests.Cataloging;

/// <summary>
/// Sinh ký hiệu xếp giá theo quy tắc cấu hình được (III.2).
/// </summary>
public class CallNumberBuilderTests
{
    private static readonly CallNumberBuilder.Context Book =
        new("005.74", "Nguyễn Văn Ánh", "Giáo trình cơ sở dữ liệu", 2023, 2);

    [Fact]
    public void The_default_rule_produces_the_form_vietnamese_libraries_use()
    {
        CallNumberBuilder.Build(CallNumberBuilder.DefaultPattern, Book).Should().Be("005.74 NGU");
    }

    [Fact]
    public void An_empty_rule_falls_back_to_the_default()
    {
        CallNumberBuilder.Build(null, Book).Should().Be("005.74 NGU");
        CallNumberBuilder.Build("   ", Book).Should().Be("005.74 NGU");
    }

    [Fact]
    public void Every_placeholder_resolves()
    {
        CallNumberBuilder.Build("{DDC}|{DDC:3}|{AUTHOR:2}|{TITLE:4}|{YEAR}|{COPY}", Book)
            .Should().Be("005.74|005|NG|GIAO|2023|2");
    }

    [Fact]
    public void The_shorthand_form_means_the_same_as_the_explicit_one()
    {
        // Rules written by hand use {AUTHOR3}; both spellings have to work or a library's existing
        // rule silently produces a shelf mark with a piece missing.
        CallNumberBuilder.Build("{DDC}/{AUTHOR3}-{COPY}", Book).Should().Be("005.74/NGU-2");
    }

    [Fact]
    public void Diacritics_are_stripped_so_shelf_order_matches_the_labels()
    {
        CallNumberBuilder.Build("{AUTHOR:3}", Book with { AuthorName = "Đặng Thị Ửng" }).Should().Be("DAN");
    }

    [Fact]
    public void A_library_that_files_under_the_given_name_gets_the_other_convention()
    {
        CallNumberBuilder.Build("{DDC} {AUTHOR_LAST:3}", Book).Should().Be("005.74 ANH");
    }

    [Fact]
    public void A_record_without_an_author_does_not_leave_a_gap_in_the_shelf_mark()
    {
        CallNumberBuilder.Build("{DDC} {AUTHOR:3}", Book with { AuthorName = null }).Should().Be("005.74");
    }

    [Fact]
    public void A_record_with_nothing_to_build_from_produces_an_empty_shelf_mark()
    {
        CallNumberBuilder.Build("{DDC} {AUTHOR:3}", new CallNumberBuilder.Context(null, null, null, null, 1))
            .Should().BeEmpty();
    }

    [Fact]
    public void An_unfinished_placeholder_is_shown_rather_than_swallowed()
    {
        // A typo in the parameter should be visible to whoever typed it.
        CallNumberBuilder.Build("{DDC} {AUTHOR", Book).Should().Be("005.74 {AUTHOR");
    }

    [Fact]
    public void An_unknown_placeholder_resolves_to_nothing()
    {
        CallNumberBuilder.Build("{DDC} {KHONGCO}", Book).Should().Be("005.74");
    }
}

/// <summary>
/// So sánh hai phiên bản biểu ghi (II.3).
/// </summary>
public class MarcDiffTests
{
    private static MarcRecord Record()
    {
        var record = new MarcRecord();
        record.SetControlField("001", "LC00000001");
        record.SetControlField("008", "240115s2023    vm a     b    000 0 vie d");
        record.AddField("245", '1', '0').AddSubfield('a', "Giáo trình cơ sở dữ liệu");
        record.AddField("650", ' ', '4').AddSubfield('a', "Cơ sở dữ liệu");

        return record;
    }

    [Fact]
    public void An_unchanged_record_reports_no_changes()
    {
        MarcDiff.CompareChangesOnly(Record(), Record()).Should().BeEmpty();
    }

    [Fact]
    public void A_changed_subfield_is_reported_with_both_versions_of_the_line()
    {
        var after = Record();
        after.GetField("245")!.Subfields[0].Value = "Giáo trình cơ sở dữ liệu nâng cao";

        var change = MarcDiff.CompareChangesOnly(Record(), after).Single();

        change.Kind.Should().Be(MarcDiffKind.Changed);
        change.Tag.Should().Be("245");
        change.Before.Should().Contain("Giáo trình cơ sở dữ liệu");
        change.After.Should().Contain("nâng cao");
    }

    [Fact]
    public void An_added_field_is_reported_as_added()
    {
        var after = Record();
        after.AddField("250").AddSubfield('a', "Tái bản lần thứ ba");

        var change = MarcDiff.CompareChangesOnly(Record(), after).Single();

        change.Kind.Should().Be(MarcDiffKind.Added);
        change.Tag.Should().Be("250");
        change.Before.Should().BeNull();
    }

    [Fact]
    public void A_removed_field_is_reported_as_removed()
    {
        var after = Record();
        after.DataFields.RemoveAll(field => field.Tag == "650");

        var change = MarcDiff.CompareChangesOnly(Record(), after).Single();

        change.Kind.Should().Be(MarcDiffKind.Removed);
        change.Tag.Should().Be("650");
        change.After.Should().BeNull();
    }

    [Fact]
    public void Adding_one_repeated_field_shows_as_one_change_not_three()
    {
        // Matching repeated fields by position is what keeps a single new subject heading from
        // reading as a rewrite of every subject on the record.
        var before = Record();
        before.AddField("650", ' ', '4').AddSubfield('a', "Tin học");

        var after = before.Clone();
        after.AddField("650", ' ', '4').AddSubfield('a', "Lập trình");

        var changes = MarcDiff.CompareChangesOnly(before, after);

        changes.Should().HaveCount(1);
        changes[0].Kind.Should().Be(MarcDiffKind.Added);
        changes[0].After.Should().Contain("Lập trình");
    }

    [Fact]
    public void A_changed_leader_is_reported()
    {
        var after = Record();
        after.Leader.RecordStatus = 'c';

        MarcDiff.CompareChangesOnly(Record(), after).Should()
            .ContainSingle(line => line.Tag == "LDR" && line.Kind == MarcDiffKind.Changed);
    }

    [Fact]
    public void A_changed_control_field_is_reported()
    {
        var after = Record();
        after.SetControlField("008", "250115s2024    vm a     b    000 0 vie d");

        MarcDiff.CompareChangesOnly(Record(), after).Should()
            .ContainSingle(line => line.Tag == "008" && line.Kind == MarcDiffKind.Changed);
    }

    [Fact]
    public void A_blank_indicator_is_printed_as_a_hash_so_it_is_visible_in_the_comparison()
    {
        var after = Record();
        after.GetField("650")!.Subfields[0].Value = "Tin học";

        MarcDiff.CompareChangesOnly(Record(), after).Single().Before.Should().StartWith("#4 ");
    }
}
