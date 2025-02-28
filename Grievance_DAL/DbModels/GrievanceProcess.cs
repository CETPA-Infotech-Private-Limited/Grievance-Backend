using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Grievance_Utility;

namespace Grievance_DAL.DbModels
{
    public class GrievanceProcess : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public int GrievanceMasterId { get; set; }
        public virtual GrievanceMaster GrievanceMaster { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }

        public int GroupId { get; set; }
        [ForeignKey(nameof(GroupId))]
        public virtual GroupMaster Group { get; set; }

        public int GroupSubTypeId { get; set; }
        [ForeignKey(nameof(GroupSubTypeId))]
        public virtual GroupMaster GroupSubType { get; set; }

        public int Round { get; set; } = (int)GrievanceRound.First; // Enum for Round

        public string AssignedUserCode { get; set; }  
        public string AssignedUserDetails { get; set; }

        public int StatusId { get; set; } 
        [ForeignKey(nameof(StatusId))]
        public virtual GrievanceStatus Status { get; set; }

        public RowStatus RowStatus { get; set; } = RowStatus.Active; // Enum for RowStatus
    }

}
