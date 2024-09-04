using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Sahlaysta.PortableTerrariaCommon;

namespace Sahlaysta.PortableTerrariaLauncher
{

    /// <summary>
    /// Portable Terraria Launcher GUI.
    /// </summary>
    internal class GuiForm : Form
    {

        private readonly TextBox exefileTextBox;
        private readonly Button exefileButton;
        private readonly Button installButton;
        private readonly Button installAudioButton;
        private readonly TextBox savedirTextBox;
        private readonly Button savedirButton;
        private readonly Button savedirresetButton;
        private readonly Button playButton;
        private readonly Button playotherButton;
        private readonly ContextMenuStrip playotherMenu;
        private readonly ToolStripMenuItem playconsoleMenuItem;
        private readonly ToolStripMenuItem forcecloseMenuItem;

        private readonly Panel terrariapathPanel;
        private readonly Panel savepathPanel;
        private readonly Panel playbuttonsPanel;

        private readonly Font savedirTextBoxNormalFont;
        private readonly Font savedirTextBoxItalicFont;
        private readonly Font playButtonNormalFont;
        private readonly Font playButtonItalicFont;

        private readonly Assembly dotNetZipAssembly;

        private string exeFile;
        private string saveDir;

        private bool isPlaying;

        public GuiForm()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            Text = "Portable Terraria Launcher";
            try
            {
                Icon = Icon.ExtractAssociatedIcon(typeof(Program).Assembly.Location) ?? throw new Exception();
            } catch { }
            ResumeLayout(false);

            exefileTextBox = new TextBox();
            exefileTextBox.ReadOnly = true;

            exefileButton = new Button();
            exefileButton.Text = "Change";
            exefileButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            installButton = new Button();
            installButton.Text = "Install";
            installButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            installAudioButton = new Button();
            installAudioButton.Text = "Install audio";
            installAudioButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            savedirTextBox = new TextBox();
            savedirTextBox.ReadOnly = true;
            savedirTextBoxNormalFont = savedirTextBox.Font;
            savedirTextBoxItalicFont = new Font(savedirTextBoxNormalFont, FontStyle.Italic);

            savedirButton = new Button();
            savedirButton.Text = "Change";
            savedirButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            savedirresetButton = new Button();
            savedirresetButton.Text = "Default";
            savedirresetButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            playButton = new Button();
            playButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            playButtonNormalFont = playButton.Font;
            playButtonItalicFont = new Font(playButtonNormalFont, FontStyle.Italic);

            playotherButton = new Button();
            playotherButton.Text = "...";
            playotherButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            playotherMenu = new ContextMenuStrip();

            playconsoleMenuItem = new ToolStripMenuItem("Play (command line)");
            playotherMenu.Items.Add(playconsoleMenuItem);

            forcecloseMenuItem = new ToolStripMenuItem("Force close");
            playotherMenu.Items.Add(forcecloseMenuItem);

            Panel panel1 = PanelBuilder.VerticallyCenter(savedirTextBox);
            Panel panel2 = PanelBuilder.VerticallyCenter(savedirButton);
            Panel panel3 = PanelBuilder.VerticallyCenter(savedirresetButton);
            Panel panel4 = PanelBuilder.GlueBottomToCenter(panel3, panel2);
            Panel panel5 = PanelBuilder.Title(
                PanelBuilder.GlueRightToCenter(panel4, panel1),
                "Save path");
            Panel panel6 = PanelBuilder.VerticallyCenter(playotherButton);
            Panel panel7 = PanelBuilder.GlueRightToCenter(panel6, playButton);
            Panel panel8 = PanelBuilder.GlueBottomToCenter(installButton, exefileButton);
            Panel panel9 = PanelBuilder.GlueBottomToCenter(installAudioButton, panel8);
            Panel panel10 = PanelBuilder.VerticallyCenter(panel9);
            Panel panel11 = PanelBuilder.VerticallyCenter(exefileTextBox);
            Panel panel12 = PanelBuilder.Title(
                PanelBuilder.GlueRightToCenter(panel10, panel11),
                "Terraria path");
            Panel panel13 = PanelBuilder.GlueBottomToCenter(panel7, panel5);
            Panel panel14 = PanelBuilder.GlueBottomToCenter(panel13, panel12);
            Panel mainPanel = PanelBuilder.Pad(
                PanelBuilder.VerticallyCenter(panel14), new Padding(5, 5, 5, 5));
            PanelBuilder.DockToForm(mainPanel, this);

