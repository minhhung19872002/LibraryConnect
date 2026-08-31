using LibraryConnect.Application.Features.Readers;

namespace LibraryConnect.UnitTests.Readers;

/// <summary>
/// Kiểm tra ảnh chân dung và bố cục mẫu thẻ (VI.1, VI.2).
///
/// Hai chỗ này hỏng theo hai kiểu khác nhau: nhận nhầm tệp không phải ảnh là một lỗ hổng bảo mật,
/// còn đặt nội dung tràn khổ thẻ là cả hộp phôi thẻ nhựa in hỏng.
/// </summary>
public class ReaderCardRulesTests
{
    private static byte[] Png() =>
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01 };

    private static byte[] Jpeg() =>
        new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 };

    [Fact]
    public void Recognises_a_real_png_and_a_real_jpeg()
    {
        ReaderPhotos.DetectImageType(Png()).Should().Be("image/png");
        ReaderPhotos.DetectImageType(Jpeg()).Should().Be("image/jpeg");
    }

    [Fact]
    public void Rejects_a_file_that_is_only_named_like_an_image()
    {
        // Đây chính là cách vượt kiểm tra bằng phần mở rộng: đổi tên tệp thực thi thành .jpg.
        var executable = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03 };

        ReaderPhotos.DetectImageType(executable).Should().BeNull();
        ReaderPhotos.DetectImageType(Array.Empty<byte>()).Should().BeNull();
        ReaderPhotos.DetectImageType(new byte[] { 0xFF }).Should().BeNull();
    }

    [Fact]
    public void Names_the_stored_object_after_the_reader_and_the_real_image_type()
    {
        var readerId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        ReaderPhotos.ObjectName(readerId, "image/png").Should().EndWith(".png");
        ReaderPhotos.ObjectName(readerId, "image/jpeg").Should().EndWith(".jpg");

        // Tên đối tượng không được lấy theo tên tệp người dùng gửi lên.
        ReaderPhotos.ObjectName(readerId, "image/png").Should().Contain(readerId.ToString("N"));
    }

    private static SaveReaderCardTemplateCommand Template(CardFaceLayoutDto front) => new()
    {
        Code = "THE-TEST",
        Name = "Thẻ thử",
        WidthMm = 85.6,
        HeightMm = 54,
        Front = front
    };

    [Fact]
    public void Accepts_a_layout_that_fits_the_card()
    {
        var command = Template(new CardFaceLayoutDto
        {
            Boxes = { new CardBoxDto { X = 4, Y = 4, Width = 50, Height = 6, Source = ReaderCardFields.FullName } },
            Images = { new CardImageDto { X = 60, Y = 10, Width = 22, Height = 28 } },
            Barcode = new CardBarcodeDto { X = 4, Y = 42, Width = 40, Height = 8 }
        });

        new SaveReaderCardTemplateCommandValidator().Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_a_text_box_that_runs_off_the_edge_of_the_card()
    {
        var command = Template(new CardFaceLayoutDto
        {
            Boxes = { new CardBoxDto { X = 70, Y = 4, Width = 30, Height = 6, Source = ReaderCardFields.FullName } }
        });

        var result = new SaveReaderCardTemplateCommandValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("ngoài khổ thẻ"));
    }

    [Fact]
    public void Rejects_a_photo_slot_taller_than_the_card()
    {
        var command = Template(new CardFaceLayoutDto
        {
            Boxes = { new CardBoxDto { X = 4, Y = 4, Width = 40, Height = 6, Source = ReaderCardFields.FullName } },
            Images = { new CardImageDto { X = 60, Y = 30, Width = 22, Height = 28 } }
        });

        new SaveReaderCardTemplateCommandValidator().Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_an_empty_front_face()
    {
        var result = new SaveReaderCardTemplateCommandValidator().Validate(Template(new CardFaceLayoutDto()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("chưa có nội dung"));
    }

    [Fact]
    public void Every_card_field_offered_by_the_designer_has_a_vietnamese_label()
    {
        ReaderCardFields.Labels.Should().ContainKey(ReaderCardFields.CardNumber);

        foreach (var label in ReaderCardFields.Labels.Values)
        {
            label.Should().NotBeNullOrWhiteSpace();
        }
    }
}
