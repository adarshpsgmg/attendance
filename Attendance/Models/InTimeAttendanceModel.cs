using System;

namespace Attendance.Models
{
    public class InTimeAttendanceModel
    {
        public DateTime InTime { get; set; }
        public DateTime OutTime { get; set; }
        public string Attendance { get; set; }
    }
}