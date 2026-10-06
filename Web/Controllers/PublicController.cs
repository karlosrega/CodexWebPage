using System;
using System.Diagnostics;
using System.Web.Mvc;
using A1IntegrationsAdmin.Web.Infrastructure;
using A1IntegrationsAdmin.Web.Models;

namespace A1IntegrationsAdmin.Web.Controllers
{
    [AllowAnonymous]
    public sealed class PublicController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            if (!string.IsNullOrEmpty(Services.SessionToken) && Services.Auth.Resolve(Services.SessionToken) != null)
                return RedirectToAction("Index", "Home");
            return View(Load(new LoginForm()));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(LoginForm form)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var token = Services.Auth.Login(form.UserName, form.Password);
                    if (token != null)
                    {
                        Services.SignIn(token);
                        return RedirectToAction("Index", "Home");
                    }
                    ModelState.AddModelError("", "No fue posible acceder. Revisa tus datos o intenta nuevamente más tarde.");
                }
                catch (Exception exception) when (exception is Npgsql.NpgsqlException || exception is InvalidOperationException)
                {
                    Trace.TraceError("A1Admin login unavailable: {0}", exception.GetType().Name);
                    ModelState.AddModelError("", "El servicio no está disponible. Intenta nuevamente más tarde.");
                }
            }
            form.Password = null;
            ModelState.Remove("Password");
            return View("Index", Load(form));
        }

        private static LoginForm Load(LoginForm form)
        {
            try
            {
                form.Promotions = Services.Admin.Promotions();
            }
            catch (Exception exception) when (exception is Npgsql.NpgsqlException || exception is InvalidOperationException)
            {
                form.Unavailable = true;
                Trace.TraceError("A1Admin catalogue unavailable: {0}", exception.GetType().Name);
            }
            return form;
        }
    }
}
