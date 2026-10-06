# Índice de código

## 01 / Solución y configuración

| Archivo | Responsabilidad |
| --- | --- |
| [A1IntegrationsAdmin.sln](../A1IntegrationsAdmin.sln) | Cinco proyectos, configuraciones Debug y Release; Web aparece primero. |
| [Directory.Build.props](../Directory.Build.props) | C# 7.3, compilación determinista y auditoría NuGet de dependencias directas y transitivas. |
| [.editorconfig](../.editorconfig) | Formato uniforme y convenciones de indentación. |
| [NuGet.Config](../NuGet.Config) | Única fuente de paquetes: nuget.org. |
| [Web/Web.config](../Web/Web.config) | MVC, Razor, autenticación, headers, formatos de recursos y redirects de ensamblados. |
| [Web/Web.Release.config](../Web/Web.Release.config) | Cookies y autenticación sobre HTTPS al publicar. |
| [Web/ConnectionStrings.example.config](../Web/ConnectionStrings.example.config) | Plantilla sin secretos para configurar PostgreSQL. |

## 02 / Core: modelos y seguridad

| Archivo / elemento | Responsabilidad |
| --- | --- |
| [Core/Models.cs](../Core/Models.cs) / `CurrentUser` | Identidad autenticada y conjunto de permisos; `Can` consulta un permiso. |
| [Core/Models.cs](../Core/Models.cs) / `UserRecord`, `ProfileRecord` | Datos administrativos y relaciones de perfiles y permisos. |
| [Core/Models.cs](../Core/Models.cs) / `Promotion` | Contenido de una sección promocional. |
| [Core/Models.cs](../Core/Models.cs) / `PageResult<T>` | Resultados paginados de 12 registros. |
| [Core/Models.cs](../Core/Models.cs) / `RuleException` | Errores de negocio que pueden mostrarse al usuario. |
| [Core/PasswordHasher.cs](../Core/PasswordHasher.cs) / `Hash`, `Verify`, `Validate` | Generación, comprobación y política de contraseña. |
| [Core/PasswordHasher.cs](../Core/PasswordHasher.cs) / `Token`, `TokenHash` | Tokens de sesión aleatorios y hashes de almacenamiento. |

## 03 / Data: acceso a PostgreSQL y lógica de negocio

| Archivo / método | Responsabilidad |
| --- | --- |
| [Data/Database.cs](../Data/Database.cs) / constructor, `Open` | Resolver configuración y abrir conexiones agrupadas. |
| [Data/Database.cs](../Data/Database.cs) / `Command` | Crear comandos SQL parametrizados con tiempo límite. |
| [Data/Database.cs](../Data/Database.cs) / `Audit` | Insertar eventos dentro de la transacción que modifica los datos. |
| [Data/AuthService.cs](../Data/AuthService.cs) / `Login` | Verificar contraseña, aplicar bloqueo y crear sesión; registra intentos. |
| [Data/AuthService.cs](../Data/AuthService.cs) / `Resolve` | Comprobar vigencia de la sesión, cuenta activa y permisos actuales. |
| [Data/AuthService.cs](../Data/AuthService.cs) / `Logout` | Revocar sesión y registrar cierre. |
| [Data/AuthService.cs](../Data/AuthService.cs) / `ChangePassword` | Verificar contraseña anterior, actualizar hash y revocar sesiones. |
| [Data/AdminRepository.cs](../Data/AdminRepository.cs) / `Users`, `User` | Búsqueda paginada y detalle de usuario. |
| [Data/AdminRepository.cs](../Data/AdminRepository.cs) / `Profiles`, `Permissions` | Catálogos administrativos. |
| [Data/AdminRepository.cs](../Data/AdminRepository.cs) / `SaveUser`, `SaveProfile` | Validar, guardar, mantener relaciones y revocar sesiones. |
| [Data/AdminRepository.cs](../Data/AdminRepository.cs) / `ValidateUser` | Política de usuario, nombre y correo. |
| [Data/AdminRepository.cs](../Data/AdminRepository.cs) / `LockAdministration`, `RequirePermission`, `EnsureAdministrator` | Serializar operaciones, volver a verificar permisos y proteger al último administrador. |
| [Data/AdminRepository.cs](../Data/AdminRepository.cs) / `Bootstrap` | Crear el primer administrador exclusivamente con catálogo de usuarios vacío. |
| [Data/AdminRepository.cs](../Data/AdminRepository.cs) / `Promotions` | Cargar el catálogo promocional ordenado desde PostgreSQL. |

## 04 / Web: infraestructura y controladores

