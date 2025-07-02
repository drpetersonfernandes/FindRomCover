# Find ROM Cover

Find ROM Cover is an easy-to-use application designed to help you effortlessly rename and organize cover images for your ROM collection. It helps you rename your images to match the filenames of your ROMs.

The main window shows the right-click context menu:
![Screenshot](screenshot1.png)

Another screenshot showing the dark theme:
![Screenshot](screenshot2.png)

## How It Works:

-   **Browse:** Select the folders where your ROMs and cover images are stored.
-   **Command-line Support:** Optionally launch the application with ROM and Image folder paths provided as arguments.
-   **List:** It will list which cover images are missing for your ROM collection.
-   **Match:** The app will search within the Cover Image folder for an image with a similar filename using a configurable similarity algorithm. Adjust the similarity threshold to find the best matches.
-   **Display:** Displays suggestions for cover images on the right panel for the selected ROM, along with their calculated similarity percentage. You can adjust the size of the image previews.
-   **Organize:** Manually select the best cover images from the suggestions provided.
-   **Copy:** Click on an image or use the right-click context menu ("Use This Image") to copy it to the Cover Image folder, renaming the image to match the ROM filename and converting it to PNG format. The context menu also provides options to copy the image filename or open its location in File Explorer.

## Features:

-   **Easy ROM and Cover Image Matching:** Automatically matches your ROM files with their corresponding cover images based on name similarity.
-   **Multiple Similarity Algorithms:** Choose between Jaro-Winkler Distance (default), Levenshtein Distance, and Jaccard Similarity to find the best matches for your needs.
-   **Adjustable Similarity Threshold:** Fine-tune how strict the matching criteria should be, whether you prefer exact matches or broader suggestions.
-   **Thumbnail Size Customization:** Choose how large or small you want the cover image thumbnails to appear, making it easier to view and select the right covers.
-   **Manual Selection & Context Menu:** Browse through suggested images, view similarity scores, and use the right-click context menu for actions like using the image, copying its filename, or opening its folder location.
-   **Missing Image Finder:** Quickly identify which ROMs are missing cover images, helping you complete your collection.
-   **Theme Customization:** Switch between Light and Dark base themes and choose from a variety of accent colors.
-   **Simple Interface:** Designed for ease of use, making it accessible to everyone.
-   **Automatic Error Reporting:** The application includes an automatic error reporting mechanism to help developers quickly identify and fix issues, ensuring a more stable experience.

## Where can I find ROM Cover Images?

You can find cover images on websites such as [Libretro Thumbnails](https://github.com/libretro-thumbnails/libretro-thumbnails) and [EmuMovies](https://emumovies.com/), with which I have no affiliation.

## Requirements

- Windows 7 or later
- [.NET 9.0 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)

## Installation

1. Download the latest release.
2. Extract the ZIP file to a folder of your choice.
3. Run `FindRomCover.exe`.

## Support the Project

If you find 'Find ROM Cover' useful, please consider showing your support!

*   **Give us a Star!** If you like this project, please give it a star ⭐ on GitHub. It helps more people discover the project!
*   **Donate:** Did you enjoy using Find ROM Cover? Consider [donating](https://www.purelogiccode.com/donate) to support the project or simply to say thanks! Your contribution helps in maintaining and improving the application.

## Developer:

- Developed by [Pure Logic Code](https://www.purelogiccode.com).

---

Thank you for using **Find Rom Cover**! For more information and support, visit [purelogiccode.com](https://www.purelogiccode.com).