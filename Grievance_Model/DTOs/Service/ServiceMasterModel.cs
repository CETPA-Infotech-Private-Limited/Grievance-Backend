namespace Grievance_Model.DTOs.Service
{
    public class ServiceMasterModel
    {
        public int Id { get; set; }
        public string ServiceName { get; set; }
        public string ServiceDescription { get; set; }
        public int? ParentServiceId { get; set; }        
        public int? GroupMasterId { get; set; }

        public string UserCode { get; set; }
    }
}
