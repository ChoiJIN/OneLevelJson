﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OneLevelJson
{
    public partial class ContextRenameForm : Form
    {
        public string Result { get; private set; }

        public ContextRenameForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Result = this.textBox1.Text;
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }
    }
}
