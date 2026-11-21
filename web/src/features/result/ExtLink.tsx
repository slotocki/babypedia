import type { ReactNode } from 'react'
import { isHttpUrl } from './labels'

export function ExtLink({ url, children }: { url: string; children: ReactNode }) {
  if (!isHttpUrl(url)) return <span>{children}</span>
  return (
    <a
      href={url}
      target="_blank"
      rel="noreferrer noopener"
      className="break-words font-semibold text-brand-strong underline underline-offset-4 hover:text-brand"
    >
      {children}
      <span className="sr-only"> (otwiera się w nowej karcie)</span>
    </a>
  )
}
