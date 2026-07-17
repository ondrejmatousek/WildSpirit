# Implementační plán DesignStore

## Zásady provádění

Každá etapa je samostatně kontrolovatelný přírůstek. Před další etapou se aktualizuje dokumentace, provedou migrace jen pro daný rozsah a spustí se `dotnet restore`, `dotnet build` a `dotnet test`. Další etapa nezačne bez kontroly výsledku.

## Etapa 0 – analýza a návrh (tento výstup)

Výstupy:

- `ARCHITECTURE.md`: architektura, solution, stránky, administrace, procesy a rizika.
- `DATA_MODEL.md`: entity, vazby, integrita, indexy, konkurence a retence.
- `IMPLEMENTATION_PLAN.md`: pořadí, akceptační kritéria a testovací strategie.
- `DECISIONS.md`: přijatá technická rozhodnutí a odložené volby.

Akceptace: dokumenty jsou vzájemně konzistentní a pokrývají skladový nákup, vyprodání, poptávku, nabídku a převod na objednávku. Bez aplikačního kódu a databázových změn.

## Etapa 1 – základ aplikace

**Stav: dokončeno 16. 7. 2026.** Implementována solution, vrstvy, EF Core/Identity, první katalogový průřez, migrace, bezpečný development seed, role/policy, lokalizované routy a obsah, veřejný katalog, detail produktu, prémiový responzivní layout a 13 testů včetně integračního testu nad skutečným SQL Serverem.

1. Vytvořit solution a pět projektů, nastavit reference a architecture testy.
2. Přidat ASP.NET Core MVC, EF Core SQL Server, Identity, Serilog, FluentValidation a lokalizaci.
3. Vytvořit první průřez datového modelu pro katalog, uživatele a překlady; připravit první migraci.
4. Implementovat role/policies, bezpečné development vytvoření admina přes user secrets/env.
5. Vytvořit vlastní prémiový layout, CSS tokeny, responzivní navigaci a přístupnost.
6. Implementovat homepage, katalog, kategorii a detail produktu s dostupnostními akcemi pouze pro čtení.
7. Seednout hlavní kategorie a minimálně 20 reprezentativních produktů v odděleném development seedu.

Akceptace: CS/CZ a základ EN/DE lokalizace fungují, katalog je server-rendered, detail nikdy nenabízí neplatnou akci, migrace a seed jsou opakovatelné.

Testy: architektura, mapování EF, lokalizační fallback, veřejné routy, dostupnostní prezentace a autorizace admin vstupu.

## Etapa 2 – katalog a jeho administrace

**Stav: dokončeno 16. 7. 2026.** Implementován rozšířený katalogový model, databázová migrace, administrační CRUD s explicitními DTO a validací, varianty a volby produktu, bezpečné ukládání obrázků s WebP deriváty, lokalizované SEO a historie slugů s trvalým přesměrováním.

1. Dokončit produktový model: varianty, kolekce, materiály, barvy, volby, média a slug historii.
2. Implementovat administraci katalogu s explicitními DTO a validátory.
3. Přidat bezpečné lokální `IFileStorageService`, deriváty WebP/náhledy a validaci uploadu.
4. Implementovat produktovou konfiguraci, cenu s příplatky a platnost kombinací.
5. Přidat SEO metadata, překlady a 301 při změně slugu.

Akceptace: administrátor spravuje celý katalog a překlady; variantový sklad je jednoznačný; upload neopustí privátní storage; změna slugu zachová funkční URL.

Testy: ceny, příplatky, kombinace, unikátní SKU/slug, validace obrázků, sanitizace a admin policies.

## Etapa 3 – košík, sklad a objednávky

1. Implementovat anonymní a uživatelský košík, merge a serverové přecenění.
2. Implementovat skladové pohyby, rezervace, nízký stav a historii.
3. Vytvořit checkout pro hosta i účet, adresy, dopravu, bankovní převod a testovací platbu.
4. Vytvořit objednávkové snapshoty a stavové historie.
5. Zajistit atomické ověření/odpočet skladu, přechod na `MadeToOrder`/`Unavailable` a idempotenci.
6. Přidat e-mailový outbox a šablony potvrzení.

Akceptace: nelze objednat nepublikovanou/nedostupnou položku ani překročit sklad; dva souběžné nákupy posledního kusu vytvoří právě jednu úspěšnou objednávku; selhání e-mailu objednávku nevrátí.

