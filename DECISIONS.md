# Technická rozhodnutí DesignStore

Formát: stručný ADR registr. Stav `Přijato` znamená výchozí rozhodnutí, které lze změnit samostatným ADR se zdůvodněním.

## ADR-001: Modulární monolit a Clean Architecture

- Stav: Přijato
- Rozhodnutí: ASP.NET Core MVC modulární monolit se čtyřmi produkčními projekty a jedním testovacím projektem.
- Důvod: odpovídá rozsahu, transakčním požadavkům a udržuje jednoduché nasazení. Mikroservisy by zvýšily riziko konzistence skladu bez přínosu první verze.

## ADR-002: Bez mechanického repository, MediatR a CQRS

- Stav: Přijato
- Rozhodnutí: aplikační služby používají úzce zaměřené persistence porty nebo přímo aplikační abstrakci contextu; EF Core není obalen generickým repository.
- Důvod: méně boilerplate a transparentní dotazy/transakce. Business logika zůstane mimo controllery i EF konfigurace.

## ADR-003: SQL Server jako autorita transakcí a konkurence

- Stav: Přijato
- Rozhodnutí: objednávka, skladové změny a historie vznikají v jedné SQL transakci. `rowversion`, podmíněný update a unikátní omezení chrání souběh.
- Důvod: požadavek zabránit oversellingu nelze spolehlivě řešit jen kontrolou v paměti.

## ADR-004: Skladová úroveň varianty

- Stav: Přijato
- Rozhodnutí: pokud produkt používá varianty, sklad i vyrobitelnost jsou autoritativní na variantě. Stav produktu je pouze agregovaný pohled.
- Důvod: jediný zdroj pravdy a žádné rozpory mezi produktem a variantou.

## ADR-005: Rezervace až při vytvoření objednávky

- Stav: Přijato pro první verzi
- Rozhodnutí: vložení do košíku sklad nerezervuje. Rezervace/odpočet nastane atomicky při vytvoření objednávky a má expiraci podle platební metody.
- Důvod: košíky se často opouštějí; rezervace v košíku by umožnila blokování zásob. Přesný okamžik finálního odpočtu se upřesní s platební bránou.

## ADR-006: Immutable snapshot obchodních dokladů

- Stav: Přijato
- Rozhodnutí: objednávka, poptávka a nabídka kopírují názvy, SKU, konfiguraci, ceny, daň, měnu, kurz a relevantní média.
- Důvod: historie nesmí záviset na pozdější změně katalogu.

## ADR-007: Nabídka je verzovaná a převod idempotentní

- Stav: Přijato
- Rozhodnutí: odeslaná nabídka je neměnná verze; úprava vytvoří další verzi. Převod na objednávku hlídá transakce, idempotency klíč a unikátní vazba.
- Důvod: auditovatelnost a ochrana před dvojklikem/souběhem.

## ADR-008: Bezpečný odkaz ukládá pouze hash tokenu

- Stav: Přijato
- Rozhodnutí: token má nejméně 256 bitů entropie, omezený účel a expiraci; databáze ukládá pouze kryptografický hash. Token lze revokovat.
- Důvod: únik databáze nesmí zpřístupnit zákaznické nabídky.

## ADR-009: Databázový outbox pro e-mail

- Stav: Přijato
- Rozhodnutí: obchodní transakce uloží outbox zprávu, background worker ji odešle s retry. Development provider ukládá HTML do lokální složky a loguje metadata.
- Důvod: chyba SMTP nesmí způsobit nekonzistenci objednávky nebo poptávky.

## ADR-010: Privátní abstraktní úložiště souborů

- Stav: Přijato
- Rozhodnutí: `IFileStorageService` s lokální implementací mimo veřejně zapisovatelný web root. Přístup přes autorizovanou službu; obrázkové deriváty mohou být publikovány odděleně.
- Důvod: bezpečný upload a budoucí přechod na Blob/S3/R2 bez změny případů užití.

