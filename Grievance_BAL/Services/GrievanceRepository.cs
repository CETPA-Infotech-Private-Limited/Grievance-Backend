using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Text;
using Grievance_BAL.IServices;
using Grievance_DAL.DatabaseContext;
using Grievance_DAL.DbModels;
using Grievance_Model.DTOs.AppResponse;
using Grievance_Model.DTOs.Grievance;
using Grievance_Utility;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using static Grievance_BAL.Services.AccountRepository;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Grievance_Model.DTOs.Notification;
using System.Globalization;
using Microsoft.VisualBasic;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.ComponentModel.Design;
using Grievance_Model.DTOs.Dashboard;
using System.Text.Json;

namespace Grievance_BAL.Services
{
    public class GrievanceRepository : IGrievanceRepository
    {
        private readonly GrievanceDbContext _dbContext;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICommonRepository _common;
        private readonly INotificationRepository _notificationRepository;
        private readonly IConfiguration _configuration;
        public GrievanceRepository(GrievanceDbContext dbContext, IEmployeeRepository employeeRepository, IUserRepository userRepository, ICommonRepository common, INotificationRepository notificationRepository, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _employeeRepository = employeeRepository;
            _userRepository = userRepository;
            _common = common;
            _notificationRepository = notificationRepository;
            _configuration = configuration;
        }

        public async Task<ResponseModel> GetGrievanceListAsync(string userCode, int pageNumber = 1, int pageSize = 10)
        {
            ResponseModel responseModel = new ResponseModel
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };

            var user = await _employeeRepository.GetEmployeeDetailsWithEmpCode(Convert.ToInt32(userCode));

            if (user == null)
            {
                return new ResponseModel
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Message = "User not found."
                };
            }

