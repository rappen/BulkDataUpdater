using Cinteros.XTB.BulkDataUpdater.AppCode;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
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
        private bool updatingExecuteOptions;

        internal void LogUse(string action, double? count = null, double? duration = null, bool ai1 = true, bool ai2 = false)
        {
            if (ai1)
            {
                this.ai1.WriteEvent(action, count, duration, HandleAIResult);
            }
            if (ai2)
            {
                this.ai2.WriteEvent(action, count, duration, HandleAIResult);
            }
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
            lblSelectedRecords.Text = "Fetching records...";
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
                        entitymeta = entities.FirstOrDefault(e => e.LogicalName == records?.EntityName);
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
                isOnForms = null;
                isOnViews = null;
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
            InitializeTab();
            Cursor = Cursors.Default;
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
            var rows = crmGridView1.SelectedRows.OfType<DataGridViewRow>();
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
            var completed = currentrecord - numberrecords;
            var remaining = total - completed;
            var processing = numberrecords > 1 ? $"{completed + 1}-{currentrecord}" : currentrecord.ToString();
            var result = string.Empty;
            if (completed > 0)
            {
                result += $"\nCompleted: {completed} ";
                if (failures > 0)
                {
                    result += $"({failures} failures) ";
                }
                var pct = 100 * completed / total;
                result += $"({pct}%) in {sw.Elapsed.ToSmartString()}";
            }
            result += $"\nProcessing {processing} of {total} records";
            if (completed > 0)
            {
                var remaintime = TimeSpan.FromMilliseconds(sw.Elapsed.TotalMilliseconds / completed * remaining);
                result += $"\nRemaining {remaintime.ToSmartString()} for {remaining} records";
                var timeperrecord = TimeSpan.FromMilliseconds((double)sw.ElapsedMilliseconds / completed);
                result += $"\nTime per record: {timeperrecord.ToSmartString()}";
            }
            return result.Trim();
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
                SetBypassPlugins(request, executeoptions);
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

        private void SetBypassPlugins(OrganizationRequest request, JobExecuteOptions options)
        {
            if (currentversion >= bypasspluginminversion)
            {
                if (options.BypassCustom)
                {
                    request.Parameters["BypassCustomPluginExecution"] = true;
                }
                else
                {
                    request.Parameters.Remove("BypassCustomPluginExecution");
                }
                if (options.BypassSync || options.BypassAsync)
                {
                    var syasy = new[] { options.BypassSync ? "CustomSync" : "", options.BypassAsync ? "CustomAsync" : "" }.Where(s => !string.IsNullOrEmpty(s));
                    request.Parameters.Add("BypassBusinessLogicExecution", string.Join(",", syasy));
                }
                else
                {
                    request.Parameters.Remove("BypassBusinessLogicExecution");
                }
                if (options.BypassSteps != null && options.BypassSteps.Any())
                {
                    request.Parameters.Add("BypassBusinessLogicExecutionStepIds", string.Join(",", options.BypassSteps.Select(i => i.ToString())));
                }
                else
                {
                    request.Parameters.Remove("BypassBusinessLogicExecutionStepIds");
                }
            }
            else
            {
                request.Parameters.Remove("BypassCustomPluginExecution");
                request.Parameters.Remove("BypassBusinessLogicExecution");
                request.Parameters.Remove("BypassBusinessLogicExecutionStepIds");
            }
        }

        private void UpdateJobFromUI(TabPage selectedTab)
        {
            if (updatingExecuteOptions)
            {
                return;
            }
            job = job ?? new BDUJob();
            if (selectedTab == tabUpdate)
            {
                UpdateJobUpdate(job.Update);
            }
            else if (selectedTab == tabAssign)
            {
                UpdateJobAssign(job.Assign);
            }
            else if (selectedTab == tabSetState)
            {
                UpdateJobSetState(job.SetState);
            }
            else if (selectedTab == tabDelete)
            {
                UpdateJobDelete(job.Delete);
            }
        }

        private void UseJob(bool retrieve)
        {
            SetImpSeqNo(forcekeepnum: true);
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
            if (action == "Update" && chkSetImpSeqNo.Checked)
            {
                linkShowImpSeqNoRecords.Enabled = true;
            }
        }
    }
}