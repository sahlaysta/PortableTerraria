using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Sahlaysta.PortableTerrariaCommon;

namespace Sahlaysta.PortableTerrariaCreator
{
    internal class GuiTerrariaDllPanel : Panel
    {

        private readonly Button autoImportButton;
        private readonly Button importButton;
        private readonly Button unimportButton;

        private readonly DataGridView dataGridView;
        private readonly DataGridViewColumn statusColumn;
        private readonly DataGridViewLinkColumn sourceColumn;
        private readonly DataGridViewColumn filenameColumn;
        private readonly DataGridViewColumn filepathColumn;

        public readonly RowInfo[] RowData;

        public class RowInfo
        {

            public readonly TerrariaDllResolver.Dll Dll;
            public string FilePath;

            public RowInfo(TerrariaDllResolver.Dll dll)
            {
                Dll = dll;
            }

        }

        public GuiTerrariaDllPanel()
        {
            dataGridView = new DataGridView();
            dataGridView.ReadOnly = true;
            dataGridView.BorderStyle = BorderStyle.None;
            dataGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dataGridView.AllowUserToAddRows = false;
            dataGridView.AllowUserToResizeRows = false;
            dataGridView.AllowUserToResizeColumns = false;
            dataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridView.RowHeadersVisible = false;
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView.EnableHeadersVisualStyles = false;
            dataGridView.MultiSelect = false;

            statusColumn = new DataGridViewColumn(new DataGridViewTextBoxCell());
            statusColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView.Columns.Add(statusColumn);

            sourceColumn = new DataGridViewLinkColumn();
            sourceColumn.HeaderText = "DLL Program";
            sourceColumn.ActiveLinkColor = sourceColumn.LinkColor;
            sourceColumn.VisitedLinkColor = sourceColumn.LinkColor;
            dataGridView.Columns.Add(sourceColumn);

            filenameColumn = new DataGridViewColumn(new DataGridViewTextBoxCell());
            filenameColumn.HeaderText = "DLL name";
            dataGridView.Columns.Add(filenameColumn);

            filepathColumn = new DataGridViewColumn(new DataGridViewTextBoxCell());
            filepathColumn.HeaderText = "DLL file path";
            dataGridView.Columns.Add(filepathColumn);

            dataGridView.CellContentDoubleClick += (o, e) =>
            {
                DataGridViewColumn column = dataGridView.Columns[e.ColumnIndex];
                if (object.ReferenceEquals(column, sourceColumn))
                {
                    RowInfo rowInfo = RowData[e.RowIndex];
                    TerrariaDllResolver.DllSource dllSource = rowInfo.Dll.Source;
                    string websiteLink = DllSourceToWebsiteLink(dllSource);
                    try
                    {
                        Process process = Process.Start(websiteLink);
                        process?.Dispose();
                    }
                    catch (Exception ex) { Console.Error.WriteLine(ex); }
                }
            };

            RowData = TerrariaDllResolver.Dll.All.Select(x => new RowInfo(x)).ToArray();
            foreach (RowInfo rowInfo in RowData)
            {
                int rowIndex = dataGridView.Rows.Add();
                DataGridViewRow row = dataGridView.Rows[rowIndex];
                UpdateRow(row);
            }

            AutoImportDlls(false);

            autoImportButton = new Button();
            importButton = new Button();
            unimportButton = new Button();

            autoImportButton.Text = "Auto-import DLLs";
            importButton.Text = "Import DLLs from folder";
            unimportButton.Text = "Unimport all";

            autoImportButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            importButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            unimportButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            autoImportButton.Click += (o, e) => AutoImportDlls(true);
            importButton.Click += (o, e) => ImportDllsFromFolder();
            unimportButton.Click += (o, e) => ResetDlls();

            Panel panel1 = GuiPanelBuilder.GlueTopToCenter(autoImportButton, importButton);
            Panel panel2 = GuiPanelBuilder.VerticallyCenter(
                GuiPanelBuilder.GlueTopToCenter(panel1, unimportButton));
            Panel panel3 = GuiPanelBuilder.GlueRightToCenter(panel2, dataGridView);

            int tabIndex = 1;
            dataGridView.TabIndex = tabIndex++;
            panel3.TabIndex = tabIndex++;
            panel1.TabIndex = tabIndex++;
            panel2.TabIndex = tabIndex++;
            autoImportButton.TabIndex = tabIndex++;
            importButton.TabIndex = tabIndex++;
            unimportButton.TabIndex = tabIndex++;

            Controls.Add(panel3);
            panel3.Dock = DockStyle.Fill;

        }

        private void UpdateRow(DataGridViewRow row)
        {
            RowInfo rowInfo = RowData[row.Index];
            row.SetValues(new object[] {
                rowInfo.FilePath != null ? "✓" : null,
                DllSourceToName(rowInfo.Dll.Source),
                rowInfo.Dll.Name,
                rowInfo.FilePath
            });
            row.Cells[0].Style.ForeColor = Color.Green;

            int nResolvedDlls = dataGridView.Rows.Cast<DataGridViewRow>().Count(x => x.Cells[0].Value != null);
            int nTotalDlls = dataGridView.Rows.Count;
            statusColumn.HeaderText = nResolvedDlls + "/" + nTotalDlls;
        }