## ADR-011: Lokalizace přes překladové tabulky a locale v URL

- Stav: Přijato
- Rozhodnutí: invariantní data v hlavní entitě, překládaný obsah v `*Translation`; URL obsahuje kulturu a lokalizovaný slug.
- Důvod: spravovatelný obsah, unikátní slugy, hreflang a omezení duplicitního SEO obsahu.

## ADR-012: CZK/EUR a snapshot kurzu

- Stav: Přijato
- Rozhodnutí: katalog má základní měnu, ruční kurz se ukládá s platností; objednávka/nabídka ukládá použitý kurz i výsledné částky. Automatické FX není v první verzi.
- Důvod: reprodukovatelné účetní hodnoty a splnění zadání bez předčasné integrace.

## ADR-013: Peníze, čas a identifikátory

- Stav: Přijato
- Rozhodnutí: `decimal`, explicitní zaokrouhlení, UTC a interní GUID. Lidsky čitelná čísla dokladů jsou samostatná a unikátní.
- Důvod: přesnost, bezpečné veřejné reference a nezávislost na sekvenčním PK.

## ADR-014: Administrace jako MVC Area s policies

- Stav: Přijato
- Rozhodnutí: `/Admin` je Area ve stejném webu. Role poskytují hrubé členění a policies oprávnění po modulech/akcích.
- Důvod: jednoduché nasazení a menší riziko příliš širokého administrátorského přístupu.

## ADR-015: Server-rendered frontend bez SPA

- Stav: Přijato
- Rozhodnutí: Razor Views, vlastní CSS tokeny a lehký JavaScript pro galerii, varianty a průběžné nacenění.
- Důvod: SEO, výkon, přístupnost a zadání; žádný zásadní důvod pro SPA nebyl identifikován.

## ADR-016: Soft delete selektivně

- Stav: Přijato
- Rozhodnutí: katalog a obsah se primárně archivují/unpublish; historické doklady se nemažou. Globální soft-delete infrastruktura nebude aplikována mechanicky.
- Důvod: menší složitost dotazů a ochrana historických vazeb.

## ADR-017: Obecný kontakt je oddělen od produktové poptávky

- Stav: Přijato
- Rozhodnutí: `ContactMessage` a `ProductInquiry` mají oddělené endpointy, validaci, administraci a účel zpracování.
- Důvod: produktová poptávka musí mít existující produkt a nesmí se stát obecným formulářem zakázkové výroby.

## ADR-018: Serilog, korelace a audit

- Stav: Přijato
- Rozhodnutí: strukturované logy s korelačním ID; významné admin a stavové operace mají append-only audit. Payloady se minimalizují a redigují.
- Důvod: diagnostika a dohledatelnost bez ukládání secrets nebo nadbytečných osobních údajů.

## ADR-019: Testování konkurence proti SQL Serveru

- Stav: Přijato
- Rozhodnutí: rychlé testy mohou používat lehčí prostředí, ale overselling, transakce a `rowversion` se testují proti SQL Serveru v integračním prostředí.
- Důvod: in-memory/SQLite providery nemají shodnou transakční a konkurenční sémantiku.

## Otevřená rozhodnutí před příslušnou etapou

Následující volby nejsou nutné pro Etapu 0 a nesmí být domyšleny bez obchodních vstupů:

- konkrétní platební brána a její model autorizace/capture/refund;
- konkrétní dopravci a země doručení;
- okamžik expirace rezervace pro bankovní převod a online platbu;
- základní katalogová měna a pravidla zobrazení cen s DPH v jednotlivých zemích;
- e-mail provider a produkční objektové úložiště;
- přesný právní retenční plán, znění souhlasů a obchodních podmínek;
- zda přijetí nabídky automaticky vytváří objednávku, nebo vyžaduje administrátora;
- analytická a marketingová řešení aktivovaná až po souhlasu.
