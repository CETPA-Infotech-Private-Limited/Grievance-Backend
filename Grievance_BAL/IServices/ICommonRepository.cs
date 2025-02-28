using Grievance_Model.DTOs.AppResponse;
using Microsoft.AspNetCore.Http;

namespace Grievance_BAL.IServices
{
    public interface ICommonRepository
    {
        Task<List<UploadDocsResponse>> UploadDocumets(List<IFormFile> files, string folderPathName = "GrievanceAttachment");
    }
}
