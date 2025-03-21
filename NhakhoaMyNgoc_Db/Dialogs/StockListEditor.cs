using System;
using System.Windows.Forms;

namespace NhakhoaMyNgoc_Db
{
    public partial class StockListEditor : Form
    {
        public StockListEditor()
        {
            InitializeComponent();
        }

        private void StockListEditor_Load(object sender, EventArgs e)
        {
            dgv_StockList.AutoGenerateColumns = false;

            bsStockList.DataSource = Database.Query("StockList");
            dgv_StockList.DataSource = bsStockList;

            Util.AttachHook<Stock>(dgv_StockList, Util.HOOK_UPDATE | Util.HOOK_DELETE | Util.HOOK_RESTORE, "StockList", false, tsi_Restore);

            cbStockList_IsActive_CheckedChanged(sender, e);
        }

        private void cbStockList_IsActive_CheckedChanged(object sender, EventArgs e)
        {
            if (cbStockList_IsActive.Checked)
                bsStockList.RemoveFilter();
            else
                bsStockList.Filter = "StockList_IsActive = 1 OR StockList_IsActive IS NULL";
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // nếu có thêm kho, thêm vào DB
            foreach (DataGridViewRow row in dgv_StockList.Rows) {
                if (row.Cells["StockList_Id"].Value == DBNull.Value)
                {
                    Stock stock = Util.MapRowTo<Stock>(row);
                    Database.AddRecord<Stock>("StockList", stock);
                }
            }
            // đóng hộp thoại
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
