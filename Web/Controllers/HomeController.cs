using System.Web.Mvc;
namespace A1IntegrationsAdmin.Web.Controllers
{
    public sealed class HomeController : Controller
    {
        [HttpGet] public ActionResult Index() => View();
    }
}