            terrariapathPanel = panel12;
            savepathPanel = panel5;
            playbuttonsPanel = panel7;

            int tabIndex = 1;
            panel14.TabIndex = tabIndex++;
            panel12.TabIndex = tabIndex++;
            panel10.TabIndex = tabIndex++;
            panel9.TabIndex = tabIndex++;
            panel8.TabIndex = tabIndex++;
            exefileButton.TabIndex = tabIndex++;
            installButton.TabIndex = tabIndex++;
            installAudioButton.TabIndex = tabIndex++;
            panel11.TabIndex = tabIndex++;
            playButton.TabIndex = tabIndex++;
            exefileTextBox.TabIndex = tabIndex++;
            panel4.TabIndex = tabIndex++;
            panel2.TabIndex = tabIndex++;
            panel3.TabIndex = tabIndex++;
            panel1.TabIndex = tabIndex++;
            panel5.TabIndex = tabIndex++;
            panel7.TabIndex = tabIndex++;
            panel6.TabIndex = tabIndex++;
            panel13.TabIndex = tabIndex++;

            try
            {
                dotNetZipAssembly = Assembly.Load(ManifestResources.ReadByteArray("DotNetZip.dll"));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw e;
            }

            try
            {
                GuiLauncherAssemblyReader.GetLauncherGuid();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }

            exefileButton.Click += (o, e) =>
            {
                string file = GuiFileDialogs.ShowOpenFileDialog(this,
                    "Select \"Terraria.exe\" or \"tModLoader.exe\"", null,
                    "Executable files (*.exe)|*.exe|All files (*.*)|*.*");
                if (file != null)
                {
                    SetExeFile(file);
                    TryWritePrefs();
                }
            };

            savedirButton.Click += (o, e) =>
            {
                string dir = GuiFileDialogs.ShowOpenFolderDialog(this, "Save path", null);
                if (dir != null)
                {
                    SetSaveDir(dir);
                    TryWritePrefs();
                }
            };

            savedirresetButton.Click += (o, e) =>
            {
                SetSaveDir(null);
                TryWritePrefs();
            };

            playButton.Click += (o, e) =>
            {
                Play(false);
            };

            playconsoleMenuItem.Click += (o, e) =>
            {
                Play(true);
            };

            forcecloseMenuItem.Click += (o, e) =>
            {
                ForceClose();
            };

            playotherButton.Click += (o, e) =>
            {
                playotherMenu.Show(playotherButton, new Point(0, playotherButton.Height));
            };

            installButton.Click += (o, e) =>
            {
                InstallButtonClicked();
            };

            installAudioButton.Click += (o, e) =>
            {
                InstallAudioButtonClicked();
            };

            forcecloseMenuItem.Enabled = false;

            SetExeFile(null);
            SetSaveDir(null);
            UpdateFormIsPlaying(false, false);

            TryReadPrefs();

            MinimumSize = Size;
            Width = 450;

            Shown += (o, e) =>
            {
                if (playButton.Enabled)
                {
                    playButton.Select();
                }
            };

        }

        private void SetExeFile(string file)
        {
            exeFile = file;
            exefileTextBox.Text = file ?? "";
            if (file == null)
            {
                playbuttonsPanel.Enabled = false;
                playconsoleMenuItem.Enabled = false;
            }
            else
            {
                playbuttonsPanel.Enabled = true;
                playconsoleMenuItem.Enabled = true;
            }
        }

        private void SetSaveDir(string dir)
        {
            saveDir = dir;
            if (dir == null)
            {
                savedirTextBox.Text = "(Default)";
                savedirTextBox.Font = savedirTextBoxItalicFont;
            }
            else
            {
                savedirTextBox.Text = dir;
                savedirTextBox.Font = savedirTextBoxNormalFont;
            }
        }

