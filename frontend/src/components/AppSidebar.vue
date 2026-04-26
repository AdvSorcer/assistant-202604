<script setup lang="ts">
import { DataAnalysis, Document, Expand, Fold, HomeFilled, Monitor, Notebook, Setting, Tickets } from '@element-plus/icons-vue'
import type { NavItem, NavSection, Vm, WikiPage } from '../types'

defineProps<{
  activeSection: NavSection
  collapsed: boolean
  favoriteVms: Vm[]
  pinnedWikiPages: WikiPage[]
}>()

const emit = defineEmits<{
  'update:activeSection': [value: NavSection]
  'update:collapsed': [value: boolean]
  viewVm: [item: Vm]
  viewWiki: [item: WikiPage]
}>()

const mainItems: NavItem[] = [
  { index: 'dashboard', label: '工作台', icon: HomeFilled },
  { index: 'logs', label: '日誌', icon: Notebook },
  { index: 'vms', label: 'VM 清單', icon: Monitor },
  { index: 'todos', label: '代辦清單', icon: Tickets },
  { index: 'wiki', label: 'Wiki 文件', icon: Document },
  { index: 'ai-weekly', label: 'AI 週報', icon: DataAnalysis },
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

    <div v-if="!collapsed && (favoriteVms.length || pinnedWikiPages.length)" class="sidebar-pins">
      <div v-if="favoriteVms.length" class="sidebar-pin-group">
        <div class="sidebar-pin-title">常用 VM</div>
        <button v-for="vm in favoriteVms.slice(0, 5)" :key="vm.id" class="sidebar-pin" type="button" @click="emit('viewVm', vm)">
          <span>{{ vm.name }}</span>
        </button>
      </div>

      <div v-if="pinnedWikiPages.length" class="sidebar-pin-group">
        <div class="sidebar-pin-title">置頂 Wiki</div>
        <button
          v-for="page in pinnedWikiPages.slice(0, 5)"
          :key="page.id"
          class="sidebar-pin"
          type="button"
          @click="emit('viewWiki', page)"
        >
          <span>{{ page.title }}</span>
        </button>
      </div>
    </div>

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
