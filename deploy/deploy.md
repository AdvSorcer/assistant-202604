# Docker Compose 部署

本專案正式環境先以 Docker Compose 部署為主。

## 1. 建立 `.env`

```bash
cp .env.example .env
openssl rand -base64 32
```

編輯 `.env`：

```text
Security__EncryptionKey=上一步產生的base64金鑰
Security__AdminPassword=正式環境登入密碼
```

`Security__EncryptionKey` 會用來加密 VM 密碼與 OpenRouter API key，正式環境建立後請妥善保存，不要任意更換。

## 2. 確認正式網址

如果正式環境不是使用預設的 `localhost`，請調整 `docker-compose.yml`：

```yaml
services:
  api:
    environment:
      Cors__AllowedOrigins__0: https://assistant.example.com

  frontend:
    build:
      args:
        VITE_API_BASE_URL: https://assistant.example.com/api
```

如果前面有 Nginx、Caddy 或 Traefik，讓外層反向代理把 `/` 指到 frontend container，把 `/api` 指到 api container。

## 3. 啟動

```bash
docker compose up -d --build
```

查看狀態：

```bash
docker compose ps
```

查看 log：

```bash
docker compose logs -f
```

## 4. 檢查

```bash
curl http://localhost:5251/api/health
```

再用瀏覽器確認：

```text
http://localhost:8080
```

若已設定正式網域，改用正式網址檢查。

## 5. 更新

```bash
git pull
docker compose up -d --build
```

## 6. 停止

```bash
docker compose down
```

不要在正式環境使用：

```bash
docker compose down -v
```

`-v` 會刪除 volume，可能造成 SQLite 資料遺失。

## 7. 備份

資料庫保存在 Docker volume：

```text
assistant-data:/app/data
```

正式環境至少要備份：

- SQLite 資料庫
- `.env`
- 系統內建匯出的 JSON 備份

搬遷主機時，SQLite 資料與 `.env` 裡的 `Security__EncryptionKey` 要一起保留。
