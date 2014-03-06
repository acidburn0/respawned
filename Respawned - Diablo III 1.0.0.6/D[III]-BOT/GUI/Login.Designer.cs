namespace Respawned
{
    partial class Login
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Login));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.chkAutologin = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.txt_KeyCode = new System.Windows.Forms.TextBox();
            this.txt_Profile_AccountEmail = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.cmd_Rename_Profil = new System.Windows.Forms.Button();
            this.label15 = new System.Windows.Forms.Label();
            this.rtxt_News = new System.Windows.Forms.RichTextBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.chkAutologin);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.txt_KeyCode);
            this.groupBox1.Controls.Add(this.txt_Profile_AccountEmail);
            this.groupBox1.Controls.Add(this.button1);
            this.groupBox1.Controls.Add(this.cmd_Rename_Profil);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(6, 8);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(377, 118);
            this.groupBox1.TabIndex = 34;
            this.groupBox1.TabStop = false;
            // 
            // chkAutologin
            // 
            this.chkAutologin.AutoSize = true;
            this.chkAutologin.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkAutologin.Location = new System.Drawing.Point(270, 98);
            this.chkAutologin.Name = "chkAutologin";
            this.chkAutologin.Size = new System.Drawing.Size(86, 17);
            this.chkAutologin.TabIndex = 55;
            this.chkAutologin.Text = "Auto Login ?";
            this.chkAutologin.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Black;
            this.label2.Location = new System.Drawing.Point(25, 45);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(28, 15);
            this.label2.TabIndex = 54;
            this.label2.Text = "Key";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Black;
            this.label1.Location = new System.Drawing.Point(19, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 15);
            this.label1.TabIndex = 53;
            this.label1.Text = "Nick";
            // 
            // txt_KeyCode
            // 
            this.txt_KeyCode.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_KeyCode.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_KeyCode.Location = new System.Drawing.Point(56, 44);
            this.txt_KeyCode.Name = "txt_KeyCode";
            this.txt_KeyCode.Size = new System.Drawing.Size(300, 20);
            this.txt_KeyCode.TabIndex = 52;
            // 
            // txt_Profile_AccountEmail
            // 
            this.txt_Profile_AccountEmail.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txt_Profile_AccountEmail.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_Profile_AccountEmail.Location = new System.Drawing.Point(56, 14);
            this.txt_Profile_AccountEmail.Name = "txt_Profile_AccountEmail";
            this.txt_Profile_AccountEmail.Size = new System.Drawing.Size(300, 20);
            this.txt_Profile_AccountEmail.TabIndex = 51;
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.SystemColors.MenuBar;
            this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Location = new System.Drawing.Point(56, 70);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(70, 23);
            this.button1.TabIndex = 50;
            this.button1.Text = "Cancel";
            this.button1.UseVisualStyleBackColor = false;
            // 
            // cmd_Rename_Profil
            // 
            this.cmd_Rename_Profil.BackColor = System.Drawing.SystemColors.MenuBar;
            this.cmd_Rename_Profil.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.cmd_Rename_Profil.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmd_Rename_Profil.Location = new System.Drawing.Point(132, 70);
            this.cmd_Rename_Profil.Name = "cmd_Rename_Profil";
            this.cmd_Rename_Profil.Size = new System.Drawing.Size(224, 23);
            this.cmd_Rename_Profil.TabIndex = 49;
            this.cmd_Rename_Profil.Text = "Login";
            this.cmd_Rename_Profil.UseVisualStyleBackColor = false;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.label15.ForeColor = System.Drawing.Color.DarkRed;
            this.label15.Location = new System.Drawing.Point(3, 327);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(339, 15);
            this.label15.TabIndex = 48;
            this.label15.Text = "Use this software on your own risk, we\'re not liable for any loss.";
            // 
            // rtxt_News
            // 
            this.rtxt_News.Location = new System.Drawing.Point(6, 132);
            this.rtxt_News.Name = "rtxt_News";
            this.rtxt_News.ReadOnly = true;
            this.rtxt_News.Size = new System.Drawing.Size(377, 190);
            this.rtxt_News.TabIndex = 35;
            this.rtxt_News.Text = "";
            // 
            // Login
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(388, 352);
            this.Controls.Add(this.rtxt_News);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label15);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Login";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Login";
            this.Load += new System.EventHandler(this.Login_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        public System.Windows.Forms.TextBox txt_KeyCode;
        public System.Windows.Forms.TextBox txt_Profile_AccountEmail;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button cmd_Rename_Profil;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.RichTextBox rtxt_News;
        public System.Windows.Forms.CheckBox chkAutologin;


    }
}