using Cinteros.XTB.BulkDataUpdater.AppCode;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Rappen.XRM.Helpers;
using Rappen.XRM.Helpers.Extensions;
using Rappen.XRM.Tokens;
using Rappen.XTB.Helpers;
using Rappen.XTB.Helpers.ControlItems;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using HelpFetch = Rappen.XRM.Helpers.FetchXML;

namespace Cinteros.XTB.BulkDataUpdater.Forms
{
    public partial class UpdateAttribute : Form
    {
        private BulkDataUpdater bdu;
        private Entity record;
        private UpdateAttributes updateAttributes;
        private bool friendly;
        private string lockedattribute;

        public static BulkActionItem Show(BulkDataUpdater bdu, bool friendly, UpdateAttributes updattrs, Entity selected, BulkActionItem bai)
        {
            var form = new UpdateAttribute
            {
                bdu = bdu,
                friendly = friendly,
                updateAttributes = updattrs,
                record = selected,
                lockedattribute = bai?.Attribute?.Metadata?.LogicalName
            };
            form.xrmRecordAttribute.Service = bdu.Service;
            form.xrmLookupDialog.Service = bdu.Service;
            form.RefreshAttributes();
            form.SetBAI(bai);
            form.EnableControls();
            return form.ShowDialog() == DialogResult.OK ? form.GetAttributeItemFromUI() : null;
        }

        public UpdateAttribute()
        {
            InitializeComponent();
        }

        #region Private Methods

