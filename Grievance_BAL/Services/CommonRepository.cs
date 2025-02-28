using Grievance_BAL.IServices;
using Grievance_Model.DTOs.AppResponse;
using Microsoft.AspNetCore.Http;

namespace Grievance_BAL.Services
{
    public class CommonRepository : ICommonRepository
    {
        public async Task<List<UploadDocsResponse>> UploadDocumets(List<IFormFile> files, string folderPathName = "GrievanceAttachment")
        {
            List<UploadDocsResponse> filePaths = new();
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        string basePath = "wwwroot/UploadDocuments/" + folderPathName + "";
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string filePathName = basePath;
                        if (!Directory.Exists(filePathName))
                        {
                            Directory.CreateDirectory(filePathName);
                        }
                        string filePath = Path.Combine(filePathName, fileName); 
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        // Save the file path to the model
                        filePath = filePath.Replace("wwwroot", "");
                        var uploadDocumentResponse = new UploadDocsResponse { filePath = filePath, fileName = file.FileName };
                        filePaths.Add(uploadDocumentResponse);
                    }
                }
            }
            return filePaths;
        }
    }
}
