using System.ComponentModel.DataAnnotations;

namespace Grievance_Model.Common
{
    public class BaseEntityModel
    {
        [Key]
        public int Id { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
