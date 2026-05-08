-- ============================================================
-- Fix: Hangfire tables charset to support emojis (📢, ✅, ❌, etc.)
-- Ejecutar en la base de datos donde están las tablas Hangfire
-- ============================================================

-- 1. Cambiar el charset de las tablas Hangfire a utf8mb4
ALTER TABLE Hangfire_Job CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
ALTER TABLE Hangfire_State CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
ALTER TABLE Hangfire_Hash CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
ALTER TABLE Hangfire_List CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
ALTER TABLE Hangfire_Set CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
ALTER TABLE Hangfire_Counter CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
ALTER TABLE Hangfire_AggregatedCounter CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
ALTER TABLE Hangfire_Server CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 2. Si las tablas aún no existen (empezar desde 0), no pasa nada, estos ALTER fallarán silenciosamente

-- 3. Verificar que el cambio se aplicó correctamente
SELECT TABLE_NAME, TABLE_COLLATION 
FROM information_schema.TABLES 
WHERE TABLE_SCHEMA = DATABASE() 
  AND TABLE_NAME LIKE 'Hangfire_%';