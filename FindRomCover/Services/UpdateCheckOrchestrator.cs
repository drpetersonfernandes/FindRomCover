using FindRomCover.Models;

namespace FindRomCover.Services;

public class UpdateCheckOrchestrator
{
    private readonly GitHubReleaseService _releaseService;

    public UpdateCheckOrchestrator(GitHubReleaseService releaseService)
    {
        _releaseService = releaseService;
    }

    public async Task<UpdateNotificationInfo> CheckAsync()
    {
        var result = await _releaseService.CheckForUpdatesAsync();

        if (result is { UpdateAvailable: true, ReleaseUrl: not null, LatestVersion: not null })
        {
            return new UpdateNotificationInfo
            {
                ShouldNotify = true,
                Message = $"A new version of FindRomCover is available!\n\n" +
                          $"Current version: {result.CurrentVersion}\n" +
                          $"Latest version: {result.LatestVersion}\n\n" +
                          "Would you like to open the release page?",
                ReleaseUrl = result.ReleaseUrl
            };
        }

        return UpdateNotificationInfo.NoUpdate;
    }
}
