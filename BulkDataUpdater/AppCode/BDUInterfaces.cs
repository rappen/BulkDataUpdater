﻿using System;
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
            else if (message.SourcePlugin == "XRM Tokens Runner" && message.TargetArgument is string tokens)
            {
                txtValueCalc.Text = tokens;
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