# Datový model DesignStore

> Stav implementace po Etapě 2: fyzicky jsou vytvořeny Identity tabulky a celý katalogový model popsaný v části 2, včetně variant, překladů, kolekcí, materiálů, barev, voleb, médií, souvisejících produktů a historie slugů. Části 3–9 zůstávají cílovým návrhem dalších etap.

## 1. Konvence

- Primární klíče: interní `Guid`; veřejná čísla objednávek, poptávek a nabídek jsou samostatné unikátní řetězce.
- Datum: `datetime2` v UTC; peníze `decimal(18,2)`, kurz `decimal(18,6)`, hmotnost/rozměry dle potřeby `decimal(18,3)`.
- Auditovatelná data: `CreatedAtUtc`, `CreatedBy`, `UpdatedAtUtc`, `UpdatedBy`.
- Konkurenčně měněné entity (`Product`, `ProductVariant`, skladové zůstatky, `Order`, `Quote`) mají SQL `rowversion`.
- Restrict delete je výchozí pro historické a obchodní vazby. Cascade jen pro vlastněné závislé řádky, které bez rodiče nemají význam.
- Soft delete pouze u obnovitelného katalogového/obsahového záznamu; historické obchodní doklady se nemažou.
- Překlady mají unikátní `(ParentId, Culture)`; slugy unikátní alespoň v `(Culture, Slug)` v příslušném typu.

## 2. Katalog

### Product

Nese invariantní údaje: SKU, `CategoryId`, volitelně `CollectionId`, základní/počáteční cenu, DPH, publikaci, pořadí, typ produktu, `HasVariants`, skladová pravidla, dostupnost, výrobu, rozměry/hmotnost a SEO invarianty. Má kolekce variant, překladů, obrázků, materiálů, voleb a vztahů.

Pravidla/indexy:

- unikátní normalizované SKU;
- index `(IsPublished, AvailabilityStatus, DisplayOrder)`;
- bezvariantový produkt může nést sklad, variantový produkt jej nesmí používat jako autoritativní;
- `MadeToOrder` vyžaduje `CanBeMadeToOrder = true`.

### ProductTranslation

`ProductId`, kultura, název, slug, krátký/dlouhý popis, příběh, výroba, péče, doprava, SEO title/description a OG údaje. Unikátní `(ProductId, Culture)` a `(Culture, Slug)`.

### ProductVariant / ProductVariantTranslation

Varianta obsahuje SKU, volitelnou cenu/příplatek, sklad, dostupnost, možnost výroby, počet dnů, rozměry, hmotnost a `rowversion`. Překlad obsahuje lokalizovaný název/popisy. Unikátní SKU a unikátní kombinace hodnot varianty na produktu.

### Category / CategoryTranslation

Hierarchie přes nullable `ParentCategoryId`, obrázek/banner, pořadí, publikace. Restrict při mazání rodiče s potomky. Překlad: název, slug, popisy a SEO. Index rodič+pořadí a unikátní kultura+slug.

### Collection / CollectionTranslation

Kolekce, publikace, období a pořadí; překlady názvu, slugů a obsahu.

### Média, materiály a barvy

- `ProductImage`: produkt, volitelná varianta, privátní storage key, veřejná odvozenina, alt text/překlady, rozměry, pořadí, `IsPrimary`.
- `Material` + `MaterialTranslation`, `Color` + `ColorTranslation`.
- `ProductMaterial`: M:N produkt–materiál s pořadím a volitelnou poznámkou.

### Volby a konfigurace

- `ProductOption` + překlad: typ (`Select`, `Text`, `Engraving`…), povinnost, pořadí, omezení a validační parametry.
- `ProductOptionValue` + překlad: hodnota, příplatek, dopad na výrobu, obrázek, aktivita.
- vazba na variantu určuje, které hodnoty jsou pro ni platné; unikátní kombinaci varianty tvoří explicitní spojovací záznamy.
- Volný text má max délku a je vždy považován za nedůvěryhodný vstup.

### Vztahy a slugy

- `ProductRelatedItem(ProductId, RelatedProductId, SortOrder)` s unikátní dvojicí a zákazem self-reference.
- `ProductSlugHistory(ProductId, Culture, OldSlug, CreatedAtUtc)` s unikátní `(Culture, OldSlug)`; nikdy nesmí kolidovat s aktivním slugem.

## 3. Sklad

Samostatná entita `InventoryMovement` doplňuje požadovaný model: produkt nebo varianta (právě jedno), typ (`Receipt`, `Adjustment`, `Reservation`, `Release`, `Sale`, `Return`), změna, původní/nové množství, důvod, `OrderId`, uživatel a čas.