| Archivo / elemento | Responsabilidad |
| --- | --- |
| [Web/Global.asax.cs](../Web/Global.asax.cs) / `Application_Start` | Registrar rutas, autorización global y manejo de errores. |
| [Web/Global.asax.cs](../Web/Global.asax.cs) / `Application_EndRequest` | Conservar respuestas 403 sin redirecciones de autenticación. |
| [Web/Infrastructure/Services.cs](../Web/Infrastructure/Services.cs) / `Services` | Acceso a servicios, identidad por petición, cookies y registro técnico. |
| [Web/Infrastructure/Services.cs](../Web/Infrastructure/Services.cs) / `PermissionAttribute` | Declarar el permiso requerido por una acción. |
| [Web/Infrastructure/Services.cs](../Web/Infrastructure/Services.cs) / `AccessFilter` | Validar sesión y permisos antes de ejecutar acciones privadas. |
| [Web/Infrastructure/Services.cs](../Web/Infrastructure/Services.cs) / `ErrorFilter` | Respuesta controlada y referencia de incidentes. |
| [Web/Models/Forms.cs](../Web/Models/Forms.cs) | Modelos de formulario con validaciones y catálogos; evita enlazar entidades completas. |
| [Web/Controllers/PublicController.cs](../Web/Controllers/PublicController.cs) / `Index`, `Login`, `Load` | Pantalla pública, autenticación y catálogo promocional. |
| [Web/Controllers/HomeController.cs](../Web/Controllers/HomeController.cs) / `Index` | Inicio autenticado. |
| [Web/Controllers/UsersController.cs](../Web/Controllers/UsersController.cs) / `Index`, `Edit` GET/POST | Consulta y mantenimiento de usuarios. |
| [Web/Controllers/ProfilesController.cs](../Web/Controllers/ProfilesController.cs) / `Index`, `Edit` GET/POST | Consulta y mantenimiento de perfiles. |
| [Web/Controllers/AccountController.cs](../Web/Controllers/AccountController.cs) / `Index` GET/POST, `Logout` | Cuenta, cambio de contraseña y cierre de sesión. |
| [Web/Controllers/ErrorController.cs](../Web/Controllers/ErrorController.cs) / `Index`, `NotFound` | Páginas públicas de servicio no disponible y recurso inexistente. |

## 05 / Web: vistas, estilos y comportamiento

| Carpeta / archivo | Responsabilidad |
| --- | --- |
| [Web/Views/Shared/_Layout.cshtml](../Web/Views/Shared/_Layout.cshtml) | Logotipo, barra superior, menú desplegado y pie común. |
| [Web/Views/Public/Index.cshtml](../Web/Views/Public/Index.cshtml) | Acceso, presentación y cinco soluciones promocionales. |
| [Web/Views/Home/Index.cshtml](../Web/Views/Home/Index.cshtml) | Bienvenida y módulos permitidos. |
| [Web/Views/Users/](../Web/Views/Users/) | Tabla, búsqueda, paginación y formulario de usuario. |
| [Web/Views/Profiles/](../Web/Views/Profiles/) | Tabla y formulario de perfiles y permisos. |
| [Web/Views/Account/Index.cshtml](../Web/Views/Account/Index.cshtml) | Cambio de contraseña. |
| [Web/Views/Error/](../Web/Views/Error/) | Servicio no disponible y permiso insuficiente. |
| [Web/Content/site.css](../Web/Content/site.css) | Secciones numeradas: fuentes, navegación, acceso, promociones, administración y adaptación responsive. |
| [Web/Scripts/site.js](../Web/Scripts/site.js) | Menú con manejo de foco y Escape; mostrar/ocultar contraseña. |
| [Web/Content/images/](../Web/Content/images/), [Web/Content/fonts/](../Web/Content/fonts/) | Imágenes y fuentes locales. |

## 06 / Instalación y verificación

| Archivo | Responsabilidad |
| --- | --- |
| [Database/001_schema.sql](../Database/001_schema.sql) | Tablas, restricciones, índices y versión de esquema. |
| [Database/002_seed.sql](../Database/002_seed.sql) | Permisos, Administrador y cinco promociones; no inserta contraseñas. |
| [Tools/Program.cs](../Tools/Program.cs) | Comandos `migrate`, `create-admin`, `bootstrap-local` y `check`. |
| [Tests/Program.cs](../Tests/Program.cs) | Pruebas automatizadas de seguridad y operación en base aislada. |
| [scripts/Build.ps1](../scripts/Build.ps1) | Restaurar, compilar y sincronizar configuración de dependencias. |
| [scripts/Initialize.ps1](../scripts/Initialize.ps1) | Instalar esquema y crear administrador interactivamente. |
| [scripts/Start.ps1](../scripts/Start.ps1) | Ejecutar IIS Express. |
| [scripts/Validate.ps1](../scripts/Validate.ps1) | Ejecutar pruebas de integración. |
| [scripts/Test-Http.ps1](../scripts/Test-Http.ps1), [scripts/Test-Permissions.ps1](../scripts/Test-Permissions.ps1) | Verificar rutas, formularios y permisos por HTTP. |

## Flujo de una petición

`Navegador → Ruta MVC → AccessFilter → Controller → AuthService / AdminRepository → Database → PostgreSQL → Vista Razor`.

Solo la página pública y el manejador de errores permiten acceso anónimo. La validación de permisos ocurre antes del controlador y nuevamente dentro de las transacciones administrativas para evitar que un permiso revocado se utilice después de esperar el bloqueo.
