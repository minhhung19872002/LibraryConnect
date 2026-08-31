using LibraryConnect.Domain.Entities.Bib;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryConnect.Infrastructure.Persistence.Configurations;

public class BibRecordConfiguration : IEntityTypeConfiguration<BibRecord>
{
    public void Configure(EntityTypeBuilder<BibRecord> builder)
    {
        builder.Property(x => x.ControlNumber).HasMaxLength(50).IsRequired();
        // The whole MARC 21 record. jsonb (not json) so it can be indexed and queried by path.
        builder.Property(x => x.MarcData).HasColumnType("jsonb").IsRequired();

        builder.Property(x => x.Title).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Subtitle).HasMaxLength(2000);
        builder.Property(x => x.AuthorMain).HasMaxLength(500);
        builder.Property(x => x.UniformTitle).HasMaxLength(1000);
        builder.Property(x => x.Isbn).HasMaxLength(50);
        builder.Property(x => x.Issn).HasMaxLength(50);
        builder.Property(x => x.PublisherName).HasMaxLength(500);
        builder.Property(x => x.PublishPlace).HasMaxLength(300);
        builder.Property(x => x.Edition).HasMaxLength(200);
        builder.Property(x => x.Pages).HasMaxLength(200);
        builder.Property(x => x.Dimensions).HasMaxLength(100);
        builder.Property(x => x.Ddc).HasMaxLength(50);
        builder.Property(x => x.SeriesVolume).HasMaxLength(100);
        builder.Property(x => x.CoverImageUrl).HasMaxLength(1000);

        builder.HasIndex(x => x.ControlNumber).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_bib_control_number");
        builder.HasIndex(x => x.Isbn).HasDatabaseName("ix_bib_isbn");
        builder.HasIndex(x => x.Issn).HasDatabaseName("ix_bib_issn");
        builder.HasIndex(x => x.PublishYear).HasDatabaseName("ix_bib_publish_year");
        builder.HasIndex(x => x.Ddc).HasDatabaseName("ix_bib_ddc");
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_bib_status");
        builder.HasIndex(x => x.DocumentTypeId).HasDatabaseName("ix_bib_document_type");

        builder.HasOne(x => x.Publisher).WithMany().HasForeignKey(x => x.PublisherId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Language).WithMany().HasForeignKey(x => x.LanguageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DocumentType).WithMany().HasForeignKey(x => x.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CarrierType).WithMany().HasForeignKey(x => x.CarrierTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Series).WithMany().HasForeignKey(x => x.SeriesId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BibAuthorConfiguration : IEntityTypeConfiguration<BibAuthor>
{
    public void Configure(EntityTypeBuilder<BibAuthor> builder)
    {
        builder.Property(x => x.Role).HasMaxLength(200);
        builder.HasOne(x => x.Bib).WithMany(b => b.Authors).HasForeignKey(x => x.BibId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.BibId, x.AuthorId }).HasDatabaseName("ix_bib_authors");
        builder.HasIndex(x => x.AuthorId).HasDatabaseName("ix_bib_authors_author");
    }
}

public class BibSubjectConfiguration : IEntityTypeConfiguration<BibSubject>
{
    public void Configure(EntityTypeBuilder<BibSubject> builder)
    {
        builder.HasOne(x => x.Bib).WithMany(b => b.Subjects).HasForeignKey(x => x.BibId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.BibId, x.SubjectId }).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_bib_subjects");
    }
}

public class BibKeywordConfiguration : IEntityTypeConfiguration<BibKeyword>
{
    public void Configure(EntityTypeBuilder<BibKeyword> builder)
    {
        builder.HasOne(x => x.Bib).WithMany(b => b.Keywords).HasForeignKey(x => x.BibId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Keyword).WithMany().HasForeignKey(x => x.KeywordId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.BibId, x.KeywordId }).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_bib_keywords");
    }
}

