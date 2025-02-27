namespace Grievance_Model.DTOs.AppRequest
{
    public class ErrorLogModel
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public string RequestPath { get; set; }
        public string StackTrace { get; set; }
        public DateTime CreateDateTime { get; set; }
    }
}