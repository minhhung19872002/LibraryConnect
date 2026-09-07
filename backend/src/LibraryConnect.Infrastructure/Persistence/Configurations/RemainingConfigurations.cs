using LibraryConnect.Domain.Entities.Cir;
using LibraryConnect.Domain.Entities.Dig;
using LibraryConnect.Domain.Entities.Ill;
using LibraryConnect.Domain.Entities.Rdr;
using LibraryConnect.Domain.Entities.Ser;
using LibraryConnect.Domain.Entities.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryConnect.Infrastructure.Persistence.Configurations;

// ---------------- ser ----------------

public class SerialConfiguration : IEntityTypeConfiguration<Serial>
{
    public void Configure(EntityTypeBuilder<Serial> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Issn).HasMaxLength(50);
        builder.Property(x => x.CallNumber).HasMaxLength(200);
        builder.Property(x => x.FrequencyConfig).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.PricePerIssue).HasPrecision(18, 2);

        builder.HasOne(x => x.Bib).WithMany().HasForeignKey(x => x.BibId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Publisher).WithMany().HasForeignKey(x => x.PublisherId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Language).WithMany().HasForeignKey(x => x.LanguageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Issn).HasDatabaseName("ix_serials_issn");
        builder.HasIndex(x => x.Title).HasDatabaseName("ix_serials_title");
    }
}

public class SerialIssueConfiguration : IEntityTypeConfiguration<SerialIssue>
{
    public void Configure(EntityTypeBuilder<SerialIssue> builder)
    {
        builder.Property(x => x.IssueNo).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Volume).HasMaxLength(50);
        builder.Property(x => x.Caption).HasMaxLength(300);
        builder.Property(x => x.Barcode).HasMaxLength(100);
        // Phải khai lại đúng độ dài của migration 20260904140000, nếu không lượt sinh migration sau
        // sẽ thấy mô hình lệch bảng và tự thêm một lệnh đổi kiểu cột không ai muốn.
        builder.Property(x => x.Condition).HasMaxLength(200);

        builder.HasOne(x => x.Serial).WithMany(s => s.Issues).HasForeignKey(x => x.SerialId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.SerialId, x.Year, x.IssueNo }).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_serial_issues");
        builder.HasIndex(x => new { x.SerialId, x.Status }).HasDatabaseName("ix_serial_issues_status");
        builder.HasIndex(x => x.ExpectedDate).HasDatabaseName("ix_serial_issues_expected");
    }
}

public class SerialIssueArticleConfiguration : IEntityTypeConfiguration<SerialIssueArticle>
{
    public void Configure(EntityTypeBuilder<SerialIssueArticle> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(2000).IsRequired();
        builder.HasOne(x => x.Issue).WithMany(i => i.Articles).HasForeignKey(x => x.IssueId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Bib).WithMany().HasForeignKey(x => x.BibId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => x.IssueId).HasDatabaseName("ix_serial_articles_issue");
    }
}

public class SerialBindingConfiguration : IEntityTypeConfiguration<SerialBinding>
{
    public void Configure(EntityTypeBuilder<SerialBinding> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.HasOne(x => x.Serial).WithMany().HasForeignKey(x => x.SerialId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_serial_bindings_code");
    }
}

public class SerialClaimConfiguration : IEntityTypeConfiguration<SerialClaim>
{
    public void Configure(EntityTypeBuilder<SerialClaim> builder)
    {
        builder.Property(x => x.ClaimNo).HasMaxLength(50).IsRequired();
        builder.HasOne(x => x.Issue).WithMany().HasForeignKey(x => x.IssueId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_serial_claims_status");
    }
}

// ---------------- dig ----------------

public class DigitalCollectionConfiguration : HierarchicalCatalogConfiguration<DigitalCollection> { }

public class DigitalDocumentConfiguration : IEntityTypeConfiguration<DigitalDocument>
{
    public void Configure(EntityTypeBuilder<DigitalDocument> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.FilePath).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.MimeType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ChecksumSha256).HasMaxLength(64);

