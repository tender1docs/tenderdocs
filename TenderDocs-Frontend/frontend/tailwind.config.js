/** @type {import('tailwindcss').Config} */
export default {
  darkMode: 'class',
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        // Exact palette sampled from the reference screenshots
        brand: {
          50:  '#E9F4F1',
          100: '#D2EAE3',
          200: '#A6D5C8',
          300: '#6FBBA8',
          400: '#3E9A85',
          500: '#1B8470',
          600: '#0F766E', // primary buttons / logo / active
          700: '#115E59', // hover / gradient end
          800: '#114E4A',
          900: '#0F3F3C',
        },
        canvas: '#F8F9FA',
        ink: {
          DEFAULT: '#0F172A',
          soft: '#334155',
          muted: '#64748B',
          faint: '#94A3B8',
        },
        line: '#ECEEF1',
        hero: {
          from: '#FBF8F1',
          to:   '#EAF4F0',
        },
        valid:  { bg: '#ECFDF3', text: '#16A34A', ring: '#BBF7D0' },
        warn:   { bg: '#FEF7E7', text: '#B45309', ring: '#FDE68A' },
        danger: { bg: '#FEF2F2', text: '#DC2626', ring: '#FECACA' },
        pdf:    '#EF6E6E',
      },
      fontFamily: {
        sans: ['Inter', 'ui-sans-serif', 'system-ui', '-apple-system', 'Segoe UI', 'Roboto', 'Helvetica', 'Arial', 'sans-serif'],
      },
      borderRadius: {
        xl: '12px',
        '2xl': '16px',
        '3xl': '20px',
      },
      boxShadow: {
        card: '0 1px 2px rgba(16,24,40,0.04), 0 1px 3px rgba(16,24,40,0.06)',
        soft: '0 4px 16px rgba(16,24,40,0.06)',
        lift: '0 12px 32px rgba(16,24,40,0.12)',
        glow: '0 0 0 3px rgba(15,118,110,0.18)',
      },
      keyframes: {
        'fade-up': { '0%': { opacity: 0, transform: 'translateY(8px)' }, '100%': { opacity: 1, transform: 'translateY(0)' } },
        'pulse-ring': { '0%,100%': { opacity: 0.45 }, '50%': { opacity: 1 } },
        dash: { to: { 'stroke-dashoffset': '-24' } },
      },
      animation: {
        'fade-up': 'fade-up .4s cubic-bezier(.16,1,.3,1) both',
        'pulse-ring': 'pulse-ring 1.4s ease-in-out infinite',
        dash: 'dash 0.6s linear infinite',
      },
    },
  },
  plugins: [],
}