public class BibClassificationConfiguration : IEntityTypeConfiguration<BibClassification>
{
    public void Configure(EntityTypeBuilder<BibClassification> builder)
    {
        builder.Property(x => x.Scheme).HasMaxLength(20).IsRequired();
        builder.HasOne(x => x.Bib).WithMany(b => b.Classifications).HasForeignKey(x => x.BibId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Classification).WithMany().HasForeignKey(x => x.ClassificationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.BibId, x.ClassificationId }).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_bib_classifications");
    }
}

public class BibCollectionConfiguration : IEntityTypeConfiguration<BibCollection>
{
    public void Configure(EntityTypeBuilder<BibCollection> builder)
    {
        builder.HasOne(x => x.Bib).WithMany(b => b.Collections).HasForeignKey(x => x.BibId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Collection).WithMany().HasForeignKey(x => x.CollectionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.BibId, x.CollectionId }).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_bib_collections");
    }
}

public class BibCourseConfiguration : IEntityTypeConfiguration<BibCourse>
{
    public void Configure(EntityTypeBuilder<BibCourse> builder)
    {
        builder.HasOne(x => x.Bib).WithMany(b => b.Courses).HasForeignKey(x => x.BibId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.BibId, x.CourseId }).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_bib_courses");
        builder.HasIndex(x => x.CourseId).HasDatabaseName("ix_bib_courses_course");
    }
}

public class BibRecordVersionConfiguration : IEntityTypeConfiguration<BibRecordVersion>
{
    public void Configure(EntityTypeBuilder<BibRecordVersion> builder)
    {
        builder.Property(x => x.MarcData).HasColumnType("jsonb").IsRequired();
        builder.HasOne(x => x.Bib).WithMany(b => b.Versions).HasForeignKey(x => x.BibId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.BibId, x.VersionNumber }).IsUnique().HasDatabaseName("ux_bib_versions");
    }
}

public class MarcFieldDefinitionConfiguration : IEntityTypeConfiguration<MarcFieldDefinition>
{
    public void Configure(EntityTypeBuilder<MarcFieldDefinition> builder)
    {
        builder.Property(x => x.Tag).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Indicators).HasColumnType("jsonb");
        builder.Property(x => x.Subfields).HasColumnType("jsonb");

        builder.HasIndex(x => x.Tag).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_marc_field_definitions_tag");
    }
}

public class MarcTemplateConfiguration : IEntityTypeConfiguration<MarcTemplate>
{
    public void Configure(EntityTypeBuilder<MarcTemplate> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Fields).HasColumnType("jsonb").IsRequired();

        builder.HasOne(x => x.DocumentType).WithMany().HasForeignKey(x => x.DocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_marc_templates_code");
    }
}

public class MarcFieldDefaultConfiguration : IEntityTypeConfiguration<MarcFieldDefault>
{
    public void Configure(EntityTypeBuilder<MarcFieldDefault> builder)
    {
        builder.Property(x => x.Tag).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Ind1).HasMaxLength(1);
        builder.Property(x => x.Ind2).HasMaxLength(1);
        builder.Property(x => x.Subfield).HasMaxLength(1);
        builder.Property(x => x.ParameterKey).HasMaxLength(150);

        builder.HasOne(x => x.DocumentType).WithMany().HasForeignKey(x => x.DocumentTypeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.DocumentTypeId, x.Tag }).HasDatabaseName("ix_marc_defaults_type_tag");
    }
}

public class CatalogQueueItemConfiguration : IEntityTypeConfiguration<CatalogQueueItem>
{
    public void Configure(EntityTypeBuilder<CatalogQueueItem> builder)
    {
        builder.HasOne(x => x.Bib).WithMany().HasForeignKey(x => x.BibId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.Status, x.Priority }).HasDatabaseName("ix_catalog_queue_status");
        builder.HasIndex(x => x.AssignedTo).HasDatabaseName("ix_catalog_queue_assignee");
    }
}

public class CardTemplateConfiguration : IEntityTypeConfiguration<CardTemplate>
{
    public void Configure(EntityTypeBuilder<CardTemplate> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.CardType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Layout).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_card_templates_code");
    }
}