        builder.HasOne(x => x.Bib).WithMany().HasForeignKey(x => x.BibId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Collection).WithMany().HasForeignKey(x => x.CollectionId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.BibId).HasDatabaseName("ix_digital_documents_bib");
        builder.HasIndex(x => x.AccessLevel).HasDatabaseName("ix_digital_documents_access");
        builder.HasIndex(x => x.ChecksumSha256).HasDatabaseName("ix_digital_documents_checksum");
    }
}

public class DigitalDocumentFileConfiguration : IEntityTypeConfiguration<DigitalDocumentFile>
{
    public void Configure(EntityTypeBuilder<DigitalDocumentFile> builder)
    {
        builder.Property(x => x.Path).HasMaxLength(1000).IsRequired();
        builder.HasOne(x => x.Document).WithMany(d => d.Files).HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.DocumentId, x.Type }).HasDatabaseName("ix_digital_files_document");
    }
}

public class DigitalAccessRequestConfiguration : IEntityTypeConfiguration<DigitalAccessRequest>
{
    public void Configure(EntityTypeBuilder<DigitalAccessRequest> builder)
    {
        builder.HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.ReaderId, x.Status }).HasDatabaseName("ix_digital_requests_reader");
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_digital_requests_status");
    }
}

public class DigitalUploadSessionConfiguration : IEntityTypeConfiguration<DigitalUploadSession>
{
    public void Configure(EntityTypeBuilder<DigitalUploadSession> builder)
    {
        builder.Property(x => x.FileName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.MimeType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(2000);
        builder.HasIndex(x => x.ExpiresAt).HasDatabaseName("ix_digital_upload_sessions_expires");
    }
}

public class DigitalAccessLogConfiguration : IEntityTypeConfiguration<DigitalAccessLog>
{
    public void Configure(EntityTypeBuilder<DigitalAccessLog> builder)
    {
        builder.Property(x => x.Ip).HasMaxLength(64);
        builder.Property(x => x.Device).HasMaxLength(300);
        builder.HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.DocumentId, x.OccurredAt }).HasDatabaseName("ix_digital_logs_document");
        builder.HasIndex(x => x.ReaderId).HasDatabaseName("ix_digital_logs_reader");
    }
}

// ---------------- rdr ----------------

public class ReaderConfiguration : IEntityTypeConfiguration<Reader>
{
    public void Configure(EntityTypeBuilder<Reader> builder)
    {
        builder.Property(x => x.CardNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.StudentCode).HasMaxLength(50);
        builder.Property(x => x.FullName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Gender).HasMaxLength(20);
        builder.Property(x => x.IdCardNumber).HasMaxLength(50);
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.ClassName).HasMaxLength(100);
        builder.Property(x => x.CourseYear).HasMaxLength(50);
        builder.Property(x => x.AvatarUrl).HasMaxLength(1000);
        builder.Property(x => x.PhotoUrl).HasMaxLength(1000);
        builder.Property(x => x.PasswordHash).HasMaxLength(200);
        builder.Property(x => x.DepositAmount).HasPrecision(18, 2);
        builder.Property(x => x.DebtAmount).HasPrecision(18, 2);

        builder.HasOne(x => x.ReaderType).WithMany().HasForeignKey(x => x.ReaderTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Faculty).WithMany().HasForeignKey(x => x.FacultyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Major).WithMany().HasForeignKey(x => x.MajorId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CardNumber).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_readers_card_number");
        builder.HasIndex(x => x.StudentCode).HasDatabaseName("ix_readers_student_code");
        builder.HasIndex(x => x.FullName).HasDatabaseName("ix_readers_full_name");
        builder.HasIndex(x => new { x.Status, x.CardExpireDate }).HasDatabaseName("ix_readers_status");
    }
}

