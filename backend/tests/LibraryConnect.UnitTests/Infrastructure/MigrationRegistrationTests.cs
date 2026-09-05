using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using LibraryConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LibraryConnect.UnitTests.Infrastructure;

/// <summary>
/// K19 (06/09/2026): hai migration viết tay được commit, dựng ảnh, triển khai — và **không chạy**.
/// Chúng thiếu thuộc tính <c>[Migration("…")]</c> nên EF Core không coi đó là migration; máy chủ khởi
/// động, ghi "Cơ sở dữ liệu đã ở phiên bản mới nhất", và dữ liệu sai vẫn nguyên. Không có tiếng động
/// nào báo hỏng.
///
/// <para>Phép thử này quét chính thư mục migration: mỗi tệp <c>&lt;id&gt;_&lt;Tên&gt;.cs</c> phải có một lớp
/// migration mang đúng mã ấy, và mọi migration trong assembly phải có mặt ở đúng một tệp. Sai một
/// trong hai chiều là bản sửa dữ liệu không bao giờ chạy.</para>
/// </summary>
public class MigrationRegistrationTests
{
    private static readonly Regex TenTep = new(@"^(?<id>\d{14})_(?<ten>[A-Za-z0-9]+)\.cs$", RegexOptions.Compiled);

    [Fact]
    public void Moi_tep_migration_deu_khai_dung_ma_cua_no()
    {
        var thuMuc = ThuMucMigrations();

        var trongAssembly = typeof(LibraryConnectDbContext).Assembly
            .GetTypes()
            .Where(type => typeof(Migration).IsAssignableFrom(type) && !type.IsAbstract)
            .ToDictionary(
                type => type.GetCustomAttribute<MigrationAttribute>()?.Id ?? $"(thiếu thuộc tính) {type.Name}",
                type => type.Name);

        var thieu = new List<string>();

        foreach (var tep in Directory.GetFiles(thuMuc, "*.cs")
                     .Where(path => !path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)))
        {
            var khop = TenTep.Match(Path.GetFileName(tep));

            if (!khop.Success)
            {
                continue;
            }

            var ma = $"{khop.Groups["id"].Value}_{khop.Groups["ten"].Value}";

            if (!trongAssembly.ContainsKey(ma))
            {
                thieu.Add($"{Path.GetFileName(tep)}: không có lớp nào mang [Migration(\"{ma}\")]");
            }
        }

        thieu.Should().BeEmpty(
            "migration thiếu thuộc tính thì EF Core bỏ qua nó trong im lặng — bản sửa dữ liệu đã triển khai mà không chạy");
    }

    [Fact]
    public void Khong_migration_nao_trong_assembly_bi_bo_quen_thuoc_tinh()
    {
        var khongMa = typeof(LibraryConnectDbContext).Assembly
            .GetTypes()
            .Where(type => typeof(Migration).IsAssignableFrom(type) && !type.IsAbstract)
            .Where(type => type.GetCustomAttribute<MigrationAttribute>() is null)
            .Select(type => type.Name)
            .ToList();

        khongMa.Should().BeEmpty("lớp kế thừa Migration mà không có [Migration] là lớp chết");
    }

    private static string ThuMucMigrations()
    {
        var thuMuc = new DirectoryInfo(AppContext.BaseDirectory);

        while (thuMuc is not null)
        {
            var duong = Path.Combine(thuMuc.FullName, "src", "LibraryConnect.Infrastructure", "Persistence", "Migrations");

            if (Directory.Exists(duong))
            {
                return duong;
            }

            thuMuc = thuMuc.Parent;
        }

        throw new DirectoryNotFoundException("Không tìm thấy thư mục Migrations của dự án hạ tầng.");
    }
}
