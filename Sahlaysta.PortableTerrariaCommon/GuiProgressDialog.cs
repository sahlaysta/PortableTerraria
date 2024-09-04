using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Sahlaysta.PortableTerrariaCommon
{

    /// <summary>
    /// Modal dialog with progress bar, label, and an OK button.
    /// Update operations are thread-safe.
    /// </summary>
    internal class GuiProgressDialog : IDisposable
    {

        private readonly Form form;
        private readonly Label label;
        private readonly ProgressBar progressBar;
        private readonly Button okButton;

        private readonly object threadLock = new object();
        private bool done = false;

        public GuiProgressDialog(string title)
        {
            form = new Form();
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.SizeGripStyle = SizeGripStyle.Hide;
            form.StartPosition = FormStartPosition.CenterParent;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.ShowIcon = false;
            form.ShowInTaskbar = false;
            form.Text = title;
            DisableWindowCloseButton(form);

            label = new Label();
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.AutoSize = true;
            label.Text = "1%";

            progressBar = new ProgressBar();

            okButton = new Button();
            okButton.Text = "OK";
            okButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            okButton.AutoSize = true;
            okButton.Enabled = false;

            Panel panel1 = PanelBuilder.GlueTopToCenter(PanelBuilder.HorizontallyCenter(label), progressBar);
            Panel panel2 = PanelBuilder.GlueBottomToCenter(PanelBuilder.HorizontallyCenter(okButton), panel1);
            Panel mainPanel = PanelBuilder.Pad(panel2, new Padding(5, 5, 5, 5));
            PanelBuilder.DockToForm(mainPanel, form);

            form.Size = new Size(250, 100);
        }

        public void ShowDialog(IWin32Window window)
        {
            form.ShowDialog(window);
        }

        void IDisposable.Dispose()
        {
            lock (threadLock)
            {
                done = true;
            }
            form.Dispose();
        }

        public void InvokeSetProgress(int value, int max)
        {
            lock (threadLock)
            {
                if (done) return;
            }

            form.BeginInvoke(new Action(() =>
            {
                lock (threadLock)
                {
                    if (done) return;
                }

                if (progressBar.Maximum != max)
                    progressBar.Maximum = max;
                if (progressBar.Value != value)
                    progressBar.Value = value;
                int percentage = max == 0 ? 0 : (int)(((value * 1.0) / (max * 1.0)) * 100.0);
                if (percentage < 1)
                    percentage = 1;
                if (percentage > 99)
                    percentage = 99;
                label.Text = percentage + "%";
            }));
        }

        public void InvokeSetProgressEnd(Exception error)
        {
            lock (threadLock)
            {
                if (done) return;
            }

            form.BeginInvoke(new Action(() =>
            {
                lock (threadLock)
                {
                    if (done) return;
                    done = true;
                }

                if (error != null)
                {
                    Console.Error.WriteLine(error);
                    MessageBox.Show(error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    form.Close();
                }
                else
                {
                    if (progressBar.Maximum <= 0)
                        progressBar.Maximum = 1;
                    progressBar.Value = progressBar.Maximum;
                    label.Text = "100%";
                    EnableWindowCloseButton(form);
                    okButton.Enabled = true;
                    okButton.Click += (o, e) => form.Close();
                    okButton.Focus();
                }
            }));
        }

        private static void DisableWindowCloseButton(IWin32Window window)
        {
            IntPtr handle = window.Handle;
            uint v = GetClassLong(handle, -26);
            if (v != 0)
            {
                SetClassLong(handle, -26, new IntPtr(v | 512));
            }
        }

        private static void EnableWindowCloseButton(IWin32Window window)
        {
            IntPtr handle = window.Handle;
            uint v = GetClassLong(handle, -26);
            if (v != 0)
            {
                SetClassLong(handle, -26, new IntPtr(v & ~512));
            }
        }

        [DllImport("user32.dll")]
        private static extern uint GetClassLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern uint SetClassLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    }
}