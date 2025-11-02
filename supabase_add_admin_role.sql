-- Add Admin role and link admin user to it
-- Run this in Supabase SQL Editor

-- First, create Admin role if it doesn't exist
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
SELECT 
    gen_random_uuid()::text,
    'Admin',
    'ADMIN',
    gen_random_uuid()::text
WHERE NOT EXISTS (
    SELECT 1 FROM "AspNetRoles" WHERE "NormalizedName" = 'ADMIN'
);

-- Find the admin user and link to Admin role
DO $$
DECLARE
    admin_user_id TEXT;
    admin_role_id TEXT;
BEGIN
    -- Get admin user ID
    SELECT "Id" INTO admin_user_id
    FROM "AspNetUsers"
    WHERE LOWER("Email") = 'admin@sportadmin.se'
    LIMIT 1;
    
    -- Get Admin role ID
    SELECT "Id" INTO admin_role_id
    FROM "AspNetRoles"
    WHERE "NormalizedName" = 'ADMIN'
    LIMIT 1;
    
    -- Link user to role if both exist and not already linked
    IF admin_user_id IS NOT NULL AND admin_role_id IS NOT NULL THEN
        INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
        VALUES (admin_user_id, admin_role_id)
        ON CONFLICT DO NOTHING;
        
        RAISE NOTICE 'Admin user linked to Admin role';
    ELSE
        RAISE NOTICE 'Admin user or role not found. User ID: %, Role ID: %', admin_user_id, admin_role_id;
    END IF;
END $$;

