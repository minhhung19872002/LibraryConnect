using System.Linq.Expressions;
using System.Text.RegularExpressions;
using LibraryConnect.Application.Common.Extensions;
using LibraryConnect.Application.Common.Text;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Domain.Enums;

namespace LibraryConnect.Application.Features.Opac;

/// <summary>
/// Nơi duy nhất dựng câu truy vấn tra cứu.
///
/// Tìm kiếm cơ bản, tìm kiếm nâng cao và bộ đếm facet đều đi qua đây, nên ba chỗ đó không bao giờ
/// lệch nhau: con số dưới mỗi bộ lọc luôn đúng bằng số dòng kết quả khi bấm vào bộ lọc đó.
/// </summary>
public static class OpacQueryBuilder
{
    /// <summary>
    /// Trần cho phép đếm và cho bộ đếm bộ lọc của trang tra cứu.
    ///
    /// Vượt ngưỡng này thì giao diện ghi "hơn 10.000 kết quả" thay vì một con số chính xác. Đếm
    /// chính xác buộc cơ sở dữ liệu đọc hết mọi dòng khớp điều kiện: trên kho 500.000 biểu ghi, một
    /// câu hỏi rộng như "giáo trình" khớp hơn 60.000 dòng và riêng phép đếm ấy đã vượt một giây —
    /// ngưỡng mà mục 6.3 đặt ra cho cả lượt tra cứu.
    /// </summary>
    public const int CountLimit = 10_000;

    /// <summary>
    /// Chỉ biểu ghi đã xuất bản mới ra trang tra cứu.
    ///
    /// Biểu ghi nháp, biểu ghi đang nằm trong hàng đợi biên mục và biểu ghi mới thu hoạch về đều
    /// chưa được cán bộ soát lại; đưa ra cho bạn đọc là đưa dữ liệu nửa vời ra ngoài.
    /// </summary>
    public static IQueryable<BibRecord> Published(IQueryable<BibRecord> records) =>
        records.Where(bib => bib.Status == RecordStatus.Published);

    /// <summary>Chuẩn hóa từ khóa: bỏ dấu và viết thường, đúng như cách dựng chỉ mục.</summary>
    public static string Normalise(string term) =>
        VietnameseText.RemoveDiacritics(term.Trim()).ToLowerInvariant();

