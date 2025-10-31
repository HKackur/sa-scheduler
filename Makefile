# -------------------------
# SchedulerMVP – Makefile
# -------------------------

# Använd: make dev | make run | make add-migration NAME=Init | make update | make reset-db | make health
# Obs: EF-kommandon kräver att du har dotnet-ef installerat:
# dotnet tool install --global dotnet-ef

# Standard-mål
.PHONY: dev run add-migration update reset-db health routes build run fly-login fly-launch fly-secrets fly-deploy

dev:
	dotnet watch --project SchedulerMVP/SchedulerMVP.csproj run

run:
	DOTNET_URLS="https://localhost:7054;http://localhost:5294" \
	dotnet run --project SchedulerMVP/SchedulerMVP.csproj -c Debug

# Skapa ny EF-migration: make add-migration NAME=MyChange
add-migration:
	@if [ -z "$(NAME)" ]; then echo "Ange migrationsnamn: make add-migration NAME=MyChange"; exit 1; fi
	dotnet ef migrations add $(NAME) --project SchedulerMVP --startup-project SchedulerMVP

# Applicera senaste migrationerna
update:
	dotnet ef database update --project SchedulerMVP --startup-project SchedulerMVP

# Rensa lokal SQLite-databas
reset-db:
	rm -f app.db

# Testa health-endpoint (prova både http och https standardportar)
health:
	-@curl -s http://localhost:5000/health || true
	-@echo
	-@curl -sk https://localhost:5001/health || true
	-@echo

# Lista rutter och varna för dubbletter
routes:
	@echo "\n== Blazor routes (SchedulerMVP/Components/Pages) =="
	@SRC=SchedulerMVP/Components/Pages; \
	if command -v rg >/dev/null 2>&1; then \
	  ROUTES=$$(rg -n -P '^@page\s+"([^"]+)"' $$SRC | sed -E 's#^(.+):[0-9]+:@page \"([^\"]+)\".*#\2\t\1#' | sort); \
	else \
	  ROUTES=$$(grep -R -n --include='*.razor' '^@page "' $$SRC 2>/dev/null | sed -E 's#^(.+):[0-9]+:@page \"([^\"]+)\".*#\2\t\1#' | sort); \
	fi; \
	if [ -z "$$ROUTES" ]; then echo "Inga rutter hittade."; exit 0; fi; \
	echo "$$ROUTES" | column -t -s $$'\t' || echo "$$ROUTES"; \
	DUPS=$$(echo "$$ROUTES" | awk -F'\t' '{print $$1}' | sort | uniq -d); \
	if [ -n "$$DUPS" ]; then \
	  echo "\nDubbletter upptäckta:"; \
	  echo "$$DUPS" | while read r; do \
	    echo "  $$r"; \
	    echo "$$ROUTES" | awk -v r="$$r" -F'\t' '$$1==r {print "    -> " $$2}'; \
	  done; \
	  exit 2; \
	else \
	  echo "\nInga dubbletter."; \
	fi

build:
	dotnet build SchedulerMVP/SchedulerMVP.csproj -c Release

fly-login:
	fly auth login

fly-launch:
	fly launch --no-deploy --copy-config --name sa-scheduler --noworkflows || true

# Usage: make fly-secrets POSTGRES_CONNECTION_STRING='Host=...;Port=5432;Database=...;Username=...;Password=...;Ssl Mode=Require;Trust Server Certificate=true'
fly-secrets:
	@if [ -z "$(POSTGRES_CONNECTION_STRING)" ]; then echo "POSTGRES_CONNECTION_STRING is required"; exit 1; fi
	fly secrets set ConnectionStrings__DefaultConnection="$(POSTGRES_CONNECTION_STRING)"

fly-deploy:
	fly deploy --build-only=false --detach