Testy: košík, cena/sleva, checkout, transakce, dva souběžné požadavky, oba přechody po vyprodání, vrácení zásoby, idempotentní platba.

## Etapa 4 – poptávky a cenové nabídky

1. Implementovat produktově vázaný formulář, konfiguraci, souhlasy a bezpečné přílohy.
2. Vytvořit immutable snapshot, číslo poptávky, stavovou historii a outbox notifikace.
3. Implementovat admin workflow, poznámky s viditelností a komunikaci.
4. Implementovat verzované nabídky, položky, daně, zálohu, termín a přílohy.
5. Přidat časově omezené hashované tokeny a zákaznickou stránku nabídky.
6. Implementovat přijetí/odmítnutí/úpravu a jednorázový převod na objednávku.

Akceptace: poptávka nevznikne bez platného `MadeToOrder` produktu/varianty; snapshot se změnou katalogu nemění; expirovaný odkaz nefunguje; souběžný převod vytvoří jednu objednávku.

Testy: povolené/zakázané stavy, konfigurace, upload, tokeny, expirace, stavové přechody, nabídka a idempotentní převod.

## Etapa 5 – výroba a zákaznický účet

1. Implementovat výrobní zakázky a stavový automat.
2. Přidat fotografie, přílohy, interní/zákaznické poznámky a termíny.
3. Implementovat zákaznický profil, adresy, objednávky, poptávky, nabídky, výrobu a oblíbené.
4. Přidat bezpečné autorizační kontroly vlastnictví.

Akceptace: zákazník vidí jen vlastní data a pouze zákaznicky viditelné výrobní informace; stavová historie je úplná.

Testy: stavový automat výroby, ownership authorization, přístup k souborům a oblíbené.

## Etapa 6 – obsah a marketing

1. Realizace, galerie, recenze a lokalizované obsahové stránky.
2. Newsletter double opt-in, odhlášení a CSV export.
3. Obecný kontaktní formulář s rate limitingem a antispamem, oddělený od poptávek.
4. Sitemap, robots, canonical/hreflang, JSON-LD a kompletní slug routing.
5. Dokončit obsah homepage a vizuální galerii bez závislosti na externím Instagram API.

Akceptace: žádné marketingové skripty před souhlasem; kontakt nevytváří produktovou poptávku; SEO výstupy jsou lokalizačně konzistentní.

Testy: double opt-in token, export, rate limit, sitemap, structured data a redirecty.

## Etapa 7 – bezpečnost a dokončení

1. Dokončit cookie consent, GDPR export/žádost o zrušení, retenci a dokumentaci.
2. Zavést CSP a další security headers, audit, redakci logů a provozní rate limits.
3. Provést kontrolu CSRF, XSS, autorizace, uploadů, tokenů, secrets a chybových stránek.
4. Optimalizovat dotazy, indexy, cache, obrázky, kompresi a statické assety.
5. Přidat Dockerfile, Compose se SQL Serverem/volume a health checks.
6. Dokončit `README.md`, `SECURITY.md`, provozní konfiguraci a deployment checklist.
7. Rozšířit deterministické unit a integrační testy na všechny požadované scénáře.

Akceptace: čistý restore/build/test, žádné secrets v repozitáři, zdravý Docker Compose, úspěšná bezpečnostní kontrola a reprodukovatelný setup.

## Migrační strategie

- Jedna malá pojmenovaná migrace na logický přírůstek, kontrolovaný SQL diff.
- Seed je idempotentní a development-only; produkční role a admin heslo nejsou hardcoded.
- Před destruktivní změnou záloha, samostatný data-migration krok a rollback plán.
- Indexy pro velké tabulky se plánují s ohledem na provozní zámky.

## Testovací pyramida

- Unit: doménová pravidla, ceny, slevy, stavy a validace.
- Integration: SQL Server mapování, omezení, transakce, konkurence, outbox a storage.
- Web integration: routy, model binding, antiforgery, cookies, lokalizace a autorizace.
- Omezené end-to-end smoke testy: nákup, poptávka, nabídka a admin workflow.

## Hlavní kontrolní body

- Po Etapě 1: schválit vizuální směr a lokalizační URL.
- Po Etapě 2: schválit katalogový model a administraci před objednávkami.
- Po Etapě 3: zátěžově ověřit poslední kus a rozhodnout okamžik rezervace.
- Po Etapě 4: právně/obchodně schválit text nezávaznosti, nabídky a expirace.
- Před produkcí: potvrdit platební bránu, dopravce, daňové země, retence a provozní infrastrukturu.
