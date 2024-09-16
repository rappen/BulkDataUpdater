namespace Cinteros.XTB.BulkDataUpdater.Forms
{
    partial class Execute
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Execute));
            this.panBatchOption = new System.Windows.Forms.Panel();
            this.rbBatchExecuteMultiple = new System.Windows.Forms.RadioButton();
            this.rbBatchMultipleRequests = new System.Windows.Forms.RadioButton();
            this.linkBulkOperations = new System.Windows.Forms.LinkLabel();
            this.linkBypassPlugins = new System.Windows.Forms.LinkLabel();
            this.chkBypassPlugins = new System.Windows.Forms.CheckBox();
            this.gbWaitBetween = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.cmbDelayCall = new System.Windows.Forms.ComboBox();
            this.cmbBatchSize = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.chkIgnoreErrors = new System.Windows.Forms.CheckBox();
            this.btnExecute = new System.Windows.Forms.Button();
            this.gbBatch = new System.Windows.Forms.GroupBox();
            this.gbBypass = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.linkLabel3 = new System.Windows.Forms.LinkLabel();
            this.gbBypassPreview = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.linkLabel2 = new System.Windows.Forms.LinkLabel();
            this.chkBypassSync = new System.Windows.Forms.CheckBox();
            this.txtBypassSteps = new System.Windows.Forms.TextBox();
            this.chkBypassAsync = new System.Windows.Forms.CheckBox();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.gbErrors = new System.Windows.Forms.GroupBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.panButtons = new System.Windows.Forms.Panel();
            this.lblInfo = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.chkDontAskNext = new System.Windows.Forms.CheckBox();
            this.panBatchOption.SuspendLayout();
            this.gbWaitBetween.SuspendLayout();
            this.gbBatch.SuspendLayout();
            this.gbBypass.SuspendLayout();
            this.gbBypassPreview.SuspendLayout();
            this.gbErrors.SuspendLayout();
            this.panButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // panBatchOption
            // 
            this.panBatchOption.Controls.Add(this.rbBatchExecuteMultiple);
            this.panBatchOption.Controls.Add(this.rbBatchMultipleRequests);
            this.panBatchOption.Location = new System.Drawing.Point(62, 46);
            this.panBatchOption.Name = "panBatchOption";
            this.panBatchOption.Size = new System.Drawing.Size(333, 19);
            this.panBatchOption.TabIndex = 114;
            // 
            // rbBatchExecuteMultiple
            // 
            this.rbBatchExecuteMultiple.AutoSize = true;
            this.rbBatchExecuteMultiple.Location = new System.Drawing.Point(127, 1);
            this.rbBatchExecuteMultiple.Name = "rbBatchExecuteMultiple";
            this.rbBatchExecuteMultiple.Size = new System.Drawing.Size(163, 17);
            this.rbBatchExecuteMultiple.TabIndex = 1;
            this.rbBatchExecuteMultiple.Text = "ExecuteMultiple (deprecated)";
            this.rbBatchExecuteMultiple.UseVisualStyleBackColor = true;
            this.rbBatchExecuteMultiple.CheckedChanged += new System.EventHandler(this.validate_Click);
            // 
            // rbBatchMultipleRequests
            // 
            this.rbBatchMultipleRequests.AutoSize = true;
            this.rbBatchMultipleRequests.Checked = true;
            this.rbBatchMultipleRequests.Location = new System.Drawing.Point(19, 1);
            this.rbBatchMultipleRequests.Name = "rbBatchMultipleRequests";
            this.rbBatchMultipleRequests.Size = new System.Drawing.Size(96, 17);
            this.rbBatchMultipleRequests.TabIndex = 0;
            this.rbBatchMultipleRequests.TabStop = true;
            this.rbBatchMultipleRequests.Text = "UpdateMultiple";
            this.rbBatchMultipleRequests.UseVisualStyleBackColor = true;
            this.rbBatchMultipleRequests.CheckedChanged += new System.EventHandler(this.validate_Click);
            // 
            // linkBulkOperations
            // 
            this.linkBulkOperations.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.linkBulkOperations.AutoSize = true;
            this.linkBulkOperations.Location = new System.Drawing.Point(239, 22);
            this.linkBulkOperations.Name = "linkBulkOperations";
            this.linkBulkOperations.Size = new System.Drawing.Size(134, 13);
            this.linkBulkOperations.TabIndex = 113;
            this.linkBulkOperations.TabStop = true;
            this.linkBulkOperations.Tag = "https://learn.microsoft.com/en-us/power-apps/developer/data-platform/bulk-operati" +
    "ons";
            this.linkBulkOperations.Text = "MS Learn: Bulk Operations";
            this.toolTip1.SetToolTip(this.linkBulkOperations, "https://learn.microsoft.com/power-apps/developer/data-platform/bulk-operations");
            this.linkBulkOperations.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.link_Click);
            // 
            // linkBypassPlugins
            // 
            this.linkBypassPlugins.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.linkBypassPlugins.AutoSize = true;
            this.linkBypassPlugins.Location = new System.Drawing.Point(242, 22);
            this.linkBypassPlugins.Name = "linkBypassPlugins";
            this.linkBypassPlugins.Size = new System.Drawing.Size(131, 13);
            this.linkBypassPlugins.TabIndex = 112;
            this.linkBypassPlugins.TabStop = true;
            this.linkBypassPlugins.Tag = "https://learn.microsoft.com/power-apps/developer/data-platform/bypass-custom-busi" +
    "ness-logic";
            this.linkBypassPlugins.Text = "MS Learn: Bypass Custom";
            this.toolTip1.SetToolTip(this.linkBypassPlugins, "https://learn.microsoft.com/power-apps/developer/data-platform/bypass-custom-busi" +
        "ness-logic");
            this.linkBypassPlugins.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.link_Click);
            // 
            // chkBypassPlugins
            // 
            this.chkBypassPlugins.AutoSize = true;
            this.chkBypassPlugins.Location = new System.Drawing.Point(81, 21);
            this.chkBypassPlugins.Name = "chkBypassPlugins";
            this.chkBypassPlugins.Size = new System.Drawing.Size(135, 17);
            this.chkBypassPlugins.TabIndex = 111;
            this.chkBypassPlugins.Text = "Custom Business Logic";
            this.chkBypassPlugins.UseVisualStyleBackColor = true;
            this.chkBypassPlugins.CheckedChanged += new System.EventHandler(this.validate_Click);
            // 
            // gbWaitBetween
            // 
            this.gbWaitBetween.Controls.Add(this.label5);
            this.gbWaitBetween.Controls.Add(this.cmbDelayCall);
            this.gbWaitBetween.Dock = System.Windows.Forms.DockStyle.Top;
            this.gbWaitBetween.Location = new System.Drawing.Point(6, 116);
            this.gbWaitBetween.Name = "gbWaitBetween";
            this.gbWaitBetween.Size = new System.Drawing.Size(379, 48);
            this.gbWaitBetween.TabIndex = 3;
            this.gbWaitBetween.TabStop = false;
            this.gbWaitBetween.Text = "Pause between calls";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 22);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(49, 13);
            this.label5.TabIndex = 101;
            this.label5.Text = "Seconds";
            // 
            // cmbDelayCall
            // 
            this.cmbDelayCall.FormattingEnabled = true;
            this.cmbDelayCall.Items.AddRange(new object[] {
            "0",
            "5",
            "10",
            "30",
            "60",
            "120",
            "300"});
            this.cmbDelayCall.Location = new System.Drawing.Point(81, 19);
            this.cmbDelayCall.Name = "cmbDelayCall";
            this.cmbDelayCall.Size = new System.Drawing.Size(85, 21);
            this.cmbDelayCall.TabIndex = 1;
            this.cmbDelayCall.SelectedIndexChanged += new System.EventHandler(this.validate_Click);
            this.cmbDelayCall.TextUpdate += new System.EventHandler(this.validate_Click);
            // 
            // cmbBatchSize
            // 
            this.cmbBatchSize.FormattingEnabled = true;
            this.cmbBatchSize.Items.AddRange(new object[] {
            "1",
            "5",
            "10",
            "50",
            "100",
            "200",
            "500",
            "1000"});
            this.cmbBatchSize.Location = new System.Drawing.Point(81, 19);
            this.cmbBatchSize.Name = "cmbBatchSize";
            this.cmbBatchSize.Size = new System.Drawing.Size(85, 21);
            this.cmbBatchSize.TabIndex = 107;
            this.cmbBatchSize.SelectedIndexChanged += new System.EventHandler(this.validate_Click);
            this.cmbBatchSize.TextUpdate += new System.EventHandler(this.validate_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 22);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(27, 13);
            this.label3.TabIndex = 109;
            this.label3.Text = "Size";
            // 
            // chkIgnoreErrors
            // 
            this.chkIgnoreErrors.AutoSize = true;
            this.chkIgnoreErrors.Location = new System.Drawing.Point(81, 15);
            this.chkIgnoreErrors.Name = "chkIgnoreErrors";
            this.chkIgnoreErrors.Size = new System.Drawing.Size(85, 17);
            this.chkIgnoreErrors.TabIndex = 108;
            this.chkIgnoreErrors.Text = "Ignore errors";
            this.chkIgnoreErrors.UseVisualStyleBackColor = true;
            // 
            // btnExecute
            // 
            this.btnExecute.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExecute.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnExecute.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExecute.Image = ((System.Drawing.Image)(resources.GetObject("btnExecute.Image")));
            this.btnExecute.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnExecute.Location = new System.Drawing.Point(245, 22);
            this.btnExecute.Name = "btnExecute";
            this.btnExecute.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnExecute.Size = new System.Drawing.Size(128, 38);
            this.btnExecute.TabIndex = 115;
            this.btnExecute.Text = "Execute!";
            this.btnExecute.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.toolTip1.SetToolTip(this.btnExecute, "This will execute the action.\r\nThere is NOT undo.");
            this.btnExecute.UseVisualStyleBackColor = true;
            // 
            // gbBatch
            // 
            this.gbBatch.Controls.Add(this.cmbBatchSize);
            this.gbBatch.Controls.Add(this.linkBulkOperations);
            this.gbBatch.Controls.Add(this.panBatchOption);
            this.gbBatch.Controls.Add(this.label3);
            this.gbBatch.Dock = System.Windows.Forms.DockStyle.Top;
            this.gbBatch.Location = new System.Drawing.Point(6, 6);
            this.gbBatch.Name = "gbBatch";
            this.gbBatch.Size = new System.Drawing.Size(379, 70);
            this.gbBatch.TabIndex = 1;
            this.gbBatch.TabStop = false;
            this.gbBatch.Text = "Batch records";
            // 
            // gbBypass
            // 
            this.gbBypass.Controls.Add(this.label4);
            this.gbBypass.Controls.Add(this.chkBypassPlugins);
            this.gbBypass.Controls.Add(this.linkBypassPlugins);
            this.gbBypass.Dock = System.Windows.Forms.DockStyle.Top;
            this.gbBypass.Location = new System.Drawing.Point(6, 164);
            this.gbBypass.Name = "gbBypass";
            this.gbBypass.Size = new System.Drawing.Size(379, 48);
            this.gbBypass.TabIndex = 4;
            this.gbBypass.TabStop = false;
            this.gbBypass.Text = "Bypass Logic";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 22);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(41, 13);
            this.label4.TabIndex = 115;
            this.label4.Text = "Bypass";
            // 
            // linkLabel3
            // 
            this.linkLabel3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.linkLabel3.AutoSize = true;
            this.linkLabel3.Location = new System.Drawing.Point(237, 10);
            this.linkLabel3.Name = "linkLabel3";
            this.linkLabel3.Size = new System.Drawing.Size(136, 13);
            this.linkLabel3.TabIndex = 1;
            this.linkLabel3.TabStop = true;
            this.linkLabel3.Tag = "https://learn.microsoft.com/en-us/power-apps/developer/data-platform/bypass-custo" +
    "m-business-logic-preview";
            this.linkLabel3.Text = "MS Learn: Preview Options";
            this.toolTip1.SetToolTip(this.linkLabel3, "https://learn.microsoft.com/en-us/power-apps/developer/data-platform/bypass-custo" +
        "m-business-logic-preview");
            this.linkLabel3.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.link_Click);
            // 
            // gbBypassPreview
            // 
            this.gbBypassPreview.Controls.Add(this.label1);
            this.gbBypassPreview.Controls.Add(this.label2);
            this.gbBypassPreview.Controls.Add(this.linkLabel3);
            this.gbBypassPreview.Controls.Add(this.linkLabel2);
            this.gbBypassPreview.Controls.Add(this.chkBypassSync);
            this.gbBypassPreview.Controls.Add(this.txtBypassSteps);
            this.gbBypassPreview.Controls.Add(this.chkBypassAsync);
            this.gbBypassPreview.Controls.Add(this.linkLabel1);
            this.gbBypassPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbBypassPreview.Location = new System.Drawing.Point(6, 212);
            this.gbBypassPreview.Name = "gbBypassPreview";
            this.gbBypassPreview.Size = new System.Drawing.Size(379, 77);
            this.gbBypassPreview.TabIndex = 5;
            this.gbBypassPreview.TabStop = false;
            this.gbBypassPreview.Text = "Preview: Bypass Logic";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 33);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(39, 13);
            this.label1.TabIndex = 117;
            this.label1.Text = "Modes";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 55);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(78, 13);
            this.label2.TabIndex = 118;
            this.label2.Text = "Plugin Step Ids";
            // 
            // linkLabel2
            // 
            this.linkLabel2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.linkLabel2.AutoSize = true;
            this.linkLabel2.Location = new System.Drawing.Point(246, 55);
            this.linkLabel2.Name = "linkLabel2";
            this.linkLabel2.Size = new System.Drawing.Size(127, 13);
            this.linkLabel2.TabIndex = 5;
            this.linkLabel2.TabStop = true;
            this.linkLabel2.Tag = "https://learn.microsoft.com/power-apps/developer/data-platform/bypass-custom-busi" +
    "ness-logic-preview#bypassbusinesslogicexecutionstepids";
            this.linkLabel2.Text = "MS Learn: Preview Steps";
            this.toolTip1.SetToolTip(this.linkLabel2, "https://learn.microsoft.com/power-apps/developer/data-platform/bypass-custom-busi" +
        "ness-logic-preview#bypassbusinesslogicexecutionstepids");
            this.linkLabel2.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.link_Click);
            // 
            // chkBypassSync
            // 
            this.chkBypassSync.AutoSize = true;
            this.chkBypassSync.Location = new System.Drawing.Point(81, 32);
            this.chkBypassSync.Name = "chkBypassSync";
            this.chkBypassSync.Size = new System.Drawing.Size(50, 17);
            this.chkBypassSync.TabIndex = 2;
            this.chkBypassSync.Text = "Sync";
            this.chkBypassSync.UseVisualStyleBackColor = true;
            this.chkBypassSync.CheckedChanged += new System.EventHandler(this.validate_Click);
            // 
            // txtBypassSteps
            // 
            this.txtBypassSteps.AcceptsReturn = true;
            this.txtBypassSteps.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtBypassSteps.Location = new System.Drawing.Point(15, 71);
            this.txtBypassSteps.Multiline = true;
            this.txtBypassSteps.Name = "txtBypassSteps";
            this.txtBypassSteps.Size = new System.Drawing.Size(358, 0);
            this.txtBypassSteps.TabIndex = 6;
            this.toolTip1.SetToolTip(this.txtBypassSteps, "Add Guids separated by comma.\r\nGuids are found on Plugin Steps in Plugin Registra" +
        "ting Tool.");
            this.txtBypassSteps.TextChanged += new System.EventHandler(this.validate_Click);
            // 
            // chkBypassAsync
            // 
            this.chkBypassAsync.AutoSize = true;
            this.chkBypassAsync.Location = new System.Drawing.Point(137, 32);
            this.chkBypassAsync.Name = "chkBypassAsync";
            this.chkBypassAsync.Size = new System.Drawing.Size(55, 17);
            this.chkBypassAsync.TabIndex = 3;
            this.chkBypassAsync.Text = "Async";
            this.chkBypassAsync.UseVisualStyleBackColor = true;
            this.chkBypassAsync.CheckedChanged += new System.EventHandler(this.validate_Click);
            // 
            // linkLabel1
            // 
            this.linkLabel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(241, 33);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(132, 13);
            this.linkLabel1.TabIndex = 4;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Tag = "https://learn.microsoft.com/power-apps/developer/data-platform/bypass-custom-busi" +
    "ness-logic-preview#bypassbusinesslogicexecution";
            this.linkLabel1.Text = "MS Learn: Preview Modes";
            this.toolTip1.SetToolTip(this.linkLabel1, "https://learn.microsoft.com/power-apps/developer/data-platform/bypass-custom-busi" +
        "ness-logic-preview#bypassbusinesslogicexecution");
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.link_Click);
            // 
            // gbErrors
            // 
            this.gbErrors.Controls.Add(this.chkIgnoreErrors);
            this.gbErrors.Dock = System.Windows.Forms.DockStyle.Top;
            this.gbErrors.Location = new System.Drawing.Point(6, 76);
            this.gbErrors.Name = "gbErrors";
            this.gbErrors.Size = new System.Drawing.Size(379, 40);
            this.gbErrors.TabIndex = 2;
            this.gbErrors.TabStop = false;
            this.gbErrors.Text = "Errors";
            // 
            // panButtons
            // 
            this.panButtons.Controls.Add(this.chkDontAskNext);
            this.panButtons.Controls.Add(this.lblInfo);
            this.panButtons.Controls.Add(this.btnCancel);
            this.panButtons.Controls.Add(this.btnExecute);
            this.panButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panButtons.Location = new System.Drawing.Point(6, 289);
            this.panButtons.Name = "panButtons";
            this.panButtons.Size = new System.Drawing.Size(379, 78);
            this.panButtons.TabIndex = 9;
            // 
            // lblInfo
            // 
            this.lblInfo.AutoSize = true;
            this.lblInfo.Location = new System.Drawing.Point(12, 60);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(95, 13);
            this.lblInfo.TabIndex = 117;
            this.lblInfo.Text = "Execute will start...";
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(15, 30);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 116;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // chkDontAskNext
            // 
            this.chkDontAskNext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkDontAskNext.AutoSize = true;
            this.chkDontAskNext.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.chkDontAskNext.Location = new System.Drawing.Point(123, 3);
            this.chkDontAskNext.Name = "chkDontAskNext";
            this.chkDontAskNext.Size = new System.Drawing.Size(250, 17);
            this.chkDontAskNext.TabIndex = 118;
            this.chkDontAskNext.Text = "Don\'t ask for these options next time to execute";
            this.chkDontAskNext.UseVisualStyleBackColor = true;
            // 
            // Execute
            // 
            this.AcceptButton = this.btnExecute;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(391, 373);
            this.Controls.Add(this.gbBypassPreview);
            this.Controls.Add(this.panButtons);
            this.Controls.Add(this.gbBypass);
            this.Controls.Add(this.gbWaitBetween);
            this.Controls.Add(this.gbErrors);
            this.Controls.Add(this.gbBatch);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(380, 412);
            this.Name = "Execute";
            this.Padding = new System.Windows.Forms.Padding(6);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Bulk Data Updater - Execute";
            this.panBatchOption.ResumeLayout(false);
            this.panBatchOption.PerformLayout();
            this.gbWaitBetween.ResumeLayout(false);
            this.gbWaitBetween.PerformLayout();
            this.gbBatch.ResumeLayout(false);
            this.gbBatch.PerformLayout();
            this.gbBypass.ResumeLayout(false);
            this.gbBypass.PerformLayout();
            this.gbBypassPreview.ResumeLayout(false);
            this.gbBypassPreview.PerformLayout();
            this.gbErrors.ResumeLayout(false);
            this.gbErrors.PerformLayout();
            this.panButtons.ResumeLayout(false);
            this.panButtons.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panBatchOption;
        private System.Windows.Forms.RadioButton rbBatchExecuteMultiple;
        private System.Windows.Forms.RadioButton rbBatchMultipleRequests;
        private System.Windows.Forms.LinkLabel linkBulkOperations;
        private System.Windows.Forms.LinkLabel linkBypassPlugins;
        private System.Windows.Forms.CheckBox chkBypassPlugins;
        private System.Windows.Forms.GroupBox gbWaitBetween;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cmbDelayCall;
        private System.Windows.Forms.ComboBox cmbBatchSize;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox chkIgnoreErrors;
        private System.Windows.Forms.Button btnExecute;
        private System.Windows.Forms.GroupBox gbBatch;
        private System.Windows.Forms.GroupBox gbBypass;
        private System.Windows.Forms.GroupBox gbErrors;
        private System.Windows.Forms.CheckBox chkBypassSync;
        private System.Windows.Forms.CheckBox chkBypassAsync;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.Panel panButtons;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtBypassSteps;
        private System.Windows.Forms.LinkLabel linkLabel2;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.GroupBox gbBypassPreview;
        private System.Windows.Forms.LinkLabel linkLabel3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox chkDontAskNext;
    }
}