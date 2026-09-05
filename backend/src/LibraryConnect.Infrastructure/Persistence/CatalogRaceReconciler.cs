using LibraryConnect.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;

namespace LibraryConnect.Infrastructure.Persistence;

/// <summary>
/// Hoà giải khi hai lượt lưu cùng lúc tạo cùng một mục danh mục.
///
/// <para>Bốn lượt biên mục sơ lược song song, cùng một tác giả chưa có trong hồ sơ thẩm quyền: cả bốn
/// đều tra không thấy nên cùng tạo, lượt đầu ghi được, ba lượt sau đổ ở <c>ux_author_code</c> — đúng
/// như bài học số 1 trong <c>CLAUDE.md</c>: tầng nghiệp vụ không chặn được tranh chấp, chỉ ràng buộc
/// duy nhất ở cơ sở dữ liệu chặn được. Nhưng ở đây ràng buộc chặn xong thì câu trả lời đúng không phải
/// "giá trị đã tồn tại, nhập giá trị khác" — cán bộ không nhập mã tác giả nào cả — mà là <b>dùng luôn
/// mục người kia vừa tạo</b>. Đó là việc của lớp này.</para>
///
/// <para>Sau khi <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> đổ vì vi phạm ràng buộc
/// duy nhất trên một bảng của lược đồ <c>cat</c>, mỗi mục danh mục đang chờ thêm vào bảng ấy được đối
/// chiếu lại với cơ sở dữ liệu:</para>
/// <list type="bullet">
///   <item>đã có mục còn sống cùng khoá tên (<c>cat.lc_name_key</c>) — lượt lưu <b>nhận mục ấy</b>: đổi
///   khoá của thực thể đang chờ sang khoá của mục có sẵn, trỏ lại mọi khoá ngoại đang tham chiếu nó,
///   rồi đánh dấu không thay đổi để không sinh dòng mới;</item>
///   <item>chưa có mục cùng tên nhưng <b>mã</b> đã bị dùng (hai tên khác nhau cắt về cùng một mã 40 ký
///   tự) — sinh mã khác có hậu tố số.</item>
/// </list>
/// <para>Xong thì lượt lưu chạy lại. Mọi bảng danh mục đều đi qua đây nên tác giả, đề mục, từ khoá,
/// nhà xuất bản, tùng thư — và cả lượt tạo tay ở màn hình danh mục — được cùng một cách xử lý.</para>
/// </summary>
internal static class CatalogRaceReconciler
{
    private const string CatalogSchema = "cat";
    private const int MaxCodeLength = 40;

    /// <summary>Đúng loại lỗi mà bộ hoà giải xử lý được: vi phạm ràng buộc duy nhất trên một bảng danh mục.</summary>
    public static bool LooksLikeCatalogRace(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            SchemaName: CatalogSchema,
            TableName: { Length: > 0 }
        };

    /// <summary>
    /// Đối chiếu lại các mục danh mục đang chờ thêm vào bảng vừa đổ. Trả về <c>true</c> khi có ít nhất
    /// một mục được trỏ lại hoặc đổi mã — lúc ấy lưu lần nữa là có nghĩa; <c>false</c> thì lỗi gốc phải
    /// được ném ra như cũ.
    /// </summary>
    public static async Task<bool> TryReconcileAsync(DbContext db, DbUpdateException exception, CancellationToken ct)
    {
        if (exception.InnerException is not PostgresException postgres || !LooksLikeCatalogRace(exception))
        {
            return false;
        }

        // Tên bảng lấy từ chính lỗi PostgreSQL và chỉ được dùng khi khớp một bảng trong mô hình EF —
        // nó là định danh của mình, không phải chuỗi người dùng gửi lên.
        var pending = db.ChangeTracker.Entries()
            .Where(entry => entry.State == EntityState.Added
                            && entry.Entity is CatalogEntity
                            && entry.Metadata.GetSchema() == CatalogSchema
                            && entry.Metadata.GetTableName() == postgres.TableName)
            .ToList();

        if (pending.Count == 0)
        {
            return false;
        }

        var table = postgres.TableName!;
        var reconciled = false;

        foreach (var entry in pending)
        {
            var entity = (CatalogEntity)entry.Entity;

            var sameNameSql = "SELECT id AS \"Value\" FROM " + Qualified(table)
                + " WHERE deleted_at IS NULL AND id <> {0}"
                + " AND cat.lc_name_key(name) = cat.lc_name_key({1})"
                + " ORDER BY created_at, id LIMIT 1";

            var existingId = await db.Database
                .SqlQueryRaw<Guid>(sameNameSql, entity.Id, entity.Name)
                .FirstOrDefaultAsync(ct);

            if (existingId != Guid.Empty)
            {
                Adopt(db, entry, entity, existingId);
                reconciled = true;
                continue;
            }

            // Chỉ mục mã không lọc theo deleted_at, nên mã của mục đã xoá mềm cũng là mã đã dùng.
            if (await CodeTakenAsync(db, table, entity.Code, ct))
            {
                entity.Code = await FreeCodeAsync(db, table, entity.Code, pending, entry, ct);
                reconciled = true;
            }
        }

        return reconciled;
    }