        private BulkActionItem GetAttributeItemFromUI()
        {
            if (!(cmbAttribute.SelectedItem is AttributeMetadataItem attribute))
            {
                MessageBoxEx.Show(this, "Select an attribute to update from the list.");
                return null;
            }
            var bai = new BulkActionItem
            {
                Attribute = attribute,
                DontTouch = chkOnlyChange.Checked,
                Action =
                    rbSetValue.Checked ? BulkActionAction.Set :
                    rbCalculate.Checked ? BulkActionAction.Calc :
                    rbSetNull.Checked ? BulkActionAction.Null :
                    BulkActionAction.Touch
            };
            try
            {
                switch (bai.Action)
                {
                    case BulkActionAction.Set:
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
                MessageBoxEx.Show(this, "Value error:\n" + e.Message, "Set value", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            return bai;
        }

        private void EnableControls()
        {
            MethodInvoker mi = delegate
            {
                try
                {
                    var attribute = cmbAttribute.SelectedItem as AttributeMetadataItem;
                    panAttribute.Enabled = string.IsNullOrEmpty(lockedattribute);
                    panAction.Enabled = attribute != null;
                    panValue.Enabled = panAction.Enabled && panAction.Tag is BulkActionAction;
                    btnSave.Enabled = attribute != null && panButtons.Enabled && IsValueValid();
                }
                catch
                {
                    // Now what?
                }
            };
            if (InvokeRequired)
            {
                Invoke(mi);
            }
            else
            {
                mi();
            }
        }

        private bool IsValueValid()
        {
            if (rbSetValue.Checked)
            {
                if (panValueText.Visible && (cmbValue.DropDownStyle == ComboBoxStyle.Simple || cmbValue.SelectedItem != null))
                {
                    return !string.IsNullOrWhiteSpace(cmbValue.Text);
                }
                if (panValueLookup.Visible && xrmRecordAttribute.Record != null)
                {
                    return true;
                }
                if (panValueTextMulti.Visible && !string.IsNullOrWhiteSpace(txtValueMultiline.Text))
                {
                    return true;
                }
                if (panValueChoices.Visible && chkMultiSelects.CheckedItems.Count > 0)
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

        private void SetBAI(BulkActionItem bai)
        {
            if (bai == null)
            {
                return;
            }
            cmbAttribute.SelectedItem = cmbAttribute.Items.Cast<AttributeMetadataItem>().FirstOrDefault(a => a.Metadata?.LogicalName == bai?.Attribute?.Metadata?.LogicalName);
            SetUIFromBAIAction(bai.Action);
            switch (bai.Action)
            {
                case BulkActionAction.Set:
                    if (bai.Value is OptionSetValueCollection osvc)
                    {
                        chkMultiSelects.Items
                            .Cast<OptionMetadataItem>()
                            .Where(o => osvc.Any(v => v.Value.Equals(o.Metadata.Value)))
                            .ToList()
                            .ForEach(i => chkMultiSelects.SetItemChecked(chkMultiSelects.Items.IndexOf(i), true));
                    }
                    else if (bai.Value is OptionSetValue osv)
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
                    else if (bai.Value is EntityReference er)
                    {
                        xrmRecordAttribute.LogicalName = er.LogicalName;
                        xrmRecordAttribute.Id = er.Id;
                    }
                    else if (bai.Attribute.Metadata is MemoAttributeMetadata)
                    {
                        txtValueMultiline.Text = bai.Value?.ToString();
                    }
                    else
                    {
                        cmbValue.Text = bai.Value?.ToString();
                    }
                    break;

                case BulkActionAction.Calc:
                    txtValueCalc.Text = bai.Value?.ToString();
                    break;

                case BulkActionAction.Null:
                case BulkActionAction.Touch:
                    cmbValue.Text = string.Empty;
                    break;
            }
            chkOnlyChange.Checked = bai.DontTouch;
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
            if (meta is MultiSelectPicklistAttributeMetadata)
            {
                var values = chkMultiSelects.CheckedItems.OfType<OptionMetadataItem>().Select(o => o.Metadata.Value);
                return new OptionSetValueCollection(values.Select(v => new OptionSetValue((int)v)).ToList());
            }
            if (meta is EnumAttributeMetadata)
            {
                var value = ((OptionMetadataItem)cmbValue.SelectedItem).Metadata.Value;
                return new OptionSetValue((int)value);
            }
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

        private void SetUIFromBAIAction(BulkActionAction action)
        {
            switch (action)
            {
                case BulkActionAction.Set:
                    rbSetValue.Checked = true;
                    break;

                case BulkActionAction.Calc:
                    rbCalculate.Checked = true;
                    break;

                case BulkActionAction.Null:
                    rbSetNull.Checked = true;
                    break;

                case BulkActionAction.Touch:
                    rbSetTouch.Checked = true;
                    break;
            }
        }

        private void SetActionFromUI()
        {
            if (rbSetValue.Checked)
            {
                panAction.Tag = BulkActionAction.Set;
            }
            else if (rbCalculate.Checked)
            {
                panAction.Tag = BulkActionAction.Calc;
            }
            else if (rbSetNull.Checked)
            {
                panAction.Tag = BulkActionAction.Null;
            }
            else if (rbSetTouch.Checked)
            {
                panAction.Tag = BulkActionAction.Touch;
            }
            else
            {
                panAction.Tag = null;
            }
        }

        private void UpdateValueField()
        {
            if (!(cmbAttribute.SelectedItem is AttributeMetadataItem attribute))
            {
                return;
            }
            if (panAction.Tag == null)
            {
                rbSetValue.Checked = true;
            }
            if (!(panAction.Tag is BulkActionAction action))
            {
                return;
            }
            cmbValue.Enabled = action == BulkActionAction.Set;
            txtValueMultiline.Enabled = action == BulkActionAction.Set;
            btnLookupValue.Enabled = action == BulkActionAction.Set;
            if (action != BulkActionAction.Set && xrmRecordAttribute.Record != null)
            {
                xrmRecordAttribute.Record = null;
            }
            chkOnlyChange.Enabled = action != BulkActionAction.Touch;
            chkOnlyChange.Checked = chkOnlyChange.Checked && action != BulkActionAction.Touch;

            cmbValue.Items.Clear();
            var value = action == BulkActionAction.Set;
            var lookup = false;
            var multitext = false;
            var multisel = false;
            switch (action)
            {
                case BulkActionAction.Set:
                    if (attribute.Metadata is MultiSelectPicklistAttributeMetadata multimeta)
                    {
                        chkMultiSelects.Items.Clear();
                        var options = multimeta.OptionSet;
                        if (options != null)
                        {
                            foreach (var option in options.Options)
                            {
                                chkMultiSelects.Items.Add(new OptionMetadataItem(option, true));
                            }
                        }
                        value = false;
                        multisel = true;
                    }
                    else if (attribute.Metadata is EnumAttributeMetadata enummeta)
                    {
                        var options = enummeta.OptionSet;
                        if (options != null)
                        {
                            foreach (var option in options.Options)
                            {
                                cmbValue.Items.Add(new OptionMetadataItem(option, true));
                            }
                        }
                        cmbValue.DropDownStyle = ComboBoxStyle.DropDownList;
                    }
                    else if (attribute.Metadata is BooleanAttributeMetadata boolmeta)
                    {
                        var options = boolmeta.OptionSet;
                        if (options != null)
                        {
                            cmbValue.Items.Add(new OptionMetadataItem(options.TrueOption, true));
                            cmbValue.Items.Add(new OptionMetadataItem(options.FalseOption, true));
                        }
                        cmbValue.DropDownStyle = ComboBoxStyle.DropDownList;
                    }
                    else if (attribute.Metadata is MemoAttributeMetadata)
                    {
                        value = false;
                        multitext = true;
                    }
                    else if (attribute.Metadata is LookupAttributeMetadata lkpmeta)
                    {
                        value = false;
                        lookup = true;
                        xrmLookupDialog.LogicalNames = lkpmeta.Targets;
                        if (!xrmLookupDialog.LogicalNames.Contains(xrmRecordAttribute.LogicalName))
                        {
                            xrmRecordAttribute.Record = null;
                        }
                    }
                    else
                    {
                        cmbValue.DropDownStyle = ComboBoxStyle.Simple;
                    }
                    break;
            }
            panValueText.Visible = value;
            panValueLookup.Visible = lookup;
            panValueToken.Visible = action == BulkActionAction.Calc;
            panValueTextMulti.Visible = multitext;
            panValueChoices.Visible = multisel;
            PreviewCalc();
            EnableControls();
        }

        private void RefreshAttributes()
        {
            var selected = cmbAttribute.SelectedItem as AttributeMetadataItem;
            cmbAttribute.Items.Clear();
            if (record != null)
            {
                var entityName = record.LogicalName;
                var attributes = GetDisplayAttributes();
                attributes.ForEach(a => AttributeMetadataItem.AddAttributeToComboBox(cmbAttribute, a, true, friendly, true));
                if (selected != null)
                {
                    cmbAttribute.SelectedItem = cmbAttribute.Items.Cast<AttributeMetadataItem>().FirstOrDefault(a => a.Metadata?.LogicalName == selected.Metadata?.LogicalName);
                }
            }
            EnableControls();
        }

        private List<AttributeMetadata> GetDisplayAttributes()
        {
            bool IsRequired(AttributeMetadata meta)
            {
                return (meta.RequiredLevel?.Value == AttributeRequiredLevel.ApplicationRequired ||
                        meta.RequiredLevel?.Value == AttributeRequiredLevel.SystemRequired) &&
                       string.IsNullOrEmpty(meta.AttributeOf);
            }
            var result = new List<AttributeMetadata>();
            var fetch = HelpFetch.Fetch.FromString(bdu.job.FetchXML);
            var attributes = bdu.entitymeta?.Attributes?.ToList() ?? new List<AttributeMetadata>();
            if (!string.IsNullOrEmpty(lockedattribute) && !attributes.Any(a => a.LogicalName.Equals(lockedattribute)))
            {
                MessageBoxEx.Show(bdu, $"The attribute '{lockedattribute}' is not available for entity '{record?.LogicalName}'", "Attribute not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return result;
            }
            foreach (var attribute in attributes)
            {
                if (attribute.IsPrimaryId == true)
                {
                    continue;
                }
                var yes = updateAttributes.Everything ||
                    (updateAttributes.ImportSequenceNumber && attribute.LogicalName == "importsequencenumber") ||
                    (updateAttributes.UnallowedUpdate && !attribute.IsValidForUpdate.Value == true && attribute.LogicalName != "importsequencenumber");
                if (updateAttributes.CombinationAnd)
                {
                    yes = yes ||
                        ((!updateAttributes.Required || IsRequired(attribute)) &&
                         (!updateAttributes.Recommended || attribute.RequiredLevel?.Value == AttributeRequiredLevel.Recommended) &&
                         (!updateAttributes.InQuery || fetch.Entity?.Attributes?.Any(a => a.Name == attribute.LogicalName) == true) &&
                         (!updateAttributes.OnForm || bdu.IsOnAnyForm(fetch.Entity?.Name, attribute.LogicalName, RefreshAttributes)) &&
                         (!updateAttributes.OnView || bdu.IsOnAnyView(fetch.Entity?.Name, attribute.LogicalName, RefreshAttributes)));
                }
                else
                {
                    yes = yes ||
                        (updateAttributes.Required && IsRequired(attribute)) ||
                        (updateAttributes.Recommended && attribute.RequiredLevel?.Value == AttributeRequiredLevel.Recommended) ||
                        (updateAttributes.InQuery && fetch.Entity?.Attributes?.Any(a => a.Name == attribute.LogicalName) == true) ||
                        (updateAttributes.OnForm && bdu.IsOnAnyForm(fetch.Entity?.Name, attribute.LogicalName, RefreshAttributes)) ||
                        (updateAttributes.OnView && bdu.IsOnAnyView(fetch.Entity?.Name, attribute.LogicalName, RefreshAttributes));
                }
                if (yes || attribute.LogicalName == lockedattribute)
                {
                    result.Add(attribute);
                }
            }
            return result;
        }

        private void PreviewCalc()
        {
            tmCalc.Enabled = false;
            if (rbCalculate.Checked &&
                bdu.Service != null &&
                record != null)
            {
                try
                {
                    var attmeta = cmbAttribute.SelectedItem as AttributeMetadataItem;
                    var preview = CalculateValue(record, attmeta?.Metadata, txtValueCalc.Text, 1);
                    if (preview is EntityReference refent)
                    {
                        preview = $"{refent.LogicalName}:{refent.Id}";
                    }
                    else if (preview is OptionSetValue opt)
                    {
                        preview = opt.Value;
                    }
                    else if (preview is OptionSetValueCollection mopt)
                    {
                        preview = string.Join(";", mopt.Select(m => m.Value.ToString()));
                    }
                    else if (preview is Money money)
                    {
                        preview = money.Value;
                    }
                    txtCalcPreview.Text = preview?.ToString();
                }
                catch (Exception ex)
                {
                    txtCalcPreview.Text = $"Error: {ex.Message}";
                }
                txtCalcPreview.ForeColor = SystemColors.WindowText;
            }
            else if (!string.IsNullOrEmpty(txtCalcPreview.Text))
            {
                txtCalcPreview.Text = string.Empty;
            }
        }

        private object CalculateValue(Entity record, AttributeMetadata attribute, string format, int sequence)
        {
            if (string.IsNullOrEmpty(format))
            {
                return null;
            }
            return record.Tokens(new GenericBag(bdu.Service), format, sequence, string.Empty, true).ConvertTo(attribute);
        }

        private void AwaitCalc()
        {
            txtCalcPreview.ForeColor = SystemColors.GrayText;
            tmCalc.Stop();
            tmCalc.Start();
        }

        private void AskXRMTR()
        {
            bdu.CallingXRMTR(record, GetAttributeItemFromUI());
            DialogResult = DialogResult.OK;
        }

        internal void GetFromXRMTR(string tokens)
        {
            txtValueCalc.Text = tokens;
        }

        #endregion Private Methods

        #region Form Events

        private void genericInputChanged(object sender, EventArgs e)
        {
            EnableControls();
        }

        private void cmbAttribute_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateValueField();
        }

        private void btnUpdateAttributeOptions_Click(object sender, EventArgs e)
        {
            AttributeOptions.Show(bdu.updateAttributes, bdu.entitymeta);
            RefreshAttributes();
        }

        private void rbSet_CheckedChanged(object sender, EventArgs e)
        {
            SetActionFromUI();
            UpdateValueField();
        }

        private void btnLookupValue_Click(object sender, EventArgs e)
        {
            switch (xrmLookupDialog.ShowDialog(this))
            {
                case DialogResult.OK:
                    xrmRecordAttribute.Record = xrmLookupDialog.Record;
                    break;

                case DialogResult.Abort:
                    xrmRecordAttribute.Record = null;
                    break;
            }
            EnableControls();
        }

        private void txtValueCalc_TextChanged(object sender, EventArgs e)
        {
            AwaitCalc();
            EnableControls();
        }

        private void tmCalc_Tick(object sender, EventArgs e)
        {
            PreviewCalc();
        }

        private void btnCalcHelp_Click(object sender, EventArgs e)
        {
            UrlUtils.OpenUrl("https://jonasr.app/bdu/#calc", bdu.ConnectionDetail);
        }

        private void btnXRMTR_Click(object sender, EventArgs e)
        {
            AskXRMTR();
        }

        #endregion Form Events
    }
}