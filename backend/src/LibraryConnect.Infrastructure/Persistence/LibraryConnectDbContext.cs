using System.Linq.Expressions;
using LibraryConnect.Application.Common.Interfaces;
using LibraryConnect.Domain.Common;
using LibraryConnect.Domain.Entities.Acq;
using LibraryConnect.Domain.Entities.Bib;
using LibraryConnect.Domain.Entities.Cat;
using LibraryConnect.Domain.Entities.Cir;
using LibraryConnect.Domain.Entities.Dig;
using LibraryConnect.Domain.Entities.Ill;
using LibraryConnect.Domain.Entities.Rdr;
using LibraryConnect.Domain.Entities.Ser;
using LibraryConnect.Domain.Entities.Sys;
using LibraryConnect.Domain.Entities.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryConnect.Infrastructure.Persistence;

/// <summary>
/// The single EF Core context. Schemas mirror the functional grouping of section 4 and are derived
/// from the entity namespace, so adding an entity to <c>Domain.Entities.Cir</c> automatically places
/// it in the <c>cir</c> schema.
/// </summary>
public class LibraryConnectDbContext : DbContext, IApplicationDbContext
{
    /// <summary>Tables whose plural form the naive pluraliser gets wrong or that the spec names differently.</summary>
    private static readonly Dictionary<string, string> TableNameOverrides = new()
    {
        [nameof(Shelf)] = "shelves",
        [nameof(Series)] = "series",
        [nameof(CmsNews)] = "cms_news",
        [nameof(CatalogQueueItem)] = "catalog_queue"
    };

    public LibraryConnectDbContext(DbContextOptions<LibraryConnectDbContext> options) : base(options) { }

    // ---- sys ----
    public DbSet<User> Users => Set<User>();
    public DbSet<UserGroup> UserGroups => Set<UserGroup>();
    public DbSet<UserGroupMember> UserGroupMembers => Set<UserGroupMember>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<GroupPermission> GroupPermissions => Set<GroupPermission>();
    public DbSet<UserDataScope> UserDataScopes => Set<UserDataScope>();
    public DbSet<SystemParameter> SystemParameters => Set<SystemParameter>();
    public DbSet<SystemParameterHistory> SystemParameterHistories => Set<SystemParameterHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AuditSetting> AuditSettings => Set<AuditSetting>();
    public DbSet<BackupJob> BackupJobs => Set<BackupJob>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();
    public DbSet<CodeSequence> CodeSequences => Set<CodeSequence>();

    // ---- cat ----
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<CarrierType> CarrierTypes => Set<CarrierType>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Publisher> Publishers => Set<Publisher>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Keyword> Keywords => Set<Keyword>();
    public DbSet<Classification> Classifications => Set<Classification>();
    public DbSet<Series> SeriesList => Set<Series>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<ReaderType> ReaderTypes => Set<ReaderType>();
    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<Major> Majors => Set<Major>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseMajor> CourseMajors => Set<CourseMajor>();
    public DbSet<Cohort> Cohorts => Set<Cohort>();
    public DbSet<StudentClass> StudentClasses => Set<StudentClass>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<FundingSource> FundingSources => Set<FundingSource>();
    public DbSet<ViolationType> ViolationTypes => Set<ViolationType>();
    public DbSet<CustomIndex> CustomIndexes => Set<CustomIndex>();
    public DbSet<CustomIndexValue> CustomIndexValues => Set<CustomIndexValue>();
    public DbSet<CustomIndexLink> CustomIndexLinks => Set<CustomIndexLink>();
    public DbSet<Holiday> Holidays => Set<Holiday>();

    // ---- bib ----
    public DbSet<BibRecord> BibRecords => Set<BibRecord>();
    public DbSet<BibAuthor> BibAuthors => Set<BibAuthor>();
    public DbSet<BibSubject> BibSubjects => Set<BibSubject>();
    public DbSet<BibKeyword> BibKeywords => Set<BibKeyword>();
    public DbSet<BibClassification> BibClassifications => Set<BibClassification>();
    public DbSet<BibCollection> BibCollections => Set<BibCollection>();
    public DbSet<BibCourse> BibCourses => Set<BibCourse>();
    public DbSet<BibRecordVersion> BibRecordVersions => Set<BibRecordVersion>();
    public DbSet<MarcFieldDefinition> MarcFieldDefinitions => Set<MarcFieldDefinition>();
    public DbSet<MarcTemplate> MarcTemplates => Set<MarcTemplate>();
    public DbSet<MarcFieldDefault> MarcFieldDefaults => Set<MarcFieldDefault>();
    public DbSet<CatalogQueueItem> CatalogQueue => Set<CatalogQueueItem>();
    public DbSet<CardTemplate> CardTemplates => Set<CardTemplate>();

