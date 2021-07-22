using System;
using System.Reflection;
using System.Windows.Forms;
using XrmToolBox.Extensibility.Args;
using XrmToolBox.Extensibility.Interfaces;

namespace Cinteros.XTB.BulkDataUpdater
{
    public partial class BulkDataUpdater : IGitHubPlugin, IPayPalPlugin, IMessageBusHost, IAboutPlugin, IStatusBarMessenger, IHelpPlugin
    {
        public event EventHandler<XrmToolBox.Extensibility.MessageBusEventArgs> OnOutgoingMessage;
        public event EventHandler<StatusBarMessageEventArgs> SendMessageToStatusBar;

        public void OnIncomingMessage(XrmToolBox.Extensibility.MessageBusEventArgs message)
        {
            if (message.SourcePlugin == "FetchXML Builder" &&
                message.TargetArgument is string)
            {
                FetchUpdated(message.TargetArgument);
            }
        }

        public void ShowAboutDialog()
        {
            LogUse("OpenAbout");
            var about = new About(this)
            {
                StartPosition = FormStartPosition.CenterParent
            };
            about.lblVersion.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            about.ShowDialog();
        }

        public string DonationDescription => "Donation to Bulk Data Updater for XrmToolBox";

        public string EmailAccount => "jonas@rappen.net";

        public string RepositoryName => "BulkDataUpdater";

        public string UserName => "rappen";

        public string HelpUrl => "https://jonasr.app/BDU/";
    }
}
