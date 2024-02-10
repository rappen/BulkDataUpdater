namespace Cinteros.XTB.BulkDataUpdater.Forms
{
    partial class AttributeOptions
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
            this.chkEverything = new System.Windows.Forms.CheckBox();
            this.chkRequired = new System.Windows.Forms.CheckBox();
            this.chkRecommended = new System.Windows.Forms.CheckBox();
            this.chkOnForm = new System.Windows.Forms.CheckBox();
            this.chkOnView = new System.Windows.Forms.CheckBox();
            this.chkInQuery = new System.Windows.Forms.CheckBox();
            this.rbThatAreAnd = new System.Windows.Forms.RadioButton();
            this.rbThatAreOr = new System.Windows.Forms.RadioButton();
            this.label1 = new System.Windows.Forms.Label();
            this.gbAttrThatAre = new System.Windows.Forms.GroupBox();
            this.gbSpecifics = new System.Windows.Forms.GroupBox();
            this.chkImportSeqNo = new System.Windows.Forms.CheckBox();
            this.chkUnallowedUpdate = new System.Windows.Forms.CheckBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.lblTable = new System.Windows.Forms.Label();
            this.gbAttrThatAre.SuspendLayout();
            this.gbSpecifics.SuspendLayout();
            this.SuspendLayout();
            // 
            // chkEverything
            // 
            this.chkEverything.AutoSize = true;
            this.chkEverything.Location = new System.Drawing.Point(22, 72);
            this.chkEverything.Name = "chkEverything";
            this.chkEverything.Size = new System.Drawing.Size(76, 17);
            this.chkEverything.TabIndex = 0;
            this.chkEverything.Text = "Everything";
            this.chkEverything.UseVisualStyleBackColor = true;
            this.chkEverything.CheckedChanged += new System.EventHandler(this.chkEverything_CheckedChanged);
            // 
            // chkRequired
            // 
            this.chkRequired.AutoSize = true;
            this.chkRequired.Location = new System.Drawing.Point(250, 25);
            this.chkRequired.Name = "chkRequired";
            this.chkRequired.Size = new System.Drawing.Size(69, 17);
            this.chkRequired.TabIndex = 1;
            this.chkRequired.Text = "Required";
            this.chkRequired.UseVisualStyleBackColor = true;
            // 
            // chkRecommended
            // 
            this.chkRecommended.AutoSize = true;
            this.chkRecommended.Location = new System.Drawing.Point(250, 48);
            this.chkRecommended.Name = "chkRecommended";
            this.chkRecommended.Size = new System.Drawing.Size(98, 17);
            this.chkRecommended.TabIndex = 2;
            this.chkRecommended.Text = "Recommended";
            this.chkRecommended.UseVisualStyleBackColor = true;
            // 
            // chkOnForm
            // 
            this.chkOnForm.AutoSize = true;
            this.chkOnForm.Location = new System.Drawing.Point(138, 25);
            this.chkOnForm.Name = "chkOnForm";
            this.chkOnForm.Size = new System.Drawing.Size(83, 17);
            this.chkOnForm.TabIndex = 3;
            this.chkOnForm.Text = "On any form";
            this.chkOnForm.UseVisualStyleBackColor = true;
            // 
            // chkOnView
            // 
            this.chkOnView.AutoSize = true;
            this.chkOnView.Location = new System.Drawing.Point(138, 48);
            this.chkOnView.Name = "chkOnView";
            this.chkOnView.Size = new System.Drawing.Size(85, 17);
            this.chkOnView.TabIndex = 4;
            this.chkOnView.Text = "On any view";
            this.chkOnView.UseVisualStyleBackColor = true;
            // 
            // chkInQuery
            // 
            this.chkInQuery.AutoSize = true;
            this.chkInQuery.Location = new System.Drawing.Point(19, 25);
            this.chkInQuery.Name = "chkInQuery";
            this.chkInQuery.Size = new System.Drawing.Size(82, 17);
            this.chkInQuery.TabIndex = 5;
            this.chkInQuery.Text = "In the query";
            this.chkInQuery.UseVisualStyleBackColor = true;
            // 
            // rbThatAreAnd
            // 
            this.rbThatAreAnd.AutoSize = true;
            this.rbThatAreAnd.Location = new System.Drawing.Point(138, 80);
            this.rbThatAreAnd.Name = "rbThatAreAnd";
            this.rbThatAreAnd.Size = new System.Drawing.Size(48, 17);
            this.rbThatAreAnd.TabIndex = 6;
            this.rbThatAreAnd.Text = "AND";
            this.rbThatAreAnd.UseVisualStyleBackColor = true;
            // 
            // rbThatAreOr
            // 
            this.rbThatAreOr.AutoSize = true;
            this.rbThatAreOr.Checked = true;
            this.rbThatAreOr.Location = new System.Drawing.Point(192, 80);
            this.rbThatAreOr.Name = "rbThatAreOr";
            this.rbThatAreOr.Size = new System.Drawing.Size(41, 17);
            this.rbThatAreOr.TabIndex = 7;
            this.rbThatAreOr.TabStop = true;
            this.rbThatAreOr.Text = "OR";
            this.rbThatAreOr.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(41, 82);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(91, 13);
            this.label1.TabIndex = 8;
            this.label1.Text = "Combination type:";
            // 
            // gbAttrThatAre
            // 
            this.gbAttrThatAre.Controls.Add(this.chkRequired);
            this.gbAttrThatAre.Controls.Add(this.label1);
            this.gbAttrThatAre.Controls.Add(this.chkRecommended);
            this.gbAttrThatAre.Controls.Add(this.rbThatAreOr);
            this.gbAttrThatAre.Controls.Add(this.chkOnForm);
            this.gbAttrThatAre.Controls.Add(this.rbThatAreAnd);
            this.gbAttrThatAre.Controls.Add(this.chkOnView);
            this.gbAttrThatAre.Controls.Add(this.chkInQuery);
            this.gbAttrThatAre.Location = new System.Drawing.Point(22, 95);
            this.gbAttrThatAre.Name = "gbAttrThatAre";
            this.gbAttrThatAre.Size = new System.Drawing.Size(374, 111);
            this.gbAttrThatAre.TabIndex = 9;
            this.gbAttrThatAre.TabStop = false;
            this.gbAttrThatAre.Text = "Columns that are";
            // 
            // gbSpecifics
            // 
            this.gbSpecifics.Controls.Add(this.chkImportSeqNo);
            this.gbSpecifics.Controls.Add(this.chkUnallowedUpdate);
            this.gbSpecifics.Location = new System.Drawing.Point(22, 221);
            this.gbSpecifics.Name = "gbSpecifics";
            this.gbSpecifics.Size = new System.Drawing.Size(374, 80);
            this.gbSpecifics.TabIndex = 10;
            this.gbSpecifics.TabStop = false;
            this.gbSpecifics.Text = "Specifics";
            // 
            // chkImportSeqNo
            // 
            this.chkImportSeqNo.AutoSize = true;
            this.chkImportSeqNo.Location = new System.Drawing.Point(19, 48);
            this.chkImportSeqNo.Name = "chkImportSeqNo";
            this.chkImportSeqNo.Size = new System.Drawing.Size(147, 17);
            this.chkImportSeqNo.TabIndex = 1;
            this.chkImportSeqNo.Text = "Import Sequence Number";
            this.chkImportSeqNo.UseVisualStyleBackColor = true;
            // 
            // chkUnallowedUpdate
            // 
            this.chkUnallowedUpdate.AutoSize = true;
            this.chkUnallowedUpdate.Location = new System.Drawing.Point(19, 25);
            this.chkUnallowedUpdate.Name = "chkUnallowedUpdate";
            this.chkUnallowedUpdate.Size = new System.Drawing.Size(126, 17);
            this.chkUnallowedUpdate.TabIndex = 0;
            this.chkUnallowedUpdate.Text = "Unallowed to Update";
            this.chkUnallowedUpdate.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(128, 317);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 11;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(220, 317);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 12;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 13);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(120, 13);
            this.label2.TabIndex = 13;
            this.label2.Text = "Show columns for table:";
            // 
            // lblTable
            // 
            this.lblTable.AutoSize = true;
            this.lblTable.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTable.Location = new System.Drawing.Point(29, 32);
            this.lblTable.Name = "lblTable";
            this.lblTable.Size = new System.Drawing.Size(62, 20);
            this.lblTable.TabIndex = 14;
            this.lblTable.Text = "<table>";
            // 
            // AttributeOptions
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(421, 352);
            this.Controls.Add(this.lblTable);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.gbSpecifics);
            this.Controls.Add(this.gbAttrThatAre);
            this.Controls.Add(this.chkEverything);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AttributeOptions";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Which Columns to show in the list";
            this.TopMost = true;
            this.gbAttrThatAre.ResumeLayout(false);
            this.gbAttrThatAre.PerformLayout();
            this.gbSpecifics.ResumeLayout(false);
            this.gbSpecifics.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox chkEverything;
        private System.Windows.Forms.CheckBox chkRequired;
        private System.Windows.Forms.CheckBox chkRecommended;
        private System.Windows.Forms.CheckBox chkOnForm;
        private System.Windows.Forms.CheckBox chkOnView;
        private System.Windows.Forms.CheckBox chkInQuery;
        private System.Windows.Forms.RadioButton rbThatAreAnd;
        private System.Windows.Forms.RadioButton rbThatAreOr;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox gbAttrThatAre;
        private System.Windows.Forms.GroupBox gbSpecifics;
        private System.Windows.Forms.CheckBox chkImportSeqNo;
        private System.Windows.Forms.CheckBox chkUnallowedUpdate;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblTable;
    }
}