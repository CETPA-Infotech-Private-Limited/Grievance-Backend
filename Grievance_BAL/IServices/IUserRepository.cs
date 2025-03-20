using Grievance_Model.DTOs.AppResponse;
using Grievance_Model.DTOs.Department;
using Grievance_Model.DTOs.Group;
using Grievance_Model.DTOs.Roles;
using Grievance_Model.DTOs.Service;

namespace Grievance_BAL.IServices
{
    public interface IUserRepository
    {
        Task<ResponseModel> AddUpdateRoleAsync(RoleModel role);
        Task<ResponseModel> UpdateUserRoleMappingAsync(UserRoleMappingModel mappings);
        Task<ResponseModel> GetApplicationRoleAsync();
        Task<ResponseModel> GetRoleDetailAsync(int roleId);
        Task<ResponseModel> GetUserRolesAsync(string empCode);
        Task<ResponseModel> GetGroupMasterListAsync();
        Task<ResponseModel> ActiveInactiveGroupAsync(int groupId, bool isActive);
        Task<ResponseModel> GetGroupDetailAsync(int groupId);
        Task<ResponseModel> UpdateUserGroupMappingAsync(UserGroupMasterMappingModel mappings);
        Task<ResponseModel> UpdateUserDepartmentMappingAsync(UserDeptMappingModel mappings);
        Task<ResponseModel> GetDepartmentMappingListAsync();
        Task<ResponseModel> GetAddressalListAsync(string? unitId);

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        Task<ResponseModel> AddUpdateGroupNewAsync(GroupMasterModel groupModel);
        Task<ResponseModel> GetOrgGroupHierarchyAsync(string unitId);
        Task<UnitRoleUserModel> GetUnitRoleUsersAsync(string unitId, int roleId = 0);
        Task<ResponseModel> GetServiceMasterAsync(bool isCorporate);

    }
}
