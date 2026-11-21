# Architektura DesignStore

> Stav po Etapě 1: solution a popsané vrstvy jsou založeny. Veřejný katalog, lokalizované routy, Identity, development seed a první SQL Server migrace jsou implementovány; další funkční moduly zůstávají plánem navazujících etap.

## 1. Účel a hranice Etapy 0

DesignStore je vícejazyčný e-shop prémiové evropské značky s designovým nábytkem, svítidly a ručně vyráběnými koženými výrobky. Systém rozlišuje běžný prodej skladových kusů a nezávaznou poptávku opětovné výroby konkrétního produktu. Obecná zakázková výroba není součástí řešení.

Tento dokument popisuje cílovou architekturu. Etapa 0 neobsahuje aplikační implementaci, migrace ani databázové změny.

## 2. Architektonické zásady

- Modulární monolit nad ASP.NET Core 10 MVC; jeden nasazovaný web a jedna databáze PostgreSQL (od 21. 11. 2026, viz ADR-027; dříve SQL Server).
- Clean Architecture bez mechanických repository, MediatR nebo CQRS vrstev.
- Doména nezná EF Core, Identity, MVC, e-mail ani úložiště souborů.
- Aplikační vrstva koordinuje případy užití a transakční hranice.
- Infrastructure implementuje databázi, Identity, soubory, e-mail a externí integrace.
- Web obsahuje pouze HTTP/MVC odpovědnost, ViewModely a prezentaci.
- Všechny I/O operace jsou asynchronní a přijímají `CancellationToken`.
- Historické doklady používají snapshoty; změna katalogu nesmí změnit objednávku, poptávku ani nabídku.
- Peněžní částky používají `decimal`, měnu ISO kódem a explicitní pravidla zaokrouhlení.
- Čas se ukládá v UTC; lokální čas se používá jen při zobrazení.

## 3. Struktura solution

```text
DesignStore.sln
src/
  DesignStore.Domain/
    Common/
    Catalog/
    Inventory/
    Carts/
    Orders/
    Inquiries/
    Quotes/
    Production/
    Customers/
    Content/
  DesignStore.Application/
    Abstractions/
      Persistence/
      Files/
      Email/
      Identity/
      Time/
    Catalog/
    Carts/
    Checkout/
    Inquiries/
    Quotes/
    Production/
    Administration/
    Common/
  DesignStore.Infrastructure/
    Persistence/
      Configurations/
      Migrations/
      Seed/
    Identity/
    Files/
    Email/
    Payments/
    Localization/
    Observability/
  DesignStore.Web/
    Areas/Admin/
    Controllers/
    ViewModels/
    Views/
    TagHelpers/
    Localization/
    Middleware/
    wwwroot/
tests/
  DesignStore.Tests/
    Unit/
    Integration/
    Architecture/
```

### Závislosti

```text
Domain <- Application <- Web
   ^          ^          |
   +----------Infrastructure
```

- `Domain`: entity, value objects, enumy, doménová pravidla a výjimky.
- `Application`: případy užití, DTO, validace, porty a oprávnění.
- `Infrastructure`: EF Core `DbContext`, konfigurace mapování, Identity, lokální soubory, e-mailový outbox, Serilog.
- `Web`: controllers, Razor Views, veřejná část, oblast `/Admin`, middleware a composition root.
- `Tests`: doménové unit testy a integrační testy přes skutečný relační provider; SQLite není náhradou pro testy SQL Server konkurence.

## 4. Funkční moduly

### Katalog

Produkty, varianty, kategorie, kolekce, materiály, barvy, volby, média, překlady, slug historie, vztahy a SEO. Překladové tabulky nesou lokalizovaný obsah; invariantní obchodní údaje zůstávají na hlavní entitě.

### Dostupnost a sklad

Sklad je autoritou pro prodejnost. U produktu s variantami se stav a zásoba vyhodnocují na variantě; produkt slouží jako agregovaný pohled. `AvailabilityStatus` není sám o sobě postačující: pro vložení do košíku se ověřuje publikace, stav `InStock`, sledování skladu a dostupné množství.

### Košík a checkout

Košík podporuje hosta i účet. Ukládá výběr, nikoli důvěryhodnou cenu; při zobrazení a checkoutu se cena znovu vypočte. Přihlášení může sloučit anonymní košík s uživatelským podle deterministických pravidel.

### Objednávky

Objednávka je historický doklad se snapshoty položek, adres, daní, měny a kurzu. Vytvoření objednávky a odpočet či rezervace skladu proběhnou v jedné databázové transakci.

