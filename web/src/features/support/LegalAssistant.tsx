import { useEffect, useId, useRef, useState, type FormEvent } from 'react'
import { Dialog } from 'radix-ui'
import { Link } from 'react-router'
import { api, ApiError } from '@/app/api/client'
import type { AssistantAnswer } from '@/app/api/types'
import { Button, ChatIcon, CrossIcon, SendIcon, TextInput } from '@/ui'
import { contactChannels } from './links'
import { linkClass } from './link-class'
import { answerBlocks, sourceLabel, type Citation } from './answer-text'

// Gdy API nie poda podpowiedzi z gotowych odpowiedzi, czat pokazuje te dwie.
const fallbackStarters = [
  'Ile trwa urlop ojcowski?',
  'Jakie dokumenty są potrzebne do zasiłku macierzyńskiego?',
]

/** Tyle wyników wyszukiwarki widać od razu; reszta po „Pokaż więcej”. */
const visibleResults = 2

type Turn = { question: string; answer: AssistantAnswer }

export function LegalAssistant({ calculatorPath }: { calculatorPath: string }) {
  const [open, setOpen] = useState(false)
  const [draft, setDraft] = useState('')
  const [turns, setTurns] = useState<Turn[]>([])
  const [error, setError] = useState<string>()
  const [pendingQuestion, setPendingQuestion] = useState<string>()
  const [starters, setStarters] = useState(fallbackStarters)
  const topic = useRef<string | undefined>(undefined)
  const log = useRef<HTMLDivElement>(null)
  const questionId = useId()
  const errorId = useId()

  useEffect(() => {
    if (!open) return
    let current = true
    api.assistant.suggestions()
      .then((list) => { if (current && list.length > 0) setStarters(list.slice(0, 6)) })
      .catch(() => undefined)
    return () => { current = false }
  }, [open])

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
      if (answer.mode === 'faq' || answer.mode === 'ai' || answer.mode === 'search') {
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
    // Okno nie blokuje strony (modal=false): na komputerze to mały panel w rogu, na telefonie pełny ekran z wyraźnym zamknięciem.
    <Dialog.Root open={open} onOpenChange={setOpen} modal={false}>
      <Dialog.Trigger asChild>
        <Button
          aria-label="Zapytaj o przepisy"
          className="fixed right-3 bottom-[max(0.75rem,env(safe-area-inset-bottom))] z-30 size-12 rounded-full p-0 shadow-overlay sm:right-5 sm:bottom-5 sm:h-10 sm:w-auto sm:gap-2 sm:px-3.5 sm:text-sm"
        >
          <ChatIcon className="size-5" />
          <span className="hidden sm:inline">Zapytaj o przepisy</span>
        </Button>
      </Dialog.Trigger>
      <Dialog.Portal>
        <Dialog.Content
          onInteractOutside={(event) => event.preventDefault()}
          className="fixed inset-0 z-50 flex animate-fade-in flex-col overflow-hidden bg-surface sm:inset-auto sm:right-5 sm:bottom-5 sm:h-[min(32rem,calc(100dvh-2.5rem))] sm:w-[22rem] sm:rounded-[1.25rem] sm:border sm:border-line sm:shadow-overlay"
        >
          <header className="flex shrink-0 items-center gap-3 bg-brand-strong px-4 py-3 text-on-brand">
            <span className="grid size-9 shrink-0 place-items-center rounded-full bg-on-brand/15">
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
            className="min-h-0 flex-1 space-y-4 overflow-x-hidden overflow-y-auto overscroll-contain bg-canvas px-3 py-4 focus-visible:outline-offset-[-3px]"
          >
            <p className="w-fit max-w-[94%] rounded-2xl rounded-tl-sm border border-line bg-surface px-3.5 py-3 text-sm text-ink">
              Cześć! Pomogę Ci zrozumieć zasiłki, urlopy i formalności po narodzinach dziecka. Wybierz pytanie albo napisz własne — odpowiem po ludzku i wskażę źródło.
            </p>
            {turns.length === 0 && !pendingQuestion ? (
              <div role="group" aria-label="Przykładowe pytania" className="flex flex-col items-start gap-2">
                {starters.map((starter) => (
                  <button
                    key={starter}
                    type="button"
                    className="min-h-11 max-w-full rounded-xl border border-line bg-surface px-3 py-2 text-left text-sm font-medium break-words text-brand-strong hover:bg-brand-soft"
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
                <div className="w-fit max-w-[94%] min-w-0 space-y-2.5 rounded-2xl rounded-tl-sm border border-line bg-surface px-3.5 py-3 text-sm text-ink">
                  <Reply answer={answer} calculatorPath={calculatorPath} />
                </div>
              </div>
            ))}
            {pendingQuestion ? (
              <div className="space-y-3">
                <UserMessage question={pendingQuestion} />
                <p className="w-fit rounded-2xl rounded-tl-sm border border-line bg-surface px-3.5 py-2 text-sm text-ink-soft">
                  Sprawdzam przepisy i poradnik ZUS…
                </p>
              </div>
            ) : null}
          </div>
          <form onSubmit={submit} className="shrink-0 space-y-2 border-t border-line bg-surface px-3 pt-3 pb-[max(0.75rem,env(safe-area-inset-bottom))]">
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
              <span>Informacja ogólna, nie decyzja ZUS. Bez danych osobowych.</span>
              {turns.length > 0 ? (
                <button
                  type="button"
                  disabled={Boolean(pendingQuestion)}
                  className="min-h-8 shrink-0 rounded px-1.5 font-semibold text-brand-strong hover:bg-brand-soft disabled:opacity-50"
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

/** Tekst odpowiedzi: akapity rozdzielone pustą linią, wiersze zaczynające się od „- ” jako lista. */
function AnswerText({ text }: { text: string }) {
  const blocks = answerBlocks(text)
  return (
    <div className="space-y-2 break-words">
      {blocks.map((block, index) =>
        block.list ? (
          <ul key={index} className="list-disc space-y-1 pl-5">
            {block.lines.map((line, item) => <li key={item}>{line}</li>)}
          </ul>
        ) : (
          <p key={index}>{block.lines.join(' ')}</p>
        ),
      )}
    </div>
  )
}

function Reply({ answer, calculatorPath }: { answer: AssistantAnswer; calculatorPath: string }) {
  return (
    <>
      <AnswerText text={answer.answer || 'Znalazłem te przepisy i materiały.'} />
      {answer.mode === 'calculator' ? <Link to={calculatorPath} className={linkClass}>Otwórz Twój plan lub kalkulator</Link> : null}
      {answer.mode === 'no_results' ? <p className="text-ink-soft">{contactChannels[0].node}</p> : null}
      {answer.mode === 'search' ? <SearchResults citations={answer.citations} /> : <SourceChips citations={answer.citations} />}
      {answer.mode === 'ai' ? <p className="text-xs text-ink-soft">Wyjaśnienie napisał model — wiążące są przepisy.</p> : null}
    </>
  )
}

/** Źródła zwinięte do paska etykiet; cytat i pełny fragment rozwijają się po kliknięciu, w oknie o ograniczonej wysokości. */
function SourceChips({ citations }: { citations: Citation[] }) {
  const [openIndex, setOpenIndex] = useState<number>()
  const panelId = useId()
  if (citations.length === 0) return null
  const opened = openIndex === undefined ? undefined : citations[openIndex]
  return (
    <div className="space-y-2 border-t border-line pt-2">
      <div className="flex flex-wrap items-center gap-1.5">
        <span className="text-xs font-semibold text-ink-soft">Źródła:</span>
        {citations.map((citation, index) => (
          <button
            key={`${citation.document}|${citation.article}|${index}`}
            type="button"
            aria-expanded={openIndex === index}
            aria-controls={openIndex === index ? panelId : undefined}
            className="min-h-8 max-w-full rounded-full border border-line bg-canvas px-2.5 py-1 text-left text-xs font-medium break-words text-brand-strong hover:bg-brand-soft aria-expanded:border-brand-strong aria-expanded:bg-brand-soft"
            onClick={() => setOpenIndex(openIndex === index ? undefined : index)}
          >
            {sourceLabel(citation)}
          </button>
        ))}
      </div>
      {opened ? <CitationPanel key={openIndex} id={panelId} citation={opened} /> : null}
    </div>
  )
}

function CitationPanel({ id, citation }: { id: string; citation: Citation }) {
  const [full, setFull] = useState(false)
  return (
    <div id={id} role="region" aria-label={`Źródło: ${sourceLabel(citation)}`} className="space-y-1.5 rounded-xl bg-canvas p-2.5">
      <p className="text-xs font-semibold break-words text-ink-soft">{citation.document} · {citation.article}</p>
      <blockquote
        tabIndex={0}
        className="max-h-36 overflow-y-auto border-l-2 border-brand-strong pl-2.5 text-xs leading-relaxed break-words whitespace-pre-wrap"
      >
        {full ? citation.articleText : citation.fragment}
      </blockquote>
      {citation.notes?.map((note) => (
        <p key={note} className="text-xs font-medium break-words">Obowiązywanie: {note}</p>
      ))}
      {citation.articleText !== citation.fragment ? (
        <button
          type="button"
          aria-pressed={full}
          className="min-h-8 rounded px-1 text-xs font-semibold text-brand-strong hover:bg-brand-soft"
          onClick={() => setFull(!full)}
        >
          {full ? 'Pokaż sam cytat' : 'Pokaż cały fragment'}
        </button>
      ) : null}
    </div>
  )
}

/** Wyniki wyszukiwarki: 2 najlepsze od razu (tytuł + 2 wiersze podglądu), reszta po „Pokaż więcej”, całość po kliknięciu. */
function SearchResults({ citations }: { citations: Citation[] }) {
  const [openIndex, setOpenIndex] = useState<number>()
  const [all, setAll] = useState(false)
  const baseId = useId()
  const shown = all ? citations : citations.slice(0, visibleResults)
  return (
    <div className="space-y-1.5">
      <ul className="space-y-1.5">
        {shown.map((citation, index) => {
          const isOpen = openIndex === index
          return (
            <li key={`${citation.document}|${citation.article}|${index}`} className="rounded-xl border border-line bg-canvas">
              <button
                type="button"
                aria-expanded={isOpen}
                aria-controls={`${baseId}-${index}`}
                className="block min-h-11 w-full rounded-xl px-2.5 py-2 text-left hover:bg-brand-soft"
                onClick={() => setOpenIndex(isOpen ? undefined : index)}
              >
                <span className="block text-xs font-semibold break-words text-brand-strong">{sourceLabel(citation)}</span>
                {isOpen ? null : <span className="mt-0.5 line-clamp-2 block text-xs break-words text-ink-soft">{citation.fragment}</span>}
              </button>
              {isOpen ? (
                <div id={`${baseId}-${index}`} className="space-y-1.5 px-2.5 pb-2.5">
                  <blockquote
                    tabIndex={0}
                    className="max-h-40 overflow-y-auto border-l-2 border-brand-strong pl-2.5 text-xs leading-relaxed break-words whitespace-pre-wrap"
                  >
                    {citation.articleText}
                  </blockquote>
                  {citation.notes?.map((note) => (
                    <p key={note} className="text-xs font-medium break-words">Obowiązywanie: {note}</p>
                  ))}
                </div>
              ) : null}
            </li>
          )
        })}
      </ul>
      {citations.length > visibleResults ? (
        <button
          type="button"
          className="min-h-8 rounded px-1 text-xs font-semibold text-brand-strong hover:bg-brand-soft"
          onClick={() => { setAll(!all); setOpenIndex(undefined) }}
        >
          {all ? 'Pokaż mniej' : `Pokaż więcej źródeł (${citations.length - visibleResults})`}
        </button>
      ) : null}
    </div>
  )
}
