<script setup lang="ts">
import { ElMessage, ElMessageBox } from 'element-plus'
import MarkdownIt from 'markdown-it'
import { MdEditor } from 'md-editor-v3'
import 'md-editor-v3/lib/style.css'
import { computed, onMounted, ref, watch } from 'vue'
import AppSidebar from './components/AppSidebar.vue'
import AppTopbar from './components/AppTopbar.vue'
import ListToolbar from './components/ListToolbar.vue'
import LoginPanel from './components/LoginPanel.vue'
import VmDialogs from './components/VmDialogs.vue'
import WikiDialogs from './components/WikiDialogs.vue'
import { api, setAuthToken, toErrorMessage } from './lib/api'
import AiWeeklyReportPage from './pages/AiWeeklyReportPage.vue'
import DashboardPage from './pages/DashboardPage.vue'
import LogsPage from './pages/LogsPage.vue'
import SettingsPage from './pages/SettingsPage.vue'
import TodosPage from './pages/TodosPage.vue'
import VmsPage from './pages/VmsPage.vue'
import WikiPageList from './pages/WikiPageList.vue'
import type {
  AiSettings,
  AiSettingsForm,
  BackupImportPreview,
  BackupResponse,
  DailyLog,
  GlobalSearchResult,
  NavSection,
  TodoItem,
  TodoStatus,
  TodoStatusOption,
  Vm,
  VmAccount,
  VmUrl,
  WikiPage,
} from './types'

const markdown = new MarkdownIt({ breaks: true, linkify: true })
const authToken = ref(localStorage.getItem('assistant_token') ?? '')
const loginPassword = ref('')
const loginLoading = ref(false)
const isAuthenticated = ref(false)
const activeSection = ref<NavSection>('dashboard')
const sidebarCollapsed = ref(false)
const loading = ref(false)
const saving = ref(false)
const searchKeyword = ref('')
const globalSearchKeyword = ref('')
const todoStatusFilter = ref<TodoStatus | ''>('')
const currentPage = ref(1)
const pageSize = ref(10)
const backupFileInput = ref<HTMLInputElement | null>(null)
const selectedBackupFile = ref<File | null>(null)
const backupImportData = ref<BackupResponse | null>(null)
const backupImportPreview = ref<BackupImportPreview | null>(null)
const importingBackup = ref(false)
const aiSettings = ref<AiSettings | null>(null)
const aiSettingsForm = ref<AiSettingsForm>({ model: 'minimax/minimax-m2.7', apiKey: '' })
const savingAiSettings = ref(false)
const vms = ref<Vm[]>([])
const logs = ref<DailyLog[]>([])
const todos = ref<TodoItem[]>([])
const wikiPages = ref<WikiPage[]>([])

const vmDialogVisible = ref(false)
const logDialogVisible = ref(false)
const todoDialogVisible = ref(false)
const wikiDialogVisible = ref(false)
const vmViewVisible = ref(false)
const logViewVisible = ref(false)
const todoViewVisible = ref(false)
const wikiViewVisible = ref(false)
const editingVmId = ref<number | null>(null)
const editingLogId = ref<number | null>(null)
const editingTodoId = ref<number | null>(null)
const editingWikiId = ref<number | null>(null)
const selectedVm = ref<Vm | null>(null)
const selectedLog = ref<DailyLog | null>(null)
const selectedTodo = ref<TodoItem | null>(null)
const selectedWiki = ref<WikiPage | null>(null)

setAuthToken(authToken.value)
watch(authToken, setAuthToken)

const emptyVmForm = () => ({
  name: '',
  hostname: '',
  ipAddress: '',
  description: '',
  isFavorite: false,
  accounts: [{ label: 'default', username: '', password: '', notes: '' }] as VmAccount[],
  urls: [{ label: 'web', url: '' }] as VmUrl[],
})

const emptyLogForm = () => ({
  date: new Date().toISOString().slice(0, 10),
  content: '',
})

