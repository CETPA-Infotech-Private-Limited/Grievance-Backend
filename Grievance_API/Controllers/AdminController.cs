using Grievance_BAL.IServices;
using Grievance_Model.DTOs.AppResponse;
using Grievance_Model.DTOs.Group;
using Grievance_Model.DTOs.Roles;
using Microsoft.AspNetCore.Mvc;

namespace Grievance_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IUserRepository userRepository;
        public AdminController(IUserRepository userRepository)
        {
            this.userRepository = userRepository;
        }

        [HttpPost("AddUpdateRole")]
        public async Task<ResponseModel> AddUpdateRole([FromBody] RoleModel role)
        {
            return await userRepository.AddUpdateRoleAsync(role);
        }
        [HttpPost("UpdateUserRoleMapping")]
        public async Task<ResponseModel> UpdateUserRoleMapping([FromBody] UserRoleMappingModel mappings)
        {
            return await userRepository.UpdateUserRoleMappingAsync(mappings);
        }
        [HttpGet("GetApplicationRole")]
        public async Task<ResponseModel> GetApplicationRole()
        {
            return await userRepository.GetApplicationRoleAsync();
        }
        [HttpGet("GetUserRoles")]
        public async Task<ResponseModel> GetUserRoles(string empCode)
        {
            return await userRepository.GetUserRolesAsync(empCode);
        }
        [HttpGet("GetGroupMasterList")]
        public async Task<ResponseModel> GetGroupMasterList()
        {
            return await userRepository.GetGroupMasterListAsync();
        }
        [HttpPost("AddUpdateGroupMaster")]
        public async Task<ResponseModel> AddUpdateGroupMaster([FromBody] GroupMasterModel groupMaster)
        {
            return await userRepository.AddUpdateGroupMasterAsync(groupMaster);
        }
        [HttpGet("ActiveInactiveGroup")]
        public async Task<ResponseModel> ActiveInactiveGroup(int groupId, bool isActive)
        {
            return await userRepository.ActiveInactiveGroupAsync(groupId, isActive);
        }
        [HttpGet("GetGroupDetail")]
        public async Task<ResponseModel> GetGroupDetail(int groupId)
        {
            return await userRepository.GetGroupDetailAsync(groupId);
        }
        [HttpPost("UpdateUserGroupMapping")]
        public async Task<ResponseModel> UpdateUserGroupMapping([FromBody] UserGroupMasterMappingModel mappings)
        {
            return await userRepository.UpdateUserGroupMappingAsync(mappings);
        }

    }
}
