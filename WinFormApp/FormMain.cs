using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormApp
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        private void textEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormEditor newMDIChild = new FormEditor();
            // Set the Parent Form of the Child window.
            newMDIChild.MdiParent = this;
            newMDIChild.Dock = DockStyle.Fill;
            // Display the new form.
            newMDIChild.Show();
        }
    }
}
