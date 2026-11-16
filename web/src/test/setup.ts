import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach } from 'vitest'
import { fetchMock } from '@/test/api'

globalThis.fetch = fetchMock as unknown as typeof fetch

afterEach(() => {
  cleanup()
  fetchMock.mockReset()
})
