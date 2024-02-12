namespace Cinteros.XTB.BulkDataUpdater
{
    using AppCode;
    using Cinteros.XTB.BulkDataUpdater.Forms;
    using McTools.Xrm.Connection;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Metadata;
    using Microsoft.Xrm.Sdk.Query;
    using Rappen.XRM.Helpers.Extensions;
    using Rappen.XTB.Helpers.ControlItems;
    using Rappen.XTB.Helpers.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows.Forms;
    using System.Xml;
    using Xrm.Common.Forms;
    using XrmToolBox;
    using XrmToolBox.Extensibility;

    using HelpFetch = Rappen.XRM.Helpers.FetchXML;

    public partial class BulkDataUpdater : PluginControlBase
    {
        #region Internal Fields

        internal static bool useFriendlyNames = false;

        #endregion Internal Fields

        #region Private Fields

        private const string aiEndpoint = "https://dc.services.visualstudio.com/v2/track";
        private const string aiKey1 = "eed73022-2444-45fd-928b-5eebd8fa46a6";    // jonas@rappen.net tenant, XrmToolBox
        private const string aiKey2 = "d46e9c12-ee8b-4b28-9643-dae62ae7d3d4";    // jonas@jonasr.app, XrmToolBoxTools
        private AppInsights ai1 = new AppInsights(aiEndpoint, aiKey1, Assembly.GetExecutingAssembly(), "Bulk Data Updater");
        private AppInsights ai2 = new AppInsights(aiEndpoint, aiKey2, Assembly.GetExecutingAssembly(), "Bulk Data Updater");

        private static List<EntityMetadata> entities;
        private static EntityMetadata entitymeta;
        private static string fetchTemplate = "<fetch><entity name=\"\"/></fetch>";
        private BDUJob job;
        private UpdateAttributes updateAttributes;

        private string deleteWarningText;

        private int fetchResulCount = -1;

        private EntityCollection records;
        private Entity view;
        private Version currentversion;
        private readonly Version bypasspluginminversion = new Version(9, 2);

        private bool working = false;
        private string currentconnection;

        private List<string> isOnForms;
        private List<string> isOnViews;

        #endregion Private Fields

        #region Public Constructors

        public BulkDataUpdater()
        {
            InitializeComponent();
            tslAbout.ToolTipText = $"Version: {Assembly.GetExecutingAssembly().GetName().Version}";
            var prop = Rappen.XRM.Helpers.Extensions.MetadataExtensions.entityProperties.ToList();
            prop.Add("Attributes");
            Rappen.XRM.Helpers.Extensions.MetadataExtensions.entityProperties = prop.ToArray();
            deleteWarningText = txtDeleteWarning.Text;
        }

        #endregion Public Constructors

        #region Public Methods

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            SaveSetting();
            LogUse("Close", ai2: true);
        }

        #endregion Public Methods

        #region Private Methods

        private void EnableControls(bool enabled, bool cancel = false)
        {
            MethodInvoker mi = delegate
            {
                try
                {
                    if (enabled)
                    {
                        Enabled = true;
                    }
                    splitContainer1.Enabled = enabled;
                    tsbOpenJob.Enabled = !cancel && enabled;
                    tsbSaveJob.Enabled = !cancel && enabled;
                    tsbFriendly.Enabled = !cancel && enabled;
                    tsbRaw.Enabled = !cancel && enabled;
                    tsbCancel.Enabled = cancel;
                    gb1select.Enabled = !cancel && enabled && Service != null;
                    btnRefresh.Enabled = !cancel && gb1select.Enabled && !string.IsNullOrWhiteSpace(job?.FetchXML);
                    gb2attribute.Enabled = !cancel && gb1select.Enabled && records != null && records.Entities.Count > 0;
                    panUpdButton.Enabled = !cancel && gb2attribute.Enabled && cmbAttribute.SelectedItem is AttributeMetadataItem;
                    btnAdd.Enabled = !cancel && panUpdButton.Enabled && IsValueValid();
                    gb3attributes.Enabled = !cancel && gb2attribute.Enabled && lvAttributes.Items.Count > 0;
                    gbExecute.Enabled = !cancel && (
                        (tabControl1.SelectedTab == tabUpdate && lvAttributes.Items.Count > 0) ||
                        (tabControl1.SelectedTab == tabAssign && xrmRecordAssign.Record != null) ||
                        (tabControl1.SelectedTab == tabSetState && cbSetStatus.SelectedItem != null && cbSetStatusReason.SelectedItem != null) ||
                        (tabControl1.SelectedTab == tabDelete));
                }
                catch
                {
                    // Now what?
                }
            };
            if (InvokeRequired)
            {
                Invoke(mi);
            }
            else
            {
                mi();
            }
        }

        private void FetchUpdated(string fetch, string layout)
        {
            if (job == null)
            {
                job = new BDUJob();
            }
            job.FetchXML = fetch;
            job.LayoutXML = layout;
            if (!string.IsNullOrWhiteSpace(fetch))
            {
                RetrieveRecords();
            }
        }

        private List<AttributeMetadata> GetDisplayAttributes(string entityName)
        {
            bool IsRequired(AttributeMetadata meta)
            {
                return (meta.RequiredLevel?.Value == AttributeRequiredLevel.ApplicationRequired ||
                        meta.RequiredLevel?.Value == AttributeRequiredLevel.SystemRequired) &&
                       string.IsNullOrEmpty(meta.AttributeOf);
            }
            var result = new List<AttributeMetadata>();
            var fetch = HelpFetch.Fetch.FromString(job.FetchXML);
            AttributeMetadata[] attributes = null;
            if (entities?.FirstOrDefault(e => e.LogicalName == entityName) is EntityMetadata entity)
            {
                attributes = entity.Attributes;
                if (attributes != null)
                {
                    foreach (var attribute in attributes)
                    {
                        var yes = updateAttributes.Everything ||
                            (updateAttributes.ImportSequenceNumber && attribute.LogicalName == "importsequencenumber") ||
                            (updateAttributes.UnallowedUpdate && !attribute.IsValidForUpdate.Value == true && attribute.LogicalName != "importsequencenumber");
                        if (updateAttributes.CombinationAnd)
                        {
                            yes = yes ||
                                ((!updateAttributes.Required || IsRequired(attribute)) &&
                                 (!updateAttributes.Recommended || attribute.RequiredLevel?.Value == AttributeRequiredLevel.Recommended) &&
                                 (!updateAttributes.InQuery || fetch.Entity.Attributes.Any(a => a.Name == attribute.LogicalName)) &&
                                 (!updateAttributes.OnForm || IsOnAnyForm(fetch.Entity.Name, attribute.LogicalName)) &&
                                 (!updateAttributes.OnView || IsOnAnyView(fetch.Entity.Name, attribute.LogicalName)));
                        }
                        else
                        {
                            yes = yes ||
                                (updateAttributes.Required && IsRequired(attribute)) ||
                                (updateAttributes.Recommended && attribute.RequiredLevel?.Value == AttributeRequiredLevel.Recommended) ||
                                (updateAttributes.InQuery && fetch.Entity.Attributes.Any(a => a.Name == attribute.LogicalName)) ||
                                (updateAttributes.OnForm && IsOnAnyForm(fetch.Entity.Name, attribute.LogicalName)) ||
                                (updateAttributes.OnView && IsOnAnyView(fetch.Entity.Name, attribute.LogicalName));
                        }
                        if (yes)
                        {
                            result.Add(attribute);
                        }
                    }
                }
            }
            return result;
        }

        private bool IsOnAnyForm(string entity, string attribute)
        {
            IEnumerable<string> FindCellControlFields(XmlNode node)
            {
                var result = new List<string>();
                if (node.Name == "cell" &&
                    node.SelectSingleNode("control") is XmlNode control &&
                    control.AttributeValue("datafieldname") is string field &&
                    !string.IsNullOrEmpty(field))
                {
                    result.Add(field);
                }
                node.ChildNodes.Cast<XmlNode>().ToList().ForEach(n => result.AddRange(FindCellControlFields(n)));
                return result;
            }
            if (isOnForms == null)
            {
                if (working)
                {
                    return false;
                }
                working = true;
                Enabled = false;
                Cursor = Cursors.WaitCursor;
                WorkAsync(new WorkAsyncInfo
                {
                    Message = "Loading forms...",
                    Work = (w, a) =>
                    {
                        if (Service == null)
                        {
                            throw new Exception("Need a connection to load forms.");
                        }
                        var query = new QueryExpression("systemform");
                        query.ColumnSet.AddColumns("name", "type", "formxml");
                        query.Criteria.AddCondition("objecttypecode", ConditionOperator.Equal, entity);
                        query.Criteria.AddCondition("formactivationstate", ConditionOperator.Equal, 1);
                        query.Criteria.AddCondition("formxml", ConditionOperator.NotNull);
                        a.Result = Service.RetrieveMultipleAll(query);
                    },
                    PostWorkCallBack = (a) =>
                    {
                        Cursor = Cursors.Default;
                        if (a.Error != null)
                        {
                            ShowErrorDialog(a.Error);
                        }
                        else if (a.Result is EntityCollection forms)
                        {
                            isOnForms = new List<string>();
                            foreach (var form in forms.Entities)
                            {
                                var formxml = form.GetAttributeValue<string>("formxml");
                                var nodes = formxml.ToXml().ChildNodes;
                                foreach (XmlNode node in nodes)
                                {
                                    isOnForms.AddRange(FindCellControlFields(node));
                                }
                            }
                            isOnForms = isOnForms.Distinct().ToList();
                            RefreshAttributes();
                        }
                        Enabled = true;
                        working = false;
                    }
                });
                return false;
            }
            return isOnForms.Contains(attribute);
        }

        private bool IsOnAnyView(string entity, string attribute)
        {
            if (isOnViews == null)
            {
                if (working)
                {
                    return false;
                }
                working = true;
                Enabled = false;
                Cursor = Cursors.WaitCursor;
                WorkAsync(new WorkAsyncInfo
                {
                    Message = "Loading views...",
                    Work = (w, a) =>
                    {
                        if (Service == null)
                        {
                            throw new Exception("Need a connection to load views.");
                        }
                        var qexs = new QueryExpression("savedquery");
                        qexs.ColumnSet = new ColumnSet("name", "returnedtypecode", "layoutxml", "iscustomizable");
                        qexs.Criteria.AddCondition("returnedtypecode", ConditionOperator.Equal, entity);
                        qexs.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
                        qexs.Criteria.AddCondition("layoutxml", ConditionOperator.NotNull);
                        a.Result = Service.RetrieveMultipleAll(qexs);
                    },
                    PostWorkCallBack = (a) =>
                    {
                        Cursor = Cursors.Default;
                        if (a.Error != null)
                        {
                            ShowErrorDialog(a.Error);
                        }
                        else if (a.Result is EntityCollection views)
                        {
                            isOnViews = new List<string>();
                            foreach (var view in views.Entities)
                            {
                                var layout = view.GetAttributeValue<string>("layoutxml");
                                if (layout.ToXml().SelectSingleNode("grid") is XmlElement grid &&
                                    grid.SelectSingleNode("row") is XmlElement row)
                                {
                                    isOnViews.AddRange(row.ChildNodes
                                        .Cast<XmlNode>()
                                        .Where(n => n.Name == "cell")
                                        .Select(c => c.AttributeValue("name")));
                                }
                            }
                            isOnViews = isOnViews.Distinct().ToList();
                            RefreshAttributes();
                        }
                        Enabled = true;
                        working = false;
                    }
                });
                return false;
            }
            return isOnViews.Contains(attribute);
        }

        private void GetFromEditor()
        {
            var fetchwin = new XmlContentDisplayDialog(job?.FetchXML ?? fetchTemplate, "Enter FetchXML to retrieve records to update", true, true);
            fetchwin.StartPosition = FormStartPosition.CenterParent;
            if (fetchwin.ShowDialog() == DialogResult.OK)
            {
                FetchUpdated(fetchwin.txtXML.Text, null);
            }
        }

        private void GetFromFXB()
        {
            OnOutgoingMessage(this, new MessageBusEventArgs("FetchXML Builder") { TargetArgument = job?.FetchXML });
        }

        private void GetRecords(string tag)
        {
            switch (tag)
            {
                case "Edit": // Edit
                    GetFromEditor();
                    break;

                case "FXB": // FXB
                    try
                    {
                        GetFromFXB();
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        MessageBox.Show("FetchXML Builder is not installed.\nDownload latest version from the Tool Library.", "FetchXML Builder",
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    catch (PluginNotFoundException)
                    {
                        MessageBox.Show("FetchXML Builder was not found.\nInstall it from the XrmToolBox Tool Library.", "FetchXML Builder",
                                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    break;

                case "File": // File
                    FetchUpdated(OpenFile(), null);
                    break;

                case "View": // View
                    OpenView();
                    break;

                case "Refresh":
                    FetchUpdated(job?.FetchXML, job?.LayoutXML);
                    break;

                default:
                    MessageBox.Show("Select record source.", "Get Records", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    break;
            }
            LogUse(tag, records?.Entities?.Count);
        }

        private void InitializeTab()
        {
            var tempworker = working;
            working = true;
            if (tabControl1.SelectedTab == tabUpdate)
            {
                btnExecute.Text = "Update records";
                SetExecuteOptions(job.Update.ExecuteOptions);
                SetUpdateFromJob(job.Update);
            }
            else if (tabControl1.SelectedTab == tabAssign)
            {
                btnExecute.Text = "Assign records";
                SetExecuteOptions(job.Assign.ExecuteOptions);
                SetAssignFromJob(job.Assign);
            }
            else if (tabControl1.SelectedTab == tabSetState)
            {
                btnExecute.Text = "Update records";
                SetExecuteOptions(job.SetState.ExecuteOptions);
                SetSetStateFromJob(job.SetState);
            }
            else if (tabControl1.SelectedTab == tabDelete)
            {
                btnExecute.Text = "Delete records";
                SetExecuteOptions(job.Delete.ExecuteOptions);
            }
            working = tempworker;
            EnableControls(true);
        }

        private void LoadMissingAttributesForRecord(Entity record, IEnumerable<BulkActionItem> attributes)
        {
            var newcols = new ColumnSet(attributes.Select(a => a.Attribute.Metadata.LogicalName).ToArray());
            if (newcols.Columns.Contains("statuscode"))
            {
                newcols.AddColumn("statecode");
            }
            var newrecord = Service.Retrieve(record.LogicalName, record.Id, newcols);
            foreach (var attribute in newrecord.Attributes.Keys)
            {
                if (newrecord.Contains(attribute) && !record.Contains(attribute))
                {
                    record.Attributes.Add(attribute, newrecord[attribute]);
                }
            }
        }

        private void LoadGlobalSetting()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            if (!SettingsManager.Instance.TryLoad(typeof(BulkDataUpdater), out GlobalSettings globalsettings, "[Global]"))
            {
                globalsettings = new GlobalSettings();
            }
            if (!version.Equals(globalsettings.CurrentVersion))
            {
                // Reset some settings when new version is deployed
                globalsettings.CurrentVersion = version;
                SettingsManager.Instance.Save(typeof(BulkDataUpdater), globalsettings, "[Global]");
                Process.Start($"https://jonasr.app/BDU/releases/#{version}");
            }
        }

        private void LoadSetting()
        {
            if (!SettingsManager.Instance.TryLoad(typeof(BulkDataUpdater), out Settings settings, ConnectionDetail?.ConnectionName))
            {
                settings = new Settings();
            }
            job = settings.Job;
            updateAttributes = settings.UpdateAttributes;
            fetchResulCount = settings.FetchResultCount;
            tsbFriendly.Checked = settings.Friendly;
            tsbRaw.Checked = !settings.Friendly;
            tsbFriendly_Click(null, null);
            InitializeTab();
        }

        private string OpenFile()
        {
            var result = "";
            var ofd = new OpenFileDialog
            {
                Title = "Select an XML file containing FetchXML",
                Filter = "XML file (*.xml)|*.xml"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                EnableControls(false);
                var fetchDoc = new XmlDocument();
                fetchDoc.Load(ofd.FileName);

                if (fetchDoc.DocumentElement.Name != "fetch" ||
                    fetchDoc.DocumentElement.ChildNodes.Count > 0 &&
                    fetchDoc.DocumentElement.ChildNodes[0].Name == "fetch")
                {
                    MessageBox.Show(this, "Invalid Xml: Definition XML root must be fetch!", "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    result = fetchDoc.OuterXml;
                    EnableControls(true);
                }
            }
            return result;
        }

        private void OpenView()
        {
            EnableControls(false);
            var viewselector = new SelectViewDialog(this);
            viewselector.StartPosition = FormStartPosition.CenterParent;
            if (viewselector.ShowDialog() == DialogResult.OK)
            {
                view = viewselector.View;
                var fetchDoc = new XmlDocument();
                if (view.Contains("fetchxml"))
                {
                    fetchDoc.LoadXml(view["fetchxml"].ToString());
                    view.TryGetAttributeValue("layoutxml", out string layout);
                    FetchUpdated(fetchDoc.OuterXml, layout);
                }
            }
            EnableControls(true);
        }

        private void RefreshAttributes()
        {
            var selected = cmbAttribute.SelectedItem as AttributeMetadataItem;
            cmbAttribute.Items.Clear();
            if (records != null)
            {
                var entityName = records.EntityName;
                var attributes = GetDisplayAttributes(entityName);
                attributes.ForEach(a => AttributeMetadataItem.AddAttributeToComboBox(cmbAttribute, a, true, useFriendlyNames));
                if (selected != null)
                {
                    cmbAttribute.SelectedItem = cmbAttribute.Items.Cast<AttributeMetadataItem>().FirstOrDefault(a => a.Metadata?.LogicalName == selected.Metadata?.LogicalName);
                }
            }
        }

        private void RefreshGridRecords()
        {
            EnableControls(false);
            crmGridView1.ShowFriendlyNames = useFriendlyNames;
            crmGridView1.ShowLocalTimes = useFriendlyNames;
            crmGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            EnableControls(true);
        }

        private void SaveSetting()
        {
            var settings = new Settings()
            {
                Job = job ?? new BDUJob(),
                UpdateAttributes = updateAttributes ?? new UpdateAttributes(),
                FetchResultCount = fetchResulCount,
                Friendly = tsbFriendly.Checked,
            };
            settings.Job.Info = null;
            SettingsManager.Instance.Save(typeof(BulkDataUpdater), settings, ConnectionDetail?.ConnectionName);
        }

        private void UpdateIncludeCount()
        {
            var count = GetIncludedRecords()?.Count();
            var entity = entities?.FirstOrDefault(e => e.LogicalName == records?.EntityName)?.DisplayCollectionName?.UserLocalizedLabel?.Label;
            lblIncludedRecords.Text = $"{count} records";
            lblUpdateHeader.Text = $"Update {count} {entity}";
            lblAssignHeader.Text = $"Assign {count} {entity}";
            lblStateHeader.Text = $"Update {count} {entity}";
            lblDeleteHeader.Text = $"Delete {count} {entity}";
            txtDeleteWarning.Text = deleteWarningText.Replace("[nn]", rbIncludeSelected.Checked ? count.ToString() : "ALL");
        }

        private void UpdateValueField()
        {
            cmbValue.Enabled = rbSetValue.Checked;
            txtValueMultiline.Enabled = rbSetValue.Checked;
            btnLookupValue.Enabled = rbSetValue.Checked;
            if (!rbSetValue.Checked && xrmRecordAttribute.Record != null)
            {
                xrmRecordAttribute.Record = null;
            }
            chkOnlyChange.Enabled = !rbSetTouch.Checked;
            chkOnlyChange.Checked = chkOnlyChange.Checked && !rbSetTouch.Checked;

            var attribute = (AttributeMetadataItem)cmbAttribute.SelectedItem;
            rbSetNull.Enabled = attribute != null;
            cmbValue.Items.Clear();
            var value = rbSetValue.Checked;
            var lookup = false;
            var multitext = false;
            var multisel = false;
            var calc = rbCalculate.Checked;
            if (rbSetValue.Checked && attribute != null)
            {
                if (attribute.Metadata is EnumAttributeMetadata enummeta)
                {
                    var options = enummeta.OptionSet;
                    if (options != null)
                    {
                        foreach (var option in options.Options)
                        {
                            cmbValue.Items.Add(new OptionMetadataItem(option, true));
                        }
                    }
                    cmbValue.DropDownStyle = ComboBoxStyle.DropDownList;
                }
                else if (attribute.Metadata is MultiSelectPicklistAttributeMetadata multimeta)
                {
                    chkMultiSelects.Items.Clear();
                    var options = multimeta.OptionSet;
                    if (options != null)
                    {
                        foreach (var option in options.Options)
                        {
                            chkMultiSelects.Items.Add(new OptionMetadataItem(option, true));
                        }
                    }
                    value = false;
                    multisel = true;
                }
                else if (attribute.Metadata is BooleanAttributeMetadata boolmeta)
                {
                    var options = boolmeta.OptionSet;
                    if (options != null)
                    {
                        cmbValue.Items.Add(new OptionMetadataItem(options.TrueOption, true));
                        cmbValue.Items.Add(new OptionMetadataItem(options.FalseOption, true));
                    }
                    cmbValue.DropDownStyle = ComboBoxStyle.DropDownList;
                }
                else if (attribute.Metadata is MemoAttributeMetadata)
                {
                    value = false;
                    multitext = true;
                }
                else if (attribute.Metadata is LookupAttributeMetadata lkpmeta)
                {
                    value = false;
                    lookup = true;
                    cdsLookupDialog.Service = Service;
                    cdsLookupDialog.LogicalNames = lkpmeta.Targets;
                    if (!cdsLookupDialog.LogicalNames.Contains(xrmRecordAttribute.LogicalName))
                    {
                        xrmRecordAttribute.Record = null;
                    }
                }
                else
                {
                    cmbValue.DropDownStyle = ComboBoxStyle.Simple;
                }
            }
            panUpdValue.Visible = value;
            panUpdLookup.Visible = lookup;
            panUpdCalc.Visible = calc;
            panUpdTextMulti.Visible = multitext;
            panMultiChoices.Visible = multisel;
            PreviewCalc();
            EnableControls(true);
        }

        #endregion Private Methods

        #region Form Event Handlers

        private void btnAdd_Click(object sender, EventArgs e)
        {
            AddAttribute();
        }

        private void btnExecute_Click(object sender, EventArgs e)
        {
            lblUpdateStatus.Text = "Initializing...";
            UpdateJobFromUI(tabControl1.SelectedTab);
            if (tabControl1.SelectedTab == tabUpdate)
            {
                UpdateRecords();
            }
            else if (tabControl1.SelectedTab == tabAssign)
            {
                AssignRecords();
            }
            else if (tabControl1.SelectedTab == tabSetState)
            {
                SetStateRecords();
            }
            else if (tabControl1.SelectedTab == tabDelete)
            {
                DeleteRecords();
            }
        }

        private void btnGetRecords_Click(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                GetRecords(btn.Tag?.ToString());
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            RemoveAttribute();
        }

        private void cbSetStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadStatuses(job?.SetState?.Status);
            EnableControls(true);
        }

        private void cbSetStatusReason_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateJobSetState(job?.SetState);
            EnableControls(true);
        }

        private void genericInputChanged(object sender, EventArgs e)
        {
            EnableControls(true);
        }

        private void cmbAttribute_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateValueField();
        }

        private void crmGridView1_SelectionChanged(object sender, EventArgs e)
        {
            UpdateIncludeCount();
            PreviewCalc();
        }

        private void DataUpdater_ConnectionUpdated(object sender, ConnectionUpdatedEventArgs e)
        {
            currentversion = new Version(e.ConnectionDetail?.OrganizationVersion);
            xrmRecordAttribute.Service = Service;
            crmGridView1.DataSource = null;
            entities = null;
            if (string.IsNullOrEmpty(currentconnection) ||
                currentconnection == e.ConnectionDetail.ConnectionName ||
                MessageBox.Show($"Changing connection from '{currentconnection}' to '{e.ConnectionDetail.ConnectionName}'.\n\nWe usually reload settings for the new connection, shall we?", "Connected Connection", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                LoadSetting();
            }
            currentconnection = e.ConnectionDetail.ConnectionName;
            EnableControls(false);
            this.GetAllEntityMetadatas(AfterEntitiesLoaded);
        }

        private void DataUpdater_Load(object sender, EventArgs e)
        {
            LoadGlobalSetting();
            LogUse("Load", ai2: true);
        }

        private void AfterEntitiesLoaded(IEnumerable<EntityMetadata> metadatas, bool forcereload)
        {
            entities = metadatas?.ToList();
            EnableControls(true);
            if (entities != null)
            {
                UseJob(!string.IsNullOrWhiteSpace(job?.FetchXML) && fetchResulCount > 0 && fetchResulCount < 100);
            }
        }

        private void lvAttributes_SelectedIndexChanged(object sender, EventArgs e)
        {
            PopulateFromAddedAttribute();
        }

        private void rbInclude_CheckedChanged(object sender, EventArgs e)
        {
            job.IncludeAll = rbIncludeAll.Checked;
            UpdateIncludeCount();
        }

        private void rbSet_CheckedChanged(object sender, EventArgs e)
        {
            UpdateValueField();
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            InitializeTab();
        }

        private void tsbCancel_Click(object sender, EventArgs e)
        {
            EnableControls(false, false);
            Cursor = Cursors.WaitCursor;
            CancelWorker();
        }

        private void tslAbout_Click(object sender, EventArgs e)
        {
            ShowAboutDialog();
        }

        private void tslDoc_Click(object sender, EventArgs e)
        {
            Process.Start("https://jonasr.app/BDU/");
        }

        private void tsbFriendly_Click(object sender, EventArgs e)
        {
            tsbFriendly.Checked = sender == tsbFriendly;
            tsbRaw.Checked = !tsbFriendly.Checked;
            useFriendlyNames = tsbFriendly.Checked;
            RefreshAttributes();
            RefreshGridRecords();
        }

        #endregion Form Event Handlers

        private void crmGridView1_RecordDoubleClick(object sender, Rappen.XTB.Helpers.Controls.XRMRecordEventArgs e)
        {
            if (e.Entity != null)
            {
                string url = GetEntityUrl(e.Entity);
                if (!string.IsNullOrEmpty(url))
                {
                    ConnectionDetail.OpenUrlWithBrowserProfile(new Uri(url));
                }
            }
        }

        private string GetEntityUrl(Entity entity)
        {
            var entref = entity.ToEntityReference();
            switch (entref.LogicalName)
            {
                case "activitypointer":
                    if (!entity.Contains("activitytypecode"))
                    {
                        MessageBox.Show("To open records of type activitypointer, attribute 'activitytypecode' must be included in the query.", "Open Record", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        entref.LogicalName = string.Empty;
                    }
                    else
                    {
                        entref.LogicalName = entity["activitytypecode"].ToString();
                    }
                    break;

                case "activityparty":
                    if (!entity.Contains("partyid"))
                    {
                        MessageBox.Show("To open records of type activityparty, attribute 'partyid' must be included in the query.", "Open Record", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        entref.LogicalName = string.Empty;
                    }
                    else
                    {
                        var party = (EntityReference)entity["partyid"];
                        entref.LogicalName = party.LogicalName;
                        entref.Id = party.Id;
                    }
                    break;
            }
            return GetEntityReferenceUrl(entref);
        }

        private string GetEntityReferenceUrl(EntityReference entref)
        {
            if (!string.IsNullOrEmpty(entref.LogicalName) && !entref.Id.Equals(Guid.Empty))
            {
                var url = GetFullWebApplicationUrl();
                url = string.Concat(url,
                    url.EndsWith("/") ? "" : "/",
                    "main.aspx?etn=",
                    entref.LogicalName,
                    "&pagetype=entityrecord&id=",
                    entref.Id.ToString());
                return url;
            }
            return string.Empty;
        }

        public string GetFullWebApplicationUrl()
        {
            var url = ConnectionDetail.WebApplicationUrl;
            if (string.IsNullOrEmpty(url))
            {
                url = ConnectionDetail.ServerName;
            }
            if (!url.ToLower().StartsWith("http"))
            {
                url = string.Concat("http://", url);
            }
            var uri = new Uri(url);
            if (!uri.Host.EndsWith(".dynamics.com"))
            {
                if (string.IsNullOrEmpty(uri.AbsolutePath.Trim('/')))
                {
                    uri = new Uri(uri, ConnectionDetail.Organization);
                }
            }
            return uri.ToString();
        }

        private void btnLookupValue_Click(object sender, EventArgs e)
        {
            switch (cdsLookupDialog.ShowDialog(this))
            {
                case DialogResult.OK:
                    xrmRecordAttribute.Record = cdsLookupDialog.Record;
                    break;

                case DialogResult.Abort:
                    xrmRecordAttribute.Record = null;
                    break;
            }
            EnableControls(true);
        }

        private void btnCalcHelp_Click(object sender, EventArgs e)
        {
            Process.Start("https://jonasr.app/bdu/#calc");
        }

        #region Implementation of ILogger

        public void EndSection()
        {
            LogInfo("<----");
        }

        public void Log(string message)
        {
            LogInfo(message);
        }

        public void Log(Exception ex)
        {
            LogError(ex.ToString());
        }

        public void StartSection(string name = null)
        {
            LogInfo($"----> {name}");
        }

        #endregion Implementation of ILogger

        private void txtValueCalc_TextChanged(object sender, EventArgs e)
        {
            AwaitCalc();
            EnableControls(true);
        }

        private void AwaitCalc()
        {
            txtCalcPreview.ForeColor = SystemColors.GrayText;
            tmCalc.Stop();
            tmCalc.Start();
        }

        private void PreviewCalc()
        {
            tmCalc.Enabled = false;
            if (tabControl1.SelectedTab == tabUpdate &&
                rbCalculate.Checked &&
                Service != null &&
                crmGridView1.SelectedCellRecords?.FirstOrDefault() is Entity record)
            {
                try
                {
                    var attmeta = cmbAttribute.SelectedItem as AttributeMetadataItem;
                    var preview = CalculateValue(record, attmeta?.Metadata, txtValueCalc.Text, 1);
                    if (preview is EntityReference refent)
                    {
                        preview = $"{refent.LogicalName}:{refent.Id}";
                    }
                    else if (preview is OptionSetValue opt)
                    {
                        preview = opt.Value;
                    }
                    else if (preview is OptionSetValueCollection mopt)
                    {
                        preview = string.Join(";", mopt.Select(m => m.Value.ToString()));
                    }
                    else if (preview is Money money)
                    {
                        preview = money.Value;
                    }
                    txtCalcPreview.Text = preview?.ToString();
                }
                catch (Exception ex)
                {
                    txtCalcPreview.Text = $"Error: {ex.Message}";
                }
                txtCalcPreview.ForeColor = SystemColors.WindowText;
            }
            else if (!string.IsNullOrEmpty(txtCalcPreview.Text))
            {
                txtCalcPreview.Text = string.Empty;
            }
        }

        private void tmCalc_Tick(object sender, EventArgs e)
        {
            PreviewCalc();
        }

        private void chkMultiSelects_SelectedIndexChanged(object sender, EventArgs e)
        {
            EnableControls(true);
        }

        private void link_XRMTRname_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (!PluginManagerExtended.Instance.Plugins.Any(p => p.Metadata.Name == "XRM Tokens Runner"))
            {
                MessageBox.Show("Please install the tool 'XRM Tokens Runner'!", "XRM Tokens Runner", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!(crmGridView1.SelectedCellRecords.FirstOrDefault() is Entity record))
            {
                MessageBox.Show("A record must be available to work with XRM Tokens Runner.", "XRM Tokens Runner", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                OnOutgoingMessage(this, new MessageBusEventArgs("XRM Tokens Runner") { TargetArgument = record });
            }
        }

        private void btnAssignSelect_Click(object sender, EventArgs e)
        {
            SelectingAssigner();
        }

        private void linkBypassPlugins_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://docs.microsoft.com/power-apps/developer/data-platform/bypass-custom-business-logic?WT.mc_id=DX-MVP-5002475");
        }

        private void linkBulkOperations_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://docs.microsoft.com/power-apps/developer/data-platform/bulk-operations?WT.mc_id=DX-MVP-5002475");
        }

        private void chkBypassPlugins_CheckedChanged(object sender, EventArgs e)
        {
            if (!working && chkBypassPlugins.Checked)
            {
                if (currentversion < bypasspluginminversion)
                {
                    MessageBox.Show(
                        $"This feature is not available in version {ConnectionDetail?.OrganizationVersion}\n" +
                        $"Need version {bypasspluginminversion} or later.", "Bypass Custom Business Logic",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    chkBypassPlugins.Checked = false;
                }
                else if (MessageBox.Show(
                    "Make sure you know exactly what this checkbox means.\n" +
                    "Please read the docs - click the link first!\n\n" +
                    "Are you OK to continue?", "Bypass Custom Business Logic",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes)
                {
                    chkBypassPlugins.Checked = false;
                }
            }
            executeOption_Changed(sender, e);
        }

        private void tsbOpenJob_Click(object sender, EventArgs e)
        {
            var opendld = new OpenFileDialog
            {
                Filter = "BDU xml (*.bdu.xml)|*.bdu.xml",
                DefaultExt = ".bdu.xml",
                Title = "Open a BDU job"
            };
            if (!string.IsNullOrEmpty(job.Info?.OriginalPath))
            {
                opendld.InitialDirectory = Path.GetDirectoryName(job.Info.OriginalPath);
            }
            if (opendld.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var document = new XmlDocument();
                    document.Load(opendld.FileName);
                    job = (BDUJob)XmlSerializerHelper.Deserialize(document.OuterXml, typeof(BDUJob));
                    UseJob(true);
                }
                catch (Exception ex)
                {
                    job = new BDUJob();
                    ShowErrorDialog(ex);
                }
            }
        }

        private void tsbSaveJob_Click(object sender, EventArgs e)
        {
            UpdateJobFromUI(tabControl1.SelectedTab);
            var savedlg = new SaveFileDialog
            {
                Filter = "BDU xml (*.bdu.xml)|*.bdu.xml",
                DefaultExt = ".bdu.xml",
                FileName = job.Info?.Name,
                Title = "Save a BDU job"
            };
            if (!string.IsNullOrEmpty(job.Info?.OriginalPath))
            {
                savedlg.InitialDirectory = Path.GetDirectoryName(job.Info.OriginalPath);
            }
            if (savedlg.ShowDialog() == DialogResult.OK)
            {
                job.Info = new JobInfo(savedlg.FileName, ConnectionDetail);
                XmlSerializerHelper.SerializeToFile(job, savedlg.FileName);
            }
        }

        private void xrmRecordAssign_ColumnValueChanged(object sender, Rappen.XTB.Helpers.Controls.XRMRecordEventArgs e)
        {
            txtAssignEntity.Text = xrmRecordAssign.EntityDisplayName;
        }

        private void cmbBatchSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            panBatchOption.Enabled = (int.TryParse(cmbBatchSize.Text, out int updsize) ? updsize : 1) > 1;
            executeOption_Changed(sender, e);
        }

        private void executeOption_Changed(object sender, EventArgs e)
        {
            UpdateJobFromUI(tabControl1.SelectedTab);
        }

        private void btnUpdateAttributeOptions_Click(object sender, EventArgs e)
        {
            AttributeOptions.Show(updateAttributes, entitymeta);
            RefreshAttributes();
        }
    }
}