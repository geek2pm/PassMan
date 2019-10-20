using System;
using System.Windows.Forms;

namespace PassMan
{
    public partial class FormMessage : Form
    {
        public FormMessage()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(textBox1.Text))
            {
                FormMain.newfilename = textBox1.Text + ".xxx";
            }
            Dispose();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FormMain.newfilename = null;
            Dispose();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button1_Click(this, new EventArgs());
            }
        }
    }
}
