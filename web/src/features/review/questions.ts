import type { MissingFact } from '@/app/api/types'
import { factMeta, isFactKey, optionsOf, type FactKind, type FactMeta } from './fact-labels'
import { bucketOfPerson, type Bucket } from './facts'

export interface ResolvedQuestion {
  id: string
  question: MissingFact
  bucket: Bucket
  kind: FactKind
  meta: FactMeta
  options: { value: string; label: string }[]
}

const special: FactKind[] = ['dateRanges', 'sytuacjaMatki']

export function resolveQuestion(question: MissingFact): ResolvedQuestion {
  const known = isFactKey(question.fact) ? factMeta[question.fact] : undefined
  const options = optionsOf(question.fact, question.options)

  let kind: FactKind
  if (known && special.includes(known.kind)) kind = known.kind
  else if (question.type === 'list') kind = 'readonly'
  else if (question.type === 'enum') kind = options.length > 0 ? 'enum' : 'readonly'
  else kind = question.type

  const meta: FactMeta = {
    ...(known ?? { label: question.question, format: String }),
    kind,
    options: Object.fromEntries(options.map((o) => [o.value, o.label])),
  }
  return { id: `${question.person ?? 'sprawa'}:${question.fact}`, question, bucket: bucketOfPerson(question.person), kind, meta, options }
}
