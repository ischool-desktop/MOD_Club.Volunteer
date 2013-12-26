using AllProveReport.Report;
using FISCA;
using FISCA.Permission;
using FISCA.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AllProveReport
{
    public class Program
    {
        [MainMethod()]
        static public void Main() 
        {
            RibbonBarItem Print = FISCA.Presentation.MotherForm.RibbonBarItems["學生", "資料統計"];
            Print["報表"]["社團相關報表"]["社團參與證明單"].Enable = Permissions.社團參與證明單權限;
            Print["報表"]["社團相關報表"]["社團參與證明單"].Click += delegate
            {
                ProveReport cpr = new ProveReport();
                cpr.ShowDialog();
            };

            Catalog detail1 = RoleAclSource.Instance["學生"]["報表"];
            detail1.Add(new RibbonFeature(Permissions.社團參與證明單, "社團參與證明單"));
        }
    }
}
