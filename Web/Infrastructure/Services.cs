using System;
using System.Diagnostics;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using A1IntegrationsAdmin.Core;
using A1IntegrationsAdmin.Data;

namespace A1IntegrationsAdmin.Web.Infrastructure
{
    public static class Services
    {
        private static readonly object LogLock = new object();
        public static void Log(string detail)
        {
            try
            {
                lock (LogLock)
                {
                    var directory = HttpContext.Current.Server.MapPath("~/App_Data");
                    Directory.CreateDirectory(directory);
                    File.AppendAllText(Path.Combine(directory, "errors.log"), DateTime.UtcNow.ToString("o") + " " + detail + Environment.NewLine);
                }
            }
            catch (IOException) { Trace.TraceError("A1Admin could not write local diagnostic log."); }
            catch (UnauthorizedAccessException) { Trace.TraceError("A1Admin could not write local diagnostic log."); }
        }
        public static Database Database => new Database();
        public static AdminRepository Admin => new AdminRepository(Database);
        public static AuthService Auth => new AuthService(Database);
        public static CurrentUser Current => HttpContext.Current?.Items["A1.User"] as CurrentUser;
        public static string SessionToken => HttpContext.Current?.User?.Identity?.IsAuthenticated == true ? HttpContext.Current.User.Identity.Name : null;
        public static void SignIn(string token)
        {
            var ticket = new FormsAuthenticationTicket(1, token, DateTime.Now, DateTime.Now.AddMinutes(30), false, "", FormsAuthentication.FormsCookiePath);
            HttpContext.Current.Response.Cookies.Add(new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true,
                Secure = FormsAuthentication.RequireSSL || HttpContext.Current.Request.IsSecureConnection,
                SameSite = SameSiteMode.Lax,
                Path = FormsAuthentication.FormsCookiePath
            });
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class PermissionAttribute : Attribute
    {
        public string Code
        {
            get;
        }
        public PermissionAttribute(string code)
        {
            Code = code;
        }
    }

    public sealed class AccessFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext context)
        {
            if (context.ActionDescriptor.IsDefined(typeof(AllowAnonymousAttribute), true) || context.ActionDescriptor.ControllerDescriptor.IsDefined(typeof(AllowAnonymousAttribute), true))
                return;
            context.HttpContext.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.HttpContext.Response.Cache.SetNoStore();
            var token = Services.SessionToken;
            var user = string.IsNullOrEmpty(token) ? null : Services.Auth.Resolve(token);
            if (user == null)
            {
                FormsAuthentication.SignOut();
                context.Result = new RedirectResult("/?expired=1");
                return;
            }
            context.HttpContext.Items["A1.User"] = user;
            foreach (PermissionAttribute permission in context.ActionDescriptor.GetCustomAttributes(typeof(PermissionAttribute), true))
                if (!user.Can(permission.Code))
                {
                    context.HttpContext.Response.StatusCode = 403;
                    context.HttpContext.Response.TrySkipIisCustomErrors = true;
                    context.Result = new ViewResult { ViewName = "~/Views/Error/Forbidden.cshtml" };
                    return;
                }
        }
    }

    /// <summary>Record a safe correlation ID, never connection strings or raw exception messages.</summary>
    public sealed class ErrorFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.ExceptionHandled)
                return;
            var reference = Guid.NewGuid().ToString("N").Substring(0, 12);
            Services.Log("Incident " + reference + ": " + context.Exception.GetType().FullName + " " + context.Exception.StackTrace);
            Trace.TraceError("A1Admin incident {0}: {1}; source={2}", reference, context.Exception.GetType().FullName, context.Exception.TargetSite?.Name);
            Trace.TraceError("A1Admin incident {0} stack: {1}", reference, context.Exception.StackTrace);
            var compile = context.Exception as HttpCompileException;
            if (compile?.Results != null)
                foreach (System.CodeDom.Compiler.CompilerError error in compile.Results.Errors)
                {
                    Services.Log("Compilation " + reference + ": " + error.ErrorNumber + " " + error.ErrorText + " line " + error.Line);
                    Trace.TraceError("A1Admin compilation {0}: {1} {2} line {3}", reference, error.ErrorNumber, error.ErrorText, error.Line);
                }
            Trace.Flush();
            context.ExceptionHandled = true;
            context.HttpContext.Response.StatusCode = context.Exception is HttpAntiForgeryException ? 400 : 503;
            context.HttpContext.Response.TrySkipIisCustomErrors = true;
            context.Result = new ViewResult { ViewName = "~/Views/Error/Index.cshtml", ViewData = new ViewDataDictionary { { "Reference", reference } } };
        }
    }
}
