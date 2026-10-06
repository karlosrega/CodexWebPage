using System.Web.Mvc;
using System.Web.Security;
using A1IntegrationsAdmin.Core;
using A1IntegrationsAdmin.Web.Infrastructure;
using A1IntegrationsAdmin.Web.Models;

namespace A1IntegrationsAdmin.Web.Controllers
{
    public sealed class AccountController : Controller
    {
        [HttpGet] public ActionResult Index() => View(new PasswordForm());
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Index(PasswordForm form)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    Services.Auth.ChangePassword(Services.Current.Id, form.CurrentPassword, form.NewPassword);
                    FormsAuthentication.SignOut();
                    return Redirect("/?changed=1");
                }
                catch (RuleException exception) { ModelState.AddModelError("", exception.Message); }
            }
            ModelState.Remove("CurrentPassword");
            ModelState.Remove("NewPassword");
            ModelState.Remove("ConfirmPassword");
            return View(new PasswordForm());
        }
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Services.Auth.Logout(Services.SessionToken);
            FormsAuthentication.SignOut();
            return Redirect("/");
        }
    }
}
