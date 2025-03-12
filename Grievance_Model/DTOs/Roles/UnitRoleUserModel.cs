namespace Grievance_Model.DTOs.Roles
{
    public class UnitRoleUserModel
    {
        public string UnitId { get; set; }
        public List<UnitRoleUsers> MappedUser {  get; set; }
    }

    public class UnitRoleUsers
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string UserCode { get; set; }
        public string UserDetails { get; set; }
    }
}
