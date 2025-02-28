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

namespace Grievance_BAL.Services
{
    public class GrievanceRepository : IGrievanceRepository
    {
        private readonly GrievanceDbContext _dbContext;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ICommonRepository _common;
        public GrievanceRepository(GrievanceDbContext dbContext, IEmployeeRepository employeeRepository, ICommonRepository common)
        {
            _dbContext = dbContext;
            _employeeRepository = employeeRepository;
            _common = common;
        }

        public async Task<ResponseModel> AddGrievance(GrievanceProcessDTO grievanceModel)
        {
            ResponseModel responseModel = new ResponseModel
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Bad Request",
            };

            if (grievanceModel != null)
            {
                var grievanceMaster = _dbContext.GrievanceMasters.Where(x => x.Id == grievanceModel.GrievanceMasterId).FirstOrDefault();
                if (grievanceMaster == null || grievanceMaster.Round == (int)Grievance_Utility.GrievanceRound.First)
                {
                    responseModel = await UpdateGrievanceRoundFirst(grievanceModel);
                }
                else
                {
                    int grievanceMasterId = grievanceMaster.Id;
                }

            }
            return responseModel;
        }

        private async Task<ResponseModel> UpdateGrievanceRoundFirst(GrievanceProcessDTO grievanceModel)
        {
            ResponseModel responseModel = new ResponseModel
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "Bad Request",
            };

            IDbContextTransaction transaction = _dbContext.Database.BeginTransaction();
            int grievanceMasterId = 0;
            var grievanceMaster = _dbContext.GrievanceMasters.Where(x => x.Id == grievanceModel.GrievanceMasterId).FirstOrDefault();
            var userDetails = await _employeeRepository.GetEmployeeDetailsWithEmpCode(Convert.ToInt32(grievanceModel.UserCode));
            if (grievanceMaster == null)
            {
                GrievanceMaster grievanceMasterobj = new GrievanceMaster()
                {
                    Title = grievanceModel.Title,
                    Description = grievanceModel.Description,
                    GroupId = grievanceModel.GroupId,
                    GroupSubTypeId = grievanceModel.GroupSubTypeId,
                    IsInternal = grievanceModel.IsInternal,
                    UserEmail = grievanceModel.IsInternal ? userDetails.empEmail : grievanceModel.UserEmail,
                    UserCode = grievanceModel.IsInternal ? grievanceModel.UserCode : string.Empty,
                    UserDetails = grievanceModel.IsInternal ? (!string.IsNullOrEmpty(userDetails.empName) ? userDetails.empName : "NA") + (!string.IsNullOrEmpty(userDetails.empCode) ? " (" + userDetails.empCode + ")" : "") + (!string.IsNullOrEmpty(userDetails.designation) ? " - " + userDetails.designation : "") + (!string.IsNullOrEmpty(userDetails.department) ? " | " + userDetails.department : "") : string.Empty,
                    Round = (int)Grievance_Utility.GrievanceRound.First,
                    StatusId = (int)Grievance_Utility.GrievanceStatus.Created,
                    RowStatus = Grievance_Utility.RowStatus.Active,

                    CreatedBy = Convert.ToInt32(userDetails?.empCode),
                    CreatedDate = DateTime.Now
                };
                _dbContext.GrievanceMasters.Add(grievanceMasterobj);
                _dbContext.SaveChanges();

                grievanceMasterId = grievanceMasterobj.Id;
            }
            else
            {

                grievanceMaster.GroupId = grievanceModel.GroupId;
                grievanceMaster.GroupSubTypeId = grievanceModel.GroupSubTypeId;

                grievanceMaster.StatusId = grievanceModel.StatusId;

                grievanceMaster.ModifyBy = Convert.ToInt32(userDetails?.empCode);
                grievanceMaster.ModifyDate = DateTime.Now;

                _dbContext.GrievanceMasters.Update(grievanceMaster);
                _dbContext.SaveChanges();

                grievanceMasterId = grievanceMaster.Id;
            }

