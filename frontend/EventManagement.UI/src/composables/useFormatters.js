export function useFormatters() {
  function formatDate(date) {
    return new Date(date).toLocaleDateString('fr-FR', { day: 'numeric', month: 'long', year: 'numeric' })
  }

  function formatPrice(price) {
    return price === 0 ? 'Gratuit' : `${price} €`
  }

  return { formatDate, formatPrice }
}
