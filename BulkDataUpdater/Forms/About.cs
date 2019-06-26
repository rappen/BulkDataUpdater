using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Cinteros.XTB.BulkDataUpdater
{
    public partial class About : Form
    {
        private BulkDataUpdater bdu;

        public About(BulkDataUpdater bdu)
        {
            InitializeComponent();
            this.bdu = bdu;
            PopulateAssemblies();
        }

        private void PopulateAssemblies()
        {
            var assemblies = GetReferencedAssemblies();
            var items = assemblies.Select(a => GetListItem(a)).ToArray();
            listAssemblies.Items.Clear();
            listAssemblies.Items.AddRange(items);
        }

        private ListViewItem GetListItem(AssemblyName a)
        {
            var item = new ListViewItem(a.Name);
            item.SubItems.Add(a.Version.ToString());
            return item;
        }

        private List<AssemblyName> GetReferencedAssemblies()
        {
            var names = Assembly.GetExecutingAssembly().GetReferencedAssemblies()
                    .Where(a => !a.Name.Equals("mscorlib") && !a.Name.StartsWith("System") && !a.Name.Contains("CSharp")).ToList();
            names.Add(Assembly.GetEntryAssembly().GetName());
            names.Add(Assembly.GetExecutingAssembly().GetName());
            names = names.OrderBy(a => assemblyPrioritizer(a.Name)).ToList();
            return names;
        }

        private static string assemblyPrioritizer(string assemblyName)
        {
            return
                assemblyName.Equals("XrmToolBox") ? "AAAAAAAAAAAA" :
                assemblyName.Contains("XrmToolBox") ? "AAAAAAAAAAAB" :
                assemblyName.Equals(Assembly.GetExecutingAssembly().GetName().Name) ? "AAAAAAAAAAAC" :
                assemblyName.Contains("Jonas") ? "AAAAAAAAAAAD" :
                assemblyName.Contains("Innofactor") ? "AAAAAAAAAAAE" :
                assemblyName.Contains("Cinteros") ? "AAAAAAAAAAAF" :
                assemblyName;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            bdu.LogUse("About-OpenHomepage");
            System.Diagnostics.Process.Start("https://github.com/rappen/BulkDataUpdater");
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            bdu.LogUse("About-OpenBlog");
            System.Diagnostics.Process.Start("http://jonasr.app");
        }

        private void linkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            bdu.LogUse("About-OpenTwitter");
            System.Diagnostics.Process.Start("http://twitter.com/rappen");
        }

        private void linkLabel5_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show(@"The evolution of Bulk Data Updater is based on feedback issues and anonymous statistics collected about usage.
The statistics are a valuable source of information for continuing the development to make the tool even easier to use and improve the most popular features.

Thank You,
Jonas", "Anonymous statistics", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
