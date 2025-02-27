using Grievance_Model.DTOs.AppResponse;
using Grievance_Model.DTOs.Group;
using Grievance_Model.DTOs.Roles;

namespace Grievance_BAL.IServices
{
    public interface IUserRepository
    {
        Task<ResponseModel> AddUpdateRoleAsync(RoleModel role);
        Task<ResponseModel> UpdateUserRoleMappingAsync(UserRoleMappingModel mappings);
        Task<ResponseModel> GetApplicationRoleAsync();
        Task<ResponseModel> GetUserRolesAsync(string empCode);
        Task<ResponseModel> GetGroupMasterListAsync();
        Task<ResponseModel> AddUpdateGroupMasterAsync(GroupMasterModel groupMaster);
        Task<ResponseModel> ActiveInactiveGroupAsync(int groupId, bool isActive);
        Task<ResponseModel> GetGroupDetailAsync(int groupId);
        Task<ResponseModel> UpdateUserGroupMappingAsync(UserGroupMasterMappingModel mappings);

    }
}