Rezervaci reprezentuje `InventoryReservation` s položkou objednávky/košíku, množstvím, expirací a stavem. Pro první verzi se rezervace vytváří až při vzniku objednávky, ne pouhým vložením do košíku. Dostupné množství je fyzický zůstatek minus aktivní rezervace, případně je zůstatek přímo atomicky snížen podle zvoleného účetního modelu; oba přístupy se nesmí míchat.

Kontrola `CHECK` zajistí právě jednu vazbu Product/Variant. Indexy jsou na skladové jednotce, času a objednávce. `rowversion` a podmíněný SQL update chrání poslední kus.

## 4. Košík a objednávka

- `Cart`: anonymní hashovaný token nebo `UserId`, kultura, měna, expirace. Unikátní aktivní košík na uživatele/token.
- `CartItem`: produkt, volitelná varianta, množství, poznámka a poslední informativní nacenění. Unikátní fingerprint produktu+varianty+konfigurace v košíku.
- `CartItemConfiguration`: option/value nebo sanitizovaný vlastní text, informativní příplatek.
- `Order`: číslo, zákazník/guest e-mail, stavy objednávky a platby, měna, kurz, mezisoučty, sleva, doprava, DPH, celkem, datum, poznámka a concurrency token.
- `OrderItem`: vazba na původní produkt/variantu může být nullable, ale snapshot názvu, SKU, varianty, obrázku, množství, ceny, sazby DPH a slevy je povinný.
- `OrderItemConfiguration`: snapshot názvu volby, hodnoty a příplatku.
- `OrderAddress`: typ, kompletní snapshot adresy; není odkazem na měnitelnou `CustomerAddress`.
- `OrderStatusHistory`: předchozí/nový stav, důvod, aktér, čas.
- `Payment`: provider/reference, metoda, částka/měna, stav, čas a unikátní externí event/idempotency ID; žádná kartová data.
- `Shipment`: dopravce, služba, tracking, stav, cena a časy.
- `DiscountCode`: normalizovaný unikátní kód, typ/hodnota, interval, limity, minimum, aktivita; vazby na kategorie/produkty a samostatné evidence použití.

## 5. Poptávky a nabídky

- `ProductInquiry`: unikátní číslo, povinný `ProductId`, volitelný `ProductVariantId`, stav, kontaktní údaje, země, množství, termín, poznámka, souhlasy a kompletní produktový snapshot (název, varianta, SKU, URL, obrázek, cena, měna, výroba).
- `ProductInquiryConfigurationItem`: snapshot volby, hodnoty, příplatku a dopadu na termín.
- `ProductInquiryAttachment`: privátní storage key, původní/bezpečný název, MIME, velikost, hash a stav kontroly.
- `ProductInquiryStatusHistory`: stavy `New` až `ConvertedToOrder`, aktér, čas, poznámka.
- Doplněné `ProductInquiryNote`: interní/zákaznická poznámka, autor, čas; oddělení viditelnosti je povinné.
- `Quote`: číslo, inquiry, verze, stav, měna/kurz, součty, DPH, sleva, záloha, doplatek, výroba, dokončení, platnost, podmínky, poznámka, `ConvertedOrderId`, concurrency token.
- `QuoteItem`: typ položky, popis snapshotu, množství, jednotková cena, DPH a součet.
- `QuoteAttachment`, `QuoteStatusHistory`, `QuoteCustomerComment`.
- `QuoteAccessToken`: pouze hash tokenu, účel, expirace, použití/revokace. Surový token se neukládá.

Unikátní `(ProductInquiryId, Version)` zabraňuje záměně revizí; unikátní filtrovaný `ConvertedOrderId` a transakce brání dvojímu převodu.

## 6. Výroba

- `ProductionJob`: číslo, objednávka/položka, produktový snapshot, množství, stav, plánované/skutečné termíny a odpovědná osoba.
- `ProductionJobStatusHistory`: předchozí/nový stav, čas, aktér a poznámka.
- `ProductionJobAttachment`: bezpečná fotografie/dokument a příznak viditelnosti zákazníkovi.
- `ProductionJobNote`: interní/zákaznická viditelnost, text, autor a čas.

Stav výroby není odvozován pouze ze stavu objednávky; propojení je explicitní.

## 7. Zákazník a souhlasy

