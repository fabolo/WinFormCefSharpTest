using FB.UserControls;
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
    public partial class FormEditor : Form
    {
        public FormEditor()
        {
            InitializeComponent();
        }

        private void FormEditor_Load(object sender, EventArgs e)
        {
            var editor = new TinyMCEeditor(Application.StartupPath+"/TinyMCEeditor.html", true);
            SuspendLayout(); // non risolve ma lo lascio
            panel1.Controls.Add(editor);
            editor.Dock = DockStyle.Fill;
            ResumeLayout();
        }
    }
}