### Poptávky a nabídky

Poptávka vzniká pouze pro existující publikovaný produkt nebo variantu, která je `MadeToOrder` a lze ji znovu vyrobit. Nabídka je verzovaný obchodní návrh odvozený z poptávky. Přijetí odkazu používá náhodný, hashovaně uložený a časově omezený token.

### Výroba

Výrobní zakázka vzniká až z přijaté nabídky/objednávky. Odděluje obchodní stav objednávky od průběhu výroby.

### Obsah, zákazník a marketing

Identity účet, adresy, oblíbené produkty, souhlasy, realizace, recenze, obsahové stránky a double opt-in newsletter. Obecný kontakt nikdy nevytváří `ProductInquiry`.

## 5. Veřejné stránky

- Domovská stránka: hero, kategorie, doporučené/nové/limitované produkty, řemeslo, proces, materiály, příběh, realizace, reference, galerie a newsletter.
- Katalog a kategorie: filtry, řazení, stránkování, lokalizované čisté URL.
- Detail produktu: galerie, varianty, konfigurace, dostupnost, cena, příběh, péče, doprava, související produkty a právě jedna platná obchodní akce.
- Košík a osm kroků checkoutu, potvrzení objednávky.
- Produktová poptávka přístupná jen z detailu konkrétní položky `MadeToOrder`.
- Veřejná nabídka přes bezpečný odkaz: zobrazení, přijetí, odmítnutí, žádost o úpravu a komentář.
- Účet: profil, adresy, objednávky, poptávky, nabídky, výroba a oblíbené.
- Realizace, příběh, materiály, péče, doprava/platba, FAQ, kontakt, právní a GDPR stránky.
- Technické endpointy: sitemap, robots, health checks a lokalizované přesměrování slugů.

## 6. Administrace

Oblast `/Admin` používá Identity, role a jemnější policies. Moduly:

- Přehled: nové objednávky/poptávky, platby, výroba, nízké zásoby.
- Katalog: produkty, varianty, volby, kategorie, kolekce, materiály, barvy, média, překlady a SEO.
- Sklad: příjem, korekce, rezervace, vrácení, pohyby a upozornění.
- Objednávky, platby, doprava, refundace a historie stavů.
- Poptávky: konfigurace, přílohy, interní/zákaznické poznámky a komunikace.
- Nabídky: položky, daně, doprava, sleva, záloha, platnost, přílohy a odeslání.
- Výroba: zakázky, stav, termíny, poznámky a fotografie.
- Zákazníci, souhlasy a žádosti subjektů údajů.
- Obsah, realizace, recenze, newsletter export a kontakt.
- Nastavení: měny/kurz, doprava, platební metody, e-mail a lokalizace.
- Audit a provozní diagnostika bez citlivých údajů.

## 7. Klíčové procesy

### Nákup skladového produktu

1. Detail načte publikovaný produkt a platné varianty.
2. Server po výběru varianty vrátí aktuální cenu a dostupné možnosti.
3. Přidání do košíku znovu ověří produkt, variantu, konfiguraci, cenu a množství.
4. Checkout znovu nacení celý košík a oznámí změny.
5. V SQL transakci se atomicky ověří a rezervuje/odečte sklad, vytvoří objednávka se snapshoty a uloží skladové pohyby.
6. Při konfliktu se transakce vrátí zpět a zákazník dostane srozumitelnou zprávu; objednávka nevznikne.
7. Potvrzovací e-mail se zapíše do outboxu ve stejné transakci a odešle až následně.

### Vyprodání a přechod dostupnosti

Po odečtení posledního kusu se stav stejné skladové jednotky změní atomicky:

- `CanBeMadeToOrder && AutomaticallySwitchToMadeToOrder` => `MadeToOrder`;
- jinak => `Unavailable`.

U variantového produktu se přepíná varianta. Souhrnný stav produktu se odvodí: alespoň jedna prodejná varianta znamená možnost nákupu; jinak alespoň jedna vyrobitelná varianta znamená poptávku. Konkrétní pravidlo agregace se implementuje v aplikační službě a otestuje. Konkurenci chrání transakce, podmíněný update a concurrency token.

### Produktová poptávka

1. Server identifikuje produkt/variantu z URL nebo formuláře; klientské snapshoty nepřijímá jako autoritu.
2. Ověří existenci, publikaci, `MadeToOrder` a `CanBeMadeToOrder` na správné skladové úrovni.
3. Validuje povolenou konfiguraci, termín, množství, souhlasy a přílohy.
4. Vytvoří unikátní číslo, produktový snapshot, konfiguraci, přílohy, stav `New` a historii.
5. Potvrzení zákazníkovi a upozornění administrátorovi zapíše do outboxu.
6. Odesláním nevzniká objednávka ani rezervace skladu.

