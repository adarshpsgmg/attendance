using System;

namespace Attendance.Models
{
    public class RequestModel
    {
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        public string name { get; set; }
    }
}