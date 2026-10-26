# Store screenshot refresh

**Goal:** Apply the approved screenshot review on develop: localized recurring rows, richer fixtures, useful first three motifs, DE/EN benefit headings, separate raw and upload assets. No ASC upload.

**Design:** Keep the existing real MAUI screens and capture pipelines. Add isolated Debug-only screenshot scenes for accounts/import/filled quick capture, using the real CSV preparation pipeline. Render seven store images from raw PNGs with Pillow; no fictional UI, settings paths, or unsupported sync claims. Preserve raw filenames for README tooling. Store order: dashboard, accounts, CSV import (Pro), transactions, recurring payments, savings (Pro for multiple goals), quick entry.

**Execution:** Inline on develop, as requested. Preserve existing metadata edits and review artifacts.

- [x] Add regression coverage for rich deterministic fixtures; demonstrate failure.
- [x] Expand localized fixture, add demo CSV and gated scene navigation; fix enum localization.
- [x] Add store compositor with input/locale/dimension validation and tests.
- [x] Wire Fastlane capture/render/upload paths and Mac scene capture; preserve raw README sources.
- [x] Run unit tests, script checks, Apple builds and feasible screenshot capture; visually inspect rendered output.
- [x] Document usage and report any remaining capture limitations.


Validation rulings:
- Capture exposed transient `Shell.CurrentPage == null` after GoToAsync: wait for the actual target page before marking readiness.
- CGWindowList returned a tiny tooltip before the app window: filter normal windows and choose greatest area.
- Intel simulator rejected the previous hardcoded ARM64 binary: build current source with host/override RID and universal simulator bridges/widget. Device artifacts remain ARM64.
- Keep the embedded widget: disabling it leaves an existing P/Invoke unresolved. The native simulator artifacts now support both host architectures instead.
- Review caught Store Debug injection gating and recurring expense sign issues; both corrected. Recurring amounts are rendered using Typ, not the sign of stored amounts.


Verification ledger:
- Fixture RED: new DE/EN richness checks failed (2 failures, 4 existing passes).
- Fixture GREEN and full suite: 698 .NET tests passed.
- Renderer: 5 tests pass (real pixel preservation, transparency, missing motif, missing device/language, output isolation).
- Mac: final build successful; all 18 raw scenes captured; 14 localized store images rendered and visually checked.
- iOS: Intel build successful; 3/6 screenshot runs completed, English runs still in progress at this ledger entry.
- Independent review: final C#/scene/native deltas reviewed, no remaining substantive findings.

- All six original iOS screenshot cases passed. Visual QA caught clipped iPad savings cards despite green capture tests. A stable isolated savings scene rendered correctly, proving a capture readiness issue. Added a full-card geometry predicate and isolated savings capture; targeted DE/EN iPad retakes in progress.

- Final iPad retakes: DE/EN both passed with the new full-card geometry predicate; German retake visually confirmed unclipped. No production layout workaround was needed.
- Final output target: 42 iPhone/iPad store images and 14 Mac store images. No ASC image upload, no review submission, no remote branch push.
