using System.ComponentModel.DataAnnotations.Schema;

namespace Grievance_DAL.DbModels
{
    public class ServiceMaster : BaseEntity
    {
        public int Id { get; set; }
        public string ServiceName { get; set; }
        public string ServiceDescription { get; set; }
        public int? ParentServiceId { get; set; }
        [ForeignKey(nameof(ParentServiceId))]
        public virtual ServiceMaster ParentService { get; set; }
        public int? GroupMasterId { get; set; }
        [ForeignKey(nameof(GroupMasterId))]
        public virtual GroupMaster? GroupMaster { get; set; }

    }
}
