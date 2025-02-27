using System.Text;
using Microsoft.AspNetCore.Http;

namespace Grievance_Model.DTOs.Notification
{
    public class MailRequestModel
    {
        public MailRequestModel()
        {
            EmailToId = new List<string>();
            EmailCCId = new List<string>();
        }
        public List<string> EmailToId { get; set; }
        public List<string> EmailCCId { get; set; }
        public string EmailToName { get; set; }
        public string EmailSubject { get; set; }
        public StringBuilder EmailBody { get; set; }
        public List<IFormFile>? Attachments { get; set; }
    }
}
