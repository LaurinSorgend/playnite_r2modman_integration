using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace PlayniteR2modman
{
    public class UserMappingEntry : ObservableObject
    {
        private string steamAppId = string.Empty;
        private string r2modmanInternalName = string.Empty;
        private string displayName = string.Empty;
        private bool isDisabled;

        public string SteamAppId { get => steamAppId; set => SetValue(ref steamAppId, value); }
        public string R2modmanInternalName { get => r2modmanInternalName; set => SetValue(ref r2modmanInternalName, value); }
        public string DisplayName { get => displayName; set => SetValue(ref displayName, value); }
        public bool IsDisabled { get => isDisabled; set => SetValue(ref isDisabled, value); }
    }

    public class PlayniteR2modmanSettings : ObservableObject
    {
        private bool enableInjection = true;
        private string dataFolderOverride = string.Empty;
        private bool enableGale = true;
        private string galeDataFolderOverride = string.Empty;
        private string galeExecutableOverride = string.Empty;
        private ObservableCollection<UserMappingEntry> userMappings = new ObservableCollection<UserMappingEntry>();

        public bool EnableInjection { get => enableInjection; set => SetValue(ref enableInjection, value); }
        public string DataFolderOverride { get => dataFolderOverride; set => SetValue(ref dataFolderOverride, value); }
        public bool EnableGale { get => enableGale; set => SetValue(ref enableGale, value); }
        public string GaleDataFolderOverride { get => galeDataFolderOverride; set => SetValue(ref galeDataFolderOverride, value); }
        public string GaleExecutableOverride { get => galeExecutableOverride; set => SetValue(ref galeExecutableOverride, value); }
        public ObservableCollection<UserMappingEntry> UserMappings { get => userMappings; set => SetValue(ref userMappings, value); }

        public Dictionary<string, UserMappingOverride> BuildOverrideMap() =>
            UserMappings
                .Where(m => !string.IsNullOrWhiteSpace(m.SteamAppId) && !string.IsNullOrWhiteSpace(m.R2modmanInternalName))
                .GroupBy(m => m.SteamAppId.Trim())
                .ToDictionary(
                    g => g.Key,
                    g => new UserMappingOverride
                    {
                        R2modmanInternalName = g.Last().R2modmanInternalName.Trim(),
                        DisplayName = g.Last().DisplayName
                    },
                    StringComparer.Ordinal);

        public Dictionary<string, bool> BuildDisabledMap() =>
            UserMappings
                .Where(m => !string.IsNullOrWhiteSpace(m.SteamAppId) && m.IsDisabled)
                .GroupBy(m => m.SteamAppId.Trim())
                .ToDictionary(g => g.Key, g => true, StringComparer.Ordinal);
    }

    public class PlayniteR2modmanSettingsViewModel : ObservableObject, ISettings
    {
        private readonly PlayniteR2modman plugin;
        private PlayniteR2modmanSettings editingClone;
        private PlayniteR2modmanSettings settings;
        private string scanStatus = string.Empty;

        public PlayniteR2modmanSettings Settings
        {
            get => settings;
            set { settings = value; OnPropertyChanged(); }
        }

        [DontSerialize]
        public string ScanStatus
        {
            get => scanStatus;
            set { scanStatus = value; OnPropertyChanged(); }
        }

        [DontSerialize]
        public ICommand ScanCommand { get; }

        [DontSerialize]
        public ICommand ReloadCommand { get; }

        [DontSerialize]
        public ICommand AddMappingCommand { get; }

        [DontSerialize]
        public ICommand RemoveMappingCommand { get; }

        public PlayniteR2modmanSettingsViewModel(PlayniteR2modman plugin)
        {
            this.plugin = plugin;
            var saved = plugin.LoadPluginSettings<PlayniteR2modmanSettings>();
            Settings = saved ?? new PlayniteR2modmanSettings();
            ScanCommand = new RelayCommandImpl(_ => RunScan());
            ReloadCommand = new RelayCommandImpl(_ => plugin.ReloadMappings());
            AddMappingCommand = new RelayCommandImpl(_ => Settings.UserMappings.Add(new UserMappingEntry()));
            RemoveMappingCommand = new RelayCommandImpl(p =>
            {
                if (p is UserMappingEntry entry)
                {
                    Settings.UserMappings.Remove(entry);
                }
            });
        }

        public void BeginEdit() => editingClone = Serialization.GetClone(Settings);
        public void CancelEdit()
        {
            Settings = editingClone;
            plugin.ReloadMappings();
        }
        public void EndEdit()
        {
            plugin.SavePluginSettings(Settings);
            plugin.ReloadMappings();
        }
        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            if (!R2modmanScanner.TryResolveDataFolder(Settings.DataFolderOverride, out _, out var folderError))
            {
                errors.Add($"Data folder override: {folderError}");
            }
            ValidateGale(errors);
            ValidateMappings(errors);
            return errors.Count == 0;
        }

        private void ValidateGale(List<string> errors)
        {
            if (!GaleScanner.TryResolveDataFolder(Settings.GaleDataFolderOverride, out _, out var folderError))
            {
                errors.Add($"Gale data folder override: {folderError}");
            }
            if (string.IsNullOrWhiteSpace(Settings.GaleExecutableOverride))
            {
                return;
            }
            if (!PathResolver.TryResolve(Settings.GaleExecutableOverride, null, out var exe, out var exeError))
            {
                errors.Add($"Gale executable: {exeError}");
            }
            else if (!System.IO.File.Exists(exe))
            {
                errors.Add($"Gale executable: '{exe}' does not exist.");
            }
        }

        private void ValidateMappings(List<string> errors)
        {
            var seenAppIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in Settings.UserMappings)
            {
                var appId = (row.SteamAppId ?? string.Empty).Trim();
                var folder = (row.R2modmanInternalName ?? string.Empty).Trim();
                if (appId.Length == 0 && folder.Length == 0)
                {
                    continue;
                }
                if (appId.Length == 0)
                {
                    errors.Add($"Mapping for '{folder}': Steam AppId is required.");
                    continue;
                }
                if (!appId.All(char.IsDigit))
                {
                    errors.Add($"Mapping '{appId}': Steam AppId must be numeric.");
                    continue;
                }
                if (!seenAppIds.Add(appId))
                {
                    errors.Add($"Mapping '{appId}': duplicate Steam AppId.");
                }
                if (folder.Length == 0 && !row.IsDisabled)
                {
                    errors.Add($"Mapping '{appId}': r2modman folder name is required unless the row is disabled.");
                }
                else if (folder.Length > 0 && !R2modmanScanner.IsValidFolderName(folder))
                {
                    errors.Add($"Mapping '{appId}': '{folder}' is not a valid folder name.");
                }
            }
        }

        private void RunScan()
        {
            var folders = new R2modmanScanner().ListGameFolders(Settings.DataFolderOverride);
            var resolved = string.IsNullOrWhiteSpace(Settings.DataFolderOverride)
                ? R2modmanScanner.DefaultDataFolder
                : Settings.DataFolderOverride;
            ScanStatus = folders.Count == 0
                ? $"No folders found under {resolved}"
                : $"Detected {folders.Count} folders under {resolved}: {string.Join(", ", folders)}";
        }
    }

    internal class RelayCommandImpl : ICommand
    {
        private readonly Action<object> execute;
        private readonly Func<object, bool> canExecute;
        public RelayCommandImpl(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }
        public bool CanExecute(object parameter) => canExecute?.Invoke(parameter) ?? true;
        public void Execute(object parameter) => execute(parameter);
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
