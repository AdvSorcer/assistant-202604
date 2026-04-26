<script setup lang="ts">
import { MdEditor } from 'md-editor-v3'
import type { DailyLog } from '../types'

type LogForm = {
  date: string
  content: string
}

defineProps<{
  selectedLog: DailyLog | null
  selectedLogPreview: string
  editingLogId: number | null
  saving: boolean
}>()

const viewVisible = defineModel<boolean>('viewVisible', { required: true })
const dialogVisible = defineModel<boolean>('dialogVisible', { required: true })
const form = defineModel<LogForm>('form', { required: true })

const emit = defineEmits<{
  edit: [item: DailyLog]
  save: []
}>()
</script>

<template>
  <el-dialog v-model="viewVisible" title="查看日誌" width="820px">
    <div v-if="selectedLog" class="detail-panel">
      <el-descriptions :column="1" border>
        <el-descriptions-item label="日期">{{ selectedLog.date }}</el-descriptions-item>
      </el-descriptions>
      <div class="preview-title">日誌內容</div>
      <div class="markdown-preview view-preview" v-html="selectedLogPreview"></div>
    </div>
    <template #footer>
      <el-button @click="viewVisible = false">關閉</el-button>
      <el-button
        v-if="selectedLog"
        type="primary"
        @click="viewVisible = false; emit('edit', selectedLog)"
      >
        編輯
      </el-button>
    </template>
  </el-dialog>

  <el-dialog v-model="dialogVisible" :title="editingLogId ? '編輯日誌' : '新增日誌'" width="920px">
    <el-form label-position="top">
      <el-form-item label="日期" required>
        <el-date-picker v-model="form.date" type="date" value-format="YYYY-MM-DD" />
      </el-form-item>
      <el-form-item label="日誌內容" required>
        <MdEditor v-model="form.content" language="zh-TW" class="log-editor" />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="dialogVisible = false">取消</el-button>
      <el-button type="primary" :loading="saving" @click="emit('save')">儲存</el-button>
    </template>
  </el-dialog>
</template>
