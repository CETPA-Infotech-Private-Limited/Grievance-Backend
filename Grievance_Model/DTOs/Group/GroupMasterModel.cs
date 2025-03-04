using System.ComponentModel.DataAnnotations;

namespace Grievance_Model.DTOs.Group
{
    public class GroupMasterModel
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string GroupName { get; set; }
        public string? Description { get; set; }
        public string? UserCode { get; set; }
        public bool IsHOD { get; set; } = false;
        public int? HODofGroupId { get; set; }
        public bool IsCommitee { get; set; } = false;
    }
}
