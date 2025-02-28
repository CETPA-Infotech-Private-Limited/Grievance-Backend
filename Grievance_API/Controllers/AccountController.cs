using Grievance_BAL.IServices;
using Grievance_Model.DTOs.AppResponse;
using Microsoft.AspNetCore.Mvc;

namespace Grievance_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        public readonly IAccountRepository _accountRepository;
        public AccountController(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }


        [HttpPost("IsValidProgress")]
        public async Task<ResponseModel> IsValidProgress(string Token, string EmpCode)
        {
            var response = await _accountRepository.IsValidProgress(Token, EmpCode);
            return response;
        }

    }
}
