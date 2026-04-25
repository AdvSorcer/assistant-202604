<script setup lang="ts">
defineProps<{
  password: string
  loading: boolean
}>()

const emit = defineEmits<{
  'update:password': [value: string]
  login: []
}>()
</script>

<template>
  <div class="login-page">
    <el-card class="login-card" shadow="never">
      <h1>個人助理系統</h1>
      <p>請登入後使用工作資料。</p>
      <el-form label-position="top" @submit.prevent="emit('login')">
        <el-form-item label="登入密碼">
          <el-input
            :model-value="password"
            type="password"
            show-password
            placeholder="開發環境預設：admin"
            @update:model-value="emit('update:password', String($event))"
            @keyup.enter="emit('login')"
          />
        </el-form-item>
        <el-button type="primary" :loading="loading" class="login-button" @click="emit('login')">登入</el-button>
      </el-form>
    </el-card>
  </div>
</template>