public class ReaderCardConfiguration : IEntityTypeConfiguration<ReaderCard>
{
    public void Configure(EntityTypeBuilder<ReaderCard> builder)
    {
        builder.Property(x => x.CardNumber).HasMaxLength(50).IsRequired();
        builder.HasOne(x => x.Reader).WithMany(r => r.Cards).HasForeignKey(x => x.ReaderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.CardNumber).HasDatabaseName("ix_reader_cards_number");

        // Một bạn đọc chỉ có đúng một thẻ đang hiệu lực (VI.1: "cấp lại thẻ, giữ lịch sử thẻ cũ").
        // Tầng nghiệp vụ đã hạ cờ thẻ cũ rồi mới dựng thẻ mới, nhưng đó là đọc-rồi-ghi: ba lượt cấp
        // lại bấm cùng lúc trên máy chủ thật ngày 07/09/2026 để lại **ba thẻ cùng hiệu lực**, nghĩa
        // là thẻ đã báo mất vẫn quét được ở cổng. Bài học 1 và 45: luật "một … một" phải có ràng
        // buộc duy nhất ở cơ sở dữ liệu.
        // Giữ chỉ mục thường của khóa ngoại: màn hình hồ sơ đọc **mọi** thẻ của bạn đọc, mà chỉ mục
        // riêng phần ở dưới chỉ phủ thẻ đang hiệu lực.
        builder.HasIndex(x => x.ReaderId, "ix_reader_cards_reader_id")
            .HasDatabaseName("ix_reader_cards_reader_id");

        builder.HasIndex(x => x.ReaderId, "ux_reader_cards_hien_hanh")
            .IsUnique()
            .HasFilter("is_current AND deleted_at IS NULL")
            .HasDatabaseName("ux_reader_cards_hien_hanh");
    }
}

public class ReaderCardTemplateConfiguration : IEntityTypeConfiguration<ReaderCardTemplate>
{
    public void Configure(EntityTypeBuilder<ReaderCardTemplate> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.FrontLayout).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.BackLayout).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_reader_card_templates_code");
    }
}

public class ReaderImportBatchConfiguration : IEntityTypeConfiguration<ReaderImportBatch>
{
    public void Configure(EntityTypeBuilder<ReaderImportBatch> builder)
    {
        builder.Property(x => x.FileName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Errors).HasColumnType("jsonb");
    }
}

public class ReaderViolationConfiguration : IEntityTypeConfiguration<ReaderViolation>
{
    public void Configure(EntityTypeBuilder<ReaderViolation> builder)
    {
        builder.Property(x => x.FineAmount).HasPrecision(18, 2);
        builder.HasOne(x => x.Reader).WithMany(r => r.Violations).HasForeignKey(x => x.ReaderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ViolationType).WithMany().HasForeignKey(x => x.ViolationTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CardRenewalRequestConfiguration : IEntityTypeConfiguration<CardRenewalRequest>
{
    public void Configure(EntityTypeBuilder<CardRenewalRequest> builder)
    {
        builder.HasOne(x => x.Reader).WithMany().HasForeignKey(x => x.ReaderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_card_renewal_status");
    }
}

// ---------------- cir ----------------

public class CirculationPolicyConfiguration : IEntityTypeConfiguration<CirculationPolicy>
{
    public void Configure(EntityTypeBuilder<CirculationPolicy> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.FinePerDay).HasPrecision(18, 2);

        builder.HasOne(x => x.ReaderType).WithMany().HasForeignKey(x => x.ReaderTypeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.DocumentType).WithMany().HasForeignKey(x => x.DocumentTypeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ReaderTypeId, x.DocumentTypeId, x.WarehouseId })
            .HasDatabaseName("ix_circulation_policies_matrix");
    }
}

public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.BibTitle).HasMaxLength(2000);
        builder.Property(x => x.Barcode).HasMaxLength(100);
        builder.Property(x => x.FineAmount).HasPrecision(18, 2);
        builder.Property(x => x.FinePaid).HasPrecision(18, 2);

        builder.HasOne(x => x.Reader).WithMany().HasForeignKey(x => x.ReaderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Code).IsUnique().HasFilter("deleted_at IS NULL").HasDatabaseName("ux_loans_code");

