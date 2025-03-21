using System.Collections.Immutable;
using System.Net;
using System.Text;
using Grievance_BAL.IServices;
using Grievance_DAL.DatabaseContext;
using Grievance_DAL.DbModels;
using Grievance_Model.DTOs.AppResponse;
using Grievance_Model.DTOs.Dashboard;
using Grievance_Model.DTOs.Grievance;
using Grievance_Model.DTOs.Notification;
using Grievance_Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

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
            if (appRoles.Count == 1 && appRoles.Contains(Constant.AppRoles.User))
            {
                return new ResponseModel
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Message = "User is neither Addressal nor NodalOfficer/CGM."
                };
            }

            bool isSuperAdmin = appRoles.Any(r => r == Constant.AppRoles.SuperAdmin || r == Constant.AppRoles.ManagingDirector);
            bool isAddressal = appRoles.Any(r => r == Constant.AppRoles.Redressal || r == Constant.AppRoles.Committee || r == Constant.AppRoles.HOD);
            bool isNodalOrCGM = appRoles.Any(r => r == Constant.AppRoles.NodalOfficer || r == Constant.AppRoles.UnitCGM || r == Constant.AppRoles.Admin);

            IQueryable<GrievanceMaster> query = _dbContext.GrievanceMasters.AsQueryable();

            if (isSuperAdmin)
            {
                // to ignore the below conditions
            }
            else if (isNodalOrCGM)
            {
                var nodalOrCGMRole = new List<int> { (int)AppRoles.Admin, (int)AppRoles.UnitCGM, (int)AppRoles.NodalOfficer };
                var userRolesOfUnit = (from g in _dbContext.Groups
                                       join gm in _dbContext.UserGroupMappings on g.Id equals gm.GroupId
                                       where gm.UserCode == userCode && nodalOrCGMRole.Contains(g.RoleId.Value)
                                       select gm.UnitId
                                        ).ToList();
                query = query.Where(g => userRolesOfUnit.Contains(g.UnitId));
            }
            else if (isAddressal)
            {
                var userGroups = _dbContext.UserGroupMappings
                    .Where(x => x.UserCode == userCode)
                    .Select(x => new { x.GroupId, x.UnitId });

                var grievanceQuery = _dbContext.GrievanceMasters.AsQueryable();
                var userDepartments = _dbContext.UserDepartmentMappings
                        .Where(x => x.UserCode == userCode)
                        .Select(x => new { x.Department, x.UnitId });

                bool isUnit396 = userGroups.Any(x => x.UnitId == "396");
                var grievanceMasterIds = new List<int>();
                if (isUnit396)
                {

                    if (appRoles.Any(r => r == Constant.AppRoles.Committee))
                    {
                        grievanceMasterIds.AddRange(from gm in grievanceQuery
                                                    join gp in _dbContext.GrievanceProcesses on gm.Id equals gp.GrievanceMasterId
                                                    join gpMst in _dbContext.Groups on gp.TGroupId equals gpMst.Id
                                                    where (gp.AssignedUserCode == userCode || (gp.TGroupId == gpMst.Id
                                                              && gpMst.RoleId == (int)AppRoles.Committee))
                                                           && gm.Round == (int)Grievance_Utility.GrievanceRound.Third
                                                    select gm.Id);
                    }

                    if (appRoles.Any(r => r == Constant.AppRoles.HOD))
                    {
                        grievanceMasterIds.AddRange(from gm in grievanceQuery
                                                    join gp in _dbContext.GrievanceProcesses on gm.Id equals gp.GrievanceMasterId
                                                    join gpMst in _dbContext.Groups on gp.TGroupId equals gpMst.Id
                                                    where (gp.AssignedUserCode == userCode || userGroups.Where(a => a.GroupId == gp.TGroupId && a.UnitId == gp.TUnitId).Any())
                                                    select gm.Id);
                    }

                    if (appRoles.Any(r => r == Constant.AppRoles.Redressal))
                    {
                        grievanceMasterIds.AddRange(from gm in grievanceQuery
                                                    join gp in _dbContext.GrievanceProcesses on gm.Id equals gp.GrievanceMasterId
                                                    join gpMst in _dbContext.Groups on gp.TGroupId equals gpMst.Id
                                                    where gp.AssignedUserCode == userCode
                                                    || (userGroups.Where(a => a.GroupId == gp.TGroupId && a.UnitId == gp.TUnitId).Any()
                                                           && userDepartments.Where(a => a.Department == gp.TDepartment && a.UnitId == gp.TUnitId).Any())
                                                    select gm.Id);

                    }
                }
                else
                {
                    grievanceMasterIds.AddRange(from gm in grievanceQuery
                                                join gp in _dbContext.GrievanceProcesses on gm.Id equals gp.GrievanceMasterId
                                                join gpMst in _dbContext.Groups on gp.TGroupId equals gpMst.Id
                                                where gp.AssignedUserCode == userCode
                                                   || (userGroups.Where(a => a.GroupId == gp.TGroupId && a.UnitId == gp.TUnitId).Any()
                                                          && userDepartments.Where(a => a.Department == gp.TDepartment && a.UnitId == gp.TUnitId).Any())
                                                select gm.Id);
                }
                grievanceMasterIds = grievanceMasterIds.Distinct().ToList();
                query = query.Where(g => grievanceMasterIds.Contains(g.Id) && g.CreatedBy != Convert.ToInt32(userCode));
            }

            int totalRecords = await query.CountAsync();

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
                                        //ServiceId = gm.ServiceId,
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
                                        TUnit = gp.TUnitId,
                                        TGroupId = gp.TGroupId,
                                        TDepartment = gp.TDepartment,
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
                                        //ServiceId = gm.ServiceId,
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

            //var existingGrievance = await _dbContext.GrievanceMasters.Where(a => a.UserCode == grievanceModel.UserCode && a.GroupId == grievanceModel.TGroupId).OrderByDescending(a => a.Id).FirstOrDefaultAsync();
            //if (existingGrievance != null && existingGrievance.StatusId != (int)Grievance_Utility.GrievanceStatus.Closed && grievanceModel.GrievanceMasterId == 0)
            //{
            //    responseModel.StatusCode = HttpStatusCode.AlreadyReported;
            //    responseModel.Message = "Grievance already in process.";
            //    return responseModel;
            //}

            IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync();
            var grievanceMaster = _dbContext.GrievanceMasters.Where(x => x.Id == grievanceModel.GrievanceMasterId).FirstOrDefault();
            var userDetails = await _employeeRepository.GetEmployeeDetailsWithEmpCode(Convert.ToInt32(grievanceModel.UserCode));
            if (grievanceMaster == null)
            {
                grievanceMaster = new GrievanceMaster()
                {
                    Title = grievanceModel.Title,
                    Description = grievanceModel.Description,
                    IsInternal = grievanceModel.IsInternal,
                    UserCode = grievanceModel.IsInternal ? grievanceModel.UserCode : string.Empty,
                    UserEmail = grievanceModel.IsInternal ? userDetails.empEmail : grievanceModel.UserEmail,
                    UserDetails = grievanceModel.IsInternal ? (!string.IsNullOrEmpty(userDetails.empName) ? userDetails.empName : "NA") + (!string.IsNullOrEmpty(userDetails.empCode) ? " (" + userDetails.empCode + ")" : "") + (!string.IsNullOrEmpty(userDetails.designation) ? " - " + userDetails.designation : "") + (!string.IsNullOrEmpty(userDetails.department) ? " | " + userDetails.department : "") : string.Empty,
                    UnitId = userDetails.unitId.ToString(),
                    UnitName = userDetails.units,
                    Department = grievanceModel.TDepartment,
                    GroupId = grievanceModel.TGroupId.Value,
                    Round = (int)Grievance_Utility.GrievanceRound.First,
                    StatusId = (int)Grievance_Utility.GrievanceStatus.Open,
                    RowStatus = Grievance_Utility.RowStatus.Active,

                    CreatedBy = Convert.ToInt32(userDetails?.empCode),
                    CreatedDate = DateTime.Now
                };
                _dbContext.GrievanceMasters.Add(grievanceMaster);
                _dbContext.SaveChanges();
            }
            else
            {
                //grievanceMaster.ServiceId = grievanceModel.ServiceId;
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
                    if (FinalCommiteeUnit == userDetails.unitId.ToString())
                    {
                        var addressal = (from ug in _dbContext.UserGroupMappings
                                         join ud in _dbContext.UserDepartmentMappings on ug.UserCode equals ud.UserCode
                                         where (ug.UnitId == FinalCommiteeUnit
                                         && ud.Department == userDetails.department && ug.GroupId == grievanceModel.TGroupId)
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
                        var addressal = (from ug in _dbContext.UserGroupMappings
                                         where ug.GroupId == grievanceModel.TGroupId && ug.UnitId == grievanceModel.TUnitId
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

                    if (string.IsNullOrEmpty(addressalCode) && string.IsNullOrEmpty(addressalDetail))
                    {
                        var addressal = (from ug in _dbContext.UserGroupMappings
                                         join gmst in _dbContext.Groups on ug.GroupId equals gmst.Id
                                         join ar in _dbContext.AppRoles on gmst.RoleId equals ar.Id
                                         where ar.RoleName == Constant.AppRoles.NodalOfficer && ug.UnitId == grievanceModel.TUnitId
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
                }
                else
                {
                    addressalCode = grievanceModel.AssignedUserCode;
                    addressalDetail = grievanceModel.AssignedUserDetails;
                }

                GrievanceProcess grievanceProcessObj = new GrievanceProcess()
                {
                    GrievanceMasterId = grievanceMaster.Id,

                    Title = grievanceModel.Title,
                    Description = grievanceModel.Description,
                    Round = grievanceModel.Round ?? grievanceMaster.Round,
                    StatusId = grievanceMaster == null ? (int)Grievance_Utility.GrievanceStatus.Open : grievanceModel.StatusId ?? grievanceMaster.StatusId,
                    RowStatus = Grievance_Utility.RowStatus.Active,

                    AssignedUserCode = string.IsNullOrEmpty(addressalCode) ? string.Empty : addressalCode,
                    AssignedUserDetails = string.IsNullOrEmpty(addressalDetail) ? string.Empty : addressalDetail,

                    CreatedBy = Convert.ToInt32(grievanceModel.UserCode),
                    CreatedDate = DateTime.Now,

                    TUnitId = grievanceModel.TUnitId,
                    TGroupId = grievanceModel.TGroupId,
                    TDepartment = grievanceModel.TDepartment
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
                if (grievanceProcessObj.StatusId == (int)Grievance_Utility.GrievanceStatus.Closed && grievanceMaster?.CreatedBy != Convert.ToInt32(grievanceModel.UserCode))
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
                            AcceptLink = Guid.NewGuid().ToString() + "$",
                            RejectLink = Guid.NewGuid().ToString(),
                            ResolutionStatus = Constant.ResolutionStatus.Pending,

                            CreatedDate = DateTime.Now,
                            CreatedBy = Convert.ToInt32(grievanceModel.AssignedUserCode)
                        };
                        await SendResolutionLink(resolutionDetail, grievanceModel.BaseUrl ?? string.Empty);
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

        private async Task<ResponseModel> SendResolutionLink(ResolutionDetail resolutionDetail, string baseUrl)
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
                getEmailTemplate.Replace("{AcceptLink}", baseUrl + "/" + resolutionDetail.AcceptLink);
                getEmailTemplate.Replace("{RejectLink}", baseUrl + "/" + resolutionDetail.RejectLink);

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
                    .Where(r => (r.AcceptLink == resolutionLink || r.RejectLink == resolutionLink) && r.ResolutionStatus == Constant.ResolutionStatus.Pending).FirstOrDefault();

                if (resolutionApproval != null)
                {
                    var transaction = _dbContext.Database.BeginTransaction();
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
                    transaction.Commit();
                    responseDetails.StatusCode = HttpStatusCode.OK;
                }
            }

            return responseDetails;
        }

        private async Task<ResponseModel> StartSecondRound(int grievanceMasterId, string lastResolverCode, string comment)
        {
            ResponseModel responseModel = new ResponseModel
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Bad Request",
            };

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

                var lastResolverUnit = _dbContext.GrievanceProcesses.Where(a => a.GrievanceMasterId == grievanceMaster.Id && a.StatusId == (int)Grievance_Utility.GrievanceStatus.Closed && a.Round == (int)Grievance_Utility.GrievanceRound.First).OrderByDescending(a => a.Id).Select(a => a.TUnitId).FirstOrDefault();

                var nodalOfficer = (from r in _dbContext.AppRoles
                                    join g in _dbContext.Groups on r.Id equals g.RoleId
                                    join gm in _dbContext.UserGroupMappings on g.Id equals gm.GroupId
                                    where r.RoleName == Constant.AppRoles.NodalOfficer && gm.UnitId ==
                                    lastResolverUnit
                                    select new { gm.UserCode, gm.UserDetails }).FirstOrDefault();

                if (nodalOfficer == null)
                {
                    return new ResponseModel
                    {
                        StatusCode = HttpStatusCode.NotFound,
                        Message = "No Nodal Officer found for this unit."
                    };
                }
                var tGroupId = 0;
                if (lastResolverUnit == _configuration["FinalCommiteeUnit"].ToString())
                    tGroupId = _dbContext.Groups.Where(a => a.UnitId == lastResolverUnit).Select(a => a.Id).FirstOrDefault();
                else
                    tGroupId = _dbContext.Groups.Where(a => a.UnitId != lastResolverUnit).Select(a => a.Id).FirstOrDefault();

                GrievanceProcess grievanceProcessObj = new GrievanceProcess()
                {
                    GrievanceMasterId = grievanceMasterId,
                    Title = grievanceMaster.Title,
                    Description = grievanceMaster.Description,
                    Round = (int)Grievance_Utility.GrievanceRound.Second,
                    StatusId = (int)Grievance_Utility.GrievanceStatus.InProgress,
                    RowStatus = Grievance_Utility.RowStatus.Active,
                    AssignedUserCode = nodalOfficer.UserCode,
                    AssignedUserDetails = nodalOfficer.UserDetails,
                    CreatedBy = Convert.ToInt32(grievanceMaster.UserCode),
                    CreatedDate = DateTime.Now,

                    TGroupId = tGroupId,
                    TUnitId = lastResolverUnit,
                    TDepartment = null
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

                responseModel.StatusCode = HttpStatusCode.OK;
                responseModel.Message = "Second round of grievance initiated successfully.";
                responseModel.Data = grievanceProcessObj.Id;
            }
            catch (Exception ex)
            {
                responseModel.StatusCode = HttpStatusCode.InternalServerError;
                responseModel.Message = "An error occurred: " + ex.Message;
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

                var committeeMember = (from r in _dbContext.AppRoles
                                       join g in _dbContext.Groups on r.Id equals g.RoleId
                                       join gm in _dbContext.UserGroupMappings on g.Id equals gm.GroupId
                                       where r.RoleName == Constant.AppRoles.Committee
                                       select new
                                       {
                                           UserCode = gm.UserCode,
                                           UserDetails = gm.UserDetails,
                                           UnitId = gm.UnitId,
                                           GroupId = g.Id
                                       }).FirstOrDefault();

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
                    //ServiceId = grievanceMaster.ServiceId,
                    Round = (int)Grievance_Utility.GrievanceRound.Third,
                    StatusId = (int)Grievance_Utility.GrievanceStatus.InProgress,
                    RowStatus = Grievance_Utility.RowStatus.Active,
                    AssignedUserCode = committeeMember.UserCode,
                    AssignedUserDetails = committeeMember.UserDetails,
                    CreatedBy = Convert.ToInt32(grievanceMaster.UserCode),
                    CreatedDate = DateTime.Now,

                    TGroupId = committeeMember.GroupId,
                    TUnitId = committeeMember.UnitId,
                    TDepartment = null
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

                responseModel.StatusCode = HttpStatusCode.OK;
                responseModel.Message = "Third round of grievance initiated successfully.";
                responseModel.Data = grievanceProcessObj.Id;
            }
            catch (Exception ex)
            {
                responseModel.StatusCode = HttpStatusCode.InternalServerError;
                responseModel.Message = "An error occurred: " + ex.Message;
            }
            return responseModel;
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
                            Round = gp.Round,
                            AssignedUserCode = gp.AssignedUserCode,
                            AssignedUserDetails = gp.AssignedUserDetails,
                            StatusId = gp.StatusId,
                            CreatedBy = grievanceDetail.UserCode,
                            CreatedDate = grievanceDetail.CreatedDate,
                            ModifiedBy = gp.CreatedBy,
                            ModifiedDate = gp.CreatedDate,

                            TGroupId = gp.TGroupId,
                            TUnitId = gp.TUnitId,
                            TDepartment = gp.TDepartment
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
                List<GrievanceChange> allChanges = new List<GrievanceChange>();
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
                processChanges.ChangeList = allChanges;
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
                    List<GrievanceChange> allChanges = new List<GrievanceChange>();
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

                    var processCount = $"Process {i}";
                    if (i > 0)
                    {
                        var currentRow = allProcesses[i];
                        var previousRow = allProcesses[i - 1];
                        var changes = CompareRows(previousRow, currentRow);
                        allChanges.AddRange(changes.Select(change => new GrievanceChange
                        {
                            Column = change.Column,
                            OldValue = change.OldValue,
                            NewValue = change.NewValue,
                            ProcessCount = processCount
                        }));

                        processChanges.ChangeBy = empList.Find(a => a.empCode == allProcesses[i - 1].CreatedBy.ToString())?.empName ?? string.Empty;
                        processChanges.CaseName = processCount;
                        processChanges.ModifyDate = allProcesses[i - 1].CreatedDate;
                    }

                    processChanges.GrievanceProcessId = allProcesses[i].Id;
                    processChanges.CommentDetails = commentList;
                    processChanges.ChangeList = allChanges;

                    //if (processChanges.CommentDetails.Count == 0)
                    //{
                    //    continue;
                    //}
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

        //Function to compare two rows and return the changes
        private List<GrievanceChange> CompareRows(GrievanceProcess previousRow, GrievanceProcess currentRow)
        {
            List<GrievanceChange> changes = new List<GrievanceChange>();

            var CompareOnlyColumns = new List<string> {
                "AssignedUserCode", "AssignedUserDetails", "TUnitId", "TGroupId", "TDepartment", "CreatedBy","CreatedDate"
            };
            var properties = (typeof(GrievanceProcess).GetProperties()).Where(a => CompareOnlyColumns.Contains(a.Name));
            foreach (var property in properties)
            {
                var columnName = property.Name;
                var oldValue = property.GetValue(previousRow);
                var newValue = property.GetValue(currentRow);

                if (!object.Equals(oldValue, newValue))
                {
                    changes.Add(new GrievanceChange
                    {
                        Column = columnName,
                        OldValue = newValue,
                        NewValue = oldValue
                    });
                }
            }
            return changes;
        }

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

            if (userAllRole.Contains(Constant.AppRoles.SuperAdmin) || userAllRole.Contains(Constant.AppRoles.Committee))
            {
                // to ignore the conditions because of priority role for a user having multiple roles
            }
            else if (userAllRole.Contains(Constant.AppRoles.NodalOfficer) || userAllRole.Contains(Constant.AppRoles.UnitCGM) || userAllRole.Contains(Constant.AppRoles.Admin))
            {
                if (string.IsNullOrEmpty(unitId))
                {
                    var assignedUnit = new List<string>();
                    assignedUnit = _dbContext.UserRoleMappings.Where(a => a.UserCode == userCode && a.Role.RoleName == Constant.AppRoles.Admin).Select(a => a.UnitId).ToList() ?? new List<string>();

                    assignedUnit.AddRange(
                        (from r in _dbContext.AppRoles
                         join g in _dbContext.Groups on r.Id equals g.RoleId
                         join gm in _dbContext.UserGroupMappings on g.Id equals gm.GroupId
                         where r.RoleName == Constant.AppRoles.NodalOfficer || r.RoleName == Constant.AppRoles.UnitCGM
                         select gm.UnitId).ToList());

                    query = query.Where(a => assignedUnit.Contains(a.UnitId));
                }
            }
            else if (userAllRole.Contains(Constant.AppRoles.Redressal))
            {
                var userGroups = _dbContext.UserGroupMappings
                    .Where(x => x.UserCode == userCode)
                    .Select(x => new { x.GroupId, x.UnitId });

                var userDepartments = _dbContext.UserDepartmentMappings
                    .Where(x => userGroups.Select(a => a.UnitId).Contains(x.UnitId) && x.UserCode == userCode)
                    .Select(x => new { x.Department, x.UnitId, x.UserCode });

                var resolvedGrievanceIds = _dbContext.GrievanceProcesses
                    .Where(gp => gp.CreatedBy == Convert.ToInt32(userCode) && (gp.StatusId == (int)Grievance_Utility.GrievanceStatus.Closed))
                    .Select(gp => gp.GrievanceMasterId);

                var masterGrievance = await (from gm in _dbContext.GrievanceMasters
                                             join gp in _dbContext.GrievanceProcesses on gm.Id equals gp.GrievanceMasterId
                                             where (userDepartments.Select(a => a.Department).Contains(gm.Department)
                                             && userDepartments.Select(a => a.UnitId).Contains(gm.UnitId)
                                             && (gp.AssignedUserCode == userCode && gp.StatusId != (int)Grievance_Utility.GrievanceStatus.Closed)
                                             || gp.StatusId == (int)Grievance_Utility.GrievanceStatus.Open)
                                             || resolvedGrievanceIds.Contains(gm.Id)
                                             select gm.Id).ToListAsync();

                query = query.Where(g => masterGrievance.Contains(g.Id));
            }

            finalData.TotalGrievance = query.Count();
            finalData.Pending = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.Open).Count();
            finalData.InProgress = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.InProgress).Count();
            finalData.Resolved = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.Closed).Count();

            var unresolvedQuery = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.Open || a.StatusId == (int)Grievance_Utility.GrievanceStatus.InProgress);
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
            finalData.InProgress = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.Open || a.StatusId == (int)Grievance_Utility.GrievanceStatus.InProgress).Count();
            finalData.Resolved = query.Where(a => a.StatusId == (int)Grievance_Utility.GrievanceStatus.Closed).Count();

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

        public async Task<ResponseModel> GetResolutionDataAsync(int grievanceMasterId)
        {
            ResponseModel responseModel = new ResponseModel
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Bad Request"
            };

            if (grievanceMasterId == 0)
            {
                return new ResponseModel
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Message = "Grievance not found."
                };
            }

            var resolution = await _dbContext.ResolutionDetails.Where(a => a.GrievanceMasterId == grievanceMasterId && a.ResolutionStatus == Constant.ResolutionStatus.Pending && a.Round < 3).OrderByDescending(a => a.Id).FirstOrDefaultAsync();

            if (resolution != null)
            {
                responseModel.StatusCode = HttpStatusCode.OK;
                responseModel.Message = "Resolution data retrieved successfully.";
                responseModel.Data = resolution;
            }
            else
            {
                responseModel.StatusCode = HttpStatusCode.OK;
                responseModel.Message = "Resolution data not found.";
            }
            return responseModel;
        }

    }
}
