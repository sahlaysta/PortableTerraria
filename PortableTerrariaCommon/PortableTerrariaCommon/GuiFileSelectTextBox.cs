using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sahlaysta.PortableTerrariaCommon
{
    class GuiFileSelectTextBox : UserControl
    {
        //path selected event args
        public class PathChangedEventArgs : EventArgs
        {
            public PathChangedEventArgs(string selectedPath)
            {
                _path = selectedPath;
            }
            public string SelectedPath => _path;
            readonly string _path;
        }
        //button click event args
        public class ButtonClickEventArgs : EventArgs
        {
            public ButtonClickEventArgs() { }
            public bool Cancel { get => c; set => c = value; }
            bool c;
        }

        string dlgTitle, fileFilter;
        IntPtr? dlgOwner;

        //constructor
        public GuiFileSelectTextBox()
        {
            init();
        }

        //public operations
        public TextBox TextBox { get { return textBox; } }
        public Button Button { get { return button; } }
        public bool FolderSelect { get { return folderSel; } set { folderSel = value; } }
        public string SelectedPath { get { return textBox.Text; } set { setPath(value); } }
        public string DialogTitle { get { return dlgTitle; } set { dlgTitle = value; } }
        public string FileFilter { get { return fileFilter; } set { fileFilter = value; } }
        public IntPtr? DialogOwner { get { return dlgOwner; } set { dlgOwner = value; } }
        public event EventHandler<ButtonClickEventArgs> ButtonClick;
        public event EventHandler<PathChangedEventArgs> PathChanged;

        public override Size GetPreferredSize(Size proposedSize)
        {
            int width = base.GetPreferredSize(proposedSize).Width;
            int height = Math.Max(textBox.Bounds.Height, button.Bounds.Height);
            return new Size(width, height);
        }

        //initializations
        TextBox textBox;//file text box
        Button button;//browse button
        int defaultButtonHeight;
        void init()
        {
            //textbox
            textBox = new TextBox();

            //button
            button = new Button();

            //button dimensions
            defaultButtonHeight = button.PreferredSize.Height;

            //textbox and button height centering
            if (textBox.Bounds.Height % 2 == 0 && button.Bounds.Height % 2 == 1)
                defaultButtonHeight++;

            //button layout
            button.Text = "Browse";
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Controls.Add(button);

            //textbox layout
            textBox.Location = new Point(0, 0);
            Controls.Add(textBox);

            //manual anchoring
            adjustBounds();
            Resize += (o, e) => adjustBounds();
            button.TextChanged += (o, e) => adjustBounds();

            //events
            var xthis = this;
            textBox.TextChanged += (o, e) =>
            {
                PathChanged?.Invoke(xthis, new PathChangedEventArgs(textBox.Text));
            };
            button.Click += (o, e) =>
            {
                var bcea = new ButtonClickEventArgs();
                ButtonClick?.Invoke(o, bcea);
                if (!bcea.Cancel)
                    promptAndSetFile();
            };
        }

        //adjust location + size for textbox and button centering
        void adjustBounds()
        {
            Control parent = Parent ?? this;

            //get button dimensions
            button.AutoSize = true;
            int buttonAutoWidth = button.PreferredSize.Width;
            button.AutoSize = false;

            //set button dimensions
            button.Size = new Size(buttonAutoWidth, defaultButtonHeight);

            //set textbox dimensions + set button location
            const int gap = 3; //extra space between button and textbox
            button.Left = Bounds.Right - button.Bounds.Width - parent.Padding.Right;
            textBox.Width = Bounds.Width - button.Bounds.Width - gap;
            button.Top = (button.Parent.Height - button.Bounds.Height) / 2;
            textBox.Top = (textBox.Parent.Height - textBox.Bounds.Height) / 2;
        }

        //set the path
        bool folderSel = false;
        void setPath(string path)
        {
            if (path == null)
                path = "";
            TextBox.Text = path;
            TextBox.SelectionStart = path.Length;
            TextBox.SelectionLength = 0;
        }
        void promptAndSetFile()
        {
            string file = promptFile();
            if (file != null)
                setPath(file);
        }

        //file prompt
        string promptFile()
        {
            string title = dlgTitle ?? button.Text;
            return
                folderSel
                ? GuiHelper.BrowseFolder(title, dlgOwner)
                : GuiHelper.BrowseFile(title, fileFilter);
        }
    }
}
