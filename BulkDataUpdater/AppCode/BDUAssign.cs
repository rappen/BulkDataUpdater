using Cinteros.XTB.BulkDataUpdater.AppCode;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
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
            WorkAsync(new WorkAsyncInfo()
            {
                Message = "Assigning records",
                IsCancelable = true,
                Work = (bgworker, workargs) => { AssignRecordsWork(bgworker, workargs, owner, includedrecords, executeoptions); },
                PostWorkCallBack = (completedargs) => { BulkRecordsCallback(completedargs, "Assign"); },
                ProgressChanged = (changeargs) => { SetWorkingMessage(changeargs.UserState.ToString()); }
            });
        }

        private void AssignRecordsWork(System.ComponentModel.BackgroundWorker bgworker, System.ComponentModel.DoWorkEventArgs workargs, Entity owner, System.Collections.Generic.IEnumerable<Entity> includedrecords, JobExecuteOptions executeoptions)
        {
            var sw = Stopwatch.StartNew();
            var progress = "Starting...";
            var total = includedrecords.Count();
            var current = 0;
            var assigned = 0;
            var failed = 0;
            var entities = new EntityCollection
            {
                EntityName = includedrecords.FirstOrDefault().LogicalName
            };
            foreach (var record in includedrecords)
            {
                if (bgworker.CancellationPending)
                {
                    workargs.Cancel = true;
                    break;
                }
                current++;
                var clone = new Entity(record.LogicalName, record.Id);
                clone.Attributes.Add("ownerid", owner.ToEntityReference());
                entities.Entities.Add(clone);
                if (entities.Entities.Count >= executeoptions.BatchSize || current == total)
                {
                    progress = GetProgressDetails(sw, total, current, entities.Entities.Count, failed);
                    WaitingExecution(bgworker, workargs, executeoptions, progress);
                    PushProgress(bgworker, progress);
                    if (entities.Entities.Count == 1)
                    {
                        failed += ExecuteRequest(new UpdateRequest { Target = entities.Entities.FirstOrDefault() }, executeoptions);
                    }
                    else if (executeoptions.MultipleRequest)
                    {
                        failed += ExecuteRequest(new UpdateMultipleRequest { Targets = entities }, executeoptions);
                    }
                    else
                    {
                        var batch = new ExecuteMultipleRequest
                        {
                            Settings = new ExecuteMultipleSettings { ContinueOnError = executeoptions.IgnoreErrors },
                            Requests = new OrganizationRequestCollection()
                        };
                        batch.Requests.AddRange(entities.Entities.Select(e => new UpdateRequest { Target = e }));
                        batch.Requests.ToList().ForEach(r => SetBypassPlugins(r, executeoptions));
                        failed += ExecuteRequest(batch, executeoptions);
                    }
                    assigned += entities.Entities.Count;
                    entities.Entities.Clear();
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