<script setup lang="ts">
import ListPager from '../components/ListPager.vue'
import type { TodoItem, TodoStatus, TodoStatusOption } from '../types'

defineProps<{
  items: TodoItem[]
  currentPage: number
  pageSize: number
  total: number
  todoStatusOptions: TodoStatusOption[]
}>()

const emit = defineEmits<{
  create: []
  view: [item: TodoItem]
  edit: [item: TodoItem]
  delete: [item: TodoItem]
  'update:currentPage': [value: number]
  'update:pageSize': [value: number]
}>()

function todoStatusMeta(status: TodoStatus, options: TodoStatusOption[]) {
  return options.find((item) => item.value === status) ?? options[0]
}
</script>

<template>
  <el-card shadow="never">
    <template #header>
      <div class="card-header">
        <span>代辦清單</span>
        <el-button type="primary" @click="emit('create')">新增代辦</el-button>
      </div>
    </template>
    <el-table :data="items" empty-text="尚未建立代辦">
      <el-table-column prop="title" label="事項" min-width="220" />
      <el-table-column prop="description" label="說明" min-width="220" show-overflow-tooltip />
      <el-table-column prop="dueDate" label="日期" width="160" />
      <el-table-column label="狀態" width="140">
        <template #default="{ row }">
          <el-tag :type="todoStatusMeta(row.status, todoStatusOptions).type">
            {{ todoStatusMeta(row.status, todoStatusOptions).label }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="操作" width="210" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" @click="emit('view', row)">查看</el-button>
          <el-button link type="primary" @click="emit('edit', row)">編輯</el-button>
          <el-button link type="danger" @click="emit('delete', row)">刪除</el-button>
        </template>
      </el-table-column>
    </el-table>
    <ListPager
      :current-page="currentPage"
      :page-size="pageSize"
      :total="total"
      @update:current-page="emit('update:currentPage', $event)"
      @update:page-size="emit('update:pageSize', $event)"
    />
  </el-card>
</template>
