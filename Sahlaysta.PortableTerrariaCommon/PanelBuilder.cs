using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Sahlaysta.PortableTerrariaCommon
{

    /// <summary>
    /// Simplify window/dialog creation and automatic resizing.
    /// </summary>
    internal static class PanelBuilder
    {

        public static Panel GlueTopToCenter(Control topControl, Control centerControl)
        {
            return new GlueTopToCenterImpl(centerControl, topControl);
        }

        public static Panel GlueBottomToCenter(Control bottomControl, Control centerControl)
        {
            return new GlueBottomToCenterImpl(centerControl, bottomControl);
        }

        public static Panel GlueLeftToCenter(Control leftControl, Control centerControl)
        {
            return new GlueLeftToCenterImpl(centerControl, leftControl);
        }

        public static Panel GlueRightToCenter(Control rightControl, Control centerControl)
        {
            return new GlueRightToCenterImpl(centerControl, rightControl);
        }

        public static Panel Pad(Control control, Padding padding)
        {
            return new PadImpl(control, padding);
        }

        public static Panel VerticallyCenter(Control control)
        {
            return new VerticallyCenterImpl(control);
        }

        public static Panel HorizontallyCenter(Control control)
        {
            return new HorizontallyCenterImpl(control);
        }

        public static Panel Title(Control control, string title)
        {
            return new TitleImpl(control, title);
        }

        public static void DockToForm(Control control, Form form)
        {
            Panel dockPanel = new Panel();
            form.Controls.Add(dockPanel);
            dockPanel.Dock = DockStyle.Fill;
            int widthOffset = form.Width - dockPanel.Width;
            int heightOffset = form.Height - dockPanel.Height;
            form.Controls.Remove(dockPanel);

            form.Controls.Add(control);
            Size preferredSize = control.PreferredSize;
            control.Dock = DockStyle.Fill;
            form.Size = new Size(preferredSize.Width + widthOffset, preferredSize.Height + heightOffset);
        }

        private abstract class PanelBase : Panel
        {

            protected readonly Control CenterControl;

            public PanelBase(Control centerControl)
            {
                CenterControl = centerControl;
                Controls.Add(CenterControl);
                if (ShouldAdjustBounds())
                {
                    AdjustBounds();
                }
                Resize += (o, e) =>
                {
                    if (ShouldAdjustBounds())
                    {
                        AdjustBounds();
                    }
                };
                CenterControl.Resize += (o, e) =>
                {
                    if (ShouldAdjustBounds())
                    {
                        AdjustBounds();
                    }
                };
            }

            protected abstract void AdjustBounds();

            protected virtual bool ShouldAdjustBounds()
            {
                return Controls.Contains(CenterControl);
            }

            public override Size GetPreferredSize(Size proposedSize)
            {
                return CenterControl.PreferredSize;
            }

        }

        private abstract class GluingPanelBase : PanelBase
        {

            protected readonly Control GluedControl;

            public GluingPanelBase(Control centerControl, Control gluedControl) : base(centerControl)
            {
                GluedControl = gluedControl;
                Controls.Add(GluedControl);
                if (ShouldAdjustBounds())
                {
                    AdjustBounds();
                }
                GluedControl.Resize += (o, e) =>
                {
                    if (ShouldAdjustBounds())
                    {
                        AdjustBounds();
                    }
                };
            }

            protected override bool ShouldAdjustBounds()
            {
                return Controls.Contains(CenterControl) && Controls.Contains(GluedControl);
            }

        }

        private class GlueTopToCenterImpl : GluingPanelBase
        {

            public GlueTopToCenterImpl(Control centerControl, Control topControl)
                : base(centerControl, topControl) { }

            protected override void AdjustBounds()
            {
                Size clientSize = ClientSize;
                Size topControlPreferredSize = GluedControl.PreferredSize;
                CenterControl.Bounds = new Rectangle(
                    0,
                    topControlPreferredSize.Height,
                    clientSize.Width,
                    Math.Max(0, clientSize.Height - topControlPreferredSize.Height));
                GluedControl.Bounds = new Rectangle(
                    0,
                    0,
                    clientSize.Width,
                    topControlPreferredSize.Height);
            }

            public override Size GetPreferredSize(Size proposedSize)
            {
                Size centerControlPreferredSize = CenterControl.PreferredSize;
                Size topControlPreferredSize = GluedControl.PreferredSize;
                return new Size(
                    Math.Max(centerControlPreferredSize.Width, topControlPreferredSize.Width),
                    centerControlPreferredSize.Height + topControlPreferredSize.Height);
            }

        }

        private class GlueBottomToCenterImpl : GluingPanelBase
        {

            public GlueBottomToCenterImpl(Control centerControl, Control bottomControl)
                : base(centerControl, bottomControl) { }

            protected override void AdjustBounds()
            {
                Size clientSize = ClientSize;
                Size bottomControlPreferredSize = GluedControl.PreferredSize;
                CenterControl.Bounds = new Rectangle(
                    0,
                    0,
                    clientSize.Width,
                    Math.Max(0, clientSize.Height - bottomControlPreferredSize.Height));
                GluedControl.Bounds = new Rectangle(
                    0,
                    Math.Max(0, clientSize.Height - bottomControlPreferredSize.Height),
                    clientSize.Width,
                    bottomControlPreferredSize.Height);
            }

            public override Size GetPreferredSize(Size proposedSize)
            {
                Size centerControlPreferredSize = CenterControl.PreferredSize;
                Size bottomControlPreferredSize = GluedControl.PreferredSize;
                return new Size(
                    Math.Max(centerControlPreferredSize.Width, bottomControlPreferredSize.Width),
                    centerControlPreferredSize.Height + bottomControlPreferredSize.Height);
            }

        }

        private class GlueLeftToCenterImpl : GluingPanelBase
        {

            public GlueLeftToCenterImpl(Control centerControl, Control leftControl)
                : base(centerControl, leftControl) { }

            protected override void AdjustBounds()
            {
                Size clientSize = ClientSize;
                Size leftControlPreferredSize = GluedControl.PreferredSize;
                CenterControl.Bounds = new Rectangle(
                    leftControlPreferredSize.Width,
                    0,
                    Math.Max(0, clientSize.Width - leftControlPreferredSize.Width),
                    clientSize.Height);
                GluedControl.Bounds = new Rectangle(
                    0,
                    0,
                    leftControlPreferredSize.Width,
                    clientSize.Height);
            }

            public override Size GetPreferredSize(Size proposedSize)
            {
                Size centerControlPreferredSize = CenterControl.PreferredSize;
                Size leftControlPreferredSize = GluedControl.PreferredSize;
                return new Size(
                    centerControlPreferredSize.Width + leftControlPreferredSize.Width,
                    Math.Max(centerControlPreferredSize.Height, leftControlPreferredSize.Height));
            }

        }

        private class GlueRightToCenterImpl : GluingPanelBase
        {

            public GlueRightToCenterImpl(Control centerControl, Control rightControl)
                : base(centerControl, rightControl) { }

            protected override void AdjustBounds()
            {
                Size clientSize = ClientSize;
                Size rightControlPreferredSize = GluedControl.PreferredSize;
                CenterControl.Bounds = new Rectangle(
                    0,
                    0,
                    Math.Max(0, clientSize.Width - rightControlPreferredSize.Width),
                    clientSize.Height);
                GluedControl.Bounds = new Rectangle(
                    Math.Max(0, clientSize.Width - rightControlPreferredSize.Width),
                    0,
                    rightControlPreferredSize.Width,
                    clientSize.Height);
            }

            public override Size GetPreferredSize(Size proposedSize)
            {
                Size centerControlPreferredSize = CenterControl.PreferredSize;
                Size rightControlPreferredSize = GluedControl.PreferredSize;
                return new Size(
                    centerControlPreferredSize.Width + rightControlPreferredSize.Width,
                    Math.Max(centerControlPreferredSize.Height, rightControlPreferredSize.Height));
            }

        }

        private class PadImpl : PanelBase
        {

            private readonly Padding padding;

            public PadImpl(Control centerControl, Padding padding) : base(centerControl)
            {
                this.padding = padding;
            }

            protected override void AdjustBounds()
            {
                Size clientSize = ClientSize;
                CenterControl.Bounds = new Rectangle(
                    padding.Left,
                    padding.Top,
                    Math.Max(0, clientSize.Width - padding.Horizontal),
                    Math.Max(0, clientSize.Height - padding.Vertical));
            }

            public override Size GetPreferredSize(Size proposedSize)
            {
                Size centerControlPreferredSize = CenterControl.PreferredSize;
                return new Size(
                    centerControlPreferredSize.Width + padding.Horizontal,
                    centerControlPreferredSize.Height + padding.Vertical);
            }

        }

        private class VerticallyCenterImpl : PanelBase
        {

            public VerticallyCenterImpl(Control centerControl) : base(centerControl) { }

            protected override void AdjustBounds()
            {
                Size clientSize = ClientSize;
                Size centerControlPreferredSize = CenterControl.PreferredSize;
                CenterControl.Bounds = new Rectangle(
                    0,
                    Math.Max(0, clientSize.Height - centerControlPreferredSize.Height) / 2,
                    clientSize.Width,
                    centerControlPreferredSize.Height);
            }

        }

        private class HorizontallyCenterImpl : PanelBase
        {

            public HorizontallyCenterImpl(Control centerControl) : base(centerControl) { }

            protected override void AdjustBounds()
            {
                Size clientSize = ClientSize;
                Size centerControlPreferredSize = CenterControl.PreferredSize;
                CenterControl.Bounds = new Rectangle(
                    Math.Max(0, clientSize.Width - centerControlPreferredSize.Width) / 2,
                    0,
                    centerControlPreferredSize.Width,
                    clientSize.Height);
            }

        }

        private class TitleImpl : PanelBase
        {

            private readonly GroupBox groupBox;

            public TitleImpl(Control centerControl, string title) : base(centerControl)
            {
                Controls.Remove(centerControl);
                groupBox = new GroupBox();
                groupBox.Text = title;
                groupBox.Controls.Add(centerControl);
                Controls.Add(groupBox);
                groupBox.Resize += (o, e) =>
                {
                    if (ShouldAdjustBounds())
                    {
                        AdjustBounds();
                    }
                };
            }

            protected override bool ShouldAdjustBounds()
            {
                return groupBox != null && groupBox.Contains(CenterControl);
            }

            protected override void AdjustBounds()
            {
                Size clientSize = ClientSize;
                groupBox.Size = clientSize;
                Padding padding = GetGroupBoxBorderPadding(groupBox);
                CenterControl.Bounds = new Rectangle(
                    padding.Left,
                    padding.Top,
                    Math.Max(0, clientSize.Width - padding.Horizontal),
                    Math.Max(0, clientSize.Height - padding.Vertical));
            }

            public override Size GetPreferredSize(Size proposedSize)
            {
                Padding padding = GetGroupBoxBorderPadding(groupBox);
                Size centerControlPreferredSize = CenterControl.PreferredSize;
                return new Size(
                    centerControlPreferredSize.Width + padding.Horizontal,
                    centerControlPreferredSize.Height + padding.Vertical);
            }

            private static Padding GetGroupBoxBorderPadding(GroupBox groupBox)
            {
                Rectangle bounds = groupBox.Bounds;
                Rectangle displayRectangle = groupBox.DisplayRectangle;
                int top = displayRectangle.Y;
                int bottom = Math.Max(0, bounds.Height - displayRectangle.Height - displayRectangle.Y);
                int left = displayRectangle.X;
                int right = Math.Max(0, bounds.Width - displayRectangle.Width - displayRectangle.X);
                return new Padding(left, top, right, bottom);
            }

        }

    }
}