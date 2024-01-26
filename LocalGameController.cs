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

namespace InstallButton
{
    public class LocalInstallController : InstallController
    {

        private CancellationTokenSource watcherToken;
        private InstallButton pluginInstance;

        public LocalInstallController(Game game, InstallButton instance) : base(game)
        {
            Name = "Install using InstallButton Plugin";
            pluginInstance = instance;
        }

        public override void Dispose()
        {
            watcherToken?.Cancel();
        }

        public override void Install(InstallActionArgs args)
        {
            pluginInstance.GameInstaller(Game);
            StartInstallWatcher();
        }

        public async void StartInstallWatcher()
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

                    if (Game.InstallDirectory == null)
                    {
                        await Task.Delay(10000);
                        continue;
                    }
                    else
                    {
                        var installInfo = new GameInstallationData()
                        {
                            InstallDirectory = Game.InstallDirectory
                        };

                        InvokeOnInstalled(new GameInstalledEventArgs(installInfo));
                        return;
                    }
                }
            });
        }
    }

    public class LocalUninstallController : UninstallController
    {

        private CancellationTokenSource watcherToken;
        private InstallButton pluginInstance;

        public LocalUninstallController(Game game, InstallButton instance) : base(game)
        {
            Name = "Uninstall using InstallButton Plugin";
            pluginInstance = instance;
        }

        public override void Dispose()
        {
            watcherToken?.Cancel();
        }

        public override void Uninstall(UninstallActionArgs args)
        {
            pluginInstance.GameUninstaller(Game);
            StartUninstallWatcher();
        }

        public async void StartUninstallWatcher()
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

                    if (Game.InstallDirectory != null)
                    {
                        await Task.Delay(10000);
                        continue;
                    }
                    else
                    {
                        InvokeOnUninstalled(new GameUninstalledEventArgs());
                        return;
                    }
                }
            });
        }
    }
} 