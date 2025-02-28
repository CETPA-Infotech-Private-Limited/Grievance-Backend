using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Grievance_DAL.DbModels
{
    public class UserRoleMapping : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public int RoleId { get; set; }
        [ForeignKey(nameof(RoleId))]
        public virtual AppRole Role { get; set; }

        public string UserCode { get; set; }
        public string UserDetails { get; set; }
        public string UnitId { get; set; }
        public string UnitName { get; set; }
    }

}
