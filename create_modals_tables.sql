-- Create Modals table if it doesn't exist
DO $$ 
BEGIN 
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_schema = 'public' AND table_name = 'Modals'
    ) THEN
        CREATE TABLE "Modals" (
            "Id" TEXT NOT NULL PRIMARY KEY,
            "Title" VARCHAR(200) NOT NULL,
            "Content" TEXT NOT NULL,
            "StartDate" DATE NOT NULL,
            "EndDate" DATE NOT NULL,
            "LinkRoute" VARCHAR(200),
            "ButtonText" VARCHAR(50),
            "CreatedAt" TIMESTAMP NOT NULL,
            "UpdatedAt" TIMESTAMP NOT NULL
        );
        CREATE INDEX "IX_Modals_StartDate_EndDate" ON "Modals" ("StartDate", "EndDate");
        RAISE NOTICE 'Created Modals table';
    ELSE
        RAISE NOTICE 'Modals table already exists';
    END IF;
END $$;

-- Create ModalReadBy table if it doesn't exist
DO $$ 
BEGIN 
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_schema = 'public' AND table_name = 'ModalReadBy'
    ) THEN
        CREATE TABLE "ModalReadBy" (
            "Id" TEXT NOT NULL PRIMARY KEY,
            "ModalId" TEXT NOT NULL,
            "UserId" VARCHAR(450) NOT NULL,
            "ReadAt" TIMESTAMP NOT NULL,
            CONSTRAINT "FK_ModalReadBy_Modals_ModalId" FOREIGN KEY ("ModalId") REFERENCES "Modals" ("Id") ON DELETE CASCADE
        );
        CREATE INDEX "IX_ModalReadBy_ModalId_UserId" ON "ModalReadBy" ("ModalId", "UserId");
        RAISE NOTICE 'Created ModalReadBy table';
    ELSE
        RAISE NOTICE 'ModalReadBy table already exists';
    END IF;
END $$;