        // Một bản in chỉ có một phiếu mượn đang mở. Luật này phải nằm ở cơ sở dữ liệu chứ không chỉ
        // ở tầng nghiệp vụ: tầng nghiệp vụ đọc "sách còn rảnh không" rồi mới ghi, nên hai quầy làm
        // việc cùng lúc đều đọc thấy rảnh và cùng ghi một phiếu. Chỉ ràng buộc duy nhất mới chặn
        // được, vì nó do chính máy chủ dữ liệu quyết định lúc ghi.
        // Phiếu đã trả, đã mất, đã hỏng đều có return_date nên không lọt vào chỉ mục này.
        builder.HasIndex(x => x.ItemId)
            .IsUnique()
            .HasFilter("return_date IS NULL AND deleted_at IS NULL")
            .HasDatabaseName("ux_loans_item_dang_muon");
        builder.HasIndex(x => new { x.ReaderId, x.Status }).HasDatabaseName("ix_loan_reader_status");
        builder.HasIndex(x => new { x.ItemId, x.Status }).HasDatabaseName("ix_loan_item_status");
        builder.HasIndex(x => x.DueDate).HasDatabaseName("ix_loan_due");
        builder.HasIndex(x => x.LoanDate).HasDatabaseName("ix_loan_date");
    }
}

public class LoanRenewalConfiguration : IEntityTypeConfiguration<LoanRenewal>
{
    public void Configure(EntityTypeBuilder<LoanRenewal> builder)
    {
        builder.HasOne(x => x.Loan).WithMany().HasForeignKey(x => x.LoanId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.LoanId, x.RenewalDate }).HasDatabaseName("ix_loan_renewals_loan");
    }
}

public class HoldConfiguration : IEntityTypeConfiguration<Hold>
{
    public void Configure(EntityTypeBuilder<Hold> builder)
    {
        builder.HasOne(x => x.Reader).WithMany().HasForeignKey(x => x.ReaderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Bib).WithMany().HasForeignKey(x => x.BibId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.PickupWarehouse).WithMany().HasForeignKey(x => x.PickupWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.BibId, x.Status, x.QueuePosition }).HasDatabaseName("ix_holds_queue");
        builder.HasIndex(x => new { x.ReaderId, x.Status }).HasDatabaseName("ix_holds_reader");

        // Một bạn đọc chỉ có một phiếu đang chờ (Waiting/Ready) cho một tài liệu. Tầng nghiệp vụ đã
        // kiểm luật này, nhưng ba lượt bấm "Đặt giữ" cùng lúc trên máy chủ thật (05/09/2026) vẫn lọt
        // hai phiếu — cùng lớp với ux_loans_item_dang_muon: chỉ máy chủ dữ liệu mới chặn được tranh chấp.
        builder.HasIndex(x => new { x.ReaderId, x.BibId })
            .IsUnique()
            .HasFilter("status IN ('Waiting', 'Ready') AND deleted_at IS NULL")
            .HasDatabaseName("ux_holds_reader_bib_dang_cho");
    }
}

public class FineConfiguration : IEntityTypeConfiguration<Fine>
{
    public void Configure(EntityTypeBuilder<Fine> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.PaidAmount).HasPrecision(18, 2);

        builder.HasOne(x => x.Reader).WithMany().HasForeignKey(x => x.ReaderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Loan).WithMany().HasForeignKey(x => x.LoanId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.Code).IsUnique().HasFilter("deleted_at IS NULL").HasDatabaseName("ux_fines_code");
        builder.HasIndex(x => new { x.ReaderId, x.PaidAt }).HasDatabaseName("ix_fines_reader");
    }
}

public class LockerConfiguration : IEntityTypeConfiguration<Locker>
{
    public void Configure(EntityTypeBuilder<Locker> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Area).HasMaxLength(200);
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("deleted_at IS NULL").HasDatabaseName("ux_lockers_code");
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_lockers_status");
    }
}

public class LockerUsageConfiguration : IEntityTypeConfiguration<LockerUsage>
{
    public void Configure(EntityTypeBuilder<LockerUsage> builder)
    {
        builder.HasOne(x => x.Locker).WithMany().HasForeignKey(x => x.LockerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Reader).WithMany().HasForeignKey(x => x.ReaderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.LockerId, x.CheckoutAt }).HasDatabaseName("ix_locker_usages_locker");
    }
}

