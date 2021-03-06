﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EPubMaker
{
    public partial class FormMove : Form
    {
        public int MaxValue
        {
            set
            {
                editMove.Maximum = value;
            }
        }

        public int Page
        {
            set
            {
                editMove.Value = value;
            }
            get
            {
                return (int)editMove.Value;
            }
        }

        public FormMove()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