        private static string DllSourceToName(TerrariaDllResolver.DllSource dllSource)
        {
            switch (dllSource)
            {
                case TerrariaDllResolver.DllSource.DirectX: return "Microsoft DirectX";
                case TerrariaDllResolver.DllSource.XnaFramework: return "Microsoft XNA";
                case TerrariaDllResolver.DllSource.VisualCPP: return "Microsoft Visual C++";
                default: throw new Exception();
            }
        }

        private static string DllSourceToWebsiteLink(TerrariaDllResolver.DllSource dllSource)
        {
            switch (dllSource)
            {
                case TerrariaDllResolver.DllSource.DirectX:
                    return "https://www.microsoft.com/en-us/download/details.aspx?id=35";
                case TerrariaDllResolver.DllSource.XnaFramework:
                    return "https://www.microsoft.com/en-us/download/details.aspx?id=20914";
                case TerrariaDllResolver.DllSource.VisualCPP:
                    return "https://www.microsoft.com/en-us/download/details.aspx?id=26999";
                default: throw new Exception();
            }
        }

        private void ResetDlls()
        {
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                RowInfo rowInfo = RowData[row.Index];
                rowInfo.FilePath = null;
                UpdateRow(row);
            }
        }

        private void AutoImportDlls(bool promptDialog)
        {
            TerrariaDllResolver.XnaFrameworkVersion xnaFrameworkVersion =
                TerrariaDllResolver.XnaFrameworkVersion.FindHighestXnaFrameworkVersionOnSystem();

            if (promptDialog)
            {
                if (!PromptXnaFrameworkVersion(out xnaFrameworkVersion))
                {
                    return;
                }
            }

            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                RowInfo rowInfo = RowData[row.Index];
                TerrariaDllResolver.Dll dll = rowInfo.Dll;
                string dllFilePath = TerrariaDllResolver.FindDllFilePathOnSystem(dll, xnaFrameworkVersion);
                rowInfo.FilePath = dllFilePath;
                UpdateRow(row);
            }
        }

        private void ImportDllsFromFolder()
        {
            string path = GuiFileDialogs.ShowOpenFolderDialog(this, importButton.Text, null);
            if (path != null)
            {
                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    RowInfo rowInfo = RowData[row.Index];
                    string dllFilePath = path + "\\" + rowInfo.Dll.Name;
                    if (File.Exists(dllFilePath))
                    {
                        rowInfo.FilePath = dllFilePath;
                        UpdateRow(row);
                    }
                }
            }
        }

        private static bool PromptXnaFrameworkVersion(out TerrariaDllResolver.XnaFrameworkVersion result)
        {
            TerrariaDllResolver.XnaFrameworkVersion[] xnaFrameworkVersions =
                TerrariaDllResolver.XnaFrameworkVersion.FindXnaFrameworkVersionsOnSystem();

            if (xnaFrameworkVersions == null || xnaFrameworkVersions.Length == 0)
            {
                result = null;
                return true;
            }
            else if (xnaFrameworkVersions.Length == 1)
            {
                result = xnaFrameworkVersions[0];
                return true;
            }

            TerrariaDllResolver.XnaFrameworkVersion dialogResult = null;
            bool dialogOk = false;
            using (Form form = new Form())
            {
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.SizeGripStyle = SizeGripStyle.Hide;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ShowIcon = false;
                form.ShowInTaskbar = false;
                form.Text = "Microsoft XNA Framework";

                Label label = new Label();
                label.AutoSize = true;
                label.Text = "The following Microsoft XNA Framework versions were found.\nSelect one:";
                label.MaximumSize = form.ClientSize;

                ListBox listBox = new ListBox();
                listBox.SelectionMode = SelectionMode.One;
                listBox.HorizontalScrollbar = true;
                listBox.IntegralHeight = false;

                Button okButton = new Button();
                Button cancelButton = new Button();
                okButton.Text = "OK";
                okButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                okButton.AutoSize = true;
                cancelButton.Text = "Cancel";
                cancelButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                cancelButton.AutoSize = true;

                Panel panel1 = GuiPanelBuilder.GlueTopToCenter(label, listBox);
                Panel panel2 = GuiPanelBuilder.GlueLeftToCenter(okButton, cancelButton);
                Panel panel3 = GuiPanelBuilder.GlueBottomToCenter(
                    GuiPanelBuilder.HorizontallyCenter(panel2), panel1);
                Panel mainPanel = GuiPanelBuilder.Pad(panel3, new Padding(5, 5, 5, 5));
                GuiPanelBuilder.DockToForm(mainPanel, form);

                int tabIndex = 1;
                panel1.TabIndex = tabIndex++;
                listBox.TabIndex = tabIndex++;
                panel2.TabIndex = tabIndex++;
                panel3.TabIndex = tabIndex++;
                okButton.TabIndex = tabIndex++;
                cancelButton.TabIndex = tabIndex++;

                foreach (TerrariaDllResolver.XnaFrameworkVersion xnaFrameworkVersion in xnaFrameworkVersions)
                {
                    listBox.Items.Add(xnaFrameworkVersion.Version);
                }
                listBox.SelectedIndex = 0;

                okButton.Click += (o, e) =>
                {
                    dialogResult = xnaFrameworkVersions[listBox.SelectedIndex];
                    dialogOk = true;
                    form.Close();
                };

                cancelButton.Click += (o, e) =>
                {
                    form.Close();
                };

                form.ShowDialog();
            }

            result = dialogResult;
            return dialogOk;
        }

    }
}