using System.Collections.Generic;

namespace Attendance.Models
{
    /// <summary>
    /// Attendance Api Model.
    /// </summary>
    public class AttendanceApiModel
    {
        public int iStatusCode { get; set; }
        public string sMessage { get; set; }
        public List<AttendanceDetailModel> sResult { get; set; }
    }
}