    /// <summary>Điều kiện ứng với một mệnh đề tìm kiếm.</summary>
    public static Expression<Func<BibRecord, bool>> Clause(OpacSearchScope scope, string term)
    {
        var keyword = Normalise(term);

        if (string.IsNullOrEmpty(keyword))
        {
            return PredicateBuilder.True<BibRecord>();
        }

        return scope switch
        {
            OpacSearchScope.Title => EveryWord(keyword,
                word => bib =>
                    DatabaseFunctions.Unaccent(bib.Title).Contains(word)
                    || DatabaseFunctions.Unaccent(bib.Subtitle ?? string.Empty).Contains(word)
                    || DatabaseFunctions.Unaccent(bib.UniformTitle ?? string.Empty).Contains(word),
                pattern => bib =>
                    Regex.IsMatch(DatabaseFunctions.Unaccent(bib.Title), pattern)
                    || Regex.IsMatch(DatabaseFunctions.Unaccent(bib.Subtitle ?? string.Empty), pattern)
                    || Regex.IsMatch(DatabaseFunctions.Unaccent(bib.UniformTitle ?? string.Empty), pattern)),

            // Chỉ hỏi qua bảng liên kết chứ không kèm điều kiện trên cột tác giả chính: tác giả
            // chính luôn được ghi vào bảng liên kết cùng lúc với biểu ghi, nên hai điều kiện cho
            // cùng một kết quả — mà nối chúng bằng HOẶC thì PostgreSQL bỏ chỉ mục và quét cả kho.
            OpacSearchScope.Author => bib =>
                bib.Authors.Any(link =>
                    DatabaseFunctions.Unaccent(link.Author!.Name).Contains(keyword)),

            OpacSearchScope.Subject => bib =>
                bib.Subjects.Any(link =>
                    DatabaseFunctions.Unaccent(link.Subject!.Name).Contains(keyword)),

            OpacSearchScope.Keyword => bib =>
                bib.Keywords.Any(link =>
                    DatabaseFunctions.Unaccent(link.Keyword!.Name).Contains(keyword)),

            OpacSearchScope.Publisher => bib =>
                DatabaseFunctions.Unaccent(bib.PublisherName ?? string.Empty).Contains(keyword),

            // Mã số thì người ta gõ có gạch nối, có khoảng trắng, tùy chỗ chép về. So sánh trên
            // chuỗi đã bỏ hết ký tự ngăn cách thì gõ kiểu nào cũng ra.
            OpacSearchScope.Isbn => bib =>
                (bib.Isbn ?? string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty)
                    .Contains(keyword.Replace("-", string.Empty).Replace(" ", string.Empty))
                || (bib.Issn ?? string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty)
                    .Contains(keyword.Replace("-", string.Empty).Replace(" ", string.Empty)),

            OpacSearchScope.CallNumber => bib =>
                (bib.Ddc ?? string.Empty).ToLower().Contains(keyword)
                || bib.Items.Any(item =>
                    (item.CallNumber ?? string.Empty).ToLower().Contains(keyword)
                    || item.RegisterNumber.ToLower().Contains(keyword)
                    || item.Barcode.ToLower().Contains(keyword)),

            // Phạm vi "tất cả" soi đúng những chỗ liệt kê ở chú thích của cột SearchAll: nhan đề,
            // nhan đề khác, tác giả chính và tác giả bổ sung, chủ đề, từ khóa, nhà xuất bản, ISBN,
            // ISSN, số kiểm soát và tóm tắt. Cơ sở dữ liệu đã gộp sẵn tất cả vào một cột có chỉ mục,
            // nên ở đây chỉ còn một điều kiện — viết tách ra thành mười một điều kiện HOẶC thì mỗi
            // lượt tra cứu phải quét cả kho.
            _ => EveryWord(keyword,
                word => bib => bib.SearchAll.Contains(word),
                pattern => bib => Regex.IsMatch(bib.SearchAll, pattern))
        };
    }

    /// <summary>
    /// Ký tự ngăn cách khi tách từ khóa thành từng từ. Dấu chấm và gạch nối giữ lại vì chúng nằm trong
    /// chỉ số phân loại (005.74) và ISBN; mọi dấu câu khác chỉ là cách người gõ chép nhan đề.
    /// </summary>
    private static readonly char[] WordSeparators =
    {
        ' ', '\t', '\r', '\n', '\"', '\'', '\u201c', '\u201d', '\u2018', '\u2019', '(', ')', '[', ']', '{', '}',
        ',', ';', ':', '!', '?', '\u2014', '\u2013', '/', '\\', '|', '+', '&', '*', '<', '>', '='
    };

    /// <summary>Tách từ khóa đã chuẩn hóa thành các từ riêng, bỏ dấu câu và từ lặp.</summary>
    public static IReadOnlyList<string> Words(string keyword) =>
        keyword.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .ToList();

    /// <summary>
    /// Mẫu biểu thức chính quy PostgreSQL cho một từ: <c>\m</c> là đầu từ, <c>\M</c> là cuối từ. Từ ngắn
    /// (âm tiết tiếng Việt: "co", "so", "du", "lieu") phải trọn vẹn — so chuỗi con thì "co" trúng "cong",
    /// "so" trúng "so" của "số hóa", và "cơ sở dữ liệu" trên kho thật vọt từ 45 lên 805 kết quả. Từ dài
    /// hơn bốn ký tự (thường là tiếng Anh) chỉ cần đúng tiền tố, để "system" vẫn khớp "systems".
    /// </summary>
    public static string WordPattern(string word)
    {
        var escaped = Regex.Escape(word);

        return word.Length <= 4 ? $"\\m{escaped}\\M" : $"\\m{escaped}";
    }

