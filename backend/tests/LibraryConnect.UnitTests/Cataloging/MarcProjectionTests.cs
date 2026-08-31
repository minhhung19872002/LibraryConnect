using FluentAssertions;
using LibraryConnect.Application.Features.Cataloging;
using LibraryConnect.Marc;

namespace LibraryConnect.UnitTests.Cataloging;

/// <summary>
/// Rút dữ liệu tra cứu từ biểu ghi MARC.
///
/// The flat columns drive every list screen, every sort and every OPAC facet, so what lands in them
/// decides whether a librarian can find a record at all. These tests pin the extraction against
/// records shaped the way Vietnamese cataloguing actually writes them, including the ISBD
/// punctuation that has to stay in the record but not in the column.
/// </summary>
public class MarcProjectionTests
{
    private static MarcRecord Book()
    {
        var record = new MarcRecord();
        record.SetControlField("001", "LC00000001");
        record.SetControlField("008", "240115s2023    vm a     b    000 0 vie d");

        record.AddField("020").AddSubfield('a', "978-604-01-2345-6").AddSubfield('c', "185.000 đ");
        record.AddField("041", '0').AddSubfield('a', "vie");
        record.AddField("044").AddSubfield('a', "vm");
        record.AddField("082", '0', '4').AddSubfield('a', "005.74").AddSubfield('2', "23");
        record.AddField("100", '1')
            .AddSubfield('a', "Nguyễn Văn Ánh")
            .AddSubfield('d', "1975-")
            .AddSubfield('e', "chủ biên");
        record.AddField("245", '1', '0')
            .AddSubfield('a', "Giáo trình cơ sở dữ liệu :")
            .AddSubfield('b', "dùng cho sinh viên ngành Công nghệ thông tin /")
            .AddSubfield('c', "Nguyễn Văn Ánh (chủ biên), Trần Thị Bưởi");
        record.AddField("250").AddSubfield('a', "Tái bản lần thứ ba, có sửa chữa");
        record.AddField("260")
            .AddSubfield('a', "Hà Nội :")
            .AddSubfield('b', "Nhà xuất bản Đại học Quốc gia Hà Nội,")
            .AddSubfield('c', "2023");
        record.AddField("300")
            .AddSubfield('a', "356 tr. :")
            .AddSubfield('b', "minh họa, bảng ;")
            .AddSubfield('c', "24 cm");
        record.AddField("490", '0').AddSubfield('a', "Tủ sách Công nghệ thông tin").AddSubfield('v', "tập 5");
        record.AddField("520").AddSubfield('a', "Trình bày mô hình quan hệ và ngôn ngữ SQL.");
        record.AddField("650", ' ', '4').AddSubfield('a', "Cơ sở dữ liệu").AddSubfield('x', "Giáo trình");
        record.AddField("653").AddSubfield('a', "SQL").AddSubfield('a', "Mô hình quan hệ");
        record.AddField("700", '1').AddSubfield('a', "Trần Thị Bưởi").AddSubfield('e', "biên soạn");

        return record;
    }

    [Fact]
    public void Isbd_punctuation_stays_in_the_record_but_not_in_the_columns()
    {
        var projection = MarcProjection.Project(Book());

        projection.Title.Should().Be("Giáo trình cơ sở dữ liệu");
        projection.Subtitle.Should().Be("dùng cho sinh viên ngành Công nghệ thông tin");
        projection.PublishPlace.Should().Be("Hà Nội");
        projection.PublisherName.Should().Be("Nhà xuất bản Đại học Quốc gia Hà Nội");
        projection.Pages.Should().Be("356 tr.");
        projection.Dimensions.Should().Be("24 cm");
    }

    [Fact]
    public void A_title_in_several_parts_is_joined_for_display()
    {
        var record = new MarcRecord();
        record.AddField("245", '1', '0')
            .AddSubfield('a', "Tuyển tập văn học Việt Nam.")
            .AddSubfield('n', "Tập 2,")
            .AddSubfield('p', "Văn học hiện đại");

        MarcProjection.Project(record).Title.Should().Be("Tuyển tập văn học Việt Nam. Tập 2. Văn học hiện đại");
    }

    [Fact]
    public void Isbn_is_normalised_so_duplicate_checking_works_across_writing_styles()
    {
        // The same ISBN is written with and without hyphens depending on who catalogued it.
        MarcProjection.NormaliseStandardNumber("978-604-01-2345-6").Should().Be("9786040123456");
        MarcProjection.NormaliseStandardNumber("9786040123456").Should().Be("9786040123456");
        MarcProjection.NormaliseStandardNumber("978 604 01 2345 6 (bìa mềm)").Should().Be("9786040123456");
        MarcProjection.NormaliseStandardNumber("0-8044-2957-x").Should().Be("080442957X");
        MarcProjection.NormaliseStandardNumber("  ").Should().BeNull();
    }