public class LibraryVisitConfiguration : IEntityTypeConfiguration<LibraryVisit>
{
    public void Configure(EntityTypeBuilder<LibraryVisit> builder)
    {
        builder.Property(x => x.Gate).HasMaxLength(100);
        builder.HasOne(x => x.Reader).WithMany().HasForeignKey(x => x.ReaderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.CheckinAt).HasDatabaseName("ix_library_visits_checkin");
        builder.HasIndex(x => new { x.ReaderId, x.CheckinAt }).HasDatabaseName("ix_library_visits_reader");
    }
}

// ---------------- web ----------------

public class CmsPageConfiguration : IEntityTypeConfiguration<CmsPage>
{
    public void Configure(EntityTypeBuilder<CmsPage> builder)
    {
        builder.Property(x => x.Slug).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("deleted_at IS NULL").HasDatabaseName("ux_cms_pages_slug");
    }
}

public class CmsNewsCategoryConfiguration : CatalogEntityConfiguration<CmsNewsCategory> { }

public class CmsNewsConfiguration : IEntityTypeConfiguration<CmsNews>
{
    public void Configure(EntityTypeBuilder<CmsNews> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ThumbnailUrl).HasMaxLength(1000);

        builder.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("deleted_at IS NULL").HasDatabaseName("ux_cms_news_slug");
        builder.HasIndex(x => new { x.IsPublished, x.PublishedAt }).HasDatabaseName("ix_cms_news_published");
    }
}

public class CmsBannerConfiguration : IEntityTypeConfiguration<CmsBanner>
{
    public void Configure(EntityTypeBuilder<CmsBanner> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ImageUrl).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Position).HasMaxLength(50).IsRequired();
    }
}

public class CmsMenuConfiguration : IEntityTypeConfiguration<CmsMenu>
{
    public void Configure(EntityTypeBuilder<CmsMenu> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Url).HasMaxLength(1000);
        builder.HasIndex(x => x.ParentId).HasDatabaseName("ix_cms_menus_parent");
    }
}

public class CmsSettingConfiguration : IEntityTypeConfiguration<CmsSetting>
{
    public void Configure(EntityTypeBuilder<CmsSetting> builder)
    {
        builder.Property(x => x.Key).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.HasIndex(x => x.Key).IsUnique().HasDatabaseName("ux_cms_settings_key");
    }
}

public class CmsExternalLinkConfiguration : IEntityTypeConfiguration<CmsExternalLink>
{
    public void Configure(EntityTypeBuilder<CmsExternalLink> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Url).HasMaxLength(1000).IsRequired();
    }
}

public class CmsGalleryConfiguration : IEntityTypeConfiguration<CmsGallery>
{
    public void Configure(EntityTypeBuilder<CmsGallery> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
    }
}

