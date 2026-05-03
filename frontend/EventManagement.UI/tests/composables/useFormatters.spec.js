import { describe, it, expect } from 'vitest'
import { useFormatters } from '@/composables/useFormatters'

describe('useFormatters', () => {
  const { formatDate, formatPrice } = useFormatters()

  describe('formatDate', () => {
    it('includes day, month name, and year in French', () => {
      const result = formatDate('2026-06-15T00:00:00Z')
      expect(result).toContain('2026')
      expect(result).toContain('juin')
    })

    it('formats December correctly', () => {
      const result = formatDate('2026-12-25T00:00:00Z')
      expect(result).toContain('25')
      expect(result).toContain('décembre')
      expect(result).toContain('2026')
    })
  })

  describe('formatPrice', () => {
    it('returns Gratuit for price 0', () => {
      expect(formatPrice(0)).toBe('Gratuit')
    })

    it('returns price with euro sign for non-zero', () => {
      expect(formatPrice(25)).toBe('25 €')
    })

    it('handles decimal prices', () => {
      expect(formatPrice(9.99)).toBe('9.99 €')
    })
  })
})
