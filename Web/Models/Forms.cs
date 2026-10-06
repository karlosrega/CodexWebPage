using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using A1IntegrationsAdmin.Core;

namespace A1IntegrationsAdmin.Web.Models
{
    public sealed class LoginForm
    {
        [Required(ErrorMessage = "Indica tu usuario."), StringLength(80)]
        public string UserName
        {
            get; set;
        }
        [Required(ErrorMessage = "Indica tu contraseña."), StringLength(128)]
        public string Password
        {
            get; set;
        }
        public List<Promotion> Promotions { get; set; } = new List<Promotion>();
        public bool Unavailable
        {
            get; set;
        }
    }
    public sealed class UserForm
    {
        public int Id
        {
            get; set;
        }
        [Required(ErrorMessage = "Indica un usuario."), StringLength(80), RegularExpression(@"[a-zA-Z0-9._-]{3,80}", ErrorMessage = "Usa de 3 a 80 letras, números, puntos o guiones.")]
        public string UserName
        {
            get; set;
        }
        [Required(ErrorMessage = "Indica el nombre."), StringLength(120)]
        public string DisplayName
        {
            get; set;
        }
        [Required(ErrorMessage = "Indica el correo."), StringLength(254), EmailAddress(ErrorMessage = "Indica un correo válido.")]
        public string Email
        {
            get; set;
        }
        [StringLength(128, MinimumLength = 12, ErrorMessage = "La contraseña debe tener entre 12 y 128 caracteres.")]
        public string Password
        {
            get; set;
        }
        public bool Active { get; set; } = true;
        public int[] ProfileIds { get; set; } = new int[0];
        public List<ProfileRecord> Profiles { get; set; } = new List<ProfileRecord>();
    }
    public sealed class ProfileForm
    {
        public int Id
        {
            get; set;
        }
        [Required(ErrorMessage = "Indica el nombre."), StringLength(80)]
        public string Name
        {
            get; set;
        }
        [StringLength(300)]
        public string Description
        {
            get; set;
        }
        public bool Active { get; set; } = true;
        public string[] Permissions { get; set; } = new string[0];
        public Dictionary<string, string> Catalogue { get; set; } = new Dictionary<string, string>();
    }
    public sealed class PasswordForm
    {
        [Required(ErrorMessage = "Indica la contraseña actual."), StringLength(128)]
        public string CurrentPassword
        {
            get; set;
        }
        [Required(ErrorMessage = "Indica la nueva contraseña."), StringLength(128, MinimumLength = 12)]
        public string NewPassword
        {
            get; set;
        }
        [Required, Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword
        {
            get; set;
        }
    }
}
