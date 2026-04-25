<script setup lang="ts">
import { Download } from '@element-plus/icons-vue'
import type { BackupImportPreview } from '../types'

defineProps<{
  selectedBackupFile: File | null
  backupImportPreview: BackupImportPreview | null
  importingBackup: boolean
}>()

const emit = defineEmits<{
  exportBackup: []
  chooseBackupFile: []
  importBackup: []
}>()
</script>

<template>
  <el-card shadow="never">
    <template #header>
      <div class="card-header">
        <span>設定</span>
      </div>
    </template>
    <div class="settings-panel">
      <div>
        <h2>備份</h2>
        <p>匯出目前 SQLite 內的 VM、日誌、代辦與 Wiki 資料為 JSON 檔。</p>
      </div>
      <el-button type="primary" :icon="Download" @click="emit('exportBackup')">匯出備份</el-button>
    </div>
    <el-divider />
    <div class="settings-panel import-panel">
      <div>
        <h2>匯入還原</h2>
        <p>選擇先前匯出的 JSON 檔，預覽後可用覆蓋模式還原資料。</p>
      </div>
      <div class="backup-actions">
        <el-button @click="emit('chooseBackupFile')">選擇備份檔</el-button>
        <el-button
          type="danger"
          :disabled="!backupImportPreview || backupImportPreview.warnings.length > 0"
          :loading="importingBackup"
          @click="emit('importBackup')"
        >
          覆蓋還原
        </el-button>
      </div>
    </div>
    <el-alert
      v-if="selectedBackupFile"
      :closable="false"
      type="info"
      show-icon
      class="backup-preview"
    >
      <template #title>已選擇：{{ selectedBackupFile.name }}</template>
    </el-alert>
    <el-descriptions
      v-if="backupImportPreview"
      :column="4"
      border
      class="backup-preview"
    >
      <el-descriptions-item label="VM">{{ backupImportPreview.vms }}</el-descriptions-item>
      <el-descriptions-item label="日誌">{{ backupImportPreview.logs }}</el-descriptions-item>
      <el-descriptions-item label="代辦">{{ backupImportPreview.todos }}</el-descriptions-item>
      <el-descriptions-item label="Wiki">{{ backupImportPreview.wikiPages }}</el-descriptions-item>
    </el-descriptions>
    <el-alert
      v-if="backupImportPreview?.warnings.length"
      title="備份檔有問題，請修正後再匯入"
      type="warning"
      show-icon
      :closable="false"
      class="backup-preview"
    >
      <ul class="warning-list">
        <li v-for="warning in backupImportPreview.warnings" :key="warning">{{ warning }}</li>
      </ul>
    </el-alert>
  </el-card>
</template>
