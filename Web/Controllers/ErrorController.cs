using System.Web.Mvc;
namespace A1IntegrationsAdmin.Web.Controllers
{
    [AllowAnonymous]
    public sealed class ErrorController : Controller
    {
        public ActionResult Index()
        {
            Response.StatusCode = 503;
            Response.TrySkipIisCustomErrors = true;
            return View();
        }
        public ActionResult NotFound()
        {
            Response.StatusCode = 404;
            Response.TrySkipIisCustomErrors = true;
            return View();
        }
    }
}
