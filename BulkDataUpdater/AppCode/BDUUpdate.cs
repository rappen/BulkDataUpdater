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
        private void UpdateRecords()
        {
            if (working)
            {
                return;
            }
            var includedrecords = GetIncludedRecords();
            if (MessageBox.Show($"{includedrecords.Count()} records will unconditionally be updated.\nUI defined rules will NOT be enforced.\n\nConfirm update!",
                "Confirm", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk) != DialogResult.OK)
            {
                return;
            }
            working = true;
            EnableControls(false, true);
            var selectedattributes = lvAttributes.Items.Cast<ListViewItem>().Select(i => i.Tag as BulkActionItem).ToList();
            var executeoptions = GetExecuteOptions();
            job.Update.ExecuteOptions = executeoptions;

            WorkAsync(new WorkAsyncInfo()
            {
                Message = "Updating records",
                IsCancelable = true,
                AsyncArgument = selectedattributes,
                Work = (bgworker, workargs) => { UpdateRecordsWork(bgworker, workargs, includedrecords, executeoptions); },
                PostWorkCallBack = (completedargs) => { BulkRecordsCallback(completedargs, "Update"); },
                ProgressChanged = (changeargs) => { SetWorkingMessage(changeargs.UserState.ToString()); }
            }); ;
        }

        private void UpdateRecordsWork(System.ComponentModel.BackgroundWorker bgworker, System.ComponentModel.DoWorkEventArgs workargs, IEnumerable<Entity> includedrecords, JobExecuteOptions executeoptions)
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
                            batch.Requests.ToList().ForEach(r => SetBypassPlugins(r, executeoptions.BypassCustom));
                            failed += ExecuteRequest(batch, executeoptions);
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
                .Where(a => a.Action == BulkActionAction.Touch || (a.Action == BulkActionAction.SetValue && a.DontTouch))
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

        private void AddAttribute()
        {
            if (!(GetAttributeItemFromUI() is BulkActionItem bai))
            {
                return;
            }
            if (lvAttributes.Items
                .Cast<ListViewItem>()
                .Select(i => i.Tag as BulkActionItem)
                .Any(i => i.Attribute.Metadata.LogicalName == bai.Attribute.Metadata.LogicalName))
            {
                if (MessageBox.Show($"Replace already added attribute {bai.Attribute} ?", "Attribute added", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.Cancel)
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

        private BulkActionItem GetAttributeItemFromUI()
        {
            if (!(cmbAttribute.SelectedItem is AttributeMetadataItem attribute))
            {
                MessageBox.Show("Select an attribute to update from the list.");
                return null;
            }
            var bai = new BulkActionItem
            {
                Attribute = attribute,
                DontTouch = chkOnlyChange.Checked,
                Action =
                    rbSetValue.Checked ? BulkActionAction.SetValue :
                    rbCalculate.Checked ? BulkActionAction.Calc :
                    rbSetNull.Checked ? BulkActionAction.Null :
                    BulkActionAction.Touch
            };
            try
            {
                switch (bai.Action)
                {
                    case BulkActionAction.SetValue:
                        bai.Value = GetValueFromUI(bai.Attribute.Metadata);
                        if (attribute.Metadata is MemoAttributeMetadata)
                        {
                            bai.StringValue = txtValueMultiline.Text;
                        }
                        else if (attribute.Metadata is LookupAttributeMetadata)
                        {
                            bai.StringValue = cdsLookupValue.Text;
                        }
                        else if (bai.Value is OptionSetValueCollection osvc)
                        {
                            bai.StringValue = $"({osvc.Count} choices)";
                        }
                        else
                        {
                            bai.StringValue = cmbValue.Text;
                        }
                        break;

                    case BulkActionAction.Calc:
                        bai.Value = txtValueCalc.Text;
                        bai.StringValue = txtValueCalc.Text;
                        break;

                    default:
                        bai.Value = null;
                        bai.StringValue = string.Empty;
                        break;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Value error:\n" + e.Message, "Set value", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            return bai;
        }

        private object GetValueFromUI(AttributeMetadata meta)
        {
            if (meta is StringAttributeMetadata)
            {
                return cmbValue.Text;
            }
            if (meta is MemoAttributeMetadata)
            {
                return txtValueMultiline.Text;
            }
            if (meta is IntegerAttributeMetadata || meta is BigIntAttributeMetadata)
            {
                return int.Parse(cmbValue.Text);
            }
            if (meta is DecimalAttributeMetadata)
            {
                return decimal.Parse(cmbValue.Text);
            }
            if (meta is DoubleAttributeMetadata)
            {
                return double.Parse(cmbValue.Text);
            }
            if (meta is EnumAttributeMetadata)
            {
                var value = ((OptionMetadataItem)cmbValue.SelectedItem).Metadata.Value;
                return new OptionSetValue((int)value);
            }
            if (meta is MultiSelectPicklistAttributeMetadata)
            {
                var values = chkMultiSelects.CheckedItems.OfType<OptionMetadataItem>().Select(o => o.Metadata.Value);
                return new OptionSetValueCollection(values.Select(v => new OptionSetValue((int)v)).ToList());
            };
            if (meta is DateTimeAttributeMetadata)
            {
                return DateTime.Parse(cmbValue.Text);
            }
            if (meta is BooleanAttributeMetadata boo)
            {
                // Is checking the cmbAttribute ok? I hope so...
                return ((OptionMetadataItem)cmbValue.SelectedItem).Metadata == boo.OptionSet.TrueOption;
            }
            if (meta is MoneyAttributeMetadata)
            {
                return new Money(decimal.Parse(cmbValue.Text));
            }
            if (meta is LookupAttributeMetadata)
            {
                return xrmRecordAttribute.Record.ToEntityReference();
            }
            throw new Exception($"Attribute of type {meta.AttributeTypeName.Value} is currently not supported.");
        }

        private void PopulateFromAddedAttribute()
        {
            if (lvAttributes.SelectedItems.Count == 0)
            {
                return;
            }
            if (lvAttributes.SelectedItems[0].Tag is BulkActionItem attribute)
            {
                cmbAttribute.SelectedItem = cmbAttribute.Items.Cast<AttributeMetadataItem>().FirstOrDefault(a => a.Metadata?.LogicalName == attribute?.Attribute?.Metadata?.LogicalName);
                switch (attribute.Action)
                {
                    case BulkActionAction.SetValue:
                        rbSetValue.Checked = true;
                        if (attribute.Value is OptionSetValue osv)
                        {
                            foreach (var option in cmbValue.Items.Cast<object>().Where(i => i is OptionMetadataItem).Select(i => i as OptionMetadataItem))
                            {
                                if (option.Metadata.Value == osv.Value)
                                {
                                    cmbValue.SelectedItem = option;
                                    break;
                                }
                            }
                        }
                        else if (attribute.Value is EntityReference er)
                        {
                            xrmRecordAttribute.LogicalName = er.LogicalName;
                            xrmRecordAttribute.Id = er.Id;
                        }
                        else if (attribute.Attribute.Metadata is MemoAttributeMetadata)
                        {
                            txtValueMultiline.Text = attribute.Value?.ToString();
                        }
                        else
                        {
                            cmbValue.Text = attribute.Value?.ToString();
                        }
                        break;

                    case BulkActionAction.Calc:
                        rbCalculate.Checked = true;
                        txtValueCalc.Text = attribute.Value?.ToString();
                        break;

                    case BulkActionAction.Touch:
                        rbSetTouch.Checked = true;
                        cmbValue.Text = string.Empty;
                        break;

                    case BulkActionAction.Null:
                        rbSetNull.Checked = true;
                        cmbValue.Text = string.Empty;
                        break;
                }
                chkOnlyChange.Checked = attribute.DontTouch;
            }
        }

        private bool IsValueValid()
        {
            if (rbSetValue.Checked)
            {
                if (panUpdValue.Visible && (cmbValue.DropDownStyle == ComboBoxStyle.Simple || cmbValue.SelectedItem != null))
                {
                    return !string.IsNullOrWhiteSpace(cmbValue.Text);
                }
                if (panUpdLookup.Visible && xrmRecordAttribute.Record != null)
                {
                    return true;
                }
                if (panUpdTextMulti.Visible && !string.IsNullOrWhiteSpace(txtValueMultiline.Text))
                {
                    return true;
                }
                if (panMultiChoices.Visible && chkMultiSelects.CheckedItems.Count > 0)
                {
                    return true;
                }
            }
            if (rbCalculate.Checked && IsCalcValid(txtValueCalc.Text))
            {
                return true;
            }
            return rbSetTouch.Checked || rbSetNull.Checked;
        }

        private bool IsCalcValid(string text)
        {
            return !string.IsNullOrWhiteSpace(text);
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
            lvAttributes.Items.Clear();
            job.Attributes.ForEach(a => AddBAI(a));
            if (lvAttributes.Items.Count > 0)
            {
                lvAttributes.Items[0].Selected = true;
            }
        }

        private void UpdateJobUpdate(JobUpdate job)
        {
            job.Attributes = lvAttributes.Items.Cast<ListViewItem>().Select(i => i.Tag as BulkActionItem).ToList();
        }

        private void FixLoadedBAI(JobUpdate job)
        {
            job.Attributes
                .Where(bai => bai.Attribute == null).ToList()
                .ForEach(bai => bai.SetAttribute(Service, useFriendlyNames, true));
        }
    }
}