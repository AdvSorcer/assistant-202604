<script setup lang="ts">
import { Search } from '@element-plus/icons-vue'
import type { GlobalSearchResult } from '../types'

defineProps<{
  modelValue: string
  results: GlobalSearchResult[]
}>()

const emit = defineEmits<{
  'update:modelValue': [value: string]
  select: [result: GlobalSearchResult]
}>()
</script>

<template>
  <div class="global-search">
    <el-popover
      :visible="modelValue.trim().length > 0"
      placement="bottom-start"
      trigger="click"
      width="420"
      popper-class="global-search-popover"
    >
      <template #reference>
        <el-input
          :model-value="modelValue"
          clearable
          placeholder="搜尋 VM、代辦、日誌、Wiki"
          @update:model-value="emit('update:modelValue', String($event))"
        >
          <template #prefix>
            <el-icon><Search /></el-icon>
          </template>
        </el-input>
      </template>

      <div class="global-search-results">
        <button
          v-for="result in results"
          :key="`${result.section}-${result.id}`"
          class="search-result"
          type="button"
          @click="emit('select', result)"
        >
          <span class="search-result-type">{{ result.typeLabel }}</span>
          <span class="search-result-title">{{ result.title }}</span>
          <span v-if="result.description" class="search-result-description">{{ result.description }}</span>
        </button>
        <div v-if="results.length === 0" class="search-empty">沒有符合的資料</div>
      </div>
    </el-popover>
  </div>
</template>
