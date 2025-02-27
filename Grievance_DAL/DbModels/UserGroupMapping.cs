using System.ComponentModel.DataAnnotations;

namespace Grievance_DAL.DbModels
{
    public class UserGroupMapping : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        public int GroupId { get; set; }
        public virtual Group Group { get; set; }

        public string UserCode { get; set; }  
        public string UserDetails { get; set; }
        public string UnitId { get; set; }
        public string UnitName { get; set; }
    }

}
