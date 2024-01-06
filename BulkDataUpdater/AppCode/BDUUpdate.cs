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
using System.ServiceModel;
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
            if (MessageBox.Show("All selected records will unconditionally be updated.\nUI defined rules will NOT be enforced.\n\nConfirm update!",
                "Confirm", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk) != DialogResult.OK)
            {
                return;
            }
            tsbCancel.Enabled = true;
            splitContainer1.Enabled = false;
            var selectedattributes = lvAttributes.Items.Cast<ListViewItem>().Select(i => i.Tag as BulkActionItem).ToList();
            var entityname = records.EntityName;
            var includedrecords = GetIncludedRecords();
            working = true;
            var executeoptions = GetExecuteOptions();
            if (job != null && job.Update != null)
            {
                job.Update.ExecuteOptions = executeoptions;
            }
            WorkAsync(new WorkAsyncInfo()
            {
                Message = "Updating records",
                IsCancelable = true,
                AsyncArgument = selectedattributes,
                Work = (bgworker, workargs) =>
                {
                    var sw = Stopwatch.StartNew();
                    var total = includedrecords.Count();
                    var waitnow = false;
                    var waitcur = 0;
                    var current = 0;
                    var updated = 0;
                    var failed = 0;
                    var attributes = workargs.Argument as List<BulkActionItem>;
                    var entities = new EntityCollection
                    {
                        EntityName = entityname
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
                        if (waitnow && executeoptions.DelayCallTime > 0)
                        {
                            waitcur = executeoptions.DelayCallTime;
                            while (waitcur > 0)
                            {
                                bgworker.ReportProgress(pct, $"Waiting between calls - {waitcur} sec{Environment.NewLine}(Next update {current} of {total})");
                                if (waitcur > 10)
                                {
                                    System.Threading.Thread.Sleep(10000);
                                    waitcur -= 10;
                                }
                                else
                                {
                                    System.Threading.Thread.Sleep(waitcur * 1000);
                                    waitcur = 0;
                                }
                                if (bgworker.CancellationPending)
                                {
                                    workargs.Cancel = true;
                                    break;
                                }
                            }
                            waitnow = false;
                        }
                        if (!CheckAllUpdateAttributesExistOnRecord(record, attributes))
                        {
                            //if ((bai.DontTouch || bai.Action == BulkActionAction.Touch) && !attributesexists)
                            bgworker.ReportProgress(pct, "Reloading record " + current.ToString());
                            LoadMissingAttributesForRecord(record, entityname, attributes);
                        }
                        try
                        {
                            if (GetUpdateRecord(record, attributes, current) is Entity updateentity && updateentity.Attributes.Count > 0)
                            {
                                if (executeoptions.BatchSize == 1)
                                {
                                    var request = new UpdateRequest { Target = updateentity };
                                    SetBypassPlugins(request, executeoptions.BypassCustom);
                                    bgworker.ReportProgress(pct, $"Updating record {current} of {total}");
                                    Service.Execute(request);
                                    updated++;
                                    waitnow = true;
                                }
                                else
                                {
                                    entities.Entities.Add(updateentity);
                                    if (entities.Entities.Count == executeoptions.BatchSize || current == total)
                                    {
                                        bgworker.ReportProgress(pct, $"Updating records {current - entities.Entities.Count + 1}-{current} of {total}");
                                        if (executeoptions.MultipleRquest)
                                        {
                                            var multireq = new UpdateMultipleRequest
                                            {
                                                //               ConcurrencyBehavior = ConcurrencyBehavior.AlwaysOverwrite,
                                                Targets = entities
                                            };
                                            var response = Service.Execute(multireq);
                                        }
                                        else
                                        {
                                            var batch = new ExecuteMultipleRequest
                                            {
                                                Settings = new ExecuteMultipleSettings { ContinueOnError = executeoptions.IgnoreErrors },
                                                Requests = new OrganizationRequestCollection()
                                            };
                                            foreach (var entity in entities.Entities)
                                            {
                                                var req = new UpdateRequest { Target = entity };
                                                SetBypassPlugins(req, executeoptions.BypassCustom);
                                                batch.Requests.Add(req);
                                            }
                                            Service.Execute(batch);
                                        }
                                        updated += entities.Entities.Count;
                                        entities.Entities.Clear();
                                        waitnow = true;
                                    }
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
                    workargs.Result = new Tuple<int, int, long>(updated, failed, sw.ElapsedMilliseconds);
                },
                PostWorkCallBack = (completedargs) =>
                {
                    working = false;
                    tsbCancel.Enabled = false;
                    if (completedargs.Error != null)
                    {
                        ShowErrorDialog(completedargs.Error, "Update");
                    }
                    else if (completedargs.Cancelled)
                    {
                        if (MessageBox.Show("Operation cancelled!\nRun query to get records again, to verify updated values.", "Cancel", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                        {
                            RetrieveRecords();
                        }
                    }
                    else if (completedargs.Result is Tuple<int, int, long> result)
                    {
                        lblUpdateStatus.Text = $"{result.Item1} records updated, {result.Item2} records failed.";
                        LogUse("Updated", result.Item1, result.Item3);
                        if (result.Item2 > 0)
                        {
                            LogUse("Failed", result.Item2);
                        }
                        if (MessageBox.Show("Update completed!\nRun query to show updated records?", "Bulk Data Updater", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
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

        private Entity GetUpdateRecord(Entity record, List<BulkActionItem> attributes, int sequence)
        {
            if (attributes.Count == 0)
            {
                return null;
            }
            var updaterecord = new Entity(record.LogicalName, record.Id);
            foreach (var bai in attributes.Where(a => !(a.Attribute.Metadata is StateAttributeMetadata)))
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
            if (meta is PicklistAttributeMetadata || meta is StateAttributeMetadata)
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
            cmbDelayCall.SelectedItem = cmbDelayCall.Items.Cast<string>().FirstOrDefault(i => i == job.ExecuteOptions.DelayCallTime.ToString());
            cmbBatchSize.SelectedItem = cmbBatchSize.Items.Cast<string>().FirstOrDefault(i => i == job.ExecuteOptions.BatchSize.ToString());
            chkMultipleRequest.Checked = job.ExecuteOptions.MultipleRquest;
            chkIgnoreErrors.Checked = job.ExecuteOptions.IgnoreErrors;
            chkBypassPlugins.Checked = job.ExecuteOptions.BypassCustom;
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