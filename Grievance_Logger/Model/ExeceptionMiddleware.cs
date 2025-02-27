using System.Net;
using System.Text.Json;
using Grievance_Model.DTOs.AppRequest;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Grievance_Logger.Model
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;
        private readonly IConfiguration _configuration;
        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env, IConfiguration configuration)
        {
            _env = env;
            _next = next;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, ex.Message);
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var response = _env.IsDevelopment()
                    ? new AppExecption(context.Response.StatusCode, ex.Message, ex.InnerException == null ? "" : ex.InnerException.ToString())
                    : new AppExecption(context.Response.StatusCode, ex.Message, ex.InnerException == null ? "" : ex.InnerException.ToString());

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

                var json = JsonSerializer.Serialize(response, options);

                var getRequestErrorPath = context.Request==null?"": context.Request.Path.Value;
                var getStackTrace = ex.StackTrace==null?"": ex.StackTrace;

                ErrorLogModel errorLog = new ErrorLogModel()
                {
                    CreateDateTime = DateTime.Now,
                    Details = response.Details,
                    StatusCode = response.StatuCode,
                    Message = response.Message,
                    RequestPath = getRequestErrorPath,
                    StackTrace = getStackTrace
                };

                //WriteLog(errorLog);

                await context.Response.WriteAsync(json);
            }
        }

        private void WriteLog(ErrorLogModel errorLog)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            string insertQuery = "INSERT INTO ErrorLogs (StatuCode, Message, Details, RequestPath, StackTrace, CreatedDateTime) " +
                                 "VALUES (@StatuCode, @Message, @Details, @RequestPath, @StackTrace, @CreatedDateTime)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(insertQuery, connection))
                {
                    // Add parameters to the command
                    command.Parameters.AddWithValue("@StatuCode", errorLog.StatusCode);
                    command.Parameters.AddWithValue("@Message", errorLog.Message == null ? string.Empty : errorLog.Message);
                    command.Parameters.AddWithValue("@Details", errorLog.Details == null ? string.Empty : errorLog.Details);
                    command.Parameters.AddWithValue("@RequestPath", errorLog.RequestPath == null ? string.Empty : errorLog.RequestPath);
                    command.Parameters.AddWithValue("@StackTrace", errorLog.StackTrace == null ? string.Empty : errorLog.StackTrace);
                    command.Parameters.AddWithValue("@CreatedDateTime", DateTime.Now);

                    try
                    {
                        // Open the connection
                        connection.Open();

                        // Execute the command
                        int rowsAffected = command.ExecuteNonQuery();
                        //Console.WriteLine("Rows affected: " + rowsAffected);
                    }
                    catch (SqlException ex)
                    {
                        //Console.WriteLine("SQL Error: " + ex.Message);
                    }
                    finally
                    {
                        // Close the connection
                        connection.Close();
                    }
                }
            }
        }
    }
}