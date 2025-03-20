namespace NhakhoaMyNgoc_Db
{
    partial class DateTimePickerDialog
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
            this.mcCalendar = new System.Windows.Forms.MonthCalendar();
            this.dtpkTime = new System.Windows.Forms.DateTimePicker();
            this.pnlClock = new System.Windows.Forms.Panel();
            this.btnOK = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // mcCalendar
            // 
            this.mcCalendar.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mcCalendar.Location = new System.Drawing.Point(18, 18);
            this.mcCalendar.MaxSelectionCount = 1;
            this.mcCalendar.Name = "mcCalendar";
            this.mcCalendar.TabIndex = 0;
            // 
            // dtpkTime
            // 
            this.dtpkTime.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.dtpkTime.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dtpkTime.Location = new System.Drawing.Point(257, 186);
            this.dtpkTime.Name = "dtpkTime";
            this.dtpkTime.ShowUpDown = true;
            this.dtpkTime.Size = new System.Drawing.Size(162, 29);
            this.dtpkTime.TabIndex = 1;
            // 
            // pnlClock
            // 
            this.pnlClock.BackColor = System.Drawing.Color.Transparent;
            this.pnlClock.Location = new System.Drawing.Point(257, 18);
            this.pnlClock.Name = "pnlClock";
            this.pnlClock.Size = new System.Drawing.Size(162, 162);
            this.pnlClock.TabIndex = 2;
            this.pnlClock.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlClock_Paint);
            // 
            // btnOK
            // 
            this.btnOK.Font = new System.Drawing.Font("Segoe UI", 13F);
            this.btnOK.Location = new System.Drawing.Point(177, 226);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(88, 37);
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // DateTimePickerDialog
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(435, 275);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.pnlClock);
            this.Controls.Add(this.dtpkTime);
            this.Controls.Add(this.mcCalendar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DateTimePickerDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Chọn ngày giờ";
            this.Load += new System.EventHandler(this.DateTimePickerDialog_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.MonthCalendar mcCalendar;
        private System.Windows.Forms.DateTimePicker dtpkTime;
        private System.Windows.Forms.Panel pnlClock;
        private System.Windows.Forms.Button btnOK;
    }
}