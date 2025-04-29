# Find ROM Cover

Find ROM Cover is an easy-to-use application designed to help you effortlessly rename and organize cover images for your ROM collection. It helps you rename your images to match the filenames of your ROMs.

Main window showing the right-click context menu:
![Screenshot](screenshot1.png)

Another screenshot showing the dark theme:
![Screenshot](screenshot2.png)

## How It Works:

- **Browse:** Select the folders where your ROMs and cover images are stored.
- **Command-line Support:** Optionally launch the application with ROM and Image folder paths provided as arguments.
- **List:** It will list which cover images are missing for your ROM collection.
- **Match:** The app will search within the Cover Image folder for an image with a similar filename using a configurable similarity algorithm. Adjust the similarity threshold to find the best matches.
- **Display:** Displays suggestions for cover images on the right panel for the selected ROM, along with their calculated similarity percentage. You can adjust the size of the image previews.
- **Organize:** Manually select the best cover images from the suggestions provided.
- **Copy:** Click on an image or use the right-click context menu ("Use This Image") to copy it to the Cover Image folder, renaming the image to match the ROM filename and converting it to PNG format. The context menu also provides options to copy the image filename or open its location in File Explorer.

## Features:

- **Easy ROM and Cover Image Matching:** Automatically matches your ROM files with their corresponding cover images based on name similarity.
- **Multiple Similarity Algorithms:** Choose between Jaro-Winkler Distance (default), Levenshtein Distance, and Jaccard Similarity to find the best matches for your needs.
- **Adjustable Similarity Threshold:** Fine-tune how strict the matching criteria should be, whether you prefer exact matches or broader suggestions.
- **Thumbnail Size Customization:** Choose how large or small you want the cover image thumbnails to appear, making it easier to view and select the right covers.
- **Manual Selection & Context Menu:** Browse through suggested images, view similarity scores, and use the right-click context menu for actions like using the image, copying its filename, or opening its folder location.
- **Missing Image Finder:** Quickly identify which ROMs are missing cover images, helping you complete your collection.
- **Theme Customization:** Switch between Light and Dark base themes and choose from a variety of accent colors.
- **Simple Interface:** Designed for ease of use, making it accessible to everyone.

## Where can I find ROM Cover Images?

You can find cover images on websites such as [Libretro Thumbnails](https://github.com/libretro-thumbnails/libretro-thumbnails) and [EmuMovies](https://emumovies.com/), with which I have no affiliation.

## Support the Project:

Did you enjoy using Find ROM Cover? Consider [donating](https://www.purelogiccode.com/donate) to support the project or simply to say thanks!

## Developer:

This project was developed by a [Simple Launcher](https://github.com/drpetersonfernandes/SimpleLauncher) developer to assist in organizing his cover image collection.

Peterson Fernandes - [GitHub Profile](https://github.com/drpetersonfernandes)

## Technical Details:

Find ROM Cover was written in C# using the Windows Presentation Foundation (WPF) framework with Microsoft .NET 9.0. It utilizes the MahApps.Metro library for enhanced UI styling. <br>
This program is designed exclusively for Windows, with compatibility expected from Windows 7 and later versions. It has been tested on Windows 11.
