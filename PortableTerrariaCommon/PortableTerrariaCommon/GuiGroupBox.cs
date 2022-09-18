using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sahlaysta.PortableTerrariaCommon
{
    //groupbox that takes a control to fill it, and autosizes based on its size
    class GuiGroupBox : GroupBox
    {
        readonly int borderX, borderY, borderWidth, borderHeight;

        public GuiGroupBox()
        {
            //control autosize
            ControlAdded += (o, e) => adjustBounds();
            ControlRemoved += (o, e) => adjustBounds();

            //get border bounds
            var bounds = Bounds;
            var dr = DisplayRectangle;
            borderX = bounds.X;
            borderY = bounds.Y;
            borderWidth = dr.Left - bounds.Left + bounds.Right - dr.Right;
            borderHeight = dr.Top - bounds.Top + bounds.Bottom - dr.Bottom;
        }

        void adjustBounds()
        {
            Control control = Controls.Count > 0 ? Controls[0] : null;

            //save control's preferred dimensions when not docked
            int compWidth, compHeight;
            if (control != null)
            {
                control.Dock = DockStyle.None;
                var compSize = control.PreferredSize;
                compWidth = compSize.Width;
                compHeight = compSize.Height;
                control.Dock = DockStyle.Fill;
            }
            else
            {
                compWidth = 0;
                compHeight = 0;
            }

            //adjust groupbox size
            Bounds = new Rectangle(
                borderX,
                borderY,
                borderWidth + compWidth,
                borderHeight + compHeight);
        }
    }
}
