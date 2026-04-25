# 專案架構快速說明

這個專案是個人工作助理系統，用來管理 VM 連線資料、日誌、代辦與 Wiki 文件。前端使用 Vue 3 + Element Plus，後端使用 ASP.NET Core Minimal API + SQLite，透過 Docker Compose 啟動。

## 技術總覽

- Frontend：Vue 3、Vite、TypeScript、Element Plus、md-editor-v3
- Backend：ASP.NET Core Minimal API、EF Core、SQLite
- Auth：單使用者密碼登入，後端記憶體 session token
- Secrets：VM 密碼使用 AES-GCM 加密後存入 SQLite
- Deploy：Docker Compose，SQLite DB 透過 Docker volume 保存

## 啟動方式

本機開發：

```bash
dotnet run --project backend/Assistant.Api
npm run dev --prefix frontend
```

本機開發網址：

- Frontend：[http://localhost:5173](http://localhost:5173)
- API：[http://localhost:5251/api](http://localhost:5251/api)

Docker Compose：

```bash
cp .env.example .env
openssl rand -base64 32
docker compose up --build
```

Docker Compose 網址：

- Frontend：[http://localhost:8080](http://localhost:8080)
- API：[http://localhost:5251/api](http://localhost:5251/api)

`.env` 重要設定：

```text
Security__EncryptionKey=base64-encoded-32-byte-key
Security__AdminPassword=change-me
```

開發環境若沒有設定 `Security__AdminPassword`，預設登入密碼是 `admin`。

## 後端架構

後端專案位於 `backend/Assistant.Api`。

目前採用 Minimal API + Feature Folder，不使用 Controller。`Program.cs` 只負責：

- 設定 DI
- 設定 SQLite
- 設定 JSON enum
- 設定 CORS
- 掛上 session auth middleware
- 掛載各 feature endpoints

主要進入點：

```text
backend/Assistant.Api/Program.cs
```

功能分組：

```text
Features/
  Auth/
    AuthEndpoints.cs
    AuthDtos.cs
    SessionStore.cs

  Vms/
    VmEndpoints.cs
    VmDtos.cs

  Logs/
    LogEndpoints.cs
    LogDtos.cs

  Todos/
    TodoEndpoints.cs
    TodoDtos.cs

  Wiki/
    WikiEndpoints.cs
    WikiDtos.cs

  Backup/
    BackupEndpoints.cs
    BackupDtos.cs
```

資料層：

```text
Data/
  AssistantDbContext.cs

Models/
  DomainModels.cs

Services/
  PasswordCipher.cs
```

### Endpoint 掛載方式

`Program.cs` 透過 extension methods 掛載 endpoint：

```csharp
var api = app.MapGroup("/api");

api.MapAuthEndpoints();
api.MapVmEndpoints();
api.MapLogEndpoints();
api.MapTodoEndpoints();
api.MapWikiEndpoints();
api.MapBackupEndpoints();
```

新增後端功能時，優先照這個模式建立：

```text
Features/{FeatureName}/{FeatureName}Endpoints.cs
Features/{FeatureName}/{FeatureName}Dtos.cs
```

不要把新 endpoint 直接塞回 `Program.cs`。

## 後端資料模型

Entity 定義在：

```text
backend/Assistant.Api/Models/DomainModels.cs
```

目前主要資料表：

- `ManagedVm`
- `VmAccount`
- `VmUrl`
- `DailyLog`
- `TodoItem`
- `WikiPage`

重要關係：

- 一台 VM 可以有多組 `VmAccount`
- 一台 VM 可以有多個 `VmUrl`
- `DailyLog.Date` 是唯一索引
- `WikiPage.Slug` 是唯一索引
- `TodoStatus` 以字串儲存在 SQLite

目前 DB 初始化使用：

```csharp
db.Database.EnsureCreated();
```

未來若 schema 常變，建議改成 EF Core migrations。

## 認證與安全

登入流程：

1. 前端呼叫 `POST /api/auth/login`
2. 後端驗證 `Security:AdminPassword` 或 `Security:AdminPasswordSha256`
3. 後端建立記憶體 session token
4. 前端把 token 存在 `localStorage`
5. 後續 API 使用 `Authorization: Bearer {token}`

開放端點：

- `/api/health`
- `/api/auth/*`

其他 `/api/*` 端點都會經過 `UseApiSessionAuth()` 檢查。

注意事項：

- `SessionStore` 是記憶體型，後端重啟後 token 會失效。
- 目前是單使用者設計，不是多帳號系統。
- 若要正式化，可改成 JWT、cookie auth，或 ASP.NET Core Identity。

VM 密碼加密：

- 實作位置：`Services/PasswordCipher.cs`
- 使用 AES-GCM
- 加密金鑰來自 `Security:EncryptionKey`
- 開發環境若沒設定金鑰會使用 development fallback
- 正式使用一定要設定 `Security__EncryptionKey`

## 備份與還原

備份功能位於：

```text
Features/Backup/
```

API：

- `GET /api/backup/export`
- `POST /api/backup/preview-import`
- `POST /api/backup/import`

目前還原策略是「覆蓋還原」：

1. 前端選擇 JSON 備份檔
2. 呼叫 preview API
3. 顯示 VM、日誌、代辦、Wiki 筆數
4. 若沒有 warnings，才允許覆蓋還原
5. import API 使用 transaction
6. 清空現有資料後重新匯入

匯入時不沿用備份檔裡的舊 ID，SQLite 會重新產生 ID。

## 前端架構

前端專案位於 `frontend`。

主要檔案：

```text
src/
  App.vue
  main.ts
  style.css
  types.ts

  lib/
    api.ts

  components/
    AppSidebar.vue
    AppTopbar.vue
    LoginPanel.vue
    ListToolbar.vue
    ListPager.vue

  pages/
    VmsPage.vue
    LogsPage.vue
    TodosPage.vue
    WikiPageList.vue
    SettingsPage.vue
```

`App.vue` 目前負責：

- 登入狀態
- 載入資料
- 搜尋、篩選、分頁狀態
- CRUD API 呼叫
- 新增/編輯/查看 dialogs
- 備份匯出與匯入

已拆出的元件負責：

- `LoginPanel.vue`：登入畫面
- `AppSidebar.vue`：左側選單與收合
- `AppTopbar.vue`：右上登出
- `ListToolbar.vue`：搜尋與代辦狀態篩選
- `ListPager.vue`：列表分頁
- `pages/*`：各模組列表頁
- `SettingsPage.vue`：備份匯出與匯入還原 UI

下一輪前端若要繼續拆，建議拆：

```text
components/dialogs/
  VmDialog.vue
  VmViewDialog.vue
  LogDialog.vue
  LogViewDialog.vue
  TodoDialog.vue
  TodoViewDialog.vue
  WikiDialog.vue
  WikiViewDialog.vue
```

## 前端資料流

登入後：

1. `App.vue` 從 `localStorage` 讀取 token
2. `setAuthToken()` 寫入 Axios default header
3. `checkAuth()` 呼叫 `/api/auth/me`
4. 成功後呼叫 `loadDashboard()`
5. 一次載入 VM、日誌、代辦、Wiki

資料目前是在前端做搜尋、篩選、分頁：

- VM 搜尋名稱、IP、hostname、備註、帳號、網址
- 日誌搜尋日期與內容
- 代辦搜尋標題、說明、日期、狀態
- Wiki 搜尋標題、slug、內容

如果資料量變大，建議改成後端分頁與 SQLite FTS5。

## UI 約定

- 左側選單固定顯示，可手動收合
- 上方區塊只保留登出按鈕
- 設定頁放備份/還原
- 日誌使用 `md-editor-v3`
- Wiki 目前是 textarea + Markdown preview
- VM 密碼在查看頁使用 Element Plus `show-password` 眼睛按鈕顯示

## 常見維護注意事項

### 新增 API

請放在對應 feature folder，不要直接改 `Program.cs`。

例如新增搜尋 API：

```text
Features/Search/
  SearchEndpoints.cs
  SearchDtos.cs
```

再於 `Program.cs` 掛載：

```csharp
api.MapSearchEndpoints();
```

### 新增前端頁面

建議新增：

```text
src/pages/{Feature}Page.vue
```

若有共用 UI，再放：

```text
src/components/
```

型別請優先放在：

```text
src/types.ts
```

API client 請用：

```text
src/lib/api.ts
```

### 修改密碼欄位

不要把 VM 密碼改成明文儲存。  
後端 entity 欄位是 `EncryptedPassword`，前端收到的是解密後 DTO 欄位 `password`。

### 修改資料表

目前使用 `EnsureCreated()`，不適合長期 schema migration。  
若開始頻繁改資料欄位，建議切換到 EF Core migrations。

### Docker 與本機 port

本機開發：

- Frontend：5173
- API：5251

Docker Compose：

- Frontend：8080
- API：5251

## 待辦建議

優先順序建議：

1. 將 `EnsureCreated()` 改成 EF Core migrations
2. 拆出 `App.vue` 裡的 dialogs
3. session 改成可跨重啟保存或正式 Identity/JWT
4. 增加自動化測試
5. 資料量變大後改成後端查詢與分頁，並評估 SQLite FTS5
6. JSON 匯入覆蓋還原後續可擴充成合併匯入

