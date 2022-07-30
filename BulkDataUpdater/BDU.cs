namespace Cinteros.XTB.BulkDataUpdater
{
    using AppCode;
    using McTools.Xrm.Connection;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Messages;
    using Microsoft.Xrm.Sdk.Metadata;
    using Microsoft.Xrm.Sdk.Query;
    using Rappen.XRM.Helpers;
    using Rappen.XRM.Helpers.Extensions;
    using Rappen.XTB.Helpers.ControlItems;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Reflection;
    using System.Windows.Forms;
    using System.Xml;
    using Xrm.Common.Forms;
    using XrmToolBox;
    using XrmToolBox.Extensibility;

    public partial class BulkDataUpdater : PluginControlBase
    {
        #region Internal Fields

        internal static bool useFriendlyNames = false;
        internal static Dictionary<string, List<Entity>> views;
        internal List<string> entityShitList = new List<string>();

        #endregion Internal Fields

        #region Private Fields

        private const string aiEndpoint = "https://dc.services.visualstudio.com/v2/track";
        private const string aiKey = "eed73022-2444-45fd-928b-5eebd8fa46a6";    // jonas@rappen.net tenant, XrmToolBox
        private AppInsights ai = new AppInsights(aiEndpoint, aiKey, Assembly.GetExecutingAssembly(), "Bulk Data Updater");

        private static Dictionary<string, EntityMetadata> entities;
        private static string fetchTemplate = "<fetch><entity name=\"\"/></fetch>";
        private BDUJob job;

        private string deleteWarningText;

        private int fetchResulCount = -1;

        private EntityCollection records;
        private bool showAttributesAll = true;
        private bool showAttributesCustom = true;
        private bool showAttributesCustomizable = true;
        private bool showAttributesManaged = true;
        private bool showAttributesOnlyValidAF = true;
        private bool showAttributesStandard = true;
        private bool showAttributesUncustomizable = true;
        private bool showAttributesUnmanaged = true;
        private Entity view;
        private Version currentversion;
        private readonly Version bypasspluginminversion = new Version(9, 2);

        private bool working = false;

        #endregion Private Fields

        #region Public Constructors

        public BulkDataUpdater()
        {
            InitializeComponent();
            var prop = MetadataExtensions.entityProperties.ToList();
            prop.Add("Attributes");
            MetadataExtensions.entityProperties = prop.ToArray();
            deleteWarningText = txtDeleteWarning.Text;
        }

        #endregion Public Constructors

        #region Public Methods

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            SaveSetting();
            LogUse("Close");
        }

        #endregion Public Methods

        #region Private Methods

        private void EnableControls(bool enabled)
        {
            MethodInvoker mi = delegate
            {
                try
                {
                    Enabled = enabled;
                    gb1select.Enabled = enabled && Service != null;
                    gb2attribute.Enabled = gb1select.Enabled && records != null && records.Entities.Count > 0;
                    panUpdButton.Enabled = gb2attribute.Enabled && cmbAttribute.SelectedItem is AttributeMetadataItem;
                    btnAdd.Enabled = panUpdButton.Enabled && IsValueValid();
                    gb3attributes.Enabled = gb2attribute.Enabled && lvAttributes.Items.Count > 0;
                    gbExecute.Enabled =
                        (tabControl1.SelectedTab == tabUpdate && panUpdButton.Enabled && lvAttributes.Items.Count > 0) ||
                        (tabControl1.SelectedTab == tabAssign && xrmRecordAssign.Record != null) ||
                        (tabControl1.SelectedTab == tabSetState && cbSetStatus.SelectedItem != null && cbSetStatusReason.SelectedItem != null) ||
                        (tabControl1.SelectedTab == tabDelete);
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

        private void FetchUpdated(string fetch)
        {
            job = new BDUJob
            {
                FetchXML = fetch
            };
            if (!string.IsNullOrWhiteSpace(fetch))
            {
                RetrieveRecords(job.FetchXML);
            }
        }

        private AttributeMetadata[] GetDisplayAttributes(string entityName)
        {
            var result = new List<AttributeMetadata>();
            AttributeMetadata[] attributes = null;
            if (entities != null && entities.ContainsKey(entityName))
            {
                attributes = entities[entityName].Attributes;
                if (attributes != null)
                {
                    foreach (var attribute in attributes)
                    {
                        if (!attribute.IsValidForUpdate.Value == true)
                        {
                            continue;
                        }
                        if (attribute.LogicalName == "statecode" || attribute.LogicalName == "statuscode" || attribute.LogicalName == "ownerid")
                        {
                            continue;
                        }
                        if (!showAttributesAll)
                        {
                            if (!string.IsNullOrEmpty(attribute.AttributeOf)) { continue; }
                            if (!showAttributesManaged && attribute.IsManaged == true) { continue; }
                            if (!showAttributesUnmanaged && attribute.IsManaged == false) { continue; }
                            if (!showAttributesCustomizable && attribute.IsCustomizable.Value) { continue; }
                            if (!showAttributesUncustomizable && !attribute.IsCustomizable.Value) { continue; }
                            if (!showAttributesStandard && attribute.IsCustomAttribute == false) { continue; }
                            if (!showAttributesCustom && attribute.IsCustomAttribute == true) { continue; }
                            if (showAttributesOnlyValidAF && attribute.IsValidForAdvancedFind.Value == false) { continue; }
                        }
                        result.Add(attribute);
                    }
                }
            }
            return result.ToArray();
        }

        private void GetFromEditor()
        {
            var fetchwin = new XmlContentDisplayDialog(job?.FetchXML ?? fetchTemplate, "Enter FetchXML to retrieve records to update", true, true);
            fetchwin.StartPosition = FormStartPosition.CenterParent;
            if (fetchwin.ShowDialog() == DialogResult.OK)
            {
                FetchUpdated(fetchwin.txtXML.Text);
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
                        MessageBox.Show("FetchXML Builder is not installed.\nDownload latest version from the Plugins Store.", "FetchXML Builder",
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    catch (PluginNotFoundException)
                    {
                        MessageBox.Show("FetchXML Builder was not found.\nInstall it from the XrmToolBox Plugin Store.", "FetchXML Builder",
                                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    break;

                case "File": // File
                    FetchUpdated(OpenFile());
                    break;

                case "View": // View
                    OpenView();
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
                cmbDelayCall.SelectedItem = cmbDelayCall.Items.Cast<string>().FirstOrDefault(i => i == job.Update.ExecuteOptions.DelayCallTime.ToString());
                cmbBatchSize.SelectedItem = cmbBatchSize.Items.Cast<string>().FirstOrDefault(i => i == job.Update.ExecuteOptions.BatchSize.ToString());
                chkIgnoreErrors.Checked = job.Update.ExecuteOptions.IgnoreErrors;
                chkBypassPlugins.Checked = job.Update.ExecuteOptions.BypassCustom;
                lvAttributes.Items.Clear();
                job.Update.Attributes.ForEach(a => AddBAI(a));
                if (lvAttributes.Items.Count > 0)
                {
                    lvAttributes.Items[0].Selected = true;
                }
            }
            else if (tabControl1.SelectedTab == tabAssign)
            {
                btnExecute.Text = "Assign records";
                cmbDelayCall.SelectedItem = cmbDelayCall.Items.Cast<string>().FirstOrDefault(i => i == "0");
                cmbBatchSize.SelectedItem = cmbBatchSize.Items.Cast<string>().FirstOrDefault(i => i == job.Assign.ExecuteOptions.BatchSize.ToString());
                chkIgnoreErrors.Checked = job.Assign.ExecuteOptions.IgnoreErrors;
                chkBypassPlugins.Checked = job.Assign.ExecuteOptions.BypassCustom;
            }
            else if (tabControl1.SelectedTab == tabSetState)
            {
                btnExecute.Text = "Update records";
                cmbDelayCall.SelectedItem = cmbDelayCall.Items.Cast<string>().FirstOrDefault(i => i == "0");
                cmbBatchSize.SelectedItem = cmbBatchSize.Items.Cast<string>().FirstOrDefault(i => i == job.SetState.ExecuteOptions.BatchSize.ToString());
                chkIgnoreErrors.Checked = job.SetState.ExecuteOptions.IgnoreErrors;
                chkBypassPlugins.Checked = job.SetState.ExecuteOptions.BypassCustom;
                LoadStates(entities?.FirstOrDefault(ent => ent.Key == records?.EntityName).Value);
            }
            else if (tabControl1.SelectedTab == tabDelete)
            {
                btnExecute.Text = "Delete records";
                cmbDelayCall.SelectedItem = cmbDelayCall.Items.Cast<string>().FirstOrDefault(i => i == "0");
                cmbBatchSize.SelectedItem = cmbBatchSize.Items.Cast<string>().FirstOrDefault(i => i == job.Delete.ExecuteOptions.BatchSize.ToString());
                chkIgnoreErrors.Checked = job.Delete.ExecuteOptions.IgnoreErrors;
                chkBypassPlugins.Checked = job.Delete.ExecuteOptions.BypassCustom;
            }
            panWaitBetween.Visible = tabControl1.SelectedTab == tabUpdate;
            working = tempworker;
            EnableControls(true);
        }

        private void LoadEntities(Action AfterLoad)
        {
            if (working)
            {
                return;
            }
            EnableControls(false);
            entities = null;
            entityShitList = new List<string>();
            working = true;
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading entities metadata...",
                Work = (worker, eventargs) =>
                {
                    eventargs.Result = Service.LoadEntities(ConnectionDetail.OrganizationMajorVersion, ConnectionDetail.OrganizationMinorVersion).EntityMetadata;
                },
                PostWorkCallBack = (completedargs) =>
                {
                    working = false;
                    if (completedargs.Error != null)
                    {
                        ShowErrorDialog(completedargs.Error, "Load Entities");
                    }
                    else if (completedargs.Result is RetrieveAllEntitiesResponse)
                    {
                        entities = new Dictionary<string, EntityMetadata>();
                        foreach (var entity in ((RetrieveAllEntitiesResponse)completedargs.Result).EntityMetadata)
                        {
                            entities.Add(entity.LogicalName, entity);
                        }
                    }
                    else if (completedargs.Result is EntityMetadataCollection metas)
                    {
                        entities = metas.ToDictionary(e => e.LogicalName);
                    }
                    EnableControls(true);
                    if (AfterLoad != null)
                    {
                        AfterLoad();
                    }
                }
            });
        }

        private void LoadMissingAttributesForRecord(Entity record, string entity, IEnumerable<BulkActionItem> attributes)
        {
            var newcols = new ColumnSet(attributes.Select(a => a.Attribute.Metadata.LogicalName).ToArray());
            if (newcols.Columns.Contains("statuscode"))
            {
                newcols.AddColumn("statecode");
            }
            var newrecord = Service.Retrieve(entity, record.Id, newcols);
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
            fetchResulCount = settings.FetchResultCount;
            tsmiFriendly.Checked = settings.Friendly;
            tsmiAttributesManaged.Checked = settings.AttributesManaged;
            tsmiAttributesUnmanaged.Checked = settings.AttributesUnmanaged;
            tsmiAttributesCustomizable.Checked = settings.AttributesCustomizable;
            tsmiAttributesUncustomizable.Checked = settings.AttributesUncustomizable;
            tsmiAttributesCustom.Checked = settings.AttributesCustom;
            tsmiAttributesStandard.Checked = settings.AttributesStandard;
            tsmiAttributesOnlyValidAF.Checked = settings.AttributesOnlyValidAF;
            tsmiFriendly_Click(null, null);
            tsmiAttributes_Click(null, null);
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
            if (views == null || views.Count == 0)
            {
                LoadViews(OpenView);
                return;
            }
            var viewselector = new SelectViewDialog(this);
            viewselector.StartPosition = FormStartPosition.CenterParent;
            if (viewselector.ShowDialog() == DialogResult.OK)
            {
                view = viewselector.View;
                var fetchDoc = new XmlDocument();
                if (view.Contains("fetchxml"))
                {
                    fetchDoc.LoadXml(view["fetchxml"].ToString());
                    FetchUpdated(fetchDoc.OuterXml);
                }
            }
            EnableControls(true);
        }

        private void RefreshAttributes()
        {
            EnableControls(false);
            cmbAttribute.Items.Clear();
            if (records != null)
            {
                var entityName = records.EntityName;
                var attributes = GetDisplayAttributes(entityName);
                foreach (var attribute in attributes)
                {
                    AttributeMetadataItem.AddAttributeToComboBox(cmbAttribute, attribute, true, useFriendlyNames);
                }
            }
            crmGridView1.ShowFriendlyNames = useFriendlyNames;
            crmGridView1.ShowLocalTimes = useFriendlyNames;
            crmGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            EnableControls(true);
        }

        private void SaveSetting()
        {
            var settings = new Settings()
            {
                Job = job,
                FetchResultCount = fetchResulCount,
                Friendly = tsmiFriendly.Checked,
                AttributesManaged = tsmiAttributesManaged.Checked,
                AttributesUnmanaged = tsmiAttributesUnmanaged.Checked,
                AttributesCustomizable = tsmiAttributesCustomizable.Checked,
                AttributesUncustomizable = tsmiAttributesUncustomizable.Checked,
                AttributesCustom = tsmiAttributesCustom.Checked,
                AttributesStandard = tsmiAttributesStandard.Checked,
                AttributesOnlyValidAF = tsmiAttributesOnlyValidAF.Checked,
            };
            SettingsManager.Instance.Save(typeof(BulkDataUpdater), settings, ConnectionDetail?.ConnectionName);
        }

        private void UpdateIncludeCount()
        {
            var count = GetIncludedRecords()?.Count();
            var entity = entities?.FirstOrDefault(e => e.Key == records?.EntityName).Value?.DisplayCollectionName?.UserLocalizedLabel?.Label;
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
                if (attribute.Metadata is PicklistAttributeMetadata pickmeta)
                {
                    var options = pickmeta.OptionSet;
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
            LoadStatuses();
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
            crmGridView1.DataSource = null;
            entities = null;
            entityShitList.Clear();
            job = null;
            EnableControls(true);
        }

        private void DataUpdater_Load(object sender, EventArgs e)
        {
            EnableControls(false);
            LoadGlobalSetting();
            LoadSetting();
            LogUse("Load");
            LoadEntities(AfterEntitiesLoaded);
            EnableControls(true);
        }

        private void AfterEntitiesLoaded()
        {
            FixLoadedBAI(job.Update);
            if (!string.IsNullOrWhiteSpace(job?.FetchXML) && fetchResulCount > 0 && fetchResulCount < 100)
            {
                RetrieveRecords(job.FetchXML);
            }
        }

        private void lvAttributes_SelectedIndexChanged(object sender, EventArgs e)
        {
            PopulateFromAddedAttribute();
        }

        private void rbInclude_CheckedChanged(object sender, EventArgs e)
        {
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
            tsbCancel.Enabled = false;
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

        private void tsmiAttributes_Click(object sender, EventArgs e)
        {
            if (sender != tsmiAttributesAll)
            {
                tsmiAttributesAll.Checked =
                    tsmiAttributesManaged.Checked &&
                    tsmiAttributesUnmanaged.Checked &&
                    tsmiAttributesCustomizable.Checked &&
                    tsmiAttributesUncustomizable.Checked &&
                    tsmiAttributesCustom.Checked &&
                    tsmiAttributesStandard.Checked &&
                    !tsmiAttributesOnlyValidAF.Checked;
            }
            if (!tsmiAttributesManaged.Checked && !tsmiAttributesUnmanaged.Checked)
            {   // Neither managed nor unmanaged is not such a good idea...
                tsmiAttributesUnmanaged.Checked = true;
            }
            if (!tsmiAttributesCustomizable.Checked && !tsmiAttributesUncustomizable.Checked)
            {   // Neither customizable nor uncustomizable is not such a good idea...
                tsmiAttributesCustomizable.Checked = true;
            }
            if (!tsmiAttributesCustom.Checked && !tsmiAttributesStandard.Checked)
            {   // Neither custom nor standard is not such a good idea...
                tsmiAttributesStandard.Checked = true;
            }
            tsmiAttributesManaged.Enabled = !tsmiAttributesAll.Checked;
            tsmiAttributesUnmanaged.Enabled = !tsmiAttributesAll.Checked;
            tsmiAttributesCustomizable.Enabled = !tsmiAttributesAll.Checked;
            tsmiAttributesUncustomizable.Enabled = !tsmiAttributesAll.Checked;
            tsmiAttributesCustom.Enabled = !tsmiAttributesAll.Checked;
            tsmiAttributesStandard.Enabled = !tsmiAttributesAll.Checked;
            tsmiAttributesOnlyValidAF.Enabled = !tsmiAttributesAll.Checked;
            showAttributesAll = tsmiAttributesAll.Checked;
            showAttributesManaged = tsmiAttributesManaged.Checked;
            showAttributesUnmanaged = tsmiAttributesUnmanaged.Checked;
            showAttributesCustomizable = tsmiAttributesCustomizable.Checked;
            showAttributesUncustomizable = tsmiAttributesUncustomizable.Checked;
            showAttributesCustom = tsmiAttributesCustom.Checked;
            showAttributesStandard = tsmiAttributesStandard.Checked;
            showAttributesOnlyValidAF = tsmiAttributesOnlyValidAF.Checked;
            RefreshAttributes();
        }

        private void tsmiFriendly_Click(object sender, EventArgs e)
        {
            useFriendlyNames = tsmiFriendly.Checked;
            RefreshAttributes();
        }

        #endregion Form Event Handlers

        private void crmGridView1_RecordDoubleClick(object sender, Rappen.XTB.Helpers.Controls.XRMRecordEventArgs e)
        {
            if (e.Entity != null)
            {
                string url = GetEntityUrl(e.Entity);
                if (!string.IsNullOrEmpty(url))
                {
                    Process.Start(url);
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
                    xrmRecordAttribute.Service = Service;
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

        private void btnCalcPreview_Click(object sender, EventArgs e)
        {
            var record = crmGridView1.SelectedCellRecords.FirstOrDefault();
            if (record == null)
            {
                MessageBox.Show("Please select a record to the left to see preview of calculation.", "Calculation Preview", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            var preview = record.Substitute(Service, txtValueCalc.Text);
            MessageBox.Show($"Preview of calculation:\n{preview}", "Calculation Preview", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

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
                crmGridView1.SelectedCellRecords.FirstOrDefault() is Entity record)
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
            Process.Start("https://docs.microsoft.com/en-us/power-apps/developer/data-platform/bypass-custom-business-logic?WT.mc_id=BA-MVP-5002475");
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
        }

        private void tsmiJobsSave_Click(object sender, EventArgs e)
        {
            if (job == null)
            {
                MessageBox.Show("Nothing to save.");
                return;
            }
            var path = @"C:\Dev\GitHub\BulkDataUpdater\BulkDataUpdater\bin\Debug\Settings\bdujob.xml";
            UpdateJob(job);
            XmlSerializerHelper.SerializeToFile(job, path);
        }

        private void tsmiJobsOpen_Click(object sender, EventArgs e)
        {
            var path = @"C:\Dev\GitHub\BulkDataUpdater\BulkDataUpdater\bin\Debug\Settings\bdujob.xml";
            try
            {
                var document = new XmlDocument();
                document.Load(path);
                job = (BDUJob)XmlSerializerHelper.Deserialize(document.OuterXml, typeof(BDUJob));
                UseJob(job);
            }
            catch (Exception ex)
            {
                job = null;
                ShowErrorDialog(ex, $"Loading and deserializing file \"{path}\"");
            }
        }

        private void UpdateJob(BDUJob bdujob)
        {
            bdujob.IncludeAll = rbIncludeAll.Checked;
            UpdateJobUpdate(bdujob.Update);
            UpdateJobAssign(bdujob.Assign);
            UpdateJobSetState(bdujob.SetState);
            UpdateJobDelete(bdujob.Delete);
        }

        private void UseJob(BDUJob bdujob)
        {
            RetrieveRecords(bdujob.FetchXML);
        }
    }
}