    /// <summary>
    /// Mọi từ trong từ khóa đều phải có mặt, mỗi từ ở đâu cũng được.
    ///
    /// Trước 05/09/2026 cả cụm từ khóa được so như một chuỗi con liền nhau: gõ đúng nhan đề "Cơ sở dữ
    /// liệu — lý thuyết và bài tập" ra 0 kết quả vì dấu gạch, gõ "cơ sở dữ liệu bài tập" cũng 0 vì
    /// hai cụm không đứng cạnh nhau. Bạn đọc nhớ vài từ của nhan đề chứ không nhớ thứ tự và dấu câu.
    ///
    /// Một từ thì giữ phép so chuỗi con như cũ ("kinh" vẫn ra "kinh tế"). Từ hai từ trở lên thì mỗi từ
    /// so trọn từ bằng biểu thức chính quy có biên từ — pg_trgm hỗ trợ toán tử <c>~</c> nên chỉ mục ba
    /// ký tự trên cột vẫn được dùng.
    /// </summary>
    private static Expression<Func<BibRecord, bool>> EveryWord(
        string keyword,
        Func<string, Expression<Func<BibRecord, bool>>> substring,
        Func<string, Expression<Func<BibRecord, bool>>> wholeWord)
    {
        var words = Words(keyword);

        if (words.Count == 0)
        {
            return PredicateBuilder.True<BibRecord>();
        }

        if (words.Count == 1)
        {
            return substring(words[0]);
        }

        return words.Skip(1).Aggregate(
            wholeWord(WordPattern(words[0])),
            (current, word) => current.And(wholeWord(WordPattern(word))));
    }

    /// <summary>
    /// Ghép các mệnh đề của tìm kiếm nâng cao.
    ///
    /// Mệnh đề đầu tiên luôn là điểm xuất phát, dấu nối của nó bị bỏ qua — "HOẶC" đứng đầu câu thì
    /// không có nghĩa gì. Riêng "KHÔNG" ở vị trí đầu được hiểu là loại trừ khỏi toàn kho.
    /// </summary>
    public static Expression<Func<BibRecord, bool>> Combine(IReadOnlyList<OpacSearchClause> clauses)
    {
        ArgumentNullException.ThrowIfNull(clauses);

        var usable = clauses
            .Where(clause => !string.IsNullOrWhiteSpace(clause.Term))
            .ToList();

        if (usable.Count == 0)
        {
            return PredicateBuilder.True<BibRecord>();
        }

        Expression<Func<BibRecord, bool>>? combined = null;

        foreach (var clause in usable)
        {
            var predicate = Clause(clause.Field, clause.Term);

            if (combined is null)
            {
                combined = clause.Connector == OpacConnector.Not
                    ? PredicateBuilder.True<BibRecord>().And(predicate.Not())
                    : predicate;

                continue;
            }

            combined = clause.Connector switch
            {
                OpacConnector.Or => combined.Or(predicate),
                OpacConnector.Not => combined.And(predicate.Not()),
                _ => combined.And(predicate)
            };
        }

        return combined!;
    }

    /// <summary>
    /// Đồng bộ delta cho ứng dụng di động (Phase 15, mục 3.4): chỉ những biểu ghi đổi từ mốc này. Biểu
    /// ghi chưa sửa lần nào thì mốc là lúc tạo.
    /// </summary>
    public static IQueryable<BibRecord> ApplyUpdatedSince(IQueryable<BibRecord> records, DateTimeOffset? since) =>
        records.WhereIf(since is not null, bib => (bib.UpdatedAt ?? bib.CreatedAt) >= since);

