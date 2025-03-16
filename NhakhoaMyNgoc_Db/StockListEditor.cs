using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            bsStockList.Filter = "StockList_IsActive = 1";
            dgv_StockList.DataSource = bsStockList;

            Util.AttachUpdateHook(dgv_StockList, "StockList");
            Util.AttachDeleteHook(dgv_StockList, "StockList", false);
            tsi_Restore.Click += (s, ev) => Util.AttachRestoreHook(dgv_StockList, "StockList");
        }

        private void cbStockList_IsActive_CheckedChanged(object sender, EventArgs e)
        {
            if (cbStockList_IsActive.Checked)
                bsStockList.RemoveFilter();
            else
                bsStockList.Filter = "StockList_IsActive = 1";
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
