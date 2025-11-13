/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        // Discord-like color scheme
        discord: {
          dark: '#202225',
          darkGray: '#2f3136',
          gray: '#36393f',
          lightGray: '#40444b',
          blurple: '#5865f2',
          green: '#3ba55c',
          yellow: '#faa61a',
          red: '#ed4245',
          white: '#ffffff',
          offWhite: '#dcddde',
          muted: '#72767d',
        },
      },
      fontFamily: {
        sans: ['Whitney', 'Helvetica Neue', 'Helvetica', 'Arial', 'sans-serif'],
      },
      animation: {
        'pulse-slow': 'pulse 3s cubic-bezier(0.4, 0, 0.6, 1) infinite',
      },
    },
  },
  plugins: [],
  darkMode: 'class',
};
