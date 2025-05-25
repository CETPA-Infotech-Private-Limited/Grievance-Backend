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

        //public int ServiceId { get; set; }
        //[ForeignKey(nameof(ServiceId))]
        //public virtual ServiceMaster ServiceMaster { get; set; }

        public int Round { get; set; } = (int)GrievanceRound.First; // Enum for Round

        public string AssignedUserCode { get; set; }  
        public string AssignedUserDetails { get; set; }

        public int StatusId { get; set; } 
        [ForeignKey(nameof(StatusId))]
        public virtual GrievanceStatus Status { get; set; }

        public RowStatus RowStatus { get; set; } = RowStatus.Active; // Enum for RowStatus

        public string? TUnitId {  get; set; } //to log the transfer changes of a grievance
        public int? TGroupId {  get; set; } //to log the transfer changes of a grievance
        public string? TDepartment { get; set; }

        public bool? IsVisited { get; set; } = true;

    }

}
