<script setup lang="ts">
import { CopyDocument, MagicStick } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { computed, ref } from 'vue'
import { api, toErrorMessage } from '../lib/api'
import { copyToClipboard } from '../lib/clipboard'
import type { AiSettings, AiWeeklyReportResponse, DailyLog } from '../types'

const props = defineProps<{
  logs: DailyLog[]
  aiSettings: AiSettings | null
}>()

const generating = ref(false)
const report = ref('')
const usedModel = ref('')
const dateRange = ref<[string, string]>(getDefaultWeekRange())

const selectedLogs = computed(() => {
  const [startDate, endDate] = dateRange.value
  return props.logs
    .filter((log) => log.date >= startDate && log.date <= endDate)
    .sort((a, b) => a.date.localeCompare(b.date))
})

const canGenerate = computed(() => Boolean(dateRange.value?.[0] && dateRange.value?.[1] && props.aiSettings?.hasApiKey))

async function generateReport() {
  const [startDate, endDate] = dateRange.value
  if (!startDate || !endDate) {
    ElMessage.warning('請選擇週報日期範圍')
    return
  }

  if (!props.aiSettings?.hasApiKey) {
    ElMessage.warning('請先到設定頁填寫 OpenRouter API key')
    return
  }

  generating.value = true
  try {
    const result = await api.post<AiWeeklyReportResponse>('/ai-weekly-report/generate', { startDate, endDate })
    report.value = result.data.report
    usedModel.value = result.data.model
    ElMessage.success(`已產生週報，包含 ${result.data.logsCount} 則日誌`)
  } catch (error) {
    ElMessage.error(toErrorMessage(error))
  } finally {
    generating.value = false
  }
}

async function copyReport() {
  if (!report.value) {
    return
  }

  try {
    await copyToClipboard(report.value)
    ElMessage.success('週報已複製')
  } catch {
    ElMessage.error('無法複製到剪貼簿')
  }
}

function getDefaultWeekRange(): [string, string] {
  const now = new Date()
  const day = now.getDay()
  const mondayOffset = day === 0 ? -6 : 1 - day
  const monday = addDays(now, mondayOffset)
  const friday = addDays(monday, 4)
  return [toDateInputValue(monday), toDateInputValue(friday)]
}

function addDays(value: Date, days: number) {
  const date = new Date(value)
  date.setDate(date.getDate() + days)
  return date
}

function toDateInputValue(value: Date) {
  const year = value.getFullYear()
  const month = `${value.getMonth() + 1}`.padStart(2, '0')
  const date = `${value.getDate()}`.padStart(2, '0')
  return `${year}-${month}-${date}`
}
</script>

<template>
  <el-card shadow="never" class="ai-weekly-page">
    <template #header>
      <div class="card-header">
        <span>AI 週報</span>
        <el-button
          type="primary"
          :icon="MagicStick"
          :disabled="!canGenerate || selectedLogs.length === 0"
          :loading="generating"
          @click="generateReport"
        >
          產生週報
        </el-button>
      </div>
    </template>

    <div class="weekly-controls">
      <el-date-picker
        v-model="dateRange"
        type="daterange"
        range-separator="至"
        start-placeholder="開始日期"
        end-placeholder="結束日期"
        value-format="YYYY-MM-DD"
      />
      <div class="weekly-meta">
        <el-tag>{{ selectedLogs.length }} 則日誌</el-tag>
        <el-tag :type="aiSettings?.hasApiKey ? 'success' : 'warning'">
          {{ aiSettings?.hasApiKey ? `模型：${aiSettings.model}` : '尚未設定 API key' }}
        </el-tag>
      </div>
    </div>

    <el-alert
      v-if="!aiSettings?.hasApiKey"
      title="請先到設定頁填寫 OpenRouter API key 與模型名稱"
      type="warning"
      show-icon
      :closable="false"
      class="weekly-alert"
    />

    <div class="weekly-grid">
      <section class="weekly-log-list">
        <div class="section-title">選取範圍日誌</div>
        <el-empty v-if="selectedLogs.length === 0" description="這個範圍內沒有日誌" :image-size="72" />
        <div v-else class="weekly-log-items">
          <article v-for="log in selectedLogs" :key="log.id" class="weekly-log-item">
            <strong>{{ log.date }}</strong>
            <p>{{ log.content }}</p>
          </article>
        </div>
      </section>

      <section class="weekly-report-panel">
        <div class="section-title">
          <span>週報輸出</span>
          <div class="weekly-report-actions">
            <el-tag v-if="usedModel" type="info">{{ usedModel }}</el-tag>
            <el-button :icon="CopyDocument" :disabled="!report" @click="copyReport">複製</el-button>
          </div>
        </div>
        <el-input
          v-model="report"
          type="textarea"
          :rows="18"
          resize="vertical"
          placeholder="產生後會顯示可直接複製到 Outlook 的週報文字"
        />
      </section>
    </div>
  </el-card>
</template>
