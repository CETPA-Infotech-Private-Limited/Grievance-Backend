using System.ComponentModel.DataAnnotations;

namespace Grievance_DAL.DbModels
{
    public class GroupMaster : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public string GroupName { get; set; }
        public string Description { get; set; }

        public bool IsHOD { get; set; } = false;
        public int? HODofGroupId { get; set; }
        public bool IsCommitee { get; set; } = false;

    }

}
