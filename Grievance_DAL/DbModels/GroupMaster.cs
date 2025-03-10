using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Grievance_DAL.DbModels
{
    public class GroupMaster : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public string GroupName { get; set; }
        public string Description { get; set; }

        public bool IsCommitee { get; set; } = false; // to link the committee
        public bool IsHOD { get; set; } = false; // to link the HOD of service
        public bool IsServiceCategory { get; set; } = false; // to start the categorization of service

        public int? ParentGroupId { get; set; }
        [ForeignKey(nameof(ParentGroupId))]
        public virtual GroupMaster? ParentGroup { get; set; }

        public string? UnitId { get; set; } // to map the group with unit 
        
        
        public virtual ICollection<GroupMaster>? ChildGroups { get; set; }
    }

}
