namespace Grievance_Model.DTOs.Department
{
    public class UserDeptMappingModel
    {
        public string Department { get; set; }
        public string UnitId { get; set; }
        public string UnitName { get; set; }
        public List<DepartmentUser>? UserCodes { get; set; }
    }

    public class DepartmentUser
    {
        public string UserCode { get; set; }
        public string UserDetails { get; set; }
    }
}
