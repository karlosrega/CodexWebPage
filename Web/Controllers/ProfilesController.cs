using System.Linq;
using System.Web.Mvc;
using A1IntegrationsAdmin.Core;
using A1IntegrationsAdmin.Web.Infrastructure;
using A1IntegrationsAdmin.Web.Models;

namespace A1IntegrationsAdmin.Web.Controllers
{
    public sealed class ProfilesController : Controller
    {
        [HttpGet, Permission("profiles.read")] public ActionResult Index() => View(Services.Admin.Profiles());
        [HttpGet, Permission("profiles.write")]
        public ActionResult Edit(int id = 0)
        {
            var profile = id == 0 ? new ProfileRecord { Active = true } : Services.Admin.Profiles().FirstOrDefault(p => p.Id == id);
            if (profile == null)
                return HttpNotFound();
            if (profile.System)
                return new HttpStatusCodeResult(403);
            return View(new ProfileForm { Id = profile.Id, Name = profile.Name, Description = profile.Description, Active = profile.Active, Permissions = profile.Permissions, Catalogue = Services.Admin.Permissions() });
        }
        [HttpPost, ValidateAntiForgeryToken, Permission("profiles.write")]
        public ActionResult Edit(ProfileForm form)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    Services.Admin.SaveProfile(new ProfileRecord { Id = form.Id, Name = form.Name, Description = form.Description, Active = form.Active, Permissions = form.Permissions }, Services.Current.Id);
                    TempData["Notice"] = "Perfil guardado. Los usuarios afectados deberán iniciar sesión nuevamente.";
                    return RedirectToAction("Index");
                }
                catch (RuleException exception) { ModelState.AddModelError("", exception.Message); }
            }
            form.Catalogue = Services.Admin.Permissions();
            return View(form);
        }
    }
}
