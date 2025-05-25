using Grievance_Model.DTOs.AppResponse;
using Grievance_Model.DTOs.Grievance;

namespace Grievance_BAL.IServices
{
    public interface IGrievanceRepository
    {
        Task<ResponseModel> GetGrievanceListAsync(string userCode, int pageNumber = 1, int pageSize = 10);
        Task<ResponseModel> MyGrievanceListAsync(string userCode, int pageNumber = 1, int pageSize = 10);
        Task<ResponseModel> AddUpdateGrievanceAsync(GrievanceProcessDTO grievanceModel);
        Task<ResponseModel> VerifyResolutionLink(string resolutionLink, string? comment);
        Task<ResponseModel> GrievanceDetailsAsync(int grievanceId, string baseUrl);
        Task<ResponseModel> GrievanceHistoryAsync(int grievanceId, string baseUrl);
        Task<ResponseModel> GetDashboardDataAsync(string userCode, string? unitId, string? department, string? year);
        Task<ResponseModel> GetAssignedDashboardDataAsync(string userCode, string? unitId, string? year);
        Task<ResponseModel> GetMyDashboardDataAsync(string userCode);
        Task<ResponseModel> GetResolutionDataAsync(int grievanceMasterId);
        Task<ResponseModel> UpdateIsVisitedAsync(int grievanceId, bool isVisited);

    }
}
