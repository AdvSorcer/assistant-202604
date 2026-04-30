# 個人助理系統

一個給個人工作使用的助理系統，用來集中管理 VM 連線資訊、每日工作日誌、代辦清單與 Markdown 文件。

## 技術選型

- 前端：Vue 3、Vite、TypeScript、Element Plus
- 後端：ASP.NET Core Web API、Minimal APIs
- 資料庫：SQLite、EF Core
- 部署：Docker Compose

## 目前功能

- VM 清單：管理名稱、IP、Hostname、多組帳號、多個對外網址，支援新增、編輯、刪除。
- 日誌：以日期與日誌內容為核心欄位，支援 Markdown 編輯與預覽。
- 代辦清單：支援有日期或無日期的待辦事項，可設定待辦、進行中、完成、封存。
- Wiki 文件：以 Markdown 內容作為文件儲存格式，支援編輯與預覽。
- VM 密碼：後端使用 AES-GCM 加密後存入 SQLite，加密金鑰存放在資料目錄的 secret file。
- 單使用者登入：保護資料 API，開發環境未設定時預設密碼為 `admin`。
- 搜尋、代辦狀態篩選、列表分頁與 JSON 備份匯出。

## 快速啟動

第一次啟動時，系統會自動在 SQLite 建立登入密碼 `admin` 的 hash，並在資料目錄建立一組 secret file 加密金鑰；登入後請到「設定」頁修改登入密碼。

啟動服務：

```bash
docker compose up --build
```

啟動後：

- 前端：[http://localhost:8080](http://localhost:8080)
- API：[http://localhost:5251/api](http://localhost:5251/api)
- OpenAPI：[http://localhost:5251/openapi/v1.json](http://localhost:5251/openapi/v1.json)

## 本機開發

後端：

```bash
dotnet run --project backend/Assistant.Api
```

前端：

```bash
npm install --prefix frontend
npm run dev --prefix frontend
```

本機前端預設呼叫 `http://localhost:5251/api`。

## 專案結構

```text
backend/Assistant.Api   ASP.NET Core Web API
frontend                Vue 3 + Element Plus
docker-compose.yml      Docker Compose 啟動設定
```
