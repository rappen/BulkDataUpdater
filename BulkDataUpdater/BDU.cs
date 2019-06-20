namespace Cinteros.XTB.BulkDataUpdater
{
    using AppCode;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Messages;
    using Microsoft.Xrm.Sdk.Metadata;
    using Microsoft.Xrm.Sdk.Query;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;
    using System.Xml;
    using Xrm.Common.Forms;
    using Xrm.XmlEditorUtils;
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

        //private const string aiKey = "cc7cb081-b489-421d-bb61-2ee53495c336";    // jonas@rappen.net tenant, TestAI
        private const string aiKey = "eed73022-2444-45fd-928b-5eebd8fa46a6";    // jonas@rappen.net tenant, XrmToolBox

        private static Dictionary<string, EntityMetadata> entities;
        private static string fetchTemplate = "<fetch><entity name=\"\"/></fetch>";
        private AppInsights ai = new AppInsights(new AiConfig(aiEndpoint, aiKey) { PluginName = "Bulk Data Updater" });
        private Dictionary<string, string> entityAttributes = new Dictionary<string, string>();
        private string fetchXml = fetchTemplate;
        private EntityCollection records;
        private bool showAttributesAll = true;
        private bool showAttributesCustom = true;
        private bool showAttributesCustomizable = true;
        private bool showAttributesManaged = true;
        private bool showAttributesOnlyValidAF = true;
        private bool showAttributesStandard = true;
        private bool showAttributesUncustomizable = true;
        private bool showAttributesUnmanaged = true;
        private string deleteWarningText;

        // Oops, did I name that one??
        private Entity view;

        private bool working = false;

        #endregion Private Fields

        #region Public Constructors

        public BulkDataUpdater()
        {
            InitializeComponent();
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

        private static OptionSetValue GetCurrentStateCodeFromStatusCode(IEnumerable<BulkActionItem> attributes)
        {
            var statusattribute = attributes.Where(a => a.Attribute.Metadata is StatusAttributeMetadata && a.Value is OptionSetValue).FirstOrDefault();
            if (statusattribute != null &&
                statusattribute.Attribute.Metadata is StatusAttributeMetadata statusmeta &&
                statusattribute.Value is OptionSetValue osv)
            {
                foreach (var statusoption in statusmeta.OptionSet.Options)
                {
                    if (statusoption is StatusOptionMetadata && statusoption.Value == osv.Value)
                    {
                        return new OptionSetValue((int)((StatusOptionMetadata)statusoption).State);
                    }
                }
            }
            return null;
        }

        private void EnableControls(bool enabled)
        {
            MethodInvoker mi = delegate
            {
                try
                {
                    gb1select.Enabled = enabled && Service != null;
                    gb2attribute.Enabled = gb1select.Enabled && records != null && records.Entities.Count > 0;
                    pan2value.Enabled = gb2attribute.Enabled && cmbAttribute.SelectedItem is AttributeItem;
                    btnAdd.Enabled = pan2value.Enabled && (!rbSetValue.Checked || cmbValue.DropDownStyle == ComboBoxStyle.Simple || cmbValue.SelectedItem != null);
                    gb3attributes.Enabled = gb2attribute.Enabled && lvAttributes.Items.Count > 0;
                    gb4update.Enabled = pan2value.Enabled && lvAttributes.Items.Count > 0;
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
            if (!string.IsNullOrWhiteSpace(fetch))
            {
                fetchXml = fetch;
                RetrieveRecords(fetchXml, RetrieveRecordsReady);
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
                        if (attribute.LogicalName == "statecode" || attribute.LogicalName == "statuscode")
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
            var fetchwin = new XmlContentDisplayDialog(fetchXml, "Enter FetchXML to retrieve records to update", true, true);
            fetchwin.StartPosition = FormStartPosition.CenterParent;
            if (fetchwin.ShowDialog() == DialogResult.OK)
            {
                FetchUpdated(fetchwin.txtXML.Text);
            }
        }

        private void GetFromFXB()
        {
            var messageBusEventArgs = new MessageBusEventArgs("FetchXML Builder")
            {
                //SourcePlugin = "Bulk Data Updater"
            };
            messageBusEventArgs.TargetArgument = fetchXml;
            OnOutgoingMessage(this, messageBusEventArgs);
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

        private void LoadEntities(Action AfterLoad)
        {
            if (working)
            {
                return;
            }
            entities = null;
            entityShitList = new List<string>();
            working = true;
            WorkAsync(new WorkAsyncInfo("Loading entities metadata...",
                (eventargs) =>
                {
                    EnableControls(false);
                    var req = new RetrieveAllEntitiesRequest()
                    {
                        EntityFilters = EntityFilters.Entity,
                        RetrieveAsIfPublished = true
                    };
                    eventargs.Result = Service.Execute(req);
                })
            {
                PostWorkCallBack = (completedargs) =>
                {
                    working = false;
                    if (completedargs.Error != null)
                    {
                        MessageBox.Show(completedargs.Error.Message);
                    }
                    else
                    {
                        if (completedargs.Result is RetrieveAllEntitiesResponse)
                        {
                            entities = new Dictionary<string, EntityMetadata>();
                            foreach (var entity in ((RetrieveAllEntitiesResponse)completedargs.Result).EntityMetadata)
                            {
                                entities.Add(entity.LogicalName, entity);
                            }
                        }
                    }
                    EnableControls(true);
                    if (AfterLoad != null)
                    {
                        AfterLoad();
                    }
                }
            });
        }

        private void LoadEntityDetails(string entityName, Action detailsLoaded)
        {
            if (working)
            {
                return;
            }
            working = true;
            WorkAsync(new WorkAsyncInfo("Loading " + GetEntityDisplayName(entityName) + " metadata...",
                (eventargs) =>
                {
                    var req = new RetrieveEntityRequest()
                    {
                        LogicalName = entityName,
                        EntityFilters = EntityFilters.Attributes | EntityFilters.Relationships,
                        RetrieveAsIfPublished = true
                    };
                    eventargs.Result = Service.Execute(req);
                })
            {
                PostWorkCallBack = (completedargs) =>
                {
                    working = false;
                    if (completedargs.Error != null)
                    {
                        entityShitList.Add(entityName);
                        MessageBox.Show(completedargs.Error.Message, "Load attribute metadata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        if (completedargs.Result is RetrieveEntityResponse)
                        {
                            var resp = (RetrieveEntityResponse)completedargs.Result;
                            if (entities == null)
                            {
                                entities = new Dictionary<string, EntityMetadata>();
                            }
                            if (entities.ContainsKey(entityName))
                            {
                                entities[entityName] = resp.EntityMetadata;
                            }
                            else
                            {
                                entities.Add(entityName, resp.EntityMetadata);
                            }
                        }
                        detailsLoaded();
                    }
                    working = false;
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

        private void LoadSetting()
        {
            Settings settings;
            if (!SettingsManager.Instance.TryLoad(typeof(BulkDataUpdater), out settings, "Settings"))
            {
                settings = new Settings();
            }

            fetchXml = settings.FetchXML;
            if (settings.EntityAttributes != null)
            {
                entityAttributes = settings.EntityAttributes.ToDictionary(i => i.key, i => i.value);
            }
            else
            {
                entityAttributes = new Dictionary<string, string>();
            }
            tsmiFriendly.Checked = settings.Friendly;
            tsmiAttributesManaged.Checked = settings.AttributesManaged;
            tsmiAttributesUnmanaged.Checked = settings.AttributesUnmanaged;
            tsmiAttributesCustomizable.Checked = settings.AttributesCustomizable;
            tsmiAttributesUncustomizable.Checked = settings.AttributesUncustomizable;
            tsmiAttributesCustom.Checked = settings.AttributesCustom;
            tsmiAttributesStandard.Checked = settings.AttributesStandard;
            tsmiAttributesOnlyValidAF.Checked = settings.AttributesOnlyValidAF;
            cmbUpdDelayCall.SelectedItem = cmbUpdDelayCall.Items.Cast<string>().FirstOrDefault(i => i == settings.DelayCallTime.ToString());
            cmbUpdBatchSize.SelectedItem = cmbUpdBatchSize.Items.Cast<string>().FirstOrDefault(i => i == settings.UpdateBatchSize.ToString());
            cmbDelBatchSize.SelectedItem = cmbDelBatchSize.Items.Cast<string>().FirstOrDefault(i => i == settings.DeleteBatchSize.ToString());
            tsmiFriendly_Click(null, null);
            tsmiAttributes_Click(null, null);
        }

        private bool NeedToLoadEntity(string entityName)
        {
            return
                !string.IsNullOrEmpty(entityName) &&
                !entityShitList.Contains(entityName) &&
                Service != null &&
                (entities == null ||
                 !entities.ContainsKey(entityName) ||
                 entities[entityName].Attributes == null);
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
                if (NeedToLoadEntity(entityName))
                {
                    if (!working)
                    {
                        LoadEntityDetails(entityName, RefreshAttributes);
                    }
                    return;
                }
                var attributes = GetDisplayAttributes(entityName);
                foreach (var attribute in attributes)
                {
                    AttributeItem.AddAttributeToComboBox(cmbAttribute, attribute, true);
                }
                if (entityAttributes.ContainsKey(records.EntityName))
                {
                    var attr = entityAttributes[records.EntityName];
                    var coll = new Dictionary<string, string>();
                    coll.Add("attribute", attr);
                    ControlUtils.FillControl(coll, cmbAttribute);
                }
            }
            crmGridView1.ShowFriendlyNames = useFriendlyNames;
            crmGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            EnableControls(true);
        }

        private void SaveSetting()
        {
            //var entattrstr = "";
            //foreach (var entattr in entityAttributes)
            //{
            //    entattrstr += entattr.Key + ":" + entattr.Value + "|";
            //}
            var settings = new Settings()
            {
                FetchXML = fetchXml,
                EntityAttributes = entityAttributes.Select(p => new KeyValuePair() { key = p.Key, value = p.Value }).ToList(),
                Friendly = tsmiFriendly.Checked,
                AttributesManaged = tsmiAttributesManaged.Checked,
                AttributesUnmanaged = tsmiAttributesUnmanaged.Checked,
                AttributesCustomizable = tsmiAttributesCustomizable.Checked,
                AttributesUncustomizable = tsmiAttributesUncustomizable.Checked,
                AttributesCustom = tsmiAttributesCustom.Checked,
                AttributesStandard = tsmiAttributesStandard.Checked,
                AttributesOnlyValidAF = tsmiAttributesOnlyValidAF.Checked,
                DelayCallTime = int.TryParse(cmbUpdDelayCall.Text, out int upddel) ? upddel : 0,
                UpdateBatchSize = int.TryParse(cmbUpdBatchSize.Text, out int updsize) ? updsize : 1,
                DeleteBatchSize = int.TryParse(cmbDelBatchSize.Text, out int delsize) ? delsize : 1
            };
            SettingsManager.Instance.Save(typeof(BulkDataUpdater), settings, "Settings");
        }

        private void UpdateIncludeCount()
        {
            var count = rbIncludeSelected.Checked ? crmGridView1.SelectedCellRecords?.Entities?.Count : records?.Entities?.Count;
            lblIncludedRecords.Text = $"{count} records";
            lblDeleteHeader.Text = $"Delete {count} {entities?.FirstOrDefault(e => e.Key == records?.EntityName).Value?.DisplayCollectionName?.UserLocalizedLabel?.Label}";
            txtDeleteWarning.Text = deleteWarningText.Replace("[nn]", rbIncludeSelected.Checked ? count.ToString() : "ALL");
        }

        private void UpdateValueField()
        {
            var attribute = (AttributeItem)cmbAttribute.SelectedItem;
            rbSetNull.Enabled = attribute != null && attribute.Metadata.LogicalName != "statecode" && attribute.Metadata.LogicalName != "statuscode";
            cmbValue.Items.Clear();
            if (attribute != null)
            {
                if (attribute.Metadata is EnumAttributeMetadata)
                {
                    var options = ((EnumAttributeMetadata)attribute.Metadata).OptionSet;
                    if (options != null)
                    {
                        foreach (var option in options.Options)
                        {
                            cmbValue.Items.Add(new OptionsetItem(option));
                        }
                    }
                    cmbValue.DropDownStyle = ComboBoxStyle.DropDownList;
                }
                else if (attribute.Metadata is BooleanAttributeMetadata)
                {
                    var options = ((BooleanAttributeMetadata)attribute.Metadata).OptionSet;
                    if (options != null)
                    {
                        cmbValue.Items.Add(new OptionsetItem(options.TrueOption));
                        cmbValue.Items.Add(new OptionsetItem(options.FalseOption));
                    }
                    cmbValue.DropDownStyle = ComboBoxStyle.DropDownList;
                }
                else
                {
                    cmbValue.DropDownStyle = ComboBoxStyle.Simple;
                }
                if (entityAttributes.ContainsKey(records.EntityName))
                {
                    entityAttributes[records.EntityName] = attribute.GetValue();
                }
                else
                {
                    entityAttributes.Add(records.EntityName, attribute.GetValue());
                }
            }
            if (attribute.Metadata.LogicalName == "statecode")
            {
                MessageBox.Show("You selected to update the statecode. In most cases this will probably not work.\n" +
                    "But if you select to update the statuscode instead, the statecode will be fixed automatically!",
                    "Statecode", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            EnableControls(true);
        }

        #endregion Private Methods

        #region Form Event Handlers

        private void btnAdd_Click(object sender, EventArgs e)
        {
            AddAttribute();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            DeleteRecords();
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

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            UpdateRecords();
        }

        private void cmbAttribute_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateValueField();
        }

        private void cmbAttribute_TextChanged(object sender, EventArgs e)
        {
            EnableControls(true);
        }

        private void cmbValue_SelectedIndexChanged(object sender, EventArgs e)
        {
            EnableControls(true);
        }

        private void crmGridView1_SelectionChanged(object sender, EventArgs e)
        {
            UpdateIncludeCount();
        }

        private void DataUpdater_ConnectionUpdated(object sender, XrmToolBox.Extensibility.PluginControlBase.ConnectionUpdatedEventArgs e)
        {
            crmGridView1.DataSource = null;
            entities = null;
            entityShitList.Clear();
            EnableControls(true);
        }

        private void DataUpdater_Load(object sender, EventArgs e)
        {
            EnableControls(false);
            LoadSetting();
            LogUse("Load");
            EnableControls(true);
        }

        private void lvAttributes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvAttributes.SelectedItems.Count == 0)
            {
                return;
            }
            if (lvAttributes.SelectedItems[0].Tag is BulkActionItem attribute)
            {
                cmbAttribute.Text = attribute.Attribute.ToString();
                switch (attribute.Action)
                {
                    case BulkActionAction.SetValue:
                        rbSetValue.Checked = true;
                        if (attribute.Value is OptionSetValue osv)
                        {
                            foreach (var option in cmbValue.Items.Cast<object>().Where(i => i is OptionsetItem).Select(i => i as OptionsetItem))
                            {
                                if (option.meta.Value == osv.Value)
                                {
                                    cmbValue.SelectedItem = option;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            cmbValue.Text = attribute.Value.ToString();
                        }
                        break;
                    case BulkActionAction.Touch:
                        rbSetTouch.Checked = true;
                        cmbValue.Text = string.Empty;
                        break;
                    case BulkActionAction.Null:
                        rbSetNull.Checked = true;
                        cmbValue.Text = string.Empty;
                        break;
                }
                chkOnlyChange.Checked = attribute.DontTouch;
            }
        }

        private void rbInclude_CheckedChanged(object sender, EventArgs e)
        {
            UpdateIncludeCount();
        }

        private void rbSet_CheckedChanged(object sender, EventArgs e)
        {
            cmbValue.Enabled = rbSetValue.Checked;
            chkOnlyChange.Enabled = !rbSetTouch.Checked;
            if (rbSetTouch.Checked)
            {
                chkOnlyChange.Checked = false;
            }
            EnableControls(true);
        }

        private void tsbCancel_Click(object sender, EventArgs e)
        {
            CancelWorker();
        }

        private void tsbCloseThisTab_Click(object sender, EventArgs e)
        {
            CloseTool();
        }

        private void tslAbout_Click(object sender, EventArgs e)
        {
            ShowAboutDialog();
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

        private void tsmiOpenFile_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

        private void tsmiOpenView_Click(object sender, EventArgs e)
        {
            OpenView();
        }

        #endregion Form Event Handlers
    }
}