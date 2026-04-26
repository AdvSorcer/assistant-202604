<script setup lang="ts">
import type { TodoItem, TodoStatus, TodoStatusOption } from '../types'

type TodoForm = {
  title: string
  description: string
  dueDate: string
  status: TodoStatus
}

defineProps<{
  selectedTodo: TodoItem | null
  editingTodoId: number | null
  saving: boolean
  todoStatusOptions: TodoStatusOption[]
  todoStatusMeta: (status: TodoStatus) => TodoStatusOption
}>()

const viewVisible = defineModel<boolean>('viewVisible', { required: true })
const dialogVisible = defineModel<boolean>('dialogVisible', { required: true })
const form = defineModel<TodoForm>('form', { required: true })

const emit = defineEmits<{
  edit: [item: TodoItem]
  save: []
}>()
</script>

<template>
  <el-dialog v-model="viewVisible" title="查看代辦" width="620px">
    <div v-if="selectedTodo" class="detail-panel">
      <el-descriptions :column="1" border>
        <el-descriptions-item label="事項">{{ selectedTodo.title }}</el-descriptions-item>
        <el-descriptions-item label="日期">{{ selectedTodo.dueDate || '-' }}</el-descriptions-item>
        <el-descriptions-item label="狀態">
          <el-tag :type="todoStatusMeta(selectedTodo.status).type">
            {{ todoStatusMeta(selectedTodo.status).label }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="說明">{{ selectedTodo.description || '-' }}</el-descriptions-item>
      </el-descriptions>
    </div>
    <template #footer>
      <el-button @click="viewVisible = false">關閉</el-button>
      <el-button
        v-if="selectedTodo"
        type="primary"
        @click="viewVisible = false; emit('edit', selectedTodo)"
      >
        編輯
      </el-button>
    </template>
  </el-dialog>

  <el-dialog v-model="dialogVisible" :title="editingTodoId ? '編輯代辦' : '新增代辦'" width="620px">
    <el-form label-position="top">
      <el-form-item label="事項" required>
        <el-input v-model="form.title" />
      </el-form-item>
      <el-form-item label="說明">
        <el-input v-model="form.description" type="textarea" :rows="4" />
      </el-form-item>
      <el-row :gutter="16">
        <el-col :span="12">
          <el-form-item label="日期">
            <el-date-picker v-model="form.dueDate" type="date" value-format="YYYY-MM-DD" clearable />
          </el-form-item>
        </el-col>
        <el-col :span="12">
          <el-form-item label="狀態">
            <el-select v-model="form.status">
              <el-option
                v-for="option in todoStatusOptions"
                :key="option.value"
                :label="option.label"
                :value="option.value"
              />
            </el-select>
          </el-form-item>
        </el-col>
      </el-row>
    </el-form>
    <template #footer>
      <el-button @click="dialogVisible = false">取消</el-button>
      <el-button type="primary" :loading="saving" @click="emit('save')">儲存</el-button>
    </template>
  </el-dialog>
</template>
