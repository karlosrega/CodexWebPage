# Modelo de datos

Base: `A1IntegrationsAdmin`. Esquema: `a1admin`. Servidor configurado: `localhost:5432`.

| Tabla | Contenido y relaciones |
| --- | --- |
| `schema_versions` | Número de script instalado y fecha de aplicación. |
| `users` | Usuario, nombre, correo, hash de contraseña, estado, bloqueo y fechas. Usuario y correo únicos sin distinguir mayúsculas. |
| `profiles` | Nombre, descripción, estado y marca de perfil protegido. Nombre único sin distinguir mayúsculas. |
| `permissions` | Código y etiqueta del permiso. |
| `user_profiles` | Relación muchos a muchos entre usuarios y perfiles; clave compuesta e índice por perfil. |
| `profile_permissions` | Relación muchos a muchos entre perfiles y permisos; clave compuesta. |
| `sessions` | Hash del token, usuario, creación, expiración y revocación; índices por usuario y expiración. |
| `audit_events` | Actor, acción, entidad y fecha; índice cronológico. No guarda contraseñas ni tokens. |
| `promotions` | Posición, ancla, categoría, título, texto, ruta de imagen, URL del producto y estado. |

Los datos promocionales pueden editarse mediante SQL dentro de `a1admin.promotions`; esta entrega no agrega un editor de contenidos al menú de usuarios y perfiles. Las rutas de imágenes apuntan a `Web/Content/images/`. Las anclas públicas actuales son `lobby`, `alimentos`, `digital`, `operacion` y `experiencia`.

Los scripts no eliminan tablas ni reemplazan datos existentes. `CREATE ... IF NOT EXISTS` e inserciones `ON CONFLICT DO NOTHING` permiten repetir la instalación. Para futuras modificaciones de esquema, agregar scripts numerados y migraciones explícitas; estos scripts iniciales no actualizan automáticamente columnas de esquemas anteriores incompatibles.

La base separada `A1IntegrationsAdmin_tests` contiene datos artificiales y permite repetir pruebas sin tocar las cuentas reales. Las herramientas de prueba rechazan conexiones cuyo nombre de base no termine en `_tests`.
