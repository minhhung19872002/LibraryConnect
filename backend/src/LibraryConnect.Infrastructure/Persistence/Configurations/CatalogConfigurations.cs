using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Entities.Cat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryConnect.Infrastructure.Persistence.Configurations;

/// <summary>
/// Shared shape for every lookup table: a unique code among live rows plus an index on the name for
/// the type-ahead pickers used all over the admin UI.
/// </summary>
public abstract class CatalogEntityConfiguration<T> : IEntityTypeConfiguration<T> where T : CatalogEntity
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(500).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(500);

        var table = builder.Metadata.ClrType.Name.ToLowerInvariant();

        builder.HasIndex(x => x.Code).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName($"ux_{table}_code");
        builder.HasIndex(x => x.Name).HasDatabaseName($"ix_{table}_name");
    }
}

public abstract class HierarchicalCatalogConfiguration<T> : CatalogEntityConfiguration<T>
    where T : HierarchicalCatalogEntity
{
    public override void Configure(EntityTypeBuilder<T> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Path).HasMaxLength(1000);
        var table = builder.Metadata.ClrType.Name.ToLowerInvariant();
        builder.HasIndex(x => x.ParentId).HasDatabaseName($"ix_{table}_parent");
        builder.HasIndex(x => x.Path).HasDatabaseName($"ix_{table}_path");
    }
}

public class DocumentTypeConfiguration : CatalogEntityConfiguration<DocumentType> { }
public class CarrierTypeConfiguration : CatalogEntityConfiguration<CarrierType> { }
public class LanguageConfiguration : CatalogEntityConfiguration<Language> { }
public class CountryConfiguration : CatalogEntityConfiguration<Country> { }
public class PublisherConfiguration : CatalogEntityConfiguration<Publisher> { }
public class KeywordConfiguration : CatalogEntityConfiguration<Keyword> { }
public class SeriesConfiguration : CatalogEntityConfiguration<Series> { }
public class ReaderTypeConfiguration : CatalogEntityConfiguration<ReaderType> { }
public class FacultyConfiguration : CatalogEntityConfiguration<Faculty> { }
public class SupplierConfiguration : CatalogEntityConfiguration<Supplier> { }
public class FundingSourceConfiguration : CatalogEntityConfiguration<FundingSource> { }
public class ViolationTypeConfiguration : CatalogEntityConfiguration<ViolationType> { }
public class SubjectConfiguration : HierarchicalCatalogConfiguration<Subject> { }
public class CollectionConfiguration : HierarchicalCatalogConfiguration<Collection> { }

public class AuthorConfiguration : CatalogEntityConfiguration<Author>
{
    public override void Configure(EntityTypeBuilder<Author> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.FullName).HasMaxLength(500).IsRequired();
        builder.Property(x => x.SortName).HasMaxLength(500);
        // Authors are the most searched authority file; a trigram index is added in the migration
        // so accent-insensitive partial matching stays fast.
        builder.HasIndex(x => x.FullName).HasDatabaseName("ix_author_full_name");
    }
}

public class ClassificationConfiguration : HierarchicalCatalogConfiguration<Classification>
{
    public override void Configure(EntityTypeBuilder<Classification> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Scheme).HasMaxLength(20).IsRequired();
        // The same notation exists in several schemes (DDC 004 and BBK 004 are unrelated), so the
        // uniqueness of Code from the base configuration has to be widened to include the scheme.
        builder.Metadata.RemoveIndex(new[] { builder.Metadata.FindProperty(nameof(Classification.Code))! });
        builder.HasIndex(x => new { x.Scheme, x.Code }).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_classification_scheme_code");
    }
}

public class MajorConfiguration : CatalogEntityConfiguration<Major>
{
    public override void Configure(EntityTypeBuilder<Major> builder)
    {
        base.Configure(builder);
        builder.HasOne(x => x.Faculty).WithMany().HasForeignKey(x => x.FacultyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CourseConfiguration : CatalogEntityConfiguration<Course>
{
    public override void Configure(EntityTypeBuilder<Course> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Lecturer).HasMaxLength(300);
        builder.Property(x => x.Semester).HasMaxLength(50);
    }
}

public class CourseMajorConfiguration : IEntityTypeConfiguration<CourseMajor>
{
    public void Configure(EntityTypeBuilder<CourseMajor> builder)
    {
        builder.HasOne(x => x.Course).WithMany(c => c.Majors)
            .HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Major).WithMany()
            .HasForeignKey(x => x.MajorId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.CourseId, x.MajorId }).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_course_majors");
    }
}

public class CustomIndexConfiguration : IEntityTypeConfiguration<CustomIndex>
{
    public void Configure(EntityTypeBuilder<CustomIndex> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.MarcTag).HasMaxLength(3).IsRequired();
        builder.Property(x => x.MarcSubfield).HasMaxLength(1).IsRequired();

        builder.HasIndex(x => x.Code).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_custom_indexes_code");
    }
}

public class CustomIndexValueConfiguration : IEntityTypeConfiguration<CustomIndexValue>
{
    public void Configure(EntityTypeBuilder<CustomIndexValue> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(500).IsRequired();

        builder.HasOne(x => x.CustomIndex).WithMany(c => c.Values)
            .HasForeignKey(x => x.CustomIndexId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.CustomIndexId, x.Code }).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_custom_index_values");
    }
}

public class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.HasIndex(x => new { x.FromDate, x.ToDate }).HasDatabaseName("ix_holidays_range");
    }
}
