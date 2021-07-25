using CefSharp;
using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormApp
{
    static class Program
    {
        /// <summary>
        /// Punto di ingresso principale dell'applicazione.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Cef.EnableHighDPISupport();
            var settings = new CefSettings();
            settings.CefCommandLineArgs.Add("disable-gpu"); // Disable GPU acceleration
            settings.CefCommandLineArgs.Add("disable-gpu-vsync"); //Disable GPU vsync
            Cef.Initialize(settings);
            var exitCode = CefSharp.BrowserSubprocess.SelfHost.Main(new string[] { });

            Application.Run(new FormMain());
        }
    }
}
