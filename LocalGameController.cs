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

        public LocalInstallController(Game game) : base(game)
        {
            Name = "Install using InstallButton Plugin";
        }

        public IPlayniteAPI Api;

        public override void Dispose()
        {
            watcherToken?.Cancel();
        }

        public override void Install(InstallActionArgs args)
        {
            InstallButton installButton = new InstallButton(Api);
            installButton.GameInstaller(Game);

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
} 