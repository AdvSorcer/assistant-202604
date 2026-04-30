import type { Component } from 'vue'

export type VmAccount = {
  id?: number
  label: string
  username: string
  password?: string
  notes?: string
}

export type VmUrl = {
  id?: number
  label: string
  url: string
}

export type Vm = {
  id: number
  name: string
  ipAddress?: string
  hostname?: string
  description?: string
  isFavorite: boolean
  accounts: VmAccount[]
  urls: VmUrl[]
  updatedAt: string
}

export type DailyLog = {
  id: number
  date: string
  content: string
}

export type TodoStatus = 'Todo' | 'Doing' | 'Done' | 'Archived'

export type TodoItem = {
  id: number
  title: string
  description?: string
  dueDate?: string
  status: TodoStatus
}

export type WikiPage = {
  id: number
  title: string
  slug: string
  content: string
  isPinned: boolean
  updatedAt: string
}

export type BackupResponse = {
  exportedAt: string
  vms: Vm[]
  logs: DailyLog[]
  todos: TodoItem[]
  wikiPages: WikiPage[]
}

export type BackupImportPreview = {
  vms: number
  logs: number
  todos: number
  wikiPages: number
  warnings: string[]
}

export type AiSettings = {
  provider: string
  model: string
  hasApiKey: boolean
}

export type AiSettingsForm = {
  model: string
  apiKey: string
}

export type SecuritySettings = {
  hasAdminPassword: boolean
  hasEncryptionKey: boolean
}

export type SecuritySettingsForm = {
  adminPassword: string
  rotateEncryptionKey: boolean
}

export type AiWeeklyReportResponse = {
  report: string
  logsCount: number
  model: string
  startDate: string
  endDate: string
}

export type TodoStatusOption = {
  label: string
  value: TodoStatus
  type: 'info' | 'primary' | 'success' | 'warning'
}

export type NavSection = 'dashboard' | 'logs' | 'vms' | 'todos' | 'wiki' | 'ai-weekly' | 'settings'

export type NavItem = {
  index: NavSection
  label: string
  icon: Component
}

export type GlobalSearchResult = {
  id: number
  section: Exclude<NavSection, 'dashboard' | 'ai-weekly' | 'settings'>
  typeLabel: string
  title: string
  description?: string
}
