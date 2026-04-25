<script setup lang="ts">
import type { NavSection, TodoStatus, TodoStatusOption } from '../types'

defineProps<{
  activeSection: NavSection
  searchKeyword: string
  todoStatusFilter: TodoStatus | ''
  todoStatusOptions: TodoStatusOption[]
}>()

const emit = defineEmits<{
  'update:searchKeyword': [value: string]
  'update:todoStatusFilter': [value: TodoStatus | '']
}>()
</script>

<template>
  <div v-if="activeSection !== 'dashboard' && activeSection !== 'settings'" class="list-toolbar">
    <el-input
      :model-value="searchKeyword"
      clearable
      placeholder="搜尋目前清單"
      @update:model-value="emit('update:searchKeyword', String($event))"
    />
    <el-select
      v-if="activeSection === 'todos'"
      :model-value="todoStatusFilter"
      clearable
      placeholder="狀態篩選"
      @update:model-value="emit('update:todoStatusFilter', ($event ?? '') as TodoStatus | '')"
    >
      <el-option
        v-for="option in todoStatusOptions"
        :key="option.value"
        :label="option.label"
        :value="option.value"
      />
    </el-select>
  </div>
</template>