### Cenová nabídka

1. Administrátor vytvoří z poptávky draft s položkami, DPH, dopravou, slevou, zálohou, termínem a platností.
2. Při odeslání se nabídka uzamkne jako konkrétní verze, stav se změní na `Sent`, poptávka na `QuoteSent` a vytvoří se bezpečný token.
3. Zákazník nabídku přijme, odmítne nebo požádá o změnu; každá změna má historii a je idempotentní.
4. Přijetí nastaví nabídku i poptávku na `Accepted`; samo o sobě nemusí vytvořit objednávku.

### Převod nabídky na objednávku

1. Lze převést pouze platnou přijatou nabídku, která ještě nebyla převedena.
2. Aplikační služba v jedné transakci vytvoří objednávku ze snapshotu nabídky, adresy, měny, daní a platebního plánu.
3. U výroby na objednávku nevzniká skladový odpočet běžného kusu; vytvoří se vazba na budoucí `ProductionJob` podle nastavení.
4. Nabídka i poptávka přejdou na `ConvertedToOrder` a uloží se jejich vazba na objednávku.
5. Unikátní omezení a idempotency klíč zabrání dvojímu převodu.

## 8. Průřezové oblasti

- Lokalizace: výchozí `cs-CZ`, dále `en` a `de`; locale v URL, překlady obsahu, `hreflang` a canonical.
- Měny: CZK/EUR, ruční kurz; objednávka a nabídka uchovávají měnu, kurz i původní částky.
- E-mail: jednoduchý databázový outbox s retry, stavem a korelačním ID.
- Soubory: `IFileStorageService`, privátní lokální úložiště, náhodné názvy, validace magic bytes/MIME/přípony/velikosti, obrázky re-encode a bezpečné autorizované stahování.
- Pozorovatelnost: Serilog, korelační ID, strukturované logy, audit administrace a health checks.
- Výkon: projekce DTO, stránkování, cílené indexy, cache veřejného katalogu s invalidací a responzivní obrázky.
- SEO a přístupnost: JSON-LD, slug historie s 301, správná sémantika, focus, kontrast a validační souhrny.

## 9. Bezpečnostní a technická rizika

| Riziko | Opatření |
|---|---|
| Overselling posledního kusu | SQL transakce, podmíněný update/concurrency token, integrační souběžné testy |
| Dvojí platba, webhook nebo převod nabídky | Idempotency klíče, unikátní indexy, stavový automat |
| Manipulace ceny/varianty z klienta | Veškeré nacenění a oprávnění konfigurace znovu na serveru |
| Neplatná poptávka | Kontrola existujícího produktu a stavu při GET i POST |
| Nebezpečný upload | Privátní storage, limity, signatura obsahu, re-encode, bez spuštění, autorizované stažení |
| XSS v CMS/admin HTML | Allowlist sanitizace při zápisu, Razor encoding, CSP |
| CSRF, bot a spam | Antiforgery, rate limiting, honeypot/časová kontrola, případně externí challenge |
| Únik osobních údajů/logů | Minimalizace, redakce logů, oprávnění, retenční pravidla a audit |
| Zneužití veřejného odkazu nabídky | 256bit token, pouze hash v DB, expirace, revokace, rate limit |
| Nekonzistence kvůli e-mailu | Transactional outbox; obchodní transakce nečeká na SMTP |
| Slug a lokalizační duplicity | Unikátní index locale+slug, historie slugů, canonical/hreflang |
| Přesnost ceny/DPH/kurzu | Decimal, centralizované zaokrouhlení, snapshot sazby a kurzu |
| Migrace velkého modelu | Modulární konfigurace, malé kontrolované migrace, záloha a rollback postup |
| Příliš široká administrace | Policy-based oprávnění a oddělení rolí Admin/Editor/Warehouse/Support |

## 10. Ověřování architektury

- Architecture testy zakážou závislost Domain na Infrastructure/Web a Application na Web.
- Unit testy pokryjí ceny a stavové přechody.
- Integrační testy pokryjí EF konfiguraci, unikátní omezení, transakce, outbox a konkurenci.
- Web integrační testy pokryjí autorizaci administrace, CSRF a zásadní scénáře.
- Každá etapa končí `dotnet restore`, `dotnet build` a `dotnet test`.
