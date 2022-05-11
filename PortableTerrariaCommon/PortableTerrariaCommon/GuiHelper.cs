using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sahlaysta.PortableTerrariaCommon
{
    //gui utility class
    static class GuiHelper
    {
        //window msg
        public static void ShowMsg(
            object o, string title = "", IWin32Window owner = null)
        {
            string text = $"{o}";
            MessageBox.Show(owner, text, title);
        }
        public static bool ShowOkCancelMsg(
            object o, string title = "", IWin32Window owner = null)
        {
            return dlgPrompt(o, title, owner, MessageBoxButtons.OKCancel);
        }
        public static bool ShowYesNoPrompt(
            object o, string title = "", IWin32Window owner = null)
        {
            return dlgPrompt(o, title, owner, MessageBoxButtons.YesNo);
        }
        static bool dlgPrompt(
            object o, string title = "", IWin32Window owner = null,
            MessageBoxButtons buttons = MessageBoxButtons.OKCancel)
        {
            string text = $"{o}";
            var result = MessageBox.Show(owner, text, title, buttons);
            return result == DialogResult.OK || result == DialogResult.Yes;
        }

        //control cross-thread run
        public static void RunCrossThread(Control control, Action action)
        {
            try
            {
                control.Invoke(new MethodInvoker(delegate
                {
                    action();
                }));
            }
            catch (ObjectDisposedException) { }
        }

        //run when form shown, only one time
        public static void RunWhenShown(Form form, Action action)
        {
            //remove eventhandler after it is ran
            EventHandler[] eh = new EventHandler[1];//wrapper array
            var shownAction = (EventHandler)((o, e) =>
            {
                form.Shown -= eh[0];
                action();
            });
            eh[0] = shownAction;
            form.Shown += eh[0];
        }

        //sets Dialog properties to a form
        public static void SetDialogProperties(Form form)
        {
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.SizeGripStyle = SizeGripStyle.Hide;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.ShowIcon = false;
            form.ShowInTaskbar = false;
        }

        //returns all children and subchildren of a control
        public static IEnumerable<Control> GetAllControls(Control control)
        {
            var controls = control.Controls.Cast<Control>();
            return controls.SelectMany(c => GetAllControls(c)).Concat(controls);
        }
        public static IEnumerable<T> GetAllControls<T>(Control control) where T : Control
        {
            return GetAllControls(control).OfType<T>();
        }

        //browse
        public static string BrowseFile(
            string title = null, string filter = null, bool save = false)
        {
            if (save)
            {
                using (var sfd = new SaveFileDialog())
                {
                    if (title != null)
                        sfd.Title = title;
                    if (filter != null)
                        sfd.Filter = filter;
                    return sfd.ShowDialog() == DialogResult.OK ? sfd.FileName : null;
                }
            }
            else
            {
                using (var ofd = new OpenFileDialog())
                {
                    if (title != null)
                        ofd.Title = title;
                    if (filter != null)
                        ofd.Filter = filter;
                    return ofd.ShowDialog() == DialogResult.OK ? ofd.FileName : null;
                }
            }
        }
        public static string BrowseFolder(
            string title = null, IntPtr? hWndOwner = null)
        {
            using (var fbd = new FolderBrowser2())
            {
                if (title != null)
                    fbd.Title = title;
                if (hWndOwner != null)
                    fbd.HWndOwner = hWndOwner;
                return fbd.ShowDialog() == DialogResult.OK ? fbd.DirectoryPath : null;
            }
        }

        //get embedded resource
        public static Stream GetResourceStream(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string assemblyName = assembly.GetName().Name;
            string embRsrcName = assemblyName + "." + resourceName;
            var stream = assembly.GetManifestResourceStream(embRsrcName);
            if (stream == null)
            {
                throw new ArgumentException(
                    "No resource with the specified" +
                    " resource name was found:\n" + embRsrcName);
            }
            return stream;
        }
        public static byte[] GetResourceBytes(string resourceName)
        {
            using (var ms = new MemoryStream())
            using (var rsrcs = GetResourceStream(resourceName))
            {
                rsrcs.CopyTo(ms);
                return ms.ToArray();
            }
        }
        public static string[] GetResourceNames()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetManifestResourceNames();
        }

        //run GuiProgressibleOperation with GuiProgressDialog
        public static void RunProgressDialog(
            string labelText, string title,
            GuiProgressibleOperation progressibleOperation,
            IntPtr? hWndOwner = null)
        {
            var dlg = new GuiProgressDialog(labelText, title);
            var po = progressibleOperation;
            po.Progress += (o, e) =>
            {
                int numerator = e.Numerator, denominator = e.Denominator;
                if (hWndOwner is IntPtr val)
                {
                    TaskbarProgress.SetValue(val, numerator, denominator);
                }
                RunCrossThread(
                    dlg,
                    () => dlg.SetProgress(numerator, denominator));
            };
            po.End += (o, e) =>
            {
                //progressbar cancel
                if (e.Error != null || e.IsCanceled)
                {
                    RunCrossThread(
                        dlg,
                        () => dlg.CancelProgress());
                }

                //error dialog
                if (e.Error != null)
                {
                    if (hWndOwner is IntPtr val)
                    {
                        TaskbarProgress.SetState(
                            val,
                            TaskbarProgress.TaskbarStates.Error);
                    }
                    RunCrossThread(
                        dlg,
                        () => ShowMsg(e.Error.ToString(), "Error", dlg));
                    RunCrossThread(
                        dlg,
                        () => dlg.Close());
                    return;
                }

                //progressbar 
                if (!e.IsCanceled)
                {
                    RunCrossThread(
                        dlg,
                        () => dlg.FinishProgress());
                    if (hWndOwner is IntPtr val)
                    {
                        TaskbarProgress.SetValue(val, 1, 1);
                    }
                }
            };
            dlg.UserCanceled += (o, e) =>
            {
                po.Cancel();
                if (hWndOwner is IntPtr val)
                {
                    TaskbarProgress.SetState(
                        val,
                        TaskbarProgress.TaskbarStates.NoProgress);
                }
            };
            dlg.Shown += (o, e) => po.RunAsync();
            dlg.FormClosed += (o, e) =>
            {
                if (hWndOwner is IntPtr val)
                {
                    TaskbarProgress.SetState(
                        val,
                        TaskbarProgress.TaskbarStates.NoProgress);
                }
            };
            dlg.ShowDialog();
        }
    }
}