const emptyTodoForm = () => ({
  title: '',
  description: '',
  dueDate: '',
  status: 'Todo' as TodoStatus,
})

const emptyWikiForm = () => ({
  title: '',
  slug: '',
  content: '',
  isPinned: false,
})

const vmForm = ref(emptyVmForm())
const logForm = ref(emptyLogForm())
const todoForm = ref(emptyTodoForm())
const wikiForm = ref(emptyWikiForm())

const wikiPreview = computed(() => markdown.render(wikiForm.value.content || ''))
const selectedLogPreview = computed(() => markdown.render(selectedLog.value?.content || ''))
const selectedWikiPreview = computed(() => markdown.render(selectedWiki.value?.content || ''))
const filteredVms = computed(() => {
  const keyword = normalize(searchKeyword.value)
  if (!keyword) {
    return vms.value
  }

  return vms.value.filter((vm) =>
    includesKeyword([vm.name, vm.ipAddress, vm.hostname, vm.description], keyword) ||
    vm.accounts.some((account) => includesKeyword([account.label, account.username, account.notes], keyword)) ||
    vm.urls.some((url) => includesKeyword([url.label, url.url], keyword)),
  )
})

const filteredLogs = computed(() => {
  const keyword = normalize(searchKeyword.value)
  return keyword
    ? logs.value.filter((log) => includesKeyword([log.date, log.content], keyword))
    : logs.value
})

const filteredTodos = computed(() => {
  const keyword = normalize(searchKeyword.value)
  return todos.value.filter((todo) => {
    const matchesKeyword = keyword ? includesKeyword([todo.title, todo.description, todo.dueDate, todo.status], keyword) : true
    const matchesStatus = todoStatusFilter.value ? todo.status === todoStatusFilter.value : true
    return matchesKeyword && matchesStatus
  })
})

const filteredWikiPages = computed(() => {
  const keyword = normalize(searchKeyword.value)
  return keyword
    ? wikiPages.value.filter((page) => includesKeyword([page.title, page.slug, page.content], keyword))
    : wikiPages.value
})

const today = computed(() => new Date().toISOString().slice(0, 10))
const favoriteVms = computed(() => vms.value.filter((vm) => vm.isFavorite).slice(0, 8))
const pinnedWikiPages = computed(() => wikiPages.value.filter((page) => page.isPinned).slice(0, 8))
const todayLog = computed(() => logs.value.find((log) => log.date === today.value) ?? null)
const todayTodos = computed(() =>
  todos.value
    .filter((todo) => todo.status !== 'Done' && todo.status !== 'Archived' && (!todo.dueDate || todo.dueDate <= today.value))
    .slice(0, 6),
)
const recentLogs = computed(() => [...logs.value].sort((a, b) => b.date.localeCompare(a.date)).slice(0, 5))
const recentWikiPages = computed(() =>
  [...wikiPages.value]
    .sort((a, b) => {
      if (a.isPinned !== b.isPinned) return Number(b.isPinned) - Number(a.isPinned)
      return new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()
    })
    .slice(0, 5),
)
const globalSearchResults = computed<GlobalSearchResult[]>(() => {
  const keyword = normalize(globalSearchKeyword.value)
  if (!keyword) {
    return []
  }

  const results: GlobalSearchResult[] = []
  vms.value
    .filter((vm) =>
      includesKeyword([vm.name, vm.ipAddress, vm.hostname, vm.description], keyword) ||
      vm.accounts.some((account) => includesKeyword([account.label, account.username, account.notes], keyword)) ||
      vm.urls.some((url) => includesKeyword([url.label, url.url], keyword)),
    )
    .slice(0, 6)
    .forEach((vm) => results.push({
      id: vm.id,
      section: 'vms',
      typeLabel: 'VM',
      title: vm.name,
      description: vm.ipAddress || vm.hostname || vm.description,
    }))

  todos.value
    .filter((todo) => includesKeyword([todo.title, todo.description, todo.dueDate, todo.status], keyword))
    .slice(0, 6)
    .forEach((todo) => results.push({
      id: todo.id,
      section: 'todos',
      typeLabel: '代辦',
      title: todo.title,
      description: todo.dueDate || todo.description,
    }))

  logs.value
    .filter((log) => includesKeyword([log.date, log.content], keyword))
    .slice(0, 6)
    .forEach((log) => results.push({
      id: log.id,
      section: 'logs',
      typeLabel: '日誌',
      title: log.date,
      description: log.content,
    }))

  wikiPages.value
    .filter((page) => includesKeyword([page.title, page.slug, page.content], keyword))
    .slice(0, 6)
    .forEach((page) => results.push({
      id: page.id,
      section: 'wiki',
      typeLabel: 'Wiki',
      title: page.title,
      description: page.slug,
    }))

  return results.slice(0, 12)
})

