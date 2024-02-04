using Cinteros.XTB.BulkDataUpdater.AppCode;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Rappen.XRM.Helpers;
using Rappen.XRM.Helpers.Extensions;
using Rappen.XTB.Helpers.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Windows.Forms;
using XrmToolBox.Extensibility;

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
                        qex.ColumnSet = new ColumnSet("name", "returnedtypecode", "fetchxml", "layoutxml");
                        qex.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
                        qex.Criteria.AddCondition("querytype", ConditionOperator.In, 0, 32);
                        qex.AddOrder("name", OrderType.Ascending);
                        bgworker.ReportProgress(33, "Loading system views...");
                        var sysviews = Service.RetrieveMultipleAll(qex, bgworker, "Loading system views... ({0})");
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
                        var userviews = Service.RetrieveMultipleAll(qex, bgworker, "Loading user views... ({0})");
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

        private void RetrieveRecords()
        {
            if (working)
            {
                CancelWorker();
                working = false;
            }
            var fetch = job?.FetchXML;
            if (string.IsNullOrEmpty(fetch))
            {
                crmGridView1.DataSource = null;
                records = null;
                RetrieveRecordsReady();
                return;
            }
            EnableControls(false, true);
            lblRecords.Text = "Retrieving records...";
            fetchResulCount = -1;
            records = null;
            working = true;
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Retrieving records...",
                IsCancelable = true,
                Work = (worker, eventargs) =>
                {
                    eventargs.Result = Service.RetrieveMultipleAll(fetch, worker, eventargs, null);
                },
                PostWorkCallBack = (completedargs) =>
                {
                    working = false;
                    EnableControls(true, false);
                    if (completedargs.Error != null)
                    {
                        ShowErrorDialog(completedargs.Error, "Retrieve Records");
                    }
                    else if (completedargs.Cancelled)
                    {
                        Cursor = Cursors.Default;
                        MessageBox.Show($"Manual abort.", "Execute", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    else if (completedargs.Result is EntityCollection result)
                    {
                        if (result.Entities.WarnIfNoIdReturned())
                        {
                            records = null;
                            crmGridView1.DataSource = null;
                        }
                        else
                        {
                            result.Entities.WarnIf50kReturned(fetch);
                            records = result;
                            fetchResulCount = records.Entities.Count;
                        }
                    }
                    RetrieveRecordsReady();
                },
                ProgressChanged = (changeargs) =>
                {
                    SetWorkingMessage(changeargs.UserState.ToString());
                }
            });
        }

        private void RetrieveRecordsReady()
        {
            Enabled = false;
            Cursor = Cursors.WaitCursor;
            if (records != null)
            {
                if (job == null)
                {
                    job = new BDUJob();
                }
                lblRecords.Text = $"{records.Entities.Count} records of entity {records.EntityName} loaded";
                crmGridView1.Service = Service;
                crmGridView1.LayoutXML = job.LayoutXML;
                crmGridView1.DataSource = records;
                crmGridView1.ShowFriendlyNames = useFriendlyNames;
                crmGridView1.ShowLocalTimes = useFriendlyNames;
                if (string.IsNullOrWhiteSpace(job.LayoutXML))
                {
                    crmGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                }
                UpdateIncludeCount();
            }
            RefreshAttributes();
            InitializeTab();
            Cursor = Cursors.Default;
            EnableControls(true);
        }

        private static void ClearIfDisabled(CheckBox checker)
        {
            if (!checker.Enabled && checker.Checked)
            {
                checker.Checked = false;
            }
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

        private static string GetProgressDetails(Stopwatch sw, int total, int currentrecord, int numberrecords, int failures)
        {
            var processing = numberrecords > 1 ? $"{currentrecord - numberrecords + 1}-{currentrecord}" : currentrecord.ToString();
            var result = $"Processing {processing} of {total} records";
            result += $"\nCompleted: {currentrecord - numberrecords} ";
            if (failures > 0)
            {
                result += $"({failures} failures) ";
            }
            //       result += $"({pct}%) in {sw.Elapsed:hh\\:mm\\:ss\\.fff}";
            if (currentrecord > numberrecords)
            {
                var pct = 100 * (currentrecord - numberrecords) / total;
                result += $"({pct}%) in {TimeSpanToString(sw.Elapsed)}";
                var remaintime = TimeSpan.FromMilliseconds(sw.Elapsed.TotalMilliseconds * total / currentrecord);
                result += $"\nRemaining Time: {TimeSpanToString(remaintime, false)} Records: {total - currentrecord}";
                var timeperrecord = (decimal)sw.ElapsedMilliseconds / (currentrecord - numberrecords) / 1000;
                result += $"\nTime per record: {timeperrecord:0.000}";
            }
            return result;
        }

        private static string TimeSpanToString(TimeSpan span, bool milli = true)
        {
            return
                (span.TotalDays >= 1 ? $"{span.TotalDays:0}d " : string.Empty) +
                (span.TotalHours >= 1 ? $"{span.Hours:00}h" : string.Empty) +
                (span.TotalMinutes >= 1 ? $"{span.Minutes:00}m" : string.Empty) +
                $"{span.Seconds:00}s" +
                (milli ? $"{span:fff}ms" : string.Empty);
        }

        private static void WaitingExecution(System.ComponentModel.BackgroundWorker bgworker, System.ComponentModel.DoWorkEventArgs workargs, JobExecuteOptions executeoptions, string progress)
        {
            if (executeoptions.DelayNow && executeoptions.DelayCallTime > 0)
            {
                executeoptions.DelayCurrent = executeoptions.DelayCallTime;
                while (executeoptions.DelayCurrent > 0)
                {
                    PushProgress(bgworker, $"{progress}{Environment.NewLine}Waiting {executeoptions.DelayCurrent} sec...");
                    if (executeoptions.DelayCurrent > 10)
                    {
                        System.Threading.Thread.Sleep(10000);
                        executeoptions.DelayCurrent -= 10;
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(executeoptions.DelayCurrent * 1000);
                        executeoptions.DelayCurrent = 0;
                    }
                    if (bgworker.CancellationPending)
                    {
                        workargs.Cancel = true;
                        break;
                    }
                }
            }
            executeoptions.DelayNow = true;
        }

        private static void PushProgress(System.ComponentModel.BackgroundWorker bgworker, string progress)
        {
            var progressrows = progress.Split('\n').ToList();
            while (progressrows.Count > 4)
            {
                progressrows.RemoveAt(3);
            }
            progress = string.Join(Environment.NewLine, progressrows);
            bgworker.ReportProgress(0, progress);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="request"></param>
        /// <param name="executeoptions"></param>
        /// <returns>Number of error occured</returns>
        private int ExecuteRequest(OrganizationRequest request, JobExecuteOptions executeoptions)
        {
            var errors = 0;
            try
            {
                SetBypassPlugins(request, executeoptions.BypassCustom);
                var response = Service.Execute(request);
                if (!executeoptions.IgnoreErrors && response is ExecuteMultipleResponse respexc && respexc?.IsFaulted == true)
                {
                    errors = respexc.Responses.Count(r => r.Fault != null);
                    throw new FaultException<OrganizationServiceFault>(respexc.Responses.FirstOrDefault(r => r.Fault != null).Fault);
                }
            }
            catch (Exception ex)
            {
                if (!executeoptions.IgnoreErrors)
                {
                    throw ex;
                }
                if (errors == 0)
                {
                    errors = 1;
                }
            }
            return errors;
        }

        private void SetBypassPlugins(OrganizationRequest request, bool bypass)
        {
            if (bypass && currentversion >= bypasspluginminversion)
            {
                request.Parameters["BypassCustomPluginExecution"] = bypass;
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
                MultipleRequest = rbBatchMultipleRequests.Checked,
                IgnoreErrors = chkIgnoreErrors.Checked,
                BypassCustom = chkBypassPlugins.Checked
            };
        }

        private void UpdateJob()
        {
            job.IncludeAll = rbIncludeAll.Checked;
            UpdateJobUpdate(job.Update);
            UpdateJobAssign(job.Assign);
            UpdateJobSetState(job.SetState);
            UpdateJobDelete(job.Delete);
        }

        private void UseJob(bool retrieve)
        {
            rbIncludeAll.Checked = job.IncludeAll;
            rbIncludeSelected.Checked = !job.IncludeAll;
            FixLoadedBAI(job.Update);
            if (retrieve)
            {
                RetrieveRecords();
            }
        }

        private void BulkRecordsCallback(System.ComponentModel.RunWorkerCompletedEventArgs completedargs, string action)
        {
            working = false;
            EnableControls(true, false);
            if (completedargs.Error != null)
            {
                ShowErrorDialog(completedargs.Error, action);
                if (MessageBox.Show("Error occured.\nRun query to get records again, to verify updated values.", "Cancel", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                {
                    RetrieveRecords();
                }
            }
            else if (completedargs.Cancelled)
            {
                Cursor = Cursors.Default;
                if (MessageBox.Show("Operation cancelled!\nRun query to get records again, to verify updated values.", "Cancel", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                {
                    RetrieveRecords();
                }
            }
            else if (completedargs.Result is Tuple<int, int, long> result)
            {
                lblUpdateStatus.Text = $"{result.Item1} records updated, {result.Item2} records failed.";
                LogUse(action, result.Item1, result.Item3);
                if (result.Item2 > 0)
                {
                    LogUse("Failed", result.Item2);
                }
                if (MessageBox.Show($"{action} completed!\nRun query to show updated records?", "Bulk Data Updater", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    RetrieveRecords();
                }
            }
        }
    }
}