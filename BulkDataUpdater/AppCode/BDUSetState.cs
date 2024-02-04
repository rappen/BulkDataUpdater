using Cinteros.XTB.BulkDataUpdater.AppCode;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Rappen.XTB.Helpers.ControlItems;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using XrmToolBox.Extensibility;

namespace Cinteros.XTB.BulkDataUpdater
{
    public partial class BulkDataUpdater
    {
        private void SetStateRecords()
        {
            if (working)
            {
                return;
            }
            var state = cbSetStatus.SelectedItem as OptionMetadataItem;
            var status = cbSetStatusReason.SelectedItem as OptionMetadataItem;
            if (state == null || status == null)
            {
                return;
            }
            var includedrecords = GetIncludedRecords();
            if (MessageBox.Show($"{includedrecords.Count()} records will unconditionally be set to {state.Metadata.Label.UserLocalizedLabel.Label} {status.Metadata.Label.UserLocalizedLabel.Label}.\n" +
                "UI defined rules will NOT be enforced.\n" +
                "Plugins and workflows WILL trigger.\n" +
                "User privileges WILL be respected.\n\n" +
                "Please confirm set state.",
                "Confirm", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk) != DialogResult.OK)
            {
                return;
            }
            var executeoptions = GetExecuteOptions();
            if (executeoptions.BatchSize > 1 && executeoptions.MultipleRequest && includedrecords.Count() > 1)
            {
                if (MessageBox.Show("Note that the new feature SetStateMultiple from Microsoft is not (yet) available.\nWe will use ExecuteMultiple instead.", "Batch & Multi", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.Cancel)
                {
                    return;
                }
            }
            working = true;
            EnableControls(false, true);
            if (job != null && job.SetState != null)
            {
                job.SetState.ExecuteOptions = executeoptions;
            }
            WorkAsync(new WorkAsyncInfo()
            {
                Message = "Set State for records",
                IsCancelable = true,
                Work = (bgworker, workargs) => { SetStateRecordsWork(bgworker, workargs, state, status, includedrecords, executeoptions); },
                PostWorkCallBack = (completedargs) => { BulkRecordsCallback(completedargs, "SetState"); },
                ProgressChanged = (changeargs) => { SetWorkingMessage(changeargs.UserState.ToString()); }
            });
        }

        private void SetStateRecordsWork(System.ComponentModel.BackgroundWorker bgworker, System.ComponentModel.DoWorkEventArgs workargs, OptionMetadataItem state, OptionMetadataItem status, IEnumerable<Entity> includedrecords, JobExecuteOptions executeoptions)
        {
            var sw = Stopwatch.StartNew();
            var progress = "Starting...";
            var total = includedrecords.Count();
            var current = 0;
            var updated = 0;
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
                var req = GetSetStateRequest(record, state, status);
                SetBypassPlugins(req, executeoptions.BypassCustom);
                batch.Requests.Add(req);
                if (batch.Requests.Count >= executeoptions.BatchSize || current == total)
                {
                    progress = GetProgressDetails(sw, total, current, batch.Requests.Count, failed);
                    WaitingExecution(bgworker, workargs, executeoptions, progress);
                    PushProgress(bgworker, progress);
                    if (batch.Requests.Count == 1)
                    {
                        failed += ExecuteRequest(batch.Requests.FirstOrDefault(), executeoptions);
                    }
                    else
                    {
                        failed += ExecuteRequest(batch, executeoptions);
                    }
                    updated += batch.Requests.Count;
                    batch.Requests.Clear();
                }
            }
            sw.Stop();
            workargs.Result = new Tuple<int, int, long>(updated, failed, sw.ElapsedMilliseconds);
        }

        private OrganizationRequest GetSetStateRequest(Entity record, OptionMetadataItem state, OptionMetadataItem status)
        {
            switch (record.LogicalName)
            {
                case Opportunity.EntityName:
                    {
                        if (state.Metadata.Value == (int)Opportunity.Status_OptionSet.Won)
                        {
                            var req = new WinOpportunityRequest
                            {
                                OpportunityClose = new Entity(OpportunityClose.EntityName),
                                Status = new OptionSetValue((int)status.Metadata.Value)
                            };
                            req.OpportunityClose.Attributes.Add(OpportunityClose.Opportunity, record.ToEntityReference());
                            return req;
                        }
                        if (state.Metadata.Value == (int)Opportunity.Status_OptionSet.Lost)
                        {
                            var req = new LoseOpportunityRequest
                            {
                                OpportunityClose = new Entity(OpportunityClose.EntityName),
                                Status = new OptionSetValue((int)status.Metadata.Value)
                            };
                            req.OpportunityClose.Attributes.Add(OpportunityClose.Opportunity, record.ToEntityReference());
                            return req;
                        }
                    }
                    break;

                case Lead.EntityName:
                    {
                        if (state.Metadata.Value == (int)Lead.Status_OptionSet.Qualified)
                        {
                            var req = new QualifyLeadRequest
                            {
                                LeadId = record.ToEntityReference(),
                                Status = new OptionSetValue((int)status.Metadata.Value),
                                CreateAccount = chkQualifyLeadCreateAccount.Checked,
                                CreateContact = chkQualifyLeadCreateContact.Checked,
                                CreateOpportunity = chkQualifyLeadCreateOpportunity.Checked
                            };
                            return req;
                        }
                    }
                    break;
            }
            if (chkSetState.Checked)
            {
                return new SetStateRequest
                {
                    EntityMoniker = record.ToEntityReference(),
                    State = new OptionSetValue((int)state.Metadata.Value),
                    Status = new OptionSetValue((int)status.Metadata.Value)
                };
            }
            var clone = new Entity(record.LogicalName, record.Id);
            clone.Attributes.Add(_common_.Status, new OptionSetValue((int)state.Metadata.Value));
            clone.Attributes.Add(_common_.StatusReason, new OptionSetValue((int)status.Metadata.Value));
            return new UpdateRequest { Target = clone };
        }

