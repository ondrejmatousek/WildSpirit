# Nasazení WildSpirit na Oracle Cloud (vedle TradeReplay)

Server: Ubuntu 24.04 ARM64 `138.2.189.34`, uživatel `ubuntu`, Docker Compose a Caddy už běží pro TradeReplay
(viz `docs/oracle-operations.md` v repozitáři TradeReplay). Eshop běží ve vlastním Compose projektu `wildspirit`:

| Část | Kde |
|---|---|
| Zdrojový kód | `/srv/wildspirit/src` |
| Konfigurace a hesla | originál `/srv/wildspirit/production.env`, kopie v `src/deploy/oracle/` (obě chmod 600, nejsou v gitu) |
| Heslo prvního administrátora | `/srv/wildspirit/admin-credentials.txt` (jen root: `sudo cat …`) |
| Data PostgreSQL | `/srv/wildspirit/data/postgres` |
| Fotky produktů a klíče přihlášení | `/srv/wildspirit/data/app/files`, `/srv/wildspirit/data/app/keys` |
| Zálohy | `/srv/wildspirit-backups` (denně 03:15 UTC, 14 dní) |
| Web | kontejner na `127.0.0.1:4175`, HTTPS přes Caddy z TradeReplay |
| Adresa | `https://wildspirit.138-2-189-34.sslip.io` (do koupě domény) |

## Před prvním nasazením

1. Zkontroluj volnou paměť a disk: `free -h`, `df -h /srv`. Eshop s PostgreSQL potřebuje zhruba 1 GB RAM.
2. Kód musí být commitnutý; na server se kopíruje archiv konkrétního commitu.

## První nasazení

Na lokálním počítači (z kořene repozitáře):

```powershell
git archive --format=tar.gz -o wildspirit.tgz HEAD
scp wildspirit.tgz ubuntu@138.2.189.34:/tmp/
```

Na serveru:

```sh
sudo mkdir -p /srv/wildspirit/src /srv/wildspirit/data/app/files /srv/wildspirit/data/app/keys /srv/wildspirit-backups
# Klíče přihlášení a zálohy čte jen root (server je sdílený s TradeReplay).
sudo chmod 700 /srv/wildspirit/data /srv/wildspirit/data/app/keys /srv/wildspirit-backups
sudo tar -xzf /tmp/wildspirit.tgz -C /srv/wildspirit/src
cd /srv/wildspirit/src/deploy/oracle
sudo sh -c 'umask 077; cp production.env.example /srv/wildspirit/production.env'
sudo nano /srv/wildspirit/production.env   # heslo DB, první administrátor, případně testovací klíče Stripe
sudo cp /srv/wildspirit/production.env production.env && sudo chmod 600 production.env
sudo docker compose --env-file production.env up -d --build
sudo docker compose --env-file production.env logs --tail=50 web
curl -fsS http://127.0.0.1:4175/health
```

Napojení na Caddy (sdílený s TradeReplay; nejdřív záloha):

```sh
cd /srv/tradereplay
sudo docker compose --env-file deploy/production.env ps caddy   # služba se musí jmenovat caddy
B=/srv/tradereplay-backups/Caddyfile-$(date -u +%Y%m%dT%H%M%SZ)
sudo mkdir -p /srv/tradereplay-backups && sudo cp deploy/Caddyfile "$B" && \
  sudo sh -c '{ echo; cat /srv/wildspirit/src/deploy/oracle/Caddyfile.wildspirit; } >> deploy/Caddyfile' && \
  sudo docker run --rm --env-file deploy/production.env -v /srv/tradereplay/deploy/Caddyfile:/etc/caddy/Caddyfile:ro caddy:2-alpine caddy validate --config /etc/caddy/Caddyfile && \
  sudo docker compose --env-file deploy/production.env restart caddy
curl -fsS https://wildspirit.138-2-189-34.sslip.io/health
```

Kroky jsou řetězené `&&`: bez zálohy se nic nepřipíše a bez úspěšné validace se nic nerestartuje. Upravený soubor se validuje v dočasném kontejneru, protože běžící Caddy má Caddyfile připojený jako jednotlivý soubor a změnu na disku nevidí (`caddy reload` by hlásil „config is unchanged“). Restart znamená několik sekund výpadku i pro TradeReplay a TradeStats. Při chybě validace vrať zálohu: `sudo cp "$B" deploy/Caddyfile`.