    // ---- acq ----
    public DbSet<Library> Libraries => Set<Library>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Shelf> Shelves => Set<Shelf>();
    public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();
    public DbSet<PurchaseRequestItem> PurchaseRequestItems => Set<PurchaseRequestItem>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<HandoverRecord> HandoverRecords => Set<HandoverRecord>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemMovement> ItemMovements => Set<ItemMovement>();
    public DbSet<ItemDisposal> ItemDisposals => Set<ItemDisposal>();
    public DbSet<BarcodeTemplate> BarcodeTemplates => Set<BarcodeTemplate>();
    public DbSet<LabelTemplate> LabelTemplates => Set<LabelTemplate>();
    public DbSet<FormTemplate> FormTemplates => Set<FormTemplate>();
    public DbSet<InventoryPeriod> InventoryPeriods => Set<InventoryPeriod>();
    public DbSet<InventoryScan> InventoryScans => Set<InventoryScan>();
    public DbSet<InventoryResult> InventoryResults => Set<InventoryResult>();

    // ---- ser ----
    public DbSet<Serial> Serials => Set<Serial>();
    public DbSet<SerialIssue> SerialIssues => Set<SerialIssue>();
    public DbSet<SerialIssueArticle> SerialIssueArticles => Set<SerialIssueArticle>();
    public DbSet<SerialBinding> SerialBindings => Set<SerialBinding>();
    public DbSet<SerialClaim> SerialClaims => Set<SerialClaim>();

    // ---- dig ----
    public DbSet<DigitalCollection> DigitalCollections => Set<DigitalCollection>();
    public DbSet<DigitalDocument> DigitalDocuments => Set<DigitalDocument>();
    public DbSet<DigitalDocumentFile> DigitalDocumentFiles => Set<DigitalDocumentFile>();
    public DbSet<DigitalAccessRequest> DigitalAccessRequests => Set<DigitalAccessRequest>();
    public DbSet<DigitalAccessLog> DigitalAccessLogs => Set<DigitalAccessLog>();
    public DbSet<DigitalUploadSession> DigitalUploadSessions => Set<DigitalUploadSession>();

    // ---- rdr ----
    public DbSet<Reader> Readers => Set<Reader>();
    public DbSet<ReaderCard> ReaderCards => Set<ReaderCard>();
    public DbSet<ReaderCardTemplate> ReaderCardTemplates => Set<ReaderCardTemplate>();
    public DbSet<ReaderImportBatch> ReaderImportBatches => Set<ReaderImportBatch>();
    public DbSet<ReaderViolation> ReaderViolations => Set<ReaderViolation>();
    public DbSet<CardRenewalRequest> CardRenewalRequests => Set<CardRenewalRequest>();

    // ---- cir ----
    public DbSet<CirculationPolicy> CirculationPolicies => Set<CirculationPolicy>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanRenewal> LoanRenewals => Set<LoanRenewal>();
    public DbSet<Hold> Holds => Set<Hold>();
    public DbSet<Fine> Fines => Set<Fine>();
    public DbSet<Locker> Lockers => Set<Locker>();
    public DbSet<LockerUsage> LockerUsages => Set<LockerUsage>();
    public DbSet<LibraryVisit> LibraryVisits => Set<LibraryVisit>();

    // ---- web ----
    public DbSet<CmsPage> CmsPages => Set<CmsPage>();
    public DbSet<CmsNews> CmsNews => Set<CmsNews>();
    public DbSet<CmsNewsCategory> CmsNewsCategories => Set<CmsNewsCategory>();
    public DbSet<CmsBanner> CmsBanners => Set<CmsBanner>();
    public DbSet<CmsMenu> CmsMenus => Set<CmsMenu>();
    public DbSet<CmsSetting> CmsSettings => Set<CmsSetting>();
    public DbSet<CmsExternalLink> CmsExternalLinks => Set<CmsExternalLink>();
    public DbSet<CmsGallery> CmsGalleries => Set<CmsGallery>();
    public DbSet<CmsGalleryImage> CmsGalleryImages => Set<CmsGalleryImage>();
    public DbSet<OpacSearchLog> OpacSearchLogs => Set<OpacSearchLog>();
    public DbSet<OpacSavedSearch> OpacSavedSearches => Set<OpacSavedSearch>();
    public DbSet<OpacFavorite> OpacFavorites => Set<OpacFavorite>();
    public DbSet<OpacReview> OpacReviews => Set<OpacReview>();

    // ---- ill ----
    public DbSet<Z3950Target> Z3950Targets => Set<Z3950Target>();
    public DbSet<Z3950SearchLog> Z3950SearchLogs => Set<Z3950SearchLog>();
    public DbSet<OaiRepository> OaiRepositories => Set<OaiRepository>();
    public DbSet<OaiHarvestLog> OaiHarvestLogs => Set<OaiHarvestLog>();
    public DbSet<ImportExportJob> ImportExportJobs => Set<ImportExportJob>();
    public DbSet<ImportMappingProfile> ImportMappingProfiles => Set<ImportMappingProfile>();
    public DbSet<ApiClient> ApiClients => Set<ApiClient>();

