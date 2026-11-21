import type { ReactNode } from 'react'
import { linkClass } from './link-class'

export function ExtLink({ url, children }: { url: string; children: ReactNode }) {
  if (!/^https?:\/\//i.test(url)) return <span>{children}</span>
  return (
    <a href={url} target="_blank" rel="noreferrer noopener" className={`break-words ${linkClass}`}>
      {children}
      <span className="sr-only"> (otwiera się w nowej karcie)</span>
    </a>
  )
}
