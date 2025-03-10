namespace Grievance_Model.DTOs.Group
{
    public class GroupMasterModel
    {
        public int? Id { get; set; }
        public string GroupName { get; set; }
        public string? Description { get; set; }

        public bool? IsCommitee { get; set; } = false; // to link the committee
        public bool? IsHOD { get; set; } = false; // to link the HOD of service
        public bool? IsServiceCategory { get; set; } = false; // to start the categorization of service

        public int? ParentGroupId { get; set; }

        public string? UnitId { get; set; } // to map the group with unit 
        public string? UnitName { get; set; } // to map the group with unit 
        public string? CreatedBy { get; set; }

        public GroupMasterModel? ChildGroup { get; set; }
        public List<GroupMasterUser>? MappedUser { get; set; }
    }

    public class GroupMasterUser
    {
        public string UserCode { get; set; }
        public string UserDetail { get; set; }
        public List<string>? Departments { get; set; }
    }
}
