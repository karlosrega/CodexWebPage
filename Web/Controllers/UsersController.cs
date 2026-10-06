using System.Linq;
using System.Web.Mvc;
using A1IntegrationsAdmin.Core;
using A1IntegrationsAdmin.Web.Infrastructure;
using A1IntegrationsAdmin.Web.Models;

namespace A1IntegrationsAdmin.Web.Controllers
{
    public sealed class UsersController : Controller
    {
        [HttpGet, Permission("users.read")]
        public ActionResult Index(string search, int page = 1)
        {
            ViewBag.Search = search;
            return View(Services.Admin.Users(search, page));
        }

        [HttpGet, Permission("users.write")]
        public ActionResult Edit(int id = 0)
        {
            var user = id == 0 ? new UserRecord { Active = true } : Services.Admin.User(id);
            if (user == null)
                return HttpNotFound();
            return View(new UserForm { Id = user.Id, UserName = user.UserName, DisplayName = user.DisplayName, Email = user.Email, Active = user.Active, ProfileIds = user.ProfileIds, Profiles = Services.Admin.Profiles().Where(p => p.Active).ToList() });
        }

        [HttpPost, ValidateAntiForgeryToken, Permission("users.write")]
        public ActionResult Edit(UserForm form)
        {
            if (form.Id == 0 && string.IsNullOrEmpty(form.Password))
                ModelState.AddModelError("Password", "Indica una contraseña inicial.");
            if (ModelState.IsValid)
            {
                try
                {
                    Services.Admin.SaveUser(new UserRecord { Id = form.Id, UserName = form.UserName, DisplayName = form.DisplayName, Email = form.Email, Active = form.Active, ProfileIds = form.ProfileIds }, form.Password, Services.Current.Id);
                    TempData["Notice"] = "Usuario guardado. Sus sesiones anteriores se han cerrado.";
                    return RedirectToAction("Index");
                }
                catch (RuleException exception) { ModelState.AddModelError("", exception.Message); }
            }
            form.Password = null;
            ModelState.Remove("Password");
            form.Profiles = Services.Admin.Profiles().Where(p => p.Active).ToList();
            return View(form);
        }
    }
}
