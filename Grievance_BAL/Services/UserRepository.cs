using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Reflection.Emit;
using Grievance_BAL.IServices;
using Grievance_DAL.DatabaseContext;
using Grievance_DAL.DbModels;
using Grievance_Model.DTOs.AppResponse;
using Grievance_Model.DTOs.Department;
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
                        Description = role.Description,
                        CreatedBy = Convert.ToInt32(role.UserCode),
                        CreatedDate = DateTime.Now,
                        IsActive = true,
                    };
                    _dbContext.AppRoles.Add(appRole);
                    resultCount = await _dbContext.SaveChangesAsync();

                    responseModel.StatusCode = HttpStatusCode.OK;
                    responseModel.Message = "Application Role Added Sucessfully.";
                    responseModel.Data = appRole.Id;
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
                    responseModel.Data = appRole.Id;
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
                var userRoles = (from g in _dbContext.Groups
                                 join r in _dbContext.AppRoles on g.RoleId equals r.Id
                                 join gm in _dbContext.UserGroupMappings on g.Id equals gm.GroupId
                                 where gm.UserCode == empCode && g.IsRoleGroup == true
                                 select r.RoleName
                             ).ToHashSet();

                var isAdmin = (from r in _dbContext.AppRoles
                               join rm in _dbContext.UserRoleMappings on r.Id equals rm.RoleId
                               where r.RoleName == Constant.AppRoles.Admin && rm.UserCode == empCode
                               select r
                               ).Any();

                if (isAdmin)
                    userRoles.Add(Constant.AppRoles.Admin);

                var isSuperAdmin = _configuration["SuperAdmin"].ToString() == empCode;
                if (isSuperAdmin)
                    userRoles.Add(Constant.AppRoles.SuperAdmin);

                userRoles.Add(Constant.AppRoles.User);

                if (userRoles != null)
                {
                    responseModel.StatusCode = HttpStatusCode.OK;
                    responseModel.Message = "User Role Details";
                    responseModel.Data = userRoles.ToList();
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
            ResponseModel responseModel = new()
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };

            if (mappings.GroupMasterId == 0 || string.IsNullOrEmpty(mappings.UnitId))
            {
                responseModel.Message = "GroupMasterId and UnitId are required";
                return responseModel;
            }

            var multipleUnit = mappings.UnitId.Split(',').Select(x => x.Trim()).ToList();
            var multipleUnitName = mappings.UnitName.Split(',').Select(x => x.Trim()).ToList();

            var userCodes = mappings.UserCodes?.Select(u => u.UserCode).ToList() ?? new List<string>();
            var userDetailsDict = mappings.UserCodes?.ToDictionary(u => u.UserCode, u => u.UserDetails) ?? new Dictionary<string, string>();

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                List<UserGroupMapping> newMappings = new();

                foreach (var unitId in multipleUnit)
                {
                    var existingMappings = _dbContext.UserGroupMappings
                        .Where(a => a.GroupId == mappings.GroupMasterId && a.UnitId == unitId)
                        .ToList();

                    if (userCodes.Count == 0)
                    {
                        if (existingMappings.Any())
                        {
                            _dbContext.UserGroupMappings.RemoveRange(existingMappings);
                        }
                        continue;
                    }

                    var usersToRemove = existingMappings.Where(a => !userCodes.Contains(a.UserCode)).ToList();
                    if (usersToRemove.Any())
                    {
                        _dbContext.UserGroupMappings.RemoveRange(usersToRemove);
                    }

                    var existingUserCodes = existingMappings.Select(a => a.UserCode).ToHashSet();

                    foreach (var userCode in userCodes)
                    {
                        if (!existingUserCodes.Contains(userCode))
                        {
                            newMappings.Add(new UserGroupMapping
                            {
                                GroupId = mappings.GroupMasterId,
                                UnitId = unitId,
                                UnitName = multipleUnitName[multipleUnit.IndexOf(unitId)],
                                UserCode = userCode,
                                UserDetails = userDetailsDict[userCode]
                            });
                        }
                    }
                }

                if (newMappings.Any())
                {
                    await _dbContext.UserGroupMappings.AddRangeAsync(newMappings);
                }

                var resultCount = await _dbContext.SaveChangesAsync();
                if (resultCount > 0)
                {
                    await transaction.CommitAsync();
                    responseModel.StatusCode = HttpStatusCode.OK;
                    responseModel.Message = "User Group Mapping Updated Successfully";
                    responseModel.Data = mappings;
                    responseModel.DataLength = newMappings.Count;
                }
                else
                {
                    responseModel.StatusCode = HttpStatusCode.NotModified;
                    responseModel.Message = "No changes made to User Group Mapping";
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

        //public async Task<ResponseModel> UpdateUserDepartmentMappingAsync(UserDeptMappingModel mappings)
        //{
        //    ResponseModel responseModel = new ResponseModel()
        //    {
        //        StatusCode = HttpStatusCode.BadRequest,
        //        Message = "Bad Request"
        //    };

        //    var multipleUnit = mappings.UnitId.Contains(",") ? mappings.UnitId.Split(",").ToList() : new List<string> { mappings.UnitId };
        //    var multipleUnitName = mappings.UnitName.Contains(",") ? mappings.UnitName.Split(",").ToList() : new List<string> { mappings.UnitName };

        //    var removeMapping = _dbContext.UserDepartmentMappings
        //        .Where(a => a.Department.Trim().ToLower() == mappings.Department.Trim().ToLower() &&
        //                    multipleUnit.Contains(a.UnitId) &&
        //                    (mappings.UserCodes == null || !mappings.UserCodes.Select(e => e.UserCode).Contains(a.UserCode)))
        //        .ToList();

        //    List<UserDepartmentMapping> newMapping = new();

        //    if (mappings.UserCodes != null && mappings.UserCodes.Count > 0)
        //    {
        //        var excludeMapping = _dbContext.UserDepartmentMappings
        //            .Where(a => a.Department.Trim().ToLower() == mappings.Department.Trim().ToLower() &&
        //                        multipleUnit.Contains(a.UnitId) &&
        //                        mappings.UserCodes.Select(e => e.UserCode).Contains(a.UserCode))
        //            .Select(a => new { a.UserCode, a.UnitId })
        //            .ToList();

        //        foreach (var emp in mappings.UserCodes)
        //        {
        //            for (int i = 0; i < multipleUnit.Count; i++)
        //            {
        //                var unitId = multipleUnit[i];
        //                var unitName = multipleUnitName[i];

        //                if (!excludeMapping.Any(b => b.UserCode == emp.UserCode && b.UnitId == unitId))
        //                {
        //                    newMapping.Add(new UserDepartmentMapping
        //                    {
        //                        Department = mappings.Department,
        //                        UnitId = unitId,
        //                        UnitName = unitName,
        //                        UserCode = emp.UserCode,
        //                        UserDetails = emp.UserDetails
        //                    });
        //                }
        //            }
        //        }
        //    }

        //    if (removeMapping.Any())
        //    {
        //        _dbContext.UserDepartmentMappings.RemoveRange(removeMapping);
        //    }

        //    if (newMapping.Any())
        //    {
        //        await _dbContext.UserDepartmentMappings.AddRangeAsync(newMapping);
        //    }

        //    var resultCount = await _dbContext.SaveChangesAsync();
        //    if (resultCount > 0)
        //    {
        //        responseModel.StatusCode = HttpStatusCode.OK;
        //        responseModel.Message = "User department mapping updated";
        //        responseModel.Data = mappings;
        //        responseModel.DataLength = mappings.UserCodes.Count;
        //    }
        //    else
        //    {
        //        responseModel.StatusCode = HttpStatusCode.NotModified;
        //        responseModel.Message = "User department mapping not updated";
        //    }

        //    return responseModel;
        //}

        public async Task<ResponseModel> UpdateUserDepartmentMappingAsync(UserDeptMappingModel mappings)
        {
            ResponseModel responseModel = new()
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };

            var multipleUnit = mappings.UnitId.Split(',').Select(x => x.Trim()).ToList();
            var multipleUnitName = mappings.UnitName.Split(',').Select(x => x.Trim()).ToList();

            var userCodes = mappings.UserCodes?.Select(u => u.UserCode).ToList() ?? new List<string>();
            var userDetailsDict = mappings.UserCodes?.ToDictionary(u => u.UserCode, u => u.UserDetails) ?? new Dictionary<string, string>();

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                List<UserDepartmentMapping> newMappings = new();

                foreach (var userCode in userCodes)
                {
                    var existingMappings = _dbContext.UserDepartmentMappings
                    .Where(a => multipleUnit.Contains(a.UnitId) && a.UserCode == userCode)
                    .ToList();

                    if (existingMappings.Any())
                    {
                        _dbContext.UserDepartmentMappings.RemoveRange(existingMappings);
                    }

                    foreach (var unitId in multipleUnit)
                    {
                        foreach (var department in mappings.Department)
                        {
                            newMappings.Add(new UserDepartmentMapping
                            {
                                Department = department,
                                UnitId = unitId,
                                UnitName = multipleUnitName[multipleUnit.IndexOf(unitId)],
                                UserCode = userCode,
                                UserDetails = userDetailsDict[userCode]
                            });
                        }
                    }
                }

                if (newMappings.Any())
                {
                    await _dbContext.UserDepartmentMappings.AddRangeAsync(newMappings);
                }

                var resultCount = await _dbContext.SaveChangesAsync();
                if (resultCount > 0)
                {
                    await transaction.CommitAsync();
                    responseModel.StatusCode = HttpStatusCode.OK;
                    responseModel.Message = "User Department Mapping Updated Successfully";
                    responseModel.Data = mappings;
                    responseModel.DataLength = newMappings.Count;
                }
                else
                {
                    responseModel.StatusCode = HttpStatusCode.NotModified;
                    responseModel.Message = "No changes made to User Department Mapping";
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

        public async Task<ResponseModel> GetAddressalListAsync(string? unitId)
        {
            ResponseModel responseModel = new ResponseModel
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };

            var addressalUsers = (await _dbContext.UserGroupMappings.Where(a => (string.IsNullOrEmpty(unitId) || a.UnitId == unitId)).Include(a => a.Group).ToListAsync()).Distinct().GroupBy(a => a.UnitId).Select(add => new
            {
                UnitId = add.First().UnitId,
                UnitName = add.First().UnitName,
                MappedUserCode = add.Select(a => new
                {
                    GroupId = a.GroupId,
                    GroupDetails = a.Group,
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

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task<ResponseModel> AddUpdateGroupNewAsync(GroupMasterModel groupModel)
        {
            ResponseModel responseModel = new ResponseModel()
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };
            if (groupModel != null)
            {
                var roleId = groupModel.RoleId;
                if (groupModel.IsRoleGroup == true && groupModel.RoleId == 0)
                {
                    RoleModel newRole = new RoleModel
                    {
                        RoleName = groupModel.GroupName,
                        Description = groupModel.Description,
                        UserCode = groupModel.CreatedBy ?? string.Empty,
                    };
                    roleId = Convert.ToInt32((await AddUpdateRoleAsync(newRole)).Data);
                }

                var existingGroup = _dbContext.Groups.Where(group => group.Id == groupModel.Id).FirstOrDefault();
                if (existingGroup == null)
                {
                    var newGroup = new GroupMaster()
                    {
                        GroupName = groupModel.GroupName,
                        Description = groupModel.Description ?? string.Empty,
                        IsRoleGroup = groupModel.IsRoleGroup ?? false,
                        RoleId = roleId != 0 ? roleId : null,
                        IsServiceCategory = groupModel.IsServiceCategory ?? false,
                        ParentGroupId = groupModel.ParentGroupId == null || groupModel.ParentGroupId == 0 ? null : groupModel.ParentGroupId,
                        UnitId = groupModel.UnitId,
                        CreatedBy = Convert.ToInt32(groupModel.CreatedBy),
                        CreatedDate = DateTime.Now,
                        IsActive = true,
                    };
                    _dbContext.Groups.Add(newGroup);
                    await _dbContext.SaveChangesAsync();

                    var parentGroupId = newGroup.Id;
                    if (groupModel.MappedUser != null)
                    {
                        var mappingData = new UserGroupMasterMappingModel
                        {
                            GroupMasterId = parentGroupId,
                            UnitId = groupModel.UnitId,
                            UnitName = groupModel.UnitName,
                            UserCodes = groupModel.MappedUser.Select(user => new GroupUser
                            {
                                UserCode = user.UserCode,
                                UserDetails = user.UserDetail
                            }).ToList()
                        };
                        await UpdateUserGroupMappingAsync(mappingData);

                        UserRoleMappingModel roleMappings = new UserRoleMappingModel
                        {
                            UnitId = groupModel.UnitId,
                            UnitName = groupModel.UnitName,
                            RoleId = new List<int> { Convert.ToInt32(roleId) },
                            UserCode = string.Join(",", groupModel.MappedUser.Select(a => a.UserCode)),
                            UserDetails = string.Join(",", groupModel.MappedUser.Select(a => a.UserDetail))
                        };
                        await UpdateUserRoleMappingAsync(roleMappings);
                    }

                    var childGroup = groupModel.ChildGroup;
                    while (childGroup != null && !string.IsNullOrEmpty(childGroup.GroupName))
                    {
                        roleId = childGroup.RoleId;
                        if (childGroup.IsRoleGroup == true && childGroup.RoleId == 0)
                        {
                            RoleModel newRole = new RoleModel
                            {
                                RoleName = childGroup.GroupName,
                                Description = childGroup.Description,
                                UserCode = childGroup.CreatedBy ?? string.Empty,
                            };
                            roleId = Convert.ToInt32((await AddUpdateRoleAsync(newRole)).Data);
                        }

                        var newChildGroup = new GroupMaster()
                        {
                            GroupName = childGroup.GroupName,
                            Description = childGroup.Description ?? string.Empty,
                            IsRoleGroup = childGroup.IsRoleGroup ?? false,
                            RoleId = roleId != 0 ? roleId : null,
                            IsServiceCategory = childGroup.IsServiceCategory ?? false,
                            ParentGroupId = parentGroupId,
                            UnitId = childGroup.UnitId,
                            CreatedBy = Convert.ToInt32(childGroup.CreatedBy),
                            CreatedDate = DateTime.Now,
                            IsActive = true,
                        };
                        _dbContext.Groups.Add(newChildGroup);
                        await _dbContext.SaveChangesAsync();

                        parentGroupId = newChildGroup.Id;
                        if (childGroup.MappedUser != null)
                        {
                            var mappingData = new UserGroupMasterMappingModel
                            {
                                GroupMasterId = parentGroupId,
                                UnitId = groupModel.UnitId,
                                UnitName = groupModel.UnitName,
                                UserCodes = childGroup.MappedUser.Select(user => new GroupUser
                                {
                                    UserCode = user.UserCode,
                                    UserDetails = user.UserDetail
                                }).ToList()
                            };
                            await UpdateUserGroupMappingAsync(mappingData);

                            UserRoleMappingModel roleMappings = new UserRoleMappingModel
                            {
                                UnitId = groupModel.UnitId,
                                UnitName = groupModel.UnitName,
                                RoleId = new List<int> { Convert.ToInt32(roleId) },
                                UserCode = string.Join(",", childGroup.MappedUser.Select(a => a.UserCode)),
                                UserDetails = string.Join(",", childGroup.MappedUser.Select(a => a.UserDetail))
                            };
                            await UpdateUserRoleMappingAsync(roleMappings);
                        }
                        childGroup = childGroup.ChildGroup;
                    }

                    responseModel.StatusCode = HttpStatusCode.OK;
                    responseModel.Message = "Group Master Added Sucessfully.";
                    responseModel.Data = new { groupId = newGroup.Id };
                }
                else
                {
                    existingGroup.GroupName = groupModel.GroupName;
                    existingGroup.Description = groupModel.Description ?? string.Empty;
                    existingGroup.IsRoleGroup = groupModel.IsRoleGroup ?? false;
                    existingGroup.RoleId = roleId != 0 ? roleId : null;
                    existingGroup.IsServiceCategory = groupModel.IsServiceCategory ?? false;
                    existingGroup.ParentGroupId = groupModel.ParentGroupId == null || groupModel.ParentGroupId == 0 ? null : groupModel.ParentGroupId;
                    existingGroup.UnitId = groupModel.UnitId;
                    existingGroup.ModifyBy = Convert.ToInt32(groupModel.CreatedBy);
                    existingGroup.ModifyDate = DateTime.Now;
                    existingGroup.IsActive = true;

                    _dbContext.Groups.Update(existingGroup);
                    await _dbContext.SaveChangesAsync();

                    var parentGroupId = existingGroup.Id;
                    if (groupModel.MappedUser != null)
                    {
                        var mappingData = new UserGroupMasterMappingModel
                        {
                            GroupMasterId = parentGroupId,
                            UnitId = groupModel.UnitId,
                            UnitName = groupModel.UnitName,
                            UserCodes = groupModel.MappedUser.Select(user => new GroupUser
                            {
                                UserCode = user.UserCode,
                                UserDetails = user.UserDetail
                            }).ToList()
                        };
                        await UpdateUserGroupMappingAsync(mappingData);

                        UserRoleMappingModel roleMappings = new UserRoleMappingModel
                        {
                            UnitId = groupModel.UnitId,
                            UnitName = groupModel.UnitName,
                            RoleId = new List<int> { Convert.ToInt32(roleId) },
                            UserCode = string.Join(",", groupModel.MappedUser.Select(a => a.UserCode)),
                            UserDetails = string.Join(",", groupModel.MappedUser.Select(a => a.UserDetail))
                        };
                        await UpdateUserRoleMappingAsync(roleMappings);
                    }

                    var childGroup = groupModel.ChildGroup;
                    while (childGroup != null && !string.IsNullOrEmpty(childGroup.GroupName))
                    {
                        roleId = childGroup.RoleId;
                        if (childGroup.IsRoleGroup == true && childGroup.RoleId == 0)
                        {
                            RoleModel newRole = new RoleModel
                            {
                                RoleName = childGroup.GroupName,
                                Description = childGroup.Description,
                                UserCode = childGroup.CreatedBy ?? string.Empty,
                            };
                            roleId = Convert.ToInt32((await AddUpdateRoleAsync(newRole)).Data);
                        }

                        var newChildGroup = new GroupMaster()
                        {
                            GroupName = childGroup.GroupName,
                            Description = childGroup.Description ?? string.Empty,
                            IsRoleGroup = childGroup.IsRoleGroup ?? false,
                            RoleId = roleId != 0 ? roleId : null,
                            IsServiceCategory = childGroup.IsServiceCategory ?? false,
                            ParentGroupId = parentGroupId,
                            UnitId = childGroup.UnitId,
                            CreatedBy = Convert.ToInt32(childGroup.CreatedBy),
                            CreatedDate = DateTime.Now,
                            IsActive = true,
                        };
                        _dbContext.Groups.Add(newChildGroup);
                        await _dbContext.SaveChangesAsync();

                        parentGroupId = newChildGroup.Id;
                        if (childGroup.MappedUser != null)
                        {
                            var mappingData = new UserGroupMasterMappingModel
                            {
                                GroupMasterId = parentGroupId,
                                UnitId = groupModel.UnitId,
                                UnitName = groupModel.UnitName,
                                UserCodes = childGroup.MappedUser.Select(user => new GroupUser
                                {
                                    UserCode = user.UserCode,
                                    UserDetails = user.UserDetail
                                }).ToList()
                            };
                            await UpdateUserGroupMappingAsync(mappingData);

                            UserRoleMappingModel roleMappings = new UserRoleMappingModel
                            {
                                UnitId = groupModel.UnitId,
                                UnitName = groupModel.UnitName,
                                RoleId = new List<int> { Convert.ToInt32(roleId) },
                                UserCode = string.Join(",", childGroup.MappedUser.Select(a => a.UserCode)),
                                UserDetails = string.Join(",", childGroup.MappedUser.Select(a => a.UserDetail))
                            };
                            await UpdateUserRoleMappingAsync(roleMappings);
                        }
                        childGroup = childGroup.ChildGroup;
                    }

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

        public async Task<ResponseModel> GetOrgGroupHierarchyAsync(string unitId)
        {
            ResponseModel responseModel = new ResponseModel
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };
            var corporateUnitId = _configuration["FinalCommiteeUnit"].ToString();

            var rootGroup = await _dbContext.Groups
                .Where(g => g.ParentGroupId == null && ((corporateUnitId == unitId) ? g.UnitId == corporateUnitId : g.UnitId != corporateUnitId) && (g.IsActive == true || g.IsActive == null))
                .Select(g => new { g.Id })
                .FirstOrDefaultAsync();

            if (rootGroup != null)
            {
                var groupHierarchy = await GetGroupHierarchyAsync(rootGroup.Id, unitId);

                responseModel.StatusCode = HttpStatusCode.OK;
                responseModel.Message = "Hierarchy retrieved successfully.";
                responseModel.Data = groupHierarchy;
                responseModel.DataLength = 1;
            }

            return responseModel;
        }

        private async Task<GroupHierarchyResponse> GetGroupHierarchyAsync(int groupId, string unitId)
        {
            var group = await _dbContext.Groups
                .Where(g => g.Id == groupId && (g.IsActive == null || g.IsActive == true))
                .Select(g => new GroupHierarchyResponse
                {
                    Id = g.Id,
                    GroupName = g.GroupName,
                    Description = g.Description,
                    IsRoleGroup = g.IsRoleGroup,
                    RoleId = g.RoleId,
                    IsServiceCategory = g.IsServiceCategory,
                    UnitId = g.UnitId
                })
                .FirstOrDefaultAsync();

            if (group != null)
            {
                group.MappedUser = await _dbContext.UserGroupMappings.Where(a => a.GroupId == group.Id && a.UnitId == unitId).Select(m => new GroupMasterUser
                {
                    UserCode = m.UserCode,
                    UserDetail = m.UserDetails,
                    Departments = _dbContext.UserDepartmentMappings.Where(a => a.UnitId == unitId && a.UserCode == m.UserCode).Select(a => a.Department).ToList(),
                }).ToListAsync();

                var childGroups = await _dbContext.Groups.Where(g => g.ParentGroupId == groupId && (g.IsActive == null || g.IsActive == true)).ToListAsync();
                group.ChildGroups = new List<GroupHierarchyResponse>();
                foreach (var child in childGroups)
                {
                    var childHierarchy = await GetGroupHierarchyAsync(child.Id, unitId);
                    group.ChildGroups.Add(childHierarchy);
                }

            }

            return group;
        }

        public async Task<UnitRoleUserModel> GetUnitRoleUsersAsync(string unitId, int roleId = 0)
        {
            UnitRoleUserModel mappedUsers = new UnitRoleUserModel();

            var assignedUser = await (from g in _dbContext.Groups
                                      join r in _dbContext.AppRoles on g.RoleId equals r.Id
                                      join gm in _dbContext.UserGroupMappings on g.Id equals gm.GroupId
                                      where gm.UnitId == unitId
                                      && (roleId == 0 || g.RoleId == roleId)
                                      select new UnitRoleUsers
                                      {
                                          RoleId = r.Id,
                                          RoleName = r.RoleName,
                                          UserCode = gm.UserCode,
                                          UserDetails = gm.UserDetails,
                                          Group = new { GroupId = g.Id, GroupName = g.GroupName }
                                      }
                         ).ToListAsync();

            mappedUsers.UnitId = unitId;
            mappedUsers.MappedUser = assignedUser;

            return mappedUsers;
        }

        public async Task<ResponseModel> GetServiceMasterAsync(bool isCorporate)
        {
            ResponseModel responseModel = new ResponseModel
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };

            var corporateUnitId = _configuration["FinalCommiteeUnit"].ToString();
            var serviceList = await _dbContext.Groups.Where(s => (s.IsActive == null || s.IsActive == true) && (isCorporate ? s.UnitId == corporateUnitId : s.UnitId != corporateUnitId)).Select(s => new ServiceGroupModel
            {
                Id = s.Id,
                GroupName = s.GroupName,
                IsServiceCategory = s.IsServiceCategory,
                IsActive = s.IsActive,
                ParentGroupId = s.ParentGroupId

            }).ToListAsync();
            var finalServiceHierarchy = GetServiceGroups(serviceList);

            if (finalServiceHierarchy.Count == 0)
            {
                responseModel.Message = "Service Not Found";
                responseModel.StatusCode = HttpStatusCode.NotFound;
            }
            else
            {
                responseModel.Message = "Service List Found";
                responseModel.Data = finalServiceHierarchy;
                responseModel.StatusCode = HttpStatusCode.OK;
            }

            return responseModel;
        }

        public static List<ServiceGroupModel> GetServiceGroups(List<ServiceGroupModel> allGroups)
        {
            var groupDict = allGroups.ToDictionary(g => g.Id);

            Func<int?, List<ServiceGroupModel>> BuildHierarchy = null;
            BuildHierarchy = (parentId) =>
            {
                return allGroups
                    .Where(g => g.ParentGroupId == parentId)
                    .SelectMany(g =>
                    {
                        if (g.IsServiceCategory == true)
                        {
                            return new List<ServiceGroupModel>
                            {
                        new ServiceGroupModel
                        {
                            Id = g.Id,
                            GroupName = g.GroupName,
                            ParentGroupId = g.ParentGroupId,
                            IsActive = g.IsActive,
                            IsServiceCategory = g.IsServiceCategory,
                            ChildGroup = BuildHierarchy(g.Id)
                        }
                            };
                        }
                        else
                        {
                            return BuildHierarchy(g.Id);
                        }
                    })
                    .ToList();
            };

            return BuildHierarchy(null);
        }


    }
}
