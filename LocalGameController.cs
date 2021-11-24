using Playnite;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Playnite.SDK.Plugins;
using InstallButton;

namespace InstallButton
{
    public class LocalInstallController : InstallController
    {
        public LocalInstallController(Game game) : base(game)
        {
            Name = "Install using InstallButton Plugin";
        }

        public IPlayniteAPI Api;

        public override void Install(InstallActionArgs args)
        {
            InstallButton installButton = new InstallButton(Api);
            installButton.GameInstaller();
        }
    }
}
