using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Grievance_DAL.DbModels
{
    public class UserGroupMapping : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public int GroupId { get; set; }
        [ForeignKey(nameof(GroupId))]
        public virtual GroupMaster Group { get; set; }

        public string UserCode { get; set; }  
        public string UserDetails { get; set; }

        public string UnitId { get; set; }
        public string UnitName { get; set; }
    }

}
