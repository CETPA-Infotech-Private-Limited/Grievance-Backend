using System.ComponentModel.DataAnnotations;

namespace Grievance_DAL.DbModels
{
    public class UserDepartmentMapping : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public string UserCode { get; set; }
        public string UserDetails { get; set; }

        public string Department { get; set; }
    }

}
