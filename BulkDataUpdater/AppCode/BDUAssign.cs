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

        private void AssignRecords(JobExecuteOptions executeoptions)
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
            var includedrecords = GetIncludedRecords();
            if (MessageBoxEx.Show($"{includedrecords.Count()} records will unconditionally be reassigned to {txtAssignEntity.Text} {xrmAssignText.Text}.\n" +
                "UI defined rules will NOT be enforced.\n" +
                "Plugins and workflows WILL trigger.\n" +
                "User privileges WILL be respected.\n\n" +
                "Please confirm assignment.",
                "Confirm", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk) != DialogResult.OK)
            {
                return;
            }
            working = true;
            EnableControls(false, true);
            if (job != null && job.Assign != null)
            {
                job.Assign.ExecuteOptions = executeoptions;
            }
            var log = new BDULogRun(ConnectionDetail.ServiceClient, "Assign", entitymeta, includedrecords.Count());

            WorkAsync(new WorkAsyncInfo()
            {
                Message = "Assigning records",
                IsCancelable = true,
                Work = (bgworker, workargs) => { AssignRecordsWork(bgworker, workargs, owner, includedrecords, executeoptions, log); },
                PostWorkCallBack = (completedargs) => { BulkRecordsCallback(completedargs, log); },
                ProgressChanged = (changeargs) => { SetWorkingMessage(changeargs.UserState.ToString()); }
            });
        }

        private void AssignRecordsWork(System.ComponentModel.BackgroundWorker bgworker, System.ComponentModel.DoWorkEventArgs workargs, Entity owner, System.Collections.Generic.IEnumerable<Entity> includedrecords, JobExecuteOptions executeoptions, BDULogRun log)
        {
            var sw = Stopwatch.StartNew();
            var progress = "Starting...";
            var total = includedrecords.Count();
            var current = 0;
            var assigned = 0;
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
                var clone = new BDUEntity(record.LogicalName, record.Id, record.ToStringExt(ConnectionDetail.ServiceClient));
                clone.Attributes.Add("ownerid", owner.ToEntityReference());
                entities.Add(clone);
                if (entities.Count >= executeoptions.BatchSize || current == total)
                {
                    progress = GetProgressDetails(sw, total, current, entities.Count, failed);
                    WaitingExecution(bgworker, workargs, executeoptions, progress);
                    PushProgress(bgworker, progress);
                    var logreq = log.AddRequest(entities);
                    if (entities.Count == 1)
                    {
                        failed += ExecuteRequest(new UpdateRequest { Target = entities.FirstOrDefault() }, executeoptions, logreq);
                    }
                    else if (executeoptions.MultipleRequest)
                    {
                        failed += ExecuteRequest(new UpdateMultipleRequest { Targets = entities.ToEntityCollection() }, executeoptions, logreq);
                    }
                    else
                    {
                        var batch = new ExecuteMultipleRequest
                        {
                            Settings = new ExecuteMultipleSettings { ContinueOnError = executeoptions.IgnoreErrors },
                            Requests = new OrganizationRequestCollection()
                        };
                        batch.Requests.AddRange(entities.Select(e => new UpdateRequest { Target = e }));
                        batch.Requests.ToList().ForEach(r => SetBypassPlugins(r, executeoptions));
                        failed += ExecuteRequest(batch, executeoptions, logreq);
                    }
                    assigned += entities.Count;
                    entities.Clear();
                }
            }
            sw.Stop();
            workargs.Result = new Tuple<int, int, long>(assigned, failed, sw.ElapsedMilliseconds);
        }

        private void SetAssignFromJob(JobAssign job)
        {
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