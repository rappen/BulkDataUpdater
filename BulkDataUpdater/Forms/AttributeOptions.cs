using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Windows.Forms;

namespace Cinteros.XTB.BulkDataUpdater.Forms
{
    public partial class AttributeOptions : Form
    {
        public AttributeOptions()
        {
            InitializeComponent();
        }

        public static void Show(UpdateAttributes settings, EntityMetadata entity)
        {
            var form = new AttributeOptions();
            form.lblTable.Text = entity.DisplayName?.UserLocalizedLabel?.Label ?? entity.LogicalName;
            form.Populate(settings);
            if (form.ShowDialog() == DialogResult.OK)
            {
                form.SetSettingsFromUI(settings);
                if (!settings.Everything &&
                    !settings.Required &&
                    !settings.Recommended &&
                    !settings.OnForm &&
                    !settings.OnView &&
                    !settings.InQuery)
                {
                    if (MessageBox.Show("Are you sure?\nNothing will be shown now...\n\nTry again?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        Show(settings, entity);
                    }
                }
            }
        }

        private void Populate(UpdateAttributes settings)
        {
            chkEverything.Checked = settings.Everything;
            chkRequired.Checked = settings.Required;
            chkRecommended.Checked = settings.Recommended;
            chkOnForm.Checked = settings.OnForm;
            chkOnView.Checked = settings.OnView;
            chkInQuery.Checked = settings.InQuery;
            rbThatAreAnd.Checked = settings.CombinationAnd;
            rbThatAreOr.Checked = !settings.CombinationAnd;
            chkUnallowedUpdate.Checked = settings.UnallowedUpdate;
            chkImportSeqNo.Checked = settings.ImportSequenceNumber;
        }

        private void SetSettingsFromUI(UpdateAttributes settings)
        {
            settings.Everything = chkEverything.Checked;
            settings.Required = chkRequired.Checked;
            settings.Recommended = chkRecommended.Checked;
            settings.OnForm = chkOnForm.Checked;
            settings.OnView = chkOnView.Checked;
            settings.InQuery = chkInQuery.Checked;
            settings.CombinationAnd = rbThatAreAnd.Checked;
            settings.UnallowedUpdate = chkUnallowedUpdate.Checked;
            settings.ImportSequenceNumber = chkImportSeqNo.Checked;
        }

        private void chkEverything_CheckedChanged(object sender, EventArgs e)
        {
            gbAttrThatAre.Enabled = !chkEverything.Checked;
        }
    }
}