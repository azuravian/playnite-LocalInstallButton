using Playnite;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InstallButton;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Management.Automation;
using System.Management;

namespace InstallButton
{
    public class InstallButton : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        
        private InstallButtonSettings settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("fd5887bb-2da2-4044-bea3-9896aea2f5b8");

        public InstallButton(IPlayniteAPI api) : base(api)
        {
            settings = new InstallButtonSettings(this);

            AddCustomElementSupport(new AddCustomElementSupportArgs
            {
                ElementList = new List<string> { "PluginButton" },
                SourceName = "PluginButton"
            });
        }

        public override IEnumerable<TopPanelItem> GetTopPanelItems()
        {
            return new List<TopPanelItem>()
            {
                new TopPanelItem
                {
                    Icon = new TextBlock
                    {
                        Text = "\uEF04",
                        FontSize = 22,
                        FontFamily = ResourceProvider.GetResource("FontIcoFont") as FontFamily
                    },
                    Title = "Install Game",
                    Activated = () =>
                    {
                        GameInstaller();
                    }
                }
            };
        }

        public override IEnumerable<InstallController> GetInstallActions(GetInstallActionsArgs args)
        {
            string PluginIdTest = new string(args.Game.PluginId.ToString().Take(8).ToArray());
            if (PluginIdTest != "00000000")
            {
                yield break;
            }

            yield return new LocalInstallController(args.Game);
        }


