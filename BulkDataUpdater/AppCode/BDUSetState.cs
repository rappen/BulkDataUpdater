using Cinteros.XTB.BulkDataUpdater.AppCode;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
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
            var state = cbSetStatus.SelectedItem as OptionsetItem;
            var status = cbSetStatusReason.SelectedItem as OptionsetItem;
            if (state == null || status == null)
            {
                return;
            }
            if (MessageBox.Show($"All selected records will unconditionally be set to {state.meta.Label.UserLocalizedLabel.Label} {status.meta.Label.UserLocalizedLabel.Label}.\n" +
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
            var ignoreerrors = chkIgnoreErrors.Checked;
            if (!int.TryParse(cmbBatchSize.Text, out int batchsize))
            {
                batchsize = 1;
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
                    var updated = 0;
                    var failed = 0;
                    var batch = new ExecuteMultipleRequest
                    {
                        Settings = new ExecuteMultipleSettings { ContinueOnError = ignoreerrors },
                        Requests = new OrganizationRequestCollection()
                    };
                    foreach (var record in includedrecords)
                    {
                        if (bgworker.CancellationPending)
                        {
                            workargs.Cancel = true;
                            break;
                        }
                        var req = GetSetStateRequest(record, state, status);
                        current++;
                        var pct = 100 * current / total;
                        try
                        {
                            if (batchsize == 1)
                            {
                                bgworker.ReportProgress(pct, $"Setting status for record {current} of {total}");
                                Service.Execute(req);
                                updated++;
                            }
                            else
                            {
                                batch.Requests.Add(req);
                                if (batch.Requests.Count == batchsize || current == total)
                                {
                                    bgworker.ReportProgress(pct, $"Setting status for records {current - batch.Requests.Count + 1}-{current} of {total}");
                                    var response = (ExecuteMultipleResponse)Service.Execute(batch);
                                    if (response.IsFaulted && !ignoreerrors)
                                    {
                                        var firsterror = response.Responses.First(r => r.Fault != null);
                                        if (firsterror != null)
                                        {
                                            throw new Exception(firsterror.Fault.Message);
                                        }
                                        throw new Exception("Unknown exception during assignment");
                                    }
                                    updated += response.Responses.Count;
                                    batch.Requests.Clear();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            failed++;
                            if (!ignoreerrors)
                            {
                                throw ex;
                            }
                        }
                    }
                    sw.Stop();
                    workargs.Result = new Tuple<int, int, long>(updated, failed, sw.ElapsedMilliseconds);
                },
                PostWorkCallBack = (completedargs) =>
                {
                    working = false;
                    tsbCancel.Enabled = false;
                    if (completedargs.Error != null)
                    {
                        MessageBox.Show(completedargs.Error.Message, "Assign", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else if (completedargs.Cancelled)
                    {
                        if (MessageBox.Show("Operation cancelled!\nRun query to get records again, to verify record information.", "Cancel", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                        {
                            RetrieveRecords(fetchXml, RetrieveRecordsReady);
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
                            RetrieveRecords(fetchXml, RetrieveRecordsReady);
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

        private OrganizationRequest GetSetStateRequest(Entity record, OptionsetItem state, OptionsetItem status)
        {
            switch (record.LogicalName)
            {
                case Opportunity.EntityName:
                    {
                        if (state.meta.Value == (int)Opportunity.Status_OptionSet.Won)
                        {
                            var req = new WinOpportunityRequest
                            {
                                OpportunityClose = new Entity(OpportunityClose.EntityName),
                                Status = new OptionSetValue((int)status.meta.Value)
                            };
                            req.OpportunityClose.Attributes.Add(OpportunityClose.Opportunity, record.ToEntityReference());
                            return req;
                        }
                        if (state.meta.Value == (int)Opportunity.Status_OptionSet.Lost)
                        {
                            var req = new LoseOpportunityRequest
                            {
                                OpportunityClose = new Entity(OpportunityClose.EntityName),
                                Status = new OptionSetValue((int)status.meta.Value)
                            };
                            req.OpportunityClose.Attributes.Add(OpportunityClose.Opportunity, record.ToEntityReference());
                            return req;
                        }
                    }
                    break;
                case Lead.EntityName:
                    {
                        if (state.meta.Value == (int)Lead.Status_OptionSet.Qualified)
                        {
                            var req = new QualifyLeadRequest
                            {
                                LeadId = record.ToEntityReference(),
                                Status = new OptionSetValue((int)status.meta.Value),
                                CreateAccount = chkQualifyLeadCreateAccount.Checked,
                                CreateContact = chkQualifyLeadCreateContact.Checked,
                                CreateOpportunity = chkQualifyLeadCreateOpportunity.Checked
                            };
                            return req;
                        }
                    }
                    break;
            }
            var clone = new Entity(record.LogicalName, record.Id);
            clone.Attributes.Add(_common_.Status, new OptionSetValue((int)state.meta.Value));
            clone.Attributes.Add(_common_.StatusReason, new OptionSetValue((int)status.meta.Value));
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

        private void LoadStates(EntityMetadata entity)
        {
            cbSetStatus.Items.Clear();
            cbSetStatusReason.Items.Clear();
            cbSetStatus.Tag = entity;
            if (entity?.Attributes?.Where(a => a.LogicalName == "statecode").FirstOrDefault() is StateAttributeMetadata statemeta)
            {
                cbSetStatus.Items.AddRange(
                    statemeta.OptionSet.Options
                        .Select(o => new OptionsetItem(o)).ToArray());
            }
        }

        private void LoadStatuses()
        {
            cbSetStatusReason.Items.Clear();
            panQualifyLead.Visible = false;
            if (cbSetStatus.Tag is EntityMetadata entity &&
                cbSetStatus.SelectedItem is OptionsetItem state &&
                entity?.Attributes?.Where(a => a.LogicalName == "statuscode").FirstOrDefault() is StatusAttributeMetadata statusmeta)
            {
                cbSetStatusReason.Items.AddRange(
                    statusmeta.OptionSet.Options
                        .Select(o => o as StatusOptionMetadata)
                        .Where(o => o != null && o.State == state.meta.Value)
                        .Select(o => new OptionsetItem(o)).ToArray());
                panQualifyLead.Visible = entity.LogicalName == Lead.EntityName && state.meta.Value == (int)Lead.Status_OptionSet.Qualified;
            }
        }
    }
}
