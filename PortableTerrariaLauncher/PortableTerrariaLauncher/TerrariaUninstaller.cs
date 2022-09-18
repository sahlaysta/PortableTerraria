using Sahlaysta.PortableTerrariaCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sahlaysta.PortableTerrariaLauncher
{
    //uninstalls terraria
    class TerrariaUninstaller : GuiProgressibleOperation
    {
        readonly string installDir;

        //constructor
        public TerrariaUninstaller(string installationDirectory)
        {
            installDir = installationDirectory;
        }

        //uninstall
        protected override void run()
        {
            //rw prefs
            var prefs = PortableTerrariaLauncherPreferences.OpenReadWrite();
            using (prefs)
            {
                //prefs set to not installed
                prefs.IsTerrariaInstalled = false;

                //delete installation directory
                void progressChanged(int numerator, int denominator)
                {
                    progress(numerator, denominator);
                }
                bool requestCancel()
                {
                    return cancelRequested();
                }
                FileHelper.DeleteDirectory(
                    installDir, progressChanged, requestCancel);
            }
        }
    }
}
