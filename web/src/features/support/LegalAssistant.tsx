import { useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { api, ApiError } from '@/app/api/client'
import type { AssistantAnswer } from '@/app/api/types'
import { Button, Card, Disclosure, Field, Section, TextInput } from '@/ui'
import { contactChannels } from './links'
import { linkClass } from './link-class'

export function LegalAssistant({ calculatorPath }: { calculatorPath: string }) {
  const [question, setQuestion] = useState('')
  const [answer, setAnswer] = useState<AssistantAnswer>()
  const [error, setError] = useState<string>()
  const [pending, setPending] = useState(false)

  async function ask(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (pending || !question.trim()) return
    setPending(true)
    setAnswer(undefined)
    setError(undefined)
    try {
      setAnswer(await api.assistant.ask(question.trim()))
    } catch (failure) {
      setError(failure instanceof ApiError && failure.status === 400
        ? failure.problem.errors?.question?.[0] ?? 'Sprawdź treść pytania.'
        : 'Nie udało się pobrać odpowiedzi. Spróbuj ponownie.')
    } finally {
      setPending(false)
    }
  }

  return (
    <Section title="Zapytaj o przepisy">
      <Card className="space-y-5">
        <form onSubmit={ask} className="space-y-3">
          <Field label="Twoje pytanie" description="Zadaj ogólne pytanie. Pomiń dane osobowe." error={error}>
            <TextInput
              value={question}
              placeholder="Np. czym jest urlop rodzicielski?"
              onChange={(event) => setQuestion(event.target.value)}
              maxLength={500}
              autoComplete="off"
              disabled={pending}
              required
            />
          </Field>
          <Button type="submit" disabled={pending || !question.trim()}>
            {pending ? 'Szukam…' : 'Zapytaj'}
          </Button>
        </form>
        <div aria-live="polite" aria-busy={pending} className="space-y-3 empty:mt-0">
          {answer ? (
            <>
              {answer.mode === 'ai' ? <p className="text-sm font-semibold text-ink-soft">Odpowiedź AI</p> : null}
              {answer.mode === 'search' ? <p className="font-semibold text-ink">Pasujące przepisy i materiały</p> : null}
              {answer.answer ? <p className="whitespace-pre-line break-words text-ink">{answer.answer}</p> : null}
              {answer.mode === 'calculator' ? <Link to={calculatorPath} className={linkClass}>Przejdź do kalkulatora</Link> : null}
              {answer.mode === 'no_results' ? <p className="text-ink-soft">{contactChannels[0].node}</p> : null}
              {answer.citations.length > 0 ? (
                <ol className="space-y-2">
                  {answer.citations.map((citation, index) => (
                    <li key={`${citation.document}|${citation.article}|${citation.fragment}`}>
                      <Disclosure
                        title={`${index + 1}. ${citation.document} · ${citation.article}`}
                        summary={<span className="break-words">{citation.fragment}</span>}
                      >
                        <blockquote className="whitespace-pre-wrap break-words text-sm text-ink">
                          {citation.articleText}
                        </blockquote>
                      </Disclosure>
                    </li>
                  ))}
                </ol>
              ) : null}
              {answer.disclaimer ? <p className="text-sm text-ink-soft">{answer.disclaimer}</p> : null}
            </>
          ) : null}
        </div>
      </Card>
    </Section>
  )
}
