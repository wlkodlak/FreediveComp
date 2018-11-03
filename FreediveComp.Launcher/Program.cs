using Microsoft.Owin.Hosting;
using MilanWilczak.FreediveComp;
using MilanWilczak.FreediveComp.Models;
using System;
using System.Configuration;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using Unity;

namespace FreediveComp.Launcher
{
    static class Program
    {
        private const int ERROR_SHARING_VIOLATION = 32;

        [STAThread]
        static void Main()
        {
            try
            {
                var baseWebUiUri = ConfigurationManager.AppSettings["web:ui"];
                var baseWebApiUri = ConfigurationManager.AppSettings["web:api"];
                var adminToken = ConfigurationManager.AppSettings["authentication:admin"];
                var startOptions = new StartOptions();
                var createdNew = false;
                var appName = "FreediveComp";
                var browserOpener = new BrowserOpener(baseWebUiUri, adminToken);
                startOptions.Urls.Add(baseWebApiUri);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                using (var mutex = new Mutex(true, appName, out createdNew))
                {
                    if (createdNew)
                    {
                        using (var web = WebApp.Start<Startup>(startOptions))
                        using (var icon = new ProcessIcon(Startup.Container.Resolve<IRacesIndexRepository>(), browserOpener))
                        {
                            icon.Display();
                            browserOpener.OpenHomepage();
                            Application.Run();
                        }
                    }
                    else
                    {
                        browserOpener.OpenHomepage();
                    }
                }
            }
            catch (Exception e)
            {
                while (e.InnerException != null) e = e.InnerException;
                var message = GetErrorMessage(e);
                MessageBox.Show(message, "Failed to start", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string GetErrorMessage(Exception e)
        {
            if (e is HttpListenerException httpError)
            {
                switch (httpError.ErrorCode)
                {
                    case ERROR_SHARING_VIOLATION: return "Another process is using selected TCP port";
                }
            }
            return e.ToString();
        }
    }
}
