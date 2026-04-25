<script setup lang="ts">
import type { WikiPage } from '../types'

type WikiForm = {
  title: string
  slug: string
  content: string
  isPinned: boolean
}

defineProps<{
  selectedWiki: WikiPage | null
  selectedWikiPreview: string
  wikiPreview: string
  editingWikiId: number | null
  saving: boolean
  formatDateTime: (value: string) => string
}>()

const viewVisible = defineModel<boolean>('viewVisible', { required: true })
const dialogVisible = defineModel<boolean>('dialogVisible', { required: true })
const form = defineModel<WikiForm>('form', { required: true })

const emit = defineEmits<{
  edit: [item: WikiPage]
  save: []
}>()
</script>

<template>
  <el-dialog v-model="viewVisible" title="查看文件" width="860px">
    <div v-if="selectedWiki" class="detail-panel">
      <el-descriptions :column="2" border>
        <el-descriptions-item label="標題">{{ selectedWiki.title }}</el-descriptions-item>
        <el-descriptions-item label="Slug">{{ selectedWiki.slug }}</el-descriptions-item>
        <el-descriptions-item label="置頂">{{ selectedWiki.isPinned ? '是' : '否' }}</el-descriptions-item>
        <el-descriptions-item label="更新時間">{{ formatDateTime(selectedWiki.updatedAt) }}</el-descriptions-item>
      </el-descriptions>
      <div class="preview-title">文件內容</div>
      <div class="markdown-preview view-preview" v-html="selectedWikiPreview"></div>
    </div>
    <template #footer>
      <el-button @click="viewVisible = false">關閉</el-button>
      <el-button
        v-if="selectedWiki"
        type="primary"
        @click="viewVisible = false; emit('edit', selectedWiki)"
      >
        編輯
      </el-button>
    </template>
  </el-dialog>

  <el-dialog v-model="dialogVisible" :title="editingWikiId ? '編輯文件' : '新增文件'" width="960px">
    <el-form label-position="top">
      <el-row :gutter="16">
        <el-col :span="12">
          <el-form-item label="標題" required>
            <el-input v-model="form.title" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="Slug" required>
            <el-input v-model="form.slug" placeholder="work/deploy-note" />
          </el-form-item>
        </el-col>
      </el-row>
      <el-form-item>
        <el-checkbox v-model="form.isPinned">置頂文件</el-checkbox>
      </el-form-item>
      <el-row :gutter="16">
        <el-col :span="12">
          <el-form-item label="Markdown">
            <el-input v-model="form.content" type="textarea" :rows="16" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <div class="preview-title">預覽</div>
          <div class="markdown-preview" v-html="wikiPreview"></div>
        </el-col>
      </el-row>
    </el-form>
    <template #footer>
      <el-button @click="dialogVisible = false">取消</el-button>
      <el-button type="primary" :loading="saving" @click="emit('save')">儲存</el-button>
    </template>
  </el-dialog>
</template>