    [Theory]
    [InlineData("2023", 2023)]
    [InlineData("c2023", 2023)]
    [InlineData("[2023]", 2023)]
    [InlineData("2023, tái bản 2024", 2023)]
    [InlineData("in lần thứ 2 năm 2019", 2019)]
    [InlineData("không rõ năm", null)]
    [InlineData("199", null)]
    public void The_year_is_read_out_of_free_text(string value, int? expected)
    {
        MarcProjection.ParseYear(value).Should().Be(expected);
    }

    [Fact]
    public void The_year_falls_back_to_the_fixed_field_when_the_publication_area_has_none()
    {
        var record = Book();
        record.GetField("260")!.Subfields.RemoveAll(subfield => subfield.Code == 'c');

        // 008 positions 07-10 carry the year in coded form.
        MarcProjection.Project(record).PublishYear.Should().Be(2023);
    }

    [Fact]
    public void The_language_falls_back_to_the_fixed_field()
    {
        var record = Book();
        record.DataFields.RemoveAll(field => field.Tag == "041");

        MarcProjection.Project(record).LanguageCode.Should().Be("vie");
    }

    [Fact]
    public void A_fixed_field_position_filled_with_bars_counts_as_not_coded()
    {
        var record = Book();
        record.DataFields.RemoveAll(field => field.Tag == "041");
        record.SetControlField("008", "240115s2023    vm a     b    000 0 ||| d");

        MarcProjection.Project(record).LanguageCode.Should().BeNull();
    }

    [Fact]
    public void Rda_publication_field_is_used_when_the_record_has_no_260()
    {
        var record = new MarcRecord();
        record.AddField("245", '1', '0').AddSubfield('a', "Nhan đề");
        record.AddField("264", ' ', '1')
            .AddSubfield('a', "Hà Nội :")
            .AddSubfield('b', "Nhà xuất bản Giáo dục,")
            .AddSubfield('c', "2024");

        var projection = MarcProjection.Project(record);

        projection.PublisherName.Should().Be("Nhà xuất bản Giáo dục");
        projection.PublishYear.Should().Be(2024);
    }

    [Fact]
    public void The_publication_field_wins_over_the_copyright_field_in_rda_records()
    {
        // 264 with second indicator 1 is publication; 4 is the copyright date, which is not what a
        // catalogue list should show as the year of publication.
        var record = new MarcRecord();
        record.AddField("245", '1', '0').AddSubfield('a', "Nhan đề");
        record.AddField("264", ' ', '4').AddSubfield('c', "©2020");
        record.AddField("264", ' ', '1').AddSubfield('b', "Nhà xuất bản Trẻ,").AddSubfield('c', "2024");

        var projection = MarcProjection.Project(record);

        projection.PublishYear.Should().Be(2024);
        projection.PublisherName.Should().Be("Nhà xuất bản Trẻ");
    }

    [Fact]
    public void Authors_carry_their_role_and_whether_they_are_the_main_entry()
    {
        var authors = MarcProjection.Project(Book()).Authors;

        authors.Should().HaveCount(2);
        authors[0].Should().BeEquivalentTo(
            new ProjectedAuthor("Nguyễn Văn Ánh", "chủ biên", true, false, "1975-"));
        authors[1].Name.Should().Be("Trần Thị Bưởi");
        authors[1].IsMain.Should().BeFalse();
    }

    [Fact]
    public void The_same_person_listed_twice_produces_one_author_and_keeps_the_main_entry()
    {
        var record = Book();
        record.AddField("700", '1').AddSubfield('a', "NGUYỄN VĂN ÁNH");

        var authors = MarcProjection.Project(record).Authors;

        authors.Should().HaveCount(2);
        authors.Single(author => author.Name.Contains("Ánh", StringComparison.OrdinalIgnoreCase))
            .IsMain.Should().BeTrue();
    }

    [Fact]
    public void A_corporate_author_is_marked_as_such()
    {
        var record = new MarcRecord();
        record.AddField("245", '1', '0').AddSubfield('a', "Báo cáo thường niên");
        record.AddField("110", '2').AddSubfield('a', "Trường Đại học Tài nguyên và Môi trường TP.HCM");

        var author = MarcProjection.Project(record).Authors.Single();

        author.IsCorporate.Should().BeTrue();
        author.IsMain.Should().BeTrue();
    }

    [Fact]
    public void Subject_headings_join_their_subdivisions_the_way_a_subject_card_reads()
    {
        MarcProjection.Project(Book()).Subjects.Should().Equal("Cơ sở dữ liệu -- Giáo trình");
    }

    [Fact]
    public void Every_free_keyword_of_a_repeated_subfield_is_kept()
    {
        MarcProjection.Project(Book()).Keywords.Should().Equal("SQL", "Mô hình quan hệ");
    }

