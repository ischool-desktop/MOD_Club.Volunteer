﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FISCA.Presentation.Controls;
using Aspose.Words;
using System.IO;
using FISCA.DSAUtil;
using FISCA.UDT;
using K12.Data;
using System.Xml;
using System.Diagnostics;

namespace K12.Club.Volunteer
{
    public partial class ClubPointsListForm : BaseForm
    {
        /// <summary>
        /// 樣版
        /// </summary>
        string ClassPrint_Config_1 = "K12.Club.General.ClubPointsListForm.cs";

        BackgroundWorker BGW = new BackgroundWorker();

        int 學生多少個 = 150;
        int 日期多少天 = 30;

        public ClubPointsListForm()
        {
            InitializeComponent();

        }

        private void ClubPointsListForm_Load(object sender, EventArgs e)
        {
            BGW.DoWork += new DoWorkEventHandler(BGW_DoWork);
            BGW.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BGW_RunWorkerCompleted);

            dateTimeInput1.Value = DateTime.Today;
            dateTimeInput2.Value = DateTime.Today.AddDays(6);

            GetDateTime_Click(null, null);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (ClubAdmin.Instance.SelectedSource.Count == 0)
            {
                MsgBox.Show("請選擇社團!!");
                return;
            }

            if (BGW.IsBusy)
            {
                MsgBox.Show("忙碌中,稍後再試!!");
                return;
            }

            #region 日期設定

            if (dataGridViewX1.Rows.Count <= 0)
            {
                MsgBox.Show("列印點名單必須有日期!!");
                return;
            }

            DSXmlHelper dxXml = new DSXmlHelper("XmlData");

            foreach (DataGridViewRow row in dataGridViewX1.Rows)
            {
                string 日期 = "" + row.Cells[0].Value;
                dxXml.AddElement(".", "item", 日期);
            }

            #endregion

            btnSave.Enabled = false;
            BGW.RunWorkerAsync(dxXml.BaseElement);

        }

