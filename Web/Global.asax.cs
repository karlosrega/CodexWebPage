using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Helpers;
using A1IntegrationsAdmin.Web.Infrastructure;

namespace A1IntegrationsAdmin.Web
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AntiForgeryConfig.SuppressXFrameOptionsHeader = true;
            MvcHandler.DisableMvcResponseHeader = true;
            GlobalFilters.Filters.Add(new AccessFilter());
            GlobalFilters.Filters.Add(new ErrorFilter());
            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            RouteTable.Routes.MapRoute("Default", "{controller}/{action}/{id}", new
            {
                controller = "Public",
                action = "Index",
                id = UrlParameter.Optional
            });
        }

        protected void Application_EndRequest()
        {
            if (Response.StatusCode == 403)
                Response.SuppressFormsAuthenticationRedirect = true;
        }
    }
}
