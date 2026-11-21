import { useState, type FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/app/api/client'
import type { Organization } from '@/app/api/types'
import { Alert, Button, Card, EmptyState, Field, Section, Stack, Switch, TextInput, cn } from '@/ui'
import { controlClass } from '@/ui/input-styles'
import { useFieldControl } from '@/ui/field-context'
import { QueryView } from './QueryView'
import { fieldErrors, generalMessage } from './problem'
import { useAdminSession } from './session'

function Textarea(props: React.ComponentProps<'textarea'>) {
  const field = useFieldControl()
  return (
    <textarea
      id={field.id}
      aria-describedby={field['aria-describedby']}
      aria-invalid={field['aria-invalid']}
      rows={4}
      {...props}
      className={cn(controlClass, 'min-h-24', props.className)}
    />
  )
}

function slugify(text: string): string {
  const slug = text
    .toLocaleLowerCase('pl')
    .replace(/ł/g, 'l')
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 80)
  return slug || 'organizacja'
}

function uniqueId(name: string, existing: Organization[]): string {
  const base = slugify(name)
  const taken = new Set(existing.map((o) => o.id))
  if (!taken.has(base)) return base
  let n = 2
  while (taken.has(`${base}-${n}`)) n += 1
  return `${base}-${n}`
}

function parseSituations(text: string): string[] {
  return text
    .split(/[,\n]/)
    .map((s) => s.trim())
    .filter(Boolean)
}

export function OrganizationsTab() {
  const { password } = useAdminSession()
  const queryClient = useQueryClient()
  const organizations = useQuery({
    queryKey: ['admin', 'organizations'],
    queryFn: () => api.admin.organizations(password),
  })
  const [editing, setEditing] = useState<string | 'new' | null>(null)
  const [confirmDelete, setConfirmDelete] = useState<string | null>(null)
  const [saved, setSaved] = useState<string | null>(null)

  const save = useMutation({
    mutationFn: (list: Organization[]) => api.admin.updateOrganizations(password, list),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin', 'organizations'] }),
  })

  function persist(list: Organization[], message: string, onDone: () => void) {
    setSaved(null)
    save.mutate(list, {
      onSuccess: () => {
        setSaved(message)
        onDone()
      },
    })
  }

  return (
    <Section
      title="Organizacje wsparcia"
      description="Użytkownikom pokazujemy wyłącznie organizacje oznaczone jako zweryfikowane. Niezweryfikowane są ukryte."
      actions={
        editing === 'new' ? null : (
          <Button
            variant="secondary"
            onClick={() => {
              save.reset()
              setSaved(null)
              setEditing('new')
            }}
          >
            Dodaj organizację
          </Button>
        )
      }
    >
      <div role="status" className="empty:hidden">
        {saved ? <Alert variant="info" title={saved} /> : null}
      </div>
      <QueryView query={organizations} loadingLabel="Ładowanie organizacji…">
        {(list) => (
          <Stack gap="md">
            {editing === 'new' ? (
              <OrganizationForm
                key="new"
                title="Nowa organizacja"
                initial={{
                  id: '',
                  nazwa: '',
                  opis: '',
                  sytuacje: [],
                  url: null,
                  telefon: null,
                  zweryfikowano: false,
                }}
                index={list.length}
                saving={save.isPending}
                error={save.error}
                onCancel={() => setEditing(null)}
                onSubmit={(org) =>
                  persist([...list, { ...org, id: uniqueId(org.nazwa, list) }], `Dodano organizację: ${org.nazwa}.`, () => setEditing(null))
                }
              />
            ) : null}

            {list.length === 0 && editing !== 'new' ? (
              <EmptyState title="Brak organizacji" description="Katalog wsparcia jest pusty. Dodaj pierwszą organizację." />
            ) : null}

            {list.map((org, index) =>
              editing === org.id ? (
                <OrganizationForm
                  key={org.id}
                  title={`Edycja: ${org.nazwa}`}
                  initial={org}
                  index={index}
                  saving={save.isPending}
                  error={save.error}
                  onCancel={() => setEditing(null)}
                  onSubmit={(next) =>
                    persist(
                      list.map((o) => (o.id === org.id ? next : o)),
                      `Zapisano organizację: ${next.nazwa}.`,
                      () => setEditing(null),
                    )
                  }
                />
              ) : (
                <Card as="article" key={org.id} aria-label={org.nazwa}>
                  <Stack gap="sm">
                    <h3 className="break-words text-lg font-semibold text-ink">{org.nazwa}</h3>
                    <p className="text-sm font-semibold text-ink">
                      Status:{' '}
                      {org.zweryfikowano ? 'Zweryfikowana — widoczna dla użytkowników' : 'Niezweryfikowana — ukryta przed użytkownikami'}
                    </p>
                    <p className="whitespace-pre-line break-words text-ink">{org.opis}</p>
                    <p className="break-words text-sm text-ink-soft">Sytuacje: {org.sytuacje.length ? org.sytuacje.join(', ') : 'brak'}</p>
                    {org.url ? <p className="break-all text-sm text-ink-soft">Strona: {org.url}</p> : null}
                    {org.telefon ? <p className="text-sm text-ink-soft">Telefon: {org.telefon}</p> : null}
                    <div className="flex flex-wrap gap-3 pt-1">
                      <Button
                        variant="secondary"
                        onClick={() => {
                          save.reset()
                          setSaved(null)
                          setConfirmDelete(null)
                          setEditing(org.id)
                        }}
                        aria-label={`Edytuj ${org.nazwa}`}
                      >
                        Edytuj
                      </Button>
                      {confirmDelete === org.id ? (
                        <>
                          <Button
                            loading={save.isPending}
                            loadingLabel="Usuwam…"
                            onClick={() =>
                              persist(
                                list.filter((o) => o.id !== org.id),
                                `Usunięto organizację: ${org.nazwa}.`,
                                () => setConfirmDelete(null),
                              )
                            }
                          >
                            Potwierdź usunięcie
                          </Button>
                          <Button variant="secondary" onClick={() => setConfirmDelete(null)}>
                            Anuluj
                          </Button>
                        </>
                      ) : (
                        <Button variant="secondary" onClick={() => setConfirmDelete(org.id)} aria-label={`Usuń ${org.nazwa}`}>
                          Usuń
                        </Button>
                      )}
                    </div>
                    {confirmDelete === org.id && save.isError ? (
                      <Alert variant="error" title="Nie usunięto organizacji">
                        {generalMessage(save.error)}
                      </Alert>
                    ) : null}
                  </Stack>
                </Card>
              ),
            )}
          </Stack>
        )}
      </QueryView>
    </Section>
  )
}

interface OrganizationFormProps {
  title: string
  initial: Organization
  index: number
  saving: boolean
  error: unknown
  onCancel: () => void
  onSubmit: (org: Organization) => void
}

function OrganizationForm({ title, initial, index, saving, error, onCancel, onSubmit }: OrganizationFormProps) {
  const [nazwa, setNazwa] = useState(initial.nazwa)
  const [opis, setOpis] = useState(initial.opis)
  const [url, setUrl] = useState(initial.url ?? '')
  const [telefon, setTelefon] = useState(initial.telefon ?? '')
  const [sytuacje, setSytuacje] = useState(initial.sytuacje.join(', '))
  const [zweryfikowano, setZweryfikowano] = useState(initial.zweryfikowano)
  const [clientErrors, setClientErrors] = useState<Record<string, string>>({})

  const serverErrors = fieldErrors(error)
  const at = (field: string) => serverErrors[`[${index}].${field}`]
  const errors: Record<string, string | undefined> = {
    nazwa: clientErrors.nazwa ?? at('nazwa'),
    opis: clientErrors.opis ?? at('opis'),
    url: at('url'),
    telefon: at('telefon'),
    sytuacje: at('sytuacje'),
  }
  const general = error && Object.keys(serverErrors).length === 0 ? generalMessage(error) : (serverErrors.body ?? null)

  function submit(event: FormEvent) {
    event.preventDefault()
    const found: Record<string, string> = {}
    if (!nazwa.trim()) found.nazwa = 'Podaj nazwę organizacji.'
    if (!opis.trim()) found.opis = 'Podaj krótki opis.'
    setClientErrors(found)
    if (Object.keys(found).length > 0) return
    onSubmit({
      ...initial,
      nazwa: nazwa.trim(),
      opis: opis.trim(),
      url: url.trim() || null,
      telefon: telefon.trim() || null,
      sytuacje: parseSituations(sytuacje),
      zweryfikowano,
    })
  }

  return (
    <form onSubmit={submit} noValidate>
      <Card tone="muted">
        <Stack gap="md">
          <h3 className="text-lg font-semibold text-ink">{title}</h3>
          <Field label="Nazwa" error={errors.nazwa}>
            <TextInput value={nazwa} onChange={(e) => setNazwa(e.target.value)} maxLength={200} />
          </Field>
          <Field label="Opis" error={errors.opis}>
            <Textarea value={opis} onChange={(e) => setOpis(e.target.value)} maxLength={2000} />
          </Field>
          <Field label="Adres www" optional error={errors.url} description="Pełny adres, np. https://przyklad.pl">
            <TextInput type="url" inputMode="url" value={url} onChange={(e) => setUrl(e.target.value)} />
          </Field>
          <Field label="Telefon" optional error={errors.telefon}>
            <TextInput type="tel" value={telefon} onChange={(e) => setTelefon(e.target.value)} maxLength={50} />
          </Field>
          <Field label="Sytuacje" optional error={errors.sytuacje} description="Oddziel przecinkami lub nowymi wierszami.">
            <Textarea rows={2} value={sytuacje} onChange={(e) => setSytuacje(e.target.value)} />
          </Field>
          <Switch
            label="Zweryfikowano"
            description={`Status: ${zweryfikowano ? 'zweryfikowana — będzie pokazywana użytkownikom' : 'niezweryfikowana — ukryta przed użytkownikami'}`}
            checked={zweryfikowano}
            onChange={(e) => setZweryfikowano(e.target.checked)}
          />
          {general ? (
            <Alert variant="error" title="Nie zapisano zmian">
              {general}
            </Alert>
          ) : null}
          <div className="flex flex-wrap gap-3">
            <Button type="submit" loading={saving} loadingLabel="Zapisuję…">
              Zapisz
            </Button>
            <Button variant="secondary" onClick={onCancel} disabled={saving}>
              Anuluj
            </Button>
          </div>
        </Stack>
      </Card>
    </form>
  )
}
