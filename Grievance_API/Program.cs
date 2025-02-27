using Grievance_API;
using Grievance_API.ActionFilter;
using Grievance_API.Extensions;
using Grievance_API.Security;
using Grievance_BAL.InjectionExtension;
using Grievance_DAL.DatabaseContext;
using Grievance_DAL.Extensions;
using Grievance_Logger.Model;
using Grievance_Model.DTOs.Notification;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);


// Add services to the container.

builder.Services.AddControllers(option =>
{
    option.Filters.Add<SSOTokenValidateFilter>();
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration);
builder.Services.AddDataAccessLayerServices(builder.Configuration);
builder.Services.AddBusinessAccessLayerServices();

var Environment = builder.Configuration["DeploymentModes"]?.ToString().Trim();

if (Environment == "CETPA")
{
    builder.Services.Configure<MailSettingsModel>(builder.Configuration.GetSection("MailSettingsCetpa"));
}
else
{
    builder.Services.Configure<MailSettingsModel>(builder.Configuration.GetSection("MailSettingsDfc"));
}

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMiddleware<SwaggerBasicAuthMiddleware>();

    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseMiddleware<SwaggerBasicAuthMiddleware>();
    app.UseSwagger();
    app.UseSwaggerUI();
}
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GrievanceDbContext>();
    dbContext.EnsureTableCreated(); 
}

//app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.UseCors("AllowOrigin");
app.MapControllers();


app.Run();