Po prvním úspěšném startu smaž z `production.env` řádky `WILDSPIRIT_ADMIN_EMAIL` a `WILDSPIRIT_ADMIN_PASSWORD`, kontejner znovu vytvoř (heslo jinak zůstane v jeho prostředí) a přihlas se do `/cs-CZ/Admin`:

```sh
sudo sed -i '/^WILDSPIRIT_ADMIN_EMAIL=/d;/^WILDSPIRIT_ADMIN_PASSWORD=/d' /srv/wildspirit/production.env
cd /srv/wildspirit/src/deploy/oracle && sudo cp /srv/wildspirit/production.env production.env && sudo docker compose --env-file production.env up -d web
```

Zálohy:

```sh
sudo cp /srv/wildspirit/src/deploy/oracle/wildspirit-backup.{service,timer} /etc/systemd/system/
sudo systemctl daemon-reload && sudo systemctl enable --now wildspirit-backup.timer
sudo systemctl start wildspirit-backup.service && sudo journalctl -u wildspirit-backup --no-pager -n 5
```

Zálohy leží na stejném serveru: chrání před chybou aplikace nebo omylem, ne před ztrátou serveru. Kopii mimo server (např. Oracle Object Storage) je potřeba doplnit.

## Aktualizace kódu

Nová verze se rozbalí do čisté složky (smazané soubory, např. staré migrace, tak nezůstanou) a předchozí zůstane jako `src.prev` pro návrat:

```sh
sudo systemctl start wildspirit-backup.service
sudo rm -rf /srv/wildspirit/src.new && sudo mkdir /srv/wildspirit/src.new
sudo tar -xzf /tmp/wildspirit.tgz -C /srv/wildspirit/src.new
sudo cp /srv/wildspirit/production.env /srv/wildspirit/src.new/deploy/oracle/production.env
sudo rm -rf /srv/wildspirit/src.prev && sudo mv /srv/wildspirit/src /srv/wildspirit/src.prev && sudo mv /srv/wildspirit/src.new /srv/wildspirit/src
cd /srv/wildspirit/src/deploy/oracle
sudo docker compose --env-file production.env up -d --build web
```

Migrace databáze se aplikují automaticky při startu (`Database__MigrateOnStartup`). Návrat: prohodit `src` a `src.prev` a znovu `up -d --build web`; databázi obnovit jen při chybné migraci (níže).

## Obnova databáze ze zálohy

```sh
cd /srv/wildspirit/src/deploy/oracle
sudo docker compose --env-file production.env stop web
# Záloha patří rootovi, proto přesměrování uvnitř sudo:
sudo sh -c 'docker compose --env-file production.env exec -T postgres pg_restore -U wildspirit -d wildspirit --clean --if-exists < /srv/wildspirit-backups/db-RRRRMMDDTHHMMSSZ.dump'
sudo docker compose --env-file production.env start web
```

## Stripe

Testovací klíče (`sk_test_…`) lze použít i na serveru. Webhook endpoint v Stripe Dashboardu: `https://<doména>/payments/stripe/webhook`, události `checkout.session.completed`, `checkout.session.async_payment_succeeded`, `checkout.session.expired`. Jeho `whsec_…` patří do `STRIPE_WEBHOOK_SECRET`. Po změně: `sudo docker compose --env-file production.env up -d web`.

## Kontrola

```sh
cd /srv/wildspirit/src/deploy/oracle
sudo docker compose --env-file production.env ps
sudo docker compose --env-file production.env logs --tail=50 web
```

## Stav nasazení

- 21. 11. 2026: první nasazení commitu `9bf93e4` (větev `feature/postgresql-stripe-oracle`) na `https://wildspirit.138-2-189-34.sslip.io`. Katalog je prázdný, administrátor `admin@wildspirit.local`, Stripe nenastavený (platba kartou skrytá). Záloha Caddyfile `/srv/tradereplay-backups/Caddyfile-20261121T170530Z`, první záloha eshopu `20261121T170653Z`.
