import axios from 'axios'

const defaultApiBaseUrl = window.location.port === '5173'
  ? 'http://localhost:15251/api'
  : '/api'

export const api = axios.create({
  baseURL: defaultApiBaseUrl,
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
