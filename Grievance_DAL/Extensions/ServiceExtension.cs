using Grievance_DAL.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grievance_DAL.Extensions
{
    public static class ServiceExtension
    {
        public static void AddDataAccessLayerServices(this IServiceCollection services, IConfiguration config)
        {

            var Environment = config["DeploymentModes"]?.ToString().Trim();
            var connectstring = "";
            if (Environment == "DFCCIL")
            {
                connectstring = config.GetConnectionString("GrievanceProd");
            }
            else if (Environment == "DFCCIL_UAT")
            {
                connectstring = config.GetConnectionString("GrievanceDfcuat");
            }
            else
            {
                connectstring = config.GetConnectionString("GrievanceCetpa");
            }
            services.AddDbContext<GrievanceDbContext>(options =>
            {
                options.UseSqlServer(connectstring);
            });
        }
    }
}
