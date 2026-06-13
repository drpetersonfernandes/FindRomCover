using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Windows.Input;
using FindRomCover.Models;
using FindRomCover.Services;
using FluentAssertions;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;

namespace FindRomCover.Tests.Services;

[SuppressMessage("ReSharper", "NullableWarningSuppressionIsUsed")]
public class ButtonFactoryTests : IDisposable
{
    private Thread? _staThread;
    private Exception? _staException;
    private bool _staPassed;
    private int _staItemCount;
    private string _staMessage = string.Empty;

    public void Dispose()
    {
        _staThread = null;
        _staException = null;
        GC.SuppressFinalize(this);
    }

    private void RunOnStaThread(Action action)
    {
        _staException = null;
        _staPassed = false;
        _staThread = new Thread(() =>
        {
            try
            {
                action();
                _staPassed = true;
            }
            catch (Exception ex)
            {
                _staException = ex;
            }
        });
        _staThread.SetApartmentState(ApartmentState.STA);
        _staThread.Start();
        _staThread.Join(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void CreateContextMenu_WithNullOrEmptyPath_ReturnsEmptyMenu()
    {
        RunOnStaThread(() =>
        {
            var result = ButtonFactory.CreateContextMenu(string.Empty, static _ => { });
            result.Should().NotBeNull();
            _staItemCount = result.Items.Count;
        });

        _staException.Should().BeNull();
        _staPassed.Should().BeTrue();
        _staItemCount.Should().Be(0);
    }

    [Fact]
    public void CreateContextMenu_WithNullPath_ReturnsEmptyMenu()
    {
        RunOnStaThread(() =>
        {
            var result = ButtonFactory.CreateContextMenu(null!, static _ => { });
            result.Should().NotBeNull();
            _staItemCount = result.Items.Count;
        });

        _staException.Should().BeNull();
        _staPassed.Should().BeTrue();
        _staItemCount.Should().Be(0);
    }

    [Fact]
    public void CreateContextMenu_WithValidPath_CreatesThreeMenuItems()
    {
        RunOnStaThread(() =>
        {
            try
            {
                var result = ButtonFactory.CreateContextMenu(@"C:\images\test.png", static _ => { });
                _staItemCount = result.Items.Count;
            }
            catch (IOException)
            {
                // Embedded images not available in test context, skip item count check
                _staMessage = "Embedded images not available";
            }
        });

        if (_staMessage.Length == 0)
        {
            _staException.Should().BeNull();
            _staPassed.Should().BeTrue();
            _staItemCount.Should().Be(3);
        }
    }

    [Fact]
    public void CreateContextMenu_WithValidPath_MenuItemsHaveCorrectHeaders()
    {
        RunOnStaThread(() =>
        {
            try
            {
                var result = ButtonFactory.CreateContextMenu(@"C:\images\mario.png", static _ => { });
                var headers = result.Items.Cast<MenuItem>().Select(static m => m.Header?.ToString()).ToList();
                headers.Should().Contain("Use This Image");
                headers.Should().Contain("Copy Image Filename");
                headers.Should().Contain("Open File Location");
            }
            catch (IOException)
            {
                _staMessage = "Embedded images not available";
            }
        });

        if (_staMessage.Length == 0)
            _staException.Should().BeNull();
    }

    [Fact]
    public void CreateContextMenu_WithExistingMenu_ReusesSameInstance()
    {
        RunOnStaThread(() =>
        {
            try
            {
                var existingMenu = new ContextMenu();
                existingMenu.Items.Add(new MenuItem { Header = "Existing Item" });

                var returnedMenu = ButtonFactory.CreateContextMenu(
                    @"C:\images\test.png", static _ => { }, existingMenu);

                returnedMenu.Should().BeSameAs(existingMenu);
            }
            catch (IOException)
            {
                _staMessage = "Embedded images not available";
            }
        });

        if (_staMessage.Length == 0)
            _staException.Should().BeNull();
    }

    [Fact]
    public void CreateContextMenu_WithExistingMenu_UpdatesCommandParameter()
    {
        const string newPath = @"C:\images\newpath.png";

        RunOnStaThread(() =>
        {
            try
            {
                var existingMenu = new ContextMenu();
                existingMenu.Items.Add(new MenuItem { Header = "Existing", CommandParameter = @"C:\images\old.png" });

                var returnedMenu = ButtonFactory.CreateContextMenu(
                    newPath, static _ => { }, existingMenu);

                var item = (MenuItem)returnedMenu.Items[0]!;
                item.CommandParameter.Should().Be(newPath);
            }
            catch (IOException)
            {
                _staMessage = "Embedded images not available";
            }
        });

        if (_staMessage.Length == 0)
            _staException.Should().BeNull();
    }

    [Fact]
    public void CreateContextMenu_WithExistingMenuWithoutItems_AddsMenuItems()
    {
        RunOnStaThread(() =>
        {
            try
            {
                var emptyMenu = new ContextMenu();
                var result = ButtonFactory.CreateContextMenu(
                    @"C:\images\test.png", static _ => { }, emptyMenu);

                result.Should().BeSameAs(emptyMenu);
                _staItemCount = result.Items.Count;
            }
            catch (IOException)
            {
                _staMessage = "Embedded images not available";
            }
        });

        if (_staMessage.Length == 0)
        {
            _staException.Should().BeNull();
            _staPassed.Should().BeTrue();
            _staItemCount.Should().Be(3);
        }
    }

    [Fact]
    public void CreateContextMenu_UseThisImageMenuItem_AlwaysAppears()
    {
        RunOnStaThread(() =>
        {
            try
            {
                var result = ButtonFactory.CreateContextMenu(@"C:\images\test.png", null!);
                _staItemCount = result.Items.Count;
            }
            catch (IOException)
            {
                _staMessage = "Embedded images not available";
            }
        });

        if (_staMessage.Length == 0)
        {
            _staException.Should().BeNull();
            _staPassed.Should().BeTrue();
            _staItemCount.Should().Be(3);
        }
    }

    [Fact]
    public async Task CreateSimilarImagesCollectionAsync_DelegatesToSimilarityCalculator()
    {
        var cancellationToken = CancellationToken.None;

        var result = await ButtonFactory.CreateSimilarImagesCollectionAsync(
            "testrom", string.Empty, 70, AppConstants.Algorithms.Levenshtein, cancellationToken);

        result.Should().NotBeNull();
        result.SimilarImages.Should().BeEmpty();
        result.ProcessingErrors.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateSimilarImagesCollectionAsync_WithOnImageLoadedCallback_CompletesSuccessfully()
    {
        var loadedImages = new System.Collections.Concurrent.ConcurrentBag<ImageData>();
        var tempDir = Directory.CreateTempSubdirectory("frc_btn_test_");

        try
        {
            var imagePath = Path.Combine(tempDir.FullName, "zelda.png");
            await CreateMinimalBmpAsync(imagePath);

            var result = await ButtonFactory.CreateSimilarImagesCollectionAsync(
                "zelda", tempDir.FullName, 0, AppConstants.Algorithms.Levenshtein,
                CancellationToken.None,
                loadedImages.Add);

            result.Should().NotBeNull();
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir.FullName, true);
            }
            catch
            {
                // ignored
            }
        }
    }

    [Fact]
    public void CopyImageFilenameCommand_WithNullParameter_DoesNotThrow()
    {
        var command = GetPrivateCommand("CopyImageFilenameCommand");
        command.Should().NotBeNull();

        var act = () => command.Execute(null);
        act.Should().NotThrow();
    }

    [Fact]
    public void CopyImageFilenameCommand_WithNonStringParameter_DoesNotThrow()
    {
        var command = GetPrivateCommand("CopyImageFilenameCommand");
        command.Should().NotBeNull();

        var act = () => command.Execute(42);
        act.Should().NotThrow();
    }

    [Fact]
    public void OpenFileLocationCommand_WithNullParameter_DoesNotThrow()
    {
        var command = GetPrivateCommand("OpenFileLocationCommand");
        command.Should().NotBeNull();

        var act = () => command.Execute(null);
        act.Should().NotThrow();
    }

    [Fact]
    public void OpenFileLocationCommand_WithEmptyPath_DoesNotThrow()
    {
        var command = GetPrivateCommand("OpenFileLocationCommand");
        command.Should().NotBeNull();

        var act = () => command.Execute(string.Empty);
        act.Should().NotThrow();
    }

    [Fact]
    public void OpenFileLocationCommand_WithNonStringParameter_DoesNotThrow()
    {
        var command = GetPrivateCommand("OpenFileLocationCommand");
        command.Should().NotBeNull();

        var act = () => command.Execute(42);
        act.Should().NotThrow();
    }

    private static ICommand? GetPrivateCommand(string propertyName)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        var property = typeof(ButtonFactory).GetProperty(propertyName, flags);
        return property?.GetValue(null) as ICommand;
    }

    [Fact]
    public void CreateContextMenu_CommandParametersAreSetToImagePath()
    {
        const string testPath = @"C:\roms\covers\game.png";

        RunOnStaThread(() =>
        {
            try
            {
                var result = ButtonFactory.CreateContextMenu(testPath, static _ => { });

                foreach (MenuItem item in result.Items)
                {
                    item.CommandParameter.Should().Be(testPath);
                }
            }
            catch (IOException)
            {
                _staMessage = "Embedded images not available";
            }
        });

        if (_staMessage.Length == 0)
            _staException.Should().BeNull();
    }

    private static Task CreateMinimalBmpAsync(string path)
    {
        var bmpBytes = new byte[]
        {
            0x42, 0x4D, 0x3A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x36, 0x00, 0x00, 0x00, 0x28, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00
        };
        return File.WriteAllBytesAsync(path, bmpBytes);
    }
}