    /// <summary>
    /// Thực thể đang chờ thêm trở thành đại diện của mục có sẵn: đổi khoá, trỏ lại các khoá ngoại còn
    /// mang khoá cũ, rồi đánh dấu không thay đổi. Đổi khoá của thực thể ở trạng thái Added là hợp lệ với
    /// EF Core, và handler cầm tham chiếu tới thực thể ấy đọc được khoá mới ngay (màn hình danh mục
    /// trả về đúng khoá của mục đã có).
    /// </summary>
    private static void Adopt(DbContext db, EntityEntry entry, CatalogEntity entity, Guid existingId)
    {
        var oldId = entity.Id;

        entry.Property(nameof(CatalogEntity.Id)).CurrentValue = existingId;

        foreach (var dependent in db.ChangeTracker.Entries().Where(other => other.State != EntityState.Detached).ToList())
        {
            foreach (var foreignKey in dependent.Metadata.GetForeignKeys())
            {
                if (!foreignKey.PrincipalEntityType.ClrType.IsAssignableFrom(entry.Metadata.ClrType)
                    || foreignKey.Properties.Count != 1)
                {
                    continue;
                }

                var property = dependent.Property(foreignKey.Properties[0].Name);

                if (property.CurrentValue is Guid value && value == oldId)
                {
                    property.CurrentValue = existingId;
                }
            }
        }

        entry.State = EntityState.Unchanged;
    }

    /// <summary>
    /// Tên bảng đủ lược đồ. Chỉ nhận tên đã khớp một bảng trong mô hình EF (xem
    /// <see cref="TryReconcileAsync"/>) và chỉ gồm chữ thường, số, gạch dưới — không phải chuỗi người dùng gửi lên.
    /// </summary>
    private static string Qualified(string table)
    {
        if (table.Length == 0 || !table.All(character => char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '_'))
        {
            throw new InvalidOperationException($"Tên bảng danh mục không hợp lệ: '{table}'.");
        }

        return string.Concat(CatalogSchema, ".", table);
    }

    private static async Task<bool> CodeTakenAsync(DbContext db, string table, string code, CancellationToken ct)
    {
        var sql = "SELECT 1 AS \"Value\" FROM " + Qualified(table) + " WHERE code = {0} LIMIT 1";

        return await db.Database.SqlQueryRaw<int>(sql, code).AnyAsync(ct);
    }

    private static async Task<string> FreeCodeAsync(
        DbContext db, string table, string root, List<EntityEntry> pending, EntityEntry self, CancellationToken ct)
    {
        // Bỏ hậu tố số cũ (nếu có) để không ra "_2_2".
        var underscore = root.LastIndexOf('_');

        if (underscore > 0 && int.TryParse(root[(underscore + 1)..], out _))
        {
            root = root[..underscore];
        }

        for (var suffix = 2; suffix <= 99; suffix++)
        {
            var tail = $"_{suffix}";
            var room = Math.Min(root.Length, MaxCodeLength - tail.Length);
            var candidate = root[..room].TrimEnd('_') + tail;

            var takenLocally = pending.Any(other => other != self && ((CatalogEntity)other.Entity).Code == candidate);

            if (!takenLocally && !await CodeTakenAsync(db, table, candidate, ct))
            {
                return candidate;
            }
        }

        return $"{root[..Math.Min(root.Length, 26)].TrimEnd('_')}_{Guid.NewGuid():N}"[..MaxCodeLength];
    }
}
