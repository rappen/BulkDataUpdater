using Cinteros.XTB.BulkDataUpdater.AppCode;
using Cinteros.XTB.BulkDataUpdater.Forms;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using System.Windows.Forms;
using XrmToolBox;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Args;
using XrmToolBox.Extensibility.Interfaces;

namespace Cinteros.XTB.BulkDataUpdater
{
    public partial class BulkDataUpdater : IGitHubPlugin, IPayPalPlugin, IMessageBusHost, IAboutPlugin, IStatusBarMessenger, IHelpPlugin
    {
        private BulkActionItem baixrmtr;

        public event EventHandler<MessageBusEventArgs> OnOutgoingMessage;

        public event EventHandler<StatusBarMessageEventArgs> SendMessageToStatusBar;

        public void OnIncomingMessage(MessageBusEventArgs message)
        {
            if (message.SourcePlugin == "FetchXML Builder")
            {
                if (message.TargetArgument is string fetch)
                {
                    FetchUpdated(fetch, null);
                }
                else if (message.TargetArgument is Tuple<string, string> fetchlayout)
                {
                    FetchUpdated(fetchlayout.Item1, fetchlayout.Item2);
                }
            }
            else if (message.SourcePlugin == "XRM Tokens Runner" && message.TargetArgument is string tokens && baixrmtr != null)
            {
                baixrmtr.Value = tokens;
                baixrmtr.StringValue = tokens;
                AddAttribute(UpdateAttribute.Show(this, useFriendlyNames, updateAttributes, crmGridView1.SelectedRowRecords?.FirstOrDefault(), baixrmtr), true);
            }
        }

        public void CallingXRMTR(Entity record, BulkActionItem bai)
        {
            if (!PluginManagerExtended.Instance.ValidatedPlugins.Any(p => p.Metadata.Name == "XRM Tokens Runner"))
            {
                MessageBoxEx.Show(this, "Please install the tool 'XRM Tokens Runner'!", "XRM Tokens Runner", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (record == null)
            {
                MessageBoxEx.Show(this, "A record must be available to work with XRM Tokens Runner.", "XRM Tokens Runner", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                baixrmtr = bai;
                OnOutgoingMessage(this, new MessageBusEventArgs("XRM Tokens Runner") { TargetArgument = record });
            }
        }

        public void ShowAboutDialog()
        {
            LogUse("OpenAbout");
            new About(this)
            {
                StartPosition = FormStartPosition.CenterParent
            }.ShowDialog();
        }

        public string DonationDescription => "Bulk Data Updater Fan Club";

        public string EmailAccount => "jonas@rappen.net";

        public string RepositoryName => "BulkDataUpdater";

        public string UserName => "rappen";

        public string HelpUrl => "https://jonasr.app/BDU/";
    }
}