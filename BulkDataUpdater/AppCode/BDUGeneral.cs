using Cinteros.XTB.BulkDataUpdater.AppCode;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Rappen.XRM.Helpers;
using Rappen.XTB.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Args;

namespace Cinteros.XTB.BulkDataUpdater
{
    public partial class BulkDataUpdater
    {
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

        internal static List<EntityMetadata> GetDisplayEntities()
        {
            var result = new List<EntityMetadata>();
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
                    result.Add(entity);
                }
            }
            return result;
        }

        internal static string GetEntityDisplayName(string entityName)
        {
            if (!useFriendlyNames)
            {
                return entityName;
            }
            if (entities?.FirstOrDefault(e => e.LogicalName == entityName) is EntityMetadata entity)
            {
                entityName = GetEntityDisplayName(entity);
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

        internal void LoadViews(Action viewsLoaded)
        {
            if (working)
            {
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
                            if (!string.IsNullOrWhiteSpace(entityname) && entities.Any(e => e.LogicalName == entityname))
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
                            if (!string.IsNullOrWhiteSpace(entityname) && entities.Any(e => e.LogicalName == entityname))
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
                        ShowErrorDialog(completedargs.Error, "Load Views");
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

        internal void LogUse(string action, double? count = null, double? duration = null)
        {
            ai.WriteEvent(action, count, duration, HandleAIResult);
        }

        private void HandleAIResult(string result)
        {
            if (!string.IsNullOrEmpty(result))
            {
                LogError("Failed to write to Application Insights:\n{0}", result);
            }
        }

        private void RetrieveRecords(string fetch = null, Action nextmethod = null)
        {
            if (working)
            {
                CancelWorker();
                working = false;
            }
            if (string.IsNullOrEmpty(fetch))
            {
                fetch = job?.FetchXML;
            }
            if (string.IsNullOrEmpty(fetch))
            {
                crmGridView1.DataSource = null;
                records = null;
                RetrieveRecordsReady(nextmethod);
                return;
            }
            EnableControls(false);
            lblRecords.Text = "Retrieving records...";
            fetchResulCount = -1;
            records = null;
            working = true;
            QueryBase query;
            try
            {
                query = ((FetchXmlToQueryExpressionResponse)Service.Execute(new FetchXmlToQueryExpressionRequest() { FetchXml = fetch })).Query;
            }
            catch
            {
                query = new FetchExpression(fetch);
            }
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Retrieving records...",
                IsCancelable = true,
                Work = (worker, eventargs) =>
                {
                    EntityCollection retrieved = RetrieveRecordsAllPages(worker, query);
                    eventargs.Result = retrieved;
                },
                PostWorkCallBack = (completedargs) =>
                {
                    working = false;
                    if (completedargs.Error != null)
                    {
                        ShowErrorDialog(completedargs.Error, "Retrieve Records");
                    }
                    else if (!completedargs.Cancelled && completedargs.Result is EntityCollection result)
                    {
                        records = result;
                        fetchResulCount = records.Entities.Count;
                    }
                    RetrieveRecordsReady(nextmethod);
                },
                ProgressChanged = (changeargs) =>
                {
                    SetWorkingMessage(changeargs.UserState.ToString());
                }
            });
        }

        private EntityCollection RetrieveRecordsAllPages(BackgroundWorker worker, QueryBase query)
        {
            var start = DateTime.Now;
            EntityCollection resultCollection = null;
            EntityCollection tmpResult = null;
            var page = 0;
            do
            {
                tmpResult = Service.RetrieveMultiple(query);
                if (resultCollection == null)
                {
                    resultCollection = tmpResult;
                }
                else
                {
                    resultCollection.Entities.AddRange(tmpResult.Entities);
                    resultCollection.MoreRecords = tmpResult.MoreRecords;
                    resultCollection.PagingCookie = tmpResult.PagingCookie;
                    resultCollection.TotalRecordCount = tmpResult.TotalRecordCount;
                    resultCollection.TotalRecordCountLimitExceeded = tmpResult.TotalRecordCountLimitExceeded;
                }
                if (query is QueryExpression && tmpResult.MoreRecords)
                {
                    ((QueryExpression)query).PageInfo.PageNumber++;
                    ((QueryExpression)query).PageInfo.PagingCookie = tmpResult.PagingCookie;
                }
                page++;
                var duration = DateTime.Now - start;
                worker.ReportProgress(0, $"Retrieving records... ({resultCollection.Entities.Count})");
                if (page == 1)
                {
                    SendMessageToStatusBar(this, new StatusBarMessageEventArgs($"Retrieved {resultCollection.Entities.Count} records on first page in {duration.TotalSeconds:F2} seconds"));
                }
                else
                {
                    SendMessageToStatusBar(this, new StatusBarMessageEventArgs($"Retrieved {resultCollection.Entities.Count} records on {page} pages in {duration.TotalSeconds:F2} seconds"));
                }
            }
            while (query is QueryExpression && tmpResult.MoreRecords);
            return resultCollection;
        }

        private void RetrieveRecordsReady(Action nextmethod)
        {
            if (records != null)
            {
                if (job == null)
                {
                    job = new BDUJob();
                }
                lblRecords.Text = $"{records.Entities.Count} records of entity {records.EntityName} loaded";
                crmGridView1.Service = Service;
                crmGridView1.DataSource = records;
                crmGridView1.ShowFriendlyNames = useFriendlyNames;
                crmGridView1.ShowLocalTimes = useFriendlyNames;
                crmGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                UpdateIncludeCount();
            }
            EnableControls(true);
            RefreshAttributes();
            InitializeTab();
            nextmethod?.Invoke();
        }

        private IEnumerable<Entity> GetIncludedRecords()
        {
            // The following line can be restored when xrmtb controls version > 2.2.6 is used
            //return rbIncludeSelected.Checked ? crmGridView1.SelectedRowRecords : records.Entities;
            var rows = rbIncludeSelected.Checked ? crmGridView1.SelectedRows.OfType<DataGridViewRow>() : crmGridView1.Rows.OfType<DataGridViewRow>();
            rows = rows.OrderBy(r => r.Index);
            var result = rows.Where(r => r.Cells["#entity"]?.Value is Entity).Select(r => r.Cells["#entity"].Value as Entity);
            return result;
        }

        private bool ValuesEqual(object value1, object value2)
        {
            return Utils.ValueToString(value1).Equals(Utils.ValueToString(value2));
        }

        private void SetBypassPlugins(OrganizationRequest request, bool value)
        {
            if (value && currentversion >= bypasspluginminversion)
            {
                request.Parameters["BypassCustomPluginExecution"] = value;
            }
            else
            {
                request.Parameters.Remove("BypassCustomPluginExecution");
            }
        }

        private JobExecuteOptions GetExecuteOptions()
        {
            return new JobExecuteOptions
            {
                DelayCallTime = int.TryParse(cmbDelayCall.Text, out var delay) ? delay : 0,
                BatchSize = int.TryParse(cmbBatchSize.Text, out int updsize) ? updsize : 1,
                IgnoreErrors = chkIgnoreErrors.Checked,
                BypassCustom = chkBypassPlugins.Checked
            };
        }
    }
}