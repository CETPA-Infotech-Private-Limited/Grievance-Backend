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
        Task<ResponseModel> AddUpdateGroupMasterAsync(GroupMasterModel groupMaster);
        Task<ResponseModel> ActiveInactiveGroupAsync(int groupId, bool isActive);
        Task<ResponseModel> GetGroupDetailAsync(int groupId);
        Task<ResponseModel> UpdateUserGroupMappingAsync(UserGroupMasterMappingModel mappings);
        Task<ResponseModel> UpdateUserDepartmentMappingAsync(UserDeptMappingModel mappings);
        Task<ResponseModel> GetDepartmentMappingListAsync();
        Task<ResponseModel> GetServiceMasterListAsync();
        Task<ResponseModel> AddUpdateServiceMasterAsync(ServiceMasterModel serviceMaster);
        Task<ResponseModel> ActiveInactiveServiceAsync(int serviceId, bool isActive);
        Task<ResponseModel> GetServiceDetailAsync(int serviceId);
        Task<ResponseModel> GetAddressalListAsync(string? unitId);
        Task<ResponseModel> GetDashboardDataAsync(string userCode, string? unitId, string? department, string? year);

    }
}
