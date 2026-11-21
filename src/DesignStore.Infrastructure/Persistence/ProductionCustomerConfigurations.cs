using DesignStore.Domain.Commerce;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DesignStore.Infrastructure.Persistence;

public sealed class ProductionJobMap : IEntityTypeConfiguration<ProductionJob>
{ public void Configure(EntityTypeBuilder<ProductionJob> b) { b.Property(x => x.Number).HasMaxLength(40); b.HasIndex(x => x.Number).IsUnique(); b.Property(x => x.ProductNameSnapshot).HasMaxLength(300); b.Property(x => x.SkuSnapshot).HasMaxLength(120); b.Property(x => x.ResponsibleUserId).HasMaxLength(450); b.ConcurrencyToken(); b.HasIndex(x => new { x.UserId, x.Status }); b.HasMany(x => x.StatusHistory).WithOne(x => x.ProductionJob).HasForeignKey(x => x.ProductionJobId).OnDelete(DeleteBehavior.Cascade); b.HasMany(x => x.Attachments).WithOne(x => x.ProductionJob).HasForeignKey(x => x.ProductionJobId).OnDelete(DeleteBehavior.Cascade); b.HasMany(x => x.Notes).WithOne(x => x.ProductionJob).HasForeignKey(x => x.ProductionJobId).OnDelete(DeleteBehavior.Cascade); } }
public sealed class ProductionJobStatusHistoryMap : IEntityTypeConfiguration<ProductionJobStatusHistory>
{ public void Configure(EntityTypeBuilder<ProductionJobStatusHistory> b) { b.Property(x => x.Actor).HasMaxLength(450); b.Property(x => x.Note).HasMaxLength(2000); } }
public sealed class ProductionJobAttachmentMap : IEntityTypeConfiguration<ProductionJobAttachment>
{ public void Configure(EntityTypeBuilder<ProductionJobAttachment> b) { b.Property(x => x.StorageKey).HasMaxLength(500); b.Property(x => x.OriginalName).HasMaxLength(255); b.Property(x => x.ContentType).HasMaxLength(100); b.Property(x => x.Sha256).HasMaxLength(64); } }
public sealed class ProductionJobNoteMap : IEntityTypeConfiguration<ProductionJobNote>
{ public void Configure(EntityTypeBuilder<ProductionJobNote> b) { b.Property(x => x.Body).HasMaxLength(4000); } }
public sealed class CustomerAddressMap : IEntityTypeConfiguration<CustomerAddress>
{ public void Configure(EntityTypeBuilder<CustomerAddress> b) { b.Property(x => x.Label).HasMaxLength(80); b.Property(x => x.FullName).HasMaxLength(200); b.Property(x => x.Company).HasMaxLength(200); b.Property(x => x.Street).HasMaxLength(250); b.Property(x => x.City).HasMaxLength(120); b.Property(x => x.PostalCode).HasMaxLength(20); b.Property(x => x.CountryCode).HasMaxLength(2); b.Property(x => x.Phone).HasMaxLength(40); b.HasIndex(x => x.UserId); } }
public sealed class FavoriteProductMap : IEntityTypeConfiguration<FavoriteProduct>
{ public void Configure(EntityTypeBuilder<FavoriteProduct> b) { b.HasIndex(x => new { x.UserId, x.ProductId }).IsUnique(); b.HasOne<DesignStore.Domain.Catalog.Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade); } }
