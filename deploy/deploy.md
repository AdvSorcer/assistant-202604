# Docker Compose 部署

## 1. 啟動

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

查看狀態：

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml ps
```

查看 log：

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml logs -f
```

第一次啟動時，系統會自動在 SQLite 建立登入密碼 `admin` 的 hash，並在資料目錄建立 secret file 加密金鑰。登入後請立即到「設定」頁修改登入密碼。

## 2. 檢查

用瀏覽器打開正式網址或主機 IP，例如：

```text
http://localhost
```

## 3. 更新

```bash
git pull
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

## 4. 停止

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml down
```

不要在正式環境使用：

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml down -v
```

`-v` 會刪除 volume，可能造成 SQLite 資料遺失。

## 5. 備份

SQLite 與 secret file 保存在 Docker volume：

```text
assistant-data:/app/data
```

正式環境至少要備份：

- SQLite 資料庫
- `secrets/encryption.key`
- 系統內建匯出的 JSON 備份

搬遷主機時，請保留整個 `assistant-data` volume；SQLite 與 `secrets/encryption.key` 要一起保留，舊密碼資料才解得開。