        public void GameInstaller()
        {
            IEnumerable<Game> selection = PlayniteApi.MainView.SelectedGames;

            string gameImagePath = null;
            List<string> driveList = new List<string>();
            List<string> driveList2 = new List<string>();
            string command = null;
            string setupFile = null;
            string driveLetter = null;

            if (selection.Count() > 1)
            {
                PlayniteApi.Dialogs.ShowErrorMessage("Only one game can be installed at a time.", "Too Many Games Selected");
                return;
            }
            List<Game> gameList = selection.ToList();
            Game selectedGame = gameList[0];
            
            string PluginIdTest = new string(selectedGame.PluginId.ToString().Take(8).ToArray());
            
            if (PluginIdTest != "00000000")
            {
                PlayniteApi.Dialogs.ShowErrorMessage("This is a Library Controlled game.  Please use the standard install button.", "Library Controlled Game");
                return;
            }
            


            try
            {
                var gameRoms = selectedGame.Roms.ToList();
                gameImagePath = gameRoms[0].Path;
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
            }

            if (String.IsNullOrEmpty(gameImagePath))
            {
                var response = PlayniteApi.Dialogs.ShowMessage("The installation path is empty.\nDo you want to specify the location of the installation media?", "No Installation Path", MessageBoxButton.YesNo);
                if (response == MessageBoxResult.Yes)
                {
                    gameImagePath = PlayniteApi.Dialogs.SelectFolder();
                }
            }
            else
            {
                if (Path.GetFileName(gameImagePath).EndsWith(".iso"))
                {
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        driveList.Add(drive.Name);
                    }
                    PowerShell mountedDisk = PowerShell.Create();
                    {
                        mountedDisk.AddCommand("Mount-DiskImage");
                        mountedDisk.AddArgument(gameImagePath);
                        mountedDisk.Invoke();
                    }
                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        driveList2.Add(drive.Name);
                    }
                    foreach (var i in driveList2)
                    {
                        if (driveList.Contains(i))
                        {
                            continue;
                        }
                        else
                        {
                            command = i + "\\Setup.exe";
                            driveLetter = i;
                        }
                    }
                }
                else if (Path.GetFileName(gameImagePath).EndsWith(".exe"))
                {
                    command = gameImagePath;
                }
                else if (!Path.HasExtension(gameImagePath))
                {
                    if (!Directory.Exists(gameImagePath))
                    {
                        PlayniteApi.Dialogs.ShowErrorMessage("The file/folder specified in the installation path does not exist.", "Invalid Path");
                        return;
                    }
                    setupFile = Path.Combine(gameImagePath, "setup.exe");
                    if (File.Exists(setupFile))
                    {
                        command = setupFile;
                    }
                    else
                    {
                        String[] Files = Directory.GetFiles(gameImagePath, "*.exe");

                        if (Files.Count() > 1)
                        {
                            MessageBoxResult result = PlayniteApi.Dialogs.ShowMessage("More than 1 .exe in folder.  Would you like to select the appropriate .exe?", "Too many programs", MessageBoxButton.YesNo);
                            if (result == MessageBoxResult.Yes)
                            {
                                command = PlayniteApi.Dialogs.SelectFile("Installer|*.exe");
                            }
                            else
                            {
                                return;
                            }
                        }
                        else if (Files.Count().ToString() == "0")
                        {
                            PlayniteApi.Dialogs.ShowErrorMessage("No executables found in folder.  Check Rom Path.", "No Executables.");
                            return;
                        }
                        else
                        {
                            command = Files[0];
                        }
                    }
                }
            }
            try
            {
                using (Process p = new Process())
                {
                    String dpath = "";
                    p.StartInfo.FileName = command;
                    p.StartInfo.UseShellExecute = false;
                    if (driveLetter != null)
                    {
                        dpath = driveLetter;
                    }
                    else if (Path.HasExtension(gameImagePath))
                    {
                        dpath = Path.GetDirectoryName(gameImagePath);
                    }
                    else
                    {
                        dpath = gameImagePath;
                    }
                    p.StartInfo.WorkingDirectory = dpath;
                    p.Start();
                    p.WaitForExit();
                }
            }
            catch
            {
                return;
            }
            if (gameImagePath.EndsWith(".iso"))
            {
                PowerShell dismountDisk = PowerShell.Create();
                {
                    dismountDisk.AddCommand("Dismount-DiskImage");
                    dismountDisk.AddArgument(gameImagePath);
                    dismountDisk.Invoke();
                }
            }

            String gameExe = PlayniteApi.Dialogs.SelectFile("Game Executable|*.exe");
            if (!String.IsNullOrEmpty(gameExe))
            {
                GameAction action = new GameAction();
                
                
                
                try
                {
                    action.Type = GameActionType.File;
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                }
                action.Path = Path.GetFileName(gameExe);
                action.WorkingDir = "{InstallDir}";
                action.Name = "Play";
                action.TrackingMode = TrackingMode.Default;
                action.IsPlayAction = true;
                
                if (selectedGame.GameActions == null)
                {
                    selectedGame.GameActions = new System.Collections.ObjectModel.ObservableCollection<GameAction>();                    
                }
                try
                {
                    selectedGame.GameActions.AddMissing(action);
                }
                catch (Exception ex)
                {
                    logger.Error(ex.ToString());
                    PlayniteApi.Dialogs.ShowErrorMessage("There was an error creating the Game Action.  Please check the Playnite log for details.", "Action Failed");
                    return;
                }
                selectedGame.IsInstalled = true;
                selectedGame.InstallDirectory = Path.GetDirectoryName(gameExe);
                PlayniteApi.Database.Games.Update(selectedGame);
            }
        }
    }

    public class LocalInstallController : InstallController
    {
        private CancellationTokenSource watcherToken;

        public LocalInstallController(Game game) : base(game)
        {
            Name = "Install using InstallButton Plugin";
        }

        public IPlayniteAPI Api;

        public override void Install(InstallActionArgs args)
        {
            Dispose();

            InstallButton installButton = new InstallButton(Api);
            installButton.GameInstaller();

            IEnumerable<Game> selection = Api.MainView.SelectedGames;
            List<Game> gameList = selection.ToList();
            Game selectedGame = gameList[0];

            StartInstallWatcher(selectedGame);
        }

        public async void StartInstallWatcher(Game game)
        {
            watcherToken = new CancellationTokenSource();

            await Task.Run(async () =>
            {
                while (true)
                {
                    if (watcherToken.IsCancellationRequested)
                    {
                        return;
                    }


                    if (game.InstallDirectory == null)
                    {
                        await Task.Delay(10000);
                        continue;
                    }
                    else
                    {
                        var installInfo = new GameInstallationData()
                        {
                            InstallDirectory = game.InstallDirectory
                        };

                        InvokeOnInstalled(new GameInstalledEventArgs(installInfo));
                        return;
                    }
                }
            });
        }
    }
}