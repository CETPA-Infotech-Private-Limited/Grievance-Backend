namespace Grievance_Model.DTOs.Group
{
    public class ServiceGroupModel
    {
        public int Id { get; set; }
        public string GroupName { get; set; }
        public int? ParentGroupId { get; set; }
        public bool? IsActive { get; set; } // 1 = Active, null = Active, 0 = Inactive
        public bool? IsServiceCategory { get; set; } // 1 = Service, 0 = Not Service
        public List<ServiceGroupModel>? ChildGroup { get; set; }
    }
}