const pagedVms = computed(() => paginate(filteredVms.value))
const pagedLogs = computed(() => paginate(filteredLogs.value))
const pagedTodos = computed(() => paginate(filteredTodos.value))
const pagedWikiPages = computed(() => paginate(filteredWikiPages.value))
const activeTotal = computed(() => {
  if (activeSection.value === 'logs') return filteredLogs.value.length
  if (activeSection.value === 'todos') return filteredTodos.value.length
  if (activeSection.value === 'wiki') return filteredWikiPages.value.length
  return filteredVms.value.length
})

const todoStatusOptions: TodoStatusOption[] = [
  { label: '待辦', value: 'Todo', type: 'info' },
  { label: '進行中', value: 'Doing', type: 'primary' },
  { label: '完成', value: 'Done', type: 'success' },
  { label: '封存', value: 'Archived', type: 'warning' },
]

watch([activeSection, searchKeyword, todoStatusFilter], () => {
  currentPage.value = 1
})

async function checkAuth() {
  if (!authToken.value) {
    isAuthenticated.value = false
    return
  }

  try {
    await api.get('/auth/me')
    isAuthenticated.value = true
    await loadDashboard()
  } catch {
    clearAuth()
  }
}

async function login() {
  if (!loginPassword.value) {
    ElMessage.warning('請輸入登入密碼')
    return
  }

  loginLoading.value = true
  try {
    const result = await api.post<{ token: string }>('/auth/login', { password: loginPassword.value })
    authToken.value = result.data.token
    setAuthToken(authToken.value)
    localStorage.setItem('assistant_token', authToken.value)
    isAuthenticated.value = true
    loginPassword.value = ''
    await loadDashboard()
  } catch {
    ElMessage.error('登入失敗，請確認密碼')
  } finally {
    loginLoading.value = false
  }
}

async function logout() {
  try {
    await api.post('/auth/logout')
  } finally {
    clearAuth()
  }
}

function clearAuth() {
  authToken.value = ''
  setAuthToken('')
  isAuthenticated.value = false
  localStorage.removeItem('assistant_token')
}

async function loadDashboard() {
  loading.value = true
  try {
    const [vmResult, logResult, todoResult, wikiResult, aiSettingsResult] = await Promise.all([
      api.get<Vm[]>('/vms'),
      api.get<DailyLog[]>('/logs'),
      api.get<TodoItem[]>('/todos'),
      api.get<WikiPage[]>('/wiki'),
      api.get<AiSettings>('/settings/ai'),
    ])

    vms.value = vmResult.data
    logs.value = logResult.data
    todos.value = todoResult.data
    wikiPages.value = wikiResult.data
    aiSettings.value = aiSettingsResult.data
    aiSettingsForm.value = { model: aiSettingsResult.data.model, apiKey: '' }
  } catch (error) {
    if ((error as { response?: { status?: number } }).response?.status === 401) {
      clearAuth()
      ElMessage.warning('登入已失效，請重新登入')
      return
    }

    ElMessage.error(toErrorMessage(error))
  } finally {
    loading.value = false
  }
}

