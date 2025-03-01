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
                    UserEmail = grievanceModel.IsInternal ? userDetails.empEmail : grievanceModel.UserEmail,
                    UserCode = grievanceModel.IsInternal ? grievanceModel.UserCode : string.Empty,
                    UserDetails = grievanceModel.IsInternal ? (!string.IsNullOrEmpty(userDetails.empName) ? userDetails.empName : "NA") + (!string.IsNullOrEmpty(userDetails.empCode) ? " (" + userDetails.empCode + ")" : "") + (!string.IsNullOrEmpty(userDetails.designation) ? " - " + userDetails.designation : "") + (!string.IsNullOrEmpty(userDetails.department) ? " | " + userDetails.department : "") : string.Empty,
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

                grievanceMaster.StatusId = grievanceModel.StatusId;

                grievanceMaster.ModifyBy = Convert.ToInt32(userDetails?.empCode);
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
                    var addressal = (from sm in _dbContext.Services
                                     join ug in _dbContext.UserGroupMappings on sm.GroupMasterId equals ug.GroupId 
                                     join ud in _dbContext.UserDepartmentMappings on ug.UserCode equals ud.UserCode
                                     where ud.Department.Trim().ToLower() == userDetails.department.Trim().ToLower()
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

                if (string.IsNullOrEmpty(addressalCode))
                {
                    transaction.Rollback();
                    responseModel.StatusCode = HttpStatusCode.BadRequest;
                    responseModel.Message = "No addressal found for this department of category.";

                    return responseModel;
                }

                GrievanceProcess grievanceProcessObj = new GrievanceProcess()
                {
                    GrievanceMasterId = grievanceMaster.Id,

                    Title = grievanceModel.Title,
                    Description = grievanceModel.Description,
                    ServiceId = grievanceModel.ServiceId,
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

                // to send the resolution link to requestor
                if (grievanceProcessObj.StatusId == (int)Grievance_Utility.GrievanceStatus.Resolved)
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
                        ResolutionStatus = Constant.ResolutionStatus.Pending
                    };
                    //SendResolutionLink(resolutionDetail);
                }

                transaction.Commit();

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


        //private async Task<ResponseModel> SendResolutionLink(ResolutionDetail resolutionDetail)
        //{
        //    ResponseModel responseDetails = new ResponseModel()
        //    {
        //        StatusCode = System.Net.HttpStatusCode.NotFound,
        //        Message = "Bad Request"
        //    };

        //    IDbContextTransaction transaction = _dbContext.Database.BeginTransaction();
        //    if (!string.IsNullOrEmpty(resolutionDetail.UserEmail))
        //    {
        //        List<string> _emailToId = new List<string>()
        //        {
        //            resolutionDetail.UserEmail
        //        };

        //        StringBuilder getEmailTemplate = new StringBuilder();

        //        string htmlFilePath = @"wwwroot\NotificationEmailTemplate\TemplateSendMemberInvitation.html";

        //        StreamReader reader = File.OpenText(htmlFilePath);// Path to your 
        //        getEmailTemplate.Append(reader.ReadToEnd());
        //        reader.Close();
        //        string acceptToken = Guid.NewGuid().ToString() + "$";
        //        string rejectToken = Guid.NewGuid().ToString();
        //        string acceptLink = baseUrl + "/" + acceptToken;
        //        string rejectLink = baseUrl + "/" + rejectToken;
        //        #region Replace HTML Template Value
        //        getEmailTemplate.Replace("{empName}", emp.empName);
        //        getEmailTemplate.Replace("{VisitorName}", visitor?.FirstName);
        //        getEmailTemplate.Replace("{PrposedDate}", visitor?.MeetDate?.ToString("dd/MM/yyyy"));
        //        getEmailTemplate.Replace("{duration}", visitor.InTime + " To " + visitor.OutTime);
        //        getEmailTemplate.Replace("{Purpose}", visitor.PurposeOfVisit);
        //        getEmailTemplate.Replace("{AcceptLink}", acceptLink);
        //        getEmailTemplate.Replace("{RejectLink}", rejectLink);
        //        #endregion
        //        transaction = context.Database.BeginTransaction();
        //        sendMemberInvitation = new VisitorApproval()
        //        {
        //            VisitorId = visitor.Id,
        //            IsLinkActive = true,
        //            RequestedDate = DateTime.Now,
        //            WhomeToMeet = visitor.WhomeToMeet,
        //            AcceptLink = acceptToken,
        //            RejectLink = rejectToken,
        //            GettingConfirmationStatus = "pending",
        //        };
        //        context.VisitorApproval.Add(sendMemberInvitation);
        //        context.SaveChanges();

        //        var date = visitor?.MeetDate ?? DateTime.MinValue;
        //        var time = visitor?.InTime ?? TimeSpan.Zero;
        //        var combinedDateTime = date.Date + time;

        //        string formattedDateTime = combinedDateTime != DateTime.MinValue
        //             ? combinedDateTime.ToString("dd/MM/yyyy HH:mm")
        //                    : "N/A";



        //        string subjectTemplate = "Subject: Meeting Request | {ApplicantName} from {OrganizationName} | Date: {DateAndTime}";


        //        string emailSubject = subjectTemplate
        //                .Replace("{ApplicantName}", visitor?.FirstName ?? "Unknown")
        //                .Replace("{OrganizationName}", visitor?.OrgName ?? "Unknown")
        //                .Replace("{DateAndTime}", formattedDateTime);

        //        MailRequestModel mailRequest = new MailRequestModel()
        //        {
        //            EmailSubject = emailSubject,
        //            EmailBody = getEmailTemplate,
        //            EmailToName = emp.empName,
        //            EmailToId = _emailToId,
        //            EmailCCId = null,
        //        };

        //        var sendMailDetails = await _notificationRepository.SendNotification(mailRequest);

        //        var empMobile = emp.empMobileNo;
        //        var visitorMobile = visitor.MobileNo;
        //        var developmentMode = configuration["DeploymentModes"]?.ToString()?.ToLower();
        //        if (developmentMode == "cetpa")
        //            empMobile = configuration["WhatsappTestNumber"]?.ToString();

        //        CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
        //        TextInfo textInfo = cultureInfo.TextInfo;
        //        var visitorName = textInfo.ToTitleCase(visitor.FirstName + " " + visitor.MiddleName + " " + visitor.LastName);
        //        visitorName = Regex.Replace(visitorName, @"\s+", " ");
        //        var requestWhatsapp = new SendWhatsApppMessageRequest()
        //        {
        //            MobileNo = empMobile,
        //            Name = visitorName,
        //            OrgName = visitor.OrgName,
        //            WhometoMeet = emp.empName,
        //            MeetDate = visitor.MeetDate.Value.ToString("dd/MM/yyyy"),
        //            StartTime = visitor.InTime.ToString().Substring(0, 5),
        //            EndTime = visitor.OutTime.ToString().Substring(0, 5),
        //            Remarks = visitor.PurposeOfVisit,
        //            AcceptToken = acceptToken,
        //            RejectToken = rejectToken
        //        };
        //        if (developmentMode != "cetpa")
        //        {
        //            await sendWhatsApp.SendWhatsApppMessage(requestWhatsapp);
        //        }


        //        if (!string.IsNullOrEmpty(visitorMobile))
        //        {
        //            var requestWhatsappVisitor = new SendWhatsApppMessageRequest()
        //            {
        //                MobileNo = visitorMobile,
        //                Name = visitorName,
        //                OrgName = visitor.OrgName,
        //                WhometoMeet = emp.empName,
        //                MeetDate = visitor.MeetDate.Value.ToString("dd/MM/yyyy"),
        //                StartTime = visitor.InTime.ToString().Substring(0, 5),
        //                EndTime = visitor.OutTime.ToString().Substring(0, 5),
        //                Remarks = visitor.PurposeOfVisit,
        //                AcceptToken = acceptToken,
        //                RejectToken = rejectToken
        //            };
        //            if (developmentMode != "cetpa")
        //            {
        //                await sendWhatsApp.SendWhatsAPPRequestConfirmation(requestWhatsappVisitor);
        //            }
        //        }

        //        ///
        //        if (sendMailDetails != null && sendMailDetails.StatusCode == HttpStatusCode.OK)
        //        {

        //            transaction.Commit();

        //            responseDetails.StatusCode = HttpStatusCode.OK;
        //            responseDetails.Message = "The invitation has been sent successfully.";
        //        }
        //        else
        //        {
        //            responseDetails.StatusCode = HttpStatusCode.BadRequest;
        //            responseDetails.Message = "Invitation not sent due to issue in SMTP. Contact to Admin IT Team. " + sendMailDetails.Message + "";
        //        }

        //    }
        //    else
        //    {
        //        responseDetails.StatusCode = HttpStatusCode.BadRequest;
        //        responseDetails.Message = "Employee details are not getting from DFCClL API. Contact to Admin IT Team.";
        //    }
        //    return responseDetails;
        //}

        //public async Task<ResponseModel> VerifyInvitationLink(string InvitationLink)
        //{
        //    ResponseModel responseDetails = new ResponseModel()
        //    {
        //        StatusCode = System.Net.HttpStatusCode.NotFound,
        //        Message = "Invalid Invitation Link."
        //    };
        //    if (!string.IsNullOrEmpty(InvitationLink))
        //    {
        //        var getInvitationLink = context.VisitorApproval.Where(opt => opt.AcceptLink == InvitationLink && opt.IsLinkActive == true).FirstOrDefault();
        //        if (getInvitationLink != null)
        //        {

        //            var vister = await context.VisitorMaster.FirstOrDefaultAsync(x => x.Id == getInvitationLink.VisitorId);

        //            DateTime meetDate = Convert.ToDateTime(vister?.MeetDate?.Date);
        //            TimeSpan meetTime = (TimeSpan)(vister?.InTime);

        //            DateTime meetingDateTime = meetDate.Add(meetTime);
        //            DateTime currentDateTime = DateTime.Now;

        //            if (currentDateTime > meetingDateTime)
        //            {
        //                getInvitationLink.UpdatedDate = DateTime.Now;
        //                getInvitationLink.UpdatedBy = getInvitationLink.WhomeToMeet;
        //                getInvitationLink.IsLinkActive = false;
        //                getInvitationLink.IsActive = false;
        //                context.VisitorApproval.Update(getInvitationLink);
        //                context.SaveChanges();

        //                responseDetails.StatusCode = HttpStatusCode.BadRequest;
        //                responseDetails.Message = "The scheduled date and time for the meeting have already passed.";
        //                return responseDetails;

        //            }

        //            getInvitationLink.GettingConfirmationStatus = "accepted";
        //            getInvitationLink.ConfirmationDate = DateTime.Now;
        //            getInvitationLink.UpdatedDate = DateTime.Now;
        //            getInvitationLink.UpdatedBy = getInvitationLink.WhomeToMeet;
        //            getInvitationLink.IsLinkActive = false;
        //            context.VisitorApproval.Update(getInvitationLink);
        //            context.SaveChanges();
        //            var emp = await _empRepository.GetEmployeeDetailsWithEmpCode(vister.WhomeToMeet);

        //            var empMobile = vister.MobileNo;
        //            var developmentMode = configuration["DeploymentModes"]?.ToString()?.ToLower();
        //            if (developmentMode == "cetpa")
        //                empMobile = configuration["WhatsappTestNumber"]?.ToString();

        //            CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
        //            TextInfo textInfo = cultureInfo.TextInfo;
        //            var visitorName = textInfo.ToTitleCase(vister.FirstName + " " + vister.MiddleName + " " + vister.LastName);
        //            visitorName = Regex.Replace(visitorName, @"\s+", " ");
        //            var requestWhatsapp = new SendWhatsApppMessageRequest()
        //            {
        //                MobileNo = empMobile,
        //                Name = visitorName,
        //                OrgName = vister.OrgName,
        //                WhometoMeet = emp.empName,
        //                MeetDate = vister.MeetDate.Value.ToString("dd/MM/yyyy"),
        //                StartTime = vister.InTime.ToString().Substring(0, 5),
        //                EndTime = vister.OutTime.ToString().Substring(0, 5),
        //                Remarks = vister.PurposeOfVisit,
        //                AcceptToken = getInvitationLink.AcceptLink,
        //                RejectToken = getInvitationLink.RejectLink,
        //            };
        //            if (developmentMode != "cetpa")
        //                await sendWhatsApp.SendWhatsAPPAcceptConfirmation(requestWhatsapp);

        //            ///
        //            if (vister != null)
        //            {
        //                var slots = new UserCalenderBooking()
        //                {
        //                    EmployeeId = Convert.ToInt32(getInvitationLink.WhomeToMeet),
        //                    Date = vister.MeetDate,
        //                    InTime = vister.InTime,
        //                    OutTime = vister.OutTime,
        //                    VisitorId = vister.Id,
        //                    CreatedDate = DateTime.Now
        //                };
        //                await context.UserCalenderBooking.AddAsync(slots);
        //                await context.SaveChangesAsync();
        //            }

        //            responseDetails.StatusCode = HttpStatusCode.OK;
        //            responseDetails.Message = "Your request has been accepted successfully.";
        //            return responseDetails;
        //        }
        //        var getRejectLink = context.VisitorApproval.Where(opt => opt.RejectLink == InvitationLink && opt.IsLinkActive == true).FirstOrDefault();
        //        if (getRejectLink != null)
        //        {
        //            var vister = await context.VisitorMaster.FirstOrDefaultAsync(x => x.Id == getRejectLink.VisitorId);

        //            DateTime meetDate = Convert.ToDateTime(vister?.MeetDate?.Date);
        //            TimeSpan meetTime = (TimeSpan)(vister?.InTime);

        //            DateTime meetingDateTime = meetDate.Add(meetTime);
        //            DateTime currentDateTime = DateTime.Now;

        //            if (currentDateTime > meetingDateTime)
        //            {
        //                getRejectLink.UpdatedDate = DateTime.Now;
        //                getRejectLink.UpdatedBy = getRejectLink.WhomeToMeet;
        //                getRejectLink.IsLinkActive = false;
        //                getRejectLink.IsActive = false;
        //                context.VisitorApproval.Update(getRejectLink);
        //                context.SaveChanges();

        //                responseDetails.StatusCode = HttpStatusCode.BadRequest;
        //                responseDetails.Message = "The scheduled date and time for the meeting have already passed.";
        //                return responseDetails;

        //            }


        //            getRejectLink.GettingConfirmationStatus = "rejected";
        //            getRejectLink.ConfirmationDate = DateTime.Now;
        //            getRejectLink.UpdatedDate = DateTime.Now;
        //            getRejectLink.UpdatedBy = getRejectLink.WhomeToMeet;
        //            getRejectLink.IsLinkActive = false;
        //            getRejectLink.UpdatedDate = DateTime.Now;
        //            context.Update(getRejectLink);
        //            context.SaveChanges();


        //            var emp = await _empRepository.GetEmployeeDetailsWithEmpCode(vister.WhomeToMeet);

        //            var empMobile = vister.MobileNo;
        //            var developmentMode = configuration["DeploymentModes"]?.ToString()?.ToLower();
        //            if (developmentMode == "cetpa")
        //                empMobile = configuration["WhatsappTestNumber"]?.ToString();

        //            CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
        //            TextInfo textInfo = cultureInfo.TextInfo;
        //            var visitorName = textInfo.ToTitleCase(vister.FirstName + " " + vister.MiddleName + " " + vister.LastName);
        //            visitorName = Regex.Replace(visitorName, @"\s+", " ");
        //            var requestWhatsapp = new SendWhatsApppMessageRequest()
        //            {
        //                MobileNo = empMobile,
        //                Name = visitorName,
        //                OrgName = vister.OrgName,
        //                WhometoMeet = emp.empName,
        //                MeetDate = vister.MeetDate.Value.ToString("dd/MM/yyyy"),
        //                StartTime = vister.InTime.ToString().Substring(0, 5),
        //                EndTime = vister.OutTime.ToString().Substring(0, 5),
        //                Remarks = vister.PurposeOfVisit,
        //                AcceptToken = getRejectLink.AcceptLink,
        //                RejectToken = getRejectLink.RejectLink,
        //            };
        //            if (developmentMode != "cetpa")
        //                await sendWhatsApp.SendWhatsAPPRejectConfirmation(requestWhatsapp);

        //            responseDetails.StatusCode = HttpStatusCode.OK;
        //            responseDetails.Message = "Your request has been rejected successfully.";
        //            return responseDetails;
        //        }

        //    }
        //    return responseDetails;
        //}

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
                            ServiceId = gp.ServiceId,
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