            var appRoles = (List<string>)(await _userRepository.GetUserRolesAsync(userCode)).Data ?? new List<string>();
            if (appRoles.Count == 1 && appRoles.First() == "User")
            {
                return new ResponseModel
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Message = "User is neither Addressal nor NodalOfficer/CGM."
                };
            }

            bool isSuperAdmin = appRoles.Any(r => r == Constant.AppRoles.SuperAdmin);
            bool isAddressal = appRoles.Any(r => r == Constant.AppRoles.Addressal || r == Constant.AppRoles.Committee || r == Constant.AppRoles.HOD);
            bool isNodalOrCGM = appRoles.Any(r => r == Constant.AppRoles.NodalOfficer || r == Constant.AppRoles.UnitCGM || r == Constant.AppRoles.Admin);

            IQueryable<GrievanceMaster> query = _dbContext.GrievanceMasters.AsQueryable();

            if (isSuperAdmin)
            {
                // to ignore the below conditions
            }
            else if (isNodalOrCGM)
            {
                var userRoles = (await _dbContext.UserRoleMappings.Where(r => r.UserCode == userCode && (r.Role.RoleName == Constant.AppRoles.NodalOfficer || r.Role.RoleName == Constant.AppRoles.UnitCGM || r.Role.RoleName == Constant.AppRoles.Admin)).Select(x => x.UnitId).ToListAsync()).Distinct();
                query = query.Where(g => userRoles.Contains(g.UnitId));
            }
            else if (isAddressal)
            {
                var userGroups = _dbContext.UserGroupMappings
                    .Where(x => x.UserCode == userCode)
                    .Select(x => new { x.GroupId, x.UnitId });

                var resolvedGrievanceIds = _dbContext.GrievanceProcesses
                    .Where(gp => gp.CreatedBy == Convert.ToInt32(userCode) &&
                        (gp.StatusId == (int)Grievance_Utility.GrievanceStatus.Resolved ||
                         gp.StatusId == (int)Grievance_Utility.GrievanceStatus.Closed))
                    .Select(gp => gp.GrievanceMasterId);

                var grievanceQuery = _dbContext.GrievanceMasters.AsQueryable();

                bool isUnit396 = userGroups.Any(x => x.UnitId == "396");
                var grievanceMasterIds = new List<int>();
                if (isUnit396)
                {
                    var userDepartments = _dbContext.UserDepartmentMappings
                            .Where(x => x.UserCode == userCode)
                            .Select(x => new { x.Department, x.UnitId });

                    if (appRoles.Any(r => r == Constant.AppRoles.Committee))
                    {
                        grievanceMasterIds.AddRange(from gm in grievanceQuery
                                                    join gp in _dbContext.GrievanceProcesses on gm.Id equals gp.GrievanceMasterId
                                                    join sm in _dbContext.Services on gm.ServiceId equals sm.Id
                                                    join gpMst in _dbContext.Groups on sm.GroupMasterId equals gpMst.Id
                                                    where (gp.Id == _dbContext.GrievanceProcesses
                                                              .Where(t => t.GrievanceMasterId == gm.Id)
                                                              .OrderByDescending(t => t.Id)
                                                              .Select(t => t.Id)
                                                              .FirstOrDefault() && gp.AssignedUserCode == userCode)
                                                           || ((gp.AssignedUserCode == userCode && gp.StatusId != (int)Grievance_Utility.GrievanceStatus.Resolved)
                                                           || gm.Round == (int)Grievance_Utility.GrievanceRound.Third)
                                                           || resolvedGrievanceIds.Contains(gm.Id)
                                                    select gm.Id);
                    }

                    if (appRoles.Any(r => r == Constant.AppRoles.HOD))
                    {
                        grievanceMasterIds.AddRange(from gm in grievanceQuery
                                                    join gp in _dbContext.GrievanceProcesses on gm.Id equals gp.GrievanceMasterId
                                                    join sm in _dbContext.Services on gm.ServiceId equals sm.Id
                                                    join gpMst in _dbContext.Groups on sm.GroupMasterId equals gpMst.Id
                                                    where (gp.Id == _dbContext.GrievanceProcesses
                                                              .Where(t => t.GrievanceMasterId == gm.Id)
                                                              .OrderByDescending(t => t.Id)
                                                              .Select(t => t.Id)
                                                              .FirstOrDefault() && gp.AssignedUserCode == userCode)
                                                    || (userGroups.Select(ug => ug.GroupId).Contains(gpMst.Id))
                                                           || (userDepartments.Select(a => a.Department.Trim().ToLower()).Contains(gm.Department.Trim().ToLower())
                                                           && (gp.AssignedUserCode == userCode && gp.StatusId != (int)Grievance_Utility.GrievanceStatus.Resolved)
                                                           || gm.StatusId == (int)Grievance_Utility.GrievanceStatus.Created)
                                                           || resolvedGrievanceIds.Contains(gm.Id)
                                                    select gm.Id);
                    }

                    if (appRoles.Any(r => r == Constant.AppRoles.Addressal))
                    {
                        grievanceMasterIds.AddRange(from gm in grievanceQuery
                                                    join gp in _dbContext.GrievanceProcesses on gm.Id equals gp.GrievanceMasterId
                                                    join sm in _dbContext.Services on gm.ServiceId equals sm.Id
                                                    join gpMst in _dbContext.Groups on sm.GroupMasterId equals gpMst.Id
                                                    where (gp.Id == _dbContext.GrievanceProcesses
                                                              .Where(t => t.GrievanceMasterId == gm.Id)
                                                              .OrderByDescending(t => t.Id)
                                                              .Select(t => t.Id)
                                                              .FirstOrDefault() && gp.AssignedUserCode == userCode)
                                                    || (userGroups.Select(ug => ug.GroupId).Contains(gpMst.Id))
                                                           && (userDepartments.Select(a => a.Department.Trim().ToLower()).Contains(gm.Department.Trim().ToLower())
                                                           && (gp.AssignedUserCode == userCode && gp.StatusId != (int)Grievance_Utility.GrievanceStatus.Resolved)
                                                           || gm.StatusId == (int)Grievance_Utility.GrievanceStatus.Created)
                                                           || resolvedGrievanceIds.Contains(gm.Id)
                                                    select gm.Id);

                    }
                }
                else
                {
                    grievanceMasterIds.AddRange(from gm in grievanceQuery
                                                join gp in _dbContext.GrievanceProcesses on gm.Id equals gp.GrievanceMasterId
                                                join sm in _dbContext.Services on gm.ServiceId equals sm.Id
                                                join gpMst in _dbContext.Groups on sm.GroupMasterId equals gpMst.Id
                                                where userGroups.Select(ug => ug.GroupId).Contains(gpMst.Id)
                                                && ((gp.AssignedUserCode == userCode && gp.StatusId != (int)Grievance_Utility.GrievanceStatus.Resolved)
                                                       || gm.StatusId == (int)Grievance_Utility.GrievanceStatus.Created)
                                                       || resolvedGrievanceIds.Contains(gm.Id)
                                                select gm.Id);
                }
                grievanceMasterIds = grievanceMasterIds.Distinct().ToList();
                query = query.Where(g => grievanceMasterIds.Contains(g.Id) && g.CreatedBy != Convert.ToInt32(userCode));
            }

            int totalRecords = await query.CountAsync();

            //var grievances = await query
            //    .Skip((pageNumber - 1) * pageSize)
            //    .Take(pageSize)
            //    .ToListAsync();

            var grievances = await (from gm in _dbContext.GrievanceMasters
                                    join gp in _dbContext.GrievanceProcesses on gm.Id equals gp.GrievanceMasterId
                                    where query.Select(a => a.Id).Contains(gm.Id)
                                    && (gp.Id == _dbContext.GrievanceProcesses
                                              .Where(t => t.GrievanceMasterId == gm.Id)
                                              .OrderByDescending(t => t.Id)
                                              .Select(t => t.Id)
                                              .FirstOrDefault())
                                    select new
                                    {
                                        Id = gm.Id,
                                        Title = gm.Title,
                                        Description = gm.Description,
                                        ServiceId = gm.ServiceId,
                                        IsInternal = gm.IsInternal,
                                        UserCode = gm.UserCode,
                                        UserEmail = gm.UserEmail,
                                        UserDetails = gm.UserDetails,
                                        UnitId = gm.UnitId,
                                        UnitName = gm.UnitName,
                                        Department = gm.Department,
                                        Round = gm.Round,
                                        StatusId = gm.StatusId,
                                        RowStatus = gm.RowStatus,
                                        AssignedUserCode = gp.AssignedUserCode,
                                        AssignedUserDetails = gp.AssignedUserDetails,
                                        CreatedDate = gm.CreatedDate,
                                        CreatedBy = gm.CreatedBy,
                                        ModifiedDate = gp.CreatedDate,
                                        ModifiedBy = gp.CreatedBy
                                    })
                        .OrderByDescending(g => g.CreatedDate)
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();

            responseModel.StatusCode = HttpStatusCode.OK;
            responseModel.Message = "Grievance list retrieved successfully.";
            responseModel.Data = new
            {
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                Data = grievances
            };

            return responseModel;
        }

        public async Task<ResponseModel> MyGrievanceListAsync(string userCode, int pageNumber = 1, int pageSize = 10)
        {
            ResponseModel responseModel = new ResponseModel
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };

            IQueryable<GrievanceMaster> query = _dbContext.GrievanceMasters
                .Where(g => g.UserCode == userCode);

            int totalRecords = await query.CountAsync();
            //var grievances = await query
            //    .Skip((pageNumber - 1) * pageSize)
            //    .Take(pageSize)
            //    .ToListAsync();

            var grievances = await (from gm in _dbContext.GrievanceMasters
                                    join gs in _dbContext.GrievanceProcesses on gm.Id equals gs.GrievanceMasterId
                                    where (gs.Id == _dbContext.GrievanceProcesses
                                              .Where(t => t.GrievanceMasterId == gm.Id)
                                              .OrderByDescending(t => t.Id)
                                              .Select(t => t.Id)
                                              .FirstOrDefault())
                                           && query.Select(a => a.Id).Contains(gm.Id)
                                    select new
                                    {
                                        Id = gm.Id,
                                        Title = gm.Title,
                                        Description = gm.Description,
                                        ServiceId = gm.ServiceId,
                                        IsInternal = gm.IsInternal,
                                        UserCode = gm.UserCode,
                                        UserEmail = gm.UserEmail,
                                        UserDetails = gm.UserDetails,
                                        UnitId = gm.UnitId,
                                        UnitName = gm.UnitName,
                                        Department = gm.Department,
                                        Round = gm.Round,
                                        StatusId = gm.StatusId,
                                        RowStatus = gm.RowStatus,
                                        AssignedUserCode = gs.AssignedUserCode,
                                        AssignedUserDetails = gs.AssignedUserDetails,
                                        CreatedDate = gm.CreatedDate,
                                        CreatedBy = gm.CreatedBy,
                                        ModifiedDate = gs.CreatedDate,
                                        ModifiedBy = gs.CreatedBy
                                    }).Distinct().Skip((pageNumber - 1) * pageSize).Take(pageSize).OrderByDescending(g => g.CreatedDate).ToListAsync();

            responseModel.StatusCode = HttpStatusCode.OK;
            responseModel.Message = "My grievances retrieved successfully.";
            responseModel.Data = new
            {
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                Data = grievances
            };

            return responseModel;
        }

        public async Task<ResponseModel> AddUpdateGrievanceAsync(GrievanceProcessDTO grievanceModel)
        {
            ResponseModel responseModel = new ResponseModel
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Bad Request",
            };

            IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync();
            var grievanceMaster = _dbContext.GrievanceMasters.Where(x => x.Id == grievanceModel.GrievanceMasterId).FirstOrDefault();
            var userDetails = await _employeeRepository.GetEmployeeDetailsWithEmpCode(Convert.ToInt32(grievanceModel.UserCode));
            if (grievanceMaster == null)
            {
                grievanceMaster = new GrievanceMaster()
                {
                    Title = grievanceModel.Title,
                    Description = grievanceModel.Description,
                    ServiceId = grievanceModel.ServiceId,
                    IsInternal = grievanceModel.IsInternal,
                    UserCode = grievanceModel.IsInternal ? grievanceModel.UserCode : string.Empty,
                    UserEmail = grievanceModel.IsInternal ? userDetails.empEmail : grievanceModel.UserEmail,
                    UserDetails = grievanceModel.IsInternal ? (!string.IsNullOrEmpty(userDetails.empName) ? userDetails.empName : "NA") + (!string.IsNullOrEmpty(userDetails.empCode) ? " (" + userDetails.empCode + ")" : "") + (!string.IsNullOrEmpty(userDetails.designation) ? " - " + userDetails.designation : "") + (!string.IsNullOrEmpty(userDetails.department) ? " | " + userDetails.department : "") : string.Empty,
                    UnitId = userDetails.unitId.ToString(),
                    UnitName = userDetails.units,
                    Department = userDetails.department,
                    Round = (int)Grievance_Utility.GrievanceRound.First,
                    StatusId = (int)Grievance_Utility.GrievanceStatus.Created,
                    RowStatus = Grievance_Utility.RowStatus.Active,

                    CreatedBy = Convert.ToInt32(userDetails?.empCode),
                    CreatedDate = DateTime.Now
                };
                _dbContext.GrievanceMasters.Add(grievanceMaster);
                _dbContext.SaveChanges();
            }
            else
            {
                grievanceMaster.ServiceId = grievanceModel.ServiceId;
                grievanceMaster.StatusId = grievanceModel.StatusId ?? grievanceMaster.StatusId;

                grievanceMaster.ModifyBy = Convert.ToInt32(grievanceModel.UserCode);
                grievanceMaster.ModifyDate = DateTime.Now;

                _dbContext.GrievanceMasters.Update(grievanceMaster);
                _dbContext.SaveChanges();
            }

            if (grievanceMaster.Id != 0)
            {
                string addressalCode = string.Empty;
                string addressalDetail = string.Empty;
                if (string.IsNullOrEmpty(grievanceModel.AssignedUserCode))
                {
                    var FinalCommiteeUnit = _configuration["FinalCommiteeUnit"].ToString();
                    var addressal = (from sm in _dbContext.Services
                                     join ug in _dbContext.UserGroupMappings on sm.GroupMasterId equals ug.GroupId
                                     join ud in _dbContext.UserDepartmentMappings on ug.UserCode equals ud.UserCode
                                     where (userDetails.unitId.ToString() != FinalCommiteeUnit
                                     || ud.Department.Trim().ToLower() == userDetails.department.Trim().ToLower())
                                     && ug.GroupId == sm.GroupMasterId && ug.UnitId == userDetails.unitId.ToString()
                                     && sm.Id == grievanceMaster.ServiceId
                                     select new
                                     {
                                         UserCode = ug.UserCode,
                                         UserDetails = ug.UserDetails
                                     }).FirstOrDefault();
                    if (addressal != null)
                    {
                        addressalCode = addressal.UserCode;
                        addressalDetail = addressal.UserDetails;
                    }
                }
                else
                {
                    addressalCode = grievanceModel.AssignedUserCode;
                    addressalDetail = grievanceModel.AssignedUserDetails;
                }

                //if (string.IsNullOrEmpty(addressalCode))
                //{
                //    responseModel.StatusCode = HttpStatusCode.BadRequest;
                //    responseModel.Message = "No addressal found for this department of category.";

                //    return responseModel;
                //}

                GrievanceProcess grievanceProcessObj = new GrievanceProcess()
                {
                    GrievanceMasterId = grievanceMaster.Id,

                    Title = grievanceModel.Title,
                    Description = grievanceModel.Description,
                    ServiceId = grievanceModel.ServiceId,
                    Round = grievanceModel.Round ?? grievanceMaster.Round,
                    StatusId = grievanceMaster == null ? (int)Grievance_Utility.GrievanceStatus.Created : grievanceModel.StatusId ?? grievanceMaster.StatusId,
                    RowStatus = Grievance_Utility.RowStatus.Active,

                    AssignedUserCode = string.IsNullOrEmpty(addressalCode) ? string.Empty : addressalCode,
                    AssignedUserDetails = string.IsNullOrEmpty(addressalDetail) ? string.Empty : addressalDetail,

                    CreatedBy = Convert.ToInt32(grievanceModel.UserCode),
                    CreatedDate = DateTime.Now
                };

                _dbContext.GrievanceProcesses.Add(grievanceProcessObj);
                _dbContext.SaveChanges();

                if (!string.IsNullOrEmpty(grievanceModel.CommentText))
                {
                    CommentDetail comment = new CommentDetail()
                    {
                        CommentText = grievanceModel.CommentText ?? string.Empty,
                        CommentedBy = grievanceModel.UserCode,
                        CommentDate = DateTime.Now,
                        CommentType = Constant.CommentType.GrievanceProcess,
                        ReferenceId = grievanceProcessObj.Id,

                        CreatedBy = Convert.ToInt32(grievanceModel.UserCode),
                        CreatedDate = DateTime.Now,
                    };
                    _dbContext.Comments.Add(comment);
                    _dbContext.SaveChanges();
                }

                if (grievanceModel.Attachments != null && grievanceModel.Attachments.Count > 0)
                {
                    var response = await _common.UploadDocumets(grievanceModel.Attachments, "GrievanceAttachment");
                    if (response != null && response.Count != 0)
                    {
                        foreach (var item in response)
                        {
                            AttachmentDetail uploadDocument = new AttachmentDetail()
                            {
                                Type = Constant.CommentType.GrievanceProcess,
                                ReferenceId = grievanceProcessObj.Id,
                                FileName = item.fileName,
                                FilePath = item.filePath,

                                CreatedBy = Convert.ToInt32(grievanceModel.UserCode),
                                CreatedDate = DateTime.Now,
                            };

                            _dbContext.Attachments.Add(uploadDocument);
                            _dbContext.SaveChanges();
                        }
                    }
                }

                // to send the resolution link to requestor or sent to commitee mail
                if (grievanceProcessObj.StatusId == (int)Grievance_Utility.GrievanceStatus.Resolved)
                {
                    if (grievanceMaster.Round < 3)
                    {
                        ResolutionDetail resolutionDetail = new ResolutionDetail
                        {
                            UserCode = grievanceMaster?.UserCode ?? string.Empty,
                            UserEmail = grievanceMaster?.UserEmail ?? string.Empty,

                            GrievanceMasterId = grievanceMaster?.Id ?? 0,
                            GrievanceProcessId = grievanceProcessObj.Id,
                            Round = grievanceMaster?.Round ?? 1,

                            ResolutionDT = DateTime.Now,
                            ResolverCode = grievanceModel.AssignedUserCode,
                            ResolverDetails = grievanceModel.AssignedUserDetails,
                            AcceptLink = grievanceModel.BaseUrl + "/" + Guid.NewGuid().ToString() + "$",
                            RejectLink = grievanceModel.BaseUrl + "/" + Guid.NewGuid().ToString(),
                            ResolutionStatus = Constant.ResolutionStatus.Pending,

                            CreatedDate = DateTime.Now,
                            CreatedBy = Convert.ToInt32(grievanceModel.AssignedUserCode)
                        };
                        await SendResolutionLink(resolutionDetail);
                    }
                }

                await transaction.CommitAsync();

                responseModel.StatusCode = HttpStatusCode.OK;
                responseModel.Message = "Grievance created successfully.";
                responseModel.Data = grievanceMaster?.Id ?? 0;
            }
            else
            {
                responseModel.StatusCode = HttpStatusCode.BadRequest;
                responseModel.Message = "Grievance failed to be created.";
            }
            return responseModel;
        }

        private async Task<ResponseModel> StartSecondRound(int grievanceMasterId, string lastResolverCode, string comment)
        {
            ResponseModel responseModel = new ResponseModel
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Bad Request",
            };

            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var grievanceMaster = await _dbContext.GrievanceMasters
                        .FirstOrDefaultAsync(x => x.Id == grievanceMasterId);

                    if (grievanceMaster == null)
                    {
                        return new ResponseModel
                        {
                            StatusCode = HttpStatusCode.NotFound,
                            Message = "Grievance record not found."
                        };
                    }
                    grievanceMaster.Round = (int)Grievance_Utility.GrievanceRound.Second;
                    grievanceMaster.StatusId = (int)Grievance_Utility.GrievanceStatus.InProgress;

                    _dbContext.Update(grievanceMaster);
                    _dbContext.SaveChanges();

                    var lastResolverDetails = await _employeeRepository.GetEmployeeDetailsWithEmpCode(Convert.ToInt32(lastResolverCode));

                    //var resolverUnitId = _dbContext.UserRoleMappings.Where(a => a.UserCode == lastResolverCode && (a.UnitId == grievanceMaster.UnitId || true)).Select(a => a.UnitId).FirstOrDefault();

                    if (lastResolverDetails == null)
                    {
                        return new ResponseModel
                        {
                            StatusCode = HttpStatusCode.NotFound,
                            Message = "Last resolver's unit not found."
                        };
                    }

                    var nodalOfficer = await _dbContext.UserRoleMappings
                        .Where(x => x.UnitId == lastResolverDetails.unitId.ToString() && x.Role.RoleName == Constant.AppRoles.NodalOfficer)
                        .Select(x => new { x.UserCode, x.UserDetails })
                        .FirstOrDefaultAsync();

                    if (nodalOfficer == null)
                    {
                        return new ResponseModel
                        {
                            StatusCode = HttpStatusCode.NotFound,
                            Message = "No Nodal Officer found for this unit."
                        };
                    }

                    GrievanceProcess grievanceProcessObj = new GrievanceProcess()
                    {
                        GrievanceMasterId = grievanceMasterId,
                        Title = grievanceMaster.Title,
                        Description = grievanceMaster.Description,
                        ServiceId = grievanceMaster.ServiceId,
                        Round = (int)Grievance_Utility.GrievanceRound.Second,
                        StatusId = (int)Grievance_Utility.GrievanceStatus.InProgress,
                        RowStatus = Grievance_Utility.RowStatus.Active,
                        AssignedUserCode = nodalOfficer.UserCode,
                        AssignedUserDetails = nodalOfficer.UserDetails,
                        CreatedBy = Convert.ToInt32(grievanceMaster.UserCode),
                        CreatedDate = DateTime.Now
                    };

                    _dbContext.GrievanceProcesses.Add(grievanceProcessObj);
                    await _dbContext.SaveChangesAsync();

                    if (!string.IsNullOrEmpty(comment))
                    {
                        CommentDetail newComment = new CommentDetail()
                        {
                            CommentText = comment,
                            CommentedBy = grievanceMaster.UserCode,
                            CommentDate = DateTime.Now,
                            CommentType = Constant.CommentType.GrievanceProcess,
                            ReferenceId = grievanceProcessObj.Id,

                            CreatedBy = Convert.ToInt32(grievanceMaster.UserCode),
                            CreatedDate = DateTime.Now,
                        };
                        _dbContext.Comments.Add(newComment);
                        _dbContext.SaveChanges();
                    }

                    await transaction.CommitAsync();

                    responseModel.StatusCode = HttpStatusCode.OK;
                    responseModel.Message = "Second round of grievance initiated successfully.";
                    responseModel.Data = grievanceProcessObj.Id;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    responseModel.StatusCode = HttpStatusCode.InternalServerError;
                    responseModel.Message = "An error occurred: " + ex.Message;
                }
            }

            return responseModel;
        }

        private async Task<ResponseModel> StartThirdRound(int grievanceMasterId, string comment)
        {
            ResponseModel responseModel = new ResponseModel
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Bad Request",
            };

            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var grievanceMaster = await _dbContext.GrievanceMasters
                        .FirstOrDefaultAsync(x => x.Id == grievanceMasterId);

                    if (grievanceMaster == null)
                    {
                        return new ResponseModel
                        {
                            StatusCode = HttpStatusCode.NotFound,
                            Message = "Grievance record not found."
                        };
                    }

                    grievanceMaster.Round = (int)Grievance_Utility.GrievanceRound.Third;
                    grievanceMaster.StatusId = (int)Grievance_Utility.GrievanceStatus.InProgress;

                    _dbContext.Update(grievanceMaster);
                    await _dbContext.SaveChangesAsync();

                    var requestorDetails = await _employeeRepository.GetEmployeeDetailsWithEmpCode(Convert.ToInt32(grievanceMaster.CreatedBy));

                    var committeeMember = await _dbContext.UserGroupMappings
                        .Where(x => x.UnitId == requestorDetails.unitId.ToString() && x.Group.IsCommitee == true)
                        .Select(x => new { x.UserCode, x.UserDetails })
                        .FirstOrDefaultAsync();

                    if (committeeMember == null)
                    {
                        var finalCommiteeUnit = _configuration["FinalCommiteeUnit"].ToString();
                        committeeMember = await _dbContext.UserGroupMappings
                            .Where(x => x.UnitId == finalCommiteeUnit && x.Group.IsCommitee == true)
                            .Select(x => new { x.UserCode, x.UserDetails })
                            .FirstOrDefaultAsync();
                    }

                    if (committeeMember == null)
                    {
                        return new ResponseModel
                        {
                            StatusCode = HttpStatusCode.NotFound,
                            Message = "No committee member found for this grievance."
                        };
                    }

                    GrievanceProcess grievanceProcessObj = new GrievanceProcess()
                    {
                        GrievanceMasterId = grievanceMasterId,
                        Title = grievanceMaster.Title,
                        Description = grievanceMaster.Description,
                        ServiceId = grievanceMaster.ServiceId,
                        Round = (int)Grievance_Utility.GrievanceRound.Third,
                        StatusId = (int)Grievance_Utility.GrievanceStatus.InProgress,
                        RowStatus = Grievance_Utility.RowStatus.Active,
                        AssignedUserCode = committeeMember.UserCode,
                        AssignedUserDetails = committeeMember.UserDetails,
                        CreatedBy = Convert.ToInt32(grievanceMaster.UserCode),
                        CreatedDate = DateTime.Now
                    };

                    _dbContext.GrievanceProcesses.Add(grievanceProcessObj);
                    var isForwardedToCommitee = await _dbContext.SaveChangesAsync();
                    if (isForwardedToCommitee > 0 && !string.IsNullOrEmpty(grievanceMaster.UserEmail))
                    {
                        List<string> _emailToId = new List<string>()
                        {
                            grievanceMaster.UserEmail
                        };

                        StringBuilder getEmailTemplate = new StringBuilder();
                        var rootPath = Directory.GetCurrentDirectory();

                        string htmlFilePath = rootPath + @"\wwwroot\EmailTemplate\ForwardedToCommitee.html";
                        using (StreamReader reader = File.OpenText(htmlFilePath))
                        {
                            getEmailTemplate.Append(reader.ReadToEnd());
                        }

                        var requestor = await _employeeRepository.GetEmployeeDetailsWithEmpCode(Convert.ToInt32(grievanceMaster.UserCode));
                        var commiteeMemberDetail = await _employeeRepository.GetEmployeeDetailsWithEmpCode(Convert.ToInt32(committeeMember.UserCode));

                        getEmailTemplate.Replace("{UserName}", requestor.empName);
                        getEmailTemplate.Replace("{GrievanceId}", grievanceMaster.Id.ToString());
                        getEmailTemplate.Replace("{CommitteeContactPerson}", committeeMember.UserDetails);
                        getEmailTemplate.Replace("{CommitteeEmail}", commiteeMemberDetail.empEmail);
                        getEmailTemplate.Replace("{CommitteePhone}", commiteeMemberDetail.empMobileNo);

                        string emailSubject = $"Resolution Update: Grievance ID {grievanceMaster.Id} - Forwarded To Commitee";

                        MailRequestModel mailRequest = new MailRequestModel()
                        {
                            EmailSubject = emailSubject,
                            EmailBody = getEmailTemplate,
                            EmailToId = _emailToId,
                        };
                        await _notificationRepository.SendNotification(mailRequest);
                    }

                    if (!string.IsNullOrEmpty(comment))
                    {
                        CommentDetail newComment = new CommentDetail()
                        {
                            CommentText = comment,
                            CommentedBy = grievanceMaster.UserCode,
                            CommentDate = DateTime.Now,
                            CommentType = Constant.CommentType.GrievanceProcess,
                            ReferenceId = grievanceProcessObj.Id,

                            CreatedBy = Convert.ToInt32(grievanceMaster.UserCode),
                            CreatedDate = DateTime.Now,
                        };
                        _dbContext.Comments.Add(newComment);
                        _dbContext.SaveChanges();
                    }

                    await transaction.CommitAsync();

                    responseModel.StatusCode = HttpStatusCode.OK;
                    responseModel.Message = "Third round of grievance initiated successfully.";
                    responseModel.Data = grievanceProcessObj.Id;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    responseModel.StatusCode = HttpStatusCode.InternalServerError;
                    responseModel.Message = "An error occurred: " + ex.Message;
                }
            }

            return responseModel;
        }

        private async Task<ResponseModel> SendResolutionLink(ResolutionDetail resolutionDetail)
        {
            ResponseModel responseDetails = new ResponseModel()
            {
                StatusCode = System.Net.HttpStatusCode.NotFound,
                Message = "Bad Request"
            };

            if (!string.IsNullOrEmpty(resolutionDetail.UserEmail))
            {
                List<string> _emailToId = new List<string>()
                    {
                        resolutionDetail.UserEmail
                    };

                StringBuilder getEmailTemplate = new StringBuilder();
                var rootPath = Directory.GetCurrentDirectory();

                string htmlFilePath = rootPath + @"\wwwroot\EmailTemplate\ResolutionLink.html";
                using (StreamReader reader = File.OpenText(htmlFilePath))
                {
                    getEmailTemplate.Append(reader.ReadToEnd());
                }

                var requestor = await _employeeRepository.GetEmployeeDetailsWithEmpCode(Convert.ToInt32(resolutionDetail.UserCode));

                getEmailTemplate.Replace("{UserName}", requestor.empName);
                getEmailTemplate.Replace("{GrievanceId}", resolutionDetail.GrievanceMasterId.ToString());
                getEmailTemplate.Replace("{Round}", resolutionDetail.Round.ToString());
                getEmailTemplate.Replace("{AcceptLink}", resolutionDetail.AcceptLink);
                getEmailTemplate.Replace("{RejectLink}", resolutionDetail.RejectLink);

                _dbContext.ResolutionDetails.Add(resolutionDetail);
                _dbContext.SaveChanges();

                string emailSubject = $"Resolution Update: Grievance ID {resolutionDetail.GrievanceMasterId} - Awaiting Your Response";

                MailRequestModel mailRequest = new MailRequestModel()
                {
                    EmailSubject = emailSubject,
                    EmailBody = getEmailTemplate,
                    EmailToId = _emailToId,
                };

                var sendMailDetails = await _notificationRepository.SendNotification(mailRequest);

                if (sendMailDetails != null && sendMailDetails.StatusCode == HttpStatusCode.OK)
                {
                    responseDetails.StatusCode = HttpStatusCode.OK;
                    responseDetails.Message = "Resolution notification has been sent successfully.";
                }
                else
                {
                    responseDetails.StatusCode = HttpStatusCode.BadRequest;
                    responseDetails.Message = "Notification not sent due to an issue in SMTP. Contact the IT Team.";
                }
            }
            else
            {
                responseDetails.StatusCode = HttpStatusCode.BadRequest;
                responseDetails.Message = "User details are missing.";
            }

            return responseDetails;
        }

        public async Task<ResponseModel> VerifyResolutionLink(string resolutionLink, string? comment)
        {
            ResponseModel responseDetails = new ResponseModel()
            {
                StatusCode = System.Net.HttpStatusCode.NotFound,
                Message = "Invalid Resolution Link."
            };

            if (!string.IsNullOrEmpty(resolutionLink))
            {
                var resolutionApproval = _dbContext.ResolutionDetails
                    .Where(r => (r.AcceptLink.Contains(resolutionLink) || r.RejectLink.Contains(resolutionLink)) && r.ResolutionStatus == Constant.ResolutionStatus.Pending).FirstOrDefault();

                if (resolutionApproval != null)
                {
                    var grievance = await _dbContext.GrievanceMasters.Where(x => x.Id == resolutionApproval.GrievanceMasterId)
                        .FirstOrDefaultAsync();

                    if (grievance == null)
                    {
                        responseDetails.Message = "Grievance record not found.";
                        return responseDetails;
                    }

                    DateTime resolutionDeadline = resolutionApproval.CreatedDate?.AddDays(7) ?? DateTime.MinValue;
                    if (DateTime.Now > resolutionDeadline)
                    {
                        resolutionApproval.ResolutionStatus = Constant.ResolutionStatus.Expired;
                        _dbContext.ResolutionDetails.Update(resolutionApproval);
                        _dbContext.SaveChanges();

                        responseDetails.StatusCode = HttpStatusCode.BadRequest;
                        responseDetails.Message = "The resolution response deadline has passed.";
                        return responseDetails;
                    }

                    if (resolutionApproval.AcceptLink.Contains(resolutionLink))
                    {
                        resolutionApproval.ResolutionStatus = Constant.ResolutionStatus.Accepted;
                        responseDetails.Message = "You have accepted the resolution.";
                    }
                    else if (resolutionApproval.RejectLink.Contains(resolutionLink))
                    {
                        resolutionApproval.ResolutionStatus = Constant.ResolutionStatus.Rejected;
                        responseDetails.Message = "You have rejected the resolution.";
                    }

                    resolutionApproval.ModifyDate = DateTime.Now;
                    resolutionApproval.ModifyBy = grievance.CreatedBy;
                    _dbContext.ResolutionDetails.Update(resolutionApproval);
                    var updateCount = await _dbContext.SaveChangesAsync();

                    if (updateCount > 0 && resolutionApproval.ResolutionStatus == Constant.ResolutionStatus.Rejected)
                    {
                        if (grievance.Round == (int)Grievance_Utility.GrievanceRound.First)
                            await StartSecondRound(grievance.Id, resolutionApproval.ResolverCode, comment ?? string.Empty);
                        else if (grievance.Round == (int)Grievance_Utility.GrievanceRound.Second)
                            await StartThirdRound(grievance.Id, comment ?? string.Empty);
                    }
                    else if (updateCount > 0 && resolutionApproval.ResolutionStatus == Constant.ResolutionStatus.Accepted)
                    {
                        grievance.StatusId = (int)Grievance_Utility.GrievanceStatus.Closed;
                        _dbContext.GrievanceMasters.Update(grievance);

                        var lastProcess = _dbContext.GrievanceProcesses.Where(a => a.GrievanceMasterId == grievance.Id).OrderByDescending(a => a.Id).FirstOrDefault();
                        if (lastProcess != null)
                        {
                            lastProcess.Id = 0;
                            lastProcess.AssignedUserCode = string.Empty;
                            lastProcess.AssignedUserCode = string.Empty;
                            lastProcess.StatusId = (int)Grievance_Utility.GrievanceStatus.Closed;

                            _dbContext.Add(lastProcess);
                        }
                        _dbContext.SaveChanges();
                    }
                    responseDetails.StatusCode = HttpStatusCode.OK;
                }
            }

            return responseDetails;
        }

        public async Task<ResponseModel> GrievanceDetailsAsync(int grievanceId, string baseUrl)
        {
            ResponseModel responseModel = new ResponseModel
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Bad Request",
            };
            if (grievanceId != 0)
            {
                var grievanceDetail = await _dbContext.GrievanceMasters.Where(grievance => grievance.Id == grievanceId).FirstOrDefaultAsync();
                if (grievanceDetail != null)
                {
                    var grievanceProcessFirstId = _dbContext.GrievanceProcesses.Where(a => a.GrievanceMasterId == grievanceId).Select(a => a.Id).FirstOrDefault();

                    var grievanceAttachments = _dbContext.Attachments.Where(x => x.ReferenceId == grievanceProcessFirstId && x.Type == Constant.CommentType.GrievanceProcess).Select(attach => baseUrl + attach.FilePath.Replace("wwwroot/", "").Replace("\\", "/")).ToList();

                    var processDetails = await _dbContext.GrievanceProcesses.Where(x => x.GrievanceMasterId == grievanceDetail.Id)
                        .OrderByDescending(gp => gp.Id)
                        .Select(gp => new
                        {
                            GrievanceMasterId = gp.GrievanceMasterId,
                            GrievanceProcessId = gp.Id,
                            Title = grievanceDetail.Title,
                            Description = grievanceDetail.Description,
                            Attachments = grievanceAttachments,
                            UserCode = grievanceDetail.UserCode,
                            UserDetails = grievanceDetail.UserDetails,
                            ServiceId = gp.ServiceId,
                            Round = gp.Round,
                            AssignedUserCode = gp.AssignedUserCode,
                            AssignedUserDetails = gp.AssignedUserDetails,
                            StatusId = gp.StatusId,
                            CreatedBy = grievanceDetail.UserCode,
                            CreatedDate = grievanceDetail.CreatedDate,
                            ModifiedBy = gp.CreatedBy,
                            ModifiedDate = gp.CreatedDate
                        })
                        .FirstOrDefaultAsync();

                    if (processDetails != null)
                    {
                        responseModel.StatusCode = HttpStatusCode.OK;
                        responseModel.Message = "Grievance Details";
                        responseModel.Data = processDetails;
                        responseModel.DataLength = 1;
                    }
                    else
                    {
                        responseModel.StatusCode = HttpStatusCode.NotFound;
                        responseModel.Message = "No grievance found";
                    }
                }
                else
                {
                    responseModel.StatusCode = HttpStatusCode.BadRequest;
                    responseModel.Message = "Invalid grievance-Id";
                }
            }
            return responseModel;
        }

        public async Task<ResponseModel> GrievanceHistoryAsync(int grievanceId, string baseUrl)
        {
            ResponseModel responseModel = new ResponseModel
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Bad Request",
            };

            var allProcesses = _dbContext.GrievanceProcesses.Where(x => x.GrievanceMasterId == grievanceId).OrderByDescending(x => x.Id).ToList();
            if (allProcesses != null && allProcesses.Count == 1)
            {
                var getCommentDetails = _dbContext.Comments.Where(a => a.CommentType == Constant.CommentType.GrievanceProcess);
                var getAttachment = _dbContext.Attachments.Where(a => a.Type == Constant.CommentType.GrievanceProcess);
                var empList = await _employeeRepository.GetOrganizationHierarchy();

                List<GrievanceProcessChanges> dtoAllChanges = new List<GrievanceProcessChanges>();
                // List to store all changes
                //List<GrievanceChange> allChanges = new List<GrievanceChange>();
                List<CommentDetailsModel> commentList = new List<CommentDetailsModel>();
                GrievanceProcessChanges processChanges = new GrievanceProcessChanges();

                var allComment = getCommentDetails.Where(x => x.ReferenceId == allProcesses[0].Id).OrderByDescending(x => x.CreatedDate).ToList();
                if (allComment != null && allComment.Count > 0)
                {
                    foreach (var item in allComment)
                    {
                        CommentDetailsModel comment = new CommentDetailsModel();
                        comment.Comment = item.CommentText;
                        comment.CommentedDate = item.CreatedDate;
                        comment.CommentedById = item.CreatedBy;
                        comment.CommentType = item.CommentType;
                        comment.CommentedByName = empList.Find(a => a.empCode == item.CreatedBy.ToString())?.empName ?? string.Empty;

                        var findAttach = getAttachment.Where(x => x.ReferenceId == allProcesses[0].Id).Select(attach => baseUrl + attach.FilePath.Replace("wwwroot/", "").Replace("\\", "/")).ToList();
                        if (findAttach != null && findAttach.Count() > 0)
                        {
                            comment.Attachment = findAttach;
                        }
                        commentList.Add(comment);
                    }
                }
                //processChanges.ChangeList = allChanges;
                processChanges.CommentDetails = commentList;
                dtoAllChanges.Add(processChanges);

                responseModel.Data = dtoAllChanges;
                responseModel.DataLength = dtoAllChanges.Count;
                responseModel.StatusCode = HttpStatusCode.OK;
                responseModel.Message = "List of all changes";
            }

            else if (allProcesses != null && allProcesses.Count > 1)
            {
                var getCommentDetails = _dbContext.Comments.Where(a => a.CommentType == Constant.CommentType.GrievanceProcess);
                var getAttachment = _dbContext.Attachments.Where(a => a.Type == Constant.CommentType.GrievanceProcess);
                var empList = await _employeeRepository.GetOrganizationHierarchy();

                List<GrievanceProcessChanges> dtoAllChanges = new List<GrievanceProcessChanges>();
                // Iterate over the list of GrievanceProcess
                for (int i = 0; i < allProcesses.Count; i++)
                {
                    // List to store all changes
                    //List<GrievanceChange> allChanges = new List<GrievanceChange>();
                    List<CommentDetailsModel> commentList = new List<CommentDetailsModel>();
                    GrievanceProcessChanges processChanges = new GrievanceProcessChanges();

                    var allComment = getCommentDetails.Where(x => x.ReferenceId == allProcesses[i].Id).OrderByDescending(x => x.CreatedDate).ToList();
                    if (allComment != null && allComment.Count > 0)
                    {
                        foreach (var item in allComment)
                        {
                            CommentDetailsModel comment = new CommentDetailsModel();
                            comment.Comment = item.CommentText;
                            comment.CommentedDate = item.CreatedDate;
                            comment.CommentedById = item.CreatedBy;
                            comment.CommentType = item.CommentType;
                            comment.CommentedByName = empList.Find(a => a.empCode == item.CreatedBy.ToString())?.empName ?? string.Empty;

                            var findAttach = getAttachment.Where(x => x.ReferenceId == allProcesses[i].Id).Select(attach => baseUrl + attach.FilePath.Replace("wwwroot/", "").Replace("\\", "/")).ToList();
                            if (findAttach != null && findAttach.Count() > 0)
                            {
                                comment.Attachment = findAttach;
                            }
                            commentList.Add(comment);
                        }
                    }

                    //var processCount = $"Process {i}";
                    //if (i > 0)
                    //{
                    //    var currentRow = allProcesses[i];
                    //    var previousRow = allProcesses[i - 1];
                    //    var changes = CompareRows(previousRow, currentRow);
                    //    allChanges.AddRange(changes.Select(change => new GrievanceChange
                    //    {
                    //        Column = change.Column,
                    //        OldValue = change.OldValue,
                    //        NewValue = change.NewValue,
                    //        ProcessCount = processCount
                    //    }));

                    //    processChanges.ChangeBy = empList.Find(a => a.empCode == allProcesses[i - 1].CreatedBy.ToString())?.empName ?? string.Empty;
                    //    processChanges.CaseName = processCount;
                    //    processChanges.ModifyDate = allProcesses[i - 1].CreatedDate;
                    //}

                    processChanges.GrievanceProcessId = allProcesses[i].Id;
                    processChanges.CommentDetails = commentList;
                    //processChanges.ChangeList = allChanges;

                    if (processChanges.CommentDetails.Count == 0)
                    {
                        continue;
                    }
                    dtoAllChanges.Add(processChanges);
                }
                responseModel.Data = dtoAllChanges;
                responseModel.DataLength = dtoAllChanges.Count;
                responseModel.StatusCode = HttpStatusCode.OK;
                responseModel.Message = "List of all changes";
            }
            else
            {
                responseModel.StatusCode = HttpStatusCode.NotFound;
                responseModel.Message = "No grievance process found";
            }
            return responseModel;
        }

        // Function to compare two rows and return the changes
        //private List<GrievanceChange> CompareRows(GrievanceProcess previousRow, GrievanceProcess currentRow)
        //{
        //    List<GrievanceChange> changes = new List<GrievanceChange>();

        //    var properties = typeof(GrievanceProcess).GetProperties();
        //    foreach (var property in properties)
        //    {
        //        var columnName = property.Name;
        //        var oldValue = property.GetValue(previousRow);
        //        var newValue = property.GetValue(currentRow);

        //        if (!object.Equals(oldValue, newValue))
        //        {
        //            changes.Add(new GrievanceChange
        //            {
        //                Column = columnName,
        //                OldValue = newValue,
        //                NewValue = oldValue
        //            });
        //        }
        //    }
        //    return changes;
        //}

        //public async Task<ResponseModel> GetDashboardDataAsync(string userCode, string? unitId, string? department, string? year)
        //{
        //    ResponseModel responseModel = new ResponseModel
        //    {
        //        StatusCode = HttpStatusCode.BadRequest,
        //        Message = "Bad Request"
        //    };

        //    if (string.IsNullOrEmpty(userCode))
        //    {
        //        return new ResponseModel
        //        {
        //            StatusCode = HttpStatusCode.NotFound,
        //            Message = "User not found."
        //        };
        //    }

        //    DashboardModel finalData = new();
        //    var userAllRole = (List<string>)(await GetUserRolesAsync(userCode)).Data ?? new List<string>();

        //    IQueryable<GrievanceMaster> query = _dbContext.GrievanceMasters.AsQueryable();

        //    if (!string.IsNullOrEmpty(unitId))
        //        query = query.Where(a => a.UnitId == unitId);
        //    if (!string.IsNullOrEmpty(department))
        //        query = query.Where(a => a.Department == department);
        //    if (!string.IsNullOrEmpty(year))
        //    {
        //        if (int.TryParse(year, out int yearInt))
        //        {
        //            query = query.Where(a => a.CreatedDate.Value.Year == yearInt);
        //        }
        //    }

        //    var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        //    var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        //    if (userAllRole.Contains(Constant.AppRoles.SuperAdmin))
        //    {
        //        finalData.TotalGrievance = query.Count();
        //        finalData.Pending = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.Created).Count();
        //        finalData.InProgress = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.InProgress).Count();
        //        finalData.Resolved = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.Resolved).Count();

        //        var unresolvedQuery = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.Created || a.StatusId == (int)Grievance_Utility.GrievanceStatus.InProgress);

        //        finalData.Unresolved = unresolvedQuery.Count();
        //        finalData.Over7Days = unresolvedQuery.Where(a => a.CreatedDate > DateTime.Now.AddDays(-7)).Count();
        //        finalData.Over14Days = unresolvedQuery.Where(a => a.CreatedDate > DateTime.Now.AddDays(-14)).Count();
        //        finalData.ThisMonth = unresolvedQuery.Where(a => a.CreatedDate >= startOfMonth && a.CreatedDate <= endOfMonth).Count();
        //        finalData.Over30Days = unresolvedQuery.Where(a => a.CreatedDate < startOfMonth).Count();
        //    }
        //    else if (userAllRole.Contains(Constant.AppRoles.NodalOfficer) || userAllRole.Contains(Constant.AppRoles.UnitCGM) || userAllRole.Contains(Constant.AppRoles.Admin))
        //    {
        //        finalData.TotalGrievance = query.Count();
        //        finalData.Pending = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.Created).Count();
        //        finalData.InProgress = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.InProgress).Count();
        //        finalData.Resolved = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.Resolved).Count();

        //        var unresolvedQuery = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.Created || a.StatusId == (int)Grievance_Utility.GrievanceStatus.InProgress);

        //        finalData.Unresolved = unresolvedQuery.Count();
        //        finalData.Over7Days = unresolvedQuery.Where(a => a.CreatedDate > DateTime.Now.AddDays(-7)).Count();
        //        finalData.Over14Days = unresolvedQuery.Where(a => a.CreatedDate > DateTime.Now.AddDays(-14)).Count();
        //        finalData.ThisMonth = unresolvedQuery.Where(a => a.CreatedDate >= startOfMonth && a.CreatedDate <= endOfMonth).Count();
        //        finalData.Over30Days = unresolvedQuery.Where(a => a.CreatedDate < startOfMonth).Count();
        //    }
        //    else if (userAllRole.Contains(Constant.AppRoles.Addressal))
        //    {
        //        var userGroups = _dbContext.UserGroupMappings
        //            .Where(x => x.UserCode == userCode)
        //            .Select(x => new { x.GroupId, x.UnitId });

        //        var userDepartments = _dbContext.UserDepartmentMappings
        //            .Where(x => userGroups.Select(a => a.UnitId).Contains(x.UnitId) && x.UserCode == userCode)
        //            .Select(x => new { x.Department, x.UnitId, x.UserCode });

        //        var resolvedGrievanceIds = _dbContext.GrievanceProcesses
        //            .Where(gp => gp.CreatedBy == Convert.ToInt32(userCode) && gp.StatusId == (int)Grievance_Utility.GrievanceStatus.Resolved)
        //            .Select(gp => gp.GrievanceMasterId);

        //        var masterGrievance = await (from gm in _dbContext.GrievanceMasters
        //                                     join gp in _dbContext.GrievanceProcesses on gm.Id equals gp.GrievanceMasterId
        //                                     where (userDepartments.Select(a => a.Department).Contains(gm.Department)
        //                                     && userDepartments.Select(a => a.UnitId).Contains(gm.UnitId)
        //                                     && (gp.AssignedUserCode == userCode && gp.StatusId != (int)Grievance_Utility.GrievanceStatus.Resolved)
        //                                     || gp.StatusId == (int)Grievance_Utility.GrievanceStatus.Created)
        //                                     || resolvedGrievanceIds.Contains(gm.Id)
        //                                     select gm.Id).ToListAsync();
        //        query = query.Where(g => masterGrievance.Contains(g.Id));

        //        finalData.TotalGrievance = query.Count();
        //        finalData.Pending = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.Created).Count();
        //        finalData.InProgress = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.InProgress).Count();
        //        finalData.Resolved = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.Resolved).Count();

        //        var unresolvedQuery = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.Created || a.StatusId == (int)Grievance_Utility.GrievanceStatus.InProgress);

        //        finalData.Unresolved = unresolvedQuery.Count();
        //        finalData.Over7Days = unresolvedQuery.Where(a => a.CreatedDate > DateTime.Now.AddDays(-7)).Count();
        //        finalData.Over14Days = unresolvedQuery.Where(a => a.CreatedDate > DateTime.Now.AddDays(-14)).Count();
        //        finalData.ThisMonth = unresolvedQuery.Where(a => a.CreatedDate >= startOfMonth && a.CreatedDate <= endOfMonth).Count();
        //        finalData.Over30Days = unresolvedQuery.Where(a => a.CreatedDate < startOfMonth).Count();
        //    }

        //    responseModel.StatusCode = HttpStatusCode.OK;
        //    responseModel.Message = "Dashboard data retrieved successfully.";
        //    responseModel.Data = finalData;

        //    return responseModel;
        //}

        public async Task<ResponseModel> GetDashboardDataAsync(string userCode, string? unitId, string? department, string? year)
        {
            ResponseModel responseModel = new ResponseModel
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };

            if (string.IsNullOrEmpty(userCode))
            {
                return new ResponseModel
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Message = "User not found."
                };
            }

            DashboardModel finalData = new();
            var userAllRole = (List<string>)(await _userRepository.GetUserRolesAsync(userCode)).Data ?? new List<string>();

            IQueryable<GrievanceMaster> query = _dbContext.GrievanceMasters.AsQueryable();

            if (!string.IsNullOrEmpty(unitId))
                query = query.Where(a => a.UnitId == unitId);
            if (!string.IsNullOrEmpty(department))
                query = query.Where(a => a.Department == department);

            int selectedYear = DateTime.Now.Year;
            if (!string.IsNullOrEmpty(year) && int.TryParse(year, out int yearInt))
            {
                selectedYear = yearInt;
                query = query.Where(a => a.CreatedDate.Value.Year == yearInt);
            }

            if (userAllRole.Contains(Constant.AppRoles.SuperAdmin))
            {
                // to ignore the conditions because of priority role for a user having multiple roles
            }
            else if (userAllRole.Contains(Constant.AppRoles.NodalOfficer) || userAllRole.Contains(Constant.AppRoles.UnitCGM) || userAllRole.Contains(Constant.AppRoles.Admin) || userAllRole.Contains(Constant.AppRoles.Committee))
            {
                if (string.IsNullOrEmpty(unitId))
                {
                    var assignedUnit = _dbContext.UserRoleMappings.Where(a => a.UserCode == userCode && (a.Role.RoleName == Constant.AppRoles.NodalOfficer || a.Role.RoleName == Constant.AppRoles.UnitCGM || a.Role.RoleName == Constant.AppRoles.Admin)).Select(a => a.UnitId).FirstOrDefault();
                    query = query.Where(a => a.UnitId == assignedUnit);
                }
            }
            else if (userAllRole.Contains(Constant.AppRoles.Addressal))
            {
                var userGroups = _dbContext.UserGroupMappings
                    .Where(x => x.UserCode == userCode)
                    .Select(x => new { x.GroupId, x.UnitId });

                var userDepartments = _dbContext.UserDepartmentMappings
                    .Where(x => userGroups.Select(a => a.UnitId).Contains(x.UnitId) && x.UserCode == userCode)
                    .Select(x => new { x.Department, x.UnitId, x.UserCode });

                var resolvedGrievanceIds = _dbContext.GrievanceProcesses
                    .Where(gp => gp.CreatedBy == Convert.ToInt32(userCode) && (gp.StatusId == (int)Grievance_Utility.GrievanceStatus.Resolved || gp.StatusId == (int)Grievance_Utility.GrievanceStatus.Closed))
                    .Select(gp => gp.GrievanceMasterId);

                var masterGrievance = await (from gm in _dbContext.GrievanceMasters
                                             join gp in _dbContext.GrievanceProcesses on gm.Id equals gp.GrievanceMasterId
                                             where (userDepartments.Select(a => a.Department).Contains(gm.Department)
                                             && userDepartments.Select(a => a.UnitId).Contains(gm.UnitId)
                                             && (gp.AssignedUserCode == userCode && gp.StatusId != (int)Grievance_Utility.GrievanceStatus.Resolved)
                                             || gp.StatusId == (int)Grievance_Utility.GrievanceStatus.Created)
                                             || resolvedGrievanceIds.Contains(gm.Id)
                                             select gm.Id).ToListAsync();

                query = query.Where(g => masterGrievance.Contains(g.Id));
            }

            finalData.TotalGrievance = query.Count();
            finalData.Pending = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.Created).Count();
            finalData.InProgress = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.InProgress || a.StatusId == (int)Grievance_Utility.GrievanceStatus.AwatingInfo).Count();
            finalData.Resolved = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.Resolved || a.StatusId == (int)Grievance_Utility.GrievanceStatus.Closed).Count();

            var unresolvedQuery = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.Created || a.StatusId == (int)Grievance_Utility.GrievanceStatus.InProgress || a.StatusId == (int)Grievance_Utility.GrievanceStatus.AwatingInfo);
            finalData.Unresolved = unresolvedQuery.Count();
            finalData.Over7Days = unresolvedQuery.Where(a => a.CreatedDate <= DateTime.Now.AddDays(-7)).Count();
            finalData.Over14Days = unresolvedQuery.Where(a => a.CreatedDate <= DateTime.Now.AddDays(-14)).Count();

            var startOfCurrentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            finalData.ThisMonth = unresolvedQuery.Where(a => a.CreatedDate >= startOfCurrentMonth).Count();
            finalData.Over30Days = unresolvedQuery.Where(a => a.CreatedDate <= DateTime.Now.AddDays(-30)).Count();

            var monthlyData = query
                .GroupBy(a => a.CreatedDate.Value.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToDictionary(g => g.Month, g => g.Count);

            for (int month = 1; month <= 12; month++)
            {
                finalData.MonthlyGrievances.Add(new MonthGrievance
                {
                    MonthName = new DateTime(selectedYear, month, 1).ToString("MMMM"), // January
                    MonthInt = month,
                    TotalCount = monthlyData.ContainsKey(month) ? monthlyData[month] : 0
                });
            }

            responseModel.StatusCode = HttpStatusCode.OK;
            responseModel.Message = "Dashboard data retrieved successfully.";
            responseModel.Data = finalData;

            return responseModel;
        }

        public async Task<ResponseModel> GetMyDashboardDataAsync(string userCode)
        {
            ResponseModel responseModel = new ResponseModel
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };

            if (string.IsNullOrEmpty(userCode))
            {
                return new ResponseModel
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Message = "User not found."
                };
            }

            MyDashboardModel finalData = new();
            var userAllRole = (List<string>)(await _userRepository.GetUserRolesAsync(userCode)).Data ?? new List<string>();

            int selectedYear = DateTime.Now.Year;
            IQueryable<GrievanceMaster> query = _dbContext.GrievanceMasters.Where(a => a.CreatedBy == Convert.ToInt32(userCode) && a.CreatedDate.Value.Year == selectedYear).AsQueryable();

            finalData.TotalGrievance = query.Count();
            finalData.InProgress = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.Created || a.StatusId == (int)Grievance_Utility.GrievanceStatus.InProgress || a.StatusId == (int)Grievance_Utility.GrievanceStatus.AwatingInfo).Count();
            finalData.Resolved = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.Resolved || a.StatusId == (int)Grievance_Utility.GrievanceStatus.Closed).Count();

            var monthlyData = query
                .GroupBy(a => a.CreatedDate.Value.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToDictionary(g => g.Month, g => g.Count);

            for (int month = 1; month <= 12; month++)
            {
                finalData.MonthlyGrievances.Add(new MonthGrievance
                {
                    MonthName = new DateTime(selectedYear, month, 1).ToString("MMMM"), // January
                    MonthInt = month,
                    TotalCount = monthlyData.ContainsKey(month) ? monthlyData[month] : 0
                });
            }

            finalData.RecentGrievances = _dbContext.GrievanceMasters.Where(a => a.CreatedBy == Convert.ToInt32(userCode)).OrderByDescending(a => a.Id).Take(3).ToList();

            responseModel.StatusCode = HttpStatusCode.OK;
            responseModel.Message = "Dashboard data retrieved successfully.";
            responseModel.Data = finalData;

            return responseModel;
        }


    }
}
