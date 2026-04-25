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
  accounts: VmAccount[]
  urls: VmUrl[]
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

export type TodoStatusOption = {
  label: string
  value: TodoStatus
  type: 'info' | 'primary' | 'success' | 'warning'
}

export type NavSection = 'logs' | 'vms' | 'todos' | 'wiki' | 'settings'

export type NavItem = {
  index: NavSection
  label: string
  icon: Component
}
