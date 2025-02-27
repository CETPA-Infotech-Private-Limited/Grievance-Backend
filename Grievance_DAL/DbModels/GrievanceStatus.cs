using System.ComponentModel.DataAnnotations;

namespace Grievance_DAL.DbModels
{
    public class GrievanceStatus : BaseEntity
    {
        [Key]
        public int Id { get; set; }  // Primary Key
        public string StatusName { get; set; }  // e.g., "Pending", "Resolved"
        public string Description { get; set; }  // Optional: Status description

    }

}
