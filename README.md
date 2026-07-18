# DesignStore / Wild Spirit

Základ prémiového vícejazyčného e-shopu pro autorský nábytek, svítidla a kožené výrobky. Aktuálně je dokončena Etapa 2: rozšířený katalog, administrační rozhraní, varianty, produktové volby, bezpečná práce s obrázky, SEO a historie slugů.

## Technologie

.NET 10, ASP.NET Core MVC, Razor Views, Entity Framework Core 10, SQL Server, ASP.NET Core Identity, FluentValidation, Serilog, ImageSharp 3.1 a xUnit.

## Struktura

- `src/DesignStore.Domain` – katalogové entity a obchodní pravidla dostupnosti.
- `src/DesignStore.Application` – DTO, případy čtení katalogu a aplikační porty.
- `src/DesignStore.Infrastructure` – EF Core, SQL Server, Identity, seed a implementace katalogu.
- `src/DesignStore.Web` – MVC controllery, Razor Views, lokalizované routy a vlastní frontend.
- `tests/DesignStore.Tests` – unit, architecture a SQL Server integrační testy.

## Lokální spuštění

Požadavky: .NET SDK 10 a SQL Server Express.

```powershell
dotnet restore DesignStore.slnx
dotnet ef database update --project src/DesignStore.Infrastructure --startup-project src/DesignStore.Web
dotnet run --project src/DesignStore.Web
```

Výchozí vývojový connection string používá lokální instanci `.\SQLEXPRESS` a databázi `WildSpiritDesignStore`. V SSMS odpovídá lokálnímu SQL Express připojení. LocalDB není výchozí cesta pro aplikaci ani testy, protože na tomto stroji selhává její LocalDB API. Pro jinou instanci nastavte `ConnectionStrings__DefaultConnection` přes environment variable nebo user secrets.

Development seed se spouští idempotentně po aplikaci migrací a vytváří katalog, role a potvrzené lokální účty. Výchozí development administrátor:

- e-mail: `admin@wildspirit.local`
- heslo: `WildSpirit.Dev.Admin.2026!`

Výchozí development zákazník pro ověření oprávnění:

- e-mail: `customer@wildspirit.local`
- heslo: `WildSpirit.Dev.Customer.2026!`

Tyto veřejně známé účty jsou určené pouze pro lokální prostředí `Development`. Hodnoty lze přepsat přes user secrets:

```powershell
dotnet user-secrets set "DevelopmentAdmin:Email" "admin@example.test" --project src/DesignStore.Web
dotnet user-secrets set "DevelopmentAdmin:Password" "pouzijte-silne-vlastni-heslo" --project src/DesignStore.Web
```

Přesné lokální adresy:

- přihlášení: `http://localhost:5198/cs-CZ/Account/Login`
- administrace: `http://localhost:5198/cs-CZ/Admin`

Administrace vyžaduje příslušnou policy a některou z povolených rolí, například `Administrator` nebo `ContentManager`. Produkty, kategorie, kolekce, materiály a barvy lze spravovat odděleně; produktový editor obsahuje překlady, SEO, varianty, volby, materiály a upload obrázků.

Obrázky se ukládají mimo `wwwroot` do adresáře nastaveného přes `FileStorage:RootPath`. Výchozí development cesta je `App_Data/storage`. Povolené jsou JPEG, PNG a WebP do 12 MB; server obsah dekóduje, odstraní metadata a vytvoří WebP hlavní obrázek a náhled. Tato cesta nemá být sdílená s adresářem zdrojových souborů ani obsluhována jako statické soubory.

V administraci produktu lze po prvním uložení produktu nahrát až 5 fotografií najednou přes drag & drop zónu. Každý soubor stále prochází stejnou serverovou validací typu, velikosti, dekódování a vytvořením WebP derivátů.

## Testy

```powershell
dotnet test DesignStore.slnx
```

SQL integrační fixture vytváří databázi `DesignStoreTests_<guid>`, aplikuje skutečnou migraci a po testu ji odstraní. Výchozí je `.\SQLEXPRESS`. Pro CI nebo jinou instanci použijte connection string s tokenem `{database}`:

```powershell
$env:DESIGNSTORE_TEST_CONNECTION='Server=localhost,1433;Database={database};User Id=sa;Password=...;TrustServerCertificate=True;Encrypt=False'
dotnet test DesignStore.slnx
```

