using Grievance_BAL.IServices;
using Grievance_Model.DTOs.AppResponse;
using Grievance_Model.DTOs.Department;
using Grievance_Model.DTOs.Group;
using Grievance_Model.DTOs.Roles;
using Grievance_Model.DTOs.Service;
using Microsoft.AspNetCore.Authorization;
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
        [HttpGet("GetRoleDetail")]
        public async Task<ResponseModel> GetRoleDetail(int roleId)
        {
            return await userRepository.GetRoleDetailAsync(roleId);
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
        [HttpPost("UpdateUserDepartmentMapping")]
        public async Task<ResponseModel> UpdateUserDepartmentMapping([FromBody] UserDeptMappingModel mappings)
        {
            return await userRepository.UpdateUserDepartmentMappingAsync(mappings);
        }
        [HttpGet("GetDepartmentMappingList")]
        public async Task<ResponseModel> GetDepartmentMappingList()
        {
            return await userRepository.GetDepartmentMappingListAsync();
        }
        [HttpGet("GetAddressalList")]
        public async Task<ResponseModel> GetAddressalList(string? unitId)
        {
            return await userRepository.GetAddressalListAsync(unitId);
        }
        [HttpPost("AddUpdateGroupNew")]
        public async Task<ResponseModel> AddUpdateGroupNew(GroupMasterModel groupModel)
        {
            return await userRepository.AddUpdateGroupNewAsync(groupModel);
        }
        [HttpGet("GetOrgGroupHierarchy")]
        public async Task<ResponseModel> GetOrgGroupHierarchy(string unitId)
        {
            return await userRepository.GetOrgGroupHierarchyAsync(unitId);
        }
    }
}
