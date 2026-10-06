-- 002 / Public catalogue and permission catalogue. Never seeds passwords.
BEGIN;
SELECT pg_advisory_xact_lock(418048);
INSERT INTO a1admin.permissions(code,label) VALUES
('users.read','Consultar usuarios'),('users.write','Administrar usuarios'),
('profiles.read','Consultar perfiles'),('profiles.write','Administrar perfiles') ON CONFLICT DO NOTHING;
INSERT INTO a1admin.profiles(name,description,is_system) VALUES ('Administrador','Administración completa de usuarios y perfiles',true) ON CONFLICT DO NOTHING;
INSERT INTO a1admin.profile_permissions(profile_id,permission_code)
SELECT p.id,q.code FROM a1admin.profiles p CROSS JOIN a1admin.permissions q WHERE p.is_system ON CONFLICT DO NOTHING;
INSERT INTO a1admin.promotions(position,anchor,category,title,body,image_path,product_url) VALUES
(1,'lobby','01 / LOBBY','La experiencia comienza antes de la función.','Taquilla y autoservicio para conectar la llegada de tus visitantes con la venta de entradas y la operación de tu cine.','/Content/images/cinema-lobby.jpg','https://admit-one.eu/solutions/lobby/'),
(2,'alimentos','02 / ALIMENTOS Y BEBIDAS','Más sabor. Menos espera.','Puntos de venta de alimentos y bebidas integrados para facilitar el trabajo de tu equipo y el servicio en dulcería.','/Content/images/cinema-concessions.jpg','https://admit-one.eu/solutions/food-and-beverage/'),
(3,'digital','03 / VENTAS DIGITALES','Tu próxima función, a un toque.','Conecta la venta de entradas y productos a través de canales digitales para acompañar a tus clientes donde estén.','/Content/images/cinema-digital.jpg','https://admit-one.eu/solutions/digital-sales/'),
(4,'operacion','04 / BACK OFFICE','Toda tu operación en perspectiva.','Organiza funciones, inventarios, entradas, membresías y vales desde una plataforma central de administración.','/Content/images/cinema-lobby.jpg','https://admit-one.eu/solutions/back-office/'),
(5,'experiencia','05 / EXPERIENCIA DEL CLIENTE','Haz que quieran volver.','Programas de recompensas e incentivos personalizados que acompañan a tus visitantes más allá de una sola función.','/Content/images/cinema-digital.jpg','https://admit-one.eu/solutions/guest-experience/') ON CONFLICT DO NOTHING;
INSERT INTO a1admin.schema_versions(version) VALUES (2) ON CONFLICT DO NOTHING;
COMMIT;