    /// <summary>Bộ lọc bên trái kết quả: năm, ngôn ngữ, dạng tài liệu, kho, bộ sưu tập…</summary>
    public static IQueryable<BibRecord> ApplyFilter(IQueryable<BibRecord> records, OpacFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        records = records
            .WhereIf(filter.PublishYearFrom is not null,
                bib => bib.PublishYear >= filter.PublishYearFrom)
            .WhereIf(filter.PublishYearTo is not null,
                bib => bib.PublishYear <= filter.PublishYearTo)
            .WhereIf(filter.LanguageId is not null, bib => bib.LanguageId == filter.LanguageId)
            .WhereIf(filter.DocumentTypeId is not null,
                bib => bib.DocumentTypeId == filter.DocumentTypeId)
            .WhereIf(filter.PublisherId is not null, bib => bib.PublisherId == filter.PublisherId)
            .WhereIf(filter.AuthorId is not null,
                bib => bib.Authors.Any(link => link.AuthorId == filter.AuthorId))
            .WhereIf(filter.SubjectId is not null,
                bib => bib.Subjects.Any(link => link.SubjectId == filter.SubjectId))
            .WhereIf(filter.CollectionId is not null,
                bib => bib.Collections.Any(link => link.CollectionId == filter.CollectionId))
            .WhereIf(filter.CustomIndexValueId is not null,
                bib => bib.CustomIndexLinks.Any(
                    link => link.CustomIndexValueId == filter.CustomIndexValueId))
            .WhereIf(filter.CourseId is not null,
                bib => bib.Courses.Any(link => link.CourseId == filter.CourseId))
            .WhereIf(filter.HasDigital == true, bib => bib.DigitalDocumentCount > 0)
            .WhereIf(filter.AvailableOnly == true, bib => bib.AvailableItemCount > 0);

        if (!string.IsNullOrWhiteSpace(filter.Ddc))
        {
            // Duyệt theo môn loại là duyệt cả nhánh: chọn 005 phải ra cả 005.1 và 005.74.
            var prefix = filter.Ddc.Trim();
            records = records.Where(bib => bib.Ddc != null && bib.Ddc.StartsWith(prefix));
        }

        if (filter.WarehouseId is not null)
        {
            records = records.Where(bib =>
                bib.Items.Any(item => item.WarehouseId == filter.WarehouseId));
        }

        return records;
    }

    /// <summary>
    /// Sắp xếp kết quả.
    ///
    /// "Liên quan nhất" ở đây là: tài liệu mà nhan đề bắt đầu bằng đúng từ khóa lên trước, rồi tới
    /// tài liệu được mượn nhiều, cuối cùng mới tới thứ tự chữ cái. Không có bảng điểm phức tạp,
    /// nhưng đúng với cái người tra cứu mong đợi khi gõ tên một cuốn sách.
    /// </summary>
    public static IQueryable<BibRecord> ApplySort(
        IQueryable<BibRecord> records, OpacSortOrder sort, string? keyword)
    {
        var normalised = string.IsNullOrWhiteSpace(keyword) ? null : Normalise(keyword);

        return sort switch
        {
            OpacSortOrder.Newest => records
                .OrderByDescending(bib => bib.CreatedAt)
                .ThenBy(bib => bib.Title),

            OpacSortOrder.Title => records
                .OrderBy(bib => bib.Title)
                .ThenBy(bib => bib.PublishYear),

            OpacSortOrder.Author => records
                .OrderBy(bib => bib.AuthorMain ?? string.Empty)
                .ThenBy(bib => bib.Title),

            OpacSortOrder.Popular => records
                .OrderByDescending(bib => bib.LoanCount)
                .ThenByDescending(bib => bib.ViewCount)
                .ThenBy(bib => bib.Title),

            _ when normalised is not null => records
                .OrderByDescending(bib =>
                    DatabaseFunctions.Unaccent(bib.Title).StartsWith(normalised))
                .ThenByDescending(bib => bib.LoanCount)
                .ThenBy(bib => bib.Title),

            _ => records
                .OrderByDescending(bib => bib.CreatedAt)
                .ThenBy(bib => bib.Title)
        };
    }

    /// <summary>Chuyển một biểu ghi sang dòng kết quả. Dùng chung để mọi danh sách hiện như nhau.</summary>
    public static Expression<Func<BibRecord, OpacResultDto>> ToResult() => bib => new OpacResultDto(
        bib.Id,
        bib.ControlNumber,
        bib.Title,
        bib.Subtitle,
        bib.AuthorMain,
        bib.PublisherName,
        bib.PublishYear,
        bib.Isbn,
        bib.Ddc,
        bib.DocumentType!.Name,
        bib.Language!.Name,
        bib.CoverImageUrl,
        bib.Abstract,
        bib.ItemCount,
        bib.AvailableItemCount,
        bib.DigitalDocumentCount,
        bib.LoanCount);
}
