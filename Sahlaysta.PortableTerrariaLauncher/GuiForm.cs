using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Sahlaysta.PortableTerrariaCommon;

namespace Sahlaysta.PortableTerrariaLauncher
{
    internal class GuiForm : Form
    {

        private readonly TextBox installpathTextBox;
        private readonly Button installpathButton;
        private readonly Button installButton;
        private readonly Button installAudioButton;
        private readonly TextBox savepathTextBox;
        private readonly Button savepathButton;
        private readonly Button savepathresetButton;
        private readonly Button playButton;
        private readonly Button playotherButton;
        private readonly ContextMenuStrip playotherMenu;
        private readonly ToolStripMenuItem playconsoleMenuItem;
        private readonly ToolStripMenuItem forcecloseMenuItem;

        private readonly Panel installpathPanel;
        private readonly Panel savepathPanel;
        private readonly Panel playbuttonsPanel;

        private readonly Assembly dotNetZipAssembly;

        private readonly TerrariaLauncher terrariaLauncher = new TerrariaLauncher();

        private string installPath;
        private string savePath;

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

            installpathTextBox = new TextBox();
            installpathTextBox.ReadOnly = true;

            installpathButton = new Button();
            installpathButton.Text = "Change";
            installpathButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            installButton = new Button();
            installButton.Text = "Install";
            installButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            installAudioButton = new Button();
            installAudioButton.Text = "Install audio";
            installAudioButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            savepathTextBox = new TextBox();
            savepathTextBox.ReadOnly = true;

            savepathButton = new Button();
            savepathButton.Text = "Change";
            savepathButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            savepathresetButton = new Button();
            savepathresetButton.Text = "Default";
            savepathresetButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            playButton = new Button();
            playButton.Text = "Play";
            playButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            playotherButton = new Button();
            playotherButton.Text = "...";
            playotherButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            playotherMenu = new ContextMenuStrip();

            playconsoleMenuItem = new ToolStripMenuItem("Play (show command line)");
            playotherMenu.Items.Add(playconsoleMenuItem);

            forcecloseMenuItem = new ToolStripMenuItem("Force close");
            playotherMenu.Items.Add(forcecloseMenuItem);

            Panel panel1 = GuiPanelBuilder.VerticallyCenter(savepathTextBox);
            Panel panel2 = GuiPanelBuilder.VerticallyCenter(savepathButton);
            Panel panel3 = GuiPanelBuilder.VerticallyCenter(savepathresetButton);
            Panel panel4 = GuiPanelBuilder.GlueBottomToCenter(panel3, panel2);
            Panel panel5 = GuiPanelBuilder.Title(
                GuiPanelBuilder.GlueRightToCenter(panel4, panel1),
                "Save path");
            Panel panel6 = GuiPanelBuilder.VerticallyCenter(playotherButton);
            Panel panel7 = GuiPanelBuilder.GlueRightToCenter(panel6, playButton);
            Panel panel8 = GuiPanelBuilder.GlueBottomToCenter(installButton, installpathButton);
            Panel panel9 = GuiPanelBuilder.GlueBottomToCenter(installAudioButton, panel8);
            Panel panel10 = GuiPanelBuilder.VerticallyCenter(panel9);
            Panel panel11 = GuiPanelBuilder.VerticallyCenter(installpathTextBox);
            Panel panel12 = GuiPanelBuilder.Title(
                GuiPanelBuilder.GlueRightToCenter(panel10, panel11),
                "Terraria path");
            Panel panel13 = GuiPanelBuilder.GlueBottomToCenter(panel7, panel5);
            Panel panel14 = GuiPanelBuilder.GlueBottomToCenter(panel13, panel12);
            Panel mainPanel = GuiPanelBuilder.Pad(
                GuiPanelBuilder.VerticallyCenter(panel14), new Padding(5, 5, 5, 5));
            GuiPanelBuilder.DockToForm(mainPanel, this);

            installpathPanel = panel12;
            savepathPanel = panel5;
            playbuttonsPanel = panel7;

            int tabIndex = 1;
            panel14.TabIndex = tabIndex++;
            panel12.TabIndex = tabIndex++;
            panel10.TabIndex = tabIndex++;
            panel9.TabIndex = tabIndex++;
            panel8.TabIndex = tabIndex++;
            installpathButton.TabIndex = tabIndex++;
            installButton.TabIndex = tabIndex++;
            installAudioButton.TabIndex = tabIndex++;
            panel11.TabIndex = tabIndex++;
            playButton.TabIndex = tabIndex++;
            installpathTextBox.TabIndex = tabIndex++;
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
                dotNetZipAssembly = Assembly.Load(ReadEmbeddedResourceToByteArray("DotNetZip.dll"));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw e;
            }

            installpathButton.Click += (o, e) =>
            {
                string path = GuiFileDialogs.ShowOpenFileDialog(this,
                    "Select \"Terraria.exe\" or \"tModLoader.exe\"", null,
                    "Executable files (*.exe)|*.exe|All files (*.*)|*.*");
                if (path != null)
                {
                    SetInstallPath(path);
                    TryWritePrefs();
                }
            };

            savepathButton.Click += (o, e) =>
            {
                string path = GuiFileDialogs.ShowOpenFolderDialog(this, "Save path", null);
                if (path != null)
                {
                    SetSavePath(path);
                    TryWritePrefs();
                }
            };

            savepathresetButton.Click += (o, e) =>
            {
                SetSavePath(null);
                TryWritePrefs();
            };

            playButton.Click += (o, e) =>
            {
                terrariaLauncher.Launch(installPath, savePath, false, null);
            };

            playconsoleMenuItem.Click += (o, e) =>
            {
                terrariaLauncher.Launch(installPath, savePath, true, null);
            };

            forcecloseMenuItem.Click += (o, e) =>
            {
                terrariaLauncher.TryKill();
            };

            playotherButton.Click += (o, e) =>
            {
                playotherMenu.Show(playotherButton, new Point(0, playotherButton.Height));
            };

            forcecloseMenuItem.Enabled = false;

            SetInstallPath(null);

            MinimumSize = Size;
            Width = 450;

        }

        private static byte[] ReadEmbeddedResourceToByteArray(string embeddedResourceName)
        {
            byte[] byteArray;
            using (MemoryStream memoryStream = new MemoryStream())
            using (Stream embeddedResourceStream =
                typeof(Program).Assembly.GetManifestResourceStream(embeddedResourceName))
            {
                if (embeddedResourceStream == null)
                {
                    throw new ArgumentException("Resource not found: " + embeddedResourceName);
                }
                embeddedResourceStream.CopyTo(memoryStream);
                byteArray = memoryStream.ToArray();
            }
            return byteArray;
        }

        private void SetInstallPath(string path)
        {
            installPath = path;
            installpathTextBox.Text = path ?? "";
            if (path == null)
            {
                installAudioButton.Enabled = false;
                playbuttonsPanel.Enabled = false;
                playconsoleMenuItem.Enabled = false;
            }
            else
            {
                installAudioButton.Enabled = true;
                playbuttonsPanel.Enabled = true;
                playconsoleMenuItem.Enabled = true;
            }
            SetSavePath(null);
        }

        private void SetSavePath(string path)
        {
            savePath = path;
            savepathTextBox.Text = path ?? "";
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

        private void WritePrefs()
        {

        }

    }
}