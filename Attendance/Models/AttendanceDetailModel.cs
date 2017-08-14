namespace Attendance.Models
{
    public class AttendanceDetailModel
    {
        public string cmpId { get; set; }
        public string empName { get; set; }
        public string WorkingHours { get; set; }
        public string InTime { get; set; }
        public string OutTime { get; set; }
        public string TimeDuration { get; set; }
        public string wrkDuration { get; set; }
        public string DayType { get; set; }
    }
}