# Externí přihlášení Google a Facebook

Wild Spirit používá stejný konfigurační přístup jako Trading Journal. Provider se aktivuje pouze tehdy, když jsou vyplněny obě hodnoty.

```powershell
dotnet user-secrets set "Authentication:Google:ClientId" "..."
dotnet user-secrets set "Authentication:Google:ClientSecret" "..."
dotnet user-secrets set "Authentication:Facebook:AppId" "..."
dotnet user-secrets set "Authentication:Facebook:AppSecret" "..."
```

Callback URL pro lokální HTTPS profil:

- `https://localhost:5198/cs-CZ/Account/ExternalLoginCallback`

Bez konfigurace se aplikace spustí normálně a tlačítka externího přihlášení se nezobrazí. Přihlášení přes externí účet vytvoří nebo propojí účet podle ověřeného e-mailu.
