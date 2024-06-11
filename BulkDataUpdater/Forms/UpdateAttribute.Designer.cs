namespace Cinteros.XTB.BulkDataUpdater.Forms
{
    partial class UpdateAttribute
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UpdateAttribute));
            this.panRoot = new System.Windows.Forms.Panel();
            this.panValue = new System.Windows.Forms.GroupBox();
            this.panValueToken = new System.Windows.Forms.Panel();
            this.btnXRMTR = new System.Windows.Forms.Button();
            this.btnCalcHelp = new System.Windows.Forms.Button();
            this.txtValueCalc = new System.Windows.Forms.TextBox();
            this.txtCalcPreview = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.panValueTextMulti = new System.Windows.Forms.Panel();
            this.txtValueMultiline = new System.Windows.Forms.TextBox();
            this.panValueChoices = new System.Windows.Forms.Panel();
            this.chkMultiSelects = new System.Windows.Forms.CheckedListBox();
            this.panValueLookup = new System.Windows.Forms.Panel();
            this.btnLookupValue = new System.Windows.Forms.Button();
            this.panValueText = new System.Windows.Forms.Panel();
            this.cmbValue = new System.Windows.Forms.ComboBox();
            this.panAction = new System.Windows.Forms.GroupBox();
            this.rbCalculate = new System.Windows.Forms.RadioButton();
            this.rbSetValue = new System.Windows.Forms.RadioButton();
            this.rbSetNull = new System.Windows.Forms.RadioButton();
            this.rbSetTouch = new System.Windows.Forms.RadioButton();
            this.panAttribute = new System.Windows.Forms.GroupBox();
            this.btnUpdateAttributeOptions = new System.Windows.Forms.Button();
            this.cmbAttribute = new System.Windows.Forms.ComboBox();
            this.panOptions = new System.Windows.Forms.GroupBox();
            this.chkOnlyChange = new System.Windows.Forms.CheckBox();
            this.panButtons = new System.Windows.Forms.Panel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.tmCalc = new System.Windows.Forms.Timer(this.components);
            this.cdsLookupValue = new Rappen.XTB.Helpers.Controls.XRMColumnText();
            this.xrmRecordAttribute = new Rappen.XTB.Helpers.Controls.XRMRecordHost();
            this.xrmLookupDialog = new Rappen.XTB.Helpers.Controls.XRMLookupDialog();
            this.panRoot.SuspendLayout();
            this.panValue.SuspendLayout();
            this.panValueToken.SuspendLayout();
            this.panValueTextMulti.SuspendLayout();
            this.panValueChoices.SuspendLayout();
            this.panValueLookup.SuspendLayout();
            this.panValueText.SuspendLayout();
            this.panAction.SuspendLayout();
            this.panAttribute.SuspendLayout();
            this.panOptions.SuspendLayout();
            this.panButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // panRoot
            // 
            this.panRoot.Controls.Add(this.panValue);
            this.panRoot.Controls.Add(this.panAction);
            this.panRoot.Controls.Add(this.panAttribute);
            this.panRoot.Controls.Add(this.panOptions);
            this.panRoot.Controls.Add(this.panButtons);
            this.panRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panRoot.Location = new System.Drawing.Point(6, 6);
            this.panRoot.Name = "panRoot";
            this.panRoot.Size = new System.Drawing.Size(383, 378);
            this.panRoot.TabIndex = 36;
            // 
            // panValue
            // 
            this.panValue.Controls.Add(this.panValueToken);
            this.panValue.Controls.Add(this.panValueTextMulti);
            this.panValue.Controls.Add(this.panValueChoices);
            this.panValue.Controls.Add(this.panValueLookup);
            this.panValue.Controls.Add(this.panValueText);
            this.panValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panValue.Location = new System.Drawing.Point(0, 96);
            this.panValue.Name = "panValue";
            this.panValue.Size = new System.Drawing.Size(383, 194);
            this.panValue.TabIndex = 3;
            this.panValue.TabStop = false;
            this.panValue.Text = "Value";
            // 
            // panValueToken
            // 
            this.panValueToken.BackColor = System.Drawing.Color.Transparent;
            this.panValueToken.Controls.Add(this.btnXRMTR);
            this.panValueToken.Controls.Add(this.btnCalcHelp);
            this.panValueToken.Controls.Add(this.txtValueCalc);
            this.panValueToken.Controls.Add(this.txtCalcPreview);
            this.panValueToken.Controls.Add(this.label2);
            this.panValueToken.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panValueToken.Location = new System.Drawing.Point(3, 96);
            this.panValueToken.MinimumSize = new System.Drawing.Size(0, 90);
            this.panValueToken.Name = "panValueToken";
            this.panValueToken.Size = new System.Drawing.Size(377, 95);
            this.panValueToken.TabIndex = 11;
            this.panValueToken.Visible = false;
            // 
            // btnXRMTR
            // 
            this.btnXRMTR.Image = ((System.Drawing.Image)(resources.GetObject("btnXRMTR.Image")));
            this.btnXRMTR.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnXRMTR.Location = new System.Drawing.Point(12, 6);
            this.btnXRMTR.Name = "btnXRMTR";
            this.btnXRMTR.Padding = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.btnXRMTR.Size = new System.Drawing.Size(200, 26);
            this.btnXRMTR.TabIndex = 1;
            this.btnXRMTR.Text = "Build with XRM Tokens Runner";
            this.btnXRMTR.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnXRMTR.UseVisualStyleBackColor = true;
            this.btnXRMTR.Click += new System.EventHandler(this.btnXRMTR_Click);
            // 
            // btnCalcHelp
            // 
            this.btnCalcHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCalcHelp.Image = ((System.Drawing.Image)(resources.GetObject("btnCalcHelp.Image")));
            this.btnCalcHelp.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCalcHelp.Location = new System.Drawing.Point(266, 6);
            this.btnCalcHelp.Name = "btnCalcHelp";
            this.btnCalcHelp.Size = new System.Drawing.Size(96, 26);
            this.btnCalcHelp.TabIndex = 3;
            this.btnCalcHelp.Text = "What is this?";
            this.btnCalcHelp.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnCalcHelp.UseVisualStyleBackColor = true;
            this.btnCalcHelp.Click += new System.EventHandler(this.btnCalcHelp_Click);
            // 
            // txtValueCalc
            // 
            this.txtValueCalc.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtValueCalc.Location = new System.Drawing.Point(12, 38);
            this.txtValueCalc.Multiline = true;
            this.txtValueCalc.Name = "txtValueCalc";
            this.txtValueCalc.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtValueCalc.Size = new System.Drawing.Size(350, 23);
            this.txtValueCalc.TabIndex = 2;
            this.txtValueCalc.TextChanged += new System.EventHandler(this.txtValueCalc_TextChanged);
            // 
            // txtCalcPreview
            // 
            this.txtCalcPreview.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCalcPreview.BackColor = System.Drawing.SystemColors.Window;
            this.txtCalcPreview.Location = new System.Drawing.Point(62, 68);
            this.txtCalcPreview.Name = "txtCalcPreview";
            this.txtCalcPreview.ReadOnly = true;
            this.txtCalcPreview.Size = new System.Drawing.Size(300, 20);
            this.txtCalcPreview.TabIndex = 4;
            this.txtCalcPreview.TabStop = false;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 71);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(45, 13);
            this.label2.TabIndex = 43;
            this.label2.Text = "Preview";
            // 
            // panValueTextMulti
            // 
            this.panValueTextMulti.Controls.Add(this.txtValueMultiline);
            this.panValueTextMulti.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panValueTextMulti.Location = new System.Drawing.Point(3, 96);
            this.panValueTextMulti.Name = "panValueTextMulti";
            this.panValueTextMulti.Size = new System.Drawing.Size(377, 95);
            this.panValueTextMulti.TabIndex = 5;
            this.panValueTextMulti.Visible = false;
            // 
            // txtValueMultiline
            // 
            this.txtValueMultiline.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtValueMultiline.Location = new System.Drawing.Point(12, 8);
            this.txtValueMultiline.Multiline = true;
            this.txtValueMultiline.Name = "txtValueMultiline";
            this.txtValueMultiline.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtValueMultiline.Size = new System.Drawing.Size(350, 81);
            this.txtValueMultiline.TabIndex = 36;
            this.txtValueMultiline.TextChanged += new System.EventHandler(this.genericInputChanged);
            // 
            // panValueChoices
            // 
            this.panValueChoices.Controls.Add(this.chkMultiSelects);
            this.panValueChoices.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panValueChoices.Location = new System.Drawing.Point(3, 96);
            this.panValueChoices.Name = "panValueChoices";
            this.panValueChoices.Size = new System.Drawing.Size(377, 95);
            this.panValueChoices.TabIndex = 11;
            this.panValueChoices.Visible = false;
            // 
            // chkMultiSelects
            // 
            this.chkMultiSelects.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.chkMultiSelects.CheckOnClick = true;
            this.chkMultiSelects.FormattingEnabled = true;
            this.chkMultiSelects.Items.AddRange(new object[] {
            "ett",
            "två",
            "tre"});
            this.chkMultiSelects.Location = new System.Drawing.Point(12, 8);
            this.chkMultiSelects.Name = "chkMultiSelects";
            this.chkMultiSelects.Size = new System.Drawing.Size(350, 64);
            this.chkMultiSelects.TabIndex = 3;
            this.chkMultiSelects.SelectedIndexChanged += new System.EventHandler(this.genericInputChanged);
            // 
            // panValueLookup
            // 
            this.panValueLookup.BackColor = System.Drawing.Color.Transparent;
            this.panValueLookup.Controls.Add(this.cdsLookupValue);
            this.panValueLookup.Controls.Add(this.btnLookupValue);
            this.panValueLookup.Dock = System.Windows.Forms.DockStyle.Top;
            this.panValueLookup.Location = new System.Drawing.Point(3, 56);
            this.panValueLookup.Name = "panValueLookup";
            this.panValueLookup.Size = new System.Drawing.Size(377, 40);
            this.panValueLookup.TabIndex = 4;
            this.panValueLookup.Visible = false;
            // 
        // btnLookupValue
            // 
            this.btnLookupValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLookupValue.Image = ((System.Drawing.Image)(resources.GetObject("btnLookupValue.Image")));
            this.btnLookupValue.Location = new System.Drawing.Point(334, 7);
            this.btnLookupValue.Name = "btnLookupValue";
            this.btnLookupValue.Size = new System.Drawing.Size(28, 22);
            this.btnLookupValue.TabIndex = 1;
            this.btnLookupValue.UseVisualStyleBackColor = true;
            this.btnLookupValue.Click += new System.EventHandler(this.btnLookupValue_Click);
            // 
            // panValueText
            // 
            this.panValueText.BackColor = System.Drawing.Color.Transparent;
            this.panValueText.Controls.Add(this.cmbValue);
            this.panValueText.Dock = System.Windows.Forms.DockStyle.Top;
            this.panValueText.Location = new System.Drawing.Point(3, 16);
            this.panValueText.Name = "panValueText";
            this.panValueText.Size = new System.Drawing.Size(377, 40);
            this.panValueText.TabIndex = 3;
            // 
            // cmbValue
            // 
            this.cmbValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbValue.FormattingEnabled = true;
            this.cmbValue.Location = new System.Drawing.Point(12, 8);
            this.cmbValue.Name = "cmbValue";
            this.cmbValue.Size = new System.Drawing.Size(350, 21);
            this.cmbValue.TabIndex = 32;
            this.cmbValue.Tag = "value";
            this.cmbValue.TextChanged += new System.EventHandler(this.genericInputChanged);
            // 
            // panAction
            // 
            this.panAction.Controls.Add(this.rbCalculate);
            this.panAction.Controls.Add(this.rbSetValue);
            this.panAction.Controls.Add(this.rbSetNull);
            this.panAction.Controls.Add(this.rbSetTouch);
            this.panAction.Dock = System.Windows.Forms.DockStyle.Top;
            this.panAction.Location = new System.Drawing.Point(0, 48);
            this.panAction.Name = "panAction";
            this.panAction.Size = new System.Drawing.Size(383, 48);
            this.panAction.TabIndex = 2;
            this.panAction.TabStop = false;
            this.panAction.Text = "How to set the Value";
            // 
            // rbCalculate
            // 
            this.rbCalculate.AutoSize = true;
            this.rbCalculate.Location = new System.Drawing.Point(87, 19);
            this.rbCalculate.Name = "rbCalculate";
            this.rbCalculate.Size = new System.Drawing.Size(69, 17);
            this.rbCalculate.TabIndex = 2;
            this.rbCalculate.Text = "Calculate";
            this.rbCalculate.UseVisualStyleBackColor = true;
            this.rbCalculate.CheckedChanged += new System.EventHandler(this.rbSet_CheckedChanged);
            // 
            // rbSetValue
            // 
            this.rbSetValue.AutoSize = true;
            this.rbSetValue.Location = new System.Drawing.Point(15, 19);
            this.rbSetValue.Name = "rbSetValue";
            this.rbSetValue.Size = new System.Drawing.Size(66, 17);
            this.rbSetValue.TabIndex = 1;
            this.rbSetValue.Text = "Set fixed";
            this.rbSetValue.UseVisualStyleBackColor = true;
            this.rbSetValue.CheckedChanged += new System.EventHandler(this.rbSet_CheckedChanged);
            // 
            // rbSetNull
            // 
            this.rbSetNull.AutoSize = true;
            this.rbSetNull.Location = new System.Drawing.Point(231, 19);
            this.rbSetNull.Name = "rbSetNull";
            this.rbSetNull.Size = new System.Drawing.Size(60, 17);
            this.rbSetNull.TabIndex = 4;
            this.rbSetNull.Text = "Set null";
            this.rbSetNull.UseVisualStyleBackColor = true;
            this.rbSetNull.CheckedChanged += new System.EventHandler(this.rbSet_CheckedChanged);
            // 
            // rbSetTouch
            // 
            this.rbSetTouch.AutoSize = true;
            this.rbSetTouch.Location = new System.Drawing.Point(163, 19);
            this.rbSetTouch.Name = "rbSetTouch";
            this.rbSetTouch.Size = new System.Drawing.Size(56, 17);
            this.rbSetTouch.TabIndex = 3;
            this.rbSetTouch.Text = "Touch";
            this.rbSetTouch.UseVisualStyleBackColor = true;
            this.rbSetTouch.CheckedChanged += new System.EventHandler(this.rbSet_CheckedChanged);
            // 
            // panAttribute
            // 
            this.panAttribute.Controls.Add(this.btnUpdateAttributeOptions);
            this.panAttribute.Controls.Add(this.cmbAttribute);
            this.panAttribute.Dock = System.Windows.Forms.DockStyle.Top;
            this.panAttribute.Location = new System.Drawing.Point(0, 0);
            this.panAttribute.Name = "panAttribute";
            this.panAttribute.Size = new System.Drawing.Size(383, 48);
            this.panAttribute.TabIndex = 1;
            this.panAttribute.TabStop = false;
            this.panAttribute.Text = "Select Attribute";
            // 
            // btnUpdateAttributeOptions
            // 
            this.btnUpdateAttributeOptions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnUpdateAttributeOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnUpdateAttributeOptions.Image")));
            this.btnUpdateAttributeOptions.Location = new System.Drawing.Point(340, 17);
            this.btnUpdateAttributeOptions.Name = "btnUpdateAttributeOptions";
            this.btnUpdateAttributeOptions.Size = new System.Drawing.Size(28, 23);
            this.btnUpdateAttributeOptions.TabIndex = 2;
            this.btnUpdateAttributeOptions.UseVisualStyleBackColor = true;
            this.btnUpdateAttributeOptions.Click += new System.EventHandler(this.btnUpdateAttributeOptions_Click);
            // 
            // cmbAttribute
            // 
            this.cmbAttribute.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbAttribute.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cmbAttribute.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbAttribute.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAttribute.FormattingEnabled = true;
            this.cmbAttribute.Location = new System.Drawing.Point(12, 18);
            this.cmbAttribute.Name = "cmbAttribute";
            this.cmbAttribute.Size = new System.Drawing.Size(322, 21);
            this.cmbAttribute.Sorted = true;
            this.cmbAttribute.TabIndex = 1;
            this.cmbAttribute.Tag = "attribute";
            this.cmbAttribute.SelectedIndexChanged += new System.EventHandler(this.cmbAttribute_SelectedIndexChanged);
            this.cmbAttribute.TextChanged += new System.EventHandler(this.genericInputChanged);
            // 
            // panOptions
            // 
            this.panOptions.Controls.Add(this.chkOnlyChange);
            this.panOptions.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panOptions.Location = new System.Drawing.Point(0, 290);
            this.panOptions.Name = "panOptions";
            this.panOptions.Size = new System.Drawing.Size(383, 48);
            this.panOptions.TabIndex = 4;
            this.panOptions.TabStop = false;
            this.panOptions.Text = "Options";
            // 
            // chkOnlyChange
            // 
            this.chkOnlyChange.AutoSize = true;
            this.chkOnlyChange.Location = new System.Drawing.Point(12, 19);
            this.chkOnlyChange.Name = "chkOnlyChange";
            this.chkOnlyChange.Size = new System.Drawing.Size(205, 17);
            this.chkOnlyChange.TabIndex = 33;
            this.chkOnlyChange.Text = "Update only when not the same value";
            this.chkOnlyChange.UseVisualStyleBackColor = true;
            // 
            // panButtons
            // 
            this.panButtons.Controls.Add(this.btnCancel);
            this.panButtons.Controls.Add(this.btnSave);
            this.panButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panButtons.Location = new System.Drawing.Point(0, 338);
            this.panButtons.Name = "panButtons";
            this.panButtons.Size = new System.Drawing.Size(383, 40);
            this.panButtons.TabIndex = 5;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(137, 11);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(109, 23);
            this.btnCancel.TabIndex = 36;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnSave.Location = new System.Drawing.Point(259, 11);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(109, 23);
            this.btnSave.TabIndex = 35;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            // 
            // tmCalc
            // 
            this.tmCalc.Interval = 1000;
            this.tmCalc.Tick += new System.EventHandler(this.tmCalc_Tick);
            // 
            // cdsLookupValue
            // 
            this.cdsLookupValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cdsLookupValue.BackColor = System.Drawing.SystemColors.Window;
            this.cdsLookupValue.Column = null;
            this.cdsLookupValue.DisplayFormat = "";
            this.cdsLookupValue.Location = new System.Drawing.Point(12, 8);
            this.cdsLookupValue.Name = "cdsLookupValue";
            this.cdsLookupValue.RecordHost = this.xrmRecordAttribute;
            this.cdsLookupValue.Size = new System.Drawing.Size(316, 20);
            this.cdsLookupValue.TabIndex = 0;
            // 
            // xrmRecordAttribute
            // 
            this.xrmRecordAttribute.Id = new System.Guid("00000000-0000-0000-0000-000000000000");
            this.xrmRecordAttribute.LogicalName = null;
            this.xrmRecordAttribute.Record = null;
            this.xrmRecordAttribute.Service = null;
            // 
            // xrmLookupDialog
            // 
            this.xrmLookupDialog.AdditionalViews = ((System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Microsoft.Xrm.Sdk.Entity>>)(resources.GetObject("xrmLookupDialog.AdditionalViews")));
            this.xrmLookupDialog.LogicalName = "";
            this.xrmLookupDialog.LogicalNames = null;
            this.xrmLookupDialog.Record = null;
            this.xrmLookupDialog.Service = null;
            this.xrmLookupDialog.Title = null;
            // 
            // UpdateAttribute
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(395, 390);
            this.Controls.Add(this.panRoot);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(330, 290);
            this.Name = "UpdateAttribute";
            this.Padding = new System.Windows.Forms.Padding(6);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Attribute";
            this.panRoot.ResumeLayout(false);
            this.panValue.ResumeLayout(false);
            this.panValueToken.ResumeLayout(false);
            this.panValueToken.PerformLayout();
            this.panValueTextMulti.ResumeLayout(false);
            this.panValueTextMulti.PerformLayout();
            this.panValueChoices.ResumeLayout(false);
            this.panValueLookup.ResumeLayout(false);
            this.panValueLookup.PerformLayout();
            this.panValueText.ResumeLayout(false);
            this.panAction.ResumeLayout(false);
            this.panAction.PerformLayout();
            this.panAttribute.ResumeLayout(false);
            this.panOptions.ResumeLayout(false);
            this.panOptions.PerformLayout();
            this.panButtons.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panRoot;
        private System.Windows.Forms.Panel panValueTextMulti;
        private System.Windows.Forms.TextBox txtValueMultiline;
        private System.Windows.Forms.Panel panValueToken;
        private System.Windows.Forms.TextBox txtCalcPreview;
        private System.Windows.Forms.Button btnCalcHelp;
        private System.Windows.Forms.Panel panButtons;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.CheckBox chkOnlyChange;
        private System.Windows.Forms.Panel panValueLookup;
        private Rappen.XTB.Helpers.Controls.XRMColumnText cdsLookupValue;
        private System.Windows.Forms.Button btnLookupValue;
        private System.Windows.Forms.Panel panValueText;
        private System.Windows.Forms.ComboBox cmbValue;
        private System.Windows.Forms.GroupBox panAction;
        private System.Windows.Forms.RadioButton rbCalculate;
        private System.Windows.Forms.RadioButton rbSetValue;
        private System.Windows.Forms.RadioButton rbSetNull;
        private System.Windows.Forms.RadioButton rbSetTouch;
        private System.Windows.Forms.GroupBox panAttribute;
        private System.Windows.Forms.Button btnUpdateAttributeOptions;
        private System.Windows.Forms.ComboBox cmbAttribute;
        private Rappen.XTB.Helpers.Controls.XRMRecordHost xrmRecordAttribute;
        private Rappen.XTB.Helpers.Controls.XRMLookupDialog xrmLookupDialog;
        private System.Windows.Forms.Timer tmCalc;
        internal System.Windows.Forms.TextBox txtValueCalc;
        private System.Windows.Forms.Panel panValueChoices;
        private System.Windows.Forms.CheckedListBox chkMultiSelects;
        private System.Windows.Forms.GroupBox panOptions;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.GroupBox panValue;
        private System.Windows.Forms.Button btnXRMTR;
        private System.Windows.Forms.Label label2;
    }
}