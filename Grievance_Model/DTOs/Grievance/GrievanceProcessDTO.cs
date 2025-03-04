using Grievance_Utility;
using Microsoft.AspNetCore.Http;

namespace Grievance_Model.DTOs.Grievance
{
    public class GrievanceProcessDTO
    {
        public int? Id { get; set; } = 0;

        public int GrievanceMasterId { get; set; }

        public string Title { get; set; }
        public string? Description { get; set; }

        public int ServiceId { get; set; }

        public int? Round { get; set; } = (int)GrievanceRound.First; 

        public string? AssignedUserCode { get; set; }  
        public string? AssignedUserDetails { get; set; }

        public int? StatusId { get; set; } = 1;
        public bool IsInternal { get; set; } = true;

        public string? UserEmail { get; set; }
        public string UserCode { get; set; }

        public string? CommentText { get; set; }
        public List<IFormFile>? Attachments { get; set; }

        // baseUrl for resolution link
        public string? BaseUrl { get; set; }
    }

}
