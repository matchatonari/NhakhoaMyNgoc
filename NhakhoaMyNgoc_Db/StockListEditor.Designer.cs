namespace NhakhoaMyNgoc_Db
{
    partial class StockListEditor
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
            this.dgv_StockList = new System.Windows.Forms.DataGridView();
            this.bsStockList = new System.Windows.Forms.BindingSource(this.components);
            this.cbStockList_IsActive = new System.Windows.Forms.CheckBox();
            this.cmsStockList = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsi_Restore = new System.Windows.Forms.ToolStripMenuItem();
            this.StockList_Id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StockList_Alias = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.StockList_Address = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnOK = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_StockList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bsStockList)).BeginInit();
            this.cmsStockList.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgv_StockList
            // 
            this.dgv_StockList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_StockList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.StockList_Id,
            this.StockList_Alias,
            this.StockList_Address});
            this.dgv_StockList.ContextMenuStrip = this.cmsStockList;
            this.dgv_StockList.Location = new System.Drawing.Point(13, 14);
            this.dgv_StockList.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.dgv_StockList.Name = "dgv_StockList";
            this.dgv_StockList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv_StockList.Size = new System.Drawing.Size(746, 428);
            this.dgv_StockList.TabIndex = 2;
            // 
            // cbStockList_IsActive
            // 
            this.cbStockList_IsActive.AutoSize = true;
            this.cbStockList_IsActive.Location = new System.Drawing.Point(13, 450);
            this.cbStockList_IsActive.Name = "cbStockList_IsActive";
            this.cbStockList_IsActive.Size = new System.Drawing.Size(108, 25);
            this.cbStockList_IsActive.TabIndex = 3;
            this.cbStockList_IsActive.Text = "Mục đã xoá";
            this.cbStockList_IsActive.UseVisualStyleBackColor = true;
            this.cbStockList_IsActive.CheckedChanged += new System.EventHandler(this.cbStockList_IsActive_CheckedChanged);
            // 
            // cmsStockList
            // 
            this.cmsStockList.ImageScalingSize = new System.Drawing.Size(17, 17);
            this.cmsStockList.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsi_Restore});
            this.cmsStockList.Name = "contextMenuStrip1";
            this.cmsStockList.Size = new System.Drawing.Size(129, 26);
            // 
            // tsi_Restore
            // 
            this.tsi_Restore.Name = "tsi_Restore";
            this.tsi_Restore.Size = new System.Drawing.Size(128, 22);
            this.tsi_Restore.Text = "Khôi phục";
            // 
            // StockList_Id
            // 
            this.StockList_Id.DataPropertyName = "StockList_Id";
            this.StockList_Id.HeaderText = "ID";
            this.StockList_Id.Name = "StockList_Id";
            this.StockList_Id.Visible = false;
            // 
            // StockList_Alias
            // 
            this.StockList_Alias.DataPropertyName = "StockList_Alias";
            this.StockList_Alias.HeaderText = "Tên";
            this.StockList_Alias.Name = "StockList_Alias";
            this.StockList_Alias.Width = 200;
            // 
            // StockList_Address
            // 
            this.StockList_Address.DataPropertyName = "StockList_Address";
            this.StockList_Address.HeaderText = "Địa chỉ";
            this.StockList_Address.Name = "StockList_Address";
            this.StockList_Address.Width = 500;
            // 
            // btnOK
            // 
            this.btnOK.BackgroundImage = global::NhakhoaMyNgoc_Db.Properties.Resources.CHECK;
            this.btnOK.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnOK.Location = new System.Drawing.Point(719, 450);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(40, 40);
            this.btnOK.TabIndex = 32;
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // StockListEditor
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(770, 498);
            this.ControlBox = false;
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.cbStockList_IsActive);
            this.Controls.Add(this.dgv_StockList);
            this.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "StockListEditor";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Chỉnh sửa danh sách kho";
            this.Load += new System.EventHandler(this.StockListEditor_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_StockList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bsStockList)).EndInit();
            this.cmsStockList.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.DataGridView dgv_StockList;
        private System.Windows.Forms.BindingSource bsStockList;
        private System.Windows.Forms.CheckBox cbStockList_IsActive;
        private System.Windows.Forms.ContextMenuStrip cmsStockList;
        private System.Windows.Forms.ToolStripMenuItem tsi_Restore;
        private System.Windows.Forms.DataGridViewTextBoxColumn StockList_Id;
        private System.Windows.Forms.DataGridViewTextBoxColumn StockList_Alias;
        private System.Windows.Forms.DataGridViewTextBoxColumn StockList_Address;
        private System.Windows.Forms.Button btnOK;
    }
}