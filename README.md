# ColourIdentifier

An offline Android app for identifying colours in photos and screenshots.

## Features

- Import an image from Android's system file picker (including gallery/cloud providers exposed there).
- Tap to identify one pixel, or enable **Rectangle mode** and drag to average a region.
- Preview the colour swatch, closest CSS colour name, `#RRGGBB`, and `rgb(r, g, b)`.
- Read point coordinates or inclusive rectangle bounds, dimensions and pixel count.
- Copy either CSS value to the clipboard.
- Images remain on-device. No network, camera, or broad storage permissions are required.
- Cancelled or unreadable selections leave the previous image available.

## Usage

1. Tap **Import image**, then choose a photo or screenshot in Files. PNG, JPEG and WebP are good starting points.
2. With Rectangle mode off, tap the image to sample a point. A crosshair marks the pixel.
3. With Rectangle mode on, drag from one corner to the opposite corner and release. The outlined rectangle is averaged; dragging in any direction works. Drags ending outside the image clamp to its edge; taps in the surrounding margins are ignored.
4. Scroll below the preview if necessary to read the result and use **Copy Hex** or **Copy rgb()**.

Coordinates start at `(0, 0)` in the top-left of the **orientation-corrected, sampled image**. Each rectangle includes both endpoints.

## Project and template conventions

The app reuses the .NET 10 / MAUI single-project Android structure, minimal `MauiProgram`/`MainPage` bootstrap, dark XAML palette and optional local release-signing import from [SortNewest](https://github.com/dbak91/SortNewest) and [VolumeRestore](https://github.com/dbak91/VolumeRestore). The `.slnx` solution and linked-source xUnit test layout follow [GitHubDownloadNotifier](https://github.com/dbak91/GitHubDownloadNotifier). ARM64 APK publishing follows VolumeRestore's device setup.

```text
ColourIdentifier.slnx
ColourIdentifier/
  ColourIdentifier.csproj
  App.xaml, MainPage.xaml       MAUI screen and app entry
  Controls/                    Image sampler view
  Models/, Services/           Pixel averaging, CSS formatting/names, coordinate mapping
  Platforms/Android/           Android entry, decoder and touch/drawing handler
ColourIdentifier.Tests/        Platform-independent calculation tests
```

Android's built-in bitmap and EXIF APIs handle decoding and rendering; there is no third-party imaging dependency. The only direct app package is `Microsoft.Maui.Controls`.

## Build and run

Prerequisites:

- .NET **10 SDK** with the **MAUI Android** workload.
- Android SDK platform **36**, Android build tools and platform-tools.
- JDK **17** (or the JDK supported by your installed .NET Android workload).
- An Android **7.0 / API 24** or newer device, or an Android emulator.
- On Windows, Visual Studio with the .NET MAUI workload is an alternative to command-line setup.

From the repository root:

```sh
dotnet workload install maui-android
dotnet restore ColourIdentifier.slnx
dotnet test ColourIdentifier.Tests/ColourIdentifier.Tests.csproj
dotnet build ColourIdentifier/ColourIdentifier.csproj -c Debug
```

If the SDK/JDK cannot be detected, pass `-p:AndroidSdkDirectory=/path/to/android-sdk` and `-p:JavaSdkDirectory=/path/to/jdk` to the build/publish command.

### Physical device (ARM64)

Enable Developer options and USB debugging on the phone, connect it, and accept the debugging prompt. To build and launch the debug app on the attached default device/emulator:

```sh
dotnet build ColourIdentifier/ColourIdentifier.csproj -t:Run -c Debug -p:RuntimeIdentifier=android-arm64
```

For an x64 Android emulator, use `-p:RuntimeIdentifier=android-x64` instead.

### Release APK

```sh
dotnet publish ColourIdentifier/ColourIdentifier.csproj -c Release -f net10.0-android36.0 -p:RuntimeIdentifier=android-arm64
```

Find the APK under `ColourIdentifier/bin/Release/net10.0-android36.0/android-arm64/publish/`. Without custom signing configuration the tooling uses its development keystore; use your own release key before distribution. Install the signed APK using `adb install -r <path-to-Signed.apk>`, or transfer it to the phone and allow installation from that source.

As in the template apps, optional signing MSBuild properties can live in `ColourIdentifier/release-signing/ColourIdentifier.signing.props` (imported only for Release). Set `AndroidKeyStore`, `AndroidSigningKeyStore`, `AndroidSigningKeyAlias`, `AndroidSigningKeyPass` and `AndroidSigningStorePass` locally. The entire signing directory and key files are gitignored; never commit credentials or signing material.

## Limitations and future improvements

- Large images are downsampled by the decoder to at most **2048 pixels per side** to bound memory use. The image readout shows the actual sampled size; coordinates and averages refer to those pixels, not the original file. Original-resolution sampling and zoom/pan are future improvements.
- Colour names are best-effort nearest matches by squared distance in 8-bit RGB against 139 distinct CSS named colours (synonyms such as cyan/aqua share one entry). This is not perceptual colour matching or a calibrated measurement tool.
- Averages are arithmetic means of the displayed 8-bit RGB channels, rounded to the nearest integer, not linear-light averages. Transparent pixels are composited onto **white**, matching the preview.
- Android controls the supported image codecs. Unsupported/corrupt files produce an error; animated images sample the decoded first frame. HDR/wide-gamut content is converted to the decoder's 8-bit bitmap representation.
- One image and selection at a time; selections are not saved across app restarts. No camera capture, palette extraction, or batch processing yet.
- The canvas currently needs touch input; accessible coordinate entry and keyboard selection are future improvements.