        private bool UpdateState(Entity record, List<BulkActionItem> attributes)
        {
            var attribute = attributes.FirstOrDefault(a => a.Attribute.Metadata is StateAttributeMetadata);
            if (attribute == null)
            {
                return false;
            }
            var statevalue = GetCurrentStateCodeFromStatusCode(attributes);
            var statusvalue = attribute.Value as OptionSetValue;
            var currentstate = record.Contains("statecode") ? record["statecode"] : new OptionSetValue(-1);
            var currentstatus = record.Contains("statuscode") ? record["statuscode"] : new OptionSetValue(-1);
            if (attribute.Action == BulkActionAction.Touch)
            {
                statevalue = (OptionSetValue)currentstate;
                statusvalue = (OptionSetValue)currentstatus;
            }
            if (!attribute.DontTouch || !ValuesEqual(currentstate, statevalue) || !ValuesEqual(currentstatus, statusvalue))
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
                    record["statuscode"] = currentstatus;
                }
                else
                {
                    record.Attributes.Add("statuscode", currentstatus);
                }
                return true;
            }
            return false;
        }

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

        private void LoadStates(EntityMetadata entity, int? selected)
        {
            cbSetStatus.Items.Clear();
            cbSetStatusReason.Items.Clear();
            cbSetStatus.Tag = entity;
            if (entity?.Attributes?.Where(a => a.LogicalName == "statecode").FirstOrDefault() is StateAttributeMetadata statemeta)
            {
                cbSetStatus.Items.AddRange(
                    statemeta.OptionSet.Options
                        .Select(o => new OptionMetadataItem(o, true)).ToArray());
            }
            cbSetStatus.SelectedItem = cbSetStatus.Items.Cast<OptionMetadataItem>().FirstOrDefault(s => s.Metadata.Value == selected);
        }

        private void LoadStatuses(int? selected)
        {
            cbSetStatusReason.Items.Clear();
            panQualifyLead.Visible = false;
            if (cbSetStatus.Tag is EntityMetadata entity &&
                cbSetStatus.SelectedItem is OptionMetadataItem state &&
                entity?.Attributes?.Where(a => a.LogicalName == "statuscode").FirstOrDefault() is StatusAttributeMetadata statusmeta)
            {
                cbSetStatusReason.Items.AddRange(
                    statusmeta.OptionSet.Options
                        .Select(o => o as StatusOptionMetadata)
                        .Where(o => o != null && o.State == state.Metadata.Value)
                        .Select(o => new OptionMetadataItem(o, true)).ToArray());
                panQualifyLead.Visible = entity.LogicalName == Lead.EntityName && state.Metadata.Value == (int)Lead.Status_OptionSet.Qualified;
            }
            cbSetStatusReason.SelectedItem = cbSetStatusReason.Items.Cast<OptionMetadataItem>().FirstOrDefault(s => s.Metadata.Value == selected);
        }

        private void SetSetStateFromJob(JobSetState job)
        {
            cmbDelayCall.SelectedItem = cmbDelayCall.Items.Cast<string>().FirstOrDefault(i => i == "0");
            cmbBatchSize.SelectedItem = cmbBatchSize.Items.Cast<string>().FirstOrDefault(i => i == job.ExecuteOptions.BatchSize.ToString());
            chkIgnoreErrors.Checked = job.ExecuteOptions.IgnoreErrors;
            chkBypassPlugins.Checked = job.ExecuteOptions.BypassCustom;
            LoadStates(entities?.FirstOrDefault(ent => ent.LogicalName == records?.EntityName), job?.State);
        }

        private void UpdateJobSetState(JobSetState job)
        {
            job.State = (cbSetStatus.SelectedItem as OptionMetadataItem)?.Metadata?.Value ?? 0;
            job.Status = (cbSetStatusReason.SelectedItem as OptionMetadataItem)?.Metadata?.Value ?? 0;
            job.StateName = cbSetStatus.Text;
            job.StatusName = cbSetStatusReason.Text;
        }
    }
}