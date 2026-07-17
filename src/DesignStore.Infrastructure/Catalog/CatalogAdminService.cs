using DesignStore.Application.Catalog;
using DesignStore.Domain.Catalog;
using DesignStore.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DesignStore.Infrastructure.Catalog;

public sealed class CatalogAdminService(ApplicationDbContext db, IValidator<EditProductRequest> productValidator,
    IValidator<EditCategoryRequest> categoryValidator) : ICatalogAdminService
{
    public async Task<IReadOnlyList<AdminProductListItem>> GetProductsAsync(CancellationToken ct) => await db.Products.AsNoTracking()
        .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Sku)
        .Select(p => new AdminProductListItem(p.Id, p.Sku, p.Translations.Where(t => t.Culture == "cs-CZ").Select(t => t.Name).FirstOrDefault() ?? p.Translations.Select(t => t.Name).First(), p.BasePrice, p.AvailabilityStatus, p.IsPublished, p.StockQuantity, p.HasVariants)).ToListAsync(ct);

    public async Task<EditProductRequest?> GetProductAsync(Guid id, CancellationToken ct)
    {
        var p = await ProductAggregate().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return null;
        return new EditProductRequest
        {
            Id = p.Id, Sku = p.Sku, CategoryId = p.CategoryId, CollectionId = p.CollectionId, BasePrice = p.BasePrice,
            StartingPrice = p.StartingPrice, VatRate = p.VatRate, AvailabilityStatus = p.AvailabilityStatus,
            TrackInventory = p.TrackInventory, StockQuantity = p.StockQuantity, CanBeMadeToOrder = p.CanBeMadeToOrder,
            AutomaticallySwitchToMadeToOrder = p.AutomaticallySwitchToMadeToOrder, EstimatedProductionDays = p.EstimatedProductionDays,
            IsPublished = p.IsPublished, IsFeatured = p.IsFeatured, IsNew = p.IsNew, IsLimited = p.IsLimited,
            HasVariants = p.HasVariants, DisplayOrder = p.DisplayOrder, MaterialIds = p.ProductMaterials.Select(x => x.MaterialId).ToList(),
            Translations = p.Translations.OrderBy(t => t.Culture).Select(t => new ProductTranslationInput { Culture=t.Culture, Name=t.Name, Slug=t.Slug, ShortDescription=t.ShortDescription, LongDescription=t.LongDescription, Story=t.Story, Materials=t.Materials, Dimensions=t.Dimensions, CareInstructions=t.CareInstructions, ShippingInformation=t.ShippingInformation, SeoTitle=t.SeoTitle, SeoDescription=t.SeoDescription, OpenGraphTitle=t.OpenGraphTitle, OpenGraphDescription=t.OpenGraphDescription }).ToList(),
            Variants = p.Variants.OrderBy(v => v.Sku).Select(v => new ProductVariantInput { Id=v.Id, Sku=v.Sku, Name=v.Translations.FirstOrDefault(t=>t.Culture=="cs-CZ")?.Name??v.Name, NameEn=v.Translations.FirstOrDefault(t=>t.Culture=="en")?.Name??string.Empty, NameDe=v.Translations.FirstOrDefault(t=>t.Culture=="de")?.Name??string.Empty, Price=v.Price, AvailabilityStatus=v.AvailabilityStatus, StockQuantity=v.StockQuantity, CanBeMadeToOrder=v.CanBeMadeToOrder, EstimatedProductionDays=v.EstimatedProductionDays }).ToList(),
            Options = p.Options.OrderBy(o => o.DisplayOrder).Select(o => new ProductOptionInput { Id=o.Id, Name=o.Translations.FirstOrDefault(t=>t.Culture=="cs-CZ")?.Name ?? o.Translations.First().Name, Type=o.Type, IsRequired=o.IsRequired, MaximumTextLength=o.MaximumTextLength, Values=o.Values.OrderBy(v=>v.DisplayOrder).Select(v=>new ProductOptionValueInput { Id=v.Id, Name=v.Translations.FirstOrDefault(t=>t.Culture=="cs-CZ")?.Name ?? v.Translations.First().Name, PriceAdjustment=v.PriceAdjustment, AdditionalProductionDays=v.AdditionalProductionDays, IsActive=v.IsActive }).ToList() }).ToList()
        };
    }

    public async Task<Guid> SaveProductAsync(EditProductRequest request, string? actor, CancellationToken ct)
    {
        await productValidator.ValidateAndThrowAsync(request, ct);
        var sku = request.Sku.Trim().ToUpperInvariant();
        if (await db.Products.AnyAsync(p => p.Sku == sku && p.Id != request.Id, ct) ||
            await db.ProductVariants.AnyAsync(v => v.Sku == sku && v.ProductId != request.Id, ct)) throw new ValidationException("SKU již existuje.");
        foreach (var t in request.Translations)
            if (await db.ProductTranslations.AnyAsync(x => x.Culture == t.Culture && x.Slug == t.Slug && x.ProductId != request.Id, ct) || await db.ProductSlugHistories.AnyAsync(x => x.Culture == t.Culture && x.OldSlug == t.Slug && x.ProductId != request.Id, ct)) throw new ValidationException($"Slug '{t.Slug}' již existuje.");

        Product product;
        if (request.Id.HasValue) product = await ProductAggregate().FirstOrDefaultAsync(p => p.Id == request.Id.Value, ct) ?? throw new KeyNotFoundException("Produkt nebyl nalezen.");
        else { product = new Product { CreatedBy = actor }; db.Products.Add(product); }
        product.Sku=sku; product.CategoryId=request.CategoryId; product.CollectionId=request.CollectionId; product.BasePrice=request.BasePrice; product.StartingPrice=request.StartingPrice; product.VatRate=request.VatRate; product.AvailabilityStatus=request.AvailabilityStatus; product.TrackInventory=request.TrackInventory; product.StockQuantity=request.HasVariants ? 0 : request.StockQuantity; product.CanBeMadeToOrder=request.CanBeMadeToOrder; product.AutomaticallySwitchToMadeToOrder=request.AutomaticallySwitchToMadeToOrder; product.EstimatedProductionDays=request.EstimatedProductionDays; product.IsPublished=request.IsPublished; product.IsFeatured=request.IsFeatured; product.IsNew=request.IsNew; product.IsLimited=request.IsLimited; product.HasVariants=request.HasVariants; product.DisplayOrder=request.DisplayOrder; product.UpdatedAtUtc=DateTime.UtcNow; product.UpdatedBy=actor;
        SyncTranslations(product, request.Translations, actor);
        SyncVariants(product, request.Variants, request.HasVariants, actor);
        SyncOptions(product, request.Options, actor);
        product.ProductMaterials.RemoveAll(x => !request.MaterialIds.Contains(x.MaterialId));
        foreach (var id in request.MaterialIds.Distinct().Where(id => product.ProductMaterials.All(x => x.MaterialId != id))) product.ProductMaterials.Add(new ProductMaterial { Product=product, MaterialId=id });
        await db.SaveChangesAsync(ct);
        return product.Id;
    }

    public async Task ArchiveProductAsync(Guid id, string? actor, CancellationToken ct)
    {
        var p = await db.Products.FindAsync([id], ct) ?? throw new KeyNotFoundException(); p.IsPublished=false; p.UpdatedAtUtc=DateTime.UtcNow; p.UpdatedBy=actor; await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<EditCategoryRequest>> GetCategoriesAsync(CancellationToken ct) => await db.Categories.AsNoTracking().OrderBy(x=>x.DisplayOrder).Select(c=>new EditCategoryRequest { Id=c.Id, ParentCategoryId=c.ParentCategoryId, DisplayOrder=c.DisplayOrder, IsPublished=c.IsPublished, ImageUrl=c.ImageUrl, BannerUrl=c.BannerUrl, Translations=c.Translations.Select(t=>new CategoryTranslationInput { Culture=t.Culture, Name=t.Name, Slug=t.Slug, ShortDescription=t.ShortDescription, LongDescription=t.LongDescription, SeoTitle=t.SeoTitle, SeoDescription=t.SeoDescription }).ToList() }).ToListAsync(ct);
    public async Task<EditCategoryRequest?> GetCategoryAsync(Guid id, CancellationToken ct) => (await GetCategoriesAsync(ct)).FirstOrDefault(x=>x.Id==id);

    public async Task<Guid> SaveCategoryAsync(EditCategoryRequest request, string? actor, CancellationToken ct)
    {
        await categoryValidator.ValidateAndThrowAsync(request, ct);
        foreach(var t in request.Translations) if(await db.CategoryTranslations.AnyAsync(x=>x.Culture==t.Culture && x.Slug==t.Slug && x.CategoryId!=request.Id,ct)) throw new ValidationException($"Slug '{t.Slug}' již existuje.");
        Category c; if(request.Id.HasValue)c=await db.Categories.Include(x=>x.Translations).FirstOrDefaultAsync(x=>x.Id==request.Id,ct)??throw new KeyNotFoundException(); else {c=new Category{CreatedBy=actor};db.Categories.Add(c);} c.ParentCategoryId=request.ParentCategoryId;c.DisplayOrder=request.DisplayOrder;c.IsPublished=request.IsPublished;c.ImageUrl=request.ImageUrl;c.BannerUrl=request.BannerUrl;c.UpdatedAtUtc=DateTime.UtcNow;c.UpdatedBy=actor;
        c.Translations.RemoveAll(x=>request.Translations.All(t=>t.Culture!=x.Culture)); foreach(var input in request.Translations){var t=c.Translations.FirstOrDefault(x=>x.Culture==input.Culture);if(t is null){t=new CategoryTranslation{Culture=input.Culture,CreatedBy=actor};c.Translations.Add(t);}t.Name=input.Name;t.Slug=input.Slug;t.ShortDescription=input.ShortDescription;t.LongDescription=input.LongDescription;t.SeoTitle=input.SeoTitle;t.SeoDescription=input.SeoDescription;t.UpdatedAtUtc=DateTime.UtcNow;t.UpdatedBy=actor;} await db.SaveChangesAsync(ct);return c.Id;
    }

    public Task<IReadOnlyList<AdminLookup>> GetCollectionsAsync(CancellationToken ct) => Lookups(db.Collections.AsNoTracking().Select(x=>new AdminLookup(x.Id,x.Translations.Where(t=>t.Culture=="cs-CZ").Select(t=>t.Name).FirstOrDefault()??x.Translations.Select(t=>t.Name).First())),ct);
    public Task<IReadOnlyList<AdminLookup>> GetMaterialsAsync(CancellationToken ct) => Lookups(db.Materials.AsNoTracking().Select(x=>new AdminLookup(x.Id,x.Translations.Where(t=>t.Culture=="cs-CZ").Select(t=>t.Name).FirstOrDefault()??x.Translations.Select(t=>t.Name).First())),ct);
    public Task<IReadOnlyList<AdminLookup>> GetColorsAsync(CancellationToken ct) => Lookups(db.Colors.AsNoTracking().Select(x=>new AdminLookup(x.Id,x.Translations.Where(t=>t.Culture=="cs-CZ").Select(t=>t.Name).FirstOrDefault()??x.Translations.Select(t=>t.Name).First())),ct);
    private static async Task<IReadOnlyList<AdminLookup>> Lookups(IQueryable<AdminLookup> query,CancellationToken ct)=>await query.OrderBy(x=>x.Name).ToListAsync(ct);

    public async Task<Guid> SaveReferenceAsync(ReferenceItemRequest r,string? actor,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(r.Name)||string.IsNullOrWhiteSpace(r.Code))throw new ValidationException("Kód a název jsou povinné.");
        if(r.Type=="material"){Material x;if(r.Id.HasValue)x=await db.Materials.Include(x=>x.Translations).FirstAsync(x=>x.Id==r.Id,ct);else{ x=new Material{CreatedBy=actor};db.Materials.Add(x);}x.Code=r.Code.ToUpperInvariant();x.IsActive=r.IsActive;foreach(var item in Names(r)){var t=x.Translations.FirstOrDefault(x=>x.Culture==item.Culture);if(t is null){t=new MaterialTranslation{Culture=item.Culture};x.Translations.Add(t);}t.Name=item.Name;}await db.SaveChangesAsync(ct);return x.Id;}
        if(r.Type=="color"){Color x;if(r.Id.HasValue)x=await db.Colors.Include(x=>x.Translations).FirstAsync(x=>x.Id==r.Id,ct);else{x=new Color{CreatedBy=actor};db.Colors.Add(x);}x.Code=r.Code.ToUpperInvariant();x.Hex=r.Hex;x.IsActive=r.IsActive;foreach(var item in Names(r)){var t=x.Translations.FirstOrDefault(x=>x.Culture==item.Culture);if(t is null){t=new ColorTranslation{Culture=item.Culture};x.Translations.Add(t);}t.Name=item.Name;}await db.SaveChangesAsync(ct);return x.Id;}
        Collection c;if(r.Id.HasValue)c=await db.Collections.Include(x=>x.Translations).FirstAsync(x=>x.Id==r.Id,ct);else{c=new Collection{CreatedBy=actor};db.Collections.Add(c);}c.IsPublished=r.IsActive;foreach(var item in Names(r)){var tr=c.Translations.FirstOrDefault(x=>x.Culture==item.Culture);if(tr is null){tr=new CollectionTranslation{Culture=item.Culture};c.Translations.Add(tr);}tr.Name=item.Name;tr.Slug=item.Culture=="cs-CZ"?r.Slug:$"{r.Slug}-{item.Culture}";}await db.SaveChangesAsync(ct);return c.Id;
    }
    public async Task<Guid> AddProductImageAsync(Guid productId, DesignStore.Application.Abstractions.Files.StoredImage image,string altText,bool isPrimary,string? actor,CancellationToken ct)
    {
        var product=await db.Products.Include(x=>x.Images).FirstOrDefaultAsync(x=>x.Id==productId,ct)??throw new KeyNotFoundException();if(isPrimary)foreach(var existing in product.Images)existing.IsPrimary=false;var entity=new ProductImage{Product=product,StorageKey=image.StorageKey,ThumbnailStorageKey=image.ThumbnailStorageKey,ContentType=image.ContentType,Width=image.Width,Height=image.Height,AltText=altText,IsPrimary=isPrimary,DisplayOrder=product.Images.Count,CreatedBy=actor};product.Images.Add(entity);product.MainImageUrl=$"/media/product/{entity.Id}";await db.SaveChangesAsync(ct);return entity.Id;
    }
    public async Task<ProductImageFile?> GetProductImageAsync(Guid imageId,bool thumbnail,CancellationToken ct)=>await db.ProductImages.AsNoTracking().Where(x=>x.Id==imageId).Select(x=>new ProductImageFile(thumbnail?x.ThumbnailStorageKey:x.StorageKey,x.ContentType)).FirstOrDefaultAsync(ct);

    private static IEnumerable<(string Culture,string Name)> Names(ReferenceItemRequest r){yield return("cs-CZ",r.Name);if(!string.IsNullOrWhiteSpace(r.NameEn))yield return("en",r.NameEn);if(!string.IsNullOrWhiteSpace(r.NameDe))yield return("de",r.NameDe);}
    private IQueryable<Product> ProductAggregate()=>db.Products.Include(x=>x.Translations).Include(x=>x.Variants).ThenInclude(x=>x.Translations).Include(x=>x.ProductMaterials).Include(x=>x.Options).ThenInclude(x=>x.Translations).Include(x=>x.Options).ThenInclude(x=>x.Values).ThenInclude(x=>x.Translations).Include(x=>x.SlugHistory).AsSplitQuery();
    private void SyncTranslations(Product p,List<ProductTranslationInput> inputs,string? actor){p.Translations.RemoveAll(x=>inputs.All(i=>i.Culture!=x.Culture));foreach(var i in inputs){var t=p.Translations.FirstOrDefault(x=>x.Culture==i.Culture);if(t is null){t=new ProductTranslation{Culture=i.Culture,CreatedBy=actor};p.Translations.Add(t);}else if(t.Slug!=i.Slug&&!string.IsNullOrWhiteSpace(t.Slug)){var history=new ProductSlugHistory{Product=p,Culture=t.Culture,OldSlug=t.Slug,CreatedBy=actor};p.SlugHistory.Add(history);db.ProductSlugHistories.Add(history);}t.Name=i.Name;t.Slug=i.Slug;t.ShortDescription=i.ShortDescription;t.LongDescription=i.LongDescription;t.Story=i.Story;t.Materials=i.Materials;t.Dimensions=i.Dimensions;t.CareInstructions=i.CareInstructions;t.ShippingInformation=i.ShippingInformation;t.SeoTitle=i.SeoTitle;t.SeoDescription=i.SeoDescription;t.OpenGraphTitle=i.OpenGraphTitle;t.OpenGraphDescription=i.OpenGraphDescription;t.UpdatedAtUtc=DateTime.UtcNow;t.UpdatedBy=actor;}}
    private static void SyncVariants(Product p,List<ProductVariantInput> inputs,bool enabled,string? actor){if(!enabled){p.Variants.Clear();return;}p.Variants.RemoveAll(x=>inputs.All(i=>i.Id!=x.Id));foreach(var i in inputs){var v=i.Id.HasValue?p.Variants.FirstOrDefault(x=>x.Id==i.Id):null;if(v is null){v=new ProductVariant{CreatedBy=actor};p.Variants.Add(v);}v.Sku=i.Sku.Trim().ToUpperInvariant();v.Name=i.Name;v.Price=i.Price;v.AvailabilityStatus=i.AvailabilityStatus;v.StockQuantity=i.StockQuantity;v.CanBeMadeToOrder=i.CanBeMadeToOrder;v.EstimatedProductionDays=i.EstimatedProductionDays;v.UpdatedAtUtc=DateTime.UtcNow;v.UpdatedBy=actor;SetVariantName(v,"cs-CZ",i.Name);if(!string.IsNullOrWhiteSpace(i.NameEn))SetVariantName(v,"en",i.NameEn);if(!string.IsNullOrWhiteSpace(i.NameDe))SetVariantName(v,"de",i.NameDe);}}
    private static void SetVariantName(ProductVariant v,string culture,string name){var t=v.Translations.FirstOrDefault(x=>x.Culture==culture);if(t is null){t=new ProductVariantTranslation{Culture=culture};v.Translations.Add(t);}t.Name=name;}
    private static void SyncOptions(Product p,List<ProductOptionInput> inputs,string? actor){p.Options.RemoveAll(x=>inputs.All(i=>i.Id!=x.Id));foreach(var i in inputs){var o=i.Id.HasValue?p.Options.FirstOrDefault(x=>x.Id==i.Id):null;if(o is null){o=new ProductOption{CreatedBy=actor,DisplayOrder=p.Options.Count};p.Options.Add(o);}o.Type=i.Type;o.IsRequired=i.IsRequired;o.MaximumTextLength=i.MaximumTextLength;var ot=o.Translations.FirstOrDefault(x=>x.Culture=="cs-CZ");if(ot is null){ot=new ProductOptionTranslation{Culture="cs-CZ"};o.Translations.Add(ot);}ot.Name=i.Name;o.Values.RemoveAll(x=>i.Values.All(v=>v.Id!=x.Id));foreach(var vi in i.Values){var v=vi.Id.HasValue?o.Values.FirstOrDefault(x=>x.Id==vi.Id):null;if(v is null){v=new ProductOptionValue{CreatedBy=actor,DisplayOrder=o.Values.Count};o.Values.Add(v);}v.PriceAdjustment=vi.PriceAdjustment;v.AdditionalProductionDays=vi.AdditionalProductionDays;v.IsActive=vi.IsActive;var vt=v.Translations.FirstOrDefault(x=>x.Culture=="cs-CZ");if(vt is null){vt=new ProductOptionValueTranslation{Culture="cs-CZ"};v.Translations.Add(vt);}vt.Name=vi.Name;}}}
}
