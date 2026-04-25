import axios from 'axios'

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5251/api',
})

export function setAuthToken(token: string) {
  api.defaults.headers.common.Authorization = token ? `Bearer ${token}` : undefined
}

export function toErrorMessage(error: unknown) {
  if (axios.isAxiosError(error)) {
    return error.response?.data?.title ?? error.response?.data?.message ?? error.message
  }

  return error instanceof Error ? error.message : '發生未知錯誤'
}
