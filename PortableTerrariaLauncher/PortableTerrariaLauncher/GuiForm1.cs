using Sahlaysta.PortableTerrariaCommon;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sahlaysta.PortableTerrariaLauncher
{
    //the main form
    partial class GuiForm1 : Form
    {
        //terraria process state enum
        enum TerrariaState
        {
            Closed,
            Open,
            Loading
        }

        //constructor
        public GuiForm1()
        {
            InitializeComponent();
            using (var stream = GuiHelper.GetResourceStream("icon.ico"))
            {
                Icon = new Icon(stream);
            }
            init();
            FormClosed += (o, e) => Application.Exit();
        }

        //initializations
        GuiFileSelectTextBox fstbSaveFolder;
        Button playBtn;
        Button playWithModsBtn;
        Button miscBtn;
        ContextMenuStrip miscMenu;
        void init()
        {
            //build main panel
            var pb = new GuiPanelBuilder(this);

            //save folder selection
            var gb = new GuiGroupBox()
            {
                Text = "Save folder"
            };
            fstbSaveFolder = new GuiFileSelectTextBox()
            {
                FolderSelect = true
            };
            fstbSaveFolder.TextBox.ReadOnly = true;
            fstbSaveFolder.Button.Text = "Change save folder";
            fstbSaveFolder.PathChanged += (o, e) =>
            {
                //rw prefs
                var tp = PortableTerrariaLauncherPreferences.OpenReadWrite();
                using (tp)
                {
                    tp.TerrariaSaveDirectory = e.SelectedPath;
                }
            };
            fstbSaveFolder.ButtonClick += (o, e) =>
            {
                //if terraria is open
                if (terrariaIsRunning())
                {
                    e.Cancel = true;
                    GuiHelper.ShowMsg(
                        "Terraria is currently running",
                        fstbSaveFolder.Button.Text);
                }
            };
            gb.Controls.Add(fstbSaveFolder);
            pb.AddControl(gb);

            //play buttons
            pb.Anchor = AnchorStyles.Left | AnchorStyles.Bottom
                | AnchorStyles.Right;
            const int buttonPanelHeight = 35;
            var buttonPanel = new UserControl()
            {
                MinimumSize = new Size(0, buttonPanelHeight),
                MaximumSize = new Size(int.MaxValue, buttonPanelHeight)
            };
            playBtn = new Button()
            {
                Text = "Play",
                MinimumSize = new Size(0, buttonPanelHeight)
            };
            playBtn.Click += (o, e) => play(false, playBtn);
            playWithModsBtn = new Button()
            {
                Text = "Play with mods",
                MinimumSize = new Size(0, buttonPanelHeight)
            };
            playWithModsBtn.Click += (o, e) => play(true, playWithModsBtn);
            miscBtn = new Button()
            {
                Text = "...",
                MinimumSize = new Size(0, buttonPanelHeight)
            };
            miscBtn.Click += (o, e) =>
            {
                miscMenu.Show(MousePosition.X, MousePosition.Y);
            };
            buttonPanel.Controls.Add(playBtn);
            buttonPanel.Controls.Add(playWithModsBtn);
            buttonPanel.Controls.Add(miscBtn);
            buttonPanel.Resize += (o, e) =>
            {
                //button positioning + resize
                int boundsWidth = buttonPanel.Bounds.Width;
                int boundsHeight = buttonPanel.Bounds.Height;
                const int gap = 4; //gap between buttons

                //miscBtn positioning + resize
                const int miscBtnWidth = 35;
                miscBtn.Size = new Size(miscBtnWidth, boundsHeight);
                miscBtn.Location = new Point(
                    boundsWidth - miscBtn.Bounds.Width,
                    0);

                //playBtn positioning + resize
                int playBtnWidth =
                    (buttonPanel.Bounds.Width - miscBtnWidth) / 2;
                playBtn.Location = new Point(0, 0);
                playBtn.Size = new Size(
                    playBtnWidth,
                    boundsHeight);

                //playWithModsBtn positioning + resize
                playWithModsBtn.Location = new Point(
                    gap + playBtnWidth,
                    0);
                playWithModsBtn.Size = new Size(
                    boundsWidth - playBtnWidth - miscBtnWidth - (gap * 2),
                    boundsHeight);
            };
            pb.AddControl(buttonPanel);

            //finish panel
            pb.Finish();

            //misc context menu
            miscMenu = new ContextMenuStrip();
            var forceClose = new ToolStripMenuItem("Force close Terraria");
            forceClose.Click += (o, e) => forceCloseTerraria(forceClose.Text);
            var installPath = new ToolStripMenuItem("Open installation folder");
            installPath.Click += (o, e) => openInstallPath(installPath.Text);
            var uninstall = new ToolStripMenuItem("Uninstall Terraria");
            uninstall.Click += (o, e) => uninstallTerraria(uninstall.Text);
            miscMenu.Items.Add(forceClose);
            miscMenu.Items.Add(installPath);
            miscMenu.Items.Add(uninstall);

            //form properties
            MinimumSize = new Size(400, Bounds.Height);
            Size = new Size(500, MinimumSize.Height);
            GuiHelper.RunWhenShown(
                this,
                () => playBtn.Focus()); //default focus
            if (!refreshPrefs())
            {
                //failed to load prefs at initialization
                GuiHelper.ShowMsg(
                    "Failed to access prefs, try restarting PC",
                    "Error");
                GuiHelper.RunWhenShown(this, () => Close());
            }
        }

        //prefs
        bool refreshPrefs()
        {
            //load prefs
            try
            {
                var tp = PortableTerrariaLauncherPreferences.Read();
                using (tp)
                {
                    installed = tp.IsTerrariaInstalled;
                    saveDir = tp.TerrariaSaveDirectory;
                }
            }
            catch (Exception)
            {
                //failed to read prefs
                return false;
            }

            //update gui
            fstbSaveFolder.SelectedPath = saveDir;
            return true;
        }

        //refresh prefs with gui error msg
        bool refreshPrefsWithErrorMsg()
        {
            //read prefs
            if (!refreshPrefs())
            {
                //failed to read prefs
                GuiHelper.ShowMsg(
                    "Error reading prefs.xml",
                    "Error");
                return false;
            }
            return true;
        }

        //if terraria is open
        bool terrariaIsRunning()
        {
            switch (terrariaState.Value)
            {
                case TerrariaState.Open:
                case TerrariaState.Loading:
                    return true;
                default:
                    return false;
            }
        }

        //check terraria is installed and not running with gui msg
        bool checkTerrariaIsInstalledAndNotRunning(
            string title,
            bool skipRunningCheck = false)
        {
            if (!skipRunningCheck && terrariaIsRunning())
            {
                GuiHelper.ShowMsg(
                    "Terraria is currently running.",
                    title);
                return false;
            }
            if (!refreshPrefsWithErrorMsg())
                return false;
            if (!installed)
            {
                GuiHelper.ShowMsg(
                    "Terraria is not installed." +
                    " Click 'Play' to install",
                    title);
                return false;
            }
            return true;
        }

        //launch terraria
        void play(bool mods, Button btn)
        {
            //if terraria already open
            if (terrariaIsRunning())
            {
                GuiHelper.ShowMsg(
                    "Terraria is currently running",
                    btn.Text);
                return;
            }

            //read prefs
            if (!refreshPrefsWithErrorMsg())
                return;

            //prompt installation if not installed
            if (!installed)
            {
                if (GuiHelper.ShowYesNoPrompt(
                    "Terraria is not installed. Install?",
                    btn.Text))
                {
                    installTerraria();
                }
                return;
            }

            //gui upd
            terrariaState.Value = TerrariaState.Loading;
            string oldBtnTxt = btn.Text;
            btn.Text = "Loading...";

            //launch terraria
            var tl = new TerrariaLauncher(installDir, saveDir, mods);
            var s = new AtomicObj<(bool, Exception)>();
            tl.Launched += (o, e) =>//for the wait
            {
                lock (s)
                {
                    s.Value = (true, e.Error);
                    Monitor.PulseAll(s);
                }
            };
            tl.Opened += (o, e) =>//window opened
            {
                terrariaState.Value = TerrariaState.Open;
                GuiHelper.RunCrossThread(
                    btn,
                    () => btn.Text = "Terraria is open");
            };
            tl.Closed += (o, e) =>//terraria exited
            {
                terrariaState.Value = TerrariaState.Closed;
                GuiHelper.RunCrossThread(
                    btn,
                    () => btn.Text = oldBtnTxt);
            };
            tl.LaunchAsync();
            atl.Value = tl;

            //wait for launch
            lock (s)
            {
                while (!s.Value.Item1)
                {
                    Monitor.Wait(s);
                }
            }

            //error msg
            Exception error = s.Value.Item2;
            if (error != null)
            {
                GuiHelper.ShowMsg(
                    "Launch error:\n\n" + error,
                    "Error");
            }
        }

        //install terraria
        void installTerraria()
        {
            //install terraria
            var ti = new TerrariaInstaller(installDir);
            GuiHelper.RunProgressDialog("Terraria", Text, ti, Handle);
        }


        // misc context menu actions
        //force closes terraria
        void forceCloseTerraria(string title)
        {
            if (!checkTerrariaIsInstalledAndNotRunning(title, true))
                return;
            if (!terrariaIsRunning())
            {
                GuiHelper.ShowMsg(
                    "Terraria is not running",
                    title);
                return;
            }
            try
            {
                atl.Value.Process.Kill();
            }
            catch (Exception)
            {
                GuiHelper.ShowMsg(
                    "Failed to end process",
                    title);
            }
        }
        //opens the install path
        void openInstallPath(string title)
        {
            if (!checkTerrariaIsInstalledAndNotRunning(title, true))
                return;
            try
            {
                Process.Start("explorer.exe", installDir);
            }
            catch (Exception)
            {
                GuiHelper.ShowMsg(
                    "Failed to open explorer",
                    title);
            }
        }
        //uninstalls terraria
        void uninstallTerraria(string title)
        {
            if (!checkTerrariaIsInstalledAndNotRunning(title))
                return;

            //prompt
            if (!GuiHelper.ShowYesNoPrompt("Uninstall Terraria?", title))
                return;

            //uninstall terraria
            var tu = new TerrariaUninstaller(installDir);
            GuiHelper.RunProgressDialog("Uninstall", Text, tu, Handle);
        }


        string saveDir;
        bool installed;
        readonly AtomicObj<TerrariaLauncher> atl =
            new AtomicObj<TerrariaLauncher>();
        readonly AtomicObj<TerrariaState> terrariaState =
            new AtomicObj<TerrariaState>();
        static readonly string installDir =
            Path.Combine(FileHelper.ApplicationFolder, "Terraria");
    }
}
