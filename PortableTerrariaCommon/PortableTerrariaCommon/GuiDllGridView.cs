using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sahlaysta.PortableTerrariaCommon
{
    class GuiDllGridView : UserControl
    {
        //constructor
        public GuiDllGridView()
        {
            init();
        }

        //public operations
        public IEnumerable<TerrariaResolver.Dll> Dlls
        {
            get => dlls.Length == 0 ? null : dlls;
        }

        //initializations
        void init()
        {
            initButtons();
            initDataGridView();
            adjustBounds();
            Resize += (o, e) => adjustBounds();
        }

        //init buttons
        List<Button> buttons = new List<Button>();
        void initButtons()
        {
            //buttons
            var autoImportButton = initButton("Auto-import DLLs");
            var importButton = initButton("Import selected DLL from file");
            var importAllButton = initButton("Import DLLs from folder");
            var unimportButton = initButton("Unimport selected DLL");
            var unimportAllButton = initButton("Unimport all DLLs");

            //button actions
            autoImportButton.Click += (o, e) => autoImportAllDlls(autoImportButton);
            importButton.Click += (o, e) => importSelectedDllFromFile(importButton);
            importAllButton.Click += (o, e) => importDllsFromFolder(importAllButton);
            unimportButton.Click += (o, e) => unimportSelectedDll(unimportButton);
            unimportAllButton.Click += (o, e) => unimportAllDlls(unimportAllButton);
        }
        Button initButton(string text)
        {
            var button = new Button()
            {
                Text = text,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            buttons.Add(button);
            Controls.Add(button);
            return button;
        }

        //init datagridview
        DataGridView dgv;
        TerrariaResolver.Dll[] dlls;
        (string, bool, bool)[] headers =
        {
            //(name, text center, hyperlink)
            ("Status", true, false),
            ("DLL name", false, false),
            ("DLL file path", false, false),
            ("Program", false, false),
            ("Program link", false, true)
        };
        void initDataGridView()
        {
            dgv = new DataGridView()
            {
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                AllowUserToAddRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,
                ColumnHeadersHeightSizeMode =
                    DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                EnableHeadersVisualStyles = false,
                MultiSelect = false
            };
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor =
                dgv.ColumnHeadersDefaultCellStyle.BackColor;

            //columns
            foreach (var headerTuple in headers)
            {
                string headerText = headerTuple.Item1;
                bool textCenter = headerTuple.Item2;
                bool hyperLink = headerTuple.Item3;

                DataGridViewColumn dgvc;
                if (!hyperLink)
                {
                    var dgvtbc = new DataGridViewTextBoxCell();
                    dgvc = new DataGridViewColumn(dgvtbc)
                    {
                        HeaderText = headerText
                    };
                }
                else
                {
                    var dgvlc = new DataGridViewLinkColumn()
                    {
                        HeaderText = headerText
                    };
                    dgvc = dgvlc;
                }

                if (textCenter)
                {
                    dgvc.DefaultCellStyle.Alignment =
                        DataGridViewContentAlignment.MiddleCenter;
                }

                dgv.Columns.Add(dgvc);
            }

            //react to hyperlink click
            dgv.CellContentDoubleClick += (o, e) =>
            {
                var column = dgv.Columns[e.ColumnIndex];
                if (column is DataGridViewLinkColumn)
                {
                    var cell = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex];
                    string val = cell.Value as string;
                    if (val != null)
                    {
                        try
                        {
                            Process.Start(val);
                        }
                        catch (Exception) { }
                    }
                }
            };

            //rows + content
            dlls = TerrariaResolver.ResolveTerrariaDlls();
            for (int i = 0; i < dlls.Length; i++)
            {
                dgv.Rows.Add();
            }
            refreshDllTable();

            Controls.Add(dgv);
        }

        //dll row content
        void refreshDllTable()
        {
            int i = 0;
            foreach (var dll in dlls)
            {
                setRow(dgv.Rows[i++], dll);
            }
        }

        //dll row content
        void setRow(DataGridViewRow row, TerrariaResolver.Dll dll)
        {
            //dll name and filepath
            string name = dll.Name;
            string filePath = dll.FilePath ?? "?";
            string programName = dll.Program.Name;
            string programLink = dll.Program.Link;

            //status and coloring
            string status;
            Color statusColor;
            if (dll.Status)
            {
                status = "✓";
                statusColor = Color.Green;
            }
            else
            {
                status = "X";
                statusColor = Color.Red;
            }

            row.SetValues(new object[]
            {
                status, name, filePath, programName, programLink
            });

            //set check mark / cross color
            row.Cells[0].Style.ForeColor = statusColor;

            //status text update
            int dllCount = dlls.Count(d => d.Status);
            int dllTotal = dlls.Length;
            dgv.Columns[0].HeaderText = dllCount + "/" + dllTotal;
        }

        //adjust location + size of datagridview and buttons
        void adjustBounds()
        {
            Control parent = Parent ?? this;

            //buttons
            //get max width of buttons
            int buttonMaxWidth = 0;
            foreach (var button in buttons)
            {
                button.AutoSize = true;
                int buttonWidth = button.PreferredSize.Width;
                if (buttonWidth > buttonMaxWidth)
                    buttonMaxWidth = buttonWidth;
                button.AutoSize = false;
            }

            //set all buttons size and position them to top right
            int buttonY = 0;
            foreach (var button in buttons)
            {
                button.Size = new Size(buttonMaxWidth, button.Height);
                button.Left = Bounds.Right - button.Bounds.Width - parent.Padding.Right;

                button.Top = buttonY;
                buttonY += button.Bounds.Height;
            }

            //set datagridview to top left, leaving width for buttons
            const int gap = 3; //gap between datagridview and buttons
            dgv.Location = new Point(0, 0);
            dgv.Size = new Size(Bounds.Width - buttonMaxWidth - gap, Bounds.Height);
        }

        
        //button actions
        void autoImportAllDlls(Button sender)
        {
            //prompt xna framework version
            bool dialogOk;
            string xnaFrameworkVersion = promptXnaFrameworkVersion(out dialogOk);
            if (!dialogOk)
                return;

            //resolve dlls
            var newDlls = TerrariaResolver.ResolveTerrariaDlls(xnaFrameworkVersion);

            //only import unimported dlls
            int i = 0;
            foreach (var newDll in newDlls)
            {
                if (!this.dlls[i].Status)
                {
                    importDll(i, newDll.FilePath);
                }
                i++;
            }
            refreshDllTable();

            //warning of uninstalled programs
            int count = 0;
            var uninstalledPrograms = new HashSet<TerrariaResolver.Program>();
            foreach (var dll in dlls)
            {
                if (!dll.Status)
                {
                    count++;
                    uninstalledPrograms.Add(dll.Program);
                }
            }
            if (count > 0)
            {
                string msg = "Failed to detect one or more DLLs of:\n\n\n"
                    + string.Join("\n\n", uninstalledPrograms.Select(p => p.Name));
                GuiHelper.ShowMsg(msg);
            }
        }
        void importSelectedDllFromFile(Button sender)
        {
            int i = getSelectedIndex();
            if (i == -1)
                return;

            //selected dll
            var dllName = dlls[i].Name;

            //file prompt
            var title = "Import " + dllName;
            var filter = dllName + "|" + dllName + "|DLL|*.dll|All files (*.*)|*.*";
            var file = GuiHelper.BrowseFile(title, filter);
            if (file == null)
                return;

            importDll(i, file);
            refreshDllTable();
        }
        void importDllsFromFolder(Button sender)
        {
            //folder prompt
            var title = sender.Text;
            var folder = GuiHelper.BrowseFolder(title);
            if (folder == null)
                return;

            //import dlls
            int i = 0;
            foreach (var dll in dlls)
            {
                //get dll in folder
                var dllName = dll.Name;
                var filePath = Path.Combine(folder, dllName);
                if (File.Exists(filePath))
                    importDll(i, filePath);
                i++;
            }
            refreshDllTable();
        }
        void unimportSelectedDll(Button sender)
        {
            int i = getSelectedIndex();
            if (i == -1)
                return;
            unimportDll(i);
            refreshDllTable();
        }
        void unimportAllDlls(Button sender)
        {
            for (int i = 0, len = dlls.Length; i < len; i++)
            {
                unimportDll(i);
            }
            refreshDllTable();
        }

        //setters
        int getSelectedIndex()
        {
            var rows = dgv.SelectedRows;
            return rows.Count == 1 ? rows[0].Index : -1;
        }
        void importDll(int i, string filePath)
        {
            var oldDll = dlls[i];
            var newDll = new TerrariaResolver.Dll(oldDll.Program, filePath, oldDll.Name);
            dlls[i] = newDll;
        }
        void unimportDll(int i)
        {
            var oldDll = dlls[i];
            var newDll = new TerrariaResolver.Dll(oldDll.Program, null, oldDll.Name);
            dlls[i] = newDll;
        }


        //xna versions prompt
        string promptXnaFrameworkVersion(out bool dialogOk)
        {
            //vers
            var vers = TerrariaResolver.ResolveXnaFrameworkVersions();
            if (vers == null || vers.Length == 0)
            {
                GuiHelper.ShowMsg("Failed to find installation of" +
                    " Microsoft XNA Framework");
                dialogOk = false;
                return null;
            }

            //the ver to return
            string xnaFrameworkVersion = null;
            bool dialogOkVal = false;

            //show and create form dialog
            var xnaFrameworkVersionPromptForm = new Form
            {
                Text = "Microsoft XNA Framework"
            };
            using (var form = xnaFrameworkVersionPromptForm)
            {
                //dialog
                GuiHelper.SetDialogProperties(form);

                //build main panel
                var pb = new GuiPanelBuilder(form);

                //label
                var label = new Label()
                {
                    AutoSize = true,
                    MaximumSize = new Size(pb.Panel.Width, 0),
                    Text = "These are the found installed " +
                        "Microsoft XNA Framework" +
                        " versions. Select the version to import."
                };
                pb.AddControl(label);

                //listbox
                var listBox = new ListBox()
                {
                    SelectionMode = SelectionMode.One,
                    HorizontalScrollbar = true
                };
                listBox.Items.AddRange(vers);
                listBox.SelectedIndex = 0;
                pb.AddControl(listBox);

                //buttons
                var buttonPanel = new GuiPanelBuilder.OkCancelButtonPanel();
                pb.AddControl(buttonPanel);

                //finish main panel
                pb.Finish();

                //button actions
                buttonPanel.CancelButton.Click += (o, e) => form.Close();
                buttonPanel.OkButton.Click += (o, e) =>
                {
                    int selectedIndex = listBox.SelectedIndex;
                    if (selectedIndex == -1)
                    {
                        GuiHelper.ShowMsg("No item selected");
                        return;
                    }

                    xnaFrameworkVersion = listBox.Items[selectedIndex] as string;
                    dialogOkVal = true;
                    form.Close();
                };

                //default focused control
                GuiHelper.RunWhenShown(form, () => buttonPanel.OkButton.Focus());

                form.ShowDialog();
            }

            dialogOk = dialogOkVal;
            return dialogOkVal ? xnaFrameworkVersion : null;
        }
    }
}
