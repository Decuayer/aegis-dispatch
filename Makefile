.PHONY: help up down restart logs status build test test-load lint format migrate run-api run-web run-mobile clean

# Default Target
.DEFAULT_GOAL := help

help: ## Display this help message
	@echo "=================================================================="
	@echo "  SOCAR Dispatch — Developer Automation & Task Runner"
	@echo "=================================================================="
	@awk 'BEGIN {FS = ":.*?## "} /^[a-zA-Z_-]+:.*?## / {printf "  \033[36m%-16s\033[0m %s\n", $$1, $$2}' $(MAKEFILE_LIST)

# -----------------------------------------------------------------------------
# 🐳 Infrastructure & Containers
# -----------------------------------------------------------------------------
up: ## Start PostgreSQL/PostGIS, Redis, and MinIO containers in background
	docker compose up -d

down: ## Stop and remove infrastructure containers
	docker compose down

restart: down up ## Restart all infrastructure containers

logs: ## Follow logs from all infrastructure containers
	docker compose logs -f

status: ## Show container health and port status
	docker compose ps

# -----------------------------------------------------------------------------
# 🔨 Build & Quality Checks
# -----------------------------------------------------------------------------
build: ## Build backend solution and fetch mobile dependencies
	cd backend && dotnet build SocarDispatch.slnx
	cd clients/mobile && flutter pub get

test: ## Run backend and Flutter test suites
	@echo "🧪 Running Backend Tests..."
	cd backend && dotnet test SocarDispatch.slnx --verbosity normal
	@echo "📱 Running Mobile Tests..."
	cd clients/mobile && flutter test

test-load: ## Run k6 emergency load and WebSocket stress tests
	@echo "🔥 Executing k6 Emergency Load Tests..."
	./load-testing/k6/run-load-tests.sh

lint: ## Run formatting check and Flutter analyzer
	@echo "🔍 Checking Backend Formatting..."
	cd backend && dotnet format --verify-no-changes
	@echo "🔍 Running Flutter Static Analysis..."
	cd clients/mobile && flutter analyze

format: ## Automatically fix backend code formatting
	cd backend && dotnet format

# -----------------------------------------------------------------------------
# 🗄️ Database & Migrations
# -----------------------------------------------------------------------------
migrate: ## Apply pending EF Core migrations to PostgreSQL
	cd backend && dotnet ef database update --project src/SocarDispatch.Infrastructure --startup-project src/SocarDispatch.API

# -----------------------------------------------------------------------------
# 🚀 Running Applications
# -----------------------------------------------------------------------------
run-api: ## Run .NET 8 Web API locally
	cd backend && dotnet run --project src/SocarDispatch.API

run-web: ## Run Blazor Web GIS Console locally
	cd clients/web/SocarDispatch.Web && dotnet run

run-mobile: ## Run Flutter Mobile application
	cd clients/mobile && flutter run

# -----------------------------------------------------------------------------
# 🧹 Clean
# -----------------------------------------------------------------------------
clean: ## Clean build artifacts and temporary files
	cd backend && dotnet clean SocarDispatch.slnx
	cd clients/mobile && flutter clean
