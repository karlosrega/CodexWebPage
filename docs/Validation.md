# Reporte de validación

Fecha: 6 de octubre de 2026. Entorno: Windows, Visual Studio 2022 Community, .NET Framework 4.8, PostgreSQL 17 e IIS Express.

## Resultados

| Comprobación | Resultado |
| --- | --- |
| Compilación de la solución | Correcta; sin errores ni advertencias en la compilación final. |
| Configuraciones Debug y Release | Compiladas correctamente. |
| Todas las vistas Razor | Compilación completa correcta mediante `aspnet_compiler`, usando Roslyn; sin errores ni advertencias. |
| Pruebas de seguridad y PostgreSQL | 54 comprobaciones aprobadas en `A1IntegrationsAdmin_tests`. |
| Pruebas HTTP del sitio | 21 comprobaciones aprobadas. |
| Permisos HTTP de cuenta de lectura | 5 comprobaciones aprobadas en instancia aislada. |
| Total automatizado | **80 comprobaciones aprobadas, 0 fallos finales.** |
| Auditoría NuGet | No reportó vulnerabilidades conocidas en dependencias directas y transitivas a la fecha de ejecución. |
| Recursos privados | Archivo de conexión, App_Data y fuentes Razor no se exponen; respuestas finales 404. |
| Navegador | Acceso real, inicio, usuarios, menú superpuesto, Escape, cierre de sesión y mostrar/ocultar contraseña verificados. |
| Diseño adaptable | Sin desbordamiento horizontal de la página en 390, 768, 820, 1024 y 1440 píxeles; las tablas disponen de desplazamiento interno. |
| Identidad y recursos | Logotipo, Poppins, Muli e imágenes locales cargaron correctamente. |

Las pruebas de integración cubrieron hash de contraseña, sal aleatoria, entradas inválidas, parámetros SQL, duplicados, protección del último administrador, permisos de servidor, cambios de perfiles, desactivación de cuentas, bloqueo después de cinco fallos, fin del bloqueo, expiración, cierre y revocación de sesiones, cambio de contraseña y auditoría. Las pruebas HTTP cubrieron además formularios antifalsificación, vistas administrativas y solicitudes no autorizadas directas.

## Incidencias detectadas y resueltas

| Incidencia / alerta | Detalle y resolución |
| --- | --- |
| Conexión local bloqueada inicialmente | El entorno restringía sockets. Se verificó mediante una ejecución autorizada con acceso al servidor local. |
| Usuario PostgreSQL incorrecto | `postgresql` recibió rechazo de autenticación; el usuario confirmó `postgres` y la conexión corregida funcionó. |
| Creación inicial del administrador rechazada | La revisión automática pidió aprobación concreta para crear una credencial administrativa persistente. El usuario la otorgó y se creó `admin`, con secreto aleatorio exclusivamente local. |
| Ruta MSBuild incorrecta | Se corrigió la importación de `Microsoft.Common.props` en el proyecto web. |
| Lectura de referencias/registro restringida | La compilación se ejecutó con el acceso necesario a las herramientas de Visual Studio. |
| Referencia de C# y versiones de ensamblados | Se agregó `Microsoft.CSharp` y redirects para System.Buffers, Unsafe y System.Text.Json. |
| Vista Razor sin referencia netstandard | Se agregó la referencia explícita para la compilación de vistas con las dependencias de Npgsql. |
| Acentos mal decodificados | Se fijó UTF-8 para archivos, solicitudes y respuestas. |
| Logotipo no servido por IIS | Se declararon los tipos MIME WebP y WOFF2. |
| Compilación mientras las pruebas usaban un ensamblado | Se detectó bloqueo temporal del archivo; se separaron las ejecuciones y la compilación posterior terminó correctamente. |
| Cadena de prueba mal construida | El control de seguridad rechazó una conexión sin el sufijo `_tests`; se corrigió la construcción de la cadena manteniendo la protección. |
| Respuesta incorrecta al bloquear una vista privada | La página de error general devolvía 503; se agregó manejo específico de páginas inexistentes con 404. |
| Advertencias CS1685 del compilador antiguo | Se incorporó el proveedor Roslyn oficial. La compilación completa de vistas posterior terminó sin advertencias. |

## Alerta de herramienta y límites de validación

El formateador `dotnet format`, ejecutado con el SDK .NET 9, informa referencias de proyecto sin metadatos coincidentes al cargar el proyecto ASP.NET clásico. Es una limitación de esa herramienta al cargar la solución mixta; MSBuild de Visual Studio 2022 sí resolvió y compiló todos los proyectos. Se aplicó formato de espacios y se entregó `.editorconfig`; la compilación y las pruebas funcionales no dependen de ese formateador. No se ocultó esta advertencia ni se deshabilitaron avisos de compilación para resolverla.

Esta entrega se verificó localmente. No se validó una publicación pública, HTTPS externo, un servidor IIS de producción, envío de correos ni operación con múltiples servidores. La auditoría de paquetes es una consulta puntual y no garantiza ausencia de vulnerabilidades futuras.

La base real conserva únicamente el administrador inicial; las cuentas artificiales permanecen en la base separada de pruebas. Los accesos de validación al sitio real generan sus eventos normales de sesión y auditoría.

El correo inicial del administrador es `admin@localhost.invalid`, un marcador local que debe sustituirse desde Usuarios. La contraseña inicial se obtiene de `.local/initial-access.txt` y se cambia desde Mi cuenta.

## Evidencia local

- `artifacts/build-final.log`: compilación final.
- `artifacts/razor-validation.log`: comprobación completa de vistas.
- `artifacts/http-tests.log`: 21 comprobaciones HTTP.
- `artifacts/permission-tests.log`: 5 comprobaciones de permisos.
- `artifacts/dependency-audit.json`: resultado de auditoría NuGet.
- `artifacts/format-diagnostics.log`: detalle de la limitación del formateador.
- `artifacts/access-desktop.jpg`, `access-mobile.jpg`, `home-desktop.jpg`, `users-desktop.jpg`, `users-mobile.jpg`: capturas visuales.

La prueba de integración de 54 comprobaciones se ejecutó con el programa `Tests`; su resultado se verificó en la salida de ejecución. Los scripts permiten repetirla sobre la base aislada. Los registros históricos de App_Data pueden contener incidentes ya corregidos y rechazos antifalsificación intencionales de las pruebas; no indican por sí solos un fallo activo.
