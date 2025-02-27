using System.Net.Http.Headers;
using System.Net;
using System.Text;

namespace Grievance_API.Security
{
    public class SwaggerBasicAuthMiddleware
    {
        private readonly RequestDelegate next;
        public SwaggerBasicAuthMiddleware(RequestDelegate next)
        {
            this.next = next;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/swagger"))
            {
                string authHeader = context.Request.Headers["Authorization"];
                if (authHeader != null && authHeader.StartsWith("Basic "))
                {
                    // Get the credentials from request header
                    var header = AuthenticationHeaderValue.Parse(authHeader);
#pragma warning disable CS8604 // Possible null reference argument.
                    var inBytes = Convert.FromBase64String(header.Parameter);
#pragma warning restore CS8604 // Possible null reference argument.
                    var credentials = Encoding.UTF8.GetString(inBytes).Split(':');
                    var username = credentials[0];
                    var password = credentials[1];
                    // validate credentials
                    if (username.Equals("Cetpa321")
                      && password.Equals("Cetpa@321"))
                    {
                        await next.Invoke(context).ConfigureAwait(false);
                        return;
                    }
                }
                context.Response.Headers["WWW-Authenticate"] = "Basic";
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                context.Response.WriteAsync((int)HttpStatusCode.Unauthorized + " Unauthorized").ToString();
            }
            else
            {
                await next.Invoke(context).ConfigureAwait(false);
            }
        }
    }
}
