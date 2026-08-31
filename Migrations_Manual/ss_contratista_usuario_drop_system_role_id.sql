-- 2026-07-20 — Eliminación de columna redundante.
-- ss_contratista_usuario.system_role_id era una copia desnormalizada del rol
-- asignado en user_role (la fuente real que alimenta el JWT y role_feature).
-- Nada la leía en el backend; se quitó del modelo EF (SsContratistaUsuario).
--
-- Verificación previa (2026-07-20, prod): 76/77 valores ya existían en user_role.
-- La única excepción (usuario 208, columna decía 11) era dato obsoleto: su rol
-- efectivo siempre fue 14 (CLINICA) vía user_role, por lo que NO se migró.
--
-- Nota EF: al generar la siguiente migración con dotnet ef, el snapshot emitirá
-- un DropColumn de esta columna; quitar esa sentencia del script si ya se aplicó
-- este archivo (o la aplicación fallará con "column does not exist").

ALTER TABLE ss_contratista_usuario DROP COLUMN IF EXISTS system_role_id;
