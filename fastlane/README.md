fastlane documentation
----

# Installation

Make sure you have the latest version of the Xcode command line tools installed:

```sh
xcode-select --install
```

For _fastlane_ installation instructions, see [Installing _fastlane_](https://docs.fastlane.tools/#installing-fastlane)

# Available Actions

## iOS

### ios screenshots

```sh
[bundle exec] fastlane ios screenshots
```

Capture App Store screenshots (2 devices × 2 locales)

### ios upload_listing

```sh
[bundle exec] fastlane ios upload_listing
```

Upload listing texts and iOS screenshots to App Store Connect (no binary, no review)

### ios upload_listing_mac

```sh
[bundle exec] fastlane ios upload_listing_mac
```

Upload listing texts to the Mac App Store listing (no screenshots, no binary, no review)

### ios upload_listing_all

```sh
[bundle exec] fastlane ios upload_listing_all
```

Upload iOS listing+screenshots then Mac listing texts

### ios upload_ipa

```sh
[bundle exec] fastlane ios upload_ipa
```

Upload the Store IPA to App Store Connect / TestFlight (API key, no Transporter.app, no review)

----

This README.md is auto-generated and will be re-generated every time [_fastlane_](https://fastlane.tools) is run.

More information about _fastlane_ can be found on [fastlane.tools](https://fastlane.tools).

The documentation of _fastlane_ can be found on [docs.fastlane.tools](https://docs.fastlane.tools).
