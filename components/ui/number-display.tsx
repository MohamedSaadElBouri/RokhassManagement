interface NumberDisplayProps {
  value: number
  className?: string
}

export function NumberDisplay({ value, className = "" }: NumberDisplayProps) {
  // Force l'utilisation des chiffres français (0-9)
  const frenchNumber = value.toString().replace(/[٠-٩]/g, (match) => {
    const arabicDigits = "٠١٢٣٤٥٦٧٨٩"
    const frenchDigits = "0123456789"
    return frenchDigits[arabicDigits.indexOf(match)]
  })

  return <span className={className}>{frenchNumber}</span>
}
