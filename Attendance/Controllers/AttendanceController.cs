using Attendance.Helper;
using Attendance.Models;
using Attendance.Models.Print_Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
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
    /// <summary>
    /// Attendance controller.
    /// </summary>
    /// <seealso cref="System.Web.Http.ApiController" />
    public class AttendanceController : ApiController
    {
        /// <summary>
        /// Gets all asynchronous.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        [Route("get-all")]
        [EnableCors(origins: "http://localhost:7000", headers: "*", methods: "*")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetAllAsync(DateTime startDate, DateTime endDate, string name)
        {
            var fromDate = startDate.StartofDay();
            var fromDateString = fromDate.ToString("MM/dd/yyyy");
            var toDate = endDate.StartofDay();
            var toDateString = toDate.ToString("MM/dd/yyyy");
            var days = ((toDate - fromDate).Days + 1);
            
            var attendanceModel = await GetAttendanceData(fromDateString, toDateString);

            #region Structuring data
            var groupedEmployeeData = (from item in attendanceModel.sResult
                                       where string.IsNullOrEmpty(name) ? true : item.empName.Contains(name)
                                       group item by new { item.empName, item.cmpId } into groupedItem
                                       select new
                                       {
                                           EmployeeName = groupedItem.Key.empName,
                                           EmployeeID = groupedItem.Key.cmpId,
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

            var nameTable = new PdfPTable(2);

            nameTable.AddCell(new AttendanceCell("Employee Name"));
            nameTable.AddCell(new AttendanceCell("Employee ID"));

            foreach (var employee in groupedEmployeeData)
            {
                nameTable.AddCell(new AttendanceCell(employee.EmployeeName));
                nameTable.AddCell(new AttendanceCell(employee.EmployeeID));
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
                    if (tEmployee == null)
                    {
                        dataTable.AddCell(new AttendanceCell(("-")));
                    }
                    else
                    {
                        dataTable.AddCell(new AttendanceCell((tEmployee.Attendance)));
                    }
                    loopStartDate = loopStartDate.AddDays(1);
                }
            }

            outerTable.AddCell(nameTable);
            outerTable.AddCell(dataTable);

            #region Packing and sending response
            document.Add(outerTable);
            document.Close();
            writer.Close();
            stream.Seek(0, SeekOrigin.Begin);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream)
            };
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
            {
                FileName = "Attendance.pdf"
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Pdf);
            return response;
            #endregion
        }

        /// <summary>
        /// Gets all detailed asynchronous.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        [Route("get-all-detailed")]
        [EnableCors(origins: "http://localhost:7000", headers: "*", methods: "*")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetAllDetailedAsync(DateTime startDate, DateTime endDate, string name)
        {
            var fromDate = startDate.StartofDay();
            var fromDateString = fromDate.ToString("MM/dd/yyyy");
            var toDate = endDate.StartofDay();
            var toDateString = toDate.ToString("MM/dd/yyyy");

            var attendanceModel = await GetAttendanceData(fromDateString, toDateString);

            #region Creating write stream and document
            var stream = new MemoryStream();
            var document = new Document(PageSize.A4, 10f, 10f, 10f, 10f);
            var writer = PdfWriter.GetInstance(document, stream);
            writer.CloseStream = false;

            document.SetPageSize(PageSize.A4.Rotate());
            document.Open();
            #endregion

            var dataTable = new PdfPTable(8);
            dataTable.AddCell(new AttendanceCell("Employee ID"));
            dataTable.AddCell(new AttendanceCell("Employee Name"));
            dataTable.AddCell(new AttendanceCell("Working Hours"));
            dataTable.AddCell(new AttendanceCell("In Time"));
            dataTable.AddCell(new AttendanceCell("Out Time"));
            dataTable.AddCell(new AttendanceCell("Time Duration"));
            dataTable.AddCell(new AttendanceCell("Work Duration"));
            dataTable.AddCell(new AttendanceCell("Day Type"));

            var employeeDetails = attendanceModel.sResult.OrderBy(x => x.cmpId).ToList();

            foreach (var employeeDetail in employeeDetails)
            {
                dataTable.AddCell(new AttendanceCell(employeeDetail.cmpId));
                dataTable.AddCell(new AttendanceCell(employeeDetail.empName));
                dataTable.AddCell(new AttendanceCell(employeeDetail.WorkingHours));
                dataTable.AddCell(new AttendanceCell(employeeDetail.InTime));
                dataTable.AddCell(new AttendanceCell(employeeDetail.OutTime));
                dataTable.AddCell(new AttendanceCell(employeeDetail.TimeDuration));
                dataTable.AddCell(new AttendanceCell(employeeDetail.wrkDuration));
                dataTable.AddCell(new AttendanceCell(employeeDetail.DayType));
            }

            #region Packing and sending response
            document.Add(dataTable);
            document.Close();
            writer.Close();
            stream.Seek(0, SeekOrigin.Begin);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream)
            };
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
            {
                FileName = "Attendance.pdf"
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Pdf);
            return response;
            #endregion
        }

        /// <summary>
        /// Gets the attendance data.
        /// </summary>
        /// <param name="fromDateString">From date string.</param>
        /// <param name="toDateString">To date string.</param>
        /// <returns></returns>
        private async Task<AttendanceApiModel> GetAttendanceData(string fromDateString, string toDateString)
        {
            var client = new HttpClient();
            var clientResponse = await client.GetAsync("http://192.168.2.180:4373/api/sgTimeCard/Timecard?iEmpId=&dtFrom=" + 
                fromDateString + "&dtTo=" + toDateString);
            clientResponse.EnsureSuccessStatusCode();
            var result = await clientResponse.Content.ReadAsStringAsync();
            var serializer = new JavaScriptSerializer();
            var attendanceModel = serializer.Deserialize<AttendanceApiModel>(result);
            return attendanceModel;
        }
    }
}
