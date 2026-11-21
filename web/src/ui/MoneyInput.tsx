import { TextInput, type TextInputProps } from './TextInput'

export type MoneyInputProps = Omit<TextInputProps, 'type' | 'suffix' | 'inputMode'>

export function MoneyInput({ placeholder = '0,00', ...props }: MoneyInputProps) {
  return <TextInput type="text" inputMode="decimal" autoComplete="off" suffix="zł" placeholder={placeholder} {...props} />
}
