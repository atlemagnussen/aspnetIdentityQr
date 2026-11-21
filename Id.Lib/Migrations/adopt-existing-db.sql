-- Run ONCE against a database created by the old AspAuth.Lib (Identity + Duende) migrations.
-- The Baseline migration describes exactly that schema (verified table by table, column by column),
-- so it must be marked as applied instead of run. Idempotent. The old history rows can stay.
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261121162544_Baseline', '10.0.15')
ON CONFLICT ("MigrationId") DO NOTHING;
