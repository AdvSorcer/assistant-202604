COMPOSE = docker compose
BASE = -f docker-compose.yml
DEV = $(BASE) -f docker-compose.dev.yml
PROD = $(BASE) -f docker-compose.prod.yml

.PHONY: dev dev-down dev-logs dev-ps dev-build prod prod-down prod-logs prod-ps prod-build

dev:
	$(COMPOSE) $(DEV) up --build

dev-down:
	$(COMPOSE) $(DEV) down

dev-logs:
	$(COMPOSE) $(DEV) logs -f

dev-ps:
	$(COMPOSE) $(DEV) ps

dev-build:
	$(COMPOSE) $(DEV) build

prod:
	$(COMPOSE) $(PROD) up -d --build

prod-down:
	$(COMPOSE) $(PROD) down

prod-logs:
	$(COMPOSE) $(PROD) logs -f

prod-ps:
	$(COMPOSE) $(PROD) ps

prod-build:
	$(COMPOSE) $(PROD) build
