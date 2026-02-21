# Find ROM Cover

[![Platform](https://img.shields.io/badge/platform-Windows%20x64%20%7C%20ARM64-blue)](https://github.com/drpetersonfernandes/FindRomCover/releases)
[![.NET 10.0](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE.txt)
[![GitHub release](https://img.shields.io/github/v/release/drpetersonfernandes/FindRomCover)](https://github.com/drpetersonfernandes/FindRomCover/releases)

Find ROM Cover is an easy-to-use WPF application designed to help you effortlessly rename and organize cover images for your ROM collection. It helps you match and rename your images to correspond with the filenames of your ROMs using intelligent similarity algorithms.

Main window with white theme:
![Screenshot](screenshot2.png)

Main window with dark theme:
![Screenshot](screenshot1.png)

## How It Works

1. **Browse:** Select the folders where your ROMs and cover images are stored.
2. **Command-line Support:** Optionally launch the application with ROM and Image folder paths provided as arguments.
3. **List:** The app identifies which cover images are missing from your ROM collection, optionally using MAME descriptions for better readability.
4. **Match:** It searches the Cover Image folder for images with similar filenames using a configurable similarity algorithm. Adjust the similarity threshold to find the best matches.
5. **Display:** Suggested cover images appear in the right panel for the selected ROM, along with their calculated similarity percentage. You can adjust the size of the image previews.
6. **Copy:** Click on an image or use the right-click context menu ("Use This Image") to copy it to the Cover Image folder, renaming the image to match the ROM filename and converting it to PNG format. The context menu also provides options to copy the image filename or open its location in File Explorer.

## Features

- **Easy ROM and Cover Image Matching:** Automatically matches ROM files with corresponding cover images based on name similarity, with robust image loading and processing.
- **Multiple Similarity Algorithms:** Choose between [Jaro-Winkler Distance](https://en.wikipedia.org/wiki/Jaro%E2%80%93Winkler_distance) (default), [Levenshtein Distance](https://en.wikipedia.org/wiki/Levenshtein_distance), and [Jaccard Similarity](https://en.wikipedia.org/wiki/Jaccard_index) to find the best matches for your needs.
- **Adjustable Similarity Threshold:** Fine-tune how strict the matching criteria should be, whether you prefer exact matches or broader suggestions.
- **Thumbnail Size Customization:** Choose how large or small you want the cover image thumbnails to appear, making it easier to view and select the right covers.
- **Manual Selection & Context Menu:** Browse through suggested images, view similarity scores, and use the right-click context menu for actions like using the image, copying its filename, or opening its folder location.
- **Missing Image Finder:** Quickly identify which ROMs are missing cover images, helping you complete your collection. Includes an option to use MAME descriptions for ROM names.
- **Theme Customization:** Switch between Light and Dark base themes and choose from a variety of accent colors.
- **Simple Interface:** Designed for ease of use, with improved folder path validation and enhanced settings management for supported extensions.
- **Audio Feedback:** Get audible confirmation for successful actions like copying images or removing items from the list.
- **Automatic Error Reporting:** The application includes an automatic error reporting mechanism to help developers quickly identify and fix issues, ensuring a more stable experience. This includes detailed logging and internal error tracking.

## Project Architecture

The codebase follows a clean, modular architecture with clear separation of concerns:

### Services (`FindRomCover/Services/`)
- **[`AudioService`](FindRomCover/Services/AudioService.cs)** - Provides audio feedback using WPF MediaPlayer with STA thread handling
- **[`ImageLoader`](FindRomCover/Services/ImageLoader.cs)** - Static utility for loading images with retry logic for locked files and corruption recovery via Magick.NET
- **[`ImageProcessor`](FindRomCover/Services/ImageProcessor.cs)** - Handles image conversion and saving to PNG format, plus cleanup of orphaned temp files
- **[`SimilarityCalculator`](FindRomCover/Services/SimilarityCalculator.cs)** - Implements multiple string similarity algorithms (Jaro-Winkler, Levenshtein, Jaccard) with parallel processing
- **[`ButtonFactory`](FindRomCover/Services/ButtonFactory.cs)** - Factory for creating context menus and orchestrating similarity calculations
- **[`ErrorLogger`](FindRomCover/Services/ErrorLogger.cs)** - Comprehensive error logging with automatic API reporting, file-based logs, and internal error tracking
- **[`IAudioService`](FindRomCover/Services/IAudioService.cs)** - Interface for audio service abstraction

### Managers (`FindRomCover/Managers/`)
- **[`SettingsManager`](FindRomCover/Managers/SettingsManager.cs)** - Manages application settings with XML persistence and INotifyPropertyChanged support
- **[`MameManager`](FindRomCover/Managers/MameManager.cs)** - Handles MAME DAT file parsing with MessagePack serialization and lazy caching

### Models (`FindRomCover/models/`)
- **[`ImageData`](FindRomCover/models/ImageData.cs)** - Immutable record representing an image with similarity score and lazy-loaded broken image fallback
- **[`MissingImageItem`](FindRomCover/models/MissingImageItem.cs)** - Represents a ROM missing its cover image
- **[`SimilarityCalculationResult`](FindRomCover/models/SimilarityCalculationResult.cs)** - Container for similarity calculation results including processing errors
- **[`Smtp2GoData`](FindRomCover/models/Smtp2GoData.cs)** / **[`Smtp2GoResponse`](FindRomCover/models/Smtp2GoResponse.cs)** - Records for error reporting API responses

## Where can I find ROM Cover Images?

You can find cover images on websites such as [Libretro Thumbnails](https://github.com/libretro-thumbnails/libretro-thumbnails) and [EmuMovies](https://emumovies.com/), with which I have no affiliation.

## Requirements

- Windows 7 or later
- [.NET 10.0 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)

## Installation

1. Download the latest release.
2. Extract the ZIP file to a folder of your choice.
3. Run `FindRomCover.exe`.

## Recent Updates (Refactored Architecture)

The codebase has undergone a significant refactor to improve maintainability and separation of concerns:

### New Service Layer
- **AudioService** - Extracted audio playback into a dedicated service with proper disposal pattern
- **ImageLoader** - Centralized image loading with Magick.NET, retry logic for locked files, and corruption recovery
- **ImageProcessor** - Dedicated image processing service for format conversion and temp file cleanup
- **SimilarityCalculator** - Pure static utility for string similarity calculations with parallel execution
- **ButtonFactory** - Factory pattern for UI element creation and similarity search orchestration

### Manager Layer
- **SettingsManager** - Comprehensive settings management with XML serialization and data binding support
- **MameManager** - Lazy-loaded MAME data cache with MessagePack binary serialization

### Model Layer
- Immutable records and classes for data representation
- Lazy initialization patterns for resource-intensive operations
- Proper encapsulation with init-only properties

### Technical Improvements
- Switched to `System.Text.Json` for all JSON handling (removed Newtonsoft.Json dependency)
- Updated dependencies: ControlzEx 7.0.3, MahApps.Metro 3.0.0-rc0529, MessagePack 3.1.4, Magick.NET 14.10.2
- Enhanced error logging with multiple log files (API, User, Internal)
- Improved image loading with retries for locked files and GDI+ error fallbacks
- Parallel processing for similarity calculations with cancellation support
- Thread-safe lazy initialization for MAME data and broken image resources

## Support the Project

If you find 'Find ROM Cover' useful, please consider showing your support!

- **Give us a Star!** If you like this project, please give it a star ⭐ on GitHub. It helps more people discover the project!
- **Donate:** Did you enjoy using Find ROM Cover? Consider [donating](https://www.purelogiccode.com/donate) to support the project or simply to say thanks! Your contribution helps in maintaining and improving the application.

## Developer

- Developed by [Pure Logic Code](https://www.purelogiccode.com).