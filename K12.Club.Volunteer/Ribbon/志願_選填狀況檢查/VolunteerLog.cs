﻿using FISCA.Presentation.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace K12.Club.Volunteer
{
    public partial class VolunteerLog : BaseForm
    {
        public VolunteerLog(string log)
        {
            InitializeComponent();

            textLog.Text = log;
        }
    }
}
