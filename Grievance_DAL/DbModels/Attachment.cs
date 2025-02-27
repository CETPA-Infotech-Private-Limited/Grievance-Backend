using System.ComponentModel.DataAnnotations;

namespace Grievance_DAL.DbModels
{
    public class Attachment : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public string Type { get; set; }
        public int ReferenceId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }

}