async function saveAiSettings() {
  if (!aiSettingsForm.value.model.trim()) {
    ElMessage.warning('請輸入模型名稱')
    return
  }

  savingAiSettings.value = true
  try {
    const result = await api.put<AiSettings>('/settings/ai', {
      model: aiSettingsForm.value.model.trim(),
      apiKey: aiSettingsForm.value.apiKey.trim() || null,
    })
    aiSettings.value = result.data
    aiSettingsForm.value = { model: result.data.model, apiKey: '' }
    ElMessage.success('AI 設定已儲存')
  } catch (error) {
    ElMessage.error(toErrorMessage(error))
  } finally {
    savingAiSettings.value = false
  }
}

async function exportBackup() {
  try {
    const result = await api.get<BackupResponse>('/backup/export')
    const blob = new Blob([JSON.stringify(result.data, null, 2)], { type: 'application/json' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `assistant-backup-${new Date().toISOString().slice(0, 10)}.json`
    link.click()
    URL.revokeObjectURL(url)
  } catch (error) {
    ElMessage.error(toErrorMessage(error))
  }
}

function openBackupFilePicker() {
  backupFileInput.value?.click()
}

async function handleBackupFileChange(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''

  if (!file) {
    return
  }

  selectedBackupFile.value = file
  backupImportPreview.value = null
  backupImportData.value = null

  try {
    const content = await file.text()
    const parsed = JSON.parse(content) as BackupResponse
    backupImportData.value = parsed
    const result = await api.post<BackupImportPreview>('/backup/preview-import', parsed)
    backupImportPreview.value = result.data

    if (result.data.warnings.length > 0) {
      ElMessage.warning('備份檔有警告，請確認內容')
    } else {
      ElMessage.success('備份檔預覽完成')
    }
  } catch (error) {
    selectedBackupFile.value = null
    backupImportPreview.value = null
    backupImportData.value = null
    ElMessage.error(toErrorMessage(error))
  }
}

async function importBackup() {
  if (!backupImportData.value || !backupImportPreview.value) {
    ElMessage.warning('請先選擇備份檔並完成預覽')
    return
  }

  if (backupImportPreview.value.warnings.length > 0) {
    ElMessage.warning('備份檔仍有警告，無法還原')
    return
  }

  await ElMessageBox.confirm(
    '這會清空目前所有 VM、日誌、代辦與 Wiki 資料，並用備份檔覆蓋。確定繼續？',
    '覆蓋還原確認',
    {
      confirmButtonText: '覆蓋還原',
      cancelButtonText: '取消',
      type: 'warning',
    },
  )

  importingBackup.value = true
  try {
    await api.post('/backup/import', backupImportData.value)
    ElMessage.success('備份已還原')
    selectedBackupFile.value = null
    backupImportPreview.value = null
    backupImportData.value = null
    await loadDashboard()
  } catch (error) {
    ElMessage.error(toErrorMessage(error))
  } finally {
    importingBackup.value = false
  }
}

function openVmDialog(vm?: Vm) {
  editingVmId.value = vm?.id ?? null
  vmForm.value = vm
    ? {
        name: vm.name,
        hostname: vm.hostname ?? '',
        ipAddress: vm.ipAddress ?? '',
        description: vm.description ?? '',
        isFavorite: vm.isFavorite,
        accounts: vm.accounts.length ? vm.accounts.map((account) => ({ ...account })) : [],
        urls: vm.urls.length ? vm.urls.map((url) => ({ ...url })) : [],
      }
    : emptyVmForm()
  vmDialogVisible.value = true
}

function openVmView(vm: Vm) {
  selectedVm.value = vm
  vmViewVisible.value = true
}

async function saveVm() {
  if (!vmForm.value.name.trim()) {
    ElMessage.warning('請輸入 VM 名稱')
    return
  }

  const invalidUrl = vmForm.value.urls.find((url) => url.url && !isValidUrl(url.url))
  if (invalidUrl) {
    ElMessage.warning(`網址格式不正確：${invalidUrl.url}`)
    return
  }

  saving.value = true
  try {
    const payload = {
      ...vmForm.value,
      accounts: vmForm.value.accounts.filter((account) => account.label || account.username || account.password),
      urls: vmForm.value.urls.filter((url) => url.label || url.url),
    }

    if (editingVmId.value) {
      await api.put(`/vms/${editingVmId.value}`, payload)
    } else {
      await api.post('/vms', payload)
    }

    vmDialogVisible.value = false
    ElMessage.success('VM 已儲存')
    await loadDashboard()
  } catch (error) {
    ElMessage.error(toErrorMessage(error))
  } finally {
    saving.value = false
  }
}

function addVmAccount() {
  vmForm.value.accounts.push({ label: '', username: '', password: '', notes: '' })
}

function addVmUrl() {
  vmForm.value.urls.push({ label: '', url: '' })
}

async function deleteVm(vm: Vm) {
  await confirmDelete(`確定刪除 VM「${vm.name}」？`)
  await api.delete(`/vms/${vm.id}`)
  ElMessage.success('VM 已刪除')
  await loadDashboard()
}

async function toggleVmFavorite(vm: Vm) {
  const payload = {
    name: vm.name,
    hostname: vm.hostname ?? '',
    ipAddress: vm.ipAddress ?? '',
    description: vm.description ?? '',
    isFavorite: !vm.isFavorite,
    accounts: vm.accounts,
    urls: vm.urls,
  }

  try {
    await api.put(`/vms/${vm.id}`, payload)
    ElMessage.success(payload.isFavorite ? '已加入常用 VM' : '已取消常用 VM')
    await loadDashboard()
  } catch (error) {
    ElMessage.error(toErrorMessage(error))
  }
}

function openLogDialog(log?: DailyLog) {
  editingLogId.value = log?.id ?? null
  logForm.value = log ? { date: log.date, content: log.content } : emptyLogForm()
  logDialogVisible.value = true
}

function openLogView(log: DailyLog) {
  selectedLog.value = log
  logViewVisible.value = true
}

async function saveLog() {
  if (!logForm.value.date || !logForm.value.content.trim()) {
    ElMessage.warning('請輸入日期與日誌內容')
    return
  }

  saving.value = true
  try {
    if (editingLogId.value) {
      await api.put(`/logs/${editingLogId.value}`, logForm.value)
    } else {
      await api.post('/logs', logForm.value)
    }

    logDialogVisible.value = false
    ElMessage.success('日誌已儲存')
    await loadDashboard()
  } catch (error) {
    ElMessage.error(toErrorMessage(error))
  } finally {
    saving.value = false
  }
}

async function deleteLog(log: DailyLog) {
  await confirmDelete(`確定刪除 ${log.date} 的日誌？`)
  await api.delete(`/logs/${log.id}`)
  ElMessage.success('日誌已刪除')
  await loadDashboard()
}

function openTodoDialog(todo?: TodoItem) {
  editingTodoId.value = todo?.id ?? null
  todoForm.value = todo
    ? {
        title: todo.title,
        description: todo.description ?? '',
        dueDate: todo.dueDate ?? '',
        status: todo.status,
      }
    : emptyTodoForm()
  todoDialogVisible.value = true
}

function openTodoView(todo: TodoItem) {
  selectedTodo.value = todo
  todoViewVisible.value = true
}

async function saveTodo() {
  if (!todoForm.value.title.trim()) {
    ElMessage.warning('請輸入代辦事項')
    return
  }

  saving.value = true
  try {
    const payload = {
      ...todoForm.value,
      dueDate: todoForm.value.dueDate || null,
    }

    if (editingTodoId.value) {
      await api.put(`/todos/${editingTodoId.value}`, payload)
    } else {
      await api.post('/todos', payload)
    }

    todoDialogVisible.value = false
    ElMessage.success('代辦已儲存')
    await loadDashboard()
  } catch (error) {
    ElMessage.error(toErrorMessage(error))
  } finally {
    saving.value = false
  }
}

async function deleteTodo(todo: TodoItem) {
  await confirmDelete(`確定刪除代辦「${todo.title}」？`)
  await api.delete(`/todos/${todo.id}`)
  ElMessage.success('代辦已刪除')
  await loadDashboard()
}

function openWikiDialog(page?: WikiPage) {
  editingWikiId.value = page?.id ?? null
  wikiForm.value = page
    ? { title: page.title, slug: page.slug, content: page.content, isPinned: page.isPinned }
    : emptyWikiForm()
  wikiDialogVisible.value = true
}

function openWikiView(page: WikiPage) {
  selectedWiki.value = page
  wikiViewVisible.value = true
}

async function saveWiki() {
  if (!wikiForm.value.title.trim() || !wikiForm.value.slug.trim()) {
    ElMessage.warning('請輸入標題與 Slug')
    return
  }

  if (!/^[a-zA-Z0-9/_-]+$/.test(wikiForm.value.slug.trim())) {
    ElMessage.warning('Slug 只能使用英文、數字、斜線、底線或連字號')
    return
  }

  saving.value = true
  try {
    if (editingWikiId.value) {
      await api.put(`/wiki/${editingWikiId.value}`, wikiForm.value)
    } else {
      await api.post('/wiki', wikiForm.value)
    }

    wikiDialogVisible.value = false
    ElMessage.success('文件已儲存')
    await loadDashboard()
  } catch (error) {
    ElMessage.error(toErrorMessage(error))
  } finally {
    saving.value = false
  }
}

async function deleteWiki(page: WikiPage) {
  await confirmDelete(`確定刪除文件「${page.title}」？`)
  await api.delete(`/wiki/${page.id}`)
  ElMessage.success('文件已刪除')
  await loadDashboard()
}

async function toggleWikiPinned(page: WikiPage) {
  const payload = {
    title: page.title,
    slug: page.slug,
    content: page.content,
    isPinned: !page.isPinned,
  }

  try {
    await api.put(`/wiki/${page.id}`, payload)
    ElMessage.success(payload.isPinned ? '文件已置頂' : '文件已取消置頂')
    await loadDashboard()
  } catch (error) {
    ElMessage.error(toErrorMessage(error))
  }
}

async function confirmDelete(message: string) {
  await ElMessageBox.confirm(message, '刪除確認', {
    confirmButtonText: '刪除',
    cancelButtonText: '取消',
    type: 'warning',
  })
}

function openSection(section: Exclude<NavSection, 'dashboard' | 'ai-weekly' | 'settings'>) {
  activeSection.value = section
}

function selectSearchResult(result: GlobalSearchResult) {
  globalSearchKeyword.value = ''
  activeSection.value = result.section

  if (result.section === 'vms') {
    const vm = vms.value.find((item) => item.id === result.id)
    if (vm) openVmView(vm)
  } else if (result.section === 'todos') {
    const todo = todos.value.find((item) => item.id === result.id)
    if (todo) openTodoView(todo)
  } else if (result.section === 'logs') {
    const log = logs.value.find((item) => item.id === result.id)
    if (log) openLogView(log)
  } else if (result.section === 'wiki') {
    const page = wikiPages.value.find((item) => item.id === result.id)
    if (page) openWikiView(page)
  }
}

function todoStatusMeta(status: TodoStatus) {
  return todoStatusOptions.find((item) => item.value === status) ?? todoStatusOptions[0]
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat('zh-TW', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

function normalize(value?: string) {
  return (value ?? '').trim().toLowerCase()
}

function includesKeyword(values: Array<string | undefined>, keyword: string) {
  return values.some((value) => normalize(value).includes(keyword))
}

function paginate<T>(items: T[]) {
  const start = (currentPage.value - 1) * pageSize.value
  return items.slice(start, start + pageSize.value)
}

function isValidUrl(value: string) {
  try {
    const url = new URL(value)
    return url.protocol === 'http:' || url.protocol === 'https:'
  } catch {
    return false
  }
}

onMounted(checkAuth)
</script>

<template>
  <LoginPanel
    v-if="!isAuthenticated"
    v-model:password="loginPassword"
    :loading="loginLoading"
    @login="login"
  />

  <el-container v-else class="app-shell">
    <AppSidebar
      v-model:active-section="activeSection"
      v-model:collapsed="sidebarCollapsed"
      :favorite-vms="favoriteVms"
      :pinned-wiki-pages="pinnedWikiPages"
      @view-vm="openVmView"
      @view-wiki="openWikiView"
    />

    <el-container class="content-shell">
      <AppTopbar
        v-model:search-keyword="globalSearchKeyword"
        :search-results="globalSearchResults"
        @select-search-result="selectSearchResult"
        @logout="logout"
      />

      <el-main v-loading="loading" class="main">
        <ListToolbar
          v-if="activeSection !== 'dashboard' && activeSection !== 'ai-weekly' && activeSection !== 'settings'"
          v-model:search-keyword="searchKeyword"
          v-model:todo-status-filter="todoStatusFilter"
          :active-section="activeSection"
          :todo-status-options="todoStatusOptions"
        />

        <DashboardPage
          v-if="activeSection === 'dashboard'"
          :today="today"
          :today-log="todayLog"
          :today-todos="todayTodos"
          :recent-logs="recentLogs"
          :favorite-vms="favoriteVms"
          :recent-wiki-pages="recentWikiPages"
          :todo-status-options="todoStatusOptions"
          @open-section="openSection"
          @create-today-log="openLogDialog()"
          @edit-log="openLogDialog"
          @view-vm="openVmView"
          @view-log="openLogView"
          @view-todo="openTodoView"
          @view-wiki="openWikiView"
        />

        <VmsPage
          v-if="activeSection === 'vms'"
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :items="pagedVms"
          :total="activeTotal"
          @create="openVmDialog()"
          @view="openVmView"
          @edit="openVmDialog"
          @delete="deleteVm"
          @toggle-favorite="toggleVmFavorite"
        />

        <LogsPage
          v-if="activeSection === 'logs'"
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :items="pagedLogs"
          :total="activeTotal"
          @create="openLogDialog()"
          @view="openLogView"
          @edit="openLogDialog"
          @delete="deleteLog"
        />

        <TodosPage
          v-if="activeSection === 'todos'"
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :items="pagedTodos"
          :total="activeTotal"
          :todo-status-options="todoStatusOptions"
          @create="openTodoDialog()"
          @view="openTodoView"
          @edit="openTodoDialog"
          @delete="deleteTodo"
        />

        <WikiPageList
          v-if="activeSection === 'wiki'"
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :items="pagedWikiPages"
          :total="activeTotal"
          @create="openWikiDialog()"
          @view="openWikiView"
          @edit="openWikiDialog"
          @delete="deleteWiki"
          @toggle-pinned="toggleWikiPinned"
        />

        <AiWeeklyReportPage
          v-if="activeSection === 'ai-weekly'"
          :logs="logs"
          :ai-settings="aiSettings"
        />

        <SettingsPage
          v-if="activeSection === 'settings'"
          v-model:ai-settings-form="aiSettingsForm"
          :selected-backup-file="selectedBackupFile"
          :backup-import-preview="backupImportPreview"
          :importing-backup="importingBackup"
          :ai-settings="aiSettings"
          :saving-ai-settings="savingAiSettings"
          @export-backup="exportBackup"
          @choose-backup-file="openBackupFilePicker"
          @import-backup="importBackup"
          @save-ai-settings="saveAiSettings"
        />

        <input
          ref="backupFileInput"
          type="file"
          accept="application/json,.json"
          class="hidden-file-input"
          @change="handleBackupFileChange"
        />
      </el-main>
    </el-container>
  </el-container>

  <VmDialogs
    v-model:view-visible="vmViewVisible"
    v-model:dialog-visible="vmDialogVisible"
    v-model:form="vmForm"
    :selected-vm="selectedVm"
    :editing-vm-id="editingVmId"
    :saving="saving"
    @edit="openVmDialog"
    @save="saveVm"
    @add-account="addVmAccount"
    @add-url="addVmUrl"
  />

  <el-dialog v-model="logViewVisible" title="查看日誌" width="820px">
    <div v-if="selectedLog" class="detail-panel">
      <el-descriptions :column="1" border>
        <el-descriptions-item label="日期">{{ selectedLog.date }}</el-descriptions-item>
      </el-descriptions>
      <div class="preview-title">日誌內容</div>
      <div class="markdown-preview view-preview" v-html="selectedLogPreview"></div>
    </div>
    <template #footer>
      <el-button @click="logViewVisible = false">關閉</el-button>
      <el-button v-if="selectedLog" type="primary" @click="logViewVisible = false; openLogDialog(selectedLog)">編輯</el-button>
    </template>
  </el-dialog>

  <el-dialog v-model="todoViewVisible" title="查看代辦" width="620px">
    <div v-if="selectedTodo" class="detail-panel">
      <el-descriptions :column="1" border>
        <el-descriptions-item label="事項">{{ selectedTodo.title }}</el-descriptions-item>
        <el-descriptions-item label="日期">{{ selectedTodo.dueDate || '-' }}</el-descriptions-item>
        <el-descriptions-item label="狀態">
          <el-tag :type="todoStatusMeta(selectedTodo.status).type">{{ todoStatusMeta(selectedTodo.status).label }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="說明">{{ selectedTodo.description || '-' }}</el-descriptions-item>
      </el-descriptions>
    </div>
    <template #footer>
      <el-button @click="todoViewVisible = false">關閉</el-button>
      <el-button v-if="selectedTodo" type="primary" @click="todoViewVisible = false; openTodoDialog(selectedTodo)">編輯</el-button>
    </template>
  </el-dialog>

  <WikiDialogs
    v-model:view-visible="wikiViewVisible"
    v-model:dialog-visible="wikiDialogVisible"
    v-model:form="wikiForm"
    :selected-wiki="selectedWiki"
    :selected-wiki-preview="selectedWikiPreview"
    :wiki-preview="wikiPreview"
    :editing-wiki-id="editingWikiId"
    :saving="saving"
    :format-date-time="formatDateTime"
    @edit="openWikiDialog"
    @save="saveWiki"
  />

  <el-dialog v-model="logDialogVisible" :title="editingLogId ? '編輯日誌' : '新增日誌'" width="920px">
    <el-form label-position="top">
      <el-form-item label="日期" required>
        <el-date-picker v-model="logForm.date" type="date" value-format="YYYY-MM-DD" />
      </el-form-item>
      <el-form-item label="日誌內容" required>
        <MdEditor v-model="logForm.content" language="zh-TW" class="log-editor" />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="logDialogVisible = false">取消</el-button>
      <el-button type="primary" :loading="saving" @click="saveLog">儲存</el-button>
    </template>
  </el-dialog>

  <el-dialog v-model="todoDialogVisible" :title="editingTodoId ? '編輯代辦' : '新增代辦'" width="620px">
    <el-form label-position="top">
      <el-form-item label="事項" required>
        <el-input v-model="todoForm.title" />
      </el-form-item>
      <el-form-item label="說明">
        <el-input v-model="todoForm.description" type="textarea" :rows="4" />
      </el-form-item>
      <el-row :gutter="16">
        <el-col :span="12">
          <el-form-item label="日期">
            <el-date-picker v-model="todoForm.dueDate" type="date" value-format="YYYY-MM-DD" clearable />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="狀態">
            <el-select v-model="todoForm.status">
              <el-option
                v-for="option in todoStatusOptions"
                :key="option.value"
                :label="option.label"
                :value="option.value"
              />
            </el-select>
          </el-form-item>
        </el-col>
      </el-row>
    </el-form>
    <template #footer>
      <el-button @click="todoDialogVisible = false">取消</el-button>
      <el-button type="primary" :loading="saving" @click="saveTodo">儲存</el-button>
    </template>
  </el-dialog>

</template>
