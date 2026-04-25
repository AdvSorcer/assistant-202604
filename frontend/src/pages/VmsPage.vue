<script setup lang="ts">
import ListPager from '../components/ListPager.vue'
import type { Vm } from '../types'

defineProps<{
  items: Vm[]
  currentPage: number
  pageSize: number
  total: number
}>()

const emit = defineEmits<{
  create: []
  view: [item: Vm]
  edit: [item: Vm]
  delete: [item: Vm]
  'update:currentPage': [value: number]
  'update:pageSize': [value: number]
}>()
</script>

<template>
  <el-card shadow="never">
    <template #header>
      <div class="card-header">
        <span>VM 清單</span>
        <el-button type="primary" @click="emit('create')">新增 VM</el-button>
      </div>
    </template>
    <el-table :data="items" empty-text="尚未建立 VM 資料">
      <el-table-column prop="name" label="名稱" min-width="160" />
      <el-table-column prop="ipAddress" label="IP" width="160" />
      <el-table-column prop="hostname" label="Hostname" min-width="180" />
      <el-table-column label="帳號" min-width="180">
        <template #default="{ row }">
          <el-tag v-for="account in row.accounts" :key="account.id ?? account.username" class="tag-gap">
            {{ account.label }} / {{ account.username }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="網址" min-width="200">
        <template #default="{ row }">
          <el-link
            v-for="url in row.urls"
            :key="url.id ?? url.url"
            :href="url.url"
            target="_blank"
            class="link-item"
          >
            {{ url.label }}
          </el-link>
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
