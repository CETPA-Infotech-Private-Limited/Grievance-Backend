using Grievance_BAL.IServices;
using Grievance_BAL.Services;
using Grievance_Model.DTOs.AppResponse;
using Grievance_Model.DTOs.Grievance;
using Microsoft.AspNetCore.Mvc;

namespace Grievance_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GrievanceController : ControllerBase
    {
        private readonly IGrievanceRepository _grievanceRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public GrievanceController(IGrievanceRepository grievanceRepository, IHttpContextAccessor httpContextAccessor)
        {
            _grievanceRepository = grievanceRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet("GetGrievanceList")]
        public async Task<ResponseModel> GetGrievanceList(string userCode, int pageNumber = 1, int pageSize = 10)
        {
            return await _grievanceRepository.GetGrievanceListAsync(userCode, pageNumber, pageSize);
        }
        [HttpGet("MyGrievanceList")]
        public async Task<ResponseModel> MyGrievanceList(string userCode, int pageNumber = 1, int pageSize = 10)
        {
            return await _grievanceRepository.MyGrievanceListAsync(userCode, pageNumber, pageSize);
        }
        [HttpPost("AddUpdateGrievance")]
        public async Task<ResponseModel> AddUpdateGrievance([FromForm] GrievanceProcessDTO grievanceModel)
        {
            return await _grievanceRepository.AddUpdateGrievanceAsync(grievanceModel);
        }
        [HttpGet("VerifyResolutionLink")]
        public async Task<ResponseModel> VerifyResolutionLink(string resolutionLink, string? comment)
        {
            return await _grievanceRepository.VerifyResolutionLink(resolutionLink, comment);
        }
        [HttpGet("GrievanceDetails")]
        public async Task<ResponseModel> GrievanceDetailsAsync(int grievanceId)
        {
            var baseUrl = GetBaseUrl();
            return await _grievanceRepository.GrievanceDetailsAsync(grievanceId, baseUrl);
        }
        [HttpGet("GrievanceHistory")]
        public async Task<ResponseModel> GrievanceHistory(int grievanceId)
        {
            var baseUrl = GetBaseUrl();
            return await _grievanceRepository.GrievanceHistoryAsync(grievanceId, baseUrl);
        }

        private string GetBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null)
                return string.Empty;

            return $"{request.Scheme}://{request.Host.Value}";
        }

        [HttpGet("GetDashboardData")]
        public async Task<ResponseModel> GetDashboardData(string userCode, string? unitId, string? department, string? year)
        {
            return await _grievanceRepository.GetDashboardDataAsync(userCode, unitId, department, year);
        }
        [HttpGet("GetMyDashboardData")]
        public async Task<ResponseModel> GetMyDashboardData(string userCode)
        {
            return await _grievanceRepository.GetMyDashboardDataAsync(userCode);
        }
    }
}
