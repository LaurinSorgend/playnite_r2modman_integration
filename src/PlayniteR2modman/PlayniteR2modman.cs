using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PlayniteR2modman
{
    public class PlayniteR2modman : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static readonly Guid SteamLibraryPluginId = Guid.Parse("cb91dfc9-b977-43bf-8e70-55f46e410fab");

        private PlayniteR2modmanSettingsViewModel settings { get; set; }
        private readonly GameMappingStore mappingStore;
        private readonly R2modmanScanner scanner = new R2modmanScanner();
        private readonly GaleGameCatalog galeCatalog;
        private readonly GaleScanner galeScanner = new GaleScanner();

        public override Guid Id { get; } = Guid.Parse("66e5c414-0820-4919-be6d-3179c4013fc1");

        public PlayniteR2modman(IPlayniteAPI api) : base(api)
        {
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
            settings = new PlayniteR2modmanSettingsViewModel(this);
            mappingStore = new GameMappingStore(GetPluginInstallPath());
            galeCatalog = new GaleGameCatalog(GetPluginInstallPath());
            try
            {
                ReloadMappings();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to load r2modman mappings during plugin init");
            }
        }

        public void ReloadMappings()
        {
            var disabled = settings.Settings.BuildDisabledMap();
            mappingStore.Reload(settings.Settings.BuildOverrideMap(), disabled);
            galeCatalog.Reload(disabled);
        }

        public override IEnumerable<PlayController> GetPlayActions(GetPlayActionsArgs args)
        {
            if (args?.Game == null || !settings.Settings.EnableInjection || args.Game.PluginId != SteamLibraryPluginId)
            {
                return Enumerable.Empty<PlayController>();
            }
            var appId = args.Game.GameId;
            if (string.IsNullOrWhiteSpace(appId))
            {
                return Enumerable.Empty<PlayController>();
            }
            return BuildR2modmanActions(args.Game, appId).Concat(BuildGaleActions(args.Game, appId)).ToList();
        }

        private IEnumerable<PlayController> BuildR2modmanActions(Game game, string appId)
        {
            if (!mappingStore.TryGet(appId, out var mapping))
            {
                return Enumerable.Empty<PlayController>();
            }

            IReadOnlyList<DiscoveredProfile> profiles;
            try
            {
                profiles = scanner.ScanGame(settings.Settings.DataFolderOverride, mapping.R2modmanInternalName, appId);
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to scan r2modman profiles for {mapping.DisplayName} ({appId})");
                return Enumerable.Empty<PlayController>();
            }
            logger.Info($"Injecting {profiles.Count} r2modman play action(s) for {mapping.DisplayName} ({appId}).");

            return profiles.Select(profile => new AutomaticPlayController(game)
            {
                Type = AutomaticPlayActionType.Url,
                Name = $"Modded ({profile.ProfileName}) via r2modman",
                Path = profile.SteamRunUrl,
                TrackingMode = TrackingMode.Default
            }).ToList();
        }

        private IEnumerable<PlayController> BuildGaleActions(Game game, string appId)
        {
            if (!settings.Settings.EnableGale || !galeCatalog.TryGet(appId, out var galeGame))
            {
                return Enumerable.Empty<PlayController>();
            }

            var profiles = galeScanner.ListProfiles(settings.Settings.GaleDataFolderOverride, galeGame.GaleSlug);
            if (profiles.Count == 0)
            {
                return Enumerable.Empty<PlayController>();
            }
            if (!GaleLauncher.TryResolveExecutable(settings.Settings.GaleExecutableOverride, out var executable, out var error))
            {
                logger.Warn($"Gale profiles exist for {galeGame.DisplayName} ({appId}) but Gale can't be launched: {error}");
                return Enumerable.Empty<PlayController>();
            }
            logger.Info($"Injecting {profiles.Count} Gale play action(s) for {galeGame.DisplayName} ({appId}).");

            return profiles
                .Select(profile => GaleLauncher.BuildPlayController(game, executable, galeGame.GaleSlug, profile))
                .ToList();
        }

        private string GetPluginInstallPath()
        {
            return System.IO.Path.GetDirectoryName(typeof(PlayniteR2modman).Assembly.Location);
        }

        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {
            // Add code to be executed when game is finished installing.
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            // Add code to be executed when game is started running.
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            // Add code to be executed when Playnite is initialized.
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            // Add code to be executed when Playnite is shutting down.
        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            // Add code to be executed when library is updated.
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new PlayniteR2modmanSettingsView();
        }
    }
}