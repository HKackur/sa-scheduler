SchedulerMVP – PromptSpec

1) Kontext & mål
	•	Produkt: MVP för veckoschemaläggning för föreningar i SportAdmin.
	•	Syfte: Bygga ett veckoschema (Mån–Sön) för alla aktiviteter, på rätt platser/ytor/hallar, utan krockar.
	•	Scope: Endast veckomall (ingen datumkalender). Fokus på drag-&-drop, konfliktlogik, ghost-visning, mallhantering.
	•	Icke-mål (MVP): Automatisk optimering, koppling till SA-API/DB, bemanning/coachkonflikter, notifieringar.

2) Teknik
	•	Blazor Server + .NET 9.
	•	EF Core med Postgres (Supabase) + SQLite fallback.
	•	Konfig via .env (DotNetEnv):
	•	DB_PROVIDER=postgres|sqlite
	•	POSTGRES_CONNECTION_STRING=Host=…;Port=5432;Database=…;User Id=…;Password=…;Ssl Mode=Require;Trust Server Certificate=true
	•	Font & UI: Inter överallt, ren, celan och modernt Ui. Främst vitt, svart och gråskalor med hög kontrast
	•	JS interop för DnD/resize i week-grid.
	•	Health: /health endpoint med providerinfo.

3) Mappstruktur

SchedulerMVP/
Components/
Data/
Migrations/
Services/
wwwroot/
css/global.css
js/dragdrop.js
Pages/
Properties/
Program.cs
SchedulerMVP.csproj
SchedulerMVP.sln
README.md
PromptSpec.md

4) Datamodell (EF Core)

Nyckelidé: Konflikter bygger på leaf-coverage = minsta bokningsbara enheter.

Entiteter
	•	Place (Id, Name, DefaultDurationMin=90, SnapMin=15, VisibleStartMin=420, VisibleEndMin=1260)
	•	Area (Id, PlaceId, Name, ParentAreaId?, Path)
	•	Path ex: /Helplan/Halvplan A/A1 (för visning/sök)
	•	Leaf (Id, PlaceId, Name, SortOrder)
	•	AreaLeaf (AreaId, LeafId) (M:N, coverage)
	•	Group (Id, Name, ColorHex)
	•	ScheduleTemplate (Id, Name, PlaceId)
	•	BookingTemplate (Id, ScheduleTemplateId, AreaId, GroupId, DayOfWeek(1–7), StartMin, EndMin, Notes?)

Konfliktregel
Overlap = StartA < EndB && StartB < EndA
AreaIntersect = coverage(AreaA) ∩ coverage(AreaB) ≠ ∅
Konflikt om Overlap && AreaIntersect.

5) Seed-data
	•	Place: “Malmö IP” (Default=90, Snap=15, Visible 07–21).
	•	Leafs: A1, A2, B1, B2.
	•	Areas: Helplan (alla leafs), Halvplan A (A1+A2), Halvplan B (B1+B2), Kvartsplan A1 (A1), Kvartsplan A2 (A2), Kvartsplan B1 (B1), Kvartsplan B2 (B2).
	•	Groups: F2012, F2011, Dam A, Herr A (med färger).
	•	En demo-ScheduleTemplate “Veckoschema HT2025” med ett par bokningar.

6) UI-layout
	•	Vänster: Plats/ytor i trädstruktur + “Skapa ny plats”.
	•	Mitten: WeekGrid (Mån–Sön, 00–24, hel/halvtimmar, default scroll position 07–21). DnD/resize med snap (15 min). Ghost visas för påverkande ytor.
	•	Höger: Grupp-lista (dummygrupper med färgindikator), drag-source.

7) Interaktioner
	•	Drag grupp → släpp i grid → öppna BookingModal.
	•	BookingModal: plats/area/grupp, start/slut, defaultlängd, veckodags-checkboxar, live-konfliktkontroll.
	•	Save = en BookingTemplate per vald dag.
	•	Edit genom att klicka block.
	•	Flytta/resize block.
	•	Hover-actions: Duplicate, Copy to day…, Delete (confirm).
	•	Undo/Redo-stack (10 steg).
	•	Ghost: halvtransparent block + text “Blockerad av [Yta] – [Grupp] [tid]”.

8) Setup av ny plats
	•	Fördefinierade mallar:
	•	Fotbollsplan (halvor/kvartar) → generera leafs + areas med coverage.
	•	Simhall (antal banor, ev. delas i två) → generera leafs (banor eller halvor) + areas.
	•	Custom → ange antal leafs, definiera ytor manuellt med coverage-editor.

9) Komponenter
	•	TopBar: välj plats, välj mall, spara/spara som ny.
	•	LeftSidebarPlacesTree: trädvy med CRUD.
	•	RightSidebarGroups: sökbar lista, drag-source.
	•	WeekGrid: 7 kolumner, bokningar + ghosts, drop-target, resize.
	•	BookingModal: create/edit, dag-checkar, konfliktlista.
	•	ToastUndo: undo/redo.

10) Services
	•	ConflictService: kolla overlap + coverage-intersect.
	•	ScheduleTemplateService: CRUD för mallar.
	•	PlaceService: CRUD + hjälp för Path och coverage.

11) CSS
	•	Inter font, 14–16px body, rubriker 600 weight.
	•	Färger: vit, ljusgrå (#f7f7f7/#ececec/#d9d9d9/#999), svart #111.
	•	Kalenderrutor med tunna linjer. Block med rundade hörn, lätt skugga. Ghosts ~0.35 opacity + streckad kant.

12) Acceptance-test (manuellt)
	1.	Välj Malmö IP → se veckoraster 07–21.
	2.	Dra F2011 till Halvplan A ons 18:00 → modal öppnas, dag-check Ons ikryssad, defaultlängd 90 min.
	3.	Kryssa Mån+Fre också, kolla live-konflikter.
	4.	Spara → block syns på Halv A, ghost på Helplan/A1/A2.
	5.	Lägg annan grupp på A1 samtidigt → konfliktvarning, kan ej spara.
	6.	Flytta block → tid uppdateras. Undo återställer.
	7.	Byt mall i topbar.
	8.	Health-endpoint visar providerinfo.
