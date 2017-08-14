using Attendance.Helper;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace Attendance.Models.Print_Models
{
    public class AttendanceCell : PdfPCell
    {
        public AttendanceCell(string text)
        {
            HorizontalAlignment = 1;
            Phrase = new Phrase(text.AttendanceCellFont());
        }
    }
}