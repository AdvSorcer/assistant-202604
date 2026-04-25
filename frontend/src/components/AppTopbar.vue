<script setup lang="ts">
import GlobalSearch from './GlobalSearch.vue'
import type { GlobalSearchResult } from '../types'

defineProps<{
  searchKeyword: string
  searchResults: GlobalSearchResult[]
}>()

const emit = defineEmits<{
  logout: []
  'update:searchKeyword': [value: string]
  selectSearchResult: [value: GlobalSearchResult]
}>()
</script>

<template>
  <el-header class="topbar">
    <div class="topbar-actions">
      <GlobalSearch
        :model-value="searchKeyword"
        :results="searchResults"
        @update:model-value="emit('update:searchKeyword', $event)"
        @select="emit('selectSearchResult', $event)"
      />
      <el-button type="primary" plain @click="emit('logout')">登出</el-button>
    </div>
  </el-header>
</template>
