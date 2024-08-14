using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Sahlaysta.PortableTerrariaCommon;

namespace Sahlaysta.PortableTerrariaCreator
{
    internal class GuiForm : Form
    {

        private readonly TextBox folderSelectTextBox;
        private readonly Button folderSelectButton;
        private readonly GuiTerrariaDllPanel terrariaDllPanel;
        private readonly Button createExeButton;

        private readonly Assembly dotNetZipAssembly;
        private readonly Assembly cecilAssembly;
        private readonly byte[] portableTerrariaLauncherAssembly;

        private string selectedPath;

        public GuiForm()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            Text = "Portable Terraria Creator";
            try
            {
                Icon = Icon.ExtractAssociatedIcon(typeof(Program).Assembly.Location) ?? throw new Exception();
            } catch { }
            ResumeLayout(false);

            string folderSelectTitle = "Terraria folder";
            folderSelectTextBox = new TextBox();
            folderSelectTextBox.ReadOnly = true;

            folderSelectButton = new Button();
            folderSelectButton.Text = "Select folder";
            folderSelectButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            string terrariaDllPanelTitle = "Terraria DLLs";
            terrariaDllPanel = new GuiTerrariaDllPanel();

            createExeButton = new Button();
            createExeButton.Text = "Create launcher EXE";
            createExeButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            Panel panel1 = GuiPanelBuilder.VerticallyCenter(folderSelectButton);
            Panel panel2 = GuiPanelBuilder.VerticallyCenter(folderSelectTextBox);
            Panel panel3 = GuiPanelBuilder.Title(
                GuiPanelBuilder.GlueRightToCenter(panel1, panel2),
                folderSelectTitle);
            Panel panel4 = GuiPanelBuilder.Title(terrariaDllPanel, terrariaDllPanelTitle);
            Panel panel5 = GuiPanelBuilder.HorizontallyCenter(createExeButton);
            Panel panel6 = GuiPanelBuilder.GlueTopToCenter(panel3, panel4);
            Panel panel7 = GuiPanelBuilder.GlueBottomToCenter(panel5, panel6);
            Panel mainPanel = GuiPanelBuilder.Pad(panel7, new Padding(5, 5, 5, 5));
            GuiPanelBuilder.DockToForm(mainPanel, this);

            int tabIndex = 1;
            panel6.TabIndex = tabIndex++;
            panel1.TabIndex = tabIndex++;
            panel2.TabIndex = tabIndex++;
            panel3.TabIndex = tabIndex++;
            panel4.TabIndex = tabIndex++;
            panel5.TabIndex = tabIndex++;
            panel7.TabIndex = tabIndex++;

            MinimumSize = new Size(650, 300);
            Size = new Size(800, 400);

            folderSelectButton.Click += (o, e) =>
            {
                string path = GuiFileDialogs.ShowOpenFolderDialog(this, folderSelectTitle, null);
                if (path != null)
                {
                    selectedPath = path;
                    folderSelectTextBox.Text = path;
                }
            };

            createExeButton.Click += (o, e) => CreateExeButtonClicked();

            try
            {
                dotNetZipAssembly = Assembly.Load(ReadEmbeddedResourceToByteArray(
                    "DotNetZip.dll"));
                cecilAssembly = Assembly.Load(ReadEmbeddedResourceToByteArray(
                    "Mono.Cecil.dll"));
                portableTerrariaLauncherAssembly = ReadEmbeddedResourceToByteArray(
                    "Sahlaysta.PortableTerrariaLauncher.exe");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw e;
            }
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

