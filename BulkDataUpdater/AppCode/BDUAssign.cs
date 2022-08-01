﻿using Cinteros.XTB.BulkDataUpdater.AppCode;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Rappen.XRM.Helpers.Extensions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using XrmToolBox.Extensibility;

namespace Cinteros.XTB.BulkDataUpdater
{
    public partial class BulkDataUpdater
    {
        private void SelectingAssigner()
        {
            xrmLookupAssign.Service = Service;
            switch (xrmLookupAssign.ShowDialog(this))
            {
                case DialogResult.OK:
                    xrmRecordAssign.Service = Service;
                    xrmRecordAssign.Record = xrmLookupAssign.Record;
                    break;

                case DialogResult.Abort:
                    xrmRecordAssign.Record = null;
                    break;
            }
            EnableControls(true);
            UpdateJobAssign(job.Assign);
        }

        private void AssignRecords()
        {
            if (working)
            {
                return;
            }
            var owner = xrmRecordAssign.Record;
            if (owner == null)
            {
                return;
            }
            if (MessageBox.Show($"All selected records will unconditionally be reassigned to {txtAssignEntity.Text} {xrmAssignText.Text}.\n" +
                "UI defined rules will NOT be enforced.\n" +
                "Plugins and workflows WILL trigger.\n" +
                "User privileges WILL be respected.\n\n" +
                "Please confirm assignment.",
                "Confirm", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk) != DialogResult.OK)
            {
                return;
            }
            tsbCancel.Enabled = true;
            splitContainer1.Enabled = false;
            working = true;
            var includedrecords = GetIncludedRecords();
            var executeoptions = GetExecuteOptions();
            if (job != null && job.Assign != null)
            {
                job.Assign.ExecuteOptions = executeoptions;
            }
            WorkAsync(new WorkAsyncInfo()
            {
                Message = "Assigning records",
                IsCancelable = true,
                Work = (bgworker, workargs) =>
                {
                    var sw = Stopwatch.StartNew();
                    var total = includedrecords.Count();
                    var current = 0;
                    var assigned = 0;
                    var failed = 0;
                    var batch = new ExecuteMultipleRequest
                    {
                        Settings = new ExecuteMultipleSettings { ContinueOnError = executeoptions.IgnoreErrors },
                        Requests = new OrganizationRequestCollection()
                    };
                    foreach (var record in includedrecords)
                    {
                        if (bgworker.CancellationPending)
                        {
                            workargs.Cancel = true;
                            break;
                        }
                        current++;
                        var pct = 100 * current / total;
                        try
                        {
                            var clone = new Entity(record.LogicalName, record.Id);
                            clone.Attributes.Add("ownerid", owner.ToEntityReference());
                            var request = new UpdateRequest { Target = clone };
                            SetBypassPlugins(request, executeoptions.BypassCustom);
                            if (executeoptions.BatchSize == 1)
                            {
                                bgworker.ReportProgress(pct, $"Assigning record {current} of {total}");
                                Service.Execute(request);
                                assigned++;
                            }
                            else
                            {
                                batch.Requests.Add(request);
                                if (batch.Requests.Count == executeoptions.BatchSize || current == total)
                                {
                                    bgworker.ReportProgress(pct, $"Assigning records {current - batch.Requests.Count + 1}-{current} of {total}");
                                    var response = (ExecuteMultipleResponse)Service.Execute(batch);
                                    if (response.IsFaulted && !executeoptions.IgnoreErrors)
                                    {
                                        var firsterror = response.Responses.First(r => r.Fault != null);
                                        if (firsterror != null)
                                        {
                                            throw new Exception(firsterror.Fault.Message);
                                        }
                                        throw new Exception("Unknown exception during assignment");
                                    }
                                    assigned += response.Responses.Count;
                                    batch.Requests.Clear();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            failed++;
                            if (!executeoptions.IgnoreErrors)
                            {
                                throw ex;
                            }
                        }
                    }
                    sw.Stop();
                    workargs.Result = new Tuple<int, int, long>(assigned, failed, sw.ElapsedMilliseconds);
                },
                PostWorkCallBack = (completedargs) =>
                {
                    working = false;
                    tsbCancel.Enabled = false;
                    if (completedargs.Error != null)
                    {
                        ShowErrorDialog(completedargs.Error, "Assign");
                    }
                    else if (completedargs.Cancelled)
                    {
                        if (MessageBox.Show("Operation cancelled!\nRun query to get records again, to verify record information.", "Cancel", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                        {
                            RetrieveRecords();
                        }
                    }
                    else if (completedargs.Result is Tuple<int, int, long> result)
                    {
                        lblUpdateStatus.Text = $"{result.Item1} records reassigned, {result.Item2} records failed.";
                        LogUse("Assigned", result.Item1, result.Item3);
                        if (result.Item2 > 0)
                        {
                            LogUse("Failed", result.Item2);
                        }
                        if (MessageBox.Show("Assign completed!\nRun query to get records again?", "Bulk Data Updater", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        {
                            RetrieveRecords();
                        }
                    }
                    splitContainer1.Enabled = true;
                },
                ProgressChanged = (changeargs) =>
                {
                    SetWorkingMessage(changeargs.UserState.ToString());
                }
            });
        }

        private void SetAssignFromJob(JobAssign job)
        {
            cmbDelayCall.SelectedItem = cmbDelayCall.Items.Cast<string>().FirstOrDefault(i => i == "0");
            cmbBatchSize.SelectedItem = cmbBatchSize.Items.Cast<string>().FirstOrDefault(i => i == job.ExecuteOptions.BatchSize.ToString());
            chkIgnoreErrors.Checked = job.ExecuteOptions.IgnoreErrors;
            chkBypassPlugins.Checked = job.ExecuteOptions.BypassCustom;
            if (!string.IsNullOrEmpty(job.Entity) && !job.Id.Equals(Guid.Empty))
            {
                xrmRecordAssign.Service = Service;
                xrmRecordAssign.LogicalName = job.Entity;
                xrmRecordAssign.Id = job.Id;
            }
            else
            {
                xrmRecordAssign.Record = null;
            }
        }

        private void UpdateJobAssign(JobAssign job)
        {
            job.Entity = xrmRecordAssign.Record?.LogicalName;
            job.Id = xrmRecordAssign.Record?.Id ?? Guid.Empty;
            job.Name = xrmAssignText.Text;
        }
    }
}