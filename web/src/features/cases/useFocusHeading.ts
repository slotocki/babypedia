import { useEffect, useRef } from 'react'

export function useFocusHeading<T extends HTMLElement>() {
  const ref = useRef<T>(null)

  useEffect(() => {
    window.scrollTo(0, 0)
    const heading = ref.current?.querySelector('h1')
    if (heading) {
      heading.tabIndex = -1
      heading.classList.add('focus:outline-none')
      heading.focus({ preventScroll: true })
    }
  }, [])

  return ref
}
