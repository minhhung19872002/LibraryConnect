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

namespace LibraryConnect.Application.Common.Interfaces;

/// <summary>
/// The persistence surface visible to use-case handlers. Keeping it as an interface lets the
/// Application layer stay free of Npgsql specifics and lets tests swap in an in-memory context.
/// </summary>
public interface IApplicationDbContext
{
    // ---- sys ----
    DbSet<User> Users { get; }
    DbSet<UserGroup> UserGroups { get; }
    DbSet<UserGroupMember> UserGroupMembers { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<GroupPermission> GroupPermissions { get; }
    DbSet<UserDataScope> UserDataScopes { get; }
    DbSet<SystemParameter> SystemParameters { get; }
    DbSet<SystemParameterHistory> SystemParameterHistories { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<AuditSetting> AuditSettings { get; }
    DbSet<BackupJob> BackupJobs { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<DeviceToken> DeviceTokens { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<LoginHistory> LoginHistories { get; }

    // ---- cat ----
    DbSet<DocumentType> DocumentTypes { get; }
    DbSet<CarrierType> CarrierTypes { get; }
    DbSet<Language> Languages { get; }
    DbSet<Country> Countries { get; }
    DbSet<Publisher> Publishers { get; }
    DbSet<Author> Authors { get; }
    DbSet<Subject> Subjects { get; }
    DbSet<Keyword> Keywords { get; }
    DbSet<Classification> Classifications { get; }
    DbSet<Series> SeriesList { get; }
    DbSet<Collection> Collections { get; }
    DbSet<ReaderType> ReaderTypes { get; }
    DbSet<Faculty> Faculties { get; }
    DbSet<Major> Majors { get; }
    DbSet<Course> Courses { get; }
    DbSet<CourseMajor> CourseMajors { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<FundingSource> FundingSources { get; }
    DbSet<ViolationType> ViolationTypes { get; }
    DbSet<CustomIndex> CustomIndexes { get; }
    DbSet<CustomIndexValue> CustomIndexValues { get; }
    DbSet<Holiday> Holidays { get; }

    // ---- bib ----
    DbSet<BibRecord> BibRecords { get; }
    DbSet<BibAuthor> BibAuthors { get; }
    DbSet<BibSubject> BibSubjects { get; }
    DbSet<BibKeyword> BibKeywords { get; }
    DbSet<BibClassification> BibClassifications { get; }
    DbSet<BibCollection> BibCollections { get; }
    DbSet<BibCourse> BibCourses { get; }
    DbSet<BibRecordVersion> BibRecordVersions { get; }
    DbSet<MarcFieldDefinition> MarcFieldDefinitions { get; }
    DbSet<MarcTemplate> MarcTemplates { get; }
    DbSet<MarcFieldDefault> MarcFieldDefaults { get; }
    DbSet<CatalogQueueItem> CatalogQueue { get; }
    DbSet<CardTemplate> CardTemplates { get; }

    // ---- acq ----
    DbSet<Library> Libraries { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<Shelf> Shelves { get; }
    DbSet<PurchaseRequest> PurchaseRequests { get; }
    DbSet<PurchaseRequestItem> PurchaseRequestItems { get; }
    DbSet<PurchaseOrder> PurchaseOrders { get; }
    DbSet<PurchaseOrderItem> PurchaseOrderItems { get; }
    DbSet<HandoverRecord> HandoverRecords { get; }
    DbSet<Item> Items { get; }
    DbSet<ItemMovement> ItemMovements { get; }
    DbSet<ItemDisposal> ItemDisposals { get; }
    DbSet<BarcodeTemplate> BarcodeTemplates { get; }
    DbSet<LabelTemplate> LabelTemplates { get; }
    DbSet<FormTemplate> FormTemplates { get; }
    DbSet<InventoryPeriod> InventoryPeriods { get; }
    DbSet<InventoryScan> InventoryScans { get; }
    DbSet<InventoryResult> InventoryResults { get; }

    // ---- ser ----
    DbSet<Serial> Serials { get; }
    DbSet<SerialIssue> SerialIssues { get; }
    DbSet<SerialIssueArticle> SerialIssueArticles { get; }
    DbSet<SerialBinding> SerialBindings { get; }
    DbSet<SerialClaim> SerialClaims { get; }

    // ---- dig ----
    DbSet<DigitalCollection> DigitalCollections { get; }
    DbSet<DigitalDocument> DigitalDocuments { get; }
    DbSet<DigitalDocumentFile> DigitalDocumentFiles { get; }
    DbSet<DigitalAccessRequest> DigitalAccessRequests { get; }
    DbSet<DigitalAccessLog> DigitalAccessLogs { get; }

    // ---- rdr ----
    DbSet<Reader> Readers { get; }
    DbSet<ReaderCard> ReaderCards { get; }
    DbSet<ReaderCardTemplate> ReaderCardTemplates { get; }
    DbSet<ReaderImportBatch> ReaderImportBatches { get; }
    DbSet<ReaderViolation> ReaderViolations { get; }
    DbSet<CardRenewalRequest> CardRenewalRequests { get; }

    // ---- cir ----
    DbSet<CirculationPolicy> CirculationPolicies { get; }
    DbSet<Loan> Loans { get; }
    DbSet<LoanRenewal> LoanRenewals { get; }
    DbSet<Hold> Holds { get; }
    DbSet<Fine> Fines { get; }
    DbSet<Locker> Lockers { get; }
    DbSet<LockerUsage> LockerUsages { get; }
    DbSet<LibraryVisit> LibraryVisits { get; }

    // ---- web ----
    DbSet<CmsPage> CmsPages { get; }
    DbSet<CmsNews> CmsNews { get; }
    DbSet<CmsNewsCategory> CmsNewsCategories { get; }
    DbSet<CmsBanner> CmsBanners { get; }
    DbSet<CmsMenu> CmsMenus { get; }
    DbSet<CmsSetting> CmsSettings { get; }
    DbSet<CmsExternalLink> CmsExternalLinks { get; }
    DbSet<CmsGallery> CmsGalleries { get; }
    DbSet<CmsGalleryImage> CmsGalleryImages { get; }
    DbSet<OpacSearchLog> OpacSearchLogs { get; }
    DbSet<OpacSavedSearch> OpacSavedSearches { get; }
    DbSet<OpacFavorite> OpacFavorites { get; }
    DbSet<OpacReview> OpacReviews { get; }

    // ---- ill ----
    DbSet<Z3950Target> Z3950Targets { get; }
    DbSet<Z3950SearchLog> Z3950SearchLogs { get; }
    DbSet<OaiRepository> OaiRepositories { get; }
    DbSet<OaiHarvestLog> OaiHarvestLogs { get; }
    DbSet<ImportExportJob> ImportExportJobs { get; }
    DbSet<ImportMappingProfile> ImportMappingProfiles { get; }
    DbSet<ApiClient> ApiClients { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Escape hatch for the few places that need raw SQL (full-text search, statistics).</summary>
    Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }
}
