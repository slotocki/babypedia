import { TextInput, type TextInputProps } from './TextInput'

export type DateInputProps = Omit<TextInputProps, 'type' | 'suffix'>

export function DateInput(props: DateInputProps) {
  return <TextInput type="date" className="min-w-0 appearance-none" {...props} />
}