        private void CreateExeButtonClicked()
        {
            string terrariaDir = selectedPath;
            GuiTerrariaDllPanel.RowInfo[] rowData = terrariaDllPanel.RowData;
            string[] dllFilePaths = rowData.Select(x => x.FilePath).Where(x => x != null).ToArray();

            if (terrariaDir == null)
            {
                MessageBox.Show(
                    "Folder not selected.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(terrariaDir))
            {
                MessageBox.Show(
                    "Directory not found:\n\n" + terrariaDir,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!File.Exists(terrariaDir + "\\Terraria.exe"))
            {
                MessageBox.Show(
                    "\"Terraria.exe\" was not found in:\n\n" + terrariaDir,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (rowData.Any(x => x.FilePath == null))
            {
                int nResolvedDlls = rowData.Count(x => x.FilePath != null);
                int nUnresolvedDlls = rowData.Length - nResolvedDlls;
                int nTotalDlls = rowData.Length;
                if (MessageBox.Show(
                    nResolvedDlls + "/" + nTotalDlls + " DLLs, " + nUnresolvedDlls + " missing.\n\n"
                        + "The game may fail or have issues. Continue anyway?",
                    "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning)
                        != DialogResult.OK)
                {
                    return;
                }
            }

            string exeOutFilePath = GuiFileDialogs.ShowSaveFileDialog(
                this, createExeButton.Text, null, "Executable files (*.exe)|*.exe|All files (*.*)|*.*");
            if (exeOutFilePath == null)
            {
                return;
            }

            if (GetPathRelativity(terrariaDir, exeOutFilePath) == PathRelativity.Path2IsInsidePath1)
            {
                MessageBox.Show(
                    "Cannot save inside the same folder. Please save to a different folder.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            RunCreatePortableTerrariaDialog(this, dotNetZipAssembly, cecilAssembly, portableTerrariaLauncherAssembly,
                exeOutFilePath, terrariaDir, dllFilePaths);
        }

        private enum PathRelativity { Path1IsInsidePath2, Path2IsInsidePath1, Equal, Unrelated, Error }

        private static PathRelativity GetPathRelativity(string path1, string path2)
        {
            try
            {
                string fullPath1 = Directory.GetParent(Path.Combine(Path.GetFullPath(path1), "a")).FullName;
                string fullPath2 = Directory.GetParent(Path.Combine(Path.GetFullPath(path2), "a")).FullName;
                Uri uri1 = new Uri(fullPath1, UriKind.Absolute);
                Uri uri2 = new Uri(fullPath2, UriKind.Absolute);

                if (uri1 == uri2)
                {
                    return PathRelativity.Equal;
                }

                DirectoryInfo directoryInfo;

                directoryInfo = Directory.GetParent(fullPath2);
                while (directoryInfo != null)
                {
                    if (uri1 == new Uri(directoryInfo.FullName, UriKind.Absolute))
                    {
                        return PathRelativity.Path2IsInsidePath1;
                    }
                    directoryInfo = Directory.GetParent(directoryInfo.FullName);
                }

                directoryInfo = Directory.GetParent(fullPath1);
                while (directoryInfo != null)
                {
                    if (uri2 == new Uri(directoryInfo.FullName, UriKind.Absolute))
                    {
                        return PathRelativity.Path1IsInsidePath2;
                    }
                    directoryInfo = Directory.GetParent(directoryInfo.FullName);
                }

                return PathRelativity.Unrelated;
            }
            catch
            {
                return PathRelativity.Error;
            }
        }

        private class CustomForm : Form
        {

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern int SetClassLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

            public void DisableWindowCloseButton()
            {
                SetClassLong(Handle, -26, new IntPtr(base.CreateParams.ClassStyle | 512));
            }

            public void EnableWindowCloseButton()
            {
                SetClassLong(Handle, -26, new IntPtr(base.CreateParams.ClassStyle));
            }

        }

        private static void RunCreatePortableTerrariaDialog(
            IWin32Window window,
            Assembly dotNetZipAssembly,
            Assembly cecilAssembly,
            byte[] portableTerrariaLauncherAssembly,
            string exeOutFilePath,
            string terrariaDir,
            string[] dllFilePaths)
        {
            using (CustomForm form = new CustomForm())
            {
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.SizeGripStyle = SizeGripStyle.Hide;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ShowIcon = false;
                form.ShowInTaskbar = false;
                form.Text = "Create launcher EXE";
                form.DisableWindowCloseButton();

                Label label = new Label();
                label.TextAlign = ContentAlignment.MiddleCenter;
                label.AutoSize = true;
                label.Text = "1%";

                ProgressBar progressBar = new ProgressBar();

                Button okButton = new Button();
                okButton.Text = "OK";
                okButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                okButton.AutoSize = true;
                okButton.Enabled = false;

                Panel panel1 = GuiPanelBuilder.GlueTopToCenter(
                    GuiPanelBuilder.HorizontallyCenter(label), progressBar);
                Panel panel2 = GuiPanelBuilder.GlueBottomToCenter(
                    GuiPanelBuilder.HorizontallyCenter(okButton), panel1);
                Panel mainPanel = GuiPanelBuilder.Pad(panel2, new Padding(5, 5, 5, 5));
                GuiPanelBuilder.DockToForm(mainPanel, form);

                object lockObj = new object();
                bool taskDone = false;

                CreatePortableTerrariaTask.EndedCallback endedCallback = (exception) =>
                {
                    lock (lockObj)
                    {
                        if (taskDone) return;
                        taskDone = true;
                    }

                    form.Invoke(new Action(() =>
                    {
                        if (exception != null)
                        {
                            Console.Error.WriteLine(exception);
                            MessageBox.Show(
                                exception.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            form.Close();
                        }
                        else
                        {
                            progressBar.Value = 1;
                            progressBar.Maximum = 1;
                            label.Text = "100%";
                            form.EnableWindowCloseButton();
                            okButton.Enabled = true;
                            okButton.Click += (o, e) => form.Close();
                            okButton.Focus();
                        }
                    }));
                };

                CreatePortableTerrariaTask.ProgressCallback progressCallback = (nDataProcessed, nDataTotal) =>
                {
                    lock (lockObj)
                    {
                        if (taskDone) return;
                    }

                    form.Invoke(new Action(() =>
                    {
                        if (progressBar.Maximum != nDataTotal)
                            progressBar.Maximum = nDataTotal;
                        if (progressBar.Value != nDataProcessed)
                            progressBar.Value = nDataProcessed;
                        int percentage = nDataTotal == 0 ? 0 :
                            (int)(((nDataProcessed * 1.0) / (nDataTotal * 1.0)) * 100.0);
                        if (percentage < 1) percentage = 1;
                        if (percentage > 99) percentage = 99;
                        label.Text = percentage + "%";
                    }));
                };

                CreatePortableTerrariaTask.RunInNewThread(
                    dotNetZipAssembly, cecilAssembly, portableTerrariaLauncherAssembly, exeOutFilePath, terrariaDir,
                    dllFilePaths, endedCallback, progressCallback);

                form.Size = new Size(250, 100);
                form.ShowDialog(window);
            }
        }

    }
}