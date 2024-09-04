using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Sahlaysta.PortableTerrariaCommon;

namespace Sahlaysta.PortableTerrariaCreator
{

    /// <summary>
    /// Portable Terraria Creator GUI.
    /// </summary>
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

            Panel panel1 = PanelBuilder.VerticallyCenter(folderSelectButton);
            Panel panel2 = PanelBuilder.VerticallyCenter(folderSelectTextBox);
            Panel panel3 = PanelBuilder.Title(
                PanelBuilder.GlueRightToCenter(panel1, panel2),
                folderSelectTitle);
            Panel panel4 = PanelBuilder.Title(terrariaDllPanel, terrariaDllPanelTitle);
            Panel panel5 = PanelBuilder.HorizontallyCenter(createExeButton);
            Panel panel6 = PanelBuilder.GlueTopToCenter(panel3, panel4);
            Panel panel7 = PanelBuilder.GlueBottomToCenter(panel5, panel6);
            Panel mainPanel = PanelBuilder.Pad(panel7, new Padding(5, 5, 5, 5));
            PanelBuilder.DockToForm(mainPanel, this);

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
                dotNetZipAssembly = Assembly.Load(ManifestResources.ReadByteArray("DotNetZip.dll"));
                cecilAssembly = Assembly.Load(ManifestResources.ReadByteArray("Mono.Cecil.dll"));
                portableTerrariaLauncherAssembly = ManifestResources.ReadByteArray(
                    "Sahlaysta.PortableTerrariaLauncher.exe");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw e;
            }
        }

        private void CreateExeButtonClicked()
        {
            string terrariaDir = selectedPath;

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
                    "Directory not found:\n" + terrariaDir,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!File.Exists(terrariaDir + "\\Terraria.exe"))
            {
                MessageBox.Show(
                    "\"Terraria.exe\" was not found in:\n" + terrariaDir,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            GuiTerrariaDllPanel.RowInfo[] rowData = terrariaDllPanel.RowData;
            if (rowData.Any(x => x.Filepath == null))
            {
                int nResolvedDlls = rowData.Count(x => x.Filepath != null);
                int nUnresolvedDlls = rowData.Length - nResolvedDlls;
                int nTotalDlls = rowData.Length;
                if (MessageBox.Show(
                    nResolvedDlls + "/" + nTotalDlls + " DLLs, " + nUnresolvedDlls + " missing.\n"
                        + "The game may fail or have issues. Continue anyway?",
                    "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning)
                        != DialogResult.OK)
                {
                    return;
                }
            }

            string exeOutFile = GuiFileDialogs.ShowSaveFileDialog(
                this, createExeButton.Text, null, "Executable files (*.exe)|*.exe|All files (*.*)|*.*");
            if (exeOutFile == null)
            {
                return;
            }

            if (PathRelativity.RelatePaths(terrariaDir, exeOutFile) == PathRelativity.Result.Path2IsInsidePath1)
            {
                MessageBox.Show(
                    "Cannot save inside the same folder. Please save to a different folder.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Dictionary<TerrariaDllResolver.Dll, string> dllFilepaths =
                rowData
                .Where(x => x.Filepath != null)
                .ToDictionary(x => x.Dll, x => x.Filepath);

            using (GuiProgressDialog progressDialog = new GuiProgressDialog(createExeButton.Text))
            {
                GuiLauncherAssemblyWriter.EndedCallback endedCallback = exception =>
                {
                    progressDialog.InvokeSetProgressEnd(exception);
                };

                GuiLauncherAssemblyWriter.ProgressCallback progressCallback = (dataProcessed, dataTotal) =>
                {
                    progressDialog.InvokeSetProgress(dataProcessed, dataTotal);
                };

                GuiLauncherAssemblyWriter.RunInNewThread(
                    dotNetZipAssembly, cecilAssembly, portableTerrariaLauncherAssembly, exeOutFile,
                    terrariaDir, dllFilepaths, endedCallback, progressCallback);

                progressDialog.ShowDialog(this);
            }
        }

    }
}