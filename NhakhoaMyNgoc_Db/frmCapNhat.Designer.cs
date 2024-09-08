namespace NhakhoaMyNgoc_Db
{
    partial class frmCapNhat
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
            this.lblTrangThai = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.btnOK = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblTrangThai
            // 
            this.lblTrangThai.AutoSize = true;
            this.lblTrangThai.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.lblTrangThai.Location = new System.Drawing.Point(12, 9);
            this.lblTrangThai.Name = "lblTrangThai";
            this.lblTrangThai.Size = new System.Drawing.Size(212, 23);
            this.lblTrangThai.TabIndex = 0;
            this.lblTrangThai.Text = "Đang kiểm tra phiên bản...";
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(16, 44);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(366, 23);
            this.progressBar.TabIndex = 1;
            // 
            // btnOK
            // 
            this.btnOK.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.btnOK.Location = new System.Drawing.Point(307, 73);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 32);
            this.btnOK.TabIndex = 2;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // frmCapNhat
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(394, 117);
            this.ControlBox = false;
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.lblTrangThai);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "frmCapNhat";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Cập nhật";
            this.Load += new System.EventHandler(this.frmCapNhat_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTrangThai;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Button btnOK;
    }
}