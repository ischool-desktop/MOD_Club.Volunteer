using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace K12.Club.Volunteer.API
{
    public interface IClubDetailItemAPI
    {
        FISCA.Presentation.IDetailBulider CreateBasicInfo();
    }
}
