import type { AssistantAnswer } from '@/app/api/types'

/**
 * Czyste funkcje czatu „Zapytaj o przepisy” (LegalAssistant): podział tekstu odpowiedzi na akapity i listy
 * oraz krótkie etykiety źródeł. Osobny moduł, żeby dało się je sprawdzić testami bez Reacta.
 */

export type Citation = AssistantAnswer['citations'][number]

export interface AnswerBlock {
  list: boolean
  lines: string[]
}

/** Akapity rozdzielone pustą linią, kolejne wiersze zaczynające się od „- ” łączone w jedną listę. */
export function answerBlocks(text: string): AnswerBlock[] {
  const blocks: AnswerBlock[] = []
  for (const raw of text.split('\n')) {
    const line = raw.trim()
    if (!line) {
      blocks.push({ list: false, lines: [] })
      continue
    }
    const list = line.startsWith('- ')
    const last = blocks.at(-1)
    const content = list ? line.slice(2) : line
    if (last && last.list === list && last.lines.length > 0) last.lines.push(content)
    else blocks.push({ list, lines: [content] })
  }
  return blocks.filter((block) => block.lines.length > 0)
}

const shortDocument: Record<string, string> = {
  'Rozporządzenie w sprawie dokumentów do zasiłków': 'Rozporządzenie (dokumenty)',
  'Rozporządzenie w sprawie wniosków pracowniczych': 'Rozporządzenie (wnioski)',
  'Załącznik ZUS (materiał pomocniczy)': 'Załącznik ZUS',
  'Poradnik ZUS (materiał pomocniczy)': 'Poradnik ZUS',
}

/** Krótka etykieta źródła, np. „Ustawa zasiłkowa art. 29” albo „Poradnik ZUS”. */
export function sourceLabel(citation: Pick<Citation, 'document' | 'article'>) {
  const name = shortDocument[citation.document] ?? citation.document
  return /^(Art\.|§)/.test(citation.article) ? `${name} ${citation.article.replace(/^Art\.\s*/, 'art. ')}` : name
}
