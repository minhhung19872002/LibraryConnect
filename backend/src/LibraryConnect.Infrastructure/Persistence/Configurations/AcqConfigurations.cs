using LibraryConnect.Domain.Entities.Acq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryConnect.Infrastructure.Persistence.Configurations;

public class LibraryConfiguration : CatalogEntityConfiguration<Library>
{
    public override void Configure(EntityTypeBuilder<Library> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.Phone).HasMaxLength(50);
    }
}

public class WarehouseConfiguration : CatalogEntityConfiguration<Warehouse>
{
    public override void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.CallNumberRule).HasMaxLength(300);
        builder.HasOne(x => x.Library).WithMany(l => l.Warehouses)
            .HasForeignKey(x => x.LibraryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ShelfConfiguration : CatalogEntityConfiguration<Shelf>
{
    public override void Configure(EntityTypeBuilder<Shelf> builder)
    {
        base.Configure(builder);
        builder.HasOne(x => x.Warehouse).WithMany(w => w.Shelves)
            .HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);

        // Mã giá chỉ duy nhất trong phạm vi một kho. Kho mở và kho đóng cùng có giá "A1" là cách
        // đánh giá quen thuộc của thư viện, nên ràng buộc duy nhất toàn hệ thống mà lớp danh mục
        // dựng sẵn phải được thay bằng ràng buộc theo kho.
        builder.Metadata.RemoveIndex(new[] { builder.Metadata.FindProperty(nameof(Shelf.Code))! });

        builder.HasIndex(x => new { x.WarehouseId, x.Code }).IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_shelf_warehouse_code");
    }
}

public class PurchaseRequestConfiguration : IEntityTypeConfiguration<PurchaseRequest>
{
    public void Configure(EntityTypeBuilder<PurchaseRequest> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.RequesterName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.ApprovedAmount).HasPrecision(18, 2);

        builder.HasOne(x => x.FundingSource).WithMany().HasForeignKey(x => x.FundingSourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Code).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_purchase_requests_code");
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_purchase_requests_status");
    }
}

public class PurchaseRequestItemConfiguration : IEntityTypeConfiguration<PurchaseRequestItem>
{
    public void Configure(EntityTypeBuilder<PurchaseRequestItem> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Property(x => x.EstimatedAmount).HasPrecision(18, 2);

        builder.HasOne(x => x.Request).WithMany(r => r.Items)
            .HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Bib).WithMany().HasForeignKey(x => x.BibId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ContractNo).HasMaxLength(100);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);

        builder.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.FundingSource).WithMany().HasForeignKey(x => x.FundingSourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Code).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_purchase_orders_code");
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_purchase_orders_status");
        builder.HasIndex(x => x.ExpectedDate).HasDatabaseName("ix_purchase_orders_expected");
    }
}

public class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);

        builder.HasOne(x => x.Order).WithMany(o => o.Items)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.RequestItem).WithMany().HasForeignKey(x => x.RequestItemId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Bib).WithMany().HasForeignKey(x => x.BibId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class HandoverRecordConfiguration : IEntityTypeConfiguration<HandoverRecord>
{
    public void Configure(EntityTypeBuilder<HandoverRecord> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.PartyA).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PartyB).HasMaxLength(500).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.FileUrl).HasMaxLength(1000);

        builder.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_handover_records_code");
    }
}

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.Property(x => x.Barcode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.RegisterNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CallNumber).HasMaxLength(200);
        builder.Property(x => x.Price).HasPrecision(18, 2);
        builder.Property(x => x.Condition).HasMaxLength(300);
        builder.Property(x => x.LockReason).HasMaxLength(500);
        builder.Property(x => x.VolumeNumber).HasMaxLength(100);

        builder.HasOne(x => x.Bib).WithMany().HasForeignKey(x => x.BibId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Shelf).WithMany().HasForeignKey(x => x.ShelfId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.FundingSource).WithMany().HasForeignKey(x => x.FundingSourceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.SetNull);

        // The barcode is what the desk scans: it must be unique among live copies.
        builder.HasIndex(x => x.Barcode).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_item_barcode");
        builder.HasIndex(x => x.RegisterNumber).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_item_register_number");
        builder.HasIndex(x => new { x.BibId, x.Status }).HasDatabaseName("ix_item_bib_status");
        builder.HasIndex(x => new { x.WarehouseId, x.Status }).HasDatabaseName("ix_item_warehouse_status");
        builder.HasIndex(x => x.CallNumber).HasDatabaseName("ix_item_call_number");
    }
}

public class ItemMovementConfiguration : IEntityTypeConfiguration<ItemMovement>
{
    public void Configure(EntityTypeBuilder<ItemMovement> builder)
    {
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.DecisionNo).HasMaxLength(100);
        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.BatchCode).HasMaxLength(50);
        builder.HasIndex(x => new { x.ItemId, x.MovementDate }).HasDatabaseName("ix_item_movements_item");
        builder.HasIndex(x => x.BatchCode).HasDatabaseName("ix_item_movements_batch");
    }
}

public class ItemDisposalConfiguration : IEntityTypeConfiguration<ItemDisposal>
{
    public void Configure(EntityTypeBuilder<ItemDisposal> builder)
    {
        builder.Property(x => x.DisposalType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DecisionNo).HasMaxLength(100);
        builder.Property(x => x.Value).HasPrecision(18, 2);
        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.DisposalDate).HasDatabaseName("ix_item_disposals_date");
    }
}

public class BarcodeTemplateConfiguration : IEntityTypeConfiguration<BarcodeTemplate>
{
    public void Configure(EntityTypeBuilder<BarcodeTemplate> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Layout).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_barcode_templates_code");
    }
}

public class LabelTemplateConfiguration : IEntityTypeConfiguration<LabelTemplate>
{
    public void Configure(EntityTypeBuilder<LabelTemplate> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Layout).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_label_templates_code");
    }
}

public class FormTemplateConfiguration : IEntityTypeConfiguration<FormTemplate>
{
    public void Configure(EntityTypeBuilder<FormTemplate> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.FormType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Layout).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_form_templates_code");
        builder.HasIndex(x => x.FormType).HasDatabaseName("ix_form_templates_type");
    }
}

public class InventoryPeriodConfiguration : IEntityTypeConfiguration<InventoryPeriod>
{
    public void Configure(EntityTypeBuilder<InventoryPeriod> builder)
    {
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ScopeType).HasMaxLength(30).IsRequired();

        builder.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ux_inventory_periods_code");
    }
}

public class InventoryScanConfiguration : IEntityTypeConfiguration<InventoryScan>
{
    public void Configure(EntityTypeBuilder<InventoryScan> builder)
    {
        builder.Property(x => x.Barcode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Device).HasMaxLength(100);
        builder.HasOne(x => x.Period).WithMany(p => p.Scans).HasForeignKey(x => x.PeriodId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => new { x.PeriodId, x.Barcode }).HasDatabaseName("ix_inventory_scans_period");
    }
}

public class InventoryResultConfiguration : IEntityTypeConfiguration<InventoryResult>
{
    public void Configure(EntityTypeBuilder<InventoryResult> builder)
    {
        builder.Property(x => x.Barcode).HasMaxLength(100).IsRequired();
        builder.HasOne(x => x.Period).WithMany(p => p.Results).HasForeignKey(x => x.PeriodId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => new { x.PeriodId, x.Result }).HasDatabaseName("ix_inventory_results_period");
    }
}
