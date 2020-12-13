using Cinteros.XTB.BulkDataUpdater.AppCode;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Rappen.XTB.Helpers;
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
            if (MessageBox.Show("All selected records will unconditionally be updated.\nUI defined rules will NOT be enforced.\n\nConfirm update!",
                "Confirm", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk) != DialogResult.OK)
            {
                return;
            }
            tsbCancel.Enabled = true;
            splitContainer1.Enabled = false;
            var selectedattributes = lvAttributes.Items.Cast<ListViewItem>().Select(i => i.Tag as BulkActionItem).ToList();
            var entity = records.EntityName;
            var includedrecords = GetIncludedRecords();
            working = true;
            var ignoreerrors = chkIgnoreErrors.Checked;
            if (!int.TryParse(cmbBatchSize.Text, out int batchsize))
            {
                batchsize = 1;
            }
            if (!int.TryParse(cmbDelayCall.Text, out int delaytime))
            {
                delaytime = 0;
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
                        current++;
                        var pct = 100 * current / total;
                        if (waitnow && delaytime > 0)
                        {
                            waitcur = delaytime;
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
                            LoadMissingAttributesForRecord(record, entity, attributes);
                        }
                        try
                        {
                            if (GetUpdateRecord(record, attributes, current) is Entity updateentity)
                            {
                                if (batchsize == 1)
                                {
                                    bgworker.ReportProgress(pct, $"Updating record {current} of {total}");
                                    Service.Update(updateentity);
                                    updated++;
                                    waitnow = true;
                                }
                                else
                                {
                                    batch.Requests.Add(new UpdateRequest { Target = updateentity });
                                    if (batch.Requests.Count == batchsize || current == total)
                                    {
                                        bgworker.ReportProgress(pct, $"Updating records {current - batch.Requests.Count + 1}-{current} of {total}");
                                        Service.Execute(batch);
                                        updated += batch.Requests.Count;
                                        batch.Requests.Clear();
                                        waitnow = true;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            failed++;
                            if (!chkIgnoreErrors.Checked)
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
                        MessageBox.Show(completedargs.Error.Message, "Update", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else if (completedargs.Cancelled)
                    {
                        if (MessageBox.Show("Operation cancelled!\nRun query to get records again, to verify updated values.", "Cancel", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                        {
                            RetrieveRecords(fetchXml, RetrieveRecordsReady);
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
                        newvalue = CalculateValue(record, bai, sequence);
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
            var item = lvAttributes.Items.Add(bai.Attribute.ToString());
            item.Tag = bai;
            item.SubItems.Add(bai.Action.ToString());
            item.SubItems.Add(bai.StringValue);
            item.SubItems.Add(bai.DontTouch ? "Yes" : "No");
            EnableControls(true);
        }

        private void RemoveAttribute()
        {
            var items = lvAttributes.SelectedItems;
            foreach (ListViewItem item in items)
            {
                lvAttributes.Items.Remove(item);
            }
            EnableControls(true);
        }

        private BulkActionItem GetAttributeItemFromUI()
        {
            if (!(cmbAttribute.SelectedItem is AttributeItem attribute))
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
                        bai.Value = GetValueFromUI(bai.Attribute.Metadata.AttributeType);
                        if (attribute.Metadata is MemoAttributeMetadata)
                        {
                            bai.StringValue = txtValueMultiline.Text;
                        }
                        else if (attribute.Metadata is LookupAttributeMetadata)
                        {
                            bai.StringValue = cdsLookupValue.Text;
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

        private object GetValueFromUI(AttributeTypeCode? type)
        {
            switch (type)
            {
                case AttributeTypeCode.String:
                    return cmbValue.Text;

                case AttributeTypeCode.Memo:
                    return txtValueMultiline.Text;

                case AttributeTypeCode.BigInt:
                case AttributeTypeCode.Integer:
                    return int.Parse(cmbValue.Text);

                case AttributeTypeCode.Decimal:
                    return decimal.Parse(cmbValue.Text);

                case AttributeTypeCode.Double:
                    return double.Parse(cmbValue.Text);

                case AttributeTypeCode.Picklist:
                case AttributeTypeCode.State:
                case AttributeTypeCode.Status:
                    var value = ((OptionsetItem)cmbValue.SelectedItem).meta.Value;
                    return new OptionSetValue((int)value);

                case AttributeTypeCode.DateTime:
                    return DateTime.Parse(cmbValue.Text);

                case AttributeTypeCode.Boolean:
                    {
                        // Is checking the cmbAttribute ok? I hope so...
                        var attr = (BooleanAttributeMetadata)((AttributeItem)cmbAttribute.SelectedItem).Metadata;
                        return ((OptionsetItem)cmbValue.SelectedItem).meta == attr.OptionSet.TrueOption;
                    }
                case AttributeTypeCode.Money:
                    return new Money(decimal.Parse(cmbValue.Text));

                case AttributeTypeCode.Lookup:
                case AttributeTypeCode.Customer:
                    return cdsLookupValue.EntityReference;

                default:
                    throw new Exception("Attribute of type " + type.ToString() + " is currently not supported.");
            }
        }

        private void PopulateFromAddedAttribute()
        {
            if (lvAttributes.SelectedItems.Count == 0)
            {
                return;
            }
            if (lvAttributes.SelectedItems[0].Tag is BulkActionItem attribute)
            {
                cmbAttribute.Text = attribute.Attribute.ToString();
                switch (attribute.Action)
                {
                    case BulkActionAction.SetValue:
                        rbSetValue.Checked = true;
                        if (attribute.Value is OptionSetValue osv)
                        {
                            foreach (var option in cmbValue.Items.Cast<object>().Where(i => i is OptionsetItem).Select(i => i as OptionsetItem))
                            {
                                if (option.meta.Value == osv.Value)
                                {
                                    cmbValue.SelectedItem = option;
                                    break;
                                }
                            }
                        }
                        else if (attribute.Value is EntityReference er)
                        {
                            cdsLookupValue.EntityReference = er;
                        }
                        else if (attribute.Attribute.Metadata is MemoAttributeMetadata)
                        {
                            txtValueMultiline.Text = attribute.Value.ToString();
                        }
                        else
                        {
                            cmbValue.Text = attribute.Value.ToString();
                        }
                        break;
                    case BulkActionAction.Calc:
                        rbCalculate.Checked = true;
                        txtValueCalc.Text = attribute.Value.ToString();
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
                if (panUpdLookup.Visible && cdsLookupValue.Entity != null)
                {
                    return true;
                }
                if (panUpdTextMulti.Visible && !string.IsNullOrWhiteSpace(txtValueMultiline.Text))
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

        private object CalculateValue(Entity record, BulkActionItem bai, int sequence)
        {
            var format = bai.Value.ToString();
            var value = record.Substitute(bag, format, string.Empty, true);
            value = XrmSubstituter.InjectSequence(value, sequence);
            return value;
        }
    }
}
