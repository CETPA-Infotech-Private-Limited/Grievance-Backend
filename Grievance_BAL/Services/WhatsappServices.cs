using Grievance_BAL.IServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grievance_BAL.Services
{
   public class WhatsappServices:Iwhatsappservice
    {

        private readonly IConfiguration _configuration;
        

        public WhatsappServices(IConfiguration configuration)
        {
            _configuration = configuration;
         
        }
        public async Task<bool> SendResolutionWhatsAppAsync(string userName, string grievanceId, string round, string acceptLink, string rejectLink, string mobileNumber)
        {
            string url = _configuration["WhatsappUrl"];
            string apiKey = _configuration["WhatsAppApiKey"];
            string TestNumber = _configuration["TestNumber"];

            try
            {
                var deploymentMode = _configuration["DeploymentModes"];
                var templateName = "";
                if (deploymentMode == "CETPA" || deploymentMode == "DFCCIL_UAT")
                {
                    templateName = "grievance_resolved_uat_new";
                }
                else if (deploymentMode == "DFCCIL")
                {
                    templateName = "grievance_resolved_prod_new";
                }

                // If in test environment, redirect all messages to test number
                string receiverNumber = mobileNumber;
                if (deploymentMode == "CETPA")
                {
                    receiverNumber = TestNumber;
                }

                // Ensure number has country code
                if (receiverNumber.Length == 10)
                {
                    receiverNumber = "91" + receiverNumber;
                }

                // Format the full URLs
                string fullAcceptLink =  acceptLink;
                string fullRejectLink =  rejectLink;

                var body = @"{
    ""messaging_product"": ""whatsapp"",
    ""recipient_type"": ""individual"",
    ""to"": """ + receiverNumber + @""",
    ""type"": ""template"",
    ""template"": {
        ""name"": """ + templateName + @""",
        ""language"": {
            ""code"": ""en""
        },
        ""components"": [
            {
                ""type"": ""body"",
                ""parameters"": [
                    {
                        ""type"": ""text"",
                        ""text"": """ + userName + @"""
                    },
                    {
                        ""type"": ""text"",
                        ""text"": """ + grievanceId + @"""
                    }
                ]
            },
            {
                ""type"": ""button"",
                ""sub_type"": ""url"",
                ""index"": ""0"",
                ""parameters"": [
                    {
                        ""type"": ""text"",
                        ""text"": """ + fullAcceptLink + @"""
                    }
                ]
            },
            {
                ""type"": ""button"",
                ""sub_type"": ""url"",
                ""index"": ""1"",
                ""parameters"": [
                    {
                        ""type"": ""text"",
                        ""text"": """ + fullRejectLink + @"""
                    }
                ]
            }
        ]
    }
}";

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("apikey", apiKey);
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    var content = new StringContent(body, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, content);

                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception)
            {
                // Log exception
                return false;
            }
        }
    }
}
