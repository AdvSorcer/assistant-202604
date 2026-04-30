<script setup lang="ts">
import { Download } from '@element-plus/icons-vue'
import type { AiSettings, AiSettingsForm, BackupImportPreview, SecuritySettings, SecuritySettingsForm } from '../types'

defineProps<{
  selectedBackupFile: File | null
  backupImportPreview: BackupImportPreview | null
  importingBackup: boolean
  aiSettings: AiSettings | null
  savingAiSettings: boolean
  securitySettings: SecuritySettings | null
  savingSecuritySettings: boolean
}>()

const aiSettingsForm = defineModel<AiSettingsForm>('aiSettingsForm', { required: true })
const securitySettingsForm = defineModel<SecuritySettingsForm>('securitySettingsForm', { required: true })

const emit = defineEmits<{
  exportBackup: []
  chooseBackupFile: []
  importBackup: []
  saveAiSettings: []
  saveSecuritySettings: []
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
    <div class="settings-panel ai-settings-panel">
      <div>
        <h2>系統安全</h2>
        <p>設定登入密碼。資料加密金鑰會保存在資料目錄的 secret file，需要時可重新產生並重加密。</p>
      </div>
      <el-form class="ai-settings-form" label-position="top">
        <el-form-item label="登入密碼">
          <el-input
            v-model="securitySettingsForm.adminPassword"
            type="password"
            show-password
            placeholder="留空代表不更新"
          />
        </el-form-item>
        <el-form-item label="資料加密金鑰">
          <el-checkbox v-model="securitySettingsForm.rotateEncryptionKey">
            重新產生加密金鑰並重加密既有密碼資料
          </el-checkbox>
        </el-form-item>
        <div class="ai-settings-actions">
          <el-tag v-if="securitySettings?.hasAdminPassword" type="success">登入密碼已設定</el-tag>
          <el-tag v-else type="warning">登入密碼尚未設定</el-tag>
          <el-tag v-if="securitySettings?.hasEncryptionKey" type="success">加密金鑰已設定</el-tag>
          <el-tag v-else type="warning">加密金鑰尚未設定</el-tag>
          <el-button type="primary" :loading="savingSecuritySettings" @click="emit('saveSecuritySettings')">儲存安全設定</el-button>
        </div>
      </el-form>
    </div>
    <el-divider />
    <div class="settings-panel ai-settings-panel">
      <div>
        <h2>AI 週報</h2>
        <p>設定 OpenRouter API key 與模型名稱，供 AI 週報頁產生主管彙報內容。</p>
      </div>
      <el-form class="ai-settings-form" label-position="top">
        <el-form-item label="OpenRouter API key">
          <el-input
            v-model="aiSettingsForm.apiKey"
            type="password"
            show-password
            :placeholder="aiSettings?.hasApiKey ? '已設定，留空代表不更新' : '請輸入 API key'"
          />
        </el-form-item>
        <el-form-item label="模型名稱">
          <el-input v-model="aiSettingsForm.model" placeholder="minimax/minimax-m2.7" />
        </el-form-item>
        <div class="ai-settings-actions">
          <el-tag v-if="aiSettings?.hasApiKey" type="success">API key 已設定</el-tag>
          <el-tag v-else type="warning">尚未設定 API key</el-tag>
          <el-button type="primary" :loading="savingAiSettings" @click="emit('saveAiSettings')">儲存 AI 設定</el-button>
        </div>
      </el-form>
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
