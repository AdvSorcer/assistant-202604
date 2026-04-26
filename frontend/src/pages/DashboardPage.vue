<script setup lang="ts">
import type { DailyLog, TodoItem, TodoStatus, TodoStatusOption, Vm, WikiPage } from '../types'

defineProps<{
  today: string
  todayLog: DailyLog | null
  todayTodos: TodoItem[]
  recentLogs: DailyLog[]
  favoriteVms: Vm[]
  recentWikiPages: WikiPage[]
  todoStatusOptions: TodoStatusOption[]
}>()

const emit = defineEmits<{
  openSection: [section: 'vms' | 'logs' | 'todos' | 'wiki']
  createTodayLog: []
  editLog: [item: DailyLog]
  viewVm: [item: Vm]
  viewLog: [item: DailyLog]
  viewTodo: [item: TodoItem]
  viewWiki: [item: WikiPage]
}>()

function todoStatusMeta(status: TodoStatus, options: TodoStatusOption[]) {
  return options.find((item) => item.value === status) ?? options[0]
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat('zh-TW', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}
</script>

<template>
  <div class="dashboard-page">
    <section class="dashboard-head">
      <div>
        <h1>工作台</h1>
        <p>{{ new Intl.DateTimeFormat('zh-TW', { dateStyle: 'full' }).format(new Date()) }}</p>
      </div>
      <el-button type="primary" plain @click="emit('openSection', 'todos')">查看代辦</el-button>
    </section>

    <div class="dashboard-grid">
      <el-card shadow="never" class="dashboard-card today-log-card">
        <template #header>
          <div class="card-header">
            <span>今日日誌</span>
            <el-button link type="primary" @click="emit('openSection', 'logs')">全部</el-button>
          </div>
        </template>
        <div v-if="todayLog" class="today-log-content">
          <div>
            <div class="item-title">{{ todayLog.date }}</div>
            <p class="today-log-snippet">{{ todayLog.content }}</p>
          </div>
          <div class="today-log-actions">
            <el-button type="primary" plain @click="emit('viewLog', todayLog)">查看</el-button>
            <el-button type="primary" @click="emit('editLog', todayLog)">編輯</el-button>
          </div>
        </div>
        <div v-else class="today-log-content">
          <div>
            <div class="item-title">{{ today }}</div>
            <p class="today-log-snippet">今天還沒有日誌。</p>
          </div>
          <div class="today-log-actions">
            <el-button type="primary" @click="emit('createTodayLog')">建立今日日誌</el-button>
          </div>
        </div>
      </el-card>

      <el-card shadow="never" class="dashboard-card">
        <template #header>
          <div class="card-header">
            <span>今天待辦</span>
            <el-button link type="primary" @click="emit('openSection', 'todos')">全部</el-button>
          </div>
        </template>
        <div v-if="todayTodos.length" class="dashboard-list">
          <button v-for="todo in todayTodos" :key="todo.id" class="dashboard-list-item" type="button" @click="emit('viewTodo', todo)">
            <span class="item-title">{{ todo.title }}</span>
            <el-tag size="small" :type="todoStatusMeta(todo.status, todoStatusOptions).type">
              {{ todoStatusMeta(todo.status, todoStatusOptions).label }}
            </el-tag>
          </button>
        </div>
        <el-empty v-else description="今天沒有待辦" :image-size="72" />
      </el-card>

      <el-card shadow="never" class="dashboard-card">
        <template #header>
          <div class="card-header">
            <span>常用 VM</span>
            <el-button link type="primary" @click="emit('openSection', 'vms')">全部</el-button>
          </div>
        </template>
        <div v-if="favoriteVms.length" class="dashboard-list">
          <button v-for="vm in favoriteVms" :key="vm.id" class="dashboard-list-item" type="button" @click="emit('viewVm', vm)">
            <span class="item-title">{{ vm.name }}</span>
            <span class="item-meta">{{ vm.ipAddress || vm.hostname || '-' }}</span>
          </button>
        </div>
        <el-empty v-else description="尚未標記常用 VM" :image-size="72" />
      </el-card>

      <el-card shadow="never" class="dashboard-card">
        <template #header>
          <div class="card-header">
            <span>最近日誌</span>
            <el-button link type="primary" @click="emit('openSection', 'logs')">全部</el-button>
          </div>
        </template>
        <div v-if="recentLogs.length" class="dashboard-list">
          <button v-for="log in recentLogs" :key="log.id" class="dashboard-list-item" type="button" @click="emit('viewLog', log)">
            <span class="item-title">{{ log.date }}</span>
            <span class="item-meta">{{ log.content }}</span>
          </button>
        </div>
        <el-empty v-else description="尚未建立日誌" :image-size="72" />
      </el-card>

      <el-card shadow="never" class="dashboard-card">
        <template #header>
          <div class="card-header">
            <span>最近更新 Wiki</span>
            <el-button link type="primary" @click="emit('openSection', 'wiki')">全部</el-button>
          </div>
        </template>
        <div v-if="recentWikiPages.length" class="dashboard-list">
          <button v-for="page in recentWikiPages" :key="page.id" class="dashboard-list-item" type="button" @click="emit('viewWiki', page)">
            <span class="item-title">
              <el-tag v-if="page.isPinned" size="small" type="warning">置頂</el-tag>
              {{ page.title }}
            </span>
            <span class="item-meta">{{ formatDateTime(page.updatedAt) }}</span>
          </button>
        </div>
        <el-empty v-else description="尚未建立 Wiki" :image-size="72" />
      </el-card>
    </div>
  </div>
</template>
