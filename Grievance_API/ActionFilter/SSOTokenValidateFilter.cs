using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using static Grievance_BAL.Services.AccountRepository;

namespace Grievance_API.ActionFilter
{
    public class SSOTokenValidateFilter : IAuthorizationFilter
    {
        private readonly IConfiguration configuration;
        private readonly IHttpContextAccessor httpContext;

        public SSOTokenValidateFilter(IConfiguration configuration,IHttpContextAccessor httpContext)
        {
            this.configuration = configuration;
            this.httpContext = httpContext;
        }
        public void OnAuthorization(AuthorizationFilterContext context)
        {

            var allowAnonymous = context.ActionDescriptor.EndpointMetadata
            .Any(em => em is AllowAnonymousAttribute);
            if (allowAnonymous)
            {
                 return;
            }
            var authorizationHeader = httpContext.HttpContext.Request.Headers["Authorization"].FirstOrDefault();

            if (!string.IsNullOrEmpty(authorizationHeader))
            {

                var token = authorizationHeader.StartsWith("Bearer ", System.StringComparison.OrdinalIgnoreCase)
                            ? authorizationHeader.Substring("Bearer ".Length).Trim()
                            : null;

                if (!string.IsNullOrEmpty(token))
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var jwtToken = tokenHandler.ReadJwtToken(token);
                    var SSOToken = Convert.ToString(jwtToken.Claims.FirstOrDefault(x => x.Type == "SSOToken")?.Value);
                    var ssoToken = configuration["TokenKeysso"];
                    if (SSOToken.Trim() == ssoToken)
                    {
                        return;
                    }
                    int empCode = Convert.ToInt32(jwtToken.Claims.FirstOrDefault(x => x.Type == "EmpCode")?.Value == null ? "0" : jwtToken.Claims.FirstOrDefault(x => x.Type == "EmpCode")?.Value);
                 
                    
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

                    HttpClient client = new HttpClient();

                    client.BaseAddress = new Uri(BaseUrl + "/Login/IsValid?username=" + empCode + "&token=" + SSOToken + "");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpResponseMessage response = client.PostAsync(client.BaseAddress, null).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        string apiResponse = response.Content.ReadAsStringAsync().Result;
                        var responsedetails = JsonConvert.DeserializeObject<RootDfccil>(apiResponse);
                        if (responsedetails != null && responsedetails.Status == "Success")
                        {
                            if (responsedetails.Employee != null)
                            {
                                return;
                            }
                            else
                            {
                                context.Result = new JsonResult(new { message = "User is unauthorized.", statusCode = StatusCodes.Status401Unauthorized })
                                {
                                    StatusCode = StatusCodes.Status401Unauthorized
                                };
                                
                                
                            }
                        }
                        else
                        {
                            context.Result = new JsonResult(new { message = "User is unauthorized.", statusCode = StatusCodes.Status401Unauthorized })
                            {
                                StatusCode = StatusCodes.Status401Unauthorized
                            };
                        }
                    }
                    else
                    {
                        context.Result = new JsonResult(new { message = "User is unauthorized.", statusCode = StatusCodes.Status401Unauthorized })
                        {
                            StatusCode = StatusCodes.Status401Unauthorized
                        };
                    }

                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }
    }
}
