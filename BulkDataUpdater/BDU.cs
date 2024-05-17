namespace Cinteros.XTB.BulkDataUpdater
{
    using AppCode;
    using Cinteros.XTB.BulkDataUpdater.Forms;
    using McTools.Xrm.Connection;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Metadata;
    using Microsoft.Xrm.Sdk.Query;
    using Rappen.XRM.Helpers.Extensions;
    using Rappen.XTB.Helpers;
    using Rappen.XTB.Helpers.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows.Forms;
    using System.Xml;
    using Xrm.Common.Forms;
    using XrmToolBox.Extensibility;

    public partial class BulkDataUpdater : PluginControlBase
    {
        #region Internal Fields

        internal bool useFriendlyNames = false;
        internal List<EntityMetadata> entities;
        internal EntityMetadata entitymeta;
        internal Version currentversion;
        internal readonly Version bypasspluginminversion = new Version(9, 2);
        internal BDUJob job;
        internal UpdateAttributes updateAttributes;
        internal List<string> isOnForms;
        internal List<string> isOnViews;
        internal bool working = false;

        internal Dictionary<string, List<Entity>> views;

        #endregion Internal Fields

        #region Private Fields

        private const string aiEndpoint = "https://dc.services.visualstudio.com/v2/track";
        private const string aiKey1 = "eed73022-2444-45fd-928b-5eebd8fa46a6";    // jonas@rappen.net tenant, XrmToolBox
        private const string aiKey2 = "d46e9c12-ee8b-4b28-9643-dae62ae7d3d4";    // jonas@jonasr.app, XrmToolBoxTools
        private AppInsights ai1 = new AppInsights(aiEndpoint, aiKey1, Assembly.GetExecutingAssembly(), "Bulk Data Updater");
        private AppInsights ai2 = new AppInsights(aiEndpoint, aiKey2, Assembly.GetExecutingAssembly(), "Bulk Data Updater");
        private static string fetchTemplate = "<fetch><entity name=\"\"/></fetch>";
        private string deleteWarningText;
        private int fetchResulCount = -1;
        private EntityCollection records;
        private Entity view;
        private string currentconnection;

        #endregion Private Fields

        #region Public Constructors

        public BulkDataUpdater()
        {
            UrlUtils.TOOL_NAME = "BulkDataUpdater";
            UrlUtils.MVP_ID = "DX-MVP-5002475";
            InitializeComponent();
            tslAbout.ToolTipText = $"Version: {Assembly.GetExecutingAssembly().GetName().Version}";
            var prop = Rappen.XRM.Helpers.Extensions.MetadataExtensions.entityProperties.ToList();
            prop.Add("Attributes");
            Rappen.XRM.Helpers.Extensions.MetadataExtensions.entityProperties = prop.ToArray();
            deleteWarningText = txtDeleteWarning.Text;
        }

        #endregion Public Constructors

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

        #region Public Methods

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            SaveSetting();
            LogUse("Close", ai2: true);
        }

        #endregion Public Methods

        #region Internal Methods

        internal bool IsOnAnyForm(string entity, string attribute, Action action)
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
                            action?.Invoke();
                        }
                        Enabled = true;
                        working = false;
                    }
                });
                return false;
            }
            return isOnForms.Contains(attribute);
        }

        internal bool IsOnAnyView(string entity, string attribute, Action action)
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
                            action?.Invoke();
                        }
                        Enabled = true;
                        working = false;
                    }
                });
                return false;
            }
            return isOnViews.Contains(attribute);
        }

        #endregion Internal Methods

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
                    tsbFetch.Enabled = !cancel && enabled;
                    tsbOpenJob.Enabled = !cancel && enabled;
                    tsbSaveJob.Enabled = !cancel && enabled;
                    tsbFriendly.Enabled = !cancel && enabled;
                    tsbRaw.Enabled = !cancel && enabled;
                    tsbCancel.Enabled = cancel;
                    panRecordSummary.Enabled = !cancel && tsbFetch.Enabled && !string.IsNullOrWhiteSpace(job?.FetchXML);
                    gb3attributes.Enabled = !cancel && crmGridView1.RowCount > 0;
                    btnAttrEdit.Enabled = !cancel && lvAttributes.SelectedItems.Count == 1;
                    btnAttrRemove.Enabled = !cancel && lvAttributes.SelectedItems.Count > 0;
                    gbExecute.Enabled = !cancel && (
                        (tabControl1.SelectedTab == tabUpdate && lvAttributes.Items.Count > 0) ||
                        (tabControl1.SelectedTab == tabAssign && xrmRecordAssign.Record != null) ||
                        (tabControl1.SelectedTab == tabSetState && cbSetStatus.SelectedItem != null && cbSetStatusReason.SelectedItem != null) ||
                        (tabControl1.SelectedTab == tabDelete));
                    SetImpSeqNo(forcekeepnum: true);
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

        private void InitializeTab()
        {
            var tempworker = working;
            working = true;
            if (tabControl1.SelectedTab == tabUpdate)
            {
                SetUpdateFromJob(job.Update);
            }
            else if (tabControl1.SelectedTab == tabAssign)
            {
                SetAssignFromJob(job.Assign);
            }
            else if (tabControl1.SelectedTab == tabSetState)
            {
                SetSetStateFromJob(job.SetState);
            }
            else if (tabControl1.SelectedTab == tabDelete)
            {
            }
            btnExecute.Text = tabControl1.SelectedTab.Name.Replace("tab", "");
            working = tempworker;
            EnableControls(true);
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
                try
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
                    }
                }
                finally
                {
                    EnableControls(true);
                }
            }
            return result;
        }

        private void OpenView()
        {
            if (views == null || views.Count == 0)
            {
                LoadViews(OpenView);
                return;
            }
            var viewselector = new SelectViewDialog(this);
            viewselector.StartPosition = FormStartPosition.CenterParent;
            if (viewselector.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    EnableControls(false);
                    view = viewselector.View;
                    var fetchDoc = new XmlDocument();
                    if (view.Contains("fetchxml"))
                    {
                        fetchDoc.LoadXml(view["fetchxml"].ToString());
                        view.TryGetAttributeValue("layoutxml", out string layout);
                        FetchUpdated(fetchDoc.OuterXml, layout);
                    }
                }
                finally
                {
                    EnableControls(true);
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
            UpdateIncludeCount();
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
            try
            {
                SettingsManager.Instance.Save(typeof(BulkDataUpdater), settings, ConnectionDetail?.ConnectionName);
            }
            catch (Exception ex)
            {
                ShowErrorDialog(ex, "Saving settings");
            }
        }

        private void UpdateIncludeCount()
        {
            if (crmGridView1.Refreshing)
            {
                return;
            }
            var count = GetIncludedRecords()?.Count();
            var entity = useFriendlyNames ? entitymeta?.DisplayCollectionName?.UserLocalizedLabel?.Label : entitymeta?.LogicalName;
            lblSelectedRecords.Text = $"Selected {count} / {crmGridView1.Rows.Count} {entity}";
            txtDeleteWarning.Text = deleteWarningText.Replace("[nn]", crmGridView1.Rows.Count > count ? count.ToString() : "ALL");
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

        private JobExecute GetJobAction()
        {
            JobExecute jobaction = null;
            if (tabControl1.SelectedTab == tabUpdate) jobaction = job.Update;
            else if (tabControl1.SelectedTab == tabAssign) jobaction = job.Assign;
            else if (tabControl1.SelectedTab == tabSetState) jobaction = job.SetState;
            else if (tabControl1.SelectedTab == tabDelete) jobaction = job.Delete;
            return jobaction;
        }

        private void ExecuteAction(JobExecute jobaction)
        {
            if (jobaction is JobUpdate) UpdateRecords(jobaction.ExecuteOptions);
            else if (jobaction is JobAssign) AssignRecords(jobaction.ExecuteOptions);
            else if (jobaction is JobSetState) SetStateRecords(jobaction.ExecuteOptions);
            else if (jobaction is JobDelete) DeleteRecords(jobaction.ExecuteOptions);
        }

        internal void LoadViews(Action viewsLoaded)
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading views...",
                Work = (worker, eventargs) =>
                {
                    EnableControls(false);
                    if (views == null || views.Count == 0)
                    {
                        if (Service == null)
                        {
                            throw new Exception("Need a connection to load views.");
                        }
                        var qexs = new QueryExpression("savedquery");
                        qexs.ColumnSet = new ColumnSet("name", "returnedtypecode", "fetchxml", "layoutxml", "iscustomizable");
                        qexs.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
                        qexs.Criteria.AddCondition("fetchxml", ConditionOperator.NotNull);
                        qexs.AddOrder("name", OrderType.Ascending);
                        var sysviews = Service.RetrieveMultipleAll(qexs, worker, eventargs, null, false);
                        foreach (var view in sysviews.Entities)
                        {
                            var entityname = view["returnedtypecode"].ToString();
                            if (!string.IsNullOrWhiteSpace(entityname) && GetEntity(entityname) != null)
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
                        var qexu = new QueryExpression("userquery");
                        qexu.ColumnSet = new ColumnSet("name", "returnedtypecode", "fetchxml", "layoutxml");
                        qexu.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
                        qexu.AddOrder("name", OrderType.Ascending);
                        var userviews = Service.RetrieveMultipleAll(qexu, worker, eventargs, null, false);
                        foreach (var view in userviews.Entities)
                        {
                            var entityname = view["returnedtypecode"].ToString();
                            if (!string.IsNullOrWhiteSpace(entityname) && GetEntity(entityname) != null)
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
                    }
                },
                PostWorkCallBack = (completedargs) =>
                {
                    EnableControls(true);
                    if (completedargs.Error != null)
                    {
                        ShowErrorDialog(completedargs.Error, "Loading Views");
                    }
                    else
                    {
                        viewsLoaded();
                    }
                }
            });
        }

        internal EntityMetadata GetEntity(string entityname)
        {
            return entities?.FirstOrDefault(e => e.LogicalName.Equals(entityname));
        }

        #endregion Private Methods

        #region Form Event Handlers

        private void btnExecute_Click(object sender, EventArgs e)
        {
            var jobaction = GetJobAction();
            if (Execute.Show(this, jobaction) == DialogResult.OK)
            {
                ExecuteAction(jobaction);
            }
        }

        private void btnGetRecords_Click(object sender, EventArgs e)
        {
            if (sender is Control ctrl)
            {
                GetRecords(ctrl.Tag?.ToString());
            }
            else if (sender is ToolStripItem tsi)
            {
                GetRecords(tsi.Tag?.ToString());
            }
        }

        private void btnAttrAdd_Click(object sender, EventArgs e)
        {
            AddAttribute(UpdateAttribute.Show(this, useFriendlyNames, updateAttributes, crmGridView1.SelectedRowRecords?.FirstOrDefault(), null), false);
        }

        private void btnAttrEdit_Click(object sender, EventArgs e)
        {
            if (lvAttributes.SelectedItems.Count == 1)
            {
                var attr = (BulkActionItem)lvAttributes.SelectedItems[0].Tag;
                AddAttribute(UpdateAttribute.Show(this, useFriendlyNames, updateAttributes, crmGridView1.SelectedRowRecords?.FirstOrDefault(), attr), true);
            }
            else if (lvAttributes.SelectedItems.Count > 1)
            {
                MessageBox.Show("Only one attribute can be edited at a time.", "Edit Attribute", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else
            {
                MessageBox.Show("Select an attribute to edit.", "Edit Attribute", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void btnAttrRemove_Click(object sender, EventArgs e)
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

        private void crmGridView1_SelectionChanged(object sender, EventArgs e)
        {
            UpdateIncludeCount();
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

        private void tsbFriendly_Click(object sender, EventArgs e)
        {
            if (sender != null)
            {
                tsbFriendly.Checked = sender == tsbFriendly;
            }
            tsbRaw.Checked = !tsbFriendly.Checked;
            useFriendlyNames = tsbFriendly.Checked;
            RefreshGridRecords();
        }

        private void crmGridView1_RecordDoubleClick(object sender, Rappen.XTB.Helpers.Controls.XRMRecordEventArgs e)
        {
            UrlUtils.OpenUrl(e, ConnectionDetail);
        }

        private void btnAssignSelect_Click(object sender, EventArgs e)
        {
            SelectingAssigner();
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
                try
                {
                    XmlSerializerHelper.SerializeToFile(job, savedlg.FileName);
                }
                catch (Exception ex)
                {
                    ShowErrorDialog(ex, "Saving Job");
                }
            }
        }

        private void xrmRecordAssign_ColumnValueChanged(object sender, Rappen.XTB.Helpers.Controls.XRMRecordEventArgs e)
        {
            txtAssignEntity.Text = xrmRecordAssign.EntityDisplayName;
        }

        private void impSeqNo_Changed(object sender, EventArgs e)
        {
            SetImpSeqNo(sender, forcenewnum: sender == btnDefImpSeqNo);
        }

        private void linkShowImpSeqNoRecords_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string fetch = GetFetchFromISN();
            OnOutgoingMessage(this, new MessageBusEventArgs("FetchXML Builder", true) { TargetArgument = fetch });
        }

        private void link_Click(object sender, EventArgs e)
        {
            UrlUtils.OpenUrl(sender);
        }

        private void link_Click(object sender, LinkLabelLinkClickedEventArgs e)
        {
            UrlUtils.OpenUrl(sender);
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            crmGridView1.SelectAll();
        }

        #endregion Form Event Handlers
    }
}