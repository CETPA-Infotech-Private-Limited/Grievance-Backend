using System.ComponentModel.Design.Serialization;
using System.Data;
using System.Drawing.Printing;
using System.Net;
using System.Text.RegularExpressions;
using Grievance_BAL.IServices;
using Grievance_DAL.DatabaseContext;
using Grievance_DAL.DbModels;
using Grievance_Model.DTOs.AppResponse;
using Grievance_Model.DTOs.Dashboard;
using Grievance_Model.DTOs.Department;
using Grievance_Model.DTOs.Employee;
using Grievance_Model.DTOs.Group;
using Grievance_Model.DTOs.Roles;
using Grievance_Model.DTOs.Service;
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
                        Description = role.Description,
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
                    appRole.Description = role.Description;
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
            ResponseModel responseModel = new()
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };

            if (mappings.RoleId == null || mappings.RoleId.Count == 0 || string.IsNullOrEmpty(mappings.UnitId))
            {
                responseModel.Message = "RoleId and UnitId are required";
                return responseModel;
            }

            var userCodes = string.IsNullOrWhiteSpace(mappings.UserCode)
                ? new List<string>()
                : mappings.UserCode.Split(',').Select(x => x.Trim()).ToList();

            var userDetails = string.IsNullOrWhiteSpace(mappings.UserDetails)
                ? new List<string>()
                : mappings.UserDetails.Split(',').Select(x => x.Trim()).ToList();

            if (userCodes.Count != userDetails.Count)
            {
                responseModel.Message = "UserCode and UserDetails count mismatch";
                return responseModel;
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                List<UserRoleMapping> newMappings = new();

                foreach (var roleId in mappings.RoleId)
                {
                    var existingMappings = _dbContext.UserRoleMappings
                        .Where(a => a.RoleId == roleId && a.UnitId == mappings.UnitId)
                        .ToList();

                    if (userCodes.Count == 0)
                    {
                        if (existingMappings.Any())
                        {
                            _dbContext.UserRoleMappings.RemoveRange(existingMappings);
                        }
                        continue;
                    }

                    var usersToRemove = existingMappings.Where(a => !userCodes.Contains(a.UserCode)).ToList();
                    if (usersToRemove.Any())
                    {
                        _dbContext.UserRoleMappings.RemoveRange(usersToRemove);
                    }

                    var existingUserCodes = existingMappings.Select(a => a.UserCode).ToHashSet();
                    for (int i = 0; i < userCodes.Count; i++)
                    {
                        if (!existingUserCodes.Contains(userCodes[i]))
                        {
                            newMappings.Add(new UserRoleMapping
                            {
                                UserCode = userCodes[i],
                                UserDetails = userDetails[i],
                                UnitId = mappings.UnitId,
                                UnitName = mappings.UnitName,
                                RoleId = roleId
                            });
                        }
                    }
                }

                if (newMappings.Any())
                {
                    await _dbContext.UserRoleMappings.AddRangeAsync(newMappings);
                }

                var resultCount = await _dbContext.SaveChangesAsync();
                if (resultCount > 0)
                {
                    await transaction.CommitAsync();
                    responseModel.StatusCode = HttpStatusCode.OK;
                    responseModel.Message = "User Role Mapping Updated Successfully";
                    responseModel.Data = mappings;
                    responseModel.DataLength = newMappings.Count;
                }
                else
                {
                    responseModel.StatusCode = HttpStatusCode.NotModified;
                    responseModel.Message = "No changes made to User Role Mapping";
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                responseModel.StatusCode = HttpStatusCode.InternalServerError;
                responseModel.Message = $"Error: {ex.Message}";
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

            var appRoles = await _dbContext.AppRoles.ToListAsync();
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

        public async Task<ResponseModel> GetRoleDetailAsync(int roleId)
        {
            ResponseModel responseModel = new ResponseModel()
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };

            var roleDetails = await _dbContext.AppRoles.Where(ar => ar.Id == roleId).Select(ar => new
            {
                RoleDetail = ar,
                MappedUsers = _dbContext.UserRoleMappings
                                    .Where(urm => urm.RoleId == roleId).IgnoreAutoIncludes()
                                    .Select(urm => new
                                    {
                                        UserCode = urm.UserCode,
                                        UserDetails = urm.UserDetails,
                                        UnitId = urm.UnitId,
                                        UnitName = urm.UnitName
                                    }).ToList()
            }).FirstOrDefaultAsync();

            if (roleDetails != null)
            {
                responseModel.StatusCode = HttpStatusCode.OK;
                responseModel.Message = "Role Details";
                responseModel.Data = roleDetails;
            }
            else
            {
                responseModel.StatusCode = HttpStatusCode.NotFound;
                responseModel.Message = "Role Not Found";
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
                    Constant.AppRoles.User
                };
                var isAddressal = _dbContext.UserGroupMappings.Where(a => a.UserCode == empCode && a.Group.IsHOD != true).Any();
                if (isAddressal)
                    empRoles.Add(Constant.AppRoles.Addressal);

                var isHOD = _dbContext.UserGroupMappings.Where(a => a.UserCode == empCode && a.Group.IsHOD == true).Any();
                if (isHOD)
                    empRoles.Add(Constant.AppRoles.HOD);

                empRoles.AddRange((await _dbContext.UserRoleMappings.Include(a => a.Role).Where(a => a.UserCode == empCode).Select(a => a.Role.RoleName).ToListAsync()).Distinct());

                var isSuperAdmin = _configuration.GetSection("SuperAdmin")?.Value?.ToString() == empCode;
                if (isSuperAdmin)
                    empRoles.Add(Constant.AppRoles.SuperAdmin);

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
                responseModel.Message = "All Group Master Details";
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
                var existingGroup = _dbContext.Groups.Where(group => group.Id == groupMaster.Id || group.GroupName.ToLower().Trim() == groupMaster.GroupName.ToLower().Trim()).FirstOrDefault();
                if (existingGroup == null)
                {
                    var newGroup = new GroupMaster()
                    {
                        GroupName = groupMaster.GroupName,
                        Description = groupMaster.Description ?? string.Empty,
                        IsCommitee = groupMaster.IsCommitee,
                        IsHOD = groupMaster.IsHOD,
                        HODofGroupId = groupMaster.HODofGroupId,
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
                    existingGroup.Description = groupMaster.Description ?? string.Empty;
                    existingGroup.IsCommitee = groupMaster.IsCommitee;
                    existingGroup.IsHOD = groupMaster.IsHOD;
                    existingGroup.HODofGroupId = groupMaster.HODofGroupId;
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
                    GroupMapping = _dbContext.UserGroupMappings.Where(a => a.GroupId == groupId).IgnoreAutoIncludes().ToList().GroupBy(a => a.UnitId)
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

        public async Task<ResponseModel> UpdateUserDepartmentMappingAsync(UserDeptMappingModel mappings)
        {
            ResponseModel responseModel = new ResponseModel()
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };

            var multipleUnit = mappings.UnitId.Contains(",") ? mappings.UnitId.Split(",").ToList() : new List<string> { mappings.UnitId };
            var multipleUnitName = mappings.UnitName.Contains(",") ? mappings.UnitName.Split(",").ToList() : new List<string> { mappings.UnitName };

            var removeMapping = _dbContext.UserDepartmentMappings
                .Where(a => a.Department.Trim().ToLower() == mappings.Department.Trim().ToLower() &&
                            multipleUnit.Contains(a.UnitId) &&
                            (mappings.UserCodes == null || !mappings.UserCodes.Select(e => e.UserCode).Contains(a.UserCode)))
                .ToList();

            List<UserDepartmentMapping> newMapping = new();

            if (mappings.UserCodes != null && mappings.UserCodes.Count > 0)
            {
                var excludeMapping = _dbContext.UserDepartmentMappings
                    .Where(a => a.Department.Trim().ToLower() == mappings.Department.Trim().ToLower() &&
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
                            newMapping.Add(new UserDepartmentMapping
                            {
                                Department = mappings.Department,
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
                _dbContext.UserDepartmentMappings.RemoveRange(removeMapping);
            }

            if (newMapping.Any())
            {
                await _dbContext.UserDepartmentMappings.AddRangeAsync(newMapping);
            }

            var resultCount = await _dbContext.SaveChangesAsync();
            if (resultCount > 0)
            {
                responseModel.StatusCode = HttpStatusCode.OK;
                responseModel.Message = "User department mapping updated";
                responseModel.Data = mappings;
                responseModel.DataLength = mappings.UserCodes.Count;
            }
            else
            {
                responseModel.StatusCode = HttpStatusCode.NotModified;
                responseModel.Message = "User department mapping not updated";
            }

            return responseModel;
        }

        public async Task<ResponseModel> GetDepartmentMappingListAsync()
        {
            ResponseModel responseModel = new ResponseModel()
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };

            var mappedUser = (await _dbContext.UserDepartmentMappings.Select(depart => new
            {
                Department = depart.Department,
                MappedUser = _dbContext.UserDepartmentMappings.Where(a => a.Department == depart.Department).Select(a => new
                {
                    UserCode = a.UserCode,
                    UserDetail = a.UserDetails,
                    UnitId = a.UnitId,
                    UnitName = a.UnitName
                }).ToList()
            }).ToListAsync()).DistinctBy(a => a.Department);
            if (mappedUser != null)
            {
                responseModel.StatusCode = HttpStatusCode.OK;
                responseModel.Message = "Department Mapping Details.";
                responseModel.Data = mappedUser;
            }
            else
            {
                responseModel.StatusCode = HttpStatusCode.NotFound;
                responseModel.Message = "Department Mapping Not Found";
            }
            return responseModel;
        }

        public async Task<ResponseModel> GetServiceMasterListAsync()
        {
            ResponseModel responseModel = new ResponseModel()
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };

            var serviceMasters = await _dbContext.Services.Include(a => a.GroupMaster).ToListAsync();
            if (serviceMasters != null && serviceMasters.Count > 0)
            {
                responseModel.StatusCode = HttpStatusCode.OK;
                responseModel.Message = "All Service Details";
                responseModel.Data = serviceMasters;
                responseModel.DataLength = serviceMasters.Count;
            }
            else
            {
                responseModel.StatusCode = System.Net.HttpStatusCode.NotFound;
                responseModel.Message = "Record Not found.";
            }
            return responseModel;
        }

        public async Task<ResponseModel> AddUpdateServiceMasterAsync(ServiceMasterModel serviceMaster)
        {
            ResponseModel responseModel = new ResponseModel()
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };
            if (serviceMaster != null)
            {
                var hasParentGroup = _dbContext.Services.Where(service => service.Id == serviceMaster.ParentServiceId).Any();
                if ((serviceMaster.ParentServiceId != 0 && !hasParentGroup) || (serviceMaster.Id != 0 && serviceMaster.Id == serviceMaster.ParentServiceId))
                {
                    responseModel.StatusCode = HttpStatusCode.BadRequest;
                    responseModel.Message = "Parent Service Invalid.";
                    return responseModel;
                }

                var existingService = _dbContext.Services.Where(service => service.Id == serviceMaster.Id || service.ServiceName.ToLower().Trim() == serviceMaster.ServiceName.ToLower().Trim()).IgnoreAutoIncludes().FirstOrDefault();
                if (existingService == null)
                {
                    var newService = new ServiceMaster()
                    {
                        ServiceName = serviceMaster.ServiceName,
                        ServiceDescription = serviceMaster.ServiceDescription ?? string.Empty,
                        ParentServiceId = serviceMaster.ParentServiceId == 0 ? null : serviceMaster.ParentServiceId,
                        GroupMasterId = serviceMaster.GroupMasterId,
                        CreatedBy = Convert.ToInt32(serviceMaster.UserCode),
                        CreatedDate = DateTime.Now,
                        IsActive = true,
                    };
                    _dbContext.Services.Add(newService);
                    await _dbContext.SaveChangesAsync();

                    responseModel.StatusCode = HttpStatusCode.OK;
                    responseModel.Message = "Service Master Added Sucessfully.";
                    responseModel.Data = new { groupId = newService.Id };
                }
                else
                {
                    existingService.ServiceName = serviceMaster.ServiceName;
                    existingService.ServiceDescription = serviceMaster.ServiceDescription ?? string.Empty;
                    existingService.ParentServiceId = serviceMaster.ParentServiceId == 0 ? null : serviceMaster.ParentServiceId;
                    existingService.GroupMasterId = serviceMaster.GroupMasterId;
                    existingService.ModifyBy = Convert.ToInt32(serviceMaster.UserCode);
                    existingService.ModifyDate = DateTime.Now;
                    existingService.IsActive = true;

                    _dbContext.Services.Update(existingService);
                    await _dbContext.SaveChangesAsync();

                    responseModel.StatusCode = HttpStatusCode.OK;
                    responseModel.Message = "Service Master Updated Sucessfully";
                    responseModel.Data = new { serviceId = existingService.Id };
                }
            }
            else
            {
                responseModel.StatusCode = HttpStatusCode.BadRequest;
                responseModel.Message = "Service Master Not Valid";
            }
            return responseModel;
        }

        public async Task<ResponseModel> ActiveInactiveServiceAsync(int serviceId, bool isActive)
        {
            ResponseModel responseModel = new ResponseModel()
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };

            var existingService = _dbContext.Services.Where(service => service.Id == serviceId).FirstOrDefault();
            if (existingService != null)
            {
                existingService.IsActive = isActive;
                _dbContext.Update(existingService);
                await _dbContext.SaveChangesAsync();

                responseModel.StatusCode = HttpStatusCode.OK;
                responseModel.Message = string.Format("Service Master {0} Sucessfully.", isActive ? "Activated" : "Deactivated");
                responseModel.Data = new { serviceId = existingService.Id };
            }
            else
            {
                responseModel.StatusCode = HttpStatusCode.NotFound;
                responseModel.Message = "Service Master Not Found";
            }
            return responseModel;
        }

        public async Task<ResponseModel> GetServiceDetailAsync(int serviceId)
        {
            ResponseModel responseModel = new ResponseModel()
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };

            var service = await _dbContext.Services.Where(a => a.Id == serviceId).FirstOrDefaultAsync();
            if (service != null)
            {
                responseModel.StatusCode = HttpStatusCode.OK;
                responseModel.Message = "Service Master Details.";
                responseModel.Data = service;
            }
            else
            {
                responseModel.StatusCode = HttpStatusCode.NotFound;
                responseModel.Message = "Service Master Not Found";
            }
            return responseModel;
        }

        public async Task<ResponseModel> GetAddressalListAsync(string? unitId)
        {
            ResponseModel responseModel = new ResponseModel
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };

            var addressalUsers = (await _dbContext.UserGroupMappings.Where(a => (string.IsNullOrEmpty(unitId) || a.UnitId == unitId)).ToListAsync()).Distinct().GroupBy(a => a.UnitId).Select(add => new
            {
                UnitId = add.First().UnitId,
                UnitName = add.First().UnitName,
                MappedUserCode = add.Select(a => new
                {
                    UserCode = a.UserCode,
                    UserDetails = a.UserDetails
                })
            });

            if (addressalUsers == null)
            {
                return new ResponseModel
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Message = "Addressal not found."
                };
            }

            responseModel.StatusCode = HttpStatusCode.OK;
            responseModel.Message = "Addressal list retrieved successfully.";
            responseModel.Data = addressalUsers;
            responseModel.DataLength = addressalUsers.Count();

            return responseModel;
        }

    }
}
