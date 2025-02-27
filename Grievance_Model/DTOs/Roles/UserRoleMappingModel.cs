namespace Grievance_Model.DTOs.Roles
{
    public class UserRoleMappingModel
    {
        public string UserCode { get; set; }
        public string UserDetails { get; set; }
        public string UnitId { get; set; }
        public string UnitName { get; set; }
        public List<int>? RoleId { get; set; }
    }
}