    [Fact]
    public void Classification_numbers_carry_the_name_of_their_scheme()
    {
        var record = Book();
        record.AddField("084").AddSubfield('a', "32.973").AddSubfield('2', "BBK");

        var classifications = MarcProjection.Project(record).Classifications;

        classifications.Should().BeEquivalentTo(new[]
        {
            new ProjectedClassification("005.74", "DDC"),
            new ProjectedClassification("32.973", "BBK")
        });
    }

    [Fact]
    public void A_record_with_only_a_title_still_projects_without_throwing()
    {
        var record = new MarcRecord();
        record.AddField("245", '1', '0').AddSubfield('a', "Nhan đề duy nhất");

        var projection = MarcProjection.Project(record);

        projection.Title.Should().Be("Nhan đề duy nhất");
        projection.Authors.Should().BeEmpty();
        projection.PublishYear.Should().BeNull();
        projection.Isbn.Should().BeNull();
    }
}

/// <summary>
/// Trình bày biểu ghi theo ISBD — dạng cán bộ đọc để soát và cũng là nội dung in lên phích.
/// </summary>
public class IsbdFormatterTests
{
    private static MarcRecord Book()
    {
        var record = new MarcRecord();
        record.AddField("020").AddSubfield('a', "978-604-01-2345-6").AddSubfield('c', "185.000 đ");
        record.AddField("082", '0', '4').AddSubfield('a', "005.74");
        record.AddField("245", '1', '0')
            .AddSubfield('a', "Giáo trình cơ sở dữ liệu :")
            .AddSubfield('b', "dùng cho sinh viên /")
            .AddSubfield('c', "Nguyễn Văn Ánh");
        record.AddField("250").AddSubfield('a', "Tái bản lần thứ ba");
        record.AddField("260")
            .AddSubfield('a', "Hà Nội :")
            .AddSubfield('b', "Nhà xuất bản Đại học Quốc gia Hà Nội,")
            .AddSubfield('c', "2023");
        record.AddField("300").AddSubfield('a', "356 tr. :").AddSubfield('b', "minh họa ;").AddSubfield('c', "24 cm");
        record.AddField("490", '0').AddSubfield('a', "Tủ sách Công nghệ thông tin").AddSubfield('v', "tập 5");
        record.AddField("504").AddSubfield('a', "Thư mục: tr. 340-350");

        return record;
    }

    [Fact]
    public void Each_area_of_the_description_is_labelled_in_vietnamese()
    {
        var areas = IsbdFormatter.Describe(Book());

        areas.Should().Contain(area => area.Label == "Nhan đề và thông tin trách nhiệm");
        areas.Single(area => area.Label == "Nhan đề và thông tin trách nhiệm").Content
            .Should().Be("Giáo trình cơ sở dữ liệu : dùng cho sinh viên / Nguyễn Văn Ánh");
    }

    [Fact]
    public void The_publication_area_reads_the_way_a_catalogue_card_does()
    {
        IsbdFormatter.Describe(Book()).Single(area => area.Label == "Thông tin xuất bản").Content
            .Should().Be("Hà Nội : Nhà xuất bản Đại học Quốc gia Hà Nội, 2023");
    }

    [Fact]
    public void The_physical_area_keeps_the_conventional_marks()
    {
        IsbdFormatter.Describe(Book()).Single(area => area.Label == "Mô tả vật lý").Content
            .Should().Be("356 tr. : minh họa ; 24 cm");
    }

    [Fact]
    public void The_series_is_wrapped_in_brackets_with_its_volume()
    {
        IsbdFormatter.Describe(Book()).Single(area => area.Label == "Tùng thư").Content
            .Should().Be("(Tủ sách Công nghệ thông tin ; tập 5)");
    }

    [Fact]
    public void The_isbn_area_carries_the_price_when_the_record_has_one()
    {
        IsbdFormatter.Describe(Book()).Single(area => area.Label == "Chỉ số ISBN").Content
            .Should().Be("9786040123456 : 185.000 đ");
    }

    [Fact]
    public void An_area_with_nothing_in_it_is_left_out_entirely()
    {
        var record = new MarcRecord();
        record.AddField("245", '1', '0').AddSubfield('a', "Chỉ có nhan đề");

        var areas = IsbdFormatter.Describe(record);

        areas.Should().HaveCount(1);
        areas[0].Content.Should().Be("Chỉ có nhan đề");
    }

    [Fact]
    public void The_paragraph_form_joins_the_areas_the_way_a_card_prints_them()
    {
        IsbdFormatter.DescribeAsParagraph(Book())
            .Should().StartWith("Giáo trình cơ sở dữ liệu : dùng cho sinh viên / Nguyễn Văn Ánh. — Tái bản lần thứ ba. — Hà Nội :");
    }
}
