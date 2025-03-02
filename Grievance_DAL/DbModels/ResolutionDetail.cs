using System.ComponentModel.DataAnnotations;

namespace Grievance_DAL.DbModels
{
    public class ResolutionDetail: BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public string UserCode { get; set; }
        public string UserEmail { get; set; }
        public int GrievanceMasterId { get; set; }
        public int GrievanceProcessId { get; set; }
        public int Round { get; set; }
        public DateTime ResolutionDT { get; set; }
        public string ResolverCode { get; set; }
        public string ResolverDetails { get; set; }

        public string AcceptLink { get; set; }
        public string RejectLink { get; set; }
        public string ResolutionStatus { get; set; }
    }
}
