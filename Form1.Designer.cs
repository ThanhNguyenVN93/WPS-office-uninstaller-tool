namespace frmwpsoffice
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.lblsys = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.btncleanup = new System.Windows.Forms.Button();
            this.btnuninstall = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.txtdirect = new System.Windows.Forms.TextBox();
            this.txtcheck = new System.Windows.Forms.TextBox();
            this.lblcheck = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(2, 1);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(401, 73);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(10, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(382, 25);
            this.label1.TabIndex = 0;
            this.label1.Text = "WPS OFFICE UNINSTALLER TOOL";
            // 
            // groupBox2
            // 
            this.groupBox2.BackColor = System.Drawing.Color.LightYellow;
            this.groupBox2.Controls.Add(this.lblsys);
            this.groupBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox2.Location = new System.Drawing.Point(2, 305);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(401, 73);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            // 
            // lblsys
            // 
            this.lblsys.AutoSize = true;
            this.lblsys.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblsys.Location = new System.Drawing.Point(10, 25);
            this.lblsys.Name = "lblsys";
            this.lblsys.Size = new System.Drawing.Size(51, 20);
            this.lblsys.TabIndex = 1;
            this.lblsys.Text = "label2";
            // 
            // groupBox3
            // 
            this.groupBox3.BackColor = System.Drawing.SystemColors.HighlightText;
            this.groupBox3.Controls.Add(this.btncleanup);
            this.groupBox3.Controls.Add(this.btnuninstall);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.txtdirect);
            this.groupBox3.Controls.Add(this.txtcheck);
            this.groupBox3.Controls.Add(this.lblcheck);
            this.groupBox3.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox3.Location = new System.Drawing.Point(2, 75);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(401, 224);
            this.groupBox3.TabIndex = 1;
            this.groupBox3.TabStop = false;
            // 
            // btncleanup
            // 
            this.btncleanup.Location = new System.Drawing.Point(270, 131);
            this.btncleanup.Name = "btncleanup";
            this.btncleanup.Size = new System.Drawing.Size(120, 63);
            this.btncleanup.TabIndex = 5;
            this.btncleanup.Text = "CLEAN UP!";
            this.btncleanup.UseVisualStyleBackColor = true;
            this.btncleanup.Click += new System.EventHandler(this.btncleanup_Click_1);
            // 
            // btnuninstall
            // 
            this.btnuninstall.Location = new System.Drawing.Point(10, 131);
            this.btnuninstall.Name = "btnuninstall";
            this.btnuninstall.Size = new System.Drawing.Size(126, 63);
            this.btnuninstall.TabIndex = 4;
            this.btnuninstall.Text = "UNINSTALL";
            this.btnuninstall.UseVisualStyleBackColor = true;
            this.btnuninstall.Click += new System.EventHandler(this.btnuninstall_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(12, 54);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(99, 18);
            this.label2.TabIndex = 3;
            this.label2.Text = "DIRECTORY:";
            // 
            // txtdirect
            // 
            this.txtdirect.Enabled = false;
            this.txtdirect.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtdirect.Location = new System.Drawing.Point(117, 50);
            this.txtdirect.Multiline = true;
            this.txtdirect.Name = "txtdirect";
            this.txtdirect.Size = new System.Drawing.Size(273, 53);
            this.txtdirect.TabIndex = 2;
            // 
            // txtcheck
            // 
            this.txtcheck.Enabled = false;
            this.txtcheck.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtcheck.Location = new System.Drawing.Point(117, 18);
            this.txtcheck.Name = "txtcheck";
            this.txtcheck.Size = new System.Drawing.Size(273, 26);
            this.txtcheck.TabIndex = 1;
            // 
            // lblcheck
            // 
            this.lblcheck.AutoSize = true;
            this.lblcheck.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblcheck.Location = new System.Drawing.Point(11, 25);
            this.lblcheck.Name = "lblcheck";
            this.lblcheck.Size = new System.Drawing.Size(65, 18);
            this.lblcheck.TabIndex = 0;
            this.lblcheck.Text = "CHECK:";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(404, 377);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximumSize = new System.Drawing.Size(420, 416);
            this.MinimumSize = new System.Drawing.Size(420, 416);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "WPS UNINSTALLER TOO - BY: THANH NGUYEN";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label lblsys;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label lblcheck;
        private System.Windows.Forms.TextBox txtcheck;
        private System.Windows.Forms.TextBox txtdirect;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btncleanup;
        private System.Windows.Forms.Button btnuninstall;
    }
}

