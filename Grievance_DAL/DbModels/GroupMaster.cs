using System.ComponentModel.DataAnnotations;

namespace Grievance_DAL.DbModels
{
    public class GroupMaster : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public string GroupName { get; set; }
        public int? ParentGroupId { get; set; }
        public virtual GroupMaster ParentGroup { get; set; }

        public string Description { get; set; }

    }

}
