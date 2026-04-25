<script setup lang="ts">
const emit = defineEmits<{
  logout: []
}>()
</script>

<template>
  <el-header class="topbar">
    <div class="topbar-actions">
      <el-button type="primary" plain @click="emit('logout')">登出</el-button>
    </div>
  </el-header>
</template>