        void BGW_DoWork(object sender, DoWorkEventArgs e)
        {
            Campus.Report.ReportConfiguration ConfigurationInCadre = new Campus.Report.ReportConfiguration(ClassPrint_Config_1);
            Aspose.Words.Document Template;

            if (ConfigurationInCadre.Template == null)
            {
                //如果範本為空,則建立一個預設範本
                Campus.Report.ReportConfiguration ConfigurationInCadre_1 = new Campus.Report.ReportConfiguration(ClassPrint_Config_1);
                ConfigurationInCadre_1.Template = new Campus.Report.ReportTemplate(Properties.Resources.社團點名單_套表列印, Campus.Report.TemplateType.Word);
                //ConfigurationInCadre_1.Template = new Campus.Report.ReportTemplate(Properties.Resources.社團點名表_合併欄位總表, Campus.Report.TemplateType.Word);
                Template = ConfigurationInCadre_1.Template.ToDocument();
            }
            else
            {
                //如果已有範本,則取得樣板
                Template = ConfigurationInCadre.Template.ToDocument();
            }

            SCJoinDataLoad crM = new SCJoinDataLoad();

            #region 日期

            List<string> config = new List<string>();

            XmlElement day = (XmlElement)e.Argument;

            if (day == null)
            {
                MsgBox.Show("第一次使用報表請先進行[日期設定]");
                return;
            }
            else
            {
                config.Clear();
                foreach (XmlElement xml in day.SelectNodes("item"))
                {
                    config.Add(xml.InnerText);
                }
            }

            #endregion

            DataTable table = new DataTable();
            table.Columns.Add("學校名稱");
            table.Columns.Add("社團名稱");
            table.Columns.Add("學年度");
            table.Columns.Add("學期");

            table.Columns.Add("上課地點");
            table.Columns.Add("社團類型");
            table.Columns.Add("指導老師1");
            table.Columns.Add("指導老師2");
            table.Columns.Add("指導老師3");

            table.Columns.Add("列印日期");
            table.Columns.Add("上課開始");
            table.Columns.Add("上課結束");
            table.Columns.Add("人數");

            for (int x = 1; x <= 日期多少天; x++)
            {
                table.Columns.Add(string.Format("日期_{0}", x));
            }

            for (int x = 1; x <= 學生多少個; x++)
            {
                table.Columns.Add(string.Format("班級_{0}", x));
            }

            for (int x = 1; x <= 學生多少個; x++)
            {
                table.Columns.Add(string.Format("座號_{0}", x));
            }

            for (int x = 1; x <= 學生多少個; x++)
            {
                table.Columns.Add(string.Format("姓名_{0}", x));
            }

            for (int x = 1; x <= 學生多少個; x++)
            {
                table.Columns.Add(string.Format("學號_{0}", x));
            }

            for (int x = 1; x <= 學生多少個; x++)
            {
                table.Columns.Add(string.Format("性別_{0}", x));
            }

            foreach (string each in crM.CLUBRecordDic.Keys)
            {
                //社團資料
                CLUBRecord cr = crM.CLUBRecordDic[each];

                DataRow row = table.NewRow();
                row["學校名稱"] = K12.Data.School.ChineseName;
                row["社團名稱"] = cr.ClubName;
                row["學年度"] = cr.SchoolYear;
                row["學期"] = cr.Semester;

                row["上課地點"] = cr.Location;
                row["社團類型"] = cr.ClubCategory;
                if (crM.TeacherDic.ContainsKey(cr.RefTeacherID))
                {
                    row["指導老師1"] = crM.TeacherDic[cr.RefTeacherID].Name;
                }
                if (crM.TeacherDic.ContainsKey(cr.RefTeacherID2))
                {
                    row["指導老師2"] = crM.TeacherDic[cr.RefTeacherID2].Name;
                }
                if (crM.TeacherDic.ContainsKey(cr.RefTeacherID3))
                {
                    row["指導老師3"] = crM.TeacherDic[cr.RefTeacherID3].Name;
                }

                //row["外聘老師"] = "";

                row["列印日期"] = DateTime.Today.ToShortDateString();
                row["上課開始"] = config[0];
                row["上課結束"] = config[config.Count - 1];
                row["人數"] = crM.ClubByStudentList[each].Count;

                for (int x = 1; x <= config.Count; x++)
                {
                    row[string.Format("日期_{0}", x)] = config[x - 1];
                }

                int y = 1;
                foreach (StudentRecord obj in crM.ClubByStudentList[each])
                {
                    if (y <= 學生多少個) //限制畫面到100名學生
                    {
                        row[string.Format("班級_{0}", y)] = obj.Class != null ? obj.Class.Name : "";
                        row[string.Format("座號_{0}", y)] = obj.SeatNo.HasValue ? obj.SeatNo.Value.ToString() : "";
                        row[string.Format("姓名_{0}", y)] = obj.Name;
                        row[string.Format("學號_{0}", y)] = obj.StudentNumber;
                        row[string.Format("性別_{0}", y)] = obj.Gender;
                        y++;
                    }
                }

                table.Rows.Add(row);
            }

            Document PageOne = (Document)Template.Clone(true);
            PageOne.MailMerge.Execute(table);
            e.Result = PageOne;
        }

        void BGW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnSave.Enabled = true;

            if (e.Cancelled)
            {
                MsgBox.Show("作業已被中止!!");
            }
            else
            {
                if (e.Error == null)
                {
                    Document inResult = (Document)e.Result;

                    try
                    {
                        SaveFileDialog SaveFileDialog1 = new SaveFileDialog();

                        SaveFileDialog1.Filter = "Word (*.doc)|*.doc|所有檔案 (*.*)|*.*";
                        SaveFileDialog1.FileName = "社團點名單(套表列印)";

                        if (SaveFileDialog1.ShowDialog() == DialogResult.OK)
                        {
                            inResult.Save(SaveFileDialog1.FileName);
                            Process.Start(SaveFileDialog1.FileName);
                        }
                        else
                        {
                            FISCA.Presentation.Controls.MsgBox.Show("檔案未儲存");
                            return;
                        }
                    }
                    catch
                    {
                        FISCA.Presentation.Controls.MsgBox.Show("檔案儲存錯誤,請檢查檔案是否開啟中!!");
                        return;
                    }

                    this.Close();
                }
                else
                {
                    MsgBox.Show("列印資料發生錯誤\n" + e.Error.Message);
                }
            }
        }

        private void GetDateTime_Click(object sender, EventArgs e)
        {
            //建立日期清單
            TimeSpan ts = dateTimeInput2.Value - dateTimeInput1.Value;

            List<DateTime> TList = new List<DateTime>();

            foreach (DataGridViewRow row in dataGridViewX1.Rows)
            {
                DateTime dt;
                DateTime.TryParse("" + row.Cells[0].Value, out dt);
                TList.Add(dt);
            }

            List<DayOfWeek> WeekList = new List<DayOfWeek>();
            if (cbDay1.Checked)
                WeekList.Add(DayOfWeek.Monday);
            if (cbDay2.Checked)
                WeekList.Add(DayOfWeek.Tuesday);
            if (cbDay3.Checked)
                WeekList.Add(DayOfWeek.Wednesday);
            if (cbDay4.Checked)
                WeekList.Add(DayOfWeek.Thursday);
            if (cbDay5.Checked)
                WeekList.Add(DayOfWeek.Friday);

            for (int x = 0; x <= ts.Days; x++)
            {
                DateTime dt = dateTimeInput1.Value.AddDays(x);

                if (WeekList.Contains(dt.DayOfWeek))
                {
                    if (!TList.Contains(dt))
                    {
                        TList.Add(dt);
                    }
                }
            }

            TList.Sort();
            //資料填入
            dataGridViewX1.Rows.Clear();
            foreach (DateTime dt in TList)
            {
                DataGridViewRow row = new DataGridViewRow();
                row.CreateCells(dataGridViewX1);
                row.Cells[0].Value = dt.ToShortDateString();
                row.Cells[1].Value = CheckWeek(dt.DayOfWeek.ToString());
                dataGridViewX1.Rows.Add(row);
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            //取得設定檔
            Campus.Report.ReportConfiguration ConfigurationInCadre = new Campus.Report.ReportConfiguration(ClassPrint_Config_1);
            Campus.Report.TemplateSettingForm TemplateForm;
            //畫面內容(範本內容,預設樣式
            if (ConfigurationInCadre.Template != null)
            {
                TemplateForm = new Campus.Report.TemplateSettingForm(ConfigurationInCadre.Template, new Campus.Report.ReportTemplate(Properties.Resources.社團點名單_套表列印, Campus.Report.TemplateType.Word));
            }
            else
            {
                ConfigurationInCadre.Template = new Campus.Report.ReportTemplate(Properties.Resources.社團點名單_套表列印, Campus.Report.TemplateType.Word);
                TemplateForm = new Campus.Report.TemplateSettingForm(ConfigurationInCadre.Template, new Campus.Report.ReportTemplate(Properties.Resources.社團點名單_套表列印, Campus.Report.TemplateType.Word));
            }

            //預設名稱
            TemplateForm.DefaultFileName = "社團點名單(套表列印範本)";

            //如果回傳為OK
            if (TemplateForm.ShowDialog() == DialogResult.OK)
            {
                //設定後樣試,回傳
                ConfigurationInCadre.Template = TemplateForm.Template;
                //儲存
                ConfigurationInCadre.Save();
            }
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "另存新檔";
            sfd.FileName = "社團點名表_合併欄位總表.doc";
            sfd.Filter = "Word檔案 (*.doc)|*.doc|所有檔案 (*.*)|*.*";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    FileStream fs = new FileStream(sfd.FileName, FileMode.Create);
                    fs.Write(Properties.Resources.社團點名表_合併欄位總表, 0, Properties.Resources.社團點名表_合併欄位總表.Length);
                    fs.Close();
                    System.Diagnostics.Process.Start(sfd.FileName);
                }
                catch
                {
                    FISCA.Presentation.Controls.MsgBox.Show("指定路徑無法存取。", "另存檔案失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        /// <summary>
        /// 依編號取代為星期
        /// </summary>
        public static string CheckWeek(string x)
        {
            if (x == "Monday")
            {
                return "一";
            }
            else if (x == "Tuesday")
            {
                return "二";
            }
            else if (x == "Wednesday")
            {
                return "三";
            }
            else if (x == "Thursday")
            {
                return "四";
            }
            else if (x == "Friday")
            {
                return "五";
            }
            else if (x == "Saturday")
            {
                return "六";
            }
            else
            {
                return "日";
            }
        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            dataGridViewX1.Rows.Clear();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
