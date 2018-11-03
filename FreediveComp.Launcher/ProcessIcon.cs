using FreediveComp.Launcher.Properties;
using MilanWilczak.FreediveComp.Models;
using System;
using System.Windows.Forms;

namespace FreediveComp.Launcher
{
    public class ProcessIcon : IDisposable
    {
        private readonly IRacesIndexRepository racesRepository;
        private readonly BrowserOpener browserOpener;
        private NotifyIcon notifyIcon;
        private ContextMenuStrip menu;

        public ProcessIcon(IRacesIndexRepository racesRepository, BrowserOpener browserOpener)
        {
            this.racesRepository = racesRepository;
            this.browserOpener = browserOpener;
            this.notifyIcon = new NotifyIcon();
            this.notifyIcon.MouseDoubleClick += (sender, e) => browserOpener.OpenHomepage();
            this.menu = new ContextMenuStrip();
            this.menu.Opening += ContextMenuStrip_Opening;
        }

        public void Display()
        {
            notifyIcon.Icon = Resources.MainIcon;
            notifyIcon.Text = "FreediveComp";
            notifyIcon.Visible = true;
            notifyIcon.ContextMenuStrip = menu;
        }

        private void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            menu.Items.Clear();
            var races = racesRepository.GetAll();
            foreach (var race in races)
            {
                AddRace(race.RaceId, race.Name);
            }
            if (menu.Items.Count > 0) AddSeparator();
            AddCreateRace();
            AddExit();

            e.Cancel = false;
        }

        private void AddRace(string raceId, string name)
        {
            var item = new ToolStripMenuItem();
            item.Text = name;
            item.Click += (sender, e) => browserOpener.OpenRace(raceId);
            menu.Items.Add(item);
        }

        private void AddSeparator()
        {
            menu.Items.Add(new ToolStripSeparator());
        }

        private void AddCreateRace()
        {
            var item = new ToolStripMenuItem();
            item.Text = "Create competition";
            item.Click += (sender, e) => browserOpener.OpenNewRace();
            menu.Items.Add(item);
        }

        private void AddExit()
        {
            var item = new ToolStripMenuItem();
            item.Text = "Exit";
            item.Click += (sender, e) => Application.Exit();
            menu.Items.Add(item);
        }

        public void Dispose()
        {
            notifyIcon.Dispose();
        }
    }
}
