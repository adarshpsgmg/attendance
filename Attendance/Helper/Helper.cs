using iTextSharp.text;
using System;

namespace Attendance.Helper
{
    public static class Helper
    {
        public static Phrase AttendanceCellFont(this string text)
        {
            return new Phrase(text, FontFactory.GetFont(FontFactory.HELVETICA, 5, Font.NORMAL));
        }

        public static Phrase AttendanceHeaderCellFont(this string text)
        {
            return new Phrase(text, FontFactory.GetFont(FontFactory.HELVETICA, 15, Font.NORMAL));
        }

        public static DateTime StartofDay(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day);
        }
    }
}