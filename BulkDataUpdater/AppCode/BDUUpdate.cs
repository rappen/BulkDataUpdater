using Cinteros.XTB.BulkDataUpdater.AppCode;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Rappen.XRM.Helpers;
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
            var bypassplugins = chkBypassPlugins.Checked;
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
                            if (GetUpdateRecord(record, attributes, current) is Entity updateentity && updateentity.Attributes.Count > 0)
                            {
                                var request = new UpdateRequest { Target = updateentity };
                                request.Parameters[bypasspluginsparam] = bypassplugins;
                                if (batchsize == 1)
                                {
                                    bgworker.ReportProgress(pct, $"Updating record {current} of {total}");
                                    Service.Execute(request);
                                    updated++;
                                    waitnow = true;
                                }
                                else
                                {
                                    batch.Requests.Add(request);
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
                        ShowErrorDialog(completedargs.Error, "Update");
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
                cmbAttribute.Text = attribute.Attribute.ToString();
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
            var substituted = record.Substitute(new GenericBag(Service), format, sequence, string.Empty, true);
            if (string.IsNullOrEmpty(substituted))
            {
                return null;
            }
            switch (attribute?.AttributeType)
            {
                case AttributeTypeCode.Boolean:
                    if (bool.TryParse(substituted, out bool boolvalue))
                    {
                        return boolvalue;
                    }
                    if (substituted == "1" || substituted == "0")
                    {
                        return substituted == "1";
                    }
                    throw new Exception("Not valid format [True|False] or [1|0]");
                case AttributeTypeCode.Customer:
                case AttributeTypeCode.Lookup:
                    var entity = string.Empty;
                    var id = Guid.Empty;
                    if (Guid.TryParse(substituted, out id))
                    {
                        var lookupmeta = attribute as LookupAttributeMetadata;
                        if (lookupmeta?.Targets?.Length == 1)
                        {
                            entity = lookupmeta.Targets[0];
                        }
                    }
                    else if (substituted.Contains(":"))
                    {
                        if (Guid.TryParse(substituted.Split(':')[1], out id))
                        {
                            entity = substituted.Split(':')[0];
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(entity) && !id.Equals(Guid.Empty))
                    {
                        return new EntityReference(entity, id);
                    }
                    throw new Exception("Not valid format [Entity:]Guid");
                case AttributeTypeCode.DateTime:
                    if (DateTime.TryParse(substituted, out DateTime datetime))
                    {
                        return datetime;
                    }
                    throw new Exception("Not valid format Date[Time]");
                case AttributeTypeCode.Decimal:
                    if (decimal.TryParse(substituted, out decimal decimalvalue))
                    {
                        return decimalvalue;
                    }
                    break;
                case AttributeTypeCode.Double:
                    if (double.TryParse(substituted, out double doublevalue))
                    {
                        return doublevalue;
                    }
                    break;
                case AttributeTypeCode.Integer:
                case AttributeTypeCode.BigInt:
                    if (int.TryParse(substituted, out int intvalue))
                    {
                        return intvalue;
                    }
                    break;
                case AttributeTypeCode.Money:
                    if (decimal.TryParse(substituted, out decimal moneyvalue))
                    {
                        return new Money(moneyvalue);
                    }
                    break;
                case AttributeTypeCode.Picklist:
                    if (int.TryParse(substituted, out int pickvalue))
                    {
                        return new OptionSetValue(pickvalue);
                    }
                    break;
                case AttributeTypeCode.String:
                case AttributeTypeCode.Memo:
                    return substituted;
                case AttributeTypeCode.Uniqueidentifier:
                    if (Guid.TryParse(substituted, out Guid guidvalue))
                    {
                        return guidvalue;
                    }
                    break;
                case AttributeTypeCode.Owner:
                case AttributeTypeCode.PartyList:
                case AttributeTypeCode.State:
                case AttributeTypeCode.Status:
                case AttributeTypeCode.CalendarRules:
                case AttributeTypeCode.EntityName:
                case AttributeTypeCode.ManagedProperty:
                case AttributeTypeCode.Virtual:
                    throw new Exception($"Not supporting {attribute.AttributeTypeName.Value.Replace("Type", "")}");

            }
            throw new Exception($"Not valid {attribute.AttributeTypeName.Value.Replace("Type", "")}");
        }
    }
}
