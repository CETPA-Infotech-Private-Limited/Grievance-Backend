using Grievance_BAL.IServices;
using Grievance_BAL.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grievance_BAL.InjectionExtension
{
    public static class ServiceExtension
    {
        public static void AddBusinessAccessLayerServices(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<IGrievanceRepository, GrievanceRepository>();
            services.AddScoped<ICommonRepository, CommonRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<Iwhatsappservice, WhatsappServices>();

        }
    }
}
