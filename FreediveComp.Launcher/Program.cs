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
            var baseWebUiUri = ConfigurationManager.AppSettings["web:ui"];
            var baseWebApiUri = ConfigurationManager.AppSettings["web:api"];

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var web = WebApp.Start<Startup>(baseWebApiUri))
            using (var icon = new ProcessIcon(Startup.Container.Resolve<IRacesIndexRepository>(), baseWebUiUri))
            {
                icon.Display();
                Application.Run();
            }
        }
    }
}
