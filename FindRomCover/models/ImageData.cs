using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using FindRomCover.Services;

namespace FindRomCover.Models;

public class ImageData : INotifyPropertyChanged
{
    private static readonly Lazy<BitmapImage> BrokenImageLazy = new(static () =>
    {
        try
        {
            var bitmap = new BitmapImage(new Uri("pack://application:,,,/images/brokenimage.png"));
            if (bitmap.CanFreeze)
                bitmap.Freeze();
            return bitmap;
        }
        catch (Exception ex)
        {
            LogService.Error(ex, "Failed to load broken image fallback resource");
            var bitmap = new BitmapImage();
            if (bitmap.CanFreeze)
                bitmap.Freeze();
            return bitmap;
        }
    });

    private int _imageWidth;
    private int _imageHeight;

    public string? ImagePath { get; init; }
    public string? ImageName { get; set; } = "Unknown Filename";
    public string ImageFileSize { get; set; } = "Unknown File Size";
    public string ImageEncodingFormat { get; set; } = "Unknown Encoding Format";

    public double SimilarityScore { get; init; }

    public BitmapImage? ImageSource { get; init; }

    public BitmapImage DisplayImage => ImageSource ?? BrokenImageLazy.Value;

    public int ImageWidth
    {
        get => _imageWidth;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Image width must be positive.");

            _imageWidth = value;
        }
    }

    public int ImageHeight
    {
        get => _imageHeight;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Image height must be positive.");

            _imageHeight = value;
        }
    }

    public int ThumbnailWidth
    {
        get;
        set
        {
            if (field == value) return;

            field = value;
            OnPropertyChanged();
        }
    }

    public int ThumbnailHeight
    {
        get;
        set
        {
            if (field == value) return;

            field = value;
            OnPropertyChanged();
        }
    }

    public ImageData()
    {
    }

    public ImageData(string? imagePath, string? imageName, double similarityScore)
    {
        ImagePath = imagePath;
        ImageName = imageName;
        SimilarityScore = similarityScore;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
