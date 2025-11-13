# YurtCord Frontend

Beautiful Discord-like communication platform built with React, TypeScript, and Tailwind CSS.

## 🎨 Features

- ✨ **Discord-inspired UI** - Familiar layout and design
- 🎭 **Smooth Animations** - Fade-ins, slides, and hover effects
- 🎨 **Beautiful Gradients** - Modern gradient backgrounds
- 🔐 **Authentication** - Login and registration with JWT
- 💬 **Real-time Chat** - Message display and sending
- 🖼️ **Rich Media** - Image attachments and embeds
- 😀 **Reactions** - Emoji reactions on messages
- 👥 **Member List** - Online/offline status indicators
- 🔊 **Voice Channels** - Voice channel UI (ready for WebRTC)
- 📱 **Responsive Design** - Works on all screen sizes

## 🚀 Quick Start

### Prerequisites

- Node.js 18+
- npm or yarn
- YurtCord Backend running (see Backend/README.md)

### Installation

```bash
# Install dependencies
npm install

# Start development server
npm run dev
```

The app will open at http://localhost:5173

### Build for Production

```bash
# Build optimized production bundle
npm run build

# Preview production build
npm run preview
```

## 🏗️ Project Structure

```
src/
├── components/          # Reusable UI components
│   ├── channels/       # Channel list components
│   ├── chat/           # Chat area and messages
│   ├── common/         # Common components (Spinner, etc)
│   └── servers/        # Server list components
├── pages/              # Page components
│   ├── HomePage.tsx    # Main app layout
│   ├── LoginPage.tsx   # Login form
│   └── RegisterPage.tsx # Registration form
├── store/              # Redux state management
│   ├── slices/         # Redux slices
│   │   ├── authSlice.ts       # Authentication state
│   │   ├── guildsSlice.ts     # Guilds/servers state
│   │   ├── channelsSlice.ts   # Channels state
│   │   └── messagesSlice.ts   # Messages state
│   ├── hooks.ts        # Typed Redux hooks
│   └── store.ts        # Store configuration
├── styles/             # Global styles
│   └── index.css       # Tailwind + custom styles
├── types/              # TypeScript definitions
│   └── index.ts        # Type definitions
├── App.tsx             # Main app component
└── main.tsx            # Entry point
```

## 🎨 Tech Stack

- **React 18** - UI library
- **TypeScript** - Type safety
- **Redux Toolkit** - State management
- **React Router** - Navigation
- **Tailwind CSS** - Styling
- **Axios** - HTTP client
- **SignalR** - Real-time messaging (ready)
- **Framer Motion** - Animations (available)
- **React Hot Toast** - Notifications

## 🎭 Components

### ServerList
Displays server icons in a vertical sidebar with hover tooltips and active indicators.

### ChannelList
Shows categorized text and voice channels with expand/collapse functionality.

### ChatArea
Main chat interface with message history, input box, and rich media display.

### MessageItem
Individual message component with avatar, content, attachments, reactions, and hover actions.

### MemberList
Right sidebar showing online and offline members with status indicators.

## 🔐 Authentication Flow

1. User visits app → Redirected to /login
2. User logs in → JWT token stored in localStorage
3. Token included in all API requests via Axios interceptor
4. Token checked on app load → Auto-login if valid

## 🌐 API Integration

The frontend connects to the backend API via environment variables:

```env
VITE_API_URL=http://localhost:5000
VITE_GATEWAY_URL=http://localhost:5000/gateway
```

Update these in `.env` file or environment variables.

## 🎨 Customization

### Change Theme Colors

Edit `tailwind.config.js`:

```js
theme: {
  extend: {
    colors: {
      discord: {
        blurple: '#YOUR_COLOR',  // Main accent color
        // ... other colors
      }
    }
  }
}
```

### Add Custom Animations

Edit `src/styles/index.css`:

```css
@keyframes your-animation {
  from { /* ... */ }
  to { /* ... */ }
}

.animate-your-animation {
  animation: your-animation 0.3s ease-out;
}
```

## 📝 Available Scripts

```bash
# Development server with hot reload
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview

# Run linter
npm run lint

# Format code with Prettier
npm run format

# Type checking
npm run type-check

# Run tests
npm run test
```

## 🐛 Common Issues

### Port 5173 already in use

```bash
# Change port in vite.config.ts
server: {
  port: 3000
}
```

### API connection failed

1. Ensure backend is running at http://localhost:5000
2. Check CORS is enabled in backend
3. Verify `.env` file has correct API URL

### Build errors

```bash
# Clear cache and reinstall
rm -rf node_modules package-lock.json
npm install
npm run build
```

## 🚀 Deployment

### Docker

```bash
# Build Docker image
docker build -t yurtcord-frontend .

# Run container
docker run -p 3000:80 yurtcord-frontend
```

### Nginx

```bash
# Build app
npm run build

# Copy dist/ to nginx web root
cp -r dist/* /var/www/html/
```

### Vercel/Netlify

```bash
# Install dependencies and build
npm install && npm run build

# Deploy dist/ folder
```

## 🎯 Keyboard Shortcuts

- `Ctrl/Cmd + K` - Quick switcher (coming soon)
- `Ctrl/Cmd + /` - Show shortcuts (coming soon)
- `Ctrl/Cmd + I` - Mark server as read (coming soon)
- `ESC` - Clear search/close modal

## 📚 Learn More

- [React Documentation](https://react.dev)
- [Redux Toolkit](https://redux-toolkit.js.org)
- [Tailwind CSS](https://tailwindcss.com)
- [TypeScript](https://www.typescriptlang.org)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests and linting
5. Submit a pull request

## 📄 License

MIT License - see LICENSE file for details

---

**Built with ❤️ by The404Studios**

For backend documentation, see [Backend/README.md](../Backend/README.md)
