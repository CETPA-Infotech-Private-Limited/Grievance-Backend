namespace Grievance_Model.DTOs.Employee
{
    public class EmployeeDetails
    {
        public int empId { get; set; }
        public string empName { get; set; }
        public string empCode { get; set; }
        public string designation { get; set; }
        public string empEmail { get; set; }
        public string empMobileNo { get; set; }
        public string department { get; set; }
        public string units { get; set; }
        public int unitId { get; set; }
        public string lavel { get; set; }
        public Nullable<int> managerId { get; set; }
        public string managerName { get; set; }
        public string Role { get; set; }
    }
}
