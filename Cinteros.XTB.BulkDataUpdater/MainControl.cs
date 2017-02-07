namespace Cinteros.XTB.BulkDataUpdater
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using System.Xml;
    using Xrm.XmlEditorUtils;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Messages;
    using Microsoft.Xrm.Sdk.Metadata;
    using Microsoft.Xrm.Sdk.Query;
    using XrmToolBox.Extensibility;
    using XrmToolBox.Extensibility.Interfaces;
    using XrmToolBox.Forms;
    using Microsoft.Crm.Sdk.Messages;
    using System.Configuration;
    using AppCode;
    using Xrm.Common.Forms;
    public partial class BulkDataUpdater : PluginControlBase, IGitHubPlugin, IPayPalPlugin, IMessageBusHost
    {
        const string settingfile = "Cinteros.Xrm.DataUpdater.Settings.xml";
        private static string fetchTemplate = "<fetch><entity name=\"\"/></fetch>";

        private string fetchXml = fetchTemplate;
        private EntityCollection records;
        private bool working = false;
        private static Dictionary<string, EntityMetadata> entities;
        internal List<string> entityShitList = new List<string>(); // Oops, did I name that one??
        internal static Dictionary<string, List<Entity>> views;
        private Entity view;
        internal static bool useFriendlyNames = false;
        private bool showAttributesAll = true;
        private bool showAttributesManaged = true;
        private bool showAttributesUnmanaged = true;
        private bool showAttributesCustomizable = true;
        private bool showAttributesUncustomizable = true;
        private bool showAttributesCustom = true;
        private bool showAttributesStandard = true;
        private bool showAttributesOnlyValidAF = true;
        private Dictionary<string, string> entityAttributes = new Dictionary<string, string>();

        public BulkDataUpdater()
        {
            InitializeComponent();
        }

        #region interface implementation

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            SaveSetting();
        }

        public string RepositoryName
        {
            get { return "DataUpdater"; }
        }

        public string UserName
        {
            get { return "Cinteros"; }
        }

        public string DonationDescription
        {
            get { return "Donation to DataUpdater for XrmToolBox"; }
        }

        public string EmailAccount
        {
            get { return "jonas@rappen.net"; }
        }

        public event EventHandler<XrmToolBox.Extensibility.MessageBusEventArgs> OnOutgoingMessage;

        public void OnIncomingMessage(XrmToolBox.Extensibility.MessageBusEventArgs message)
        {
            if (message.SourcePlugin == "FetchXML Builder" &&
                message.TargetArgument is string)
            {
                FetchUpdated(message.TargetArgument);
            }
        }

        #endregion interface implementation

        #region Event handlers

        private void DataUpdater_Load(object sender, EventArgs e)
        {
            EnableControls(false);
            LoadSetting();
            EnableControls(true);
        }

        private void DataUpdater_ConnectionUpdated(object sender, XrmToolBox.Extensibility.PluginControlBase.ConnectionUpdatedEventArgs e)
        {
            crmGridView1.DataSource = null;
            entities = null;
            entityShitList.Clear();
            EnableControls(true);
        }

        private void tsbCloseThisTab_Click(object sender, EventArgs e)
        {
            CloseTool();
        }

        private void tsmiOpenFile_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

        private void tsmiOpenView_Click(object sender, EventArgs e)
        {
            OpenView();
        }

        private void tsmiFriendly_Click(object sender, EventArgs e)
        {
            useFriendlyNames = tsmiFriendly.Checked;
            RefreshAttributes();
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

        private void btnGetRecords_Click(object sender, EventArgs e)
        {
            GetRecords();
        }

        private void cmbAttribute_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateValueField();
        }

        private void cmbAttribute_TextChanged(object sender, EventArgs e)
        {
            EnableControls(true);
        }

        private void rbSet_CheckedChanged(object sender, EventArgs e)
        {
            cmbValue.Enabled = rbSetValue.Checked;
            chkOnlyChange.Enabled = !rbSetTouch.Checked;
            if (rbSetTouch.Checked)
            {
                chkOnlyChange.Checked = false;
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            UpdateRecords();
        }

        private void tsbAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Bulk Data Updater for XrmToolBox\n" +
                "Version: " + Assembly.GetExecutingAssembly().GetName().Version + "\n\n" +
                "Developed by Jonas Rapp at Innofactor Sweden.",
                "About Bulk Data Updater", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion Event handlers

        #region Methods

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
                AttributesOnlyValidAF = tsmiAttributesOnlyValidAF.Checked
            };
            SettingsManager.Instance.Save(typeof(BulkDataUpdater), settings, "Settings");
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
            tsmiFriendly_Click(null, null);
            tsmiAttributes_Click(null, null);
        }

        private void EnableControls(bool enabled)
        {
            MethodInvoker mi = delegate
            {
                try
                {
                    gb1select.Enabled = enabled && Service != null;
                    gb2attribute.Enabled = gb1select.Enabled && records != null && records.Entities.Count > 0;
                    gb3value.Enabled = gb2attribute.Enabled && cmbAttribute.SelectedItem is AttributeItem;
                    gb4update.Enabled = gb3value.Enabled;
                    var attribute = (AttributeItem)cmbAttribute.SelectedItem;
                    rbSetNull.Enabled = attribute != null && attribute.Metadata.LogicalName != "statecode" && attribute.Metadata.LogicalName != "statuscode";
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

        private void GetRecords()
        {
            switch (cmbSource.SelectedIndex)
            {
                case 0: // Edit
                    GetFromEditor();
                    break;
                case 1: // FXB
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
                case 2: // File
                    FetchUpdated(OpenFile());
                    break;
                case 3: // View
                    OpenView();
                    break;
                default:
                    MessageBox.Show("Select record source.", "Get Records", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    break;
            }
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

        private void FetchUpdated(string fetch)
        {
            if (!string.IsNullOrWhiteSpace(fetch))
            {
                fetchXml = fetch;
                RetrieveRecords(fetchXml, RetrieveRecordsReady);
            }
        }

        private void RetrieveRecordsReady()
        {
            if (records != null)
            {
                lblRecords.Text = records.Entities.Count.ToString() + " records of entity " + records.EntityName;
                crmGridView1.OrganizationService = Service;
                crmGridView1.DataSource = records;
            }
            RefreshAttributes();
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
            EnableControls(true);
        }

        internal static Dictionary<string, EntityMetadata> GetDisplayEntities()
        {
            var result = new Dictionary<string, EntityMetadata>();
            if (entities != null)
            {
                foreach (var entity in entities)
                {
                    //if (!showEntitiesAll)
                    //{
                    //    if (!showEntitiesManaged && entity.Value.IsManaged == true) { continue; }
                    //    if (!showEntitiesUnmanaged && entity.Value.IsManaged == false) { continue; }
                    //    if (!showEntitiesCustomizable && entity.Value.IsCustomizable.Value) { continue; }
                    //    if (!showEntitiesUncustomizable && !entity.Value.IsCustomizable.Value) { continue; }
                    //    if (!showEntitiesStandard && entity.Value.IsCustomEntity == false) { continue; }
                    //    if (!showEntitiesCustom && entity.Value.IsCustomEntity == true) { continue; }
                    //    if (!showEntitiesIntersect && entity.Value.IsIntersect == true) { continue; }
                    //    if (showEntitiesOnlyValidAF && entity.Value.IsValidForAdvancedFind == false) { continue; }
                    //}
                    result.Add(entity.Key, entity.Value);
                }
            }
            return result;
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

        internal static string GetEntityDisplayName(string entityName)
        {
            if (!useFriendlyNames)
            {
                return entityName;
            }
            if (entities != null && entities.ContainsKey(entityName))
            {
                entityName = GetEntityDisplayName(entities[entityName]);
            }
            return entityName;
        }

        internal static string GetEntityDisplayName(EntityMetadata entity)
        {
            var result = entity.LogicalName;
            if (useFriendlyNames)
            {
                if (entity.DisplayName.UserLocalizedLabel != null)
                {
                    result = entity.DisplayName.UserLocalizedLabel.Label;
                }
                if (result == entity.LogicalName && entity.DisplayName.LocalizedLabels.Count > 0)
                {
                    result = entity.DisplayName.LocalizedLabels[0].Label;
                }
            }
            return result;
        }

        internal static string GetAttributeDisplayName(AttributeMetadata attribute)
        {
            string attributeName = attribute.LogicalName;
            if (useFriendlyNames)
            {
                if (attribute.DisplayName.UserLocalizedLabel != null)
                {
                    attributeName = attribute.DisplayName.UserLocalizedLabel.Label;
                }
                if (attributeName == attribute.LogicalName && attribute.DisplayName.LocalizedLabels.Count > 0)
                {
                    attributeName = attribute.DisplayName.LocalizedLabels[0].Label;
                }
                attributeName += " (" + attribute.LogicalName + ")";
            }
            return attributeName;
        }

        private bool ValuesEqual(object value1, object value2)
        {
            if (value1 != null && value2 != null)
            {
                if (value1 is OptionSetValue && value2 is OptionSetValue)
                {
                    return ((OptionSetValue)value1).Value == ((OptionSetValue)value2).Value;
                }
                else
                {
                    return value1.ToString().Equals(value2.ToString());
                }
            }
            else
            {
                return value1 == null && value2 == null;
            }
        }

        private object GetValue(AttributeTypeCode? type)
        {
            switch (type)
            {
                case AttributeTypeCode.String:
                case AttributeTypeCode.Memo:
                    return cmbValue.Text;

                case AttributeTypeCode.BigInt:
                case AttributeTypeCode.Integer:
                    return int.Parse(cmbValue.Text);

                case AttributeTypeCode.Picklist:
                case AttributeTypeCode.State:
                case AttributeTypeCode.Status:
                    var value = ((OptionsetItem)cmbValue.SelectedItem).meta.Value;
                    return new OptionSetValue((int)value);

                case AttributeTypeCode.DateTime:
                    return DateTime.Parse(cmbValue.Text);

                case AttributeTypeCode.Boolean:
                    {
                        // Is checking the cmbAttribute ok? I hope so...
                        var attr = (BooleanAttributeMetadata)((AttributeItem)cmbAttribute.SelectedItem).Metadata;
                        return ((OptionsetItem)cmbValue.SelectedItem).meta == attr.OptionSet.TrueOption;
                    }

                // The following allows to specify an entity reference in the following form:
                // attribute_name,attribute_guid
                // eg: account,08e943f8-1ff0-41ea-89c0-5478dd465806
                // As a security check, the attribute_name MUST be in the targets 
                // specified by the lookup attribute metadata targets
                case AttributeTypeCode.Lookup:
                    {
                        // Get the attribute metadata for the lookup type:
                        var attr = (LookupAttributeMetadata)((AttributeItem)cmbAttribute.SelectedItem).Metadata;

                        // split the text: first part: attribute meta name, second part: attribute guid to update
                        var t = cmbValue.Text.Split(',', ';', '/', '\\', ':').ToArray();
                        // get the first target
                        string attrname = (attr.Targets != null && attr.Targets.Length > 0) ? attr.Targets[0] : null;
                        if (attr.Targets != null && t.Length > 1)
                            attrname = attr.Targets.FirstOrDefault(x => t.First().Equals(x, StringComparison.OrdinalIgnoreCase));

                        if (String.IsNullOrEmpty(attrname))
                            throw new Exception("Target entity: '" + t.First() + "' is null or not found");

                        return new EntityReference(attrname, new Guid(t.Last()));
                    }

                default:
                    throw new Exception("Attribute of type " + type.ToString() + " is currently not supported.");
            }
        }

        private void UpdateValueField()
        {
            cmbValue.Items.Clear();
            var attribute = (AttributeItem)cmbAttribute.SelectedItem;
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

        #endregion Methods

        #region Async SDK methods

        internal void LoadViews(Action viewsLoaded)
        {
            if (working)
            {
                return;
            }
            if (entities == null || entities.Count == 0)
            {
                LoadEntities(viewsLoaded);
                return;
            }
            working = true;
            WorkAsync(new WorkAsyncInfo("Loading views...",
                (bgworker, workargs) =>
                {
                    EnableControls(false);
                    if (views == null || views.Count == 0)
                    {
                        if (Service == null)
                        {
                            throw new Exception("Need a connection to load views.");
                        }
                        var qex = new QueryExpression("savedquery");
                        qex.ColumnSet = new ColumnSet("name", "returnedtypecode", "fetchxml");
                        qex.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
                        qex.Criteria.AddCondition("querytype", ConditionOperator.In, 0, 32);
                        qex.AddOrder("name", OrderType.Ascending);
                        bgworker.ReportProgress(33, "Loading system views...");
                        var sysviews = Service.RetrieveMultiple(qex);
                        foreach (var view in sysviews.Entities)
                        {
                            var entityname = view["returnedtypecode"].ToString();
                            if (!string.IsNullOrWhiteSpace(entityname) && entities.ContainsKey(entityname))
                            {
                                if (views == null)
                                {
                                    views = new Dictionary<string, List<Entity>>();
                                }
                                if (!views.ContainsKey(entityname + "|S"))
                                {
                                    views.Add(entityname + "|S", new List<Entity>());
                                }
                                views[entityname + "|S"].Add(view);
                            }
                        }
                        qex.EntityName = "userquery";
                        bgworker.ReportProgress(66, "Loading user views...");
                        var userviews = Service.RetrieveMultiple(qex);
                        foreach (var view in userviews.Entities)
                        {
                            var entityname = view["returnedtypecode"].ToString();
                            if (!string.IsNullOrWhiteSpace(entityname) && entities.ContainsKey(entityname))
                            {
                                if (views == null)
                                {
                                    views = new Dictionary<string, List<Entity>>();
                                }
                                if (!views.ContainsKey(entityname + "|U"))
                                {
                                    views.Add(entityname + "|U", new List<Entity>());
                                }
                                views[entityname + "|U"].Add(view);
                            }
                        }
                        bgworker.ReportProgress(100, "Finalizing...");
                    }
                })
            {
                PostWorkCallBack = (completedargs) =>
                {
                    working = false;
                    EnableControls(true);
                    if (completedargs.Error != null)
                    {
                        MessageBox.Show(completedargs.Error.Message);
                    }
                    else
                    {
                        viewsLoaded();
                    }
                },
                ProgressChanged = (changeargs) =>
                {
                    SetWorkingMessage(changeargs.UserState.ToString());
                }
            });
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

        private void RetrieveRecords(string fetch, Action AfterRetrieve)
        {
            if (working)
            {
                return;
            }
            lblRecords.Text = "Retrieving records...";
            records = null;
            working = true;
            WorkAsync(new WorkAsyncInfo("Retrieving records...",
                (eventargs) =>
                {
                    eventargs.Result = Service.RetrieveMultiple(new FetchExpression(fetch));
                })
            {
                PostWorkCallBack = (completedargs) =>
                {
                    working = false;
                    if (completedargs.Error != null)
                    {
                        MessageBox.Show(completedargs.Error.Message, "Retrieve Records", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else if (completedargs.Result is EntityCollection)
                    {
                        records = (EntityCollection)completedargs.Result;
                    }
                    AfterRetrieve();
                }
            });
        }

        private void UpdateRecords()
        {
            if (working)
            {
                return;
            }
            if (!(cmbAttribute.SelectedItem is AttributeItem))
            {
                MessageBox.Show("Select an attribute to update from the list.");
                return;
            }
            if (MessageBox.Show("All selected records will unconditionally be updated.\nUI defined rules will not be enforced.\n\nConfirm update!",
                "Confirm", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk) != DialogResult.OK)
            {
                return;
            }
            var attributeitem = (AttributeItem)cmbAttribute.SelectedItem;
            var onlychange = chkOnlyChange.Checked;
            var ignoreerror = chkIgnoreErrors.Checked;
            var entity = records.EntityName;
            var attributes = new List<string>();
            attributes.Add(attributeitem.GetValue());
            if (attributes[0] == "statuscode")
            {
                attributes.Add("statecode");
            }
            var touch = rbSetTouch.Checked;
            object value = null;
            try
            {
                value = rbSetValue.Checked ? GetValue(attributeitem.Metadata.AttributeType) : null;
            }
            catch (Exception e)
            {
                MessageBox.Show("Value error:\n" + e.Message, "Set value", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            OptionSetValue statevalue = null;
            if (attributeitem.Metadata is StatusAttributeMetadata && value is OptionSetValue)
            {
                var statusmeta = (StatusAttributeMetadata)attributeitem.Metadata;
                foreach (var statusoption in statusmeta.OptionSet.Options)
                {
                    if (statusoption is StatusOptionMetadata && statusoption.Value == ((OptionSetValue)value).Value)
                    {
                        statevalue = new OptionSetValue((int)((StatusOptionMetadata)statusoption).State);
                        break;
                    }
                }
            }
            working = true;
            WorkAsync(new WorkAsyncInfo("Updating records",
                (bgworker, workargs) =>
                {
                    var total = records.Entities.Count;
                    var current = 0;
                    var updated = 0;
                    var failed = 0;
                    foreach (var record in records.Entities)
                    {
                        current++;
                        var pct = 100 * current / total;
                        var attributesexists = true;
                        foreach (var attribute in attributes)
                        {
                            if (!record.Contains(attribute))
                            {
                                attributesexists = false;
                                break;
                            }
                        }
                        if ((onlychange || touch) && !attributesexists)
                        {
                            bgworker.ReportProgress(pct, "Reloading record " + current.ToString());
                            var newcols = new ColumnSet(attributes.ToArray());
                            var newrecord = Service.Retrieve(entity, record.Id, newcols);
                            foreach (var attribute in attributes)
                            {
                                if (newrecord.Contains(attribute) && !record.Contains(attribute))
                                {
                                    record.Attributes.Add(attribute, newrecord[attribute]);
                                }
                            }
                        }
                        bgworker.ReportProgress(pct, "Updating record " + current.ToString());
                        try
                        {
                            if (attributeitem.Metadata is StatusAttributeMetadata)
                            {
                                if (UpdateState(touch, onlychange, (OptionSetValue)value, statevalue, record))
                                {
                                    updated++;
                                }
                            }
                            else
                            {
                                // This currently only supports ONE attribute being updated, can be expanded to set/touch several attributes in the future
                                var currentvalue = record.Contains(attributes[0]) ? record[attributes[0]] : null;
                                if (touch)
                                {
                                    value = currentvalue;
                                }
                                if (UpdateAttributes(onlychange, entity, attributes, value, record, currentvalue))
                                {
                                    updated++;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            failed++;
                            if (!ignoreerror)
                            {
                                throw ex;
                            }
                        }
                    }
                    workargs.Result = new Tuple<int, int>(updated, failed);
                })
            {
                PostWorkCallBack = (completedargs) =>
                {
                    working = false;
                    if (completedargs.Error != null)
                    {
                        MessageBox.Show(completedargs.Error.Message, "Update", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        var result = completedargs.Result as Tuple<int, int>;
                        lblUpdateStatus.Text = $"{result.Item1} records updated, {result.Item2} records failed.";
                    }
                },
                ProgressChanged = (changeargs) =>
                {
                    SetWorkingMessage(changeargs.UserState.ToString());
                }
            });
        }

        // This currently only supports ONE attribute being updated, can be expanded to set/touch several attributes in the future
        private bool UpdateAttributes(bool onlychange, string entity, List<string> attributes, object value, Entity record, object currentvalue)
        {
            if (!onlychange || !ValuesEqual(value, currentvalue))
            {
                var updaterecord = new Entity(entity);
                updaterecord.Id = record.Id;
                foreach (var attribute in attributes)
                {
                    updaterecord.Attributes.Add(attribute, value);
                }
                Service.Update(updaterecord);
                if (record.Contains(attributes[0]))
                {
                    record[attributes[0]] = value;
                }
                else
                {
                    record.Attributes.Add(attributes[0], value);
                }
                return true;
            }
            return false;
        }

        private bool UpdateState(bool touch, bool onlychange, OptionSetValue statusvalue, OptionSetValue statevalue, Entity record)
        {
            var currentstate = record.Contains("statecode") ? record["statecode"] : new OptionSetValue(-1);
            var currentstatus = record.Contains("statuscode") ? record["statuscode"] : new OptionSetValue(-1);
            if (touch)
            {
                statevalue = (OptionSetValue)currentstate;
                statusvalue = (OptionSetValue)currentstatus;
            }
            if (!onlychange || !ValuesEqual(currentstate, statevalue) || !ValuesEqual(currentstatus, statusvalue))
            {
                var req = new SetStateRequest()
                {
                    EntityMoniker = record.ToEntityReference(),
                    State = statevalue,
                    Status = statusvalue
                };
                var resp = Service.Execute(req);
                if (record.Contains("statecode"))
                {
                    record["statecode"] = statevalue;
                }
                else
                {
                    record.Attributes.Add("statecode", statevalue);
                }
                if (record.Contains("statuscode"))
                {
                    record["statuscode"] = statusvalue;
                }
                else
                {
                    record.Attributes.Add("statuscode", statusvalue);
                }
                return true;
            }
            return false;
        }

        #endregion Async SDK methods
    }
}
