using Sahlaysta.PortableTerrariaCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sahlaysta.PortableTerrariaLauncher
{
    //launches terraria
    class TerrariaLauncher
    {
        //terraria launched eventargs
        public class LaunchedEventArgs : EventArgs
        {
            public LaunchedEventArgs(Exception error) => _error = error;
            public Exception Error => _error;
            readonly Exception _error;
        }
        //terraria intptrhandle eventargs
        public class OpenedEventArgs : EventArgs
        {
            public OpenedEventArgs() { }
        }
        //terraria closed eventargs
        public class ClosedEventArgs : EventArgs
        {
            public ClosedEventArgs() { }
        }

        //constructor
        public TerrariaLauncher(
            string installationDirectory,
            string saveDirectory,
            bool playWithMods)
        {
            installDir = installationDirectory;
            saveDir = saveDirectory;
            mods = playWithMods;
        }

        //public operations
        public bool Ended => ended.Value;
        public Process Process => ended.Value ? null : process;
        public event EventHandler<LaunchedEventArgs> Launched;
        public event EventHandler<OpenedEventArgs> Opened;
        public event EventHandler<ClosedEventArgs> Closed;
        public void LaunchAsync()
        {
            Exception error = null;
            Task.Run(() =>
            {
                try
                {
                    launch();
                }
                catch (Exception e)
                {
                    error = e;
                }

                Launched?.Invoke(null, new LaunchedEventArgs(error));
                if (error != null)
                {
                    ended.Value = true;
                    Closed?.Invoke(null, new ClosedEventArgs());
                }
            });
        }
        void launch()
        {
            var xthis = this;

            //terraria exe
            string exe = getTerrariaExe();
            if (!File.Exists(exe))
            {
                throw new FileNotFoundException(
                    "File does not exist:\n\n" + exe);
            }

            //terraria process
            process = new Process();

            //process timer
            AtomicObj<bool> opened = new AtomicObj<bool>();
            var procTimer = new System.Timers.Timer()
            {
                Interval = 300
            };
            procTimer.Elapsed += (o, e) =>
            {
                lock (opened)
                {
                    if (opened.Value)
                        return;
                    process.Refresh();
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        opened.Value = true;
                        if (procTimer.Enabled)
                            procTimer.Stop();
                        procTimer.Dispose();
                        Opened?.Invoke(xthis, new OpenedEventArgs());
                    }
                }
            };

            //start process
            process.StartInfo.FileName = exe;
            process.StartInfo.Arguments = "-savedirectory \"" + saveDir + "\"";
            process.StartInfo.WorkingDirectory =
                mods ? Path.Combine(installDir, "tModLoader") : installDir;
            process.EnableRaisingEvents = true;
            process.Exited += (o, e) =>
            {
                ended.Value = true;
                if (procTimer.Enabled)
                    procTimer.Stop();
                procTimer.Dispose();
                Closed?.Invoke(xthis, new ClosedEventArgs());
            };
            process.Start();

            //start timer
            procTimer.Start();
        }

        //get terraria process file exe
        string getTerrariaExe()
        {
            return
                mods
                ? Path.Combine(Path.Combine(
                    installDir, "tModLoader"), "tModLoader.exe")
                : Path.Combine(
                    installDir, "Terraria.exe");
        }

        volatile Process process;
        readonly AtomicObj<bool> ended = new AtomicObj<bool>();
        readonly string installDir, saveDir;
        readonly bool mods;
    }
}
