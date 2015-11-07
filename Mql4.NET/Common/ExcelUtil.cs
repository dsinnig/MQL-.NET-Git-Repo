using System;
using System.Collections.Generic;
using System.Text;
using NQuotes;

namespace biiuse
{
    class ExcelUtil
    {
        public static string datetimeToExcelDate(System.DateTime _date)
        {
            return _date.Year.ToString().PadLeft(4, '0') + "-" +
                    _date.Month.ToString().PadLeft(2, '0') + "-" +
                    _date.Day.ToString().PadLeft(2, '0') + " " +
                    _date.Hour.ToString().PadLeft(2, '0') + ":" +
                    _date.Minute.ToString().PadLeft(2, '0') + ":" +
                    _date.Second.ToString().PadLeft(2, '0');
        }


    }
}
