-- Crea el primer usuario administrador del sistema.
-- Uso (desde la raiz del repositorio, con el stack levanta):
--
--   set -a && . ./.env && set +a
--   docker compose exec -T db psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" \
--     -v admin_user=admin \
--     -v admin_pass='TU_CONTRASENA' \
--     -v admin_name='Administrador' \
--     -v admin_id='00000000' \
--     -v admin_phone='0000000000' \
--     < docker/seed-admin.sql
--
-- IMPORTANTE: las contrasenas se guardan en texto plano en la tabla 'access'
-- (comparacion en Pages/Login.cshtml.cs). Usa una clave debil solo para la
-- primera carga y cambiala despues.

INSERT INTO persons (last_name, identification, numbers, active)
SELECT :'admin_name', :'admin_id', :'admin_phone', true
WHERE NOT EXISTS (SELECT 1 FROM persons WHERE identification = :'admin_id');

INSERT INTO access ("user", password, active, person_id)
SELECT :'admin_user', :'admin_pass', true, p.id
FROM persons p
WHERE p.identification = :'admin_id'
  AND NOT EXISTS (SELECT 1 FROM access WHERE "user" = :'admin_user');
