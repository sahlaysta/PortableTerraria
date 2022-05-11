using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sahlaysta.PortableTerrariaCommon
{
    //shows radiobuttons "Enable" and "Disable" grouped on top of a control
    class GuiDisableableControl : Panel
    {
        //constructor
        public GuiDisableableControl()
        {
            initRadioButtons();

            adjustBounds();
            ControlAdded += (o, e) => adjustBounds(true);
            ControlRemoved += (o, e) => adjustBounds();
            Resize += (o, e) => adjustBounds();
        }

        //public operations
        public RadioButton EnableRadioButton { get { return enableBtn; } }
        public RadioButton DisableRadioButton { get { return disableBtn; } }
        public override Size GetPreferredSize(Size proposedSize)
        {
            int width = base.GetPreferredSize(proposedSize).Width;
            int height = y;
            return new Size(width, height);
        }

        //initializations
        RadioButton enableBtn, disableBtn;
        void initRadioButtons()
        {
            enableBtn = new RadioButton()
            {
                AutoSize = true,
                Text = "Enable"
            };
            disableBtn = new RadioButton()
            {
                AutoSize = true,
                Text = "Disable"
            };
            enableBtn.Click += (o, e) =>
            {
                Control control = getDC();
                if (control != null)
                    control.Enabled = true;
            };
            disableBtn.Click += (o, e) =>
            {
                Control control = getDC();
                if (control != null)
                    control.Enabled = false;
            };
            Controls.Add(enableBtn);
            Controls.Add(disableBtn);
            updateRadioButtons();
            enableBtn.TextChanged += (o, e) => updateRadioButtons();
            disableBtn.TextChanged += (o, e) => updateRadioButtons();
        }

        //adjust location + size
        int y;
        void adjustBounds(bool adding = false)
        {
            Control control = getDC();

            y = Math.Max(enableBtn.Bounds.Height, disableBtn.Bounds.Height);

            if (control != null)
            {
                //control positioning
                var cs = ClientSize;
                control.Location = new Point(0, y);
                control.Size = new Size(cs.Width, cs.Height - y);

                //set bounds
                y += control.PreferredSize.Height;
                
                //radiobutton selection
                if (adding)
                    disableBtn.Checked = !(enableBtn.Checked = control.Enabled);
            }
        }

        //placement of the two radiobuttons based on their text width
        void updateRadioButtons()
        {
            enableBtn.Location = new Point(0, 0);
            disableBtn.Left = 30 + TextRenderer.MeasureText(
                enableBtn.Text, enableBtn.Font).Width;
        }

        Control getDC() => Controls.Cast<Control>().FirstOrDefault(
            c => c != enableBtn && c != disableBtn);
    }
}
