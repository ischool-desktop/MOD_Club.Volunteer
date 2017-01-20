using Campus.DocumentValidator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace K12.Club.General.ImportClubScore.ImportExport.ValidationRule.FieldValidator
{
    class NumberIsNewScoreCheck : IFieldValidator
    {
        /// <summary>
        /// 自動修正
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public string Correct(string Value)
        {
            return string.Empty;
        }

        /// <summary>
        /// 回傳訊息
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public string ToString(string template)
        {
            return template;
        }

        /// <summary>
        /// 驗證
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public bool Validate(string value)
        {
            //回傳是否為空白
            if (String.IsNullOrEmpty(value))
            {
                return true;
            }
            else
            {
                decimal abc = 0;
                if (decimal.TryParse(value, out abc))
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }
        }
    }
}
