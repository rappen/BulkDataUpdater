using Cinteros.XTB.BulkDataUpdater.AppCode;
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
        private void DeleteRecords(JobExecuteOptions executeoptions)
        {
            if (working)
            {
                return;
            }
            var includedrecords = GetIncludedRecords();
            if (MessageBoxEx.Show(this, $"{includedrecords.Count()} records will unconditionally be deleted.\n" +
                "UI defined rules will NOT be enforced.\n" +
                "Plugins and workflows WILL trigger.\n" +
                "User privileges WILL be respected.\n\n" +
                "Confirm delete!",
                "Confirm", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk) != DialogResult.OK)
            {
                return;
            }
            if (executeoptions.BatchSize > 1 && executeoptions.MultipleRequest && includedrecords.Count() > 1)
            {
                if (MessageBoxEx.Show(this, "Note that the new feature DeleteMultiple from Microsoft is not yet available.\nWe will use ExecuteMultiple instead.", "Batch & Multi", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.Cancel)
                {
                    return;
                }
            }
            working = true;
            EnableControls(false, true);
            if (job != null && job.Delete != null)
            {
                job.Delete.ExecuteOptions = executeoptions;
            }
            var log = new BDULogRun(ConnectionDetail.ServiceClient, "Delete", entitymeta, includedrecords.Count());

            WorkAsync(new WorkAsyncInfo()
            {
                Message = "Deleting records",
                IsCancelable = true,
                Work = (bgworker, workargs) => { DeleteRecordsWork(bgworker, workargs, includedrecords, executeoptions, log); },
                PostWorkCallBack = (completedargs) => { BulkRecordsCallback(completedargs, log); },
                ProgressChanged = (changeargs) => { SetWorkingMessage(changeargs.UserState.ToString()); }
            });
        }

        private void DeleteRecordsWork(System.ComponentModel.BackgroundWorker bgworker, System.ComponentModel.DoWorkEventArgs workargs, System.Collections.Generic.IEnumerable<Entity> includedrecords, JobExecuteOptions executeoptions, BDULogRun log)
        {
            var sw = Stopwatch.StartNew();
            var progress = "Starting...";
            var total = includedrecords.Count();
            var current = 0;
            var deleted = 0;
            var failed = 0;
            var entities = new BDUEntityCollection();
            foreach (var record in includedrecords)
            {
                if (bgworker.CancellationPending)
                {
                    workargs.Cancel = true;
                    break;
                }
                current++;
                entities.Add(new BDUEntity(record.LogicalName, record.Id, record.ToStringExt(ConnectionDetail.ServiceClient)));
                if (entities.Count >= executeoptions.BatchSize || current == total)
                {
                    progress = GetProgressDetails(sw, total, current, entities.Count, failed);
                    WaitingExecution(bgworker, workargs, executeoptions, progress);
                    PushProgress(bgworker, progress);
                    var batch = new ExecuteMultipleRequest
                    {
                        Settings = new ExecuteMultipleSettings { ContinueOnError = executeoptions.IgnoreErrors },
                        Requests = new OrganizationRequestCollection()
                    };
                    foreach (var entity in entities)
                    {
                        var request = new DeleteRequest { Target = entity.ToEntityReference() };
                        batch.Requests.Add(request);
                        SetBypassPlugins(request, executeoptions);
                    }
                    var logreq = log.AddRequest(entities);
                    if (batch.Requests.Count == 1)
                    {
                        failed += ExecuteRequest(batch.Requests.FirstOrDefault(), executeoptions, logreq);
                    }
                    else
                    {
                        failed += ExecuteRequest(batch, executeoptions, logreq);
                    }
                    deleted += batch.Requests.Count;    // - response.Responses.Count(r => r.Fault != null);
                    entities.Clear();
                }
            }
            sw.Stop();
            workargs.Result = new Tuple<int, int, long>(deleted, failed, sw.ElapsedMilliseconds);
        }

        private void UpdateJobDelete(JobDelete job)
        {
        }
    }
}