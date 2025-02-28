using System.ComponentModel.DataAnnotations;

namespace Grievance_DAL.DbModels
{
    public class GrievanceStatus : BaseEntity
    {
        [Key]
        public int Id { get; set; }  
        public string StatusName { get; set; } 
        public string? InternalStatusName { get; set; }
        public string? Description { get; set; }  

    }

}
