namespace Grievance_Model.DTOs.Group
{
    public class UserGroupMasterMappingModel
    {
        public int GroupMasterId { get; set; }
        public string UnitId { get; set; }
        public string UnitName { get; set; }
        public List<GroupUser>? UserCodes { get; set; }
    }

    public class GroupUser
    {
        public string UserCode { get; set; }
        public string UserDetails { get; set; }
    }
}
