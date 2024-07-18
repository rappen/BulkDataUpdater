using Cinteros.XTB.BulkDataUpdater.AppCode;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Rappen.XRM.Helpers;
using Rappen.XRM.Helpers.Extensions;
using Rappen.XRM.Tokens;
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
        private void UpdateRecords(JobExecuteOptions executeoptions)
        {
            if (working)
            {
                return;
            }
            var includedrecords = GetIncludedRecords();
            if (MessageBoxEx.Show(this, $"{includedrecords.Count()} records will unconditionally be updated.\nUI defined rules will NOT be enforced.\n\nConfirm update!",
                "Confirm", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk) != DialogResult.OK)
            {
                return;
            }
            working = true;
            EnableControls(false, true);
            var selectedattributes = lvAttributes.Items.Cast<ListViewItem>().Select(i => i.Tag as BulkActionItem).ToList();
            if (job.Update.SetImpSeqNo &&
                !selectedattributes.Any(a => a.Attribute.Metadata.LogicalName == "importsequencenumber") &&
                entitymeta?.Attributes?.FirstOrDefault(a => a.LogicalName == "importsequencenumber") is AttributeMetadata isnmeta)
            {
                selectedattributes.Add(new BulkActionItem
                {
                    Attribute = new AttributeMetadataItem(isnmeta, useFriendlyNames, false),
                    DontTouch = false,
                    Action = BulkActionAction.Set,
                    Value = job.Update.ImpSeqNo
                });
            }
            job.Update.ExecuteOptions = executeoptions;
            var isn = selectedattributes.Any(a => a.Attribute.Metadata.LogicalName == "importsequencenumber");

            WorkAsync(new WorkAsyncInfo()
            {
                Message = "Updating records",
                MessageWidth = 400,
                MessageHeight = 200,
                IsCancelable = true,
                AsyncArgument = selectedattributes,
                Work = (bgworker, workargs) => { UpdateRecordsWork(bgworker, workargs, includedrecords, executeoptions, isn); },
                PostWorkCallBack = (completedargs) => { BulkRecordsCallback(completedargs, "Update"); },
                ProgressChanged = (changeargs) => { SetWorkingMessage(changeargs.UserState.ToString(), 400, 200); }
            }); ;
        }

        private void UpdateRecordsWork(System.ComponentModel.BackgroundWorker bgworker, System.ComponentModel.DoWorkEventArgs workargs, IEnumerable<Entity> includedrecords, JobExecuteOptions executeoptions, bool isn)
        {
            var sw = Stopwatch.StartNew();
            var progress = "Starting...";
            var total = includedrecords.Count();
            var current = 0;
            var updated = 0;
            var failed = 0;
            var attributes = workargs.Argument as List<BulkActionItem>;
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
                if (!CheckAllUpdateAttributesExistOnRecord(record, attributes))
                {
                    //if ((bai.DontTouch || bai.Action == BulkActionAction.Touch) && !attributesexists)
                    PushProgress(bgworker, $"{progress}{Environment.NewLine}Reloading record {current}");
                    LoadMissingAttributesForRecord(record, attributes);
                }
                if (GetUpdateRecord(record, attributes, current) is Entity updateentity && updateentity.Attributes.Count > 0)
                {
                    entities.Entities.Add(updateentity);
                    if (entities.Entities.Count >= executeoptions.BatchSize || current == total)
                    {
                        progress = GetProgressDetails(sw, total, current, entities.Entities.Count, failed);
                        WaitingExecution(bgworker, workargs, executeoptions, progress);
                        PushProgress(bgworker, progress);
                        if (entities.Entities.Count == 1)
                        {
                            failed += ExecuteRequest(new UpdateRequest { Target = entities.Entities.FirstOrDefault() }, executeoptions);
                        }
                        else
                        {
                            if (executeoptions.MultipleRequest || executeoptions.BatchSize == 1)
                            {
                                failed += ExecuteRequest(new UpdateMultipleRequest { Targets = entities }, executeoptions);
                            }
                            else if (isn)
                            {
                                throw new Exception("Import Sequence Number can only be set by UpdateMultiple.");
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
                        }
                        updated += entities.Entities.Count;
                        entities.Entities.Clear();
                    }
                }
            }
            sw.Stop();
            workargs.Result = new Tuple<int, int, long>(updated, failed, sw.ElapsedMilliseconds);
        }

        private Entity GetUpdateRecord(Entity record, List<BulkActionItem> attributes, int sequence)
        {
            if (attributes.Count == 0)
            {
                return null;
            }
            var updaterecord = new Entity(record.LogicalName, record.Id);
            foreach (var bai in attributes)
            {
                var attribute = bai.Attribute.Metadata.LogicalName;
                var currentvalue = record.Contains(attribute) ? record[attribute] : null;
                var newvalue = bai.Value;
                switch (bai.Action)
                {
                    case BulkActionAction.Touch:
                        newvalue = currentvalue;
                        break;

                    case BulkActionAction.Calc:
                        newvalue = CalculateValue(record, bai.Attribute.Metadata, bai.Value.ToString(), sequence);
                        break;
                }
                if (!bai.DontTouch || !ValuesEqual(newvalue, currentvalue))
                {
                    updaterecord[attribute] = newvalue;
                    record[attribute] = newvalue;
                }
            }
            return updaterecord;
        }

        private static bool CheckAllUpdateAttributesExistOnRecord(Entity record, List<BulkActionItem> attributes)
        {
            var allattributesexist = true;
            foreach (var attribute in attributes
                .Where(a => a.Action == BulkActionAction.Touch || (a.Action == BulkActionAction.Set && a.DontTouch))
                .Select(a => a.Attribute.Metadata.LogicalName))
            {
                if (!record.Contains(attribute))
                {
                    allattributesexist = false;
                    break;
                }
            }
            return allattributesexist;
        }

        private void AddAttribute(BulkActionItem bai, bool editing)
        {
            if (bai == null)
            {
                return;
            }
            if (lvAttributes.Items
                .Cast<ListViewItem>()
                .Select(i => i.Tag as BulkActionItem)
                .Any(i => i.Attribute.Metadata.LogicalName == bai.Attribute.Metadata.LogicalName))
            {
                if (!editing && MessageBoxEx.Show(this, $"Replace already added attribute {bai.Attribute} ?", "Attribute added", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.Cancel)
                {
                    return;
                }
                var removeitem = lvAttributes.Items
                    .Cast<ListViewItem>()
                    .FirstOrDefault(i => (i.Tag as BulkActionItem).Attribute.Metadata.LogicalName == bai.Attribute.Metadata.LogicalName);
                lvAttributes.Items.Remove(removeitem);
            }
            AddBAI(bai);
            EnableControls(true);
            UpdateJobUpdate(job.Update);
        }

        private void AddBAI(BulkActionItem bai)
        {
            if (bai.Attribute == null)
            {
                return;
            }
            var item = lvAttributes.Items.Add(bai.Attribute.ToString());
            item.Tag = bai;
            item.SubItems.Add(bai.Action.ToString());
            item.SubItems.Add(bai.StringValue);
            item.SubItems.Add(bai.DontTouch ? "Yes" : "No");
        }

        private void RemoveAttribute()
        {
            var items = lvAttributes.SelectedItems;
            foreach (ListViewItem item in items)
            {
                lvAttributes.Items.Remove(item);
            }
            EnableControls(true);
            UpdateJobUpdate(job.Update);
        }

        private object CalculateValue(Entity record, AttributeMetadata attribute, string format, int sequence)
        {
            if (string.IsNullOrEmpty(format))
            {
                return null;
            }
            return record.Tokens(new GenericBag(Service), format, sequence, string.Empty, true).ConvertTo(attribute);
        }

        private void SetUpdateFromJob(JobUpdate job)
        {
            chkSetImpSeqNo.Checked = job.SetImpSeqNo;
            chkDefImpSeqNo.Checked = job.DefaultImpSeqNo;
            numImpSeqNo.Value = job.ImpSeqNo;
            lvAttributes.Items.Clear();
            job.Attributes.ForEach(a => AddBAI(a));
            if (lvAttributes.Items.Count > 0)
            {
                lvAttributes.Items[0].Selected = true;
            }
        }

        private void UpdateJobUpdate(JobUpdate job)
        {
            job.SetImpSeqNo = chkSetImpSeqNo.Checked;
            job.DefaultImpSeqNo = chkDefImpSeqNo.Checked;
            job.ImpSeqNo = (int)numImpSeqNo.Value;
            job.Attributes = lvAttributes.Items.Cast<ListViewItem>().Select(i => i.Tag as BulkActionItem).ToList();
        }

        private void FixLoadedBAI(JobUpdate job)
        {
            job.Attributes
                .Where(bai => bai.Attribute == null).ToList()
                .ForEach(bai => bai.SetAttribute(Service, useFriendlyNames, true));
        }

        private void SetImpSeqNo(object sender = null, bool forcenewnum = false, bool forcekeepnum = false)
        {
            if (working)
            {
                return;
            }
            working = true;
            if (entitymeta?.Attributes?.Any(a => a.LogicalName == "importsequencenumber") == true)
            {
                gbImpSeqNo.Enabled = true;
                chkDefImpSeqNo.Enabled = chkSetImpSeqNo.Checked;
                numImpSeqNo.Enabled = chkSetImpSeqNo.Checked && !chkDefImpSeqNo.Checked;
                btnDefImpSeqNo.Enabled = chkSetImpSeqNo.Checked && chkDefImpSeqNo.Checked;
                if ((!forcekeepnum || numImpSeqNo.Value == 0) && chkSetImpSeqNo.Checked && chkDefImpSeqNo.Checked)
                {
                    var impseqno = numImpSeqNo.Value;
                    var today = DateTime.Today.ToString("yyMMdd");
                    if (forcenewnum || !impseqno.ToString().StartsWith(today))
                    {
                        impseqno = int.Parse(today) * 1000;
                        impseqno = impseqno + new Random().Next(999) + 1;
                        numImpSeqNo.Value = impseqno;
                        linkShowImpSeqNoRecords.Enabled = false;
                    }
                }
                if (sender != null)
                {
                    UpdateJobUpdate(job.Update);
                }
            }
            else
            {
                gbImpSeqNo.Enabled = false;
                chkSetImpSeqNo.Checked = false;
            }
            working = false;
        }

        private string GetFetchFromISN()
        {
            var attrs = string.Join("", lvAttributes.Items
                .Cast<ListViewItem>()
                .Select(i => i.Tag as BulkActionItem)
                .Select(a => a.Attribute.Metadata.LogicalName)
                .Where(a => a != "importsequencenumber")
                .Where(a => a != entitymeta?.PrimaryNameAttribute)
                .Select(a => $"<attribute name='{a}'/>"));
            return $"<fetch><entity name='{entitymeta.LogicalName}'><attribute name='{entitymeta.PrimaryNameAttribute}'/><attribute name='importsequencenumber'/>{attrs}<filter><condition attribute='importsequencenumber' operator='eq' value='{numImpSeqNo.Value}'/></filter></entity></fetch>";
        }
    }
}