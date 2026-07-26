# Finanzübersicht — öffentliche Legal-/Support-Seiten

Statische Seiten für **App Store Connect** (Support URL + Privacy Policy URL).

Liegen bewusst im App-Repo unter `site/`, weil das Repository öffentlich ist — kein Extra-Repo nötig (anders als bei SimpleTD).

| Seite | URL (nach Pages) | ASC-Feld |
|-------|------------------|----------|
| Support | Site-Root / `index.html` | Support URL |
| Privacy | `privacy.html` | Privacy Policy URL |
| Impressum | `impressum.html` | DE-Pflicht auf der Website |

Erwartete Base-URL:

`https://thomas-menzl-softwareentwicklung.github.io/finanzuebersicht/`

## Setup

1. Platzhalter ersetzen in den HTML-Dateien:
   - `REPLACE_WITH_EMAIL`
   - `REPLACE_WITH_STREET`
   - `REPLACE_WITH_ZIP_CITY`
   - `REPLACE_WITH_VAT_OR_EXEMPTION`
2. GitHub Pages aktivieren (Repo → Settings → Pages):
   - Source: **Deploy from a branch**
   - Branch: `main` (oder `develop`, solange der Store-Release von dort kommt)
   - Folder: `/site`
3. Optional per API:

```bash
gh api repos/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/pages \
  -X POST \
  -f build_type=legacy \
  -f source[branch]=main \
  -f source[path]=/site
```

4. In App Store Connect eintragen:
   - Support URL → `…/finanzuebersicht/`
   - Privacy Policy URL → `…/finanzuebersicht/privacy.html`
