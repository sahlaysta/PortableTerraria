using System;
using System.Windows.Forms;

namespace Sahlaysta.PortableTerrariaCreator
{

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GuiForm());
        }
    }
}