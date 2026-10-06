# A1 Integrations Admin

Aplicación ASP.NET MVC 5 en C#, .NET Framework 4.8, editable en Visual Studio 2022. Utiliza PostgreSQL mediante Npgsql 8.0.8.

## Abrir el sitio instalado

El sitio local está configurado en **http://localhost:5080/**. La cuenta inicial es `admin`; la contraseña aleatoria está únicamente en `.local/initial-access.txt`, excluido del código versionado. Cambia la contraseña desde **Mi cuenta** y actualiza el nombre y el correo del administrador. El correo inicial `admin@localhost.invalid` es un marcador local, no una dirección de contacto.

Si IIS Express ya está ejecutándose en ese puerto, abre el sitio directamente. Para iniciarlo nuevamente, ejecuta `scripts/Start.ps1` o abre la solución en Visual Studio.

## Visual Studio 2022

1. Tener instalada la carga **Desarrollo de ASP.NET y web**, el Developer Pack de **.NET Framework 4.8**, IIS Express y un SDK .NET que permita cargar los proyectos SDK de la solución.
2. Abrir `A1IntegrationsAdmin.sln`.
3. Seleccionar `A1IntegrationsAdmin.Web` como proyecto de inicio.
4. Restaurar los paquetes NuGet y compilar. `scripts/Build.ps1` reproduce la compilación y sincroniza los redirects de dependencias con `Web.config`.
5. Ejecutar con IIS Express. La dirección configurada es `http://localhost:5080/`.

No es un proyecto ASP.NET Core. Todos los proyectos tienen como destino .NET Framework 4.8; la sintaxis C# se limita a 7.3.

## Configuración en otra máquina

1. Copiar `Web/ConnectionStrings.example.config` como `Web/ConnectionStrings.local.config` y establecer las credenciales locales.
2. Crear la base `A1IntegrationsAdmin` si no existe. El instalador de la aplicación no crea ni reemplaza bases de datos existentes.
3. Ejecutar `scripts/Build.ps1`.
4. Ejecutar `scripts/Initialize.ps1 -CreateAdmin`; aplicará los scripts SQL en orden y solicitará los datos del primer administrador, con contraseña oculta.
5. Ejecutar `scripts/Start.ps1`.

La variable de entorno `A1ADMIN_CONNECTION_STRING` tiene prioridad sobre el archivo local y también permite ejecutar las herramientas de consola. La contraseña de PostgreSQL no está incluida en las fuentes, documentación, scripts SQL ni archivo de ejemplo. El archivo local real está excluido por `.gitignore` y no se incorpora al contenido publicable del proyecto.

## Funcionalidad

- Página pública: acceso y cinco secciones promocionales de A1 Suite. Textos, orden, enlaces y referencias de imágenes se almacenan en PostgreSQL.
- Inicio privado: accesos a los módulos que permite el perfil del usuario.
- Usuarios: altas, edición, búsqueda, paginación, activación/desactivación y asignación de múltiples perfiles.
- Perfiles: altas, edición, activación/desactivación y permisos de consulta o administración por módulo.
- Mi cuenta: cambio de contraseña con verificación de la contraseña actual.
- Auditoría: eventos de acceso, cambios administrativos, cierre de sesión y cambio de contraseña.

Se desactivan registros en vez de eliminarlos. El Administrador es un perfil protegido y debe permanecer al menos una cuenta administrativa activa. La autorización se comprueba en el servidor y nuevamente dentro de las transacciones administrativas.

## Diseño y recursos

La identidad utiliza el logotipo oficial, rojo `#A9252B`, gris `#F5F5F5`, texto `#191919`, blanco, Poppins y Muli de [Admit One](https://admit-one.eu/). La navegación incluye barra superior y un menú de pantalla completa inspirado en [Bahía](https://www.bahia360.mx/). La implementación no utiliza la paleta ni los logotipos de Bahía.

Las tres fotografías de ambientación son originales, generadas con la herramienta integrada de imágenes y optimizadas a JPEG. Están identificadas como imágenes conceptuales. El sitio no las presenta como capturas reales del software. Todos los recursos gráficos y tipográficos se sirven localmente. Las licencias tipográficas y la procedencia de recursos están en `Web/Content/fonts/` y `docs/Assets.md`.

## Seguridad y operación

Las contraseñas se almacenan con PBKDF2-SHA256, sal aleatoria y 600.000 iteraciones. Las sesiones utilizan tokens aleatorios de 256 bits; PostgreSQL almacena solo su hash. Las cookies están cifradas por Forms Authentication, son HttpOnly y SameSite=Lax. Las sesiones vencen a los 30 minutos sin renovación automática. Cinco intentos fallidos bloquean la cuenta durante 15 minutos.

Todos los formularios que modifican datos tienen protección antifalsificación. Al cambiar usuarios, perfiles o contraseñas se revocan las sesiones afectadas. Se usan parámetros SQL, transacciones y un bloqueo administrativo de PostgreSQL para proteger la última cuenta Administrador.

El entorno actual es local. Para publicar se necesita IIS con HTTPS, configurar la conexión y permisos de la cuenta de servicio, aplicar el transform `Web.Release.config` y proteger la clave de Forms Authentication según el entorno. Una compilación Release por sí sola no aplica transforms: se aplican al publicar desde Visual Studio. En otro servidor también debe provisionarse el archivo de conexión local. El despliegue público y HTTPS externo no fueron validados en esta entrega.

Los incidentes de ejecución se registran en `Web/App_Data/errors.log` con un identificador de referencia y detalles técnicos sin contraseñas. Los errores se muestran en la interfaz; no se envían correos ni avisos externos automáticamente.

## Pruebas

- `scripts/Validate.ps1`: pruebas de criptografía, controles y conexión real. Requiere `A1ADMIN_TEST_CONNECTION_STRING` con una base cuyo nombre termine en `_tests`.
- `scripts/Test-Http.ps1`: acceso y vistas HTTP en el sitio local; utiliza el archivo de acceso inicial, sin imprimir el secreto. Después de cambiar la contraseña inicial, deberá adaptarse la obtención del secreto para repetir esas pruebas.
- `scripts/Test-Permissions.ps1`: permisos HTTP de una cuenta de prueba sobre una instancia conectada a la base de pruebas.

Las pruebas de integración reinicializan datos exclusivamente en la base separada `A1IntegrationsAdmin_tests`; nunca deben apuntarse a la base de aplicación. Las capturas y los registros de ejecución están en `artifacts/`, excluidos de versionado.

Consulta el [índice del código](docs/CodeIndex.md), el [modelo de datos](docs/Database.md) y el [reporte de validación](docs/Validation.md).
