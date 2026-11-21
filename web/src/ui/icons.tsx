import type { ReactNode, SVGProps } from 'react'

export type IconProps = SVGProps<SVGSVGElement>

function Icon({ children, ...props }: IconProps & { children: ReactNode }) {
  return (
    <svg
      viewBox="0 0 24 24"
      width="1em"
      height="1em"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      focusable="false"
      {...props}
    >
      {children}
    </svg>
  )
}

export const CheckIcon = (p: IconProps) => <Icon {...p}><path d="m5 12.5 4.5 4.5L19 7.5" /></Icon>
export const CrossIcon = (p: IconProps) => <Icon {...p}><path d="m6 6 12 12M18 6 6 18" /></Icon>
export const QuestionIcon = (p: IconProps) => (
  <Icon {...p}><circle cx="12" cy="12" r="9" /><path d="M9.5 9.5a2.5 2.5 0 1 1 3.5 2.3c-.7.4-1 .9-1 1.7M12 17h.01" /></Icon>
)
export const MinusIcon = (p: IconProps) => <Icon {...p}><circle cx="12" cy="12" r="9" /><path d="M8 12h8" /></Icon>
export const InfoIcon = (p: IconProps) => <Icon {...p}><circle cx="12" cy="12" r="9" /><path d="M12 11v5M12 8h.01" /></Icon>
export const WarningIcon = (p: IconProps) => <Icon {...p}><path d="M12 3.5 2.5 20h19L12 3.5ZM12 10v4.5M12 17.5h.01" /></Icon>
export const ErrorIcon = (p: IconProps) => <Icon {...p}><circle cx="12" cy="12" r="9" /><path d="m9 9 6 6M15 9l-6 6" /></Icon>
export const ShieldIcon = (p: IconProps) => (
  <Icon {...p}><path d="M12 3 4.5 6v5.5c0 4.5 3 8 7.5 9.5 4.5-1.5 7.5-5 7.5-9.5V6L12 3Z" /><path d="M12 8v4M12 15h.01" /></Icon>
)
export const ChevronDownIcon = (p: IconProps) => <Icon {...p}><path d="m6 9 6 6 6-6" /></Icon>
export const CalendarIcon = (p: IconProps) => (
  <Icon {...p}><rect x="4" y="5" width="16" height="15" rx="2" /><path d="M8 3v4M16 3v4M4 10h16" /></Icon>
)
export const UserIcon = (p: IconProps) => <Icon {...p}><circle cx="12" cy="8" r="3.5" /><path d="M5 20c.8-3.5 3.5-5 7-5s6.2 1.5 7 5" /></Icon>
export const BuildingIcon = (p: IconProps) => <Icon {...p}><path d="M4 20V9l8-5 8 5v11M9 20v-6h6v6M3 20h18" /></Icon>
export const FileIcon = (p: IconProps) => <Icon {...p}><path d="M7 3h7l4 4v14H7V3Z" /><path d="M14 3v4h4M10 13h5M10 17h5" /></Icon>
export const LightbulbIcon = (p: IconProps) => (
  <Icon {...p}><path d="M9 18h6M10 21h4M12 3a6 6 0 0 0-3.5 10.9c.6.5 1 1.2 1 2.1h5c0-.9.4-1.6 1-2.1A6 6 0 0 0 12 3Z" /></Icon>
)
export const ClockIcon = (p: IconProps) => <Icon {...p}><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></Icon>
export const FlaskIcon = (p: IconProps) => (
  <Icon {...p}><path d="M9 3h6M10 3v6l-5.5 9.5A1.5 1.5 0 0 0 5.8 21h12.4a1.5 1.5 0 0 0 1.3-2.5L14 9V3M7.5 15h9" /></Icon>
)
