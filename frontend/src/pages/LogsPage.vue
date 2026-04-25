<script setup lang="ts">
import ListPager from '../components/ListPager.vue'
import type { DailyLog } from '../types'

defineProps<{
  items: DailyLog[]
  currentPage: number
  pageSize: number
  total: number
}>()

const emit = defineEmits<{
  create: []
  view: [item: DailyLog]
  edit: [item: DailyLog]
  delete: [item: DailyLog]
  'update:currentPage': [value: number]
  'update:pageSize': [value: number]
}>()
</script>

<template>
  <el-card shadow="never">
    <template #header>
      <div class="card-header">
        <span>日誌</span>
        <el-button type="primary" @click="emit('create')">新增日誌</el-button>
      </div>
    </template>
    <el-table :data="items" empty-text="尚未建立日誌">
      <el-table-column prop="date" label="日期" width="160" />
      <el-table-column prop="content" label="日誌" show-overflow-tooltip />
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
