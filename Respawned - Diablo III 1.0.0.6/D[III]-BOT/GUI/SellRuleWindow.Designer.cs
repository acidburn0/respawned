namespace Respawned
{
    partial class SellRuleWindow
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
            this.label2 = new System.Windows.Forms.Label();
            this.txtRuleName = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cboItemTypes = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.nudMinIlvl = new System.Windows.Forms.NumericUpDown();
            this.lblStat1 = new System.Windows.Forms.Label();
            this.cboStat1 = new System.Windows.Forms.ComboBox();
            this.lblGrtr1 = new System.Windows.Forms.Label();
            this.nudStat1 = new System.Windows.Forms.NumericUpDown();
            this.btnAddLine = new System.Windows.Forms.Button();
            this.btnRemoveLine1 = new System.Windows.Forms.Button();
            this.pnlRuleDef = new System.Windows.Forms.Panel();
            this.btnConfirm = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.nudMinIlvl)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudStat1)).BeginInit();
            this.pnlRuleDef.SuspendLayout();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 15);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(63, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Rule Name:";
            // 
            // txtRuleName
            // 
            this.txtRuleName.Location = new System.Drawing.Point(75, 12);
            this.txtRuleName.Name = "txtRuleName";
            this.txtRuleName.Size = new System.Drawing.Size(384, 20);
            this.txtRuleName.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Item Type:";
            // 
            // cboItemTypes
            // 
            this.cboItemTypes.DisplayMember = "Text";
            this.cboItemTypes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboItemTypes.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboItemTypes.FormattingEnabled = true;
            this.cboItemTypes.Location = new System.Drawing.Point(84, 9);
            this.cboItemTypes.Name = "cboItemTypes";
            this.cboItemTypes.Size = new System.Drawing.Size(199, 21);
            this.cboItemTypes.Sorted = true;
            this.cboItemTypes.TabIndex = 1;
            this.cboItemTypes.ValueMember = "Value";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(289, 12);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(42, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Min ilvl:";
            // 
            // nudMinIlvl
            // 
            this.nudMinIlvl.Location = new System.Drawing.Point(341, 10);
            this.nudMinIlvl.Maximum = new decimal(new int[] {
            63,
            0,
            0,
            0});
            this.nudMinIlvl.Name = "nudMinIlvl";
            this.nudMinIlvl.Size = new System.Drawing.Size(49, 20);
            this.nudMinIlvl.TabIndex = 2;
            this.nudMinIlvl.Value = new decimal(new int[] {
            63,
            0,
            0,
            0});
            // 
            // lblStat1
            // 
            this.lblStat1.AutoSize = true;
            this.lblStat1.Location = new System.Drawing.Point(7, 52);
            this.lblStat1.Name = "lblStat1";
            this.lblStat1.Size = new System.Drawing.Size(71, 13);
            this.lblStat1.TabIndex = 0;
            this.lblStat1.Text = "With this stat:";
            // 
            // cboStat1
            // 
            this.cboStat1.BackColor = System.Drawing.SystemColors.Window;
            this.cboStat1.DisplayMember = "Text";
            this.cboStat1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboStat1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cboStat1.FormattingEnabled = true;
            this.cboStat1.Items.AddRange(new object[] {
            "Dexterity",
            "Intellect",
            "Strength",
            "Vitality"});
            this.cboStat1.Location = new System.Drawing.Point(84, 49);
            this.cboStat1.Name = "cboStat1";
            this.cboStat1.Size = new System.Drawing.Size(199, 21);
            this.cboStat1.Sorted = true;
            this.cboStat1.TabIndex = 3;
            this.cboStat1.ValueMember = "Value";
            this.cboStat1.SelectedIndexChanged += new System.EventHandler(this.cboStat_SelectedIndexChanged);
            // 
            // lblGrtr1
            // 
            this.lblGrtr1.AutoSize = true;
            this.lblGrtr1.Location = new System.Drawing.Point(303, 53);
            this.lblGrtr1.Name = "lblGrtr1";
            this.lblGrtr1.Size = new System.Drawing.Size(19, 13);
            this.lblGrtr1.TabIndex = 0;
            this.lblGrtr1.Text = ">=";
            // 
            // nudStat1
            // 
            this.nudStat1.Location = new System.Drawing.Point(341, 50);
            this.nudStat1.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.nudStat1.Name = "nudStat1";
            this.nudStat1.Size = new System.Drawing.Size(49, 20);
            this.nudStat1.TabIndex = 4;
            // 
            // btnAddLine
            // 
            this.btnAddLine.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddLine.Location = new System.Drawing.Point(197, 453);
            this.btnAddLine.Name = "btnAddLine";
            this.btnAddLine.Size = new System.Drawing.Size(75, 23);
            this.btnAddLine.TabIndex = 30;
            this.btnAddLine.Text = "+";
            this.btnAddLine.UseVisualStyleBackColor = true;
            this.btnAddLine.Click += new System.EventHandler(this.AddLine);
            // 
            // btnRemoveLine1
            // 
            this.btnRemoveLine1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRemoveLine1.Location = new System.Drawing.Point(396, 48);
            this.btnRemoveLine1.Name = "btnRemoveLine1";
            this.btnRemoveLine1.Size = new System.Drawing.Size(31, 23);
            this.btnRemoveLine1.TabIndex = 5;
            this.btnRemoveLine1.Text = "-";
            this.btnRemoveLine1.UseVisualStyleBackColor = true;
            this.btnRemoveLine1.Visible = false;
            // 
            // pnlRuleDef
            // 
            this.pnlRuleDef.AutoScroll = true;
            this.pnlRuleDef.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlRuleDef.Controls.Add(this.btnRemoveLine1);
            this.pnlRuleDef.Controls.Add(this.cboStat1);
            this.pnlRuleDef.Controls.Add(this.label1);
            this.pnlRuleDef.Controls.Add(this.nudStat1);
            this.pnlRuleDef.Controls.Add(this.lblStat1);
            this.pnlRuleDef.Controls.Add(this.nudMinIlvl);
            this.pnlRuleDef.Controls.Add(this.label3);
            this.pnlRuleDef.Controls.Add(this.cboItemTypes);
            this.pnlRuleDef.Controls.Add(this.lblGrtr1);
            this.pnlRuleDef.Location = new System.Drawing.Point(9, 38);
            this.pnlRuleDef.Name = "pnlRuleDef";
            this.pnlRuleDef.Size = new System.Drawing.Size(450, 409);
            this.pnlRuleDef.TabIndex = 4;
            // 
            // btnConfirm
            // 
            this.btnConfirm.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnConfirm.Location = new System.Drawing.Point(155, 494);
            this.btnConfirm.Name = "btnConfirm";
            this.btnConfirm.Size = new System.Drawing.Size(75, 23);
            this.btnConfirm.TabIndex = 31;
            this.btnConfirm.Text = "Confirm";
            this.btnConfirm.UseVisualStyleBackColor = true;
            this.btnConfirm.Click += new System.EventHandler(this.btnConfirm_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Location = new System.Drawing.Point(238, 494);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 32;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // SellRuleWindow
            // 
            this.AcceptButton = this.btnConfirm;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(471, 529);
            this.ControlBox = false;
            this.Controls.Add(this.pnlRuleDef);
            this.Controls.Add(this.txtRuleName);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnConfirm);
            this.Controls.Add(this.btnAddLine);
            this.Controls.Add(this.label2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SellRuleWindow";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Configure Sell Rule";
            ((System.ComponentModel.ISupportInitialize)(this.nudMinIlvl)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudStat1)).EndInit();
            this.pnlRuleDef.ResumeLayout(false);
            this.pnlRuleDef.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtRuleName;
        private System.Windows.Forms.Button btnAddLine;
        private System.Windows.Forms.NumericUpDown nudStat1;
        private System.Windows.Forms.NumericUpDown nudMinIlvl;
        private System.Windows.Forms.ComboBox cboStat1;
        private System.Windows.Forms.Label lblGrtr1;
        private System.Windows.Forms.ComboBox cboItemTypes;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label lblStat1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnRemoveLine1;
        private System.Windows.Forms.Panel pnlRuleDef;
        private System.Windows.Forms.Button btnConfirm;
        private System.Windows.Forms.Button btnCancel;
    }
}