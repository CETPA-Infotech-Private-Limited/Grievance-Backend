using Grievance_BAL.IServices;
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
        public GrievanceController(IGrievanceRepository grievanceRepository)
        {
            _grievanceRepository = grievanceRepository;
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
        public async Task<ResponseModel> AddUpdateGrievance([FromBody] GrievanceProcessDTO grievanceModel)
        {
            return await _grievanceRepository.AddUpdateGrievanceAsync(grievanceModel);
        }
        [HttpGet("VerifyResolutionLink")]
        public async Task<ResponseModel> VerifyResolutionLink(string resolutionLink)
        {
            return await _grievanceRepository.VerifyResolutionLink(resolutionLink);
        }
        [HttpGet("GrievanceDetails")]
        public async Task<ResponseModel> GrievanceDetailsAsync(int grievanceId, string baseUrl)
        {
            return await _grievanceRepository.GrievanceDetailsAsync(grievanceId, baseUrl);
        }
        [HttpGet("GrievanceHistory")]
        public async Task<ResponseModel> GrievanceHistory(int grievanceId, string baseUrl)
        {
            return await _grievanceRepository.GrievanceHistoryAsync(grievanceId, baseUrl);
        }
    }
}