public class CmsGalleryImageConfiguration : IEntityTypeConfiguration<CmsGalleryImage>
{
    public void Configure(EntityTypeBuilder<CmsGalleryImage> builder)
    {
        builder.Property(x => x.ImageUrl).HasMaxLength(1000).IsRequired();
        builder.HasOne(x => x.Gallery).WithMany(g => g.Images).HasForeignKey(x => x.GalleryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class OpacSearchLogConfiguration : IEntityTypeConfiguration<OpacSearchLog>
{
    public void Configure(EntityTypeBuilder<OpacSearchLog> builder)
    {
        builder.Property(x => x.Keyword).HasMaxLength(500).IsRequired();
        builder.Property(x => x.SearchType).HasMaxLength(50);
        builder.Property(x => x.Ip).HasMaxLength(64);
        builder.HasIndex(x => x.OccurredAt).IsDescending().HasDatabaseName("ix_opac_search_logs_occurred");
        builder.HasIndex(x => x.Keyword).HasDatabaseName("ix_opac_search_logs_keyword");
    }
}

public class OpacSavedSearchConfiguration : IEntityTypeConfiguration<OpacSavedSearch>
{
    public void Configure(EntityTypeBuilder<OpacSavedSearch> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Query).HasColumnType("jsonb").IsRequired();
        builder.HasOne(x => x.Reader).WithMany().HasForeignKey(x => x.ReaderId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class OpacFavoriteConfiguration : IEntityTypeConfiguration<OpacFavorite>
{
    public void Configure(EntityTypeBuilder<OpacFavorite> builder)
    {
        builder.HasOne(x => x.Reader).WithMany().HasForeignKey(x => x.ReaderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Bib).WithMany().HasForeignKey(x => x.BibId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.ReaderId, x.BibId }).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_opac_favorites");
    }
}

public class OpacReviewConfiguration : IEntityTypeConfiguration<OpacReview>
{
    public void Configure(EntityTypeBuilder<OpacReview> builder)
    {
        builder.HasOne(x => x.Reader).WithMany().HasForeignKey(x => x.ReaderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Bib).WithMany().HasForeignKey(x => x.BibId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.BibId, x.IsApproved }).HasDatabaseName("ix_opac_reviews_bib");
    }
}

// ---------------- ill ----------------

public class Z3950TargetConfiguration : IEntityTypeConfiguration<Z3950Target>
{
    public void Configure(EntityTypeBuilder<Z3950Target> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Host).HasMaxLength(300).IsRequired();
        builder.Property(x => x.DatabaseName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Charset).HasMaxLength(50).IsRequired();
        builder.Property(x => x.RecordSyntax).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SruBaseUrl).HasMaxLength(1000);
    }
}

public class Z3950SearchLogConfiguration : IEntityTypeConfiguration<Z3950SearchLog>
{
    public void Configure(EntityTypeBuilder<Z3950SearchLog> builder)
    {
        builder.Property(x => x.Query).HasMaxLength(1000).IsRequired();
        builder.HasOne(x => x.Target).WithMany().HasForeignKey(x => x.TargetId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => x.OccurredAt).IsDescending().HasDatabaseName("ix_z3950_logs_occurred");
    }
}

public class OaiRepositoryConfiguration : IEntityTypeConfiguration<OaiRepository>
{
    public void Configure(EntityTypeBuilder<OaiRepository> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.BaseUrl).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.MetadataPrefix).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ScheduleCron).HasMaxLength(100);
    }
}

public class OaiHarvestLogConfiguration : IEntityTypeConfiguration<OaiHarvestLog>
{
    public void Configure(EntityTypeBuilder<OaiHarvestLog> builder)
    {
        // Danh sách lỗi ở đây là văn bản cho cán bộ đọc, mỗi dòng một lỗi, chứ không phải dữ liệu
        // có cấu trúc. Khai jsonb thì PostgreSQL từ chối ngay lần thu hoạch đầu tiên gặp lỗi.
        builder.HasOne(x => x.Repository).WithMany().HasForeignKey(x => x.RepositoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ImportExportJobConfiguration : IEntityTypeConfiguration<ImportExportJob>
{
    public void Configure(EntityTypeBuilder<ImportExportJob> builder)
    {
        builder.Property(x => x.FileName).HasMaxLength(500);
        builder.Property(x => x.FilePath).HasMaxLength(1000);
        builder.Property(x => x.ResultFilePath).HasMaxLength(1000);
        builder.Property(x => x.Options).HasColumnType("jsonb");
        builder.Property(x => x.Errors).HasColumnType("jsonb");
        builder.HasIndex(x => new { x.Type, x.Status }).HasDatabaseName("ix_import_export_jobs_type");
    }
}

public class ImportMappingProfileConfiguration : IEntityTypeConfiguration<ImportMappingProfile>
{
    public void Configure(EntityTypeBuilder<ImportMappingProfile> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Target).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Mapping).HasColumnType("jsonb").IsRequired();
    }
}

public class ApiClientConfiguration : IEntityTypeConfiguration<ApiClient>
{
    public void Configure(EntityTypeBuilder<ApiClient> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ClientId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ClientSecretHash).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.ClientId).IsUnique().HasDatabaseName("ux_api_clients_client_id");
    }
}
