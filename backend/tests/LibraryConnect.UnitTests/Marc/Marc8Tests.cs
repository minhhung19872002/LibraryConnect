using System.Text;
using FluentAssertions;
using LibraryConnect.Marc;

namespace LibraryConnect.UnitTests.Marc;

/// <summary>
/// Giải mã MARC-8 — bảng mã của biểu ghi lấy từ các máy chủ Z39.50 cũ như Thư viện Quốc hội Mỹ.
/// </summary>
public class Marc8Tests
{
    [Fact]
    public void Ascii_text_passes_through_unchanged()
    {
        Marc8Encoding.GetString("Database systems"u8).Should().Be("Database systems");
    }

    [Fact]
    public void A_combining_mark_before_a_letter_becomes_a_composed_vietnamese_letter()
    {
        // MARC-8 writes the diacritic first: 0xE2 (acute) then 'e' means "é". Unicode writes the
        // letter first, so the decoder has to reorder them.
        var bytes = new byte[] { 0xE2, (byte)'e' };

        Marc8Encoding.GetString(bytes).Should().Be("é");
    }

    [Fact]
    public void Vietnamese_letters_with_two_marks_are_decoded_and_composed()
    {
        // "ế" is an e carrying a circumflex and an acute. Both marks precede the letter, in the
        // same order Unicode expects after it: circumflex (0xE3) first, then acute (0xE2).
        var bytes = new byte[] { 0xE3, 0xE2, (byte)'e' };

        var text = Marc8Encoding.GetString(bytes);

        text.Normalize(NormalizationForm.FormC).Should().Be("ế");
        text.Length.Should().Be(1, "kết quả đã được chuẩn hóa NFC thành một ký tự");
    }

    [Fact]
    public void The_vietnamese_specific_letters_are_single_codes_in_ansel()
    {
        // Ơ, Ư and Đ have their own positions in the extended Latin set.
        var bytes = new byte[] { 0xAC, 0xAD, 0xA3, 0xBC, 0xBD, 0xB3 };

        Marc8Encoding.GetString(bytes).Should().Be("ƠƯĐơưđ");
    }

    [Fact]
    public void A_word_combining_both_features_decodes_correctly()
    {
        // "Trừng": T, r, then the grave mark, then ư from the extended Latin set, then n, g.
        var bytes = new byte[] { (byte)'T', (byte)'r', 0xE1, 0xBD, (byte)'n', (byte)'g' };

        Marc8Encoding.GetString(bytes).Should().Be("Trừng");
    }

    [Fact]
    public void A_record_that_claims_marc8_in_the_leader_is_decoded_as_marc8()
    {
        // A two-character placeholder occupies exactly the two bytes that MARC-8 "é" needs
        // (acute mark, then the letter), so the substitution leaves every offset intact.
        var record = new MarcRecord();
        record.SetControlField("001", "LOC00001");
        record.AddField("245", '1', '0').AddSubfield('a', "XY");

        var bytes = Iso2709Writer.Write(record);
        var index = IndexOf(bytes, "XY"u8.ToArray());
        bytes[index] = 0xE2;
        bytes[index + 1] = (byte)'e';
        bytes[9] = (byte)' ';

        Iso2709Reader.Read(bytes).GetSubfield("245", 'a').Should().Be("é");
    }

    [Fact]
    public void Utf8_data_is_detected_even_when_the_leader_says_marc8()
    {
        // Exports that write UTF-8 but forget to set Leader/09 are common. Trusting the leader
        // blindly would turn "Giáo trình" into rubbish.
        var record = new MarcRecord();
        record.SetControlField("001", "BAD00001");
        record.AddField("245", '1', '0').AddSubfield('a', "Giáo trình");

        var bytes = Iso2709Writer.Write(record);
        bytes[9] = (byte)' ';

        Iso2709Reader.Read(bytes).GetSubfield("245", 'a').Should().Be("Giáo trình");
    }

    [Fact]
    public void An_east_asian_marc8_record_is_refused_with_advice_instead_of_being_mangled()
    {
        // ESC $ 1 designates the Chinese/Japanese/Korean set, which this decoder does not handle.
        var bytes = new byte[] { 0x1B, (byte)'$', (byte)'1', 0x21, 0x21 };

        var act = () => Marc8Encoding.GetString(bytes);

        act.Should().Throw<MarcException>().WithMessage("*UTF-8*");
    }

    [Fact]
    public void Designating_the_extended_latin_set_is_accepted_and_skipped()
    {
        // ESC ( ! E switches G0 to Extended Latin; the sequence itself is not content.
        var bytes = new byte[] { 0x1B, (byte)'(', (byte)'!', (byte)'E', 0xAC, 0xAD };

        Marc8Encoding.GetString(bytes).Should().Be("ƠƯ");
    }

    private static int IndexOf(byte[] haystack, byte[] needle)
    {
        for (var index = 0; index <= haystack.Length - needle.Length; index++)
        {
            var match = true;

            for (var offset = 0; offset < needle.Length && match; offset++)
            {
                match = haystack[index + offset] == needle[offset];
            }

            if (match)
            {
                return index;
            }
        }

        return -1;
    }
}
