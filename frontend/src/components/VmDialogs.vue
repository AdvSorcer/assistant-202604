<script setup lang="ts">
import { Connection, CopyDocument, Link, Monitor } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { copyToClipboard } from '../lib/clipboard'
import type { Vm, VmAccount, VmUrl } from '../types'

type VmForm = {
  name: string
  hostname: string
  ipAddress: string
  description: string
  isFavorite: boolean
  accounts: VmAccount[]
  urls: VmUrl[]
}

defineProps<{
  selectedVm: Vm | null
  editingVmId: number | null
  saving: boolean
}>()

const viewVisible = defineModel<boolean>('viewVisible', { required: true })
const dialogVisible = defineModel<boolean>('dialogVisible', { required: true })
const form = defineModel<VmForm>('form', { required: true })

const emit = defineEmits<{
  edit: [item: Vm]
  save: []
  addAccount: []
  addUrl: []
}>()

async function copyText(value: string | undefined, label: string) {
  const text = value?.trim()
  if (!text) {
    ElMessage.warning(`${label} 沒有可複製的內容`)
    return
  }

  try {
    await copyToClipboard(text)
    ElMessage.success(`${label} 已複製`)
  } catch {
    ElMessage.error('無法複製到剪貼簿')
  }
}

function vmHost(vm: Vm) {
  return vm.ipAddress?.trim() || vm.hostname?.trim() || ''
}

function sshCommand(vm: Vm, account?: VmAccount) {
  const host = vmHost(vm)
  if (!host) {
    return ''
  }

  const username = account?.username?.trim()
  return username ? `ssh ${username}@${host}` : `ssh ${host}`
}

function rdpCommand(vm: Vm) {
  const host = vmHost(vm)
  return host ? `mstsc /v:${host}` : ''
}
</script>

