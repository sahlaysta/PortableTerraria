using Sahlaysta.PortableTerrariaCommon;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sahlaysta.PortableTerrariaCreator
{
    //the main form
    partial class GuiForm1 : Form
    {

        //constructor
        internal GuiForm1()
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

        //terrariaInput
        GuiFileSelectTextBox terrariaInputFSTB;
        GuiDisableableControl terrariaInputDC;
        GuiGroupBox terrariaInputGB;

        //dllInput
        GuiDllGridView dllInputDGV;
        GuiDisableableControl dllInputDC;
        GuiGroupBox dllInputGB;

        //dllRegistry
        GuiDisableableControl dllRegistryDC;
        GuiGroupBox dllRegistryGB;

        //script
        GuiFileSelectTextBox scriptFSTB;
        GuiDisableableControl scriptDC;
        GuiGroupBox scriptGB;

        //create
        Button createBtn;

        void init()
        {
            //build main panel
            var pb = new GuiPanelBuilder(this)
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            //terrariaInput
            const string terrariaInputText = "Terraria (for mods," +
                " tModLoader folder must be inside the folder)";
            terrariaInputFSTB = new GuiFileSelectTextBox()
            {
                FolderSelect = true,
                DialogTitle = terrariaInputText,
                DialogOwner = Handle
            };
            terrariaInputFSTB.Button.Text = "Browse folder";

            terrariaInputDC = new GuiDisableableControl();
            terrariaInputDC.Controls.Add(terrariaInputFSTB);

            terrariaInputGB = new GuiGroupBox()
            {
                Text = terrariaInputText
            };
            terrariaInputGB.Controls.Add(terrariaInputDC);

            pb.AddControl(terrariaInputGB);

            //dllInput
            dllInputDGV = new GuiDllGridView();

            dllInputDC = new GuiDisableableControl();
            dllInputDC.Controls.Add(dllInputDGV);

            dllInputGB = new GuiGroupBox()
            {
                Text = "Terraria DLLs"
            };
            dllInputGB.Controls.Add(dllInputDC);

            pb.Anchor = AnchorStyles.Top | AnchorStyles.Left
                | AnchorStyles.Right | AnchorStyles.Bottom;
            pb.AddControl(dllInputGB);

            //dllRegistry
            dllRegistryDC = new GuiDisableableControl();
            dllRegistryDC.Controls.Add(new UserControl());
            dllRegistryDC.EnableRadioButton.Text = "Yes";
            dllRegistryDC.DisableRadioButton.Text = "No";

            dllRegistryGB = new GuiGroupBox()
            {
                Text = "Register XAudio2_6.dll and xactengine3_6.dll to HKCU"
            };
            dllRegistryGB.Controls.Add(dllRegistryDC);

            pb.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pb.AddControl(dllRegistryGB);

            //script
            const string scriptText = "Script";
            scriptFSTB = new GuiFileSelectTextBox()
            {
                DialogTitle = scriptText,
                FileFilter = "C# script file|*.csx|All files (*.*)|*.*"
            };
            scriptFSTB.Button.Text = "Browse file";

            scriptDC = new GuiDisableableControl();
            scriptDC.Controls.Add(scriptFSTB);
            scriptDC.DisableRadioButton.PerformClick();

            scriptGB = new GuiGroupBox()
            {
                Text = scriptText
            };
            scriptGB.Controls.Add(scriptDC);

            pb.AddControl(scriptGB);

            //the create button
            createBtn = new Button()
            {
                Text = "Create exe"
            };
            createBtn.Click += (o, e) => create();
            pb.AddControl(createBtn);


            //link behavior of dllInputDC and dllRegistryDC
            bool dllRegistryDCWasOn = false;
            dllInputDC.EnableRadioButton.Click += (o, e) =>
            {
                dllRegistryDC.Enabled = true;
                if (dllRegistryDCWasOn)
                {
                    dllRegistryDCWasOn = false;
                    dllRegistryDC.EnableRadioButton.PerformClick();
                }
            };
            dllInputDC.DisableRadioButton.Click += (o, e) =>
            {
                if (dllRegistryDC.EnableRadioButton.Checked)
                {
                    dllRegistryDCWasOn = true;
                    dllRegistryDC.DisableRadioButton.PerformClick();
                }
                dllRegistryDC.Enabled = false;
            };

            //finish main panel
            pb.Finish();

            //form size
            const int minWidth = 500;//form minimum width
            MinimumSize = new Size(minWidth, Height);
            Width = minWidth + 200;
            Height += 20;

            //default focused control
            GuiHelper.RunWhenShown(
                this,
                () => terrariaInputFSTB.TextBox.Focus());
        }

        //create
        void create()
        {
            //errors
            string errorMsg = getInputError();
            if (errorMsg != null)
            {
                GuiHelper.ShowMsg(errorMsg, "Error");
                return;
            }

            //warnings
            IEnumerable<string> warningMsgs = getInputWarnings();
            if (warningMsgs != null)
            {
                foreach (var warning in warningMsgs)
                {
                    if (!GuiHelper.ShowOkCancelMsg(warning, "Warning"))
                        return;
                }
            }

            //output .exe file prompt
            string outputFile = GuiHelper.BrowseFile(
                "Create Portable Terraria",
                "Application|*.exe|All files (*.*)|*.*",
                true);
            if (outputFile == null)
                return;

            //options
            PortableTerrariaLauncherCreator.Options options;
            try
            {
                options = getInputOptions(outputFile);
            }
            catch (Exception)
            {
                GuiHelper.ShowMsg("Failed to handle GUI input");
                return;
            }

            //check output.exe isnt inside terraria path
            string terrPath = options.TerrariaPath;
            if (terrPath != null)
            {
                string fullDir1 =
                    Path.GetFullPath(terrPath) + Path.DirectorySeparatorChar;
                string fullDir2 =
                    Path.GetFullPath(terrPath) + Path.AltDirectorySeparatorChar;
                string fullFile =
                    Path.GetFullPath(outputFile);
                int fullDirLen = fullDir1.Length;
                int fullFileLen = fullFile.Length;
                if (
                    fullDirLen < fullFileLen
                    &&
                    (fullFile.Substring(0, fullDirLen)
                        .Equals(
                            fullDir1,
                            StringComparison.InvariantCultureIgnoreCase)
                    ||fullFile.Substring(0, fullDirLen)
                        .Equals(
                            fullDir2,
                            StringComparison.InvariantCultureIgnoreCase)))
                {
                    GuiHelper.ShowMsg(
                        "The output .exe path cannot" +
                        " be inside Terraria's directory",
                        "Error");
                    return;
                }
            }

            //create + progress bar dialog
            var ptl = new PortableTerrariaLauncherCreator(options);
            string labelText = "Create to\n" + options.OutputPath;
            GuiHelper.RunProgressDialog(labelText, Text, ptl, Handle);
        }

        //checks user GUI input for errors
        string getInputError()
        {
            //check FSTBs
            foreach (var fstb in GuiHelper.GetAllControls<GuiFileSelectTextBox>(this))
            {
                if (!fstb.Enabled)
                    continue;

                //path empty check
                var path = fstb.SelectedPath;
                if (path == null || path.Length == 0)
                {
                    return "Path not entered";
                }

                //path exists check
                if (fstb.FolderSelect)
                {
                    if (!Directory.Exists(path))
                        return "Directory does not exist:\n" + path;
                }
                else
                {
                    if (!File.Exists(path))
                        return "File does not exist:\n" + path;
                }
            }

            //check terraria path
            if (terrariaInputFSTB.Enabled)
            {
                //check terraria.exe
                const string terrariaExe = "Terraria.exe";
                string terrariaPath = terrariaInputFSTB.SelectedPath;
                if (!File.Exists(Path.Combine(terrariaPath, terrariaExe)))
                {
                    return
                        "The file \"" + terrariaExe +
                        "\" does not exist in:\n" + terrariaPath;
                }
            }

            //check DLLs
            if (dllInputDGV.Enabled)
            {
                var dlls = dllInputDGV.Dlls;
                foreach (var dll in dlls)
                {
                    if (!dll.Status)
                        continue;
                    string path = dll.FilePath;
                    if (!File.Exists(path))
                        return "File does not exist:\n" + path;
                }
            }

            return null;
        }

        //checks user GUI input for warnings
        IEnumerable<string> getInputWarnings()
        {
            //terraria folder check warnings
            if (terrariaInputFSTB.Enabled)
            {
                //check tModLoader folder
                const string tModLoaderPath = "tModLoader";
                string terrariaPath = terrariaInputFSTB.SelectedPath;
                string dir = Path.Combine(terrariaPath, tModLoaderPath);
                if (!Directory.Exists(dir))
                {
                    yield return
                        "The folder \"" + tModLoaderPath +
                        "\" does not exist in:\n" + terrariaPath +
                        "\n\nProceed without mods?";
                }
                else
                {
                    //check tModLoader.exe
                    const string tModLoaderExe = "tModLoader.exe";
                    if (!File.Exists(Path.Combine(dir, tModLoaderExe)))
                    {
                        yield return
                            "The folder \"" + tModLoaderPath +
                            "\" exists, but \"" + tModLoaderExe +
                            "\" does not exist inside it." +
                            "\n\nProceed without mods?";
                    }
                }
            }
            else
            {
                yield return
                    "Terraria disabled. Proceed?";
            }

            //dll check warnings
            if (dllInputDGV.Enabled)
            {
                var dlls = dllInputDGV.Dlls;
                int missingDllCount = 0;
                foreach (var dll in dlls)
                {
                    if (!dll.Status)
                    {
                        missingDllCount++;
                    }
                }
                if (missingDllCount == 1)
                {
                    yield return
                        "Missing 1 DLL." +
                        "\n\nProceed anyways?";
                }
                else if (missingDllCount > 1)
                {
                    yield return
                        "Missing " + missingDllCount + " DLLs." +
                        "\n\nProceed anyways?";
                }
            }
            else
            {
                yield return
                    "DLLs disabled. Proceed?";
            }
        }

        //get options from GUI input
        PortableTerrariaLauncherCreator.Options getInputOptions(string outputFile)
        {
            // output filepath check
            if (Directory.Exists(outputFile))
            {
                GuiHelper.ShowMsg("Directory selected", "Error");
            }

            //check disableable controls arent bugged
            foreach (var dc in GuiHelper.GetAllControls<GuiDisableableControl>(this))
            {
                bool enableBtnChecked = dc.EnableRadioButton.Checked;
                bool disableBtnChecked = dc.DisableRadioButton.Checked;
                if ((enableBtnChecked && disableBtnChecked)
                    || !enableBtnChecked && !disableBtnChecked)
                {
                    throw new InvalidOperationException(
                        "Bugged radio buttons of DisableableControl");
                }
            }

            //terraria path
            string terrariaPath;
            if (terrariaInputFSTB.Enabled)
            {
                terrariaPath = terrariaInputFSTB.SelectedPath;
            }
            else
            {
                terrariaPath = null;
            }

            //dlls
            IEnumerable<TerrariaResolver.Dll> dlls;
            if (dllInputDGV.Enabled)
            {
                dlls = dllInputDGV.Dlls;
            }
            else
            {
                dlls = null;
            }

            //registerHKCU
            bool registerHKCU = dllRegistryDC.EnableRadioButton.Checked;

            //script path
            string scriptPath;
            if (scriptFSTB.Enabled)
            {
                scriptPath = scriptFSTB.SelectedPath;
            }
            else
            {
                scriptPath = null;
            }

            return new PortableTerrariaLauncherCreator.Options(
                outputFile, terrariaPath, dlls, registerHKCU, scriptPath);
        }
    }
}
