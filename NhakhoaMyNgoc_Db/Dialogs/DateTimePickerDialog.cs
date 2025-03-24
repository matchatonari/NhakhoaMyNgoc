using System;
using System.Drawing;
using System.Windows.Forms;

namespace NhakhoaMyNgoc_Db
{
    public partial class DateTimePickerDialog : Form
    {
        public DateTime SelectedDate { get; private set; }

        public DateTimePickerDialog(DateTime initialDateTime)
        {
            InitializeComponent();

            mcCalendar.SelectionStart = initialDateTime.Date;
            dtpkTime.Value = new DateTime(2000, 1, 1).Add(initialDateTime.TimeOfDay);
        }

        private void DateTimePickerDialog_Load(object sender, EventArgs e)
        {
            dtpkTime.ValueChanged += (s, ev) => pnlClock.Invalidate();
        }

        private void pnlClock_Paint(object sender, PaintEventArgs e)
        {            
            Pen pen = new Pen(Color.Black, 2);
            Pen secondHandPen = new Pen(Color.DarkRed, 1);

            Point clockCenter = new Point(pnlClock.Width / 2, pnlClock.Height / 2);

            int hourHandLength = pnlClock.Width / 5;
            int minuteHandLength = pnlClock.Width / 3;
            int secondHandLength = pnlClock.Width / 3;

            double hourAngleRadians = (30.0 * (dtpkTime.Value.Hour % 12) + 0.5 * dtpkTime.Value.Minute) * Math.PI / 180.0;
            double minuteAngleRadians = 6.0 * dtpkTime.Value.Minute * Math.PI / 180.0;
            double secondAngleRadians = 6.0 * dtpkTime.Value.Second * Math.PI / 180.0;

            Point hourHand = new Point(
                clockCenter.X + Convert.ToInt32(hourHandLength * Math.Sin(hourAngleRadians)),
                clockCenter.Y - Convert.ToInt32(hourHandLength * Math.Cos(hourAngleRadians))
            );

            Point minuteHand = new Point(
                clockCenter.X + Convert.ToInt32(minuteHandLength * Math.Sin(minuteAngleRadians)),
                clockCenter.Y - Convert.ToInt32(minuteHandLength * Math.Cos(minuteAngleRadians))
            );

            Point secondHand = new Point(
                clockCenter.X + Convert.ToInt32(secondHandLength * Math.Sin(secondAngleRadians)),
                clockCenter.Y - Convert.ToInt32(secondHandLength * Math.Cos(secondAngleRadians))
            );

            e.Graphics.DrawEllipse(pen, pen.Width, pen.Width, pnlClock.Width - pen.Width * 2, pnlClock.Height - pen.Width * 2);

            e.Graphics.DrawLine(pen, clockCenter, hourHand);
            e.Graphics.DrawLine(pen, clockCenter, minuteHand);
            e.Graphics.DrawLine(secondHandPen, clockCenter, secondHand);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            DateTime selectedDate = mcCalendar.SelectionStart.Date;
            DateTime selectedTime = dtpkTime.Value;
            SelectedDate = selectedDate.Add(selectedTime.TimeOfDay);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