LocalDB se nepoužívá jako běžná vývojová ani testovací cesta. Pokud je potřeba diagnostikovat starou lokální databázi, řešte ji odděleně; aplikace a testy mají běžet proti SQL Server Express přes standardní název instance.

## Rozsah po Etapě 2

Katalogová administrace a konfigurace produktu jsou implementované. Tlačítka košíku a produktové poptávky na detailu stále pouze bezpečně komunikují budoucí akci a jsou deaktivovaná. Košík bude implementován až v Etapě 3 a produktové poptávky v Etapě 4.

Podrobnosti jsou v [ARCHITECTURE.md](ARCHITECTURE.md), [DATA_MODEL.md](DATA_MODEL.md), [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) a [DECISIONS.md](DECISIONS.md).
 
## Stav po Etapě 6

Etapy 1–6 nyní pokrývají základ aplikace, katalog a administraci, košík/checkout/objednávky, poptávky a nabídky, výrobu a zákaznický účet a marketingový obsah. V Etapě 6 přibyly lokalizované obsahové stránky, galerie realizací, recenze, newsletter double opt-in s odhlášením a CSV exportem, samostatný kontaktní formulář s rate limitingem, `robots.txt` a `sitemap.xml`.

Marketingové lokální adresy:

- kontakt: `http://localhost:5198/cs-CZ/Contact`
- galerie: `http://localhost:5198/cs-CZ/Gallery`
- obsahová stránka: `http://localhost:5198/cs-CZ/Page/pribeh`
- sitemap: `http://localhost:5198/sitemap.xml`
- robots: `http://localhost:5198/robots.txt`
- administrace marketingu: `http://localhost:5198/cs-CZ/Admin/Marketing`

Obecný kontakt nevytváří produktovou poptávku; produktové poptávky zůstávají oddělený proces Etapy 4. Newsletter ukládá pouze hashované tokeny a potvrzení probíhá double opt-in tokem.

## Poznámky k administraci katalogu

Při vytváření nového produktu se SKU automaticky předvyplní v řadě `WS-000001`, `WS-000002`, … podle nejvyššího existujícího produktového SKU. Hodnotu lze před uložením ručně změnit; server dál hlídá unikátnost proti produktům i variantám.

Editor produktu obsahuje pracovní tlačítko `Přeložit z češtiny`, které z českých polí vyplní návrhy pro EN/DE. Jde o rychlý draft pro administrátora, ne o definitivní schválený překlad; text je potřeba před publikací zkontrolovat.

Produktový editor má přepínač `Hotový kus` / `Zakázková výroba`. Hotový kus zobrazuje pevnou základní cenu a sklad. Zakázková výroba zobrazuje orientační `Cena od`, termín výroby a nastaví dostupnost na `MadeToOrder`.

Po výběru podporované kategorie editor automaticky doplní prázdná textová pole šablonou ve všech třech jazycích. Ručně vyplněná pole nepřepisuje. První sada šablon pokrývá kožené kabelky, pásky a kožené doplňky.

Pole slug je v administraci viditelné, aby šla každému produktu nastavit konkrétní čitelná URL, například `kozena-kabelka-aurora`. Při uložení ho aplikace normalizuje na bezpečný tvar pro URL.
## Stav po Etapě 7

Etapa 7 doplňuje provozní bezpečnost a dokončovací infrastrukturu: security headers middleware, CSP mimo Development, health endpoint `/health`, response compression, cookie consent banner bez marketingových skriptů, zákaznický JSON export osobních údajů, žádost o zrušení účtu přes outbox, Dockerfile, Compose se SQL Serverem a provozní `SECURITY.md`.

Nové lokální/provozní adresy:

- health check: `http://localhost:5198/health`
- export osobních údajů: `http://localhost:5198/cs-CZ/Privacy/Export` pouze pro přihlášeného uživatele
- žádost o zrušení účtu: dostupná z `http://localhost:5198/cs-CZ/Account`

Docker Compose vyžaduje heslo mimo repozitář:

```powershell
$env:WILDSPIRIT_SQL_PASSWORD='zvolte-silne-lokalni-heslo'
docker compose up --build
```

Compose vystaví web na `http://localhost:5198` a SQL Server na lokálním portu `14333`. Heslo ani produkční connection stringy se neukládají do repozitáře.
