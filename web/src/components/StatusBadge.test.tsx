import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { type Status, StatusBadge } from '@/components/StatusBadge'

describe('StatusBadge', () => {
  it.each<[Status, 'solid' | 'hollow']>([
    ['Draft', 'solid'],
    ['Published', 'solid'],
    ['Archived', 'hollow'],
    ['Open', 'solid'],
    ['Handled', 'hollow'],
    ['Featured', 'solid'],
  ])('shows %s as a word with a %s dot', (status, dot) => {
    const { container } = render(<StatusBadge status={status} />)

    expect(screen.getByText(status)).toBeInTheDocument()
    expect(container.querySelector('[data-dot]')).toHaveAttribute('data-dot', dot)
  })
})
