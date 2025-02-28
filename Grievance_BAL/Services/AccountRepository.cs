using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Grievance_BAL.IServices;
using Grievance_DAL.DatabaseContext;
using Grievance_Model.DTOs.AppResponse;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Grievance_BAL.Services
{
    public class AccountRepository : IAccountRepository
    {
        private readonly IUserRepository userRepo;
        private readonly IEmployeeRepository employeeRepository;
        private readonly IConfiguration configuration;
        private readonly GrievanceDbContext grievanceDbContext;
        public AccountRepository(IUserRepository userRepo, IEmployeeRepository employeeRepository, IConfiguration configuration, GrievanceDbContext grievanceDbContext)
        {
            this.configuration = configuration;
            this.userRepo = userRepo;
            this.employeeRepository = employeeRepository;
            this.grievanceDbContext = grievanceDbContext;
        }

        public async Task<ResponseModel> IsValidProgress(string token, string empCode)
        {
            ResponseModel responseModel = new ResponseModel();

            var Environment = configuration["DeploymentModes"]?.ToString().Trim();
            var BaseUrl = "";
            if (Environment == "DFCCIL")
            {
                BaseUrl = configuration["ApiBaseUrlsProd:SSO"];
            }
            else if (Environment == "DFCCIL_UAT")
            {
                BaseUrl = configuration["ApiBaseUrlsDfcuat:SSO"];
            }
            else
            {
                BaseUrl = configuration["ApiBaseUrlsCetpauat:SSO"];
            }
            if (!string.IsNullOrEmpty(empCode) && !string.IsNullOrEmpty(token))
            {
                HttpClient client = new HttpClient();
                var key = configuration["TokenKey"];
                client.BaseAddress = new Uri(BaseUrl + "/Login/IsValid?username=" + empCode + "&token=" + token + "");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage response = client.PostAsync(client.BaseAddress, null).Result;
                if (response.IsSuccessStatusCode)
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    var responsedetails = JsonConvert.DeserializeObject<RootDfccil>(apiResponse);
                    if (responsedetails != null && responsedetails.Status == "Success")
                    {
                        if (responsedetails.Employee != null)
                        {
                            var employeeDetails = employeeRepository.GetEmployeeDetailsWithEmpCode(Convert.ToInt32(empCode)).Result;
                            if (employeeDetails != null)
                            {
                                var isSuperAdmin = configuration["SuperAdmin"]?.ToString() == employeeDetails.empCode;
                                if (!isSuperAdmin)
                                {
                                    var userRoles = (List<string>)(await userRepo.GetUserRolesAsync(empCode)).Data;
                                    if (userRoles.Count > 0)
                                    {

                                        string commaSeparatedRole = string.Join(", ", userRoles);

                                        var claims = new[]
                                                         {
                                              new Claim("Roles", commaSeparatedRole),
                                              new Claim("unique_name",employeeDetails.empName),
                                              new Claim("EmpCode", employeeDetails.empCode),
                                              new Claim("Designation", employeeDetails.designation),
                                              new Claim("Unit", employeeDetails.units),
                                              new Claim("unitId", employeeDetails.unitId.ToString()),
                                              new Claim("Lavel", employeeDetails.lavel),
                                              new Claim("SSOToken", token),
                                              new Claim("Department",  employeeDetails.department)};

                                        var Tokens = GenerateJwtUsingCustomClaims(key, claims, 30);

                                        if (Tokens != null)
                                        {
                                            responseModel.Message = "token successfully return.";
                                            responseModel.StatusCode = HttpStatusCode.OK;
                                            responseModel.Data = Tokens;
                                            return responseModel;
                                        }
                                        else
                                        {
                                            responseModel.Message = "Some error occured";
                                            responseModel.StatusCode = HttpStatusCode.BadRequest;
                                            responseModel.Data = responsedetails.Employee;
                                            return responseModel;
                                        }

                                    }
                                    else
                                    {
                                        var claims = new[]{
                                          new Claim("Roles", "user"),
                                          new Claim("unique_name",employeeDetails.empName),
                                          new Claim("EmpCode", employeeDetails.empCode),
                                          new Claim("Designation", employeeDetails.designation),
                                          new Claim("Unit", employeeDetails.units),
                                          new Claim("unitId", employeeDetails.unitId.ToString()),
                                          new Claim("Lavel", employeeDetails.lavel),
                                          new Claim("SSOToken", token),
                                          new Claim("Department",  employeeDetails.department)
                                        };

                                        var Tokens = GenerateJwtUsingCustomClaims(key, claims, 30);

                                        if (Tokens != null)
                                        {
                                            responseModel.Message = "token successfully return.";
                                            responseModel.StatusCode = HttpStatusCode.OK;
                                            responseModel.Data = Tokens;
                                            return responseModel;
                                        }
                                        else
                                        {
                                            responseModel.Message = "Some error occured";
                                            responseModel.StatusCode = HttpStatusCode.BadRequest;
                                            responseModel.Data = responsedetails.Employee;
                                            return responseModel;
                                        }
                                    }
                                }
                                else
                                {
                                    var claims = new[]{
                                        new Claim("Roles", "superAdmin"),
                                        new Claim("unique_name",employeeDetails.empName),
                                        new Claim("EmpCode", employeeDetails.empCode),
                                        new Claim("Designation", employeeDetails.designation),
                                        new Claim("Unit", employeeDetails.units),
                                        new Claim("unitId", employeeDetails.unitId.ToString()),
                                        new Claim("Lavel", employeeDetails.lavel),
                                        new Claim("SSOToken", token),
                                        new Claim("Department",  employeeDetails.department)
                                    };

                                    var Tokens = GenerateJwtUsingCustomClaims(key, claims, 30);

                                    if (Tokens != null)
                                    {
                                        responseModel.Message = "token successfully return.";
                                        responseModel.StatusCode = HttpStatusCode.OK;
                                        responseModel.Data = Tokens;
                                        return responseModel;
                                    }
                                    else
                                    {
                                        responseModel.Message = "Some error occured";
                                        responseModel.StatusCode = HttpStatusCode.BadRequest;
                                        responseModel.Data = responsedetails.Employee;
                                        return responseModel;
                                    }
                                }

                            }
                            else
                            {
                                responseModel.StatusCode = HttpStatusCode.NotFound;
                                responseModel.Message = "Employee is not found ,may be employee service is not working. ";
                                return responseModel;
                            }
                        }

                    }
                    else
                    {
                        responseModel.StatusCode = HttpStatusCode.Unauthorized;
                        responseModel.Message = "Unauthorized";
                        return responseModel;
                    }
                }
            }
            else
            {
                responseModel.Message = "please provide token.";
                responseModel.StatusCode = HttpStatusCode.BadRequest;
            }

            return responseModel;

        }
        private string GenerateJwtUsingCustomClaims(string secretKey, Claim[] claims, int expiryMinutes)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(

                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public class RootDfccil
        {
            [JsonProperty("$id")]
            public string id { get; set; }
            public string Status { get; set; }
            public object DBStatus { get; set; }
            public Employee Employee { get; set; }
        }

        public class Employee
        {
            [JsonProperty("$id")]
            public string id { get; set; }
            public string EmployeeCode { get; set; }
            public string UserName { get; set; }
            public string Post { get; set; }
            public string PositionGrade { get; set; }
            public string Mobile { get; set; }
            public DateTime LoginDate { get; set; }
            public string Location { get; set; }
            public string Department { get; set; }
            public string UserType { get; set; }
            public string FNEmployeeCode { get; set; }
            public string FUserType { get; set; }
            public string EPEmployeeCode { get; set; }
            public string EPMEmployeeCode { get; set; }
            public string Unit { get; set; }
            public string UnitId { get; set; }
            public string Designation { get; set; }
            public string Level { get; set; }
        }
    }
}