<template>
  <el-dialog v-model="viewVisible" title="查看 VM" width="940px">
    <div v-if="selectedVm" class="detail-panel">
      <el-descriptions :column="2" border>
        <el-descriptions-item label="名稱">{{ selectedVm.name }}</el-descriptions-item>
        <el-descriptions-item label="IP">
          <span class="copyable-value">
            <span>{{ selectedVm.ipAddress || '-' }}</span>
            <el-button
              v-if="selectedVm.ipAddress"
              :icon="CopyDocument"
              circle
              size="small"
              title="複製 IP"
              @click="copyText(selectedVm.ipAddress, 'IP')"
            />
          </span>
        </el-descriptions-item>
        <el-descriptions-item label="Hostname">
          <span class="copyable-value">
            <span>{{ selectedVm.hostname || '-' }}</span>
            <el-button
              v-if="selectedVm.hostname"
              :icon="CopyDocument"
              circle
              size="small"
              title="複製 Hostname"
              @click="copyText(selectedVm.hostname, 'Hostname')"
            />
          </span>
        </el-descriptions-item>
        <el-descriptions-item label="常用">{{ selectedVm.isFavorite ? '是' : '否' }}</el-descriptions-item>
        <el-descriptions-item label="備註">{{ selectedVm.description || '-' }}</el-descriptions-item>
      </el-descriptions>

      <div class="quick-actions">
        <el-button
          :icon="CopyDocument"
          :disabled="!selectedVm.ipAddress"
          @click="copyText(selectedVm.ipAddress, 'IP')"
        >
          複製 IP
        </el-button>
        <el-button
          :icon="CopyDocument"
          :disabled="!selectedVm.hostname"
          @click="copyText(selectedVm.hostname, 'Hostname')"
        >
          複製 Hostname
        </el-button>
        <el-button
          :icon="Connection"
          :disabled="!vmHost(selectedVm)"
          @click="copyText(sshCommand(selectedVm), 'SSH 指令')"
        >
          複製 SSH
        </el-button>
        <el-button
          :icon="Monitor"
          :disabled="!vmHost(selectedVm)"
          @click="copyText(rdpCommand(selectedVm), 'RDP 指令')"
        >
          複製 RDP
        </el-button>
      </div>

      <div class="section-title">帳號</div>
      <el-table :data="selectedVm.accounts" empty-text="沒有帳號資料" size="small">
        <el-table-column prop="label" label="標籤" width="140" />
        <el-table-column prop="username" label="帳號" width="180" />
        <el-table-column label="密碼" min-width="180">
          <template #default="{ row }">
            <el-input :model-value="row.password || ''" readonly show-password />
          </template>
        </el-table-column>
        <el-table-column prop="notes" label="備註" min-width="160" />
        <el-table-column label="快速操作" width="250" fixed="right">
          <template #default="{ row }">
            <div class="table-actions">
              <el-button
                :icon="CopyDocument"
                link
                type="primary"
                @click="copyText(row.username, '帳號')"
              >
                帳號
              </el-button>
              <el-button
                :icon="CopyDocument"
                link
                type="primary"
                @click="copyText(row.password, '密碼')"
              >
                密碼
              </el-button>
              <el-button
                :icon="Connection"
                link
                type="primary"
                :disabled="!vmHost(selectedVm)"
                @click="copyText(sshCommand(selectedVm, row), 'SSH 指令')"
              >
                SSH
              </el-button>
            </div>
          </template>
        </el-table-column>
      </el-table>

      <div class="section-title">對外網址</div>
      <el-table :data="selectedVm.urls" empty-text="沒有網址資料" size="small">
        <el-table-column prop="label" label="標籤" width="160" />
        <el-table-column label="網址" min-width="260">
          <template #default="{ row }">
            <el-link :href="row.url" target="_blank">{{ row.url }}</el-link>
          </template>
        </el-table-column>
        <el-table-column label="快速操作" width="180" fixed="right">
          <template #default="{ row }">
            <div class="table-actions">
              <el-button :icon="Link" link type="primary" tag="a" :href="row.url" target="_blank">
                開啟
              </el-button>
              <el-button :icon="CopyDocument" link type="primary" @click="copyText(row.url, '網址')">
                複製
              </el-button>
            </div>
          </template>
        </el-table-column>
      </el-table>
    </div>
    <template #footer>
      <el-button @click="viewVisible = false">關閉</el-button>
      <el-button
        v-if="selectedVm"
        type="primary"
        @click="viewVisible = false; emit('edit', selectedVm)"
      >
        編輯
      </el-button>
    </template>
  </el-dialog>

  <el-dialog v-model="dialogVisible" :title="editingVmId ? '編輯 VM' : '新增 VM'" width="860px">
    <el-form label-position="top">
      <el-row :gutter="16">
        <el-col :span="12">
          <el-form-item label="名稱" required>
            <el-input v-model="form.name" placeholder="例：prod-app-01" />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="IP">
            <el-input v-model="form.ipAddress" placeholder="例：10.0.0.10" />
          </el-form-item>
        </el-col>
      </el-row>
      <el-form-item label="Hostname">
        <el-input v-model="form.hostname" />
      </el-form-item>
      <el-form-item label="備註">
        <el-input v-model="form.description" type="textarea" :rows="2" />
      </el-form-item>
      <el-form-item>
        <el-checkbox v-model="form.isFavorite">常用 VM</el-checkbox>
      </el-form-item>

      <div class="section-title">
        <span>帳號</span>
        <el-button size="small" @click="emit('addAccount')">新增帳號</el-button>
      </div>
      <div v-for="(account, index) in form.accounts" :key="index" class="nested-row">
        <el-input v-model="account.label" placeholder="標籤" />
        <el-input v-model="account.username" placeholder="帳號" />
        <el-input v-model="account.password" placeholder="密碼" show-password />
        <el-button type="danger" plain @click="form.accounts.splice(index, 1)">移除</el-button>
      </div>

      <div class="section-title">
        <span>對外網址</span>
        <el-button size="small" @click="emit('addUrl')">新增網址</el-button>
      </div>
      <div v-for="(url, index) in form.urls" :key="index" class="nested-row url-row">
        <el-input v-model="url.label" placeholder="標籤" />
        <el-input v-model="url.url" placeholder="https://example.com" />
        <el-button type="danger" plain @click="form.urls.splice(index, 1)">移除</el-button>
      </div>
    </el-form>
    <template #footer>
      <el-button @click="dialogVisible = false">取消</el-button>
      <el-button type="primary" :loading="saving" @click="emit('save')">儲存</el-button>
    </template>
  </el-dialog>
</template>
