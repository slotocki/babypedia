import { TextInput, type TextInputProps } from './TextInput'

export type NumberInputProps = Omit<TextInputProps, 'type'>

export function NumberInput(props: NumberInputProps) {
  return <TextInput type="number" inputMode="decimal" autoComplete="off" {...props} />
}
