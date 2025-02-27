using Grievance_Model.DTOs.AppResponse;

namespace Grievance_BAL.IServices
{
    public interface IAccountRepository
    {
        Task<ResponseModel> IsValidProgress(string token, string empCode);

    }
}
