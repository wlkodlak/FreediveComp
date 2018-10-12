using Microsoft.Owin.Hosting;
using MilanWilczak.FreediveComp;
using MilanWilczak.FreediveComp.Models;
using System;
using System.Configuration;
using System.Windows.Forms;
using Unity;

namespace FreediveComp.Launcher
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                var baseWebUiUri = ConfigurationManager.AppSettings["web:ui"];
                var baseWebApiUri = ConfigurationManager.AppSettings["web:api"];
                var adminToken = ConfigurationManager.AppSettings["authentication:admin"];
                var startOptions = new StartOptions();
                startOptions.Urls.Add(baseWebApiUri);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                using (var web = WebApp.Start<Startup>(startOptions))
                using (var icon = new ProcessIcon(Startup.Container.Resolve<IRacesIndexRepository>(), baseWebUiUri, adminToken))
                {
                    icon.Display();
                    Application.Run();
                }
            }
            catch (Exception e)
            {
                while (e.InnerException != null) e = e.InnerException;
                MessageBox.Show(e.ToString(), "Failed to start", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