- `ApplicationUser`: Identity účet, preferovaná kultura/měna a stav účtu; osobní profil jen v nezbytném rozsahu.
- `CustomerAddress`: uživatelský adresář, nikoli historická objednávková adresa.
- `FavoriteProduct`: unikátní `(UserId, ProductId)`.
- Etapa 5 doplňuje `ProductionJob`, stavovou historii, bezpečné přílohy a poznámky s příznakem viditelnosti, dále `CustomerAddress` a `FavoriteProduct` s unikátním vlastnickým indexem.
- `ConsentRecord`: subjekt nebo pseudonymní identifikátor, typ, verze textu, udělení/odvolání, čas, kultura a minimalizované technické důkazy.

## 8. Obsah

- `Review`: produkt, zákazník, hodnocení, text, publikace a moderace.
- `ProjectGalleryItem` + `ProjectGalleryImage`: realizace, překlady, média, pořadí a publikace.
- `ContentPage` + `ContentPageTranslation`: typ stránky, publikace, lokalizovaný slug/obsah/SEO.
- `NewsletterSubscriber`: normalizovaný unikátní e-mail, kultura, stav, potvrzovací token hash, časy přihlášení/potvrzení/odhlášení a důkaz souhlasu.
- Doplněný `ContactMessage`: obecný kontakt oddělený od produktové poptávky.

## 9. Provozní entity

- `OutboxMessage`: typ, serializovaný payload bez zbytečných osobních údajů, vznik, pokusy, další pokus, zpracování a chyba.
- `AdminAuditEntry`: aktér, akce, typ/ID objektu, korelace, čas a bezpečný diff bez secrets.
- `CurrencyRate`: základní/cílová měna, kurz, platnost a administrátor.
- `SlugRedirect` lze použít společně pro další obsahové typy; produkt má požadovanou specializovanou historii.

## 10. Hlavní vazby

```text
Category 1--* Product 1--* ProductVariant
Product 1--* ProductTranslation
Product 1--* ProductImage
Product *--* Material
Product 1--* ProductOption 1--* ProductOptionValue

Cart 1--* CartItem 1--* CartItemConfiguration
Order 1--* OrderItem 1--* OrderItemConfiguration
Order 1--* Payment
Order 1--* Shipment

Product 1--* ProductInquiry *--0..1 ProductVariant
ProductInquiry 1--* Quote
Quote 0..1--1 Order
Order 1--* ProductionJob
```

## 11. Integritní pravidla

- Poptávka vždy odkazuje na existující produkt; historický snapshot zůstane neměnný.
- Varianta v košíku/poptávce musí patřit zadanému produktu.
- Sklad je u variantového produktu měněn výhradně na variantě.
- `Unavailable` a `Discontinued` nelze koupit ani poptat.
- Aktivní nabídku nelze přijmout po expiraci ani převést dvakrát.
- Objednávkové snapshoty a finanční hodnoty se po potvrzení nemění; opravy probíhají přes explicitní události/doklady.
- Historie stavů je append-only.

## 12. Mazání a retence

- Objednávky, platby, poptávky, nabídky, výroba, pohyby skladu, souhlasy a audit se fyzicky nemažou běžnou administrací.
- Produkty použité v historii se archivují/unpublish; FK je `Restrict`.
- Média lze odstranit až po ověření referencí a retenčních pravidel.
- Osobní údaje se po zákonných lhůtách anonymizují tam, kde účetní povinnosti nevyžadují uchování.

## Stav po Etapě 3

Datový model nyní obsahuje košíky, konfigurace položek, objednávkové snapshoty, adresy, platby, dopravu, skladové pohyby, historii stavů a transakční outbox. Části 4–9 zůstávají mimo rozsah.
## Stav po Etapě 4 (průběžně)

Přidány entity `ProductInquiry`, `Quote`, jejich položky, snapshoty, stavové historie a hashované přístupové tokeny. Přílohy jsou připravené v modelu; bezpečný upload a kompletní administrační UI zůstávají k dokončení před uzavřením etapy.
 
## Stav po Etapě 6

Datový model nyní obsahuje marketingový obsah:

- `ContentPage` a `ContentPageTranslation` pro publikované lokalizované stránky se slugem, titulkem, souhrnem, HTML tělem a SEO metadaty.
- `GalleryItem` a `GalleryItemTranslation` pro publikovanou galerii realizací.
- `Review` pro publikované recenze.
- `NewsletterSubscription` s normalizovaným unikátním e-mailem, stavem odběru, hashem potvrzovacího tokenu, hashem odhlašovacího tokenu a časovými stopami potvrzení/odhlášení.
- `ContactMessage` pro obecný kontaktní formulář, oddělený od produktových poptávek, s IP hashem pro rate limiting.

Marketingové tabulky jsou vytvořeny migrací `20260718070959_Stage6MarketingContent`. Historické obchodní entity zůstávají beze změny.
