using Grievance_Model.DTOs.AppResponse;
using Grievance_Model.DTOs.Notification;

namespace Grievance_BAL.IServices
{
    public interface INotificationRepository
    {
        Task<ResponseModel> SendNotification(MailRequestModel mailRequest);
    }
}