            if (grievanceMasterId != 0)
            {
                string addressalCode = string.Empty;
                string addressalDetail = string.Empty;

                if (string.IsNullOrEmpty(grievanceModel.AssignedUserCode))
                {
                    var finalGroupId = grievanceModel.GroupSubTypeId != 0 ? grievanceModel.GroupSubTypeId : grievanceModel.GroupId;
                    var addressal = (from ug in _dbContext.UserGroupMappings
                                     join ud in _dbContext.UserDepartmentMappings on ug.UserCode equals ud.UserCode
                                     where ud.Department.Trim().ToLower() == userDetails.department.Trim().ToLower()
                                     && ug.GroupId == finalGroupId
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

                if (string.IsNullOrEmpty(addressalCode))
                {
                    transaction.Rollback();
                    responseModel.StatusCode = HttpStatusCode.BadRequest;
                    responseModel.Message = "No addressal found for this department of category.";

                    return responseModel;
                }

                GrievanceProcess grievanceProcessObj = new GrievanceProcess()
                {
                    GrievanceMasterId = grievanceMasterId,

                    Title = grievanceModel.Title,
                    Description = grievanceModel.Description,
                    GroupId = grievanceModel.GroupId,
                    GroupSubTypeId = grievanceModel.GroupSubTypeId,
                    Round = grievanceModel.Round,
                    StatusId = grievanceMaster == null ? (int)Grievance_Utility.GrievanceStatus.Created : grievanceModel.StatusId,
                    RowStatus = Grievance_Utility.RowStatus.Active,

                    AssignedUserCode = addressalCode,
                    AssignedUserDetails = addressalDetail,

                    CreatedBy = Convert.ToInt32(grievanceModel.UserCode),
                    CreatedDate = DateTime.Now
                };

                _dbContext.GrievanceProcesses.Add(grievanceProcessObj);
                _dbContext.SaveChanges();

                var grievanceProcessId = grievanceProcessObj.Id;

                var commentId = 0;
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

                    commentId = comment.Id;
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
                transaction.Commit();

                responseModel.StatusCode = HttpStatusCode.OK;
                responseModel.Message = "Grievance created successfully.";
                responseModel.Data = grievanceMasterId;
            }
            else
            {
                responseModel.StatusCode = HttpStatusCode.BadRequest;
                responseModel.Message = "Grievance failed to be created.";
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
                            GrievanceId = gp.GrievanceMasterId,
                            GrievanceProcessId = gp.Id,
                            Title = grievanceDetail.Title,
                            Description = grievanceDetail.Description,
                            Attachments = grievanceAttachments,
                            UserCode = grievanceDetail.UserCode,
                            UserDetails = grievanceDetail.UserDetails,
                            Group = gp.GroupId,
                            SubGroup = gp.GroupSubTypeId,
                            Round = gp.Round,
                            AssignedUserCode = gp.AssignedUserCode,
                            AssignedUserDetails = gp.AssignedUserDetails,
                            Status = grievanceDetail.StatusId,
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

        public async Task<ResponseModel> GrievanceHistory(int grievanceId, string baseUrl)
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
                        comment.CommentedByName = empList.Find(a => a.empId == Convert.ToInt32(item.CreatedBy))?.empName ?? string.Empty;

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
                            comment.CommentedByName = empList.Find(a => a.empId == Convert.ToInt32(item.CreatedBy))?.empName ?? string.Empty;

                            var findAttach = getAttachment.Where(x => x.ReferenceId == allProcesses[0].Id).Select(attach => baseUrl + attach.FilePath.Replace("wwwroot/", "").Replace("\\", "/")).ToList();
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

                        processChanges.ChangeBy = empList.Find(a => a.empId == Convert.ToInt32(allProcesses[i - 1].CreatedBy))?.empName ?? string.Empty;
                        processChanges.CaseName = processCount;
                        processChanges.ModifyDate = allProcesses[i - 1].CreatedDate;
                    }

                    processChanges.GrievanceId = allProcesses[i].Id;
                    processChanges.CommentDetails = commentList;
                    processChanges.ChangeList = allChanges;
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
        private List<GrievanceChange> CompareRows(GrievanceProcess previousRow, GrievanceProcess currentRow)
        {
            List<GrievanceChange> changes = new List<GrievanceChange>();

            var properties = typeof(GrievanceProcess).GetProperties();
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
    }
}
