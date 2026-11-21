import type { Fact, FactSet, PersonRef } from '@/app/api/types'

export type Bucket = 'sprawa' | PersonRef
export type AnyFact = Fact<unknown>
type FactMap = Record<string, AnyFact | undefined>

export const bucketOfPerson = (person?: PersonRef | null): Bucket => person ?? 'sprawa'

const mapOf = (facts: FactSet, bucket: Bucket): FactMap => ((facts[bucket] ?? {}) as unknown as FactMap)

export const factsIn = (facts: FactSet, bucket: Bucket): FactMap => mapOf(facts, bucket)

export function getFact(facts: FactSet, bucket: Bucket, key: string): AnyFact | undefined {
  return mapOf(facts, bucket)[key]
}

export function withFact(facts: FactSet, bucket: Bucket, key: string, fact: AnyFact | undefined): FactSet {
  const next: FactMap = { ...mapOf(facts, bucket) }
  if (fact === undefined) delete next[key]
  else next[key] = fact
  return { ...facts, [bucket]: next } as unknown as FactSet
}

export const userFactOf = (value: unknown): AnyFact => ({ value, source: 'user' })

function onlyUser(map: FactMap): FactMap {
  return Object.fromEntries(Object.entries(map).filter(([, fact]) => fact?.source === 'user'))
}

export function withoutIntegrationFacts(facts: FactSet): FactSet {
  const result: FactSet = {
    sprawa: onlyUser(mapOf(facts, 'sprawa')) as unknown as FactSet['sprawa'],
    wnioskodawca: onlyUser(mapOf(facts, 'wnioskodawca')) as unknown as FactSet['wnioskodawca'],
  }
  if (facts.drugiRodzic) result.drugiRodzic = onlyUser(mapOf(facts, 'drugiRodzic')) as unknown as FactSet['drugiRodzic']
  return result
}

export function mergeIntegrationFacts(current: FactSet, integration: FactSet): FactSet {
  const mine = withoutIntegrationFacts(current)
  const merged: FactSet = {
    sprawa: { ...integration.sprawa, ...mine.sprawa },
    wnioskodawca: { ...integration.wnioskodawca, ...mine.wnioskodawca },
  }
  const second = { ...(integration.drugiRodzic ?? {}), ...(mine.drugiRodzic ?? {}) }
  if (integration.drugiRodzic || mine.drugiRodzic) merged.drugiRodzic = second
  return merged
}

export function countFacts(facts: FactSet): number {
  return (['sprawa', 'wnioskodawca', 'drugiRodzic'] as const).reduce((sum, bucket) => sum + Object.keys(mapOf(facts, bucket)).length, 0)
}
