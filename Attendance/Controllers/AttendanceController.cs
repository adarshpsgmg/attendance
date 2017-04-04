using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Script.Serialization;

namespace Attendance.Controllers
{
    public class AttendanceController : ApiController
    {
        [Route("get-all")]
        [EnableCors(origins: "http://localhost:3000", headers: "*", methods: "*")]
        [HttpPost]
        public async Task<HttpResponseMessage> GetAllAsync(RequestModel request)
        {
            var fromDate = request.startDate.StartofDay();
            var fromDateString = fromDate.ToString("MM/dd/yyyy");
            var toDate = request.endDate.StartofDay();
            var toDateString = toDate.ToString("MM/dd/yyyy");
            var days = ((toDate - fromDate).Days + 1);

            var client = new HttpClient();
            var clientResponse = await client.GetAsync(
                "http://192.168.2.180:4373/api/sgTimeCard/Timecard?iEmpId=&dtFrom=" + fromDateString + "&dtTo=" + toDateString);
            clientResponse.EnsureSuccessStatusCode();
            var result = await clientResponse.Content.ReadAsStringAsync();
            var serializer = new JavaScriptSerializer();
            var attendanceModel = serializer.Deserialize<AttendanceModel>(result);

            var groupedEmployeeData = (from item in attendanceModel.sResult
                                       where string.IsNullOrEmpty(request.name) ? true : item.empName.Contains(request.name)
                                       group item by item.empName into groupedItem
                                       select new
                                       {
                                           EmployeeName = groupedItem.Key,
                                           EmployeeData = groupedItem.ToList(),
                                           EmployeeAttendance = groupedItem.Select(x => new InTimeAttendanceModel
                                           {
                                               InTime = DateTime.Parse(x.InTime),
                                               Attendance = string.IsNullOrEmpty(x.DayType) ? "P" : x.DayType
                                           }).ToList()
                                       }).ToList();

            var stream = new MemoryStream();
            Document document = new Document(PageSize.A4, 1, 1, 1, 1);
            PdfWriter writer = PdfWriter.GetInstance(document, stream);
            writer.CloseStream = false;

            document.SetPageSize(PageSize.A4.Rotate());
            document.Open();

            var table = new PdfPTable(days + 1);
            var cell = new PdfPCell(("AttendanceReport").AttendanceFont())
            {
                Colspan = days + 1
            };
            table.AddCell(cell);

            var startDate2 = fromDate;
            for (int i = 0; i < days + 1; i++)
            {
                if (i == 0)
                    table.AddCell(("Employee Name").AttendanceFont());
                else
                {
                    table.AddCell((startDate2.ToString("MM/dd/yyyy")).AttendanceFont());
                    startDate2 = startDate2.AddDays(1);
                }
            }

            foreach (var employee in groupedEmployeeData)
            {
                startDate2 = fromDate;
                for (int i = 0; i < days + 1; i++)
                {
                    if (i == 0)
                        table.AddCell(employee.EmployeeName.AttendanceFont());
                    else if (employee.EmployeeAttendance.Any(x => x.InTime.StartofDay() == startDate2))
                    {
                        var dayType = employee.EmployeeAttendance.FirstOrDefault(x => x.InTime.StartofDay() == startDate2).Attendance;
                        table.AddCell(dayType.AttendanceFont());
                        startDate2 = startDate2.AddDays(1);
                    }
                    else
                    {
                        table.AddCell(("-").AttendanceFont());
                        startDate2 = startDate2.AddDays(1);
                    }
                }
            }

            document.Add(table);
            document.Close();
            writer.Close();
            byte[] bytes = stream.ToArray();
            HttpContent metaDataContent = new ByteArrayContent(bytes);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = metaDataContent
            };
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
            {
                FileName = "Attendance.pdf"
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Pdf);
            return response;
        }
        
        public class AttendanceModel
        {
            public int iStatusCode { get; set; }
            public string sMessage { get; set; }
            public List<AttendanceDetailModel> sResult { get; set; }
        }

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

        public class InTimeAttendanceModel
        {
            public DateTime InTime { get; set; }
            public string Attendance { get; set; }
        }

        public class RequestModel
        {
            public DateTime startDate { get; set; }
            public DateTime endDate { get; set; }
            public string name { get; set; }
        }
    }
    
    public static class Helper
    {
        public static Phrase AttendanceFont(this string text)
        {
            return new Phrase(text, FontFactory.GetFont(FontFactory.HELVETICA, 5, Font.NORMAL));
        }

        public static DateTime StartofDay(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day);
        }
    }
}
