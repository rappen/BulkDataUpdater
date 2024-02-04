namespace Cinteros.Xrm.Common.Forms
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using Rappen.XRM.Helpers.Extensions;
    using Rappen.XTB.Helpers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;
    using XrmToolBox.Extensibility;
    using XTB.BulkDataUpdater.AppCode;

    public partial class SelectViewDialog : Form
    {
        #region Public Fields

        public Entity View;

        #endregion Public Fields

        #region Private Fields

        private List<string> entities;
        private PluginControlBase host;
        private Dictionary<string, List<Entity>> views;

        #endregion Private Fields

        #region Public Constructors

        public SelectViewDialog(PluginControlBase sender)
        {
            InitializeComponent();
            host = sender;
            ScintillaInitialize.InitXML(txtFetch, false);
        }

        #endregion Public Constructors

        #region Internal Methods

        internal void LoadViews(Action action)
        {
            Cursor = Cursors.WaitCursor;
            Enabled = false;
            host.WorkAsync(new WorkAsyncInfo("Loading views...",
                (a) =>
                {
                    views = new Dictionary<string, List<Entity>>();

                    if (views.Count == 0)
                    {
                        var combinedResult = new Dictionary<string, DataCollection<Entity>>();
                        DataCollection<Entity> singleResult;

                        var qex = new QueryExpression();

                        qex.ColumnSet = new ColumnSet("name", "returnedtypecode", "fetchxml", "layoutxml");
                        qex.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
                        qex.AddOrder("name", OrderType.Ascending);

                        foreach (var entity in new string[] { "savedquery", "userquery" })
                        {
                            qex.PageInfo = new PagingInfo();
                            qex.EntityName = entity;

                            singleResult = host.Service.RetrieveMultipleAll(qex).Entities;
                            if (singleResult.Count > 0)
                            {
                                combinedResult.Add(qex.EntityName, singleResult);
                            }
                        }

                        a.Result = combinedResult;
                    }
                })
            {
                PostWorkCallBack = (a) =>
                {
                    if (a.Error != null)
                    {
                        host.ShowErrorDialog(a.Error);
                    }
                    else if (a.Result is Dictionary<string, DataCollection<Entity>> allViews)
                    {
                        foreach (var key in allViews.Keys)
                        {
                            ExtractViews(allViews[key]);
                        }

                        entities = views.Keys.Select(x => x.Split('|')[0]).Distinct().ToList();
                        action();
                    }
                    Cursor = Cursors.Default;
                }
            });
        }

        #endregion Internal Methods

        #region Private Methods

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (cmbView.SelectedItem is ViewItem)
            {
                this.View = ((ViewItem)cmbView.SelectedItem).GetView();
            }
            else
            {
                this.View = null;
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            cmbEntity.SelectedIndex = -1;
            cmbEntity.Items.Clear();

            cmbView.SelectedIndex = -1;
            cmbView.Items.Clear();

            txtFetch.Text = string.Empty;

            LoadViews(PopulateForm);
        }

        private void cmbEntity_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateViews();
        }

        private void cmbView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbView.SelectedItem is ViewItem)
            {
                txtFetch.Text = ((ViewItem)cmbView.SelectedItem).GetFetch();
                ScintillaInitialize.Format(txtFetch);
                btnOk.Enabled = true;
            }
            else
            {
                txtFetch.Text = string.Empty;
                btnOk.Enabled = false;
            }
        }

        private void ExtractViews(DataCollection<Entity> views)
        {
            var suffix = (views.FirstOrDefault().LogicalName == "savedquery") ? "|S" : "|U";

            foreach (var view in views)
            {
                var entityname = view["returnedtypecode"].ToString();

                if (!string.IsNullOrWhiteSpace(entityname))
                {
                    if (!this.views.ContainsKey(entityname + suffix))
                    {
                        this.views.Add(entityname + suffix, new List<Entity>());
                    }
                    this.views[entityname + suffix].Add(view);
                }
            }
        }

        private void PopulateForm()
        {
            cmbView.Items.Clear();
            cmbView.Text = string.Empty;
            txtFetch.Text = string.Empty;

            if (entities != null)
            {
                foreach (var entity in entities)
                {
                    cmbEntity.Items.Add(entity);
                }
            }
            Enabled = true;
        }

        private void SelectViewDialog_Load(object sender, EventArgs e)
        {
            if (host.Service == null)
            {
                MessageBox.Show("Need a connection to load views!", "Connection problem", MessageBoxButtons.OK, MessageBoxIcon.Error);

                DialogResult = System.Windows.Forms.DialogResult.Abort;
                Close();

                return;
            }

            LoadViews(PopulateForm);
        }

        private void UpdateViews()
        {
            cmbView.Items.Clear();
            cmbView.Text = string.Empty;
            txtFetch.Text = string.Empty;
            btnOk.Enabled = false;
            var entity = ControlUtils.GetValueFromControl(cmbEntity);

            if (this.views.ContainsKey(entity + "|S"))
            {
                var views = this.views[entity + "|S"];
                cmbView.Items.Add("-- System Views --");
                foreach (var view in views)
                {
                    cmbView.Items.Add(new ViewItem(view));
                }
            }

            if (this.views.ContainsKey(entity + "|U"))
            {
                var views = this.views[entity + "|U"];
                cmbView.Items.Add("-- Personal Views --");
                foreach (var view in views)
                {
                    cmbView.Items.Add(new ViewItem(view));
                }
            }
        }

        #endregion Private Methods
    }
}