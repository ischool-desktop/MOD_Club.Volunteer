﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FISCA.Presentation.Controls;
using K12.Data;
using System.Xml;

namespace K12.Club.Volunteer
{
    public partial class VolunteerClassForm : BaseForm
    {
        BackgroundWorker BGW = new BackgroundWorker();

        BackgroundWorker BGW_Save = new BackgroundWorker();

        Setup_ByV By_V { get; set; }

        //畫面上的Row資料
        List<社團志願分配的Row> _RowList { get; set; }

        //取得狀態合理之學生基本資料(狀態一般/延修)
        List<一名學生> StudentList { get; set; }

        //取得狀態合理之學生基本資料(狀態一般/延修)
        Dictionary<string, 一名學生> StudentDic { get; set; }

        List<string> grade_yearList = new List<string>() { "1", "2", "3", "7", "8", "9", "10", "11", "12" };

        //取得目前系統內社團學生資料
        Dictionary<string, SCJoin> SCJLockDic { get; set; }

        Dictionary<string, CLUBRecord> CLUBDic { get; set; }

        Dictionary<string, 一個社團檢查> CLUBCheckDic { get; set; }

        List<SCJoin> DeleteList = new List<SCJoin>();
        List<SCJoin> InsertList1 = new List<SCJoin>();
        List<SCJoin> InsertList2 = new List<SCJoin>();

        bool Is社團已分配 = false;

        public VolunteerClassForm()
        {
            InitializeComponent();
        }

        private void VolunteerClassForm_Load(object sender, EventArgs e)
        {
            BGW.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BGW_RunWorkerCompleted);
            BGW.DoWork += new DoWorkEventHandler(BGW_DoWork);
            BGW.ProgressChanged += new ProgressChangedEventHandler(BGW_ProgressChanged);
            BGW.WorkerReportsProgress = true;

            BGW_Save.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BGW_Save_RunWorkerCompleted);
            BGW_Save.ProgressChanged += new ProgressChangedEventHandler(BGW_Save_ProgressChanged);
            BGW_Save.DoWork += new DoWorkEventHandler(BGW_Save_DoWork);
            BGW_Save.WorkerReportsProgress = true;

            btnRunStart.Enabled = false;

            this.Text = "社團志願分配(資料取得中)";
            BGW.RunWorkerAsync();
        }

        void BGW_DoWork(object sender, DoWorkEventArgs e)
        {
            BGW.ReportProgress(0, "取得社團志願設定...");
            By_V = new Setup_ByV();

            //取得狀態合理之學生基本資料(狀態一般/延修)
            BGW.ReportProgress(15, "取得學生基本資料...");
            StudentList = GetVolunteerData.GetStudentData();
            StudentDic = GetVolunteerData.GetStudentDic(StudentList);

            //取得畫面可顯示的Row資料
            BGW.ReportProgress(25, "建立班級學生資料...");
            Dictionary<string, 社團志願分配的Row> VolClassRowDic = GetVolClassRow(StudentList);

            //取得學生選社物件
            BGW.ReportProgress(35, "取得學生志願資料...");
            Dictionary<string, VolunteerRecord> VolDic = GetVolunteerData.GetVolunteerDic();

            //本學年度學期的社團清單
            BGW.ReportProgress(45, "取得本期社團資料...");
            CLUBDic = GetVolunteerData.GetSchoolYearClub();

            //取得目前系統內Lock之學生清單
            BGW.ReportProgress(53, "取得社團學生資料...");
            SCJLockDic = GetVolunteerData.GetSCJDic(CLUBDic.Keys.ToList());

            //社團選社限制檢查工具
            BGW.ReportProgress(60, "取得社團選社限制...");
            CLUBCheckDic = GetCLUBCheckDic();

            Increase(CLUBCheckDic, SCJLockDic);

            BGW.ReportProgress(75, "建立畫面資料樣式...");
            //取出班級
            foreach (社團志願分配的Row each in VolClassRowDic.Values)
            {
                //取出學生
                foreach (一名學生 student in each._StudentDic.Values)
                {
                    //學生志願選填分配
                    if (VolDic.ContainsKey(student.student_id))
                    {
                        each._Volunteer.Add(student.student_id, VolDic[student.student_id]);
                    }

                    //學生已參與社團分配
                    if (SCJLockDic.ContainsKey(student.student_id))
                    {
                        each._SCJDic.Add(student.student_id, SCJLockDic[student.student_id]);

                        //順便取得此社團記錄
                        if (CLUBDic.ContainsKey(SCJLockDic[student.student_id].RefClubID))
                        {
                            CLUBRecord club = CLUBDic[SCJLockDic[student.student_id].RefClubID];
                            if (!each._ClubDic.ContainsKey(club.UID))
                            {
                                each._ClubDic.Add(club.UID, club);
                            }
                        }
                    }

                }
            }

            _RowList = VolClassRowDic.Values.ToList();
            _RowList.Sort(SortClass);
            BGW.ReportProgress(100, "學生志願檢查完成!");
        }

        private void Increase(Dictionary<string, 一個社團檢查> CLUBCheckDic, Dictionary<string, SCJoin> SCJLockDic)
        {
            foreach (SCJoin stud in SCJLockDic.Values)
            {
                if (CLUBCheckDic.ContainsKey(stud.RefClubID))
                {
                    if (StudentDic.ContainsKey(stud.RefStudentID))
                    {
                        一個社團檢查 c =CLUBCheckDic[stud.RefClubID];
                        一名學生 s = StudentDic[stud.RefStudentID];
                        SetClubGradeYearCount(c, s, true);
                    }
                }
            }
        }

        /// <summary>
        /// 建立可快速取得檢查資料的字典
        /// 社團UID:社團檢查
        /// 使用的全域資料:
        /// CLUBDic / SCJLockDic / StudentDic
        /// </summary>
        private Dictionary<string, 一個社團檢查> GetCLUBCheckDic()
        {

            Dictionary<string, 一個社團檢查> dic = new Dictionary<string, 一個社團檢查>();
            foreach (CLUBRecord each in CLUBDic.Values)
            {
                if (!dic.ContainsKey(each.UID))
                {
                    一個社團檢查 一個 = new 一個社團檢查(each);
                    dic.Add(each.UID, 一個);
                }
            }

            return dic;
        }

        void BGW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnRunStart.Enabled = true;
            btnExit.Enabled = true;
            this.Text = "社團志願分配";
            if (e.Error == null)
            {
                if (!e.Cancelled)
                {
                    FISCA.Presentation.MotherForm.SetStatusBarMessage("學生志願檢查完成");
                    dataGridViewX1.AutoGenerateColumns = false;
                    dataGridViewX1.DataSource = _RowList;
                }
                else
                {
                    MsgBox.Show("資料取得作業已取消!!");
                }
            }
            else
            {
                MsgBox.Show("資料取得錯誤!!");
            }
        }

        /// <summary>
        /// 開始進行社團志願分配
        /// </summary>
        private void btnRunStart_Click(object sender, EventArgs e)
        {
            if (!BGW_Save.IsBusy)
            {
                btnRunStart.Enabled = false;
                btnExit.Enabled = false;
                btnRunStart.Text = "開始分配(執行中)";
                BGW_Save.RunWorkerAsync();
                Is社團已分配 = true;
            }
            else
            {
                MsgBox.Show("系統忙碌中\n請稍後...");
            }
        }

        void BGW_Save_DoWork(object sender, DoWorkEventArgs e)
        {

            BGW_Save.ReportProgress(0, "志願分配作業...");
            //已有社團參與者略過或覆蓋
            DeleteList = new List<SCJoin>();
            InsertList1 = new List<SCJoin>();
            InsertList2 = new List<SCJoin>();


            //增加退社相關作業(2015/9/14)
            if (By_V.已有社團記錄時) //有社團記錄時,不進行退社(True覆蓋,False略過)
            {
                //進行已選學生的退社處理
                foreach (DataGridViewRow row in dataGridViewX1.SelectedRows)
                {
                    if (row.DataBoundItem is 社團志願分配的Row)
                    {
                        社團志願分配的Row 班級 = (社團志願分配的Row)row.DataBoundItem;
                        foreach (一名學生 一學生 in 班級._StudentDic.Values)
                        {
                            if (SCJLockDic.ContainsKey(一學生.student_id))
                            {
                                SCJoin scj_del = SCJLockDic[一學生.student_id];
                                if (!scj_del.Lock) //不是鎖定的學生
                                {
                                    scj_del.Deleted = true;
                                    scj_del.Save();

                                    //設定社團人數
                                    一個社團檢查 一社團 = CLUBCheckDic[scj_del.RefClubID];
                                    SetClubGradeYearCount(一社團, 一學生, false);
                                }
                            }
                        }
                    }
                }
            }


            #region 社團參與依據

            //用條件進行排序的物件
            List<學生選社亂數檔> RunList = new List<學生選社亂數檔>();

            if (By_V.社團分配優先序)
            {
                #region 使用獎懲依據
                BGW_Save.ReportProgress(10, "取得獎懲資料...");
                獎懲換算機制 m = new 獎懲換算機制();

                BGW_Save.ReportProgress(20, "取得資料模型...");
                Dictionary<string, int> StudentMeritDic = m.GetMerit(StudentList);

                BGW_Save.ReportProgress(20, "取得資料模型...");

                List<社團志願分配的Row> IsRowList = new List<社團志願分配的Row>();
                foreach (DataGridViewRow row in dataGridViewX1.SelectedRows)
                {
                    if (row.DataBoundItem is 社團志願分配的Row)
                    {
                        IsRowList.Add((社團志願分配的Row)row.DataBoundItem);
                    }
                }
                foreach (社團志願分配的Row each in IsRowList)
                {
                    foreach (VolunteerRecord vol in each._Volunteer.Values)
                    {
                        if (StudentMeritDic.ContainsKey(vol.RefStudentID))
                        {
                            學生選社亂數檔 rr = new 學生選社亂數檔(vol, StudentMeritDic[vol.RefStudentID]);
                            RunList.Add(rr);
                        }
                    }
                }
                #endregion
            }
            else
            {
                #region 使用亂數依據
                BGW_Save.ReportProgress(10, "使用亂數依據...");

                Random _r = new Random();

                BGW_Save.ReportProgress(20, "取得資料模型...");
                List<社團志願分配的Row> IsRowList = new List<社團志願分配的Row>();
                foreach (DataGridViewRow row in dataGridViewX1.SelectedRows)
                {
                    if (row.DataBoundItem is 社團志願分配的Row)
                    {
                        IsRowList.Add((社團志願分配的Row)row.DataBoundItem);
                    }
                }
                foreach (社團志願分配的Row each in IsRowList)
                {
                    foreach (VolunteerRecord vol in each._Volunteer.Values)
                    {
                        學生選社亂數檔 rr = new 學生選社亂數檔(vol, _r.Next(99999));
                        RunList.Add(rr);
                    }
                }
                #endregion
            }

            BGW_Save.ReportProgress(30, "資料排序作業...");
            RunList.Sort(SortRandom);

            #endregion

            if (!By_V.已有社團記錄時) //True為覆蓋
            {
                //要略過的話
                //要先進行資料的檢查            
                SetCount(RunList);
            }

            #region 開始分配學生社團狀態(相關狀態與學生都應該已處理)

            //取志願
            BGW_Save.ReportProgress(45, "志願選填作業...");
            for (int x = 1; x <= By_V.學生選填志願數; x++)
            {
                BGW_Save.ReportProgress(65, string.Format("志願選填作業...{0}", x));
                Judge(RunList, x);
            }

            #endregion

            //新增資料

            if (By_V.已有社團記錄時) //覆蓋
            {
                BGW_Save.ReportProgress(77, "清除學生選社...");
                //刪除學生社團參與記錄
                tool._A.DeletedValues(DeleteList);

                //新增未選社之學生
                BGW_Save.ReportProgress(84, "新增選社記錄..1");
                tool._A.InsertValues(InsertList1);
                //新增已選社之學生
                BGW_Save.ReportProgress(88, "新增選社記錄..2");
                tool._A.InsertValues(InsertList2);
            }
            else //略過資料
            {
                BGW_Save.ReportProgress(85, "新增選社記錄..1");
                //只新增未選社之學生
                tool._A.InsertValues(InsertList1);
            }
            BGW_Save.ReportProgress(100, "選社分配完成!!");
        }

        private void SetCount(List<學生選社亂數檔> RunList)
        {
            foreach (學生選社亂數檔 ran in RunList)
            {
                //社團選社資料
                VolunteerRecord vr = ran._record;
                string StudentID = vr.RefStudentID;

                //是否有社團記錄
                if (!SCJLockDic.ContainsKey(StudentID))
                    continue;
                SCJoin scj = SCJLockDic[StudentID];

                //社團記錄是否有該社團
                if (!CLUBCheckDic.ContainsKey(scj.RefClubID))
                    continue;
                一個社團檢查 一社團 = CLUBCheckDic[scj.RefClubID];

                //是否為學生
                if (!StudentDic.ContainsKey(StudentID))
                    continue;
                一名學生 一學生 = StudentDic[StudentID];


                if (grade_yearList.Contains(一學生.grade_year))
                {
                    ran.AllocationSucceeds = true;
                }

                SetClubGradeYearCount(一社團, 一學生, true);

            }
        }

        private void SetClubGradeYearCount(一個社團檢查 一社團, 一名學生 一學生, bool IncreaseOrDecrease)
        {

            if (一學生.grade_year == "1" || 一學生.grade_year == "7" || 一學生.grade_year == "10")
            {
                if (IncreaseOrDecrease)
                {
                    一社團._Now_ClubStudentCount++;
                    一社團._Now_GradeYear1++;
                }
                else
                {
                    一社團._Now_ClubStudentCount--;
                    一社團._Now_GradeYear1--;
                }
            }
            else if (一學生.grade_year == "2" || 一學生.grade_year == "8" || 一學生.grade_year == "11")
            {
                if (IncreaseOrDecrease)
                {
                    一社團._Now_ClubStudentCount++;
                    一社團._Now_GradeYear2++;
                }
                else
                {
                    一社團._Now_ClubStudentCount--;
                    一社團._Now_GradeYear2--;
                }
            }
            else if (一學生.grade_year == "3" || 一學生.grade_year == "9" || 一學生.grade_year == "12")
            {
                if (IncreaseOrDecrease)
                {
                    一社團._Now_ClubStudentCount++;
                    一社團._Now_GradeYear3++;
                }
                else
                {
                    一社團._Now_ClubStudentCount--;
                    一社團._Now_GradeYear3--;
                }
            }
        }

        /// <summary>
        /// 傳入分配單位
        /// 與分配序
        /// </summary>
        private void Judge(List<學生選社亂數檔> RunList, int FirstChoice)
        {
            foreach (學生選社亂數檔 ran in RunList)
            {
                //判斷志願選填狀態
                if (!ran.AllocationSucceeds)
                {
                    //第一志願
                    if (Allocation(ran, FirstChoice))
                    {
                        //有成功取得社團記錄,表示志願選填成功
                        //如為覆蓋 - 也要跑一次資料整理
                        ran.AllocationSucceeds = true;
                    }
                    else
                    {

                    }
                }
            }
        }

        /// <summary>
        /// 傳入分配
        /// </summary>
        private bool Allocation(學生選社亂數檔 ran, int NumberIndex)
        {
            //社團選社資料
            VolunteerRecord vr = ran._record;

            XmlElement xml = XmlHelper.LoadXml(vr.Content);

            //每一個社團
            foreach (XmlElement each in xml.SelectNodes("Club"))
            {
                int index = 0;
                int.TryParse(each.GetAttribute("Index"), out index);
                //當進行第一輪志願分配時
                if (index != NumberIndex)
                    continue;

                string clubID = each.GetAttribute("Ref_Club_ID");
                string StudentID = vr.RefStudentID;

                //2013/4/8號
                //需建立一個ClubCount字典
                //覆蓋 - 把本期除了鎖定學生之社團記錄清除
                //略過 - 本期的社團記錄不予更動

                //1.須檢查目前系統是否有要加入的社團(例如該社團被選社後刪除)
                //2.目前此社團是否已額滿或目前社團人數為可加入狀態
                if (!CLUBDic.ContainsKey(clubID))
                    continue;

                //不存在社團記錄,表示可以新增
                if (!SCJLockDic.ContainsKey(StudentID))
                {
                    #region 不存在社團記錄,表示可以新增
                    SCJoin scj = new SCJoin();
                    一個社團檢查 一社團 = CLUBCheckDic[clubID];

                    if (!一社團.人數未滿)
                        continue;

                    if (StudentDic.ContainsKey(StudentID))
                    {
                        一名學生 一學生 = StudentDic[StudentID];

                        //你必須是本社團科別限制之學生
                        //Count大於0,表示有科別限制
                        if (一社團.DeptList.Count > 0)
                        {
                            if (!一社團.DeptList.Contains(一學生.dept_name))
                            {
                                //本社團選社失敗
                                continue;
                            }
                        }

                        if (一社團.男女限制 == 一學生.gender || 一社團.男女限制 == GetVolunteerData.男女.不限制)
                        {
                            if (一學生.grade_year == "1" || 一學生.grade_year == "7" || 一學生.grade_year == "10")
                            {
                                if (一社團.一年級未滿)
                                {
                                    scj.RefClubID = clubID;
                                    scj.RefStudentID = StudentID;

                                    一社團._Now_ClubStudentCount++;
                                    一社團._Now_GradeYear1++;

                                    InsertList1.Add(scj);
                                    return true;
                                }
                            }
                            else if (一學生.grade_year == "2" || 一學生.grade_year == "8" || 一學生.grade_year == "11")
                            {
                                if (一社團.二年級未滿)
                                {
                                    scj.RefClubID = clubID;
                                    scj.RefStudentID = StudentID;

                                    一社團._Now_ClubStudentCount++;
                                    一社團._Now_GradeYear2++;

                                    InsertList1.Add(scj);
                                    return true;
                                }
                            }
                            else if (一學生.grade_year == "3" || 一學生.grade_year == "9" || 一學生.grade_year == "12")
                            {
                                if (一社團.三年級未滿)
                                {
                                    scj.RefClubID = clubID;
                                    scj.RefStudentID = StudentID;

                                    一社團._Now_ClubStudentCount++;
                                    一社團._Now_GradeYear3++;

                                    InsertList1.Add(scj);
                                    return true;
                                }
                            }
                        }
                    }
                    #endregion
                }
                else //已存在,要判斷是覆蓋還是略過
                {
                    #region 已存在,要判斷是覆蓋還是略過
                    //已有社團記錄時 - 覆蓋
                    if (By_V.已有社團記錄時)
                    {
                        #region 覆蓋
                        SCJoin scj_del = SCJLockDic[StudentID];

                        if (!scj_del.Lock)
                        {
                            #region 未鎖定
                            //新增一筆資料
                            SCJoin scj = new SCJoin();

                            //因為覆蓋所以 - 列入刪除
                            if (!DeleteList.Contains(scj_del))
                            {
                                DeleteList.Add(scj_del);
                            }

                            一個社團檢查 一社團 = CLUBCheckDic[clubID];
                            if (!一社團.人數未滿)
                                continue;

                            if (StudentDic.ContainsKey(StudentID))
                            {
                                一名學生 一學生 = StudentDic[StudentID];

                                //你必須是本社團科別限制之學生
                                //Count大於0,表示有科別限制
                                if (一社團.DeptList.Count > 0)
                                {
                                    if (!一社團.DeptList.Contains(一學生.dept_name))
                                    {
                                        //本社團選社失敗
                                        continue;
                                    }
                                }

                                if (一社團.男女限制 == 一學生.gender || 一社團.男女限制 == GetVolunteerData.男女.不限制)
                                {
                                    if (一學生.grade_year == "1" || 一學生.grade_year == "7")
                                    {
                                        if (一社團.一年級未滿)
                                        {
                                            scj.RefStudentID = StudentID;
                                            scj.RefClubID = clubID;

                                            一社團._Now_ClubStudentCount++;
                                            一社團._Now_GradeYear1++;

                                            InsertList2.Add(scj);
                                            return true;
                                        }
                                    }
                                    else if (一學生.grade_year == "2" || 一學生.grade_year == "8")
                                    {
                                        if (一社團.二年級未滿)
                                        {
                                            scj.RefStudentID = StudentID;
                                            scj.RefClubID = clubID;

                                            一社團._Now_ClubStudentCount++;
                                            一社團._Now_GradeYear2++;

                                            InsertList2.Add(scj);
                                            return true;
                                        }
                                    }
                                    else if (一學生.grade_year == "3" || 一學生.grade_year == "9")
                                    {
                                        if (一社團.三年級未滿)
                                        {
                                            scj.RefStudentID = StudentID;
                                            scj.RefClubID = clubID;

                                            一社團._Now_ClubStudentCount++;
                                            一社團._Now_GradeYear3++;

                                            InsertList2.Add(scj);
                                            return true;
                                        }
                                    }
                                }

                            }
                            #endregion
                        }
                        else
                        {
                            //取得原社團,進行資料登記
                            #region 鎖定
                            一個社團檢查 一社團 = CLUBCheckDic[scj_del.RefClubID];

                            if (StudentDic.ContainsKey(StudentID))
                            {
                                一名學生 一學生 = StudentDic[StudentID];

                                if (一學生.grade_year == "1" || 一學生.grade_year == "7")
                                {
                                    一社團._Now_ClubStudentCount++;
                                    一社團._Now_GradeYear1++;
                                    return true;

                                }
                                else if (一學生.grade_year == "2" || 一學生.grade_year == "8")
                                {
                                    一社團._Now_ClubStudentCount++;
                                    一社團._Now_GradeYear2++;
                                    return true;

                                }
                                else if (一學生.grade_year == "3" || 一學生.grade_year == "9")
                                {
                                    一社團._Now_ClubStudentCount++;
                                    一社團._Now_GradeYear3++;
                                    return true;

                                }
                            }
                            #endregion
                        }
                        #endregion
                    }
                    else
                    {
                        #region 略過
                        //取得社團記錄
                        SCJoin scj = SCJLockDic[StudentID];
                        //取得社團記錄
                        一個社團檢查 一社團 = CLUBCheckDic[scj.RefClubID];

                        if (StudentDic.ContainsKey(StudentID))
                        {
                            一名學生 一學生 = StudentDic[StudentID];

                            if (一學生.grade_year == "1" || 一學生.grade_year == "7")
                            {
                                一社團._Now_ClubStudentCount++;
                                一社團._Now_GradeYear1++;
                                return true;

                            }
                            else if (一學生.grade_year == "2" || 一學生.grade_year == "8")
                            {
                                一社團._Now_ClubStudentCount++;
                                一社團._Now_GradeYear2++;
                                return true;

                            }
                            else if (一學生.grade_year == "3" || 一學生.grade_year == "9")
                            {
                                一社團._Now_ClubStudentCount++;
                                一社團._Now_GradeYear3++;
                                return true;

                            }
                        }
                        #endregion
                    }
                    #endregion
                }
            }
            //選社失敗
            return false;
        }

        void BGW_Save_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            if (!e.Cancelled)
            {
                if (e.Error == null)
                {
                    this.Text = "社團志願分配(畫面資料更新中)";
                    btnRunStart.Text = "開始分配(已完成)";
                    BGW.RunWorkerAsync();
                    MsgBox.Show("志願分配作業,已完成!!");
                }
                else
                {
                    MsgBox.Show("志願分配作業,失敗!!\n" + e.Error.Message.ToString());
                }

            }
            else
            {
                MsgBox.Show("不明原因\n已中止背景模式!!");
            }
        }

        private int SortRandom(學生選社亂數檔 r1, 學生選社亂數檔 r2)
        {
            return r2._Number.CompareTo(r1._Number);
        }

        #region 檢視社團志願明細

        private void 檢視學生選填明細ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridViewX1.SelectedRows.Count == 1)
            {
                ReViewStudentDetail(dataGridViewX1.CurrentRow);
            }
            else
            {
                MsgBox.Show("僅能檢視單一班級!!");
            }
        }

        private void dataGridViewX1_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            ReViewStudentDetail(dataGridViewX1.Rows[e.RowIndex]);
        }

        private void ReViewStudentDetail(DataGridViewRow dataGridViewRow)
        {
            社團志願分配的Row VolRow = (社團志願分配的Row)dataGridViewRow.DataBoundItem;

            VolunteerStudentForm f = new VolunteerStudentForm(VolRow, By_V);
            f.ShowDialog();
        }

        #endregion

        /// <summary>
        /// 取得學生基本資料與建置RowData
        /// </summary>
        private Dictionary<string, 社團志願分配的Row> GetVolClassRow(List<一名學生> StudentList)
        {
            Dictionary<string, 社團志願分配的Row> VolClassRowDic = new Dictionary<string, 社團志願分配的Row>();
            foreach (一名學生 each in StudentList)
            {
                if (VolClassRowDic.ContainsKey(each.class_id))
                {
                    if (!VolClassRowDic[each.class_id]._StudentDic.ContainsKey(each.student_id))
                    {
                        VolClassRowDic[each.class_id]._StudentDic.Add(each.student_id, each);
                    }
                }
                else
                {
                    社團志願分配的Row classRow = new 社團志願分配的Row();
                    classRow._Class = each.class_name;
                    classRow._GradeYear = each.grade_year;
                    classRow._Class_display_order = each.display_order;
                    classRow._teacher = each.teacher_name;

                    if (!string.IsNullOrEmpty(each.nickname))
                    {
                        classRow._teacher += "(" + each.nickname + ")";
                    }

                    if (!classRow._StudentDic.ContainsKey(each.student_id))
                    {
                        classRow._StudentDic.Add(each.student_id, each);
                    }

                    VolClassRowDic.Add(each.class_id, classRow);
                }
            }
            return VolClassRowDic;
        }

        void BGW_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            FISCA.Presentation.MotherForm.SetStatusBarMessage(e.UserState.ToString(), e.ProgressPercentage);
        }

        void BGW_Save_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            FISCA.Presentation.MotherForm.SetStatusBarMessage(e.UserState.ToString(), e.ProgressPercentage);
        }

        /// <summary>
        /// 排序
        /// </summary>
        private int SortClass(社團志願分配的Row row1, 社團志願分配的Row row2)
        {
            string Grat1 = row1._GradeYear.PadLeft(1, '0');
            Grat1 += row1._Class_display_order.PadLeft(3, '9');
            Grat1 += row1._Class.PadLeft(10, '0');

            string Grat2 = row2._GradeYear.PadLeft(1, '0');
            Grat2 += row2._Class_display_order.PadLeft(3, '9');
            Grat2 += row2._Class.PadLeft(10, '0');

            return Grat1.CompareTo(Grat2);
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            if (Is社團已分配)
            {
                this.DialogResult = System.Windows.Forms.DialogResult.Yes;
            }
            else
            {
                this.DialogResult = System.Windows.Forms.DialogResult.No;
            }
        }

        private void dataGridViewX1_SelectionChanged(object sender, EventArgs e)
        {
            labelX2.Text = string.Format("選擇：{0}", "" + dataGridViewX1.SelectedRows.Count);
        }
    }
}
