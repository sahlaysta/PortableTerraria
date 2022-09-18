using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sahlaysta.PortableTerrariaCommon
{
    //puts controls in vertical consecution
    class GuiPanelBuilder
    {
        readonly Panel panel;
        readonly Form form;
        int y = 0;
        AnchorStyles anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        //constructor
        public GuiPanelBuilder(Form form)
        {
            this.form = form;
            panel = new Panel()
            {
                Dock = DockStyle.Fill
            };
            form.Controls.Add(panel);
        }

        //public operations
        public Panel Panel { get { return panel; } }
        public AnchorStyles Anchor { get { return anchor; } set { anchor = value; } }
        public void AddControl(Control control)
        {
            //gap
            if (y != 0)
                y += 10;

            //control positioning
            panel.Controls.Add(control);
            control.Width = panel.Bounds.Width;
            control.Top = y;
            control.Anchor = anchor;
            y += control.Bounds.Height;
        }
        public void Finish()
        {
            //panel positioning + form size
            var bounds = panel.Bounds;
            panel.Dock = DockStyle.None;
            panel.Bounds = bounds;
            form.ClientSize = new Size(form.ClientSize.Width, y);

            //panel gap border
            const int gap = 6;//pixels of gap border
            panel.Location = new Point(6, 6);
            form.Width += gap * 2;
            form.Height += gap * 2;

            //panel anchor
            panel.Anchor = AnchorStyles.Top | AnchorStyles.Left
                | AnchorStyles.Right | AnchorStyles.Bottom;
        }

        // group of "OK" button and "Cancel" button
        public class OkCancelButtonPanel : UserControl
        {
            readonly Button okButton, cancelButton;

            //constructor
            public OkCancelButtonPanel(
                string okButtonText = "OK", string cancelButtonText = "Cancel")
            {
                okButton = initButton(okButtonText);
                cancelButton = initButton(cancelButtonText);
                Controls.Add(okButton);
                Controls.Add(cancelButton);
                Height = Math.Max(okButton.Height, cancelButton.Height);
                Resize += (o, e) =>
                {
                    const int gap = 5; //gap in pixels between buttons
                    cancelButton.Left = Bounds.Width - cancelButton.Bounds.Width;
                    okButton.Left = cancelButton.Left - okButton.Bounds.Width - gap;
                };
            }

            //public operations
            public Button OkButton { get { return okButton; } }
            public Button CancelButton { get { return cancelButton; } }

            static Button initButton(string buttonText)
            {
                return new Button()
                {
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    MinimumSize = new Size(80, 0),
                    Text = buttonText
                };
            }
        }
    }
}
