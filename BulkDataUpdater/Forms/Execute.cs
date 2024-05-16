using Cinteros.XTB.BulkDataUpdater.AppCode;
using Rappen.XTB.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace Cinteros.XTB.BulkDataUpdater.Forms
{
    public partial class Execute : Form
    {
        private BulkDataUpdater bdu;
        private JobExecute jobexe;
        private string action;
        private bool confirmedbypass;
        private bool confirmedbypasspreview;
        private bool init;

        public static DialogResult Show(BulkDataUpdater bdu, JobExecute jobexe)
        {
            if (jobexe == null)
            {
                return DialogResult.Cancel;
            }
            var form = new Execute
            {
                bdu = bdu,
                jobexe = jobexe,
                action =
                    jobexe is JobUpdate ? "Update" :
                    jobexe is JobAssign ? "Assign" :
                    jobexe is JobSetState ? "SetState" :
                    jobexe is JobDelete ? "Delete" : "Execute"
            };
            form.init = true;
            form.SetExecuteOptions(jobexe);
            form.ValidateOptions();
            form.init = false;
            if (jobexe.ExecuteOptions.FormWidth > 0) form.Width = jobexe.ExecuteOptions.FormWidth;
            if (jobexe.ExecuteOptions.FormHeight > 0) form.Height = jobexe.ExecuteOptions.FormHeight;
            var result = form.ShowDialog();
            form.jobexe.ExecuteOptions = form.ExecuteOptions;
            return result;
        }

        public Execute()
        {
            InitializeComponent();
        }

        public JobExecuteOptions ExecuteOptions =>
            new JobExecuteOptions
            {
                FormHeight = Height,
                FormWidth = Width,
                DelayCallTime = int.TryParse(cmbDelayCall.Text, out var delay) ? delay : 0,
                BatchSize = int.TryParse(cmbBatchSize.Text, out int updsize) ? updsize : 1,
                MultipleRequest = rbBatchMultipleRequests.Checked,
                IgnoreErrors = chkIgnoreErrors.Checked,
                BypassCustom = chkBypassPlugins.Checked,
                BypassSync = chkBypassSync.Checked,
                BypassAsync = chkBypassAsync.Checked,
                BypassSteps = GetStepGuids()
            };

        private List<Guid> GetStepGuids()
        {
            try
            {
                return txtBypassSteps.Text.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).Distinct().ToList();
            }
            catch
            {
                return new List<Guid>();
            }
        }

        private void SetExecuteOptions(JobExecute jobexe)
        {
            var options = jobexe?.ExecuteOptions ?? new JobExecuteOptions();
            cmbDelayCall.Text = options.DelayCallTime.ToString();
            cmbBatchSize.Text = options.BatchSize.ToString();
            rbBatchMultipleRequests.Checked = options.MultipleRequest;
            rbBatchExecuteMultiple.Checked = !options.MultipleRequest;
            chkIgnoreErrors.Checked = options.IgnoreErrors;
            chkBypassPlugins.Checked = options.BypassCustom;
            chkBypassSync.Checked = options.BypassSync;
            chkBypassAsync.Checked = options.BypassAsync;
            txtBypassSteps.Text = string.Join(",\n\r", options.BypassSteps);
        }

        private void ValidateOptions()
        {
            var error = string.Empty;
            panBatchOption.Enabled = (int.TryParse(cmbBatchSize.Text, out int updsize) ? updsize : 1) > 1;
            if (!string.IsNullOrWhiteSpace(cmbDelayCall.Text) && !int.TryParse(cmbDelayCall.Text, out _))
            {
                error = "Pause seconds must be a number or empty.";
            }
            if (!string.IsNullOrWhiteSpace(cmbBatchSize.Text))
            {
                if (!int.TryParse(cmbBatchSize.Text, out var size))
                {
                    error = "Batch size must be a whole number or empty.";
                }
                if (size < 1)
                {
                    error = "Batch size must be positive whole number or empty.";
                }
                if (size > 1 && jobexe is JobDelete && rbBatchMultipleRequests.Checked)
                {
                    error = "Delete is still not supported by UpdateMultiple.";
                }
            }
            if (chkBypassPlugins.Checked)
            {
                if (bdu.currentversion < bdu.bypasspluginminversion)
                {
                    MessageBox.Show(
                        $"This feature is not available in version {bdu.ConnectionDetail?.OrganizationVersion}\n" +
                        $"Need version {bdu.bypasspluginminversion} or later.", "Bypass Custom Business Logic",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    chkBypassPlugins.Checked = false;
                }
                else if (!init && !confirmedbypass && MessageBox.Show(
                    "Make sure you know exactly what this checkbox means.\n" +
                    "Please read the docs - click the link first!\n\n" +
                    "Are you OK to continue?", "Bypass Custom Business Logic",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes)
                {
                    chkBypassPlugins.Checked = false;
                }
                confirmedbypass = true;
            }
            if (chkBypassSync.Checked || chkBypassAsync.Checked || !string.IsNullOrWhiteSpace(txtBypassSteps.Text))
            {
                if (!init && !confirmedbypasspreview && MessageBox.Show(
                    "This is a PREVIEW Microsoft feature!\n" +
                    "Make sure you know exactly what this checkbox means.\n" +
                    "Please read the docs - click the link first!\n\n" +
                    "Are you OK to continue?", "PREVIEW Bypass",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes)
                {
                    chkBypassSync.Checked = false;
                    chkBypassAsync.Checked = false;
                    txtBypassSteps.Text = string.Empty;
                }
                confirmedbypasspreview = true;
            }
            if (!string.IsNullOrWhiteSpace(txtBypassSteps.Text))
            {
                var steps = txtBypassSteps.Text.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var step in steps)
                {
                    if (!Guid.TryParse(step, out _))
                    {
                        error = "Bypassing steps must be a Guid.";
                    }
                }
            }
            if (string.IsNullOrEmpty(error))
            {
                lblInfo.Text = $"{action} will start with Execute button!";
                lblInfo.ForeColor = System.Drawing.SystemColors.ControlText;
                btnExecute.Enabled = true;
            }
            else
            {
                lblInfo.Text = error;
                lblInfo.ForeColor = System.Drawing.Color.Red;
                btnExecute.Enabled = false;
            }
        }

        private void link_Click(object sender, LinkLabelLinkClickedEventArgs e)
        {
            UrlUtils.OpenUrl(sender);
        }

        private void validate_Click(object sender, System.EventArgs e)
        {
            ValidateOptions();
        }
    }
}