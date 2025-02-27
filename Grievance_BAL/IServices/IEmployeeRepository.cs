using Grievance_Model.DTOs.Employee;

namespace Grievance_BAL.IServices
{
    public interface IEmployeeRepository
    {
        public Task<EmployeeDetails> GetEmployeeDetails(int empId);
        public Task<EmployeeDetails> GetEmployeeDetailsWithEmpCode(int empCode);
        Task<List<EmployeeDetails>> GetOrganizationHierarchy();
    }
}
