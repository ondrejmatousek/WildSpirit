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

Požadavky: .NET SDK 10 a SQL Server/LocalDB.

```powershell
dotnet restore DesignStore.slnx
dotnet ef database update --project src/DesignStore.Infrastructure --startup-project src/DesignStore.Web
dotnet run --project src/DesignStore.Web
```

Výchozí vývojový connection string používá pojmenovanou instanci `(localdb)\WildSpiritLocalDb` a databázi `WildSpiritDesignStore`. Instanci vytvoříte jednou příkazem `sqllocaldb create WildSpiritLocalDb 15.0 -s`. Pro jinou instanci nastavte `ConnectionStrings__DefaultConnection` přes environment variable nebo user secrets.

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

## Testy

```powershell
dotnet test DesignStore.slnx
```

SQL integrační fixture vytváří databázi `DesignStoreTests_<guid>`, aplikuje skutečnou migraci a po testu ji odstraní. Výchozí je LocalDB. Pro CI nebo jinou instanci použijte connection string s tokenem `{database}`:

```powershell
$env:DESIGNSTORE_TEST_CONNECTION='Server=localhost,1433;Database={database};User Id=sa;Password=...;TrustServerCertificate=True'
dotnet test DesignStore.slnx
```

Automatická instance `MSSQLLocalDB` se nepoužívá, protože její LocalDB API může v omezeném procesu selhat. Spusťte `sqllocaldb start WildSpiritLocalDb`. V sandboxovaném prostředí spusťte start, testy i aplikaci ve stejném uživatelském kontextu; přímá named pipe je pouze diagnostická nouzová cesta.

## Rozsah po Etapě 2

Katalogová administrace a konfigurace produktu jsou implementované. Tlačítka košíku a produktové poptávky na detailu stále pouze bezpečně komunikují budoucí akci a jsou deaktivovaná. Košík bude implementován až v Etapě 3 a produktové poptávky v Etapě 4.

Podrobnosti jsou v [ARCHITECTURE.md](ARCHITECTURE.md), [DATA_MODEL.md](DATA_MODEL.md), [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) a [DECISIONS.md](DECISIONS.md).
