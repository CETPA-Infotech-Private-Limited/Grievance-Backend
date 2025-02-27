using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;

namespace Grievance_API
{
    public  static class ServiceExtension
    {
        public static IServiceCollection AddApiServices(this IServiceCollection service, IConfiguration config)
        {
            service.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            service.AddEndpointsApiExplorer();

            service.AddSwaggerGen(option =>
            {
                option.SwaggerDoc("v1", new OpenApiInfo { Title = "DFCCIL Grievance Services", Version = "v1" });
                option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter a valid token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });
                option.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                     Type=ReferenceType.SecurityScheme,
                                       Id="Bearer"
                                }
                              },
                            new string[]{}
                    }
                });
                
            });

            var allowedOrigins = config.GetSection("AllowedOrigins").Get<string[]>();

            if (allowedOrigins != null)
            {
                service.AddCors(options =>
                {
                    options.AddPolicy(name: "AllowOrigin",
                        builder =>
                        {
                            builder.WithOrigins(allowedOrigins)
                                   .AllowAnyHeader()
                                   .AllowAnyMethod();
                        });
                });
            }
       

            return service;


        }
    }
}
