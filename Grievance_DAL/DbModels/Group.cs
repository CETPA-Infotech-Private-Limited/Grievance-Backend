using System.ComponentModel.DataAnnotations;

namespace Grievance_DAL.DbModels
{
    public class Group : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public string GroupName { get; set; }
        public int? ParentGroupId { get; set; }
        public virtual Group ParentGroup { get; set; }

        public string Description { get; set; }

    }

}
