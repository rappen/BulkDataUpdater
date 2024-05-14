namespace Cinteros.Xrm.Common.Forms
{
    using Cinteros.XTB.BulkDataUpdater;
    using Microsoft.Xrm.Sdk;
    using Rappen.XTB.Helpers;
    using Rappen.XTB.Helpers.ControlItems;
    using System;
    using System.Windows.Forms;
    using XTB.BulkDataUpdater.AppCode;

    public partial class SelectViewDialog : Form
    {
        private BulkDataUpdater Caller;
        public Entity View;

        public SelectViewDialog(BulkDataUpdater caller)
        {
            InitializeComponent();
            Caller = caller;
            ScintillaInitialize.InitXML(txtFetch, false);
            PopulateForm();
        }

        private void PopulateForm()
        {
            if (Caller.views == null || Caller.views.Count == 0)
            {
                Caller.LoadViews(PopulateForm);
                return;
            }
            cmbEntity.Items.Clear();
            var entities = Caller.entities;
            if (entities != null)
            {
                object selectedItem = null;
                foreach (var entity in entities)
                {
                    if (entity.IsIntersect != true && Caller.views.ContainsKey(entity.LogicalName + "|S"))
                    {
                        var ei = new EntityMetadataItem(entity, Caller.useFriendlyNames, true);
                        cmbEntity.Items.Add(ei);
                        if (entity.LogicalName == Caller.entitymeta?.LogicalName)
                        {
                            selectedItem = ei;
                        }
                    }
                }
                if (selectedItem != null)
                {
                    cmbEntity.SelectedItem = selectedItem;
                    UpdateViews();
                }
            }
            Enabled = true;
        }

        private void cmbEntity_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateViews();
        }

        private void UpdateViews()
        {
            cmbView.Items.Clear();
            cmbView.Text = "";
            txtFetch.Text = "";
            btnOk.Enabled = false;
            var entity = ControlUtils.GetValueFromControl(cmbEntity);
            object selectedItem = null;
            if (Caller.views.ContainsKey(entity + "|S"))
            {
                var views = Caller.views[entity + "|S"];
                cmbView.Items.Add("-- System Views --");
                foreach (var view in views)
                {
                    var vi = new ViewItem(view);
                    cmbView.Items.Add(vi);
                    //if (view.Id.Equals(Caller.settings.LastOpenedViewId))
                    //{
                    //    selectedItem = vi;
                    //}
                }
            }
            if (Caller.views.ContainsKey(entity + "|U"))
            {
                var views = Caller.views[entity + "|U"];
                cmbView.Items.Add("-- Personal Views --");
                foreach (var view in views)
                {
                    var vi = new ViewItem(view);
                    cmbView.Items.Add(vi);
                    //if (view.Id.Equals(Caller.settings.LastOpenedViewId))
                    //{
                    //    selectedItem = vi;
                    //}
                }
            }
            if (selectedItem != null)
            {
                cmbView.SelectedItem = selectedItem;
                UpdateFetch();
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (cmbView.SelectedItem is ViewItem)
            {
                View = ((ViewItem)cmbView.SelectedItem).GetView();
                //Caller.settings.LastOpenedViewEntity = ControlUtils.GetValueFromControl(cmbEntity);
                //Caller.settings.LastOpenedViewId = View.Id;
            }
            else
            {
                View = null;
            }
        }

        private void cmbView_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateFetch();
        }

        private void UpdateFetch()
        {
            if (cmbView.SelectedItem is ViewItem viewitem)
            {
                txtFetch.Text = viewitem.GetFetch();
                ScintillaInitialize.Format(txtFetch);
                btnOk.Enabled = true;
            }
            else
            {
                txtFetch.Text = "";
                btnOk.Enabled = false;
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            Enabled = false;
            cmbView.SelectedIndex = -1;
            cmbEntity.SelectedIndex = -1;
            txtFetch.Text = "";
            Caller.views = null;
            Caller.LoadViews(PopulateForm);
        }

        private void cmbEntity_KeyDown(object sender, KeyEventArgs e)
        {
            cmbEntity.DroppedDown = false;
        }
    }
}