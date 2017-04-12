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

            #region Calling API and fetching data
            var client = new HttpClient();
            var clientResponse = await client.GetAsync(
                "http://192.168.2.180:4373/api/sgTimeCard/Timecard?iEmpId=&dtFrom=" + fromDateString + "&dtTo=" + toDateString);
            clientResponse.EnsureSuccessStatusCode();
            var result = await clientResponse.Content.ReadAsStringAsync();
            var serializer = new JavaScriptSerializer();
            var attendanceModel = serializer.Deserialize<AttendanceModel>(result);
            #endregion

            #region Tester
            //var a = new DateTime();
            //foreach(var item in attendanceModel.sResult)
            //{
            //    try
            //    {
            //        a = DateTime.Parse(item.InTime);
            //        a = DateTime.Parse(item.OutTime);
            //    }
            //    catch(Exception ex)
            //    {

            //    }
            //} 
            #endregion

            #region Structuring data
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
                                               OutTime = x.OutTime == "--" ? DateTime.MinValue : DateTime.Parse(x.OutTime),
                                               Attendance = string.IsNullOrEmpty(x.DayType) ? "P" : x.DayType
                                           }).ToList()
                                       }).ToList();
            #endregion

            #region Creating write stream and document
            var stream = new MemoryStream();
            var document = new Document(PageSize.A4, 10f, 10f, 10f, 10f);
            var writer = PdfWriter.GetInstance(document, stream);
            writer.CloseStream = false;

            document.SetPageSize(PageSize.A4.Rotate());
            document.Open();
            #endregion

            var outerTable = new PdfPTable(2);
            outerTable.SetWidths(new float[] { 10, 10 });
            //outerTable.AddCell(new AttendanceHeaderCell("AttendanceReport\n", 2));

            var nameTable = new PdfPTable(1);

            nameTable.AddCell(new AttendanceCell("Employee Name"));

            foreach (var employee in groupedEmployeeData)
            {
                nameTable.AddCell(new AttendanceCell(employee.EmployeeName));
            }

            var dataTable = new PdfPTable(days);

            var loopStartDate = fromDate;
            for (int i = 0; i < days; i++)
            {
                dataTable.AddCell(new AttendanceCell(loopStartDate.ToString("MM/dd/yyyy")));
                loopStartDate = loopStartDate.AddDays(1);
            }

            foreach (var employee in groupedEmployeeData)
            {
                loopStartDate = fromDate;
                for (int i = 0; i < days; i++)
                {
                    var tEmployee = employee.EmployeeAttendance.FirstOrDefault(x => x.InTime.StartofDay() == loopStartDate);
                    dataTable.AddCell(new AttendanceCell(
                        (
                        tEmployee.Attendance
                        //tEmployee.InTime.ToString("HH:mm tt")
                        //+ " - " + (tEmployee.OutTime == DateTime.MinValue ? " Missed" : tEmployee.OutTime.ToString("HH:mm tt"))
                        )
                    ));
                    loopStartDate = loopStartDate.AddDays(1);
                }
            }

            outerTable.AddCell(nameTable);
            outerTable.AddCell(dataTable);

            #region Packing and sending response
            document.Add(outerTable);
            document.Close();
            writer.Close();
            var bytes = stream.ToArray();
            var metaDataContent = new ByteArrayContent(bytes);

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
            #endregion
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
            public DateTime OutTime { get; set; }
            public string Attendance { get; set; }
        }

        public class RequestModel
        {
            public DateTime startDate { get; set; }
            public DateTime endDate { get; set; }
            public string name { get; set; }
        }

        public class AttendanceCell : PdfPCell
        {
            public AttendanceCell(string text)
            {
                HorizontalAlignment = 1;
                Phrase = new Phrase(text.AttendanceCellFont());
            }
        }

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
