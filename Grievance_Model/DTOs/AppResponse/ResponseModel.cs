using System.Net;

namespace Grievance_Model.DTOs.AppResponse
{
    public class ResponseModel
    {
        public HttpStatusCode StatusCode { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public int DataLength { get; set; }
        public int TotalRecords { get; set; }
        public bool Error { get; set; }
        public object ErrorDetail { get; set; }
    }

    public class ResponseModelPagination
    {
        public HttpStatusCode StatusCode { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public int DataLength { get; set; }
        public int PageCount { get; set; }
        public int TotalCount { get; set; }
        public bool Error { get; set; }
        public object ErrorDetail { get; set; }

    }
}
