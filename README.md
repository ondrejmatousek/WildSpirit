# DesignStore / Wild Spirit

Základ prémiového vícejazyčného e-shopu pro autorský nábytek, svítidla a kožené výrobky. Aktuálně je dokončena Etapa 1: solution, katalog, Identity, lokalizované veřejné stránky, development seed, první migrace a automatické testy.

## Technologie

.NET 10, ASP.NET Core MVC, Razor Views, Entity Framework Core 10, SQL Server, ASP.NET Core Identity, FluentValidation, Serilog a xUnit.

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

Výchozí connection string používá `(localdb)\MSSQLLocalDB`. Pro jinou instanci nastavte `ConnectionStrings__DefaultConnection` přes environment variable nebo user secrets.

Development seed se spouští idempotentně po aplikaci migrací a vytváří 7 kategorií, 20 produktů a role. Administrátor se nevytvoří bez secrets:

```powershell
dotnet user-secrets set "DevelopmentAdmin:Email" "admin@example.test" --project src/DesignStore.Web
dotnet user-secrets set "DevelopmentAdmin:Password" "pouzijte-silne-vlastni-heslo" --project src/DesignStore.Web
```

## Testy

```powershell
dotnet test DesignStore.slnx
```

SQL integrační fixture vytváří databázi `DesignStoreTests_<guid>`, aplikuje skutečnou migraci a po testu ji odstraní. Výchozí je LocalDB. Pro CI nebo jinou instanci použijte connection string s tokenem `{database}`:

```powershell
$env:DESIGNSTORE_TEST_CONNECTION='Server=localhost,1433;Database={database};User Id=sa;Password=...;TrustServerCertificate=True'
dotnet test DesignStore.slnx
```

Pokud LocalDB hlásí chybu automatické instance, spusťte ji pomocí `sqllocaldb start MSSQLLocalDB`. V omezeném prostředí lze použít konkrétní named pipe vrácený příkazem `sqllocaldb info MSSQLLocalDB` přes stejnou proměnnou.

## Rozsah Etapy 1

Tlačítka košíku a produktové poptávky na detailu pouze bezpečně komunikují budoucí akci a jsou deaktivovaná. Košík bude implementován v Etapě 3 a produktové poptávky v Etapě 4. Administrace je zatím pouze chráněný vstup; katalogová správa patří do Etapy 2.

Podrobnosti jsou v [ARCHITECTURE.md](ARCHITECTURE.md), [DATA_MODEL.md](DATA_MODEL.md), [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) a [DECISIONS.md](DECISIONS.md).
