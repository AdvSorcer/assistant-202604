<script setup lang="ts">
import { Document, Expand, Fold, Monitor, Notebook, Setting, Tickets } from '@element-plus/icons-vue'
import type { NavItem, NavSection } from '../types'

defineProps<{
  activeSection: NavSection
  collapsed: boolean
}>()

const emit = defineEmits<{
  'update:activeSection': [value: NavSection]
  'update:collapsed': [value: boolean]
}>()

const mainItems: NavItem[] = [
  { index: 'logs', label: '日誌', icon: Notebook },
  { index: 'vms', label: 'VM 清單', icon: Monitor },
  { index: 'todos', label: '代辦清單', icon: Tickets },
  { index: 'wiki', label: 'Wiki 文件', icon: Document },
]
</script>

<template>
  <el-aside :width="collapsed ? '72px' : '248px'" class="sidebar">
    <div class="brand">
      <div v-if="!collapsed" class="brand-text">
        <strong>Personal Assistant</strong>
        <span>個人工作助理</span>
      </div>
      <el-button
        text
        circle
        :title="collapsed ? '展開選單' : '收合選單'"
        @click="emit('update:collapsed', !collapsed)"
      >
        <el-icon>
          <Expand v-if="collapsed" />
          <Fold v-else />
        </el-icon>
      </el-button>
    </div>

    <el-menu
      :default-active="activeSection"
      :collapse="collapsed"
      class="nav-menu"
      @select="emit('update:activeSection', $event as NavSection)"
    >
      <el-menu-item v-for="item in mainItems" :key="item.index" :index="item.index">
        <el-icon><component :is="item.icon" /></el-icon>
        <span>{{ item.label }}</span>
      </el-menu-item>
    </el-menu>

    <el-menu
      :default-active="activeSection"
      :collapse="collapsed"
      class="nav-menu settings-menu"
      @select="emit('update:activeSection', $event as NavSection)"
    >
      <el-menu-item index="settings">
        <el-icon><Setting /></el-icon>
        <span>設定</span>
      </el-menu-item>
    </el-menu>
  </el-aside>
</template>
