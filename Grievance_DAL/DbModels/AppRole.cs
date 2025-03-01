using System.ComponentModel.DataAnnotations;

namespace Grievance_DAL.DbModels
{
    public class AppRole : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public string RoleName { get; set; }
        public string? Description { get; set; }

    }

}
