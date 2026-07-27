# Finanzübersicht — Support / Legal Site

Öffentliche Seiten im Stil von [simpletd.thomasmenzl.de](https://simpletd.thomasmenzl.de) für **App Store Connect**.

| Seite | URL | ASC-Feld |
|-------|-----|----------|
| Support | `https://finanzuebersicht.thomasmenzl.de/` | Support URL |
| Privacy | `https://finanzuebersicht.thomasmenzl.de/privacy.html` | Privacy Policy URL |
| Impressum | `https://finanzuebersicht.thomasmenzl.de/impressum.html` | DE-Pflicht |

Fallback ohne Custom Domain:

`https://thomas-menzl-softwareentwicklung.github.io/finanzuebersicht/`

## Setup

### 1. GitHub Pages

Repo → Settings → Pages:

- Source: **Deploy from a branch**
- Branch: `main` (oder `develop`)
- Folder: **`/site`**

Optional:

```bash
gh api repos/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/pages \
  -X POST \
  -f build_type=legacy \
  -f source[branch]=main \
  -f source[path]=/site
```

### 2. Custom Domain (wie SimpleTD)

1. DNS bei deinem Domain-Anbieter: CNAME

   `finanzuebersicht.thomasmenzl.de` → `thomas-menzl-softwareentwicklung.github.io`

2. In GitHub Pages die Custom Domain `finanzuebersicht.thomasmenzl.de` eintragen (HTTPS erzwingen).

Die Datei `site/CNAME` enthält bereits:

```
finanzuebersicht.thomasmenzl.de
```

### 3. Noch offen

- USt-Hinweis in `impressum.html` (`REPLACE_WITH_VAT_OR_EXEMPTION`) — analog SimpleTD

### 4. App Store Connect

- Support URL → `https://finanzuebersicht.thomasmenzl.de/`
- Privacy Policy URL → `https://finanzuebersicht.thomasmenzl.de/privacy.html`