    public override Task<int> SaveChangesAsync(CancellationToken ct = default) => base.SaveChangesAsync(ct);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("unaccent");
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryConnectDbContext).Assembly);

        // Lets a LINQ query call bib.vn_unaccent, so accent-insensitive searching runs in the
        // database against the pg_trgm indexes rather than by loading rows into memory.
        modelBuilder
            .HasDbFunction(typeof(Application.Common.Extensions.DatabaseFunctions)
                .GetMethod(nameof(Application.Common.Extensions.DatabaseFunctions.Unaccent), new[] { typeof(string) })!)
            .HasName("vn_unaccent")
            .HasSchema("bib");

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            ApplyTableNaming(entityType);
            ApplyColumnNaming(entityType);
            ApplyEnumConversions(modelBuilder, entityType);
            ApplyInstantConversions(entityType);
            ApplySoftDeleteFilter(modelBuilder, clrType);
        }
    }

    /// <summary>Places every table in the schema matching the last segment of its namespace.</summary>
    private static void ApplyTableNaming(Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType)
    {
        if (entityType.GetTableName() is null)
        {
            return;
        }

        var clrType = entityType.ClrType;
        var schema = (clrType.Namespace?.Split('.').LastOrDefault() ?? "public").ToLowerInvariant();

        var table = TableNameOverrides.TryGetValue(clrType.Name, out var overridden)
            ? overridden
            : NamingConventions.TableName(clrType.Name);

        entityType.SetTableName(table);
        entityType.SetSchema(schema);
    }

    private static void ApplyColumnNaming(Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType)
    {
        foreach (var property in entityType.GetProperties())
        {
            property.SetColumnName(NamingConventions.ToSnakeCase(property.Name));
        }

        foreach (var key in entityType.GetKeys())
        {
            key.SetName(NamingConventions.ToSnakeCase(key.GetName() ?? string.Empty));
        }

        foreach (var index in entityType.GetIndexes())
        {
            index.SetDatabaseName(NamingConventions.ToSnakeCase(index.GetDatabaseName() ?? string.Empty));
        }
    }

    /// <summary>
    /// Enums are stored as text rather than as integers: the database stays readable for the DBA and
    /// adding a member never shifts the meaning of existing rows.
    /// </summary>
    private static void ApplyEnumConversions(ModelBuilder modelBuilder, Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType)
    {
        foreach (var property in entityType.GetProperties())
        {
            var propertyType = property.ClrType;
            var underlying = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            if (!underlying.IsEnum)
            {
                continue;
            }

            var converterType = typeof(Microsoft.EntityFrameworkCore.Storage.ValueConversion.EnumToStringConverter<>)
                .MakeGenericType(underlying);

            property.SetValueConverter((Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter?)
                Activator.CreateInstance(converterType, new object?[] { null }));
            property.SetMaxLength(64);
        }
    }

    /// <summary>
    /// Normalises every <see cref="DateTimeOffset"/> to UTC before it reaches PostgreSQL.
    ///
    /// <c>timestamptz</c> stores an instant, and Npgsql refuses to write a value whose offset is not
    /// zero. The application deliberately works in Asia/Ho_Chi_Minh local time (a due date belongs to
    /// a Vietnamese calendar day), so the conversion happens here rather than forcing every call site
    /// to remember <c>ToUniversalTime()</c>. Reading back yields the same instant; the connection
    /// string pins the session timezone so it renders as local time in SQL tooling.
    /// </summary>
    private static void ApplyInstantConversions(Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType)
    {
        foreach (var property in entityType.GetProperties())
        {
            if (property.ClrType == typeof(DateTimeOffset))
            {
                property.SetValueConverter(UtcDateTimeOffsetConverter);
            }
            else if (property.ClrType == typeof(DateTimeOffset?))
            {
                property.SetValueConverter(NullableUtcDateTimeOffsetConverter);
            }
        }
    }

    private static readonly Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset, DateTimeOffset>
        UtcDateTimeOffsetConverter = new(
            value => value.ToUniversalTime(),
            value => value);

    private static readonly Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset?, DateTimeOffset?>
        NullableUtcDateTimeOffsetConverter = new(
            value => value.HasValue ? value.Value.ToUniversalTime() : value,
            value => value);

    /// <summary>
    /// Nothing is ever hard-deleted (E-HSMT: data must be retained permanently), so every query
    /// transparently excludes soft-deleted rows.
    /// </summary>
    private static void ApplySoftDeleteFilter(ModelBuilder modelBuilder, Type clrType)
    {
        if (!typeof(BaseEntity).IsAssignableFrom(clrType))
        {
            return;
        }

        var parameter = Expression.Parameter(clrType, "e");
        var property = Expression.Property(parameter, nameof(BaseEntity.DeletedAt));
        var condition = Expression.Equal(property, Expression.Constant(null, typeof(DateTimeOffset?)));

        modelBuilder.Entity(clrType).HasQueryFilter(Expression.Lambda(condition, parameter));
    }
}
