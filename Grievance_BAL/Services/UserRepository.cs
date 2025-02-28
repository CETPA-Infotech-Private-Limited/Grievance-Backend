using System.Data;
using System.Net;
using Grievance_BAL.IServices;
using Grievance_DAL.DatabaseContext;
using Grievance_DAL.DbModels;
using Grievance_Model.DTOs.AppResponse;
using Grievance_Model.DTOs.Group;
using Grievance_Model.DTOs.Roles;
using Grievance_Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Grievance_BAL.Services
{
    public class UserRepository : IUserRepository
    {
        private readonly GrievanceDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly IEmployeeRepository _employeeRepository;
        public UserRepository(GrievanceDbContext dbContext, IConfiguration configuration, IEmployeeRepository employeeRepository)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _employeeRepository = employeeRepository;

        }

        public async Task<ResponseModel> AddUpdateRoleAsync(RoleModel role)
        {
            ResponseModel responseModel = new ResponseModel()
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };
            if (role != null)
            {
                var appRole = _dbContext.AppRoles.Where(r => r.Id == role.Id || r.RoleName.ToLower().Trim() == role.RoleName.ToLower().Trim()).FirstOrDefault();
                var resultCount = 0;
                if (appRole == null)
                {
                    appRole = new AppRole()
                    {
                        RoleName = role.RoleName,
                        CreatedBy = Convert.ToInt32(role.UserCode),
                        CreatedDate = DateTime.Now,
                        IsActive = true,
                    };
                    _dbContext.AppRoles.Add(appRole);
                    resultCount = await _dbContext.SaveChangesAsync();

                    responseModel.StatusCode = HttpStatusCode.OK;
                    responseModel.Message = "Application Role Added Sucessfully.";
                    responseModel.Data = new { roleId = appRole.Id };
                }
                else
                {
                    appRole.RoleName = role.RoleName;
                    appRole.ModifyBy = Convert.ToInt32(role.UserCode);
                    appRole.ModifyDate = DateTime.Now;
                    appRole.IsActive = true;

                    _dbContext.AppRoles.Update(appRole);
                    resultCount = await _dbContext.SaveChangesAsync();

                    responseModel.StatusCode = HttpStatusCode.OK;
                    responseModel.Message = "Application Role Updated Sucessfully";
                    responseModel.Data = new { roleId = appRole.Id };
                }
            }
            else
            {
                responseModel.StatusCode = HttpStatusCode.BadRequest;
                responseModel.Message = "Application Role Not Valid";
            }
            return responseModel;
        }

        public async Task<ResponseModel> UpdateUserRoleMappingAsync(UserRoleMappingModel mappings)
        {
            ResponseModel responseModel = new ResponseModel()
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };
            var transaction = _dbContext.Database.BeginTransaction();
            var removeMapping = _dbContext.UserRoleMappings.Where(a => a.UserCode == mappings.UserCode && (mappings.RoleId == null || !mappings.RoleId.Contains(a.RoleId))).ToList();

            List<UserRoleMapping> newMapping = new();
            if (mappings.RoleId != null && mappings.RoleId.Count > 0)
            {
                var excludeMapping = _dbContext.UserRoleMappings.Where(a => a.UserCode == mappings.UserCode && mappings.RoleId.Contains(a.RoleId)).Select(a => a.RoleId);

                newMapping = mappings.RoleId.Where(a => !excludeMapping.Contains(a)).Select(newMap => new UserRoleMapping()
                {
                    UserCode = mappings.UserCode,
                    UserDetails = mappings.UserDetails,
                    UnitId = mappings.UnitId,
                    UnitName = mappings.UnitName,
                    RoleId = newMap
                }).ToList();
            }

            if (removeMapping != null && removeMapping.Count() > 0)
            {
                _dbContext.UserRoleMappings.RemoveRange(removeMapping);
            }
            if (newMapping != null && newMapping.Count() > 0)
            {
                await _dbContext.UserRoleMappings.AddRangeAsync(newMapping);
            }

            var resultCount = await _dbContext.SaveChangesAsync();
            if (resultCount > 0)
            {
                await _dbContext.SaveChangesAsync();
                transaction.Commit();
                responseModel.StatusCode = HttpStatusCode.OK;
                responseModel.Message = "User Role Mapping Updated";
                responseModel.Data = mappings;
                responseModel.DataLength = mappings.RoleId.Count();
            }
            else
            {
                responseModel.StatusCode = System.Net.HttpStatusCode.NotModified;
                responseModel.Message = "User Role Mapping Not Updated";
            }
            return responseModel;
        }

        public async Task<ResponseModel> GetApplicationRoleAsync()
        {
            ResponseModel responseModel = new ResponseModel()
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };

            var appRoles = _dbContext.AppRoles.ToList();
            if (appRoles != null && appRoles.Count() > 0)
            {
                responseModel.StatusCode = HttpStatusCode.OK;
                responseModel.Message = "All Application Role List";
                responseModel.Data = appRoles;
                responseModel.DataLength = appRoles.Count();
            }
            else
            {
                responseModel.StatusCode = System.Net.HttpStatusCode.NotFound;
                responseModel.Message = "Record Not found.";
            }
            return responseModel;
        }

        public async Task<ResponseModel> GetUserRolesAsync(string empCode)
        {
            ResponseModel responseModel = new ResponseModel
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Bad Request",
            };

            if (!string.IsNullOrEmpty(empCode))
            {
                List<string> empRoles = new()
                {
                    "User"
                };

                empRoles.AddRange(_dbContext.UserRoleMappings.Include(a => a.Role).Where(a => a.UserCode == empCode).Select(a => a.Role.RoleName).ToList());

                var employeeDetails = await _employeeRepository.GetEmployeeDetailsWithEmpCode(Convert.ToInt32(empCode));
                if (employeeDetails != null)
                {
                    var isSuperAdmin = _configuration.GetSection("SuperAdmin")?.Value?.ToString() == employeeDetails.empCode;
                    if (isSuperAdmin)
                        empRoles.Add(Constant.AppRoles.SuperAdmin);
                }
                if (empRoles != null)
                {
                    responseModel.StatusCode = HttpStatusCode.OK;
                    responseModel.Message = "User Role Details";
                    responseModel.Data = empRoles;
                }
                else
                {
                    responseModel.StatusCode = HttpStatusCode.NotFound;
                    responseModel.Message = "User Role Not Found";
                }
            }
            else
            {
                responseModel.StatusCode = HttpStatusCode.NotFound;
                responseModel.Message = "User Role Not Found";
            }
            return responseModel;
        }

        public async Task<ResponseModel> GetGroupMasterListAsync()
        {
            ResponseModel responseModel = new ResponseModel()
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };

            var groupMasters = await _dbContext.Groups.Where(a => a.IsActive == true || a.IsActive == null).ToListAsync();
            if (groupMasters != null && groupMasters.Count > 0)
            {
                responseModel.StatusCode = HttpStatusCode.OK;
                responseModel.Message = "All Ticket Service Details";
                responseModel.Data = groupMasters;
                responseModel.DataLength = groupMasters.Count;
            }
            else
            {
                responseModel.StatusCode = System.Net.HttpStatusCode.NotFound;
                responseModel.Message = "Record Not found.";
            }
            return responseModel;
        }

        public async Task<ResponseModel> AddUpdateGroupMasterAsync(GroupMasterModel groupMaster)
        {
            ResponseModel responseModel = new ResponseModel()
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };
            if (groupMaster != null)
            {
                var hasParentGroup = _dbContext.Groups.Where(group => group.Id == groupMaster.ParentGroupId).Any();
                if((groupMaster.ParentGroupId != 0 && !hasParentGroup) || (groupMaster.Id == groupMaster.ParentGroupId))
                {
                    responseModel.StatusCode = HttpStatusCode.BadRequest;
                    responseModel.Message = "Parent Group Invalid.";
                    return responseModel;
                }

                var existingGroup = _dbContext.Groups.Where(group => group.Id == groupMaster.Id || group.GroupName.ToLower().Trim() == groupMaster.GroupName.ToLower().Trim()).FirstOrDefault();
                if (existingGroup == null)
                {
                    var newGroup = new GroupMaster()
                    {
                        Id = groupMaster.Id,
                        GroupName = groupMaster.GroupName,
                        ParentGroupId = groupMaster.ParentGroupId == 0 ? null : groupMaster.ParentGroupId,
                        Description = groupMaster.Description ?? string.Empty,
                        CreatedBy = Convert.ToInt32(groupMaster.UserCode),
                        CreatedDate = DateTime.Now,
                        IsActive = true,
                    };
                    _dbContext.Groups.Add(newGroup);
                    await _dbContext.SaveChangesAsync();

                    responseModel.StatusCode = HttpStatusCode.OK;
                    responseModel.Message = "Group Master Added Sucessfully.";
                    responseModel.Data = new { groupId = newGroup.Id };
                }
                else
                {
                    existingGroup.GroupName = groupMaster.GroupName;
                    existingGroup.ParentGroupId = groupMaster.ParentGroupId == 0 ? null : groupMaster.ParentGroupId;
                    existingGroup.Description = groupMaster.Description ?? string.Empty;
                    existingGroup.ModifyBy = Convert.ToInt32(groupMaster.UserCode);
                    existingGroup.ModifyDate = DateTime.Now;
                    existingGroup.IsActive = true;

                    _dbContext.Groups.Update(existingGroup);
                    await _dbContext.SaveChangesAsync();

                    responseModel.StatusCode = HttpStatusCode.OK;
                    responseModel.Message = "Group Master Updated Sucessfully";
                    responseModel.Data = new { groupId = existingGroup.Id };
                }
            }
            else
            {
                responseModel.StatusCode = HttpStatusCode.BadRequest;
                responseModel.Message = "Group Master Not Valid";
            }
            return responseModel;
        }

        public async Task<ResponseModel> ActiveInactiveGroupAsync(int groupId, bool isActive)
        {
            ResponseModel responseModel = new ResponseModel()
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };

            var existingGroup = _dbContext.Groups.Where(group => group.Id == groupId).FirstOrDefault();
            if (existingGroup != null)
            {
                existingGroup.IsActive = isActive;
                _dbContext.Update(existingGroup);
                await _dbContext.SaveChangesAsync();

                responseModel.StatusCode = HttpStatusCode.OK;
                responseModel.Message = string.Format("Group Master {0} Sucessfully.", isActive ? "Activated" : "Deactivated");
                responseModel.Data = new { groupId = existingGroup.Id };
            }
            else
            {
                responseModel.StatusCode = HttpStatusCode.NotFound;
                responseModel.Message = "Group Master Not Found";
            }
            return responseModel;
        }

        public async Task<ResponseModel> GetGroupDetailAsync(int groupId)
        {
            ResponseModel responseModel = new ResponseModel()
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };

            var group = await _dbContext.Groups.Where(a => a.Id == groupId).FirstOrDefaultAsync();
            if (group != null)
            {
                var groupDetails = new
                {
                    Group = group,
                    GroupMapping = _dbContext.UserGroupMappings.Where(a => a.GroupId == groupId).ToList().GroupBy(a => a.UnitId)
                };

                responseModel.StatusCode = HttpStatusCode.OK;
                responseModel.Message = "Group Master Details.";
                responseModel.Data = groupDetails;
            }
            else
            {
                responseModel.StatusCode = HttpStatusCode.NotFound;
                responseModel.Message = "Group Master Not Found";
            }

            return responseModel;
        }

        public async Task<ResponseModel> UpdateUserGroupMappingAsync(UserGroupMasterMappingModel mappings)
        {
            ResponseModel responseModel = new ResponseModel()
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };

            var multipleUnit = mappings.UnitId.Contains(",") ? mappings.UnitId.Split(",").ToList() : new List<string> { mappings.UnitId };
            var multipleUnitName = mappings.UnitName.Contains(",") ? mappings.UnitName.Split(",").ToList() : new List<string> { mappings.UnitName };

            var removeMapping = _dbContext.UserGroupMappings
                .Where(a => a.GroupId == mappings.GroupMasterId &&
                            multipleUnit.Contains(a.UnitId) &&
                            (mappings.UserCodes == null || !mappings.UserCodes.Select(e => e.UserCode).Contains(a.UserCode)))
                .ToList();

            List<UserGroupMapping> newMapping = new();

            if (mappings.UserCodes != null && mappings.UserCodes.Count > 0)
            {
                var excludeMapping = _dbContext.UserGroupMappings
                    .Where(a => a.GroupId == mappings.GroupMasterId &&
                                multipleUnit.Contains(a.UnitId) &&
                                mappings.UserCodes.Select(e => e.UserCode).Contains(a.UserCode))
                    .Select(a => new { a.UserCode, a.UnitId })
                    .ToList();

                foreach (var emp in mappings.UserCodes)
                {
                    for (int i = 0; i < multipleUnit.Count; i++)
                    {
                        var unitId = multipleUnit[i];
                        var unitName = multipleUnitName[i];

                        if (!excludeMapping.Any(b => b.UserCode == emp.UserCode && b.UnitId == unitId))
                        {
                            newMapping.Add(new UserGroupMapping
                            {
                                GroupId = mappings.GroupMasterId,
                                UnitId = unitId,
                                UnitName = unitName,
                                UserCode = emp.UserCode,
                                UserDetails = emp.UserDetails
                            });
                        }
                    }
                }
            }

            if (removeMapping.Any())
            {
                _dbContext.UserGroupMappings.RemoveRange(removeMapping);
            }

            if (newMapping.Any())
            {
                await _dbContext.UserGroupMappings.AddRangeAsync(newMapping);
            }

            var resultCount = await _dbContext.SaveChangesAsync();
            if (resultCount > 0)
            {
                responseModel.StatusCode = HttpStatusCode.OK;
                responseModel.Message = "User GroupMaster Mapping Updated";
                responseModel.Data = mappings;
                responseModel.DataLength = mappings.UserCodes.Count;
            }
            else
            {
                responseModel.StatusCode = HttpStatusCode.NotModified;
                responseModel.Message = "User GroupMaster Mapping Not Updated";
            }

            return responseModel;
        }

    }
}
