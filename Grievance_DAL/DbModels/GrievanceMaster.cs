using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Grievance_Utility;

namespace Grievance_DAL.DbModels
{
    public class GrievanceMaster : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public int ServiceId { get; set; }
        [ForeignKey(nameof(ServiceId))]
        public virtual GroupMaster ServiceMaster { get; set; }

        public bool IsInternal { get; set; }
        public string UserEmail { get; set; }

        public string UserCode { get; set; }  
        public string UserDetails { get; set; }

        public string UnitId { get; set; }
        public string UnitName { get; set; }

        public int Round { get; set; } = (int)GrievanceRound.First; 

        public int StatusId { get; set; }
        [ForeignKey(nameof(StatusId))]
        public virtual GrievanceStatus Status { get; set; }

        public RowStatus RowStatus { get; set; } = RowStatus.Active;
    }

}
