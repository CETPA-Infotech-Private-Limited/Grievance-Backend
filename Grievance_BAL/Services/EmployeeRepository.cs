using System.Net.Http.Headers;
using Grievance_BAL.IServices;
using Grievance_Model.DTOs.AppResponse;
using Grievance_Model.DTOs.Employee;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Grievance_BAL.Services
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly IConfiguration _conf;
        public EmployeeRepository(IConfiguration conf)
        {
            _conf = conf;
        }
        public async Task<EmployeeDetails> GetEmployeeDetails(int empId)
        {
            var Environment = _conf["DeploymentModes"]?.ToString().Trim();
            EmployeeDetails employeeDetails = null;
            var BaseUrl = "";
            if (Environment == "DFCCIL")
            {
                BaseUrl = _conf["ApiBaseUrlsProd:Organization"];
            }
            else if (Environment == "DFCCIL_UAT")
            {
                BaseUrl = _conf["ApiBaseUrlsDfcuat:Organization"];
            }
            else
            {
                BaseUrl = _conf["ApiBaseUrlsCetpauat:Organization"];
            }
           

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(BaseUrl + "/api/Organization/GetReporting/" + empId + "");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = client.GetAsync(client.BaseAddress).Result;
            if (response.IsSuccessStatusCode)
            {
                string apiResponse = await response.Content.ReadAsStringAsync();
                if (apiResponse != null)
                {
                    ResponseModel respone = JsonConvert.DeserializeObject<ResponseModel>(apiResponse);
                    if (!Equals(respone.Data, null))
                    {
                        employeeDetails = JsonConvert.DeserializeObject<EmployeeDetails>(respone.Data.ToString());
                    }
                }
            }
            return employeeDetails;
        }

        public async Task<EmployeeDetails> GetEmployeeDetailsWithEmpCode(int empCode)
        {
            var Environment = _conf["DeploymentModes"]?.ToString().Trim();
            EmployeeDetails employeeDetails = null;
            var BaseUrl = "";
            if (Environment == "DFCCIL")
            {
                BaseUrl = _conf["ApiBaseUrlsProd:Organization"];
            }
            else if (Environment == "DFCCIL_UAT")
            {
                BaseUrl = _conf["ApiBaseUrlsDfcuat:Organization"];
            }
            else
            {
                BaseUrl = _conf["ApiBaseUrlsCetpauat:Organization"];
            }
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(BaseUrl + "/api/Organization/GetEmployeDetailsWithEmpCode/" + empCode + "");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = client.GetAsync(client.BaseAddress).Result;
            if (response.IsSuccessStatusCode)
            {
                string apiResponse = await response.Content.ReadAsStringAsync();
                if (apiResponse != null)
                {
                    ResponseModel respone = JsonConvert.DeserializeObject<ResponseModel>(apiResponse);
                    if (!Equals(respone.Data, null))
                    {
                        employeeDetails = JsonConvert.DeserializeObject<EmployeeDetails>(respone.Data.ToString());
                    }
                }
            }
            return employeeDetails;
        }

        public async Task<List<EmployeeDetails>> GetOrganizationHierarchy()
        {
            var Environment = _conf["DeploymentModes"]?.ToString().Trim();
           
            var BaseUrl = "";
            if (Environment == "DFCCIL")
            {
                BaseUrl = _conf["ApiBaseUrlsProd:Organization"];
            }
            else if (Environment == "DFCCIL_UAT")
            {
                BaseUrl = _conf["ApiBaseUrlsDfcuat:Organization"];
            }
            else
            {
                BaseUrl = _conf["ApiBaseUrlsCetpauat:Organization"];
            }


            List<EmployeeDetails> employeeDetails = new List<EmployeeDetails>();
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(BaseUrl + "/api/Organization/GetOrganizationHierarchy");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = client.GetAsync(client.BaseAddress).Result;
            if (response.IsSuccessStatusCode)
            {
                string apiResponse = await response.Content.ReadAsStringAsync();
                if (apiResponse != null)
                {
                    ResponseModel respone = JsonConvert.DeserializeObject<ResponseModel>(apiResponse);
                    if (!Equals(respone.Data, null))
                    {
                        employeeDetails = JsonConvert.DeserializeObject<List<EmployeeDetails>>(respone.Data.ToString());
                    }
                }
            }
            return employeeDetails;
        }
    }
}
