Enhanced Chat & Zombie Survival Game
A comprehensive real-time communication platform featuring chat rooms, user management, and an integrated multiplayer zombie survival game. This project combines a robust C# server with an interactive HTML5-based game client, allowing users to chat and play together.
Overview
This project provides a complete solution for real-time communication and gaming:

Enhanced Chat System: Multi-room chat with private messaging, shoutbox, and user management
Integrated Zombie Survival Game: Browser-based multiplayer game where players cooperate to survive waves of zombies
Account System: User registration, authentication, and persistent data storage
Credit System: Virtual currency that can be transferred between users

Technology Stack

Server: C# (.NET) with TCP socket implementation
Game Client: HTML5, CSS, JavaScript (Canvas API)
Persistence: JSON file-based storage for users, rooms, and game data
Communication Protocol: Custom TCP protocol with JSON message format

Features
Chat System

Multi-room Chat: Create, join, and participate in different chat rooms
Private Messaging: Direct communication between users
Shoutbox: Global announcements visible to all users
User Management: Registration, authentication, and profile management
Credit Transfer: Send credits to other users

Zombie Survival Game

Multiplayer Cooperation: Play with other connected users in real-time
Wave-based Zombie Combat: Fight increasingly difficult waves of zombies
Character Progression: Collect weapons and items to increase survival chances
Revival System: Help fallen teammates get back in the action
Leaderboards: Track top performers and statistics

Installation
Prerequisites

.NET 6.0 SDK or higher
Web browser with HTML5 support

Server Setup

Clone the repository:
Copygit clone https://github.com/yourusername/enhanced-chat-zombie-game.git

Navigate to the server directory:
Copycd enhanced-chat-zombie-game/server

Build the server:
Copydotnet build

Run the server:
Copydotnet run


Client Setup
The client-side game runs directly in the browser. You can:

Connect to the server using any modern web browser
Navigate to the provided URL (default: http://localhost:8080)

Usage Guide
Server Administration
The server console provides several administrative commands:

B - Broadcast a message to all users
L - List connected clients
G - List active games
N - Create a new game
Q - Shut down the server

Chat Commands
Users can interact with the system using these commands:

HELP - Display available commands
USERS - List all connected users
JOIN roomName - Join a chat room
LEAVE - Leave current room and return to lobby
WHISPER username message - Send a private message
SHOUT message - Post a message to the global shoutbox

Game Commands

LISTGAMES - List all active games
JOIN GAME gameId - Join a game by ID
LEAVE - Leave the current game

Game Controls

Movement: WASD or Arrow keys
Aim: Mouse cursor
Shoot: Left mouse button
Switch Weapon: Number keys (1-4) or click inventory items
Revive Teammate: Press E when close to fallen player

Architecture Overview
Server Components

ClientConnection: Manages individual client connections and state
Room System: Handles chat rooms and messaging
Game System: Coordinates game instances, player states, and zombie AI
User Management: Handles authentication, registration, and user data

Game Components

Game Loop: Updates game state and renders at 60 FPS
Physics System: Handles movement, collision detection, and combat
Rendering Engine: Canvas-based rendering of players, zombies, and environment
Networking: Real-time communication with server for synchronized gameplay

Development Roadmap

Additional Weapons and Items: Expand the arsenal available to players
More Zombie Types: Introduce additional enemy varieties with unique behaviors
Map Editor: Allow creating and sharing custom game maps
Mobile Support: Responsive design for mobile gameplay
Voice Chat Integration: Add real-time voice communication

Contributing
Contributions are welcome! Please feel free to submit a Pull Request.

Fork the repository
Create your feature branch (git checkout -b feature/amazing-feature)
Commit your changes (git commit -m 'Add some amazing feature')
Push to the branch (git push origin feature/amazing-feature)
Open a Pull Request

License
This project is licensed under the MIT License - see the LICENSE file for details.