        private void UpdateFormIsPlaying(bool isPlaying, bool commandLine)
        {
            this.isPlaying = isPlaying;
            terrariapathPanel.Enabled = !isPlaying;
            savepathPanel.Enabled = !isPlaying;
            playButton.Text = !isPlaying ? "Play" : "Playing";
            playButton.Font = !isPlaying ? playButtonNormalFont : playButtonItalicFont;
            playconsoleMenuItem.Enabled = !isPlaying;
            forcecloseMenuItem.Enabled = isPlaying && !commandLine;
        }

        private void Play(bool commandLine)
        {
            if (!File.Exists(exeFile))
            {
                MessageBox.Show("File not found:\n" + exeFile, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (GuiTerrariaLauncher.IsActive()) return;

            GuiTerrariaLauncher.StartedCallback startedCallback = () =>
            {
                BeginInvoke(new Action(() =>
                {
                    UpdateFormIsPlaying(true, commandLine);
                }));
            };

            GuiTerrariaLauncher.EndedCallback endedCallback = () =>
            {
                BeginInvoke(new Action(() =>
                {
                    UpdateFormIsPlaying(false, commandLine);
                }));
            };

            GuiTerrariaLauncher.ErrorCallback errorCallback = exception =>
            {
                BeginInvoke(new Action(() =>
                {
                    Console.Error.WriteLine(exception);
                    MessageBox.Show(exception.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
            };

            GuiTerrariaLauncher.LaunchTerraria(exeFile, saveDir, commandLine,
                startedCallback, endedCallback, errorCallback);
        }

        private void ForceClose()
        {
            GuiTerrariaLauncher.KillStartedProcess();
        }

        private void InstallButtonClicked()
        {
            string path = GuiFileDialogs.ShowOpenFolderDialog(this,
                "Select install folder", null);
            if (path != null)
            {
                string installDir = Path.Combine(path, "Terraria");

                if (Directory.Exists(installDir))
                {
                    MessageBox.Show("The folder already exists:\n" + installDir,
                        installButton.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                using (GuiProgressDialog progressDialog = new GuiProgressDialog(installButton.Text))
                {
                    GuiTerrariaInstaller.EndedCallback endedCallback = exception =>
                    {
                        if (exception == null)
                        {
                            BeginInvoke(new Action(() =>
                            {
                                if (File.Exists(Path.Combine(installDir, "tModLoader.exe")))
                                {
                                    SetExeFile(Path.Combine(installDir, "tModLoader.exe"));
                                }
                                else
                                {
                                    SetExeFile(Path.Combine(installDir, "Terraria.exe"));
                                }
                                TryWritePrefs();
                            }));
                        }

                        progressDialog.InvokeSetProgressEnd(exception);
                    };

                    GuiTerrariaInstaller.ProgressCallback progressCallback = (dataProcessed, dataTotal) =>
                    {
                        progressDialog.InvokeSetProgress(dataProcessed, dataTotal);
                    };

                    GuiTerrariaInstaller.RunInNewThread(
                        dotNetZipAssembly, installDir, endedCallback, progressCallback);

                    progressDialog.ShowDialog(this);
                }
            }
        }

        private void InstallAudioButtonClicked()
        {
            bool success = false;
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrEmpty(localAppData))
                {
                    throw new Exception("LocalAppData null");
                }
                string audioDir = Path.Combine(localAppData, "DirectXAudioRegistry");
                Directory.CreateDirectory(audioDir);

                string xaudio26dllName = "XAudio2_6.dll";
                string xactengine36dllName = "xactengine3_6.dll";
                string[] dllNames = new string[] { xaudio26dllName, xactengine36dllName };
                string[] dllFilepaths = new string[] {
                    Path.Combine(audioDir, xaudio26dllName), Path.Combine(audioDir, xactengine36dllName) };
                for (int i = 0; i < 2; i++)
                {
                    string dllName = dllNames[i];
                    string dllFilepath = dllFilepaths[i];

                    if (exeFile != null)
                    {
                        string exeContainedDir = Path.GetDirectoryName(exeFile);
                        string path = exeContainedDir == null ? dllName : Path.Combine(exeContainedDir, dllName);
                        if (File.Exists(path))
                        {
                            File.Copy(path, dllFilepath, true);
                            continue;
                        }
                    }

                    using (Stream zipStream = new GuiLauncherAssemblyReader.DotNetZipCompatibilityStream(
                        GuiLauncherAssemblyReader.GetZipStream()))
                    {
                        string dllEntryName = "Dlls/" + dllName;
                        string[] entryNames = DotNetZip.ReadZipArchiveEntryNames(dotNetZipAssembly, zipStream);
                        if (entryNames.Contains(dllEntryName))
                        {
                            zipStream.Position = 0;
                            DotNetZip.DelegateExtractEntry entryExtractor =
                                (string entryName, out Stream stream, out bool closeStream) =>
                                {
                                    stream = File.Open(dllFilepath, FileMode.Create, FileAccess.Write);
                                    closeStream = true;
                                };
                            DotNetZip.ExtractZipArchive(
                                dotNetZipAssembly, zipStream, new string[] { dllEntryName }, entryExtractor, null);
                            continue;
                        }
                    }

                    TerrariaDllResolver.Dll dll = TerrariaDllResolver.Dll.All.FirstOrDefault(x => x.Name == dllName);
                    if (dll != null)
                    {
                        string resolvedDllFilepath = TerrariaDllResolver.FindDllFilepathOnSystem(dll, null);
                        if (resolvedDllFilepath != null)
                        {
                            File.Copy(resolvedDllFilepath, dllFilepath, true);
                            continue;
                        }
                    }

                    throw new Exception("DLL not found: " + dllName);
                }

                DirectXAudioRegistry.RegisterAudioDllsToSystemRegistry(dllFilepaths[0], dllFilepaths[1]);

                success = true;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                MessageBox.Show(e.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (success)
            {
                MessageBox.Show("Success", installAudioButton.Text);
            }
        }

        private void TryWritePrefs()
        {
            try
            {
                WritePrefs();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }

        private void TryReadPrefs()
        {
            try
            {
                ReadPrefs();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }

        private void WritePrefs()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(localAppData))
            {
                throw new Exception("LocalAppData null");
            }

            string launcherGuid = GuiLauncherAssemblyReader.GetLauncherGuid();
            string preferencesDir =
                Path.Combine(localAppData, "PortableTerrariaLauncher", "Preferences", launcherGuid);
            Directory.CreateDirectory(preferencesDir);

            Dictionary<string, string> prefs = new Dictionary<string, string>
            {
                { "exeFile", exeFile },
                { "saveDir", saveDir }
            };
            foreach (KeyValuePair<string, string> entry in prefs)
            {
                string filepath = Path.Combine(preferencesDir, entry.Key + ".pref");
                string prefValue = entry.Value;

                if (prefValue == null)
                {
                    File.Delete(filepath);
                }
                else
                {
                    using (FileStream fileStream = File.Open(filepath, FileMode.Create, FileAccess.Write))
                    using (StreamWriter streamWriter = new StreamWriter(fileStream, new UTF8Encoding(false, true)))
                    {
                        streamWriter.Write(prefValue);
                    }
                }
            }
        }

        private void ReadPrefs()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(localAppData))
            {
                throw new Exception("LocalAppData null");
            }

            string launcherGuid = GuiLauncherAssemblyReader.GetLauncherGuid();
            string preferencesDir =
                Path.Combine(localAppData, "PortableTerrariaLauncher", "Preferences", launcherGuid);

            if (!Directory.Exists(preferencesDir))
            {
                return;
            }

            foreach (string key in new string[] { "exeFile", "saveDir" })
            {
                string filepath = Path.Combine(preferencesDir, key + ".pref");
                if (File.Exists(filepath))
                {
                    string prefValue;
                    using (FileStream fileStream = File.OpenRead(filepath))
                    using (StreamReader streamReader = new StreamReader(fileStream, new UTF8Encoding(false, true)))
                    {
                        prefValue = streamReader.ReadToEnd();
                    }

                    switch (key)
                    {
                        case "exeFile":
                            SetExeFile(prefValue);
                            break;
                        case "saveDir":
                            SetSaveDir(prefValue);
                            break;
                    }
                }
            }
        }

    }
}