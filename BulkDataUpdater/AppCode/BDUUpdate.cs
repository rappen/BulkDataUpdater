using Cinteros.XTB.BulkDataUpdater.AppCode;
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
            if (!int.TryParse(cmbUpdBatchSize.Text, out int batchsize))
            {
                batchsize = 1;
            }
            if (!int.TryParse(cmbUpdDelayCall.Text, out int delaytime))
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
                    var total = includedrecords.Entities.Count;
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
                    foreach (var record in includedrecords.Entities)
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
                            if (GetUpdateRecord(record, attributes) is Entity updateentity)
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

        private Entity GetUpdateRecord(Entity record, List<BulkActionItem> attributes)
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
                if (bai.Action == BulkActionAction.Touch)
                {
                    bai.Value = currentvalue;
                }
                if (!bai.DontTouch || !ValuesEqual(bai.Value, currentvalue))
                {
                    updaterecord.Attributes.Add(attribute, bai.Value);
                    if (record.Contains(attribute))
                    {
                        record[attribute] = bai.Value;
                    }
                    else
                    {
                        record.Attributes.Add(attribute, bai.Value);
                    }
                }
            }
            return updaterecord;
        }

        private object GetValue(AttributeTypeCode? type)
        {
            switch (type)
            {
                case AttributeTypeCode.String:
                case AttributeTypeCode.Memo:
                    return cmbValue.Text;

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

                // The following allows to specify an entity reference in the following form:
                // attribute_name,attribute_guid
                // eg: account,08e943f8-1ff0-41ea-89c0-5478dd465806
                // As a security check, the attribute_name MUST be in the targets
                // specified by the lookup attribute metadata targets
                case AttributeTypeCode.Lookup:
                case AttributeTypeCode.Customer:
                    {
                        // Get the attribute metadata for the lookup type:
                        var attr = (LookupAttributeMetadata)((AttributeItem)cmbAttribute.SelectedItem).Metadata;

                        // split the text: first part: attribute meta name, second part: attribute guid to update
                        var t = cmbValue.Text.Split(',', ';', '/', '\\', ':').ToArray();
                        // get the first target
                        string attrname = (attr.Targets != null && attr.Targets.Length > 0) ? attr.Targets[0] : null;
                        if (attr.Targets != null && t.Length > 1)
                            attrname = attr.Targets.FirstOrDefault(x => t.First().Equals(x, StringComparison.OrdinalIgnoreCase));

                        if (String.IsNullOrEmpty(attrname))
                            throw new Exception("Target entity: '" + t.First() + "' is null or not found");

                        return new EntityReference(attrname, new Guid(t.Last()));
                    }

                default:
                    throw new Exception("Attribute of type " + type.ToString() + " is currently not supported.");
            }
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
                if (attribute == "statuscode" && !record.Contains("statecode"))
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
            item.SubItems.Add(bai.Action == BulkActionAction.SetValue ? bai.StringValue : string.Empty);
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
            if (!(cmbAttribute.SelectedItem is AttributeItem))
            {
                MessageBox.Show("Select an attribute to update from the list.");
                return null;
            }
            var bai = new BulkActionItem
            {
                Attribute = (AttributeItem)cmbAttribute.SelectedItem,
                DontTouch = chkOnlyChange.Checked,
                Action = rbSetValue.Checked ? BulkActionAction.SetValue : rbSetNull.Checked ? BulkActionAction.Null : BulkActionAction.Touch
            };
            var logicalname = bai.Attribute.GetValue();
            bai.Value = null;
            try
            {
                bai.Value = bai.Action == BulkActionAction.SetValue ? GetValue(bai.Attribute.Metadata.AttributeType) : null;
                bai.StringValue = bai.Action == BulkActionAction.SetValue ? cmbValue.Text : string.Empty;
            }
            catch (Exception e)
            {
                MessageBox.Show("Value error:\n" + e.Message, "Set value", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            return bai;
        }
    }
}
