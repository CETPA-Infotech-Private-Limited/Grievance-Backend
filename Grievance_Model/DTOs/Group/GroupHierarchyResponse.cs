namespace Grievance_Model.DTOs.Group
{
    public class GroupHierarchyResponse
    {
        public int? Id { get; set; }
        public string GroupName { get; set; }
        public string Description { get; set; }

        public bool IsCommitee { get; set; } = false;
        public bool IsHOD { get; set; } = false;
        public bool IsServiceCategory { get; set; } = false;

        public string? UnitId { get; set; }
        public List<GroupHierarchyResponse>? ChildGroups { get; set; } 
        public List<GroupMasterUser>? MappedUser { get; set; }
    }

    public class GroupHierarchyUser
    {
        public string UserCode { get; set; }
        public string UserDetail { get; set; }
        public List<string>? Departments { get; set; }
    }
}
