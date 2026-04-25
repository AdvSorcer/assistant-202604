<script setup lang="ts">
import ListPager from '../components/ListPager.vue'
import type { WikiPage } from '../types'

defineProps<{
  items: WikiPage[]
  currentPage: number
  pageSize: number
  total: number
}>()

const emit = defineEmits<{
  create: []
  view: [item: WikiPage]
  edit: [item: WikiPage]
  delete: [item: WikiPage]
  'update:currentPage': [value: number]
  'update:pageSize': [value: number]
}>()

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat('zh-TW', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}
</script>

<template>
  <el-card shadow="never">
    <template #header>
      <div class="card-header">
        <span>Wiki 文件</span>
        <el-button type="primary" @click="emit('create')">新增文件</el-button>
      </div>
    </template>
    <el-table :data="items" empty-text="尚未建立文件">
      <el-table-column prop="title" label="標題" min-width="220" />
      <el-table-column prop="slug" label="Slug" min-width="200" />
      <el-table-column label="更新時間" width="220">
        <template #default="{ row }">{{ formatDateTime(row.updatedAt) }}</template>
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
