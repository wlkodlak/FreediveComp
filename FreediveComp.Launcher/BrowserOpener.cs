using System;
using System.Diagnostics;
using System.Text;

namespace FreediveComp.Launcher
{
    public class BrowserOpener
    {
        private readonly string baseWebUri;
        private readonly string adminToken;

        public BrowserOpener(string baseWebUri, string adminToken)
        {
            this.baseWebUri = baseWebUri;
            this.adminToken = adminToken;
        }

        public void OpenHomepage()
        {
            OpenBrowser("");
        }

        public void OpenRace(string raceId)
        {
            OpenBrowser(raceId + "/homepage");
        }

        public void OpenNewRace()
        {
            OpenBrowser(Guid.NewGuid().ToString() + "/create");
        }

        private void OpenBrowser(string relativePath)
        {
            var sb = new StringBuilder();
            sb.Append(baseWebUri);
            sb.Append(relativePath);
            if (adminToken != null)
            {
                sb.Append("?token=").Append(adminToken);
            }
            Process.Start(sb.ToString());
        }
    }
}
