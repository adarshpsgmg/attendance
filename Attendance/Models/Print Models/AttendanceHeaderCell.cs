using Attendance.Helper;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace Attendance.Models.Print_Models
{
    public class AttendanceHeaderCell : PdfPCell
    {
        public AttendanceHeaderCell(string text, int colspan)
        {
            HorizontalAlignment = 1;
            Colspan = colspan;
            Phrase = new Phrase(text.AttendanceHeaderCellFont());
        }
    }
}