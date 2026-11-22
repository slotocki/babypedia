import { useEffect, useId, useRef, useState, type FormEvent } from 'react'
import { Dialog } from 'radix-ui'
import { Link } from 'react-router'
import { api, ApiError } from '@/app/api/client'
import type { AssistantAnswer } from '@/app/api/types'
import { Button, ChatIcon, CrossIcon, Disclosure, SendIcon, TextInput } from '@/ui'
import { contactChannels } from './links'
import { linkClass } from './link-class'

const starters = [
  'Kiedy można skorzystać z urlopu ojcowskiego?',
  'Jakie dokumenty są potrzebne do zasiłku macierzyńskiego?',
]

type Turn = { question: string; answer: AssistantAnswer }

export function LegalAssistant({ calculatorPath }: { calculatorPath: string }) {
  const [open, setOpen] = useState(false)
  const [draft, setDraft] = useState('')
  const [turns, setTurns] = useState<Turn[]>([])
  const [error, setError] = useState<string>()
  const [pendingQuestion, setPendingQuestion] = useState<string>()
  const topic = useRef<string | undefined>(undefined)
  const log = useRef<HTMLDivElement>(null)
  const questionId = useId()
  const errorId = useId()

  useEffect(() => {
    if (open && log.current) log.current.scrollTop = log.current.scrollHeight
  }, [open, turns, pendingQuestion])

  async function ask(input: string) {
    const question = input.trim()
    if (pendingQuestion || !question) return
    setDraft('')
    setError(undefined)
    setPendingQuestion(question)
    try {
      const answer = await api.assistant.ask(question, topic.current)
      setTurns((current) => [...current, { question, answer }])
      if (answer.mode === 'ai' || answer.mode === 'search') {
        const followsTopic = /^(a\s|(?:czy|jak|gdzie|kiedy|ile)\s+(?:to|tego|wtedy|tam)\b|ile (?:trwa|dni|tygodni)\s*\??$)/i.test(question)
        if (!followsTopic || !topic.current) topic.current = question
      }
    } catch (failure) {
      setDraft(question)
      setError(failure instanceof ApiError && failure.status === 400
        ? failure.problem.errors?.question?.[0] ?? 'Sprawdź treść pytania.'
        : 'Nie udało się uzyskać odpowiedzi. Spróbuj ponownie.')
    } finally {
      setPendingQuestion(undefined)
    }
  }

  function reset() {
    topic.current = undefined
    setTurns([])
    setDraft('')
    setError(undefined)
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    void ask(draft)
  }

  return (
    <Dialog.Root open={open} onOpenChange={setOpen}>
      <Dialog.Trigger asChild>
        <Button className="fixed right-4 bottom-[max(1rem,env(safe-area-inset-bottom))] z-30 h-12 rounded-pill px-4 shadow-overlay sm:right-6 sm:bottom-6">
          <ChatIcon className="size-5" />
          Zapytaj o przepisy
        </Button>
      </Dialog.Trigger>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-40 animate-fade-in bg-ink/35 sm:bg-ink/15" />
        <Dialog.Content className="fixed inset-x-3 bottom-3 z-50 flex h-[min(39rem,calc(100dvh-1.5rem))] animate-fade-in flex-col overflow-hidden rounded-[1.25rem] border border-line bg-surface shadow-overlay sm:inset-x-auto sm:right-6 sm:bottom-6 sm:w-[25rem]">
          <header className="flex shrink-0 items-center gap-3 bg-brand-strong p-4 text-on-brand">
            <span className="grid size-10 shrink-0 place-items-center rounded-full bg-on-brand/15">
              <ChatIcon className="size-5" />
            </span>
            <div className="min-w-0 flex-1">
              <Dialog.Title className="font-semibold">Zapytaj o przepisy</Dialog.Title>
              <Dialog.Description className="text-xs text-on-brand/85">Pomocnik Kompasu Rodzica</Dialog.Description>
            </div>
            <Dialog.Close
              aria-label="Zamknij rozmowę"
              className="grid size-11 shrink-0 place-items-center rounded-full text-on-brand hover:bg-on-brand/15 focus-visible:outline-on-brand"
            >
              <CrossIcon className="size-5" />
            </Dialog.Close>
          </header>
          <div
            ref={log}
            role="log"
            aria-label="Przebieg rozmowy"
            aria-live="polite"
            tabIndex={0}
            className="min-h-0 flex-1 space-y-4 overflow-y-auto overscroll-contain bg-canvas px-4 py-5 focus-visible:outline-offset-[-3px]"
          >
            <p className="w-fit max-w-[94%] rounded-2xl rounded-tl-sm border border-line bg-surface px-3.5 py-3 text-sm text-ink">
              Zapytaj o świadczenia, urlopy lub dokumenty. Wyjaśnię to krótko i pokażę fragmenty przepisów, na których opiera się odpowiedź.
            </p>
            {turns.length === 0 && !pendingQuestion ? (
              <div className="flex flex-wrap gap-2">
                {starters.map((starter) => (
                  <button
                    key={starter}
                    type="button"
                    className="min-h-11 rounded-xl border border-line bg-surface px-3 py-2 text-left text-sm font-medium text-brand-strong hover:bg-brand-soft"
                    onClick={() => void ask(starter)}
                  >
                    {starter}
                  </button>
                ))}
              </div>
            ) : null}
            {turns.map(({ question, answer }, index) => (
              <div key={index} className="space-y-3">
                <UserMessage question={question} />
                <div className="w-fit max-w-[94%] space-y-3 rounded-2xl rounded-tl-sm border border-line bg-surface px-3.5 py-3 text-sm text-ink">
                  <Reply answer={answer} calculatorPath={calculatorPath} />
                </div>
              </div>
            ))}
            {pendingQuestion ? (
              <div className="space-y-3">
                <UserMessage question={pendingQuestion} />
                <p className="w-fit rounded-2xl rounded-tl-sm border border-line bg-surface px-3.5 py-2 text-sm text-ink-soft">
                  Szukam przepisów…
                </p>
              </div>
            ) : null}
          </div>
          <form onSubmit={submit} className="shrink-0 space-y-2 border-t border-line bg-surface px-4 pt-3 pb-[max(0.75rem,env(safe-area-inset-bottom))]">
            {error ? <p id={errorId} role="alert" className="text-sm text-bad">{error}</p> : null}
            <div className="flex items-center gap-2">
              <label htmlFor={questionId} className="sr-only">Twoje pytanie</label>
              <TextInput
                id={questionId}
                value={draft}
                placeholder="Napisz pytanie…"
                onChange={(event) => setDraft(event.target.value)}
                maxLength={500}
                autoComplete="off"
                readOnly={Boolean(pendingQuestion)}
                aria-describedby={error ? errorId : undefined}
                aria-invalid={Boolean(error)}
                className="min-w-0 rounded-xl"
              />
              <Button
                type="submit"
                aria-label="Wyślij pytanie"
                disabled={Boolean(pendingQuestion) || !draft.trim()}
                className="size-11 shrink-0 rounded-xl p-0"
              >
                <SendIcon className="size-5" />
              </Button>
            </div>
            <div className="flex min-h-7 items-center justify-between gap-2 text-xs text-ink-soft">
              <span>Pomiń dane osobowe.</span>
              {turns.length > 0 ? (
                <button
                  type="button"
                  disabled={Boolean(pendingQuestion)}
                  className="min-h-8 rounded px-1.5 font-semibold text-brand-strong hover:bg-brand-soft disabled:opacity-50"
                  onClick={reset}
                >
                  Nowa rozmowa
                </button>
              ) : null}
            </div>
          </form>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}

function UserMessage({ question }: { question: string }) {
  return (
    <p className="ml-auto w-fit max-w-[88%] rounded-2xl rounded-tr-sm bg-brand-strong px-3.5 py-2.5 text-sm break-words text-on-brand">
      <span className="sr-only">Ty: </span>{question}
    </p>
  )
}

function Reply({ answer, calculatorPath }: { answer: AssistantAnswer; calculatorPath: string }) {
  return (
    <>
      <p>{answer.answer || 'Znalazłem te przepisy i materiały.'}</p>
      {answer.mode === 'calculator' ? <Link to={calculatorPath} className={linkClass}>Przejdź do kalkulatora</Link> : null}
      {answer.mode === 'no_results' ? <p className="text-ink-soft">{contactChannels[0].node}</p> : null}
      {answer.citations.map((citation, index) => (
        <Disclosure
          key={`${citation.document}|${citation.article}|${index}`}
          title={`${citation.document} · ${citation.article}`}
          summary={<span className="break-words">{citation.fragment}</span>}
        >
          <blockquote className="whitespace-pre-wrap break-words text-sm">{citation.articleText}</blockquote>
        </Disclosure>
      ))}
      {answer.disclaimer ? <p className="text-xs text-ink-soft">{answer.disclaimer}</p> : null}
    </>
  )
}
