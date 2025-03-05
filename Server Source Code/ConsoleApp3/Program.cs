using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EnhancedRelayServer
{
    class ClientConnection
    {
        public TcpClient Client { get; set; }
        public NetworkStream Stream { get; set; }
        public string Username { get; set; }
        public bool IsAuthenticated { get; set; }
        public DateTime ConnectedAt { get; set; }
        public string IPAddress { get; set; }
        public string CurrentRoom { get; set; } = "lobby";
    }

    class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public decimal Credits { get; set; } = 100.0m; // Default starting credits
        public DateTime RegisteredAt { get; set; }
        public DateTime LastLoginAt { get; set; }
        public string Email { get; set; }
        public List<TransactionRecord> TransactionHistory { get; set; } = new List<TransactionRecord>();
        public int GamesPlayed { get; set; } = 0;
        public int GamesWon { get; set; } = 0;
        public decimal TotalWinnings { get; set; } = 0m;
        public decimal TotalLosses { get; set; } = 0m;
        public DateTime LastGamePlayed { get; set; }
    }

    class TransactionRecord
    {
        public DateTime Timestamp { get; set; }
        public string FromUser { get; set; }
        public string ToUser { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }

    class Room
    {
        public string Name { get; set; }
        public string Owner { get; set; }
        public string Description { get; set; }
        public bool IsPrivate { get; set; }
        public List<string> AllowedUsers { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
    }

    class ShoutboxMessage
    {
        public string Username { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    class GamblingPot
    {
        public string Id { get; set; } = Guid.NewGuid().ToString().Substring(0, 8);
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal TotalAmount { get; set; } = 0m;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime EndsAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string WinnerUsername { get; set; }
        public Dictionary<string, decimal> Participants { get; set; } = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
    }

    class GamblingHistory
    {
        public string PotId { get; set; }
        public string PotName { get; set; }
        public decimal TotalAmount { get; set; }
        public string WinnerUsername { get; set; }
        public int ParticipantCount { get; set; }
        public DateTime CompletedAt { get; set; }
    }

    enum GameType
    {
        Dice,
        CoinFlip,
        SlotMachine
    }

    class GameResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString().Substring(0, 8);
        public GameType GameType { get; set; }
        public string Player { get; set; }
        public decimal BetAmount { get; set; }
        public decimal WinAmount { get; set; }
        public bool IsWin { get; set; }
        public object GameData { get; set; } // Game-specific data like dice numbers, etc.
        public DateTime PlayedAt { get; set; } = DateTime.Now;
    }

    class Program
    {
        // Server configuration
        private const int Port = 8080;
        private static TcpListener _listener;
        private static CancellationTokenSource _cts;
        private static readonly List<ClientConnection> _clients = new List<ClientConnection>();
        private static readonly Dictionary<string, User> _users = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Room> _rooms = new Dictionary<string, Room>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<ShoutboxMessage> _shoutboxMessages = new List<ShoutboxMessage>();
        private static readonly List<GamblingPot> _activePots = new List<GamblingPot>();
        private static readonly List<GamblingHistory> _gamblingHistory = new List<GamblingHistory>();
        private static readonly List<GameResult> _gameHistory = new List<GameResult>();
        private static readonly Dictionary<string, Timer> _potTimers = new Dictionary<string, Timer>();
        private static readonly Random _random = new Random();

        // Game settings
        private static readonly decimal _diceHouseEdge = 0.02m; // 2% house edge
        private static readonly decimal _slotMachineHouseEdge = 0.10m; // 10% house edge
        private static readonly decimal _maxBetMultiplier = 10.0m; // Maximum payout multiplier

        private static readonly object _lockClients = new object();
        private static readonly object _lockUsers = new object();
        private static readonly object _lockRooms = new object();
        private static readonly object _lockShoutbox = new object();
        private static readonly object _lockGambling = new object();

        // File paths for persistence
        private const string UsersFilePath = "users.json";
        private const string RoomsFilePath = "rooms.json";
        private const string ShoutboxFilePath = "shoutbox.json";
        private const string GamblingHistoryFilePath = "gambling_history.json";

        static async Task Main(string[] args)
        {
            Console.Title = "Enhanced Information Relay Server";
            _cts = new CancellationTokenSource();

            // Load data from disk
            LoadData();

            // Create default room if no rooms exist
            if (!_rooms.Any())
            {
                _rooms.Add("lobby", new Room
                {
                    Name = "lobby",
                    Owner = "system",
                    Description = "Main lobby for all users",
                    IsPrivate = false,
                    CreatedAt = DateTime.Now
                });

                SaveRooms();
            }

            try
            {
                // Start the server
                await StartServerAsync();

                // Create a default gambling pot 
                await CreateGamblingPotAsync("Daily Jackpot", "Daily jackpot that ends in 24 hours", 24 * 60);

                Console.WriteLine("Administrator commands:");
                Console.WriteLine("  B - Broadcast a message to all users");
                Console.WriteLine("  L - List connected clients");
                Console.WriteLine("  R - List all rooms");
                Console.WriteLine("  U - List all registered users");
                Console.WriteLine("  S - Send a shoutbox message");
                Console.WriteLine("  G - Gambling management");
                Console.WriteLine("  Q - Shut down the server");
                Console.WriteLine();

                while (true)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);

                        switch (key.Key)
                        {
                            case ConsoleKey.B:
                                Console.Write("Broadcast message: ");
                                string message = Console.ReadLine();
                                await BroadcastMessageAsync($"[SERVER ANNOUNCEMENT] {message}");
                                break;

                            case ConsoleKey.L:
                                ListConnectedClients();
                                break;

                            case ConsoleKey.R:
                                ListAllRooms();
                                break;

                            case ConsoleKey.U:
                                ListAllUsers();
                                break;

                            case ConsoleKey.S:
                                Console.Write("Shoutbox message: ");
                                string shoutMessage = Console.ReadLine();
                                await AddShoutboxMessageAsync("Admin", shoutMessage);
                                break;

                            case ConsoleKey.G:
                                await ShowGamblingAdminMenu();
                                break;

                            default:
                                break;
                        }
                    }

                    await Task.Delay(100);
                }

        
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server error: {ex.Message}");
            }
            finally
            {
                // Stop the server
                StopServer();
                Console.WriteLine("Server shutdown complete. Press any key to exit.");
                Console.ReadKey();
            }
        }

        #region Persistence Methods

        private static void LoadData()
        {
            // Load users
            if (File.Exists(UsersFilePath))
            {
                try
                {
                    string json = File.ReadAllText(UsersFilePath);
                    var users = JsonSerializer.Deserialize<List<User>>(json);
                    lock (_lockUsers)
                    {
                        _users.Clear();
                        foreach (var user in users)
                        {
                            _users[user.Username] = user;
                        }
                    }
                    Console.WriteLine($"Loaded {_users.Count} users from storage.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading users: {ex.Message}");
                }
            }

            // Load rooms
            if (File.Exists(RoomsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(RoomsFilePath);
                    var rooms = JsonSerializer.Deserialize<List<Room>>(json);
                    lock (_lockRooms)
                    {
                        _rooms.Clear();
                        foreach (var room in rooms)
                        {
                            _rooms[room.Name] = room;
                        }
                    }
                    Console.WriteLine($"Loaded {_rooms.Count} rooms from storage.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading rooms: {ex.Message}");
                }
            }

            // Load shoutbox
            if (File.Exists(ShoutboxFilePath))
            {
                try
                {
                    string json = File.ReadAllText(ShoutboxFilePath);
                    var messages = JsonSerializer.Deserialize<List<ShoutboxMessage>>(json);
                    lock (_lockShoutbox)
                    {
                        _shoutboxMessages.Clear();
                        _shoutboxMessages.AddRange(messages);
                    }
                    Console.WriteLine($"Loaded {_shoutboxMessages.Count} shoutbox messages from storage.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading shoutbox: {ex.Message}");
                }
            }

            // Load gambling history
            if (File.Exists(GamblingHistoryFilePath))
            {
                try
                {
                    string json = File.ReadAllText(GamblingHistoryFilePath);
                    var history = JsonSerializer.Deserialize<List<GamblingHistory>>(json);
                    lock (_lockGambling)
                    {
                        _gamblingHistory.Clear();
                        _gamblingHistory.AddRange(history);
                    }
                    Console.WriteLine($"Loaded {_gamblingHistory.Count} gambling history records from storage.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading gambling history: {ex.Message}");
                }
            }
        }

        private static void SaveUsers()
        {
            try
            {
                List<User> users;
                lock (_lockUsers)
                {
                    users = _users.Values.ToList();
                }

                string json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(UsersFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving users: {ex.Message}");
            }
        }

        private static void SaveRooms()
        {
            try
            {
                List<Room> rooms;
                lock (_lockRooms)
                {
                    rooms = _rooms.Values.ToList();
                }

                string json = JsonSerializer.Serialize(rooms, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(RoomsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving rooms: {ex.Message}");
            }
        }

        private static void SaveShoutbox()
        {
            try
            {
                List<ShoutboxMessage> messages;
                lock (_lockShoutbox)
                {
                    messages = _shoutboxMessages.ToList();
                }

                string json = JsonSerializer.Serialize(messages, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ShoutboxFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving shoutbox: {ex.Message}");
            }
        }

        private static void SaveGamblingHistory()
        {
            try
            {
                List<GamblingHistory> history;
                lock (_lockGambling)
                {
                    history = _gamblingHistory.ToList();
                }

                string json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(GamblingHistoryFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving gambling history: {ex.Message}");
            }
        }

        private static void SaveGameHistory()
        {
            try
            {
                List<GameResult> gameHistory;
                lock (_lockGambling)
                {
                    gameHistory = _gameHistory.ToList();
                }

                string json = JsonSerializer.Serialize(gameHistory, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText("game_history.json", json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving game history: {ex.Message}");
            }
        }

        #endregion

        #region Server Management

        private static async Task StartServerAsync()
        {
            // Start listening for client connections
            _listener = new TcpListener(IPAddress.Any, Port);
            _listener.Start();
            Console.WriteLine($"Server started on port {Port}");

            // Accept clients in a background task
            _ = Task.Run(async () =>
            {
                try
                {
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        TcpClient client = await _listener.AcceptTcpClientAsync();
                        _ = HandleClientAsync(client);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal cancellation, no need to report
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting clients: {ex.Message}");
                }
            }, _cts.Token);
        }

        private static void StopServer()
        {
            _cts.Cancel();

            // Disconnect all clients
            lock (_lockClients)
            {
                foreach (var client in _clients.ToList())
                {
                    try
                    {
                        SendToClientAsync(client, "Server is shutting down. Goodbye!").Wait();
                        client.Stream?.Close();
                        client.Client?.Close();
                    }
                    catch
                    {
                        // Ignore errors during shutdown
                    }
                }
                _clients.Clear();
            }

            // Save data
            SaveUsers();
            SaveRooms();
            SaveShoutbox();

            // Stop listening
            _listener?.Stop();
        }

        #endregion

        #region Client Connection Handling

        private static async Task HandleClientAsync(TcpClient client)
        {
            var clientConnection = new ClientConnection
            {
                Client = client,
                Stream = client.GetStream(),
                IsAuthenticated = false,
                ConnectedAt = DateTime.Now,
                IPAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString()
            };

            Console.WriteLine($"New connection from {clientConnection.IPAddress}");

            try
            {
                // Send welcome message
                await SendToClientAsync(clientConnection, "Welcome to the Enhanced Information Relay Server!");
                await SendToClientAsync(clientConnection, "Commands: LOGIN username password, REGISTER username password email");

                byte[] buffer = new byte[4096];

                while (client.Connected && !_cts.Token.IsCancellationRequested)
                {
                    if (client.Available > 0 || clientConnection.Stream.DataAvailable)
                    {
                        int bytesRead = await clientConnection.Stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
                        if (bytesRead <= 0)
                            break;  // Client disconnected

                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        // Process the message
                        if (!clientConnection.IsAuthenticated)
                        {
                            await ProcessAuthenticationAsync(clientConnection, message);
                        }
                        else
                        {
                            // Regular message from authenticated user
                            await ProcessMessageAsync(clientConnection, message);
                        }
                    }

                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client {clientConnection.IPAddress}: {ex.Message}");
            }
            finally
            {
                // Clean up
                DisconnectClient(clientConnection);
            }
        }

        private static async Task ProcessAuthenticationAsync(ClientConnection client, string message)
        {
            string[] parts = message.Trim().Split(' ');

            if (parts.Length >= 3 && parts[0].Equals("LOGIN", StringComparison.OrdinalIgnoreCase))
            {
                string username = parts[1];
                string password = parts[2];

                User user = null;
                lock (_lockUsers)
                {
                    _users.TryGetValue(username, out user);
                }

                if (user != null && user.Password == password)
                {
                    client.Username = username;
                    client.IsAuthenticated = true;
                    client.CurrentRoom = "lobby";

                    // Update last login time
                    lock (_lockUsers)
                    {
                        user.LastLoginAt = DateTime.Now;
                    }
                    SaveUsers();

                    // Add to connected clients
                    lock (_lockClients)
                    {
                        _clients.Add(client);
                    }

                    await SendToClientAsync(client, $"Authentication successful. Welcome, {username}!");
                    await SendToClientAsync(client, $"Your current credit balance: {user.Credits} credits");
                    await RoomMessageAsync("lobby", $"* {username} has joined the chat", client);
                    await SendToClientAsync(client, "Type 'HELP' for available commands.");

                    // Send recent shoutbox messages
                    await SendRecentShoutboxMessages(client);
                }
                else
                {
                    await SendToClientAsync(client, "Authentication failed. Please try again or REGISTER a new account.");
                }
            }
            else if (parts.Length >= 4 && parts[0].Equals("REGISTER", StringComparison.OrdinalIgnoreCase))
            {
                string username = parts[1];
                string password = parts[2];
                string email = parts[3];

                // Validate username (alphanumeric only)
                if (!username.All(char.IsLetterOrDigit))
                {
                    await SendToClientAsync(client, "Username must contain only letters and numbers.");
                    return;
                }

                bool userExists;
                lock (_lockUsers)
                {
                    userExists = _users.ContainsKey(username);
                }

                if (userExists)
                {
                    await SendToClientAsync(client, "Username already exists. Please choose another name.");
                }
                else
                {
                    // Create new user
                    var newUser = new User
                    {
                        Username = username,
                        Password = password,
                        Email = email,
                        RegisteredAt = DateTime.Now,
                        LastLoginAt = DateTime.Now,
                        Credits = 100.0m // Starting credits
                    };

                    lock (_lockUsers)
                    {
                        _users[username] = newUser;
                    }
                    SaveUsers();

                    await SendToClientAsync(client, $"Registration successful! You have been given 100 starting credits.");
                    await SendToClientAsync(client, "Please LOGIN with your new credentials.");
                }
            }
            else
            {
                await SendToClientAsync(client, "Available commands:");
                await SendToClientAsync(client, "  LOGIN username password - Log in to an existing account");
                await SendToClientAsync(client, "  REGISTER username password email - Create a new account");
            }
        }

        private static async Task ProcessMessageAsync(ClientConnection client, string message)
        {
            try
            {
                message = message.Trim();
                string[] parts = message.Split(new[] { ' ' }, 2);
                string command = parts[0].ToUpper();

                switch (command)
                {
                    case "HELP":
                        await SendHelpMessage(client);
                        break;

                    case "USERS":
                        await ListUsersAsync(client);
                        break;

                    case "BALANCE":
                        await CheckBalanceAsync(client);
                        break;

                    case "ROOMS":
                        await ListRoomsAsync(client);
                        break;

                    case "CREATEROOM":
                        if (parts.Length > 1)
                            await CreateRoomAsync(client, parts[1]);
                        else
                            await SendToClientAsync(client, "Usage: CREATEROOM roomName [description]");
                        break;

                    case "JOIN":
                        if (parts.Length > 1)
                            await JoinRoomAsync(client, parts[1]);
                        else
                            await SendToClientAsync(client, "Usage: JOIN roomName");
                        break;

                    case "LEAVE":
                        await LeaveRoomAsync(client);
                        break;

                    case "WHISPER":
                        if (parts.Length > 1)
                        {
                            string[] whisperParts = parts[1].Split(new[] { ' ' }, 2);
                            if (whisperParts.Length >= 2)
                            {
                                string targetUsername = whisperParts[0];
                                string privateMessage = whisperParts[1];
                                await SendPrivateMessageAsync(client, targetUsername, privateMessage);
                            }
                            else
                            {
                                await SendToClientAsync(client, "Usage: WHISPER username message");
                            }
                        }
                        else
                        {
                            await SendToClientAsync(client, "Usage: WHISPER username message");
                        }
                        break;

                    case "TRANSFER":
                        if (parts.Length > 1)
                        {
                            await ProcessTransferCommand(client, parts[1]);
                        }
                        else
                        {
                            await SendToClientAsync(client, "Usage: TRANSFER username amount [description]");
                        }
                        break;

                    case "SHOUT":
                        if (parts.Length > 1)
                        {
                            await AddShoutboxMessageAsync(client.Username, parts[1]);
                        }
                        else
                        {
                            await SendToClientAsync(client, "Usage: SHOUT message");
                        }
                        break;

                    case "SHOUTBOX":
                        await SendRecentShoutboxMessages(client);
                        break;

                    case "TRANSACTIONS":
                        await ListTransactionsAsync(client);
                        break;

                    // Gambling commands
                    case "GAMBLE":
                        if (parts.Length > 1)
                        {
                            await ProcessGambleCommand(client, parts[1]);
                        }
                        else
                        {
                            await SendToClientAsync(client, "Usage: GAMBLE bet amount - Places a bet in the active pot");
                        }
                        break;

                    case "POTS":
                        await ListActivePots(client);
                        break;

                    case "POTINFO":
                        if (parts.Length > 1)
                        {
                            await GetPotInfo(client, parts[1]);
                        }
                        else
                        {
                            await SendToClientAsync(client, "Usage: POTINFO potId");
                        }
                        break;

                    case "GAMBLINGHISTORY":
                        await ShowGamblingHistory(client);
                        break;

                    // Direct gambling games
                    case "DICE":
                        if (parts.Length > 1)
                        {
                            await PlayDiceGame(client, parts[1]);
                        }
                        else
                        {
                            await SendToClientAsync(client, "Usage: DICE amount [target] - Roll dice to win. Target (2-12) is optional.");
                        }
                        break;

                    case "FLIP":
                        if (parts.Length > 1)
                        {
                            await PlayCoinFlip(client, parts[1]);
                        }
                        else
                        {
                            await SendToClientAsync(client, "Usage: FLIP amount choice - Flip a coin. Choice must be HEADS or TAILS.");
                        }
                        break;

                    case "SLOTS":
                        if (parts.Length > 1)
                        {
                            await PlaySlotMachine(client, parts[1]);
                        }
                        else
                        {
                            await SendToClientAsync(client, "Usage: SLOTS amount - Play the slot machine.");
                        }
                        break;

                    case "STATS":
                        await ShowGamblingStats(client);
                        break;

                    case "LEADERBOARD":
                        await ShowLeaderboard(client);
                        break;

                    case "QUIT":
                        await SendToClientAsync(client, "Goodbye!");
                        DisconnectClient(client);
                        break;

                    default:
                        // Regular chat message to current room
                        await RoomMessageAsync(client.CurrentRoom, $"[{client.Username}] {message}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
                await SendToClientAsync(client, $"Error processing your request: {ex.Message}");
            }
        }

        private static async Task SendHelpMessage(ClientConnection client)
        {
            await SendToClientAsync(client, "Available commands:");
            await SendToClientAsync(client, "  HELP - Display this help message");
            await SendToClientAsync(client, "  USERS - List all connected users");
            await SendToClientAsync(client, "  BALANCE - Check your credit balance");
            await SendToClientAsync(client, "  ROOMS - List all available chat rooms");
            await SendToClientAsync(client, "  CREATEROOM name [description] - Create a new chat room");
            await SendToClientAsync(client, "  JOIN roomName - Join a chat room");
            await SendToClientAsync(client, "  LEAVE - Leave current room and return to lobby");
            await SendToClientAsync(client, "  WHISPER username message - Send a private message");
            await SendToClientAsync(client, "  TRANSFER username amount [description] - Send credits to another user");
            await SendToClientAsync(client, "  SHOUT message - Post a message to the global shoutbox");
            await SendToClientAsync(client, "  SHOUTBOX - View recent shoutbox messages");
            await SendToClientAsync(client, "  TRANSACTIONS - View your recent transactions");
            await SendToClientAsync(client, "");
            await SendToClientAsync(client, "Gambling commands:");
            await SendToClientAsync(client, "  GAMBLE bet amount - Place a bet in the active pot");
            await SendToClientAsync(client, "  POTS - List all active gambling pots");
            await SendToClientAsync(client, "  POTINFO potId - Get detailed information about a specific pot");
            await SendToClientAsync(client, "  GAMBLINGHISTORY - View recent gambling winners");
            await SendToClientAsync(client, "");
            await SendToClientAsync(client, "Casino games:");
            await SendToClientAsync(client, "  DICE amount [target] - Roll dice (target 2-12 is optional)");
            await SendToClientAsync(client, "  FLIP amount choice - Flip a coin (choice: HEADS or TAILS)");
            await SendToClientAsync(client, "  SLOTS amount - Play the slot machine");
            await SendToClientAsync(client, "  STATS - See your gambling statistics");
            await SendToClientAsync(client, "  LEADERBOARD - View top gamblers");
            await SendToClientAsync(client, "");
            await SendToClientAsync(client, "  QUIT - Disconnect from the server");
            await SendToClientAsync(client, "");
            await SendToClientAsync(client, "You can also just type a message to chat in your current room.");
        }

        #endregion

        #region Message Handling

        private static async Task SendToClientAsync(ClientConnection client, string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message + "\n");
                await client.Stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending to client {client.Username}: {ex.Message}");
                DisconnectClient(client);
            }
        }

        private static async Task BroadcastMessageAsync(string message, ClientConnection excludeClient = null)
        {
            Console.WriteLine($"BROADCAST: {message}");

            List<ClientConnection> clientsCopy;
            lock (_lockClients)
            {
                clientsCopy = _clients.Where(c => c.IsAuthenticated).ToList();
            }

            foreach (var client in clientsCopy)
            {
                if (excludeClient != null && client == excludeClient)
                    continue;

                try
                {
                    await SendToClientAsync(client, message);
                }
                catch
                {
                    // If we fail to send to one client, continue with others
                }
            }
        }

        private static async Task RoomMessageAsync(string roomName, string message, ClientConnection excludeClient = null)
        {
            Console.WriteLine($"ROOM {roomName}: {message}");

            List<ClientConnection> roomClients;
            lock (_lockClients)
            {
                roomClients = _clients.Where(c => c.IsAuthenticated &&
                                                 c.CurrentRoom.Equals(roomName, StringComparison.OrdinalIgnoreCase))
                                      .ToList();
            }

            foreach (var client in roomClients)
            {
                if (excludeClient != null && client == excludeClient)
                    continue;

                try
                {
                    await SendToClientAsync(client, message);
                }
                catch
                {
                    // If we fail to send to one client, continue with others
                }
            }
        }

        private static async Task SendPrivateMessageAsync(ClientConnection sender, string targetUsername, string message)
        {
            ClientConnection targetClient;
            lock (_lockClients)
            {
                targetClient = _clients.FirstOrDefault(c =>
                    c.IsAuthenticated &&
                    c.Username.Equals(targetUsername, StringComparison.OrdinalIgnoreCase));
            }

            if (targetClient != null)
            {
                await SendToClientAsync(targetClient, $"[PM from {sender.Username}] {message}");
                await SendToClientAsync(sender, $"[PM to {targetUsername}] {message}");
            }
            else
            {
                await SendToClientAsync(sender, $"User '{targetUsername}' not found or not online.");
            }
        }

        private static void DisconnectClient(ClientConnection client)
        {
            try
            {
                lock (_lockClients)
                {
                    _clients.Remove(client);
                }

                if (client.IsAuthenticated && !string.IsNullOrEmpty(client.Username))
                {
                    _ = RoomMessageAsync(client.CurrentRoom, $"* {client.Username} has left the chat");
                    Console.WriteLine($"Client {client.Username} disconnected");
                }
                else
                {
                    Console.WriteLine($"Unauthenticated client from {client.IPAddress} disconnected");
                }

                client.Stream?.Close();
                client.Client?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting client: {ex.Message}");
            }
        }

        #endregion

        #region Room Management

        private static async Task CreateRoomAsync(ClientConnection client, string parameters)
        {
            string[] parts = parameters.Split(new[] { ' ' }, 2);
            string roomName = parts[0];
            string description = parts.Length > 1 ? parts[1] : $"{roomName} chat room";

            // Validate room name (alphanumeric plus some special chars)
            if (!roomName.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
            {
                await SendToClientAsync(client, "Room name must contain only letters, numbers, underscores, and hyphens.");
                return;
            }

            bool roomExists;
            lock (_lockRooms)
            {
                roomExists = _rooms.ContainsKey(roomName);
            }

            if (roomExists)
            {
                await SendToClientAsync(client, $"Room '{roomName}' already exists. Please choose another name.");
                return;
            }

            var newRoom = new Room
            {
                Name = roomName,
                Owner = client.Username,
                Description = description,
                IsPrivate = false,
                CreatedAt = DateTime.Now
            };

            lock (_lockRooms)
            {
                _rooms[roomName] = newRoom;
            }
            SaveRooms();

            await SendToClientAsync(client, $"Room '{roomName}' created successfully!");

            // Automatically join the new room
            await JoinRoomAsync(client, roomName);
        }

        private static async Task JoinRoomAsync(ClientConnection client, string roomName)
        {
            Room room;
            lock (_lockRooms)
            {
                _rooms.TryGetValue(roomName, out room);
            }

            if (room == null)
            {
                await SendToClientAsync(client, $"Room '{roomName}' does not exist.");
                return;
            }

            if (room.IsPrivate && room.Owner != client.Username && !room.AllowedUsers.Contains(client.Username))
            {
                await SendToClientAsync(client, $"Room '{roomName}' is private and you don't have access.");
                return;
            }

            // Leave current room
            if (!string.IsNullOrEmpty(client.CurrentRoom))
            {
                await RoomMessageAsync(client.CurrentRoom, $"* {client.Username} has left the room");
            }

            // Join new room
            client.CurrentRoom = roomName;
            await RoomMessageAsync(roomName, $"* {client.Username} has joined the room");
            await SendToClientAsync(client, $"You have joined room: {roomName}");
            await SendToClientAsync(client, $"Description: {room.Description}");
            await SendToClientAsync(client, $"Created by: {room.Owner} on {room.CreatedAt}");
        }

        private static async Task LeaveRoomAsync(ClientConnection client)
        {
            if (client.CurrentRoom.Equals("lobby", StringComparison.OrdinalIgnoreCase))
            {
                await SendToClientAsync(client, "You are already in the lobby.");
                return;
            }

            string oldRoom = client.CurrentRoom;
            await RoomMessageAsync(oldRoom, $"* {client.Username} has left the room");

            client.CurrentRoom = "lobby";
            await RoomMessageAsync("lobby", $"* {client.Username} has joined the lobby");
            await SendToClientAsync(client, "You have returned to the lobby.");
        }

        private static async Task ListRoomsAsync(ClientConnection client)
        {
            List<Room> roomsList;
            lock (_lockRooms)
            {
                roomsList = _rooms.Values.ToList();
            }

            await SendToClientAsync(client, $"Available Rooms ({roomsList.Count}):");
            foreach (var room in roomsList)
            {
                string status = room.IsPrivate ? "[Private]" : "[Public]";
                await SendToClientAsync(client, $"  {room.Name} {status} - {room.Description} (Owner: {room.Owner})");
            }
            await SendToClientAsync(client, "Use JOIN roomName to enter a room.");
        }

        #endregion

        #region User Management

        private static async Task ListUsersAsync(ClientConnection client)
        {
            List<ClientConnection> onlineUsers;
            lock (_lockClients)
            {
                onlineUsers = _clients.Where(c => c.IsAuthenticated).ToList();
            }

            await SendToClientAsync(client, $"Online Users ({onlineUsers.Count}):");
            foreach (var user in onlineUsers)
            {
                await SendToClientAsync(client, $"  {user.Username} (in room: {user.CurrentRoom})");
            }

            await SendToClientAsync(client, "Use WHISPER username message to send a private message.");
        }

        private static void ListConnectedClients()
        {
            lock (_lockClients)
            {
                Console.WriteLine("\nConnected clients:");
                Console.WriteLine("------------------");

                if (_clients.Count == 0)
                {
                    Console.WriteLine("No clients connected.");
                }
                else
                {
                    foreach (var client in _clients)
                    {
                        TimeSpan duration = DateTime.Now - client.ConnectedAt;
                        Console.WriteLine($"- {client.Username} (IP: {client.IPAddress}, Room: {client.CurrentRoom}, Connected: {duration.TotalMinutes:0.0} minutes)");
                    }
                }

                Console.WriteLine();
            }
        }

        private static void ListAllUsers()
        {
            lock (_lockUsers)
            {
                Console.WriteLine("\nRegistered Users:");
                Console.WriteLine("----------------");

                if (_users.Count == 0)
                {
                    Console.WriteLine("No users registered.");
                }
                else
                {
                    foreach (var user in _users.Values)
                    {
                        Console.WriteLine($"- {user.Username} (Email: {user.Email}, Credits: {user.Credits}, Registered: {user.RegisteredAt})");
                    }
                }

                Console.WriteLine();
            }
        }

        private static void ListAllRooms()
        {
            lock (_lockRooms)
            {
                Console.WriteLine("\nChat Rooms:");
                Console.WriteLine("----------");

                if (_rooms.Count == 0)
                {
                    Console.WriteLine("No rooms created.");
                }
                else
                {
                    foreach (var room in _rooms.Values)
                    {
                        string privacy = room.IsPrivate ? "Private" : "Public";
                        Console.WriteLine($"- {room.Name} ({privacy}) - {room.Description}");
                        Console.WriteLine($"  Owner: {room.Owner}, Created: {room.CreatedAt}");
                    }
                }

                Console.WriteLine();
            }
        }

        #endregion

        #region Credit System

        private static async Task ProcessTransferCommand(ClientConnection sender, string parameters)
        {
            string[] parts = parameters.Split(new[] { ' ' }, 3);

            if (parts.Length < 2)
            {
                await SendToClientAsync(sender, "Usage: TRANSFER username amount [description]");
                return;
            }

            string targetUsername = parts[0];
            string description = parts.Length > 2 ? parts[2] : "Credit transfer";

            if (!decimal.TryParse(parts[1], out decimal amount) || amount <= 0)
            {
                await SendToClientAsync(sender, "Please specify a valid positive amount to transfer.");
                return;
            }

            // Round to 2 decimal places
            amount = Math.Round(amount, 2);

            User senderUser = null;
            User targetUser = null;

            lock (_lockUsers)
            {
                _users.TryGetValue(sender.Username, out senderUser);
                _users.TryGetValue(targetUsername, out targetUser);
            }

            if (targetUser == null)
            {
                await SendToClientAsync(sender, $"User '{targetUsername}' not found.");
                return;
            }

            if (senderUser.Username == targetUser.Username)
            {
                await SendToClientAsync(sender, "You cannot transfer credits to yourself.");
                return;
            }

            if (senderUser.Credits < amount)
            {
                await SendToClientAsync(sender, $"Insufficient credits. Your balance: {senderUser.Credits}");
                return;
            }

            // Process the transfer
            lock (_lockUsers)
            {
                senderUser.Credits -= amount;
                targetUser.Credits += amount;

                // Record the transaction in both user's history
                var transaction = new TransactionRecord
                {
                    Timestamp = DateTime.Now,
                    FromUser = senderUser.Username,
                    ToUser = targetUser.Username,
                    Amount = amount,
                    Description = description
                };

                senderUser.TransactionHistory.Add(transaction);
                targetUser.TransactionHistory.Add(transaction);
            }

            SaveUsers();

            await SendToClientAsync(sender, $"Successfully transferred {amount} credits to {targetUsername}.");
            await SendToClientAsync(sender, $"Your new balance: {senderUser.Credits} credits");

            // Notify the recipient if they're online
            ClientConnection targetClient;
            lock (_lockClients)
            {
                targetClient = _clients.FirstOrDefault(c =>
                    c.IsAuthenticated &&
                    c.Username.Equals(targetUsername, StringComparison.OrdinalIgnoreCase));
            }

            if (targetClient != null)
            {
                await SendToClientAsync(targetClient, $"You received {amount} credits from {sender.Username}.");
                await SendToClientAsync(targetClient, $"Description: \"{description}\"");
                await SendToClientAsync(targetClient, $"Your new balance: {targetUser.Credits} credits");
            }
        }

        private static async Task CheckBalanceAsync(ClientConnection client)
        {
            User user = null;
            lock (_lockUsers)
            {
                _users.TryGetValue(client.Username, out user);
            }

            if (user != null)
            {
                await SendToClientAsync(client, $"Your current credit balance: {user.Credits}");
            }
            else
            {
                await SendToClientAsync(client, "Error retrieving your account information.");
            }
        }

        private static async Task ListTransactionsAsync(ClientConnection client)
        {
            User user = null;
            lock (_lockUsers)
            {
                _users.TryGetValue(client.Username, out user);
            }

            if (user != null && user.TransactionHistory.Any())
            {
                await SendToClientAsync(client, "Your recent transactions (most recent first):");

                // Get the 10 most recent transactions
                var recentTransactions = user.TransactionHistory
                    .OrderByDescending(t => t.Timestamp)
                    .Take(10);

                foreach (var tx in recentTransactions)
                {
                    string direction = tx.FromUser == user.Username ? "To" : "From";
                    string otherUser = tx.FromUser == user.Username ? tx.ToUser : tx.FromUser;
                    string sign = tx.FromUser == user.Username ? "-" : "+";

                    await SendToClientAsync(client,
                        $"[{tx.Timestamp.ToString("yyyy-MM-dd HH:mm")}] {sign}{tx.Amount} credits {direction} {otherUser} - {tx.Description}");
                }
            }
            else
            {
                await SendToClientAsync(client, "You have no transaction history.");
            }
        }

        #endregion

        #region Shoutbox

        private static async Task AddShoutboxMessageAsync(string username, string message)
        {
            var shoutMessage = new ShoutboxMessage
            {
                Username = username,
                Message = message,
                Timestamp = DateTime.Now
            };

            lock (_lockShoutbox)
            {
                _shoutboxMessages.Add(shoutMessage);

                // Keep only the 50 most recent messages
                if (_shoutboxMessages.Count > 50)
                {
                    _shoutboxMessages.RemoveAt(0);
                }
            }

            SaveShoutbox();

            // Broadcast to all connected clients
            await BroadcastMessageAsync($"[SHOUTBOX] {username}: {message}");
        }

        private static async Task SendRecentShoutboxMessages(ClientConnection client)
        {
            List<ShoutboxMessage> recentMessages;
            lock (_lockShoutbox)
            {
                // Get the 10 most recent messages in chronological order
                recentMessages = _shoutboxMessages
                    .OrderByDescending(m => m.Timestamp)
                    .Take(10)
                    .Reverse()
                    .ToList();
            }

            if (recentMessages.Any())
            {
                await SendToClientAsync(client, "--- Recent Shoutbox Messages ---");
                foreach (var msg in recentMessages)
                {
                    await SendToClientAsync(client,
                        $"[{msg.Timestamp.ToString("MM-dd HH:mm")}] {msg.Username}: {msg.Message}");
                }
                await SendToClientAsync(client, "------------------------------");
            }
            else
            {
                await SendToClientAsync(client, "No recent shoutbox messages.");
            }
        }

        #endregion

        #region Gambling System

        #region Casino Games

        private static async Task PlayDiceGame(ClientConnection client, string parameters)
        {
            string[] parts = parameters.Split(' ');

            if (parts.Length < 1)
            {
                await SendToClientAsync(client, "Usage: DICE amount [target] - Roll dice with optional target number (2-12)");
                return;
            }

            // Parse bet amount
            if (!decimal.TryParse(parts[0], out decimal betAmount) || betAmount <= 0)
            {
                await SendToClientAsync(client, "Please specify a valid positive amount to bet.");
                return;
            }

            // Round to 2 decimal places
            betAmount = Math.Round(betAmount, 2);

            // Parse target number if provided
            int targetNumber = 7; // Default target
            bool customTarget = false;

            if (parts.Length > 1 && int.TryParse(parts[1], out int userTarget) && userTarget >= 2 && userTarget <= 12)
            {
                targetNumber = userTarget;
                customTarget = true;
            }

            // Get the user
            User user;
            lock (_lockUsers)
            {
                _users.TryGetValue(client.Username, out user);
            }

            if (user == null)
            {
                await SendToClientAsync(client, "Error retrieving your user account.");
                return;
            }

            if (user.Credits < betAmount)
            {
                await SendToClientAsync(client, $"Insufficient credits. Your balance: {user.Credits}");
                return;
            }

            // Roll the dice
            int die1 = _random.Next(1, 7);
            int die2 = _random.Next(1, 7);
            int rollTotal = die1 + die2;

            // Determine win/loss and payout
            bool isWin = false;
            decimal winAmount = 0;
            string resultMessage;

            if (customTarget)
            {
                // Custom target mode - player wins if they roll their chosen number
                isWin = (rollTotal == targetNumber);

                // Calculate payout based on probability
                decimal payoutMultiplier = GetDicePayoutMultiplier(targetNumber);
                winAmount = betAmount * payoutMultiplier;

                if (isWin)
                {
                    resultMessage = $"You rolled {die1} and {die2} for a total of {rollTotal}, matching your target of {targetNumber}! You win {winAmount} credits!";
                }
                else
                {
                    resultMessage = $"You rolled {die1} and {die2} for a total of {rollTotal}. Your target was {targetNumber}. Better luck next time!";
                }
            }
            else
            {
                // Default mode - 7 or 11 wins, 2, 3, or 12 loses, anything else returns your bet
                if (rollTotal == 7 || rollTotal == 11)
                {
                    isWin = true;
                    winAmount = betAmount * 1.9m; // 1.9x payout for 7 or 11 (with house edge)
                    resultMessage = $"You rolled {die1} and {die2} for a total of {rollTotal}. Winner! You win {winAmount} credits!";
                }
                else if (rollTotal == 2 || rollTotal == 3 || rollTotal == 12)
                {
                    isWin = false;
                    winAmount = 0;
                    resultMessage = $"You rolled {die1} and {die2} for a total of {rollTotal}. Snake eyes! You lose {betAmount} credits.";
                }
                else
                {
                    isWin = false;
                    winAmount = betAmount * 0.5m; // Return half your bet
                    resultMessage = $"You rolled {die1} and {die2} for a total of {rollTotal}. Push! You get {winAmount} credits back.";
                }
            }

            // Update user credits
            lock (_lockUsers)
            {
                // Deduct bet
                user.Credits -= betAmount;

                // Add winnings if any
                if (winAmount > 0)
                {
                    user.Credits += winAmount;
                }

                // Update stats
                user.GamesPlayed++;
                user.LastGamePlayed = DateTime.Now;

                if (isWin)
                {
                    user.GamesWon++;
                    user.TotalWinnings += winAmount;
                }
                else
                {
                    user.TotalLosses += betAmount - winAmount; // Account for partial returns
                }

                // Record transaction
                var transaction = new TransactionRecord
                {
                    Timestamp = DateTime.Now,
                    FromUser = isWin ? "Casino" : user.Username,
                    ToUser = isWin ? user.Username : "Casino",
                    Amount = isWin ? winAmount : betAmount,
                    Description = $"Dice game: Rolled {die1}+{die2}={rollTotal}"
                };

                user.TransactionHistory.Add(transaction);
            }

            SaveUsers();

            // Record game result
            var gameResult = new GameResult
            {
                GameType = GameType.Dice,
                Player = client.Username,
                BetAmount = betAmount,
                WinAmount = winAmount,
                IsWin = isWin,
                GameData = new { Die1 = die1, Die2 = die2, Total = rollTotal, Target = targetNumber }
            };

            lock (_lockGambling)
            {
                _gameHistory.Add(gameResult);
            }

            SaveGameHistory();

            // Send result to user
            await SendToClientAsync(client, $"🎲 {resultMessage}");
            await SendToClientAsync(client, $"Your new balance: {user.Credits} credits");

            // Announce big wins to everyone
            if (isWin && winAmount >= 100)
            {
                await BroadcastMessageAsync($"[GAMBLING] {client.Username} just won {winAmount} credits playing dice!");
            }
        }

        private static decimal GetDicePayoutMultiplier(int target)
        {
            // Calculate payout based on probability of rolling this number
            // There are 36 possible combinations with two dice
            decimal probability = 0;

            switch (target)
            {
                case 2: // 1+1 (1 way)
                case 12: // 6+6 (1 way)
                    probability = 1.0m / 36.0m;
                    break;
                case 3: // 1+2, 2+1 (2 ways)
                case 11: // 5+6, 6+5 (2 ways)
                    probability = 2.0m / 36.0m;
                    break;
                case 4: // 1+3, 2+2, 3+1 (3 ways)
                case 10: // 4+6, 5+5, 6+4 (3 ways)
                    probability = 3.0m / 36.0m;
                    break;
                case 5: // 1+4, 2+3, 3+2, 4+1 (4 ways)
                case 9: // 3+6, 4+5, 5+4, 6+3 (4 ways)
                    probability = 4.0m / 36.0m;
                    break;
                case 6: // 1+5, 2+4, 3+3, 4+2, 5+1 (5 ways)
                case 8: // 2+6, 3+5, 4+4, 5+3, 6+2 (5 ways)
                    probability = 5.0m / 36.0m;
                    break;
                case 7: // 1+6, 2+5, 3+4, 4+3, 5+2, 6+1 (6 ways)
                    probability = 6.0m / 36.0m;
                    break;
                default:
                    probability = 1.0m / 36.0m; // Fallback
                    break;
            }

            // Calculate fair payout (1/probability)
            decimal fairPayout = 1.0m / probability;

            // Apply house edge
            decimal actualPayout = fairPayout * (1.0m - _diceHouseEdge);

            // Cap the maximum payout
            return Math.Min(actualPayout, _maxBetMultiplier);
        }

        private static async Task PlayCoinFlip(ClientConnection client, string parameters)
        {
            string[] parts = parameters.Split(' ');

            if (parts.Length < 2)
            {
                await SendToClientAsync(client, "Usage: FLIP amount choice - Flip a coin. Choice must be HEADS or TAILS.");
                return;
            }

            // Parse bet amount
            if (!decimal.TryParse(parts[0], out decimal betAmount) || betAmount <= 0)
            {
                await SendToClientAsync(client, "Please specify a valid positive amount to bet.");
                return;
            }

            // Round to 2 decimal places
            betAmount = Math.Round(betAmount, 2);

            // Parse choice
            string choice = parts[1].ToUpper();
            if (choice != "HEADS" && choice != "TAILS")
            {
                await SendToClientAsync(client, "Your choice must be either HEADS or TAILS.");
                return;
            }

            // Get the user
            User user;
            lock (_lockUsers)
            {
                _users.TryGetValue(client.Username, out user);
            }

            if (user == null)
            {
                await SendToClientAsync(client, "Error retrieving your user account.");
                return;
            }

            if (user.Credits < betAmount)
            {
                await SendToClientAsync(client, $"Insufficient credits. Your balance: {user.Credits}");
                return;
            }

            // Flip the coin
            bool isHeads = _random.Next(2) == 0;
            string result = isHeads ? "HEADS" : "TAILS";

            // Check if the player won
            bool isWin = (choice == result);
            decimal winAmount = isWin ? betAmount * 1.95m : 0; // 1.95x payout for a correct guess (5% house edge)

            // Update user credits
            lock (_lockUsers)
            {
                // Deduct bet
                user.Credits -= betAmount;

                // Add winnings if any
                if (winAmount > 0)
                {
                    user.Credits += winAmount;
                }

                // Update stats
                user.GamesPlayed++;
                user.LastGamePlayed = DateTime.Now;

                if (isWin)
                {
                    user.GamesWon++;
                    user.TotalWinnings += winAmount;
                }
                else
                {
                    user.TotalLosses += betAmount;
                }

                // Record transaction
                var transaction = new TransactionRecord
                {
                    Timestamp = DateTime.Now,
                    FromUser = isWin ? "Casino" : user.Username,
                    ToUser = isWin ? user.Username : "Casino",
                    Amount = isWin ? winAmount : betAmount,
                    Description = $"Coin flip: {choice} vs {result}"
                };

                user.TransactionHistory.Add(transaction);
            }

            SaveUsers();

            // Record game result
            var gameResult = new GameResult
            {
                GameType = GameType.CoinFlip,
                Player = client.Username,
                BetAmount = betAmount,
                WinAmount = winAmount,
                IsWin = isWin,
                GameData = new { PlayerChoice = choice, Result = result }
            };

            lock (_lockGambling)
            {
                _gameHistory.Add(gameResult);
            }

            SaveGameHistory();

            // Prepare result message
            string resultMessage;
            if (isWin)
            {
                resultMessage = $"The coin shows {result}! Your choice of {choice} was correct! You win {winAmount} credits!";
            }
            else
            {
                resultMessage = $"The coin shows {result}! Your choice of {choice} was incorrect. You lose {betAmount} credits.";
            }

            // Send result to user
            await SendToClientAsync(client, $"🪙 {resultMessage}");
            await SendToClientAsync(client, $"Your new balance: {user.Credits} credits");

            // Announce big wins to everyone
            if (isWin && winAmount >= 100)
            {
                await BroadcastMessageAsync($"[GAMBLING] {client.Username} just won {winAmount} credits on a coin flip!");
            }
        }

        private static async Task PlaySlotMachine(ClientConnection client, string parameters)
        {
            // Parse bet amount
            if (!decimal.TryParse(parameters, out decimal betAmount) || betAmount <= 0)
            {
                await SendToClientAsync(client, "Please specify a valid positive amount to bet.");
                return;
            }

            // Round to 2 decimal places
            betAmount = Math.Round(betAmount, 2);

            // Get the user
            User user;
            lock (_lockUsers)
            {
                _users.TryGetValue(client.Username, out user);
            }

            if (user == null)
            {
                await SendToClientAsync(client, "Error retrieving your user account.");
                return;
            }

            if (user.Credits < betAmount)
            {
                await SendToClientAsync(client, $"Insufficient credits. Your balance: {user.Credits}");
                return;
            }

            // Define slot symbols and their probabilities
            string[] symbols = { "🍒", "🍋", "🍊", "🍉", "🍇", "💎", "7️⃣", "🎰" };

            // Define multipliers for each combination
            Dictionary<string, decimal> multipliers = new Dictionary<string, decimal>
            {
                { "🍒🍒🍒", 3.0m },
                { "🍋🍋🍋", 5.0m },
                { "🍊🍊🍊", 8.0m },
                { "🍉🍉🍉", 10.0m },
                { "🍇🍇🍇", 15.0m },
                { "💎💎💎", 25.0m },
                { "7️⃣7️⃣7️⃣", 50.0m },
                { "🎰🎰🎰", 100.0m }
            };

            // Spin the reels
            string reel1 = symbols[_random.Next(symbols.Length)];
            string reel2 = symbols[_random.Next(symbols.Length)];
            string reel3 = symbols[_random.Next(symbols.Length)];

            string combination = $"{reel1}{reel2}{reel3}";

            // Check for wins
            bool isWin = false;
            decimal winAmount = 0;

            if (reel1 == reel2 && reel2 == reel3 && multipliers.ContainsKey(combination))
            {
                isWin = true;
                winAmount = betAmount * multipliers[combination];
            }

            // Special case for two matching symbols (small win)
            if (!isWin && (reel1 == reel2 || reel2 == reel3 || reel1 == reel3))
            {
                isWin = true;
                winAmount = betAmount * 0.5m; // Return half the bet
            }

            // Update user credits
            lock (_lockUsers)
            {
                // Deduct bet
                user.Credits -= betAmount;

                // Add winnings if any
                if (winAmount > 0)
                {
                    user.Credits += winAmount;
                }

                // Update stats
                user.GamesPlayed++;
                user.LastGamePlayed = DateTime.Now;

                if (isWin)
                {
                    user.GamesWon++;
                    user.TotalWinnings += winAmount;
                }
                else
                {
                    user.TotalLosses += betAmount;
                }

                // Record transaction
                var transaction = new TransactionRecord
                {
                    Timestamp = DateTime.Now,
                    FromUser = isWin ? "Casino" : user.Username,
                    ToUser = isWin ? user.Username : "Casino",
                    Amount = isWin ? winAmount : betAmount,
                    Description = $"Slot machine: {reel1} {reel2} {reel3}"
                };

                user.TransactionHistory.Add(transaction);
            }

            SaveUsers();

            // Record game result
            var gameResult = new GameResult
            {
                GameType = GameType.SlotMachine,
                Player = client.Username,
                BetAmount = betAmount,
                WinAmount = winAmount,
                IsWin = isWin,
                GameData = new { Reel1 = reel1, Reel2 = reel2, Reel3 = reel3 }
            };

            lock (_lockGambling)
            {
                _gameHistory.Add(gameResult);
            }

            SaveGameHistory();

            // Send the result to the user
            await SendToClientAsync(client, "🎰 Spinning the reels...");
            await SendToClientAsync(client, $"[ {reel1} | {reel2} | {reel3} ]");

            if (isWin)
            {
                if (reel1 == reel2 && reel2 == reel3)
                {
                    await SendToClientAsync(client, $"JACKPOT! Three {reel1} symbols! You win {winAmount} credits!");
                }
                else
                {
                    await SendToClientAsync(client, $"Two matching symbols! You win {winAmount} credits!");
                }
            }
            else
            {
                await SendToClientAsync(client, $"No match. You lose {betAmount} credits.");
            }

            await SendToClientAsync(client, $"Your new balance: {user.Credits} credits");

            // Announce big wins to everyone
            if (isWin && winAmount >= 100)
            {
                await BroadcastMessageAsync($"[GAMBLING] {client.Username} just won {winAmount} credits on the slot machine with {reel1}{reel2}{reel3}!");
            }
        }

        private static async Task ShowGamblingStats(ClientConnection client)
        {
            User user;
            lock (_lockUsers)
            {
                _users.TryGetValue(client.Username, out user);
            }

            if (user == null)
            {
                await SendToClientAsync(client, "Error retrieving your user account.");
                return;
            }

            await SendToClientAsync(client, "=== Your Gambling Statistics ===");
            await SendToClientAsync(client, $"Games played: {user.GamesPlayed}");
            await SendToClientAsync(client, $"Games won: {user.GamesWon} ({(user.GamesPlayed > 0 ? (user.GamesWon * 100.0 / user.GamesPlayed).ToString("F1") : "0")}%)");
            await SendToClientAsync(client, $"Total winnings: {user.TotalWinnings} credits");
            await SendToClientAsync(client, $"Total losses: {user.TotalLosses} credits");
            await SendToClientAsync(client, $"Net profit/loss: {user.TotalWinnings - user.TotalLosses} credits");

            if (user.LastGamePlayed > DateTime.MinValue)
            {
                await SendToClientAsync(client, $"Last game played: {user.LastGamePlayed}");
            }

            // Calculate recent performance (last 10 games)
            var recentGames = _gameHistory.Where(g => g.Player == client.Username).OrderByDescending(g => g.PlayedAt).Take(10).ToList();

            if (recentGames.Any())
            {
                int recentWins = recentGames.Count(g => g.IsWin);
                decimal recentProfit = recentGames.Sum(g => g.IsWin ? g.WinAmount - g.BetAmount : -g.BetAmount);

                await SendToClientAsync(client, $"Recent performance (last {recentGames.Count} games):");
                await SendToClientAsync(client, $"  Wins: {recentWins} ({(recentWins * 100.0 / recentGames.Count).ToString("F1")}%)");
                await SendToClientAsync(client, $"  Profit/Loss: {recentProfit} credits");
            }
        }

        private static async Task ShowLeaderboard(ClientConnection client)
        {
            List<User> users;
            lock (_lockUsers)
            {
                users = _users.Values.Where(u => u.GamesPlayed > 0).ToList();
            }

            if (!users.Any())
            {
                await SendToClientAsync(client, "No gambling statistics available yet.");
                return;
            }

            await SendToClientAsync(client, "=== Gambling Leaderboard ===");

            // Top winners by amount
            await SendToClientAsync(client, "Top Winners (by amount):");
            var topWinners = users.OrderByDescending(u => u.TotalWinnings).Take(5).ToList();
            for (int i = 0; i < topWinners.Count; i++)
            {
                await SendToClientAsync(client, $"{i + 1}. {topWinners[i].Username} - {topWinners[i].TotalWinnings} credits");
            }

            // Top winners by win rate (min 10 games)
            await SendToClientAsync(client, "Top Win Rates (minimum 10 games):");
            var topRates = users.Where(u => u.GamesPlayed >= 10)
                                .OrderByDescending(u => (double)u.GamesWon / u.GamesPlayed)
                                .Take(5).ToList();

            for (int i = 0; i < topRates.Count; i++)
            {
                double winRate = (double)topRates[i].GamesWon / topRates[i].GamesPlayed * 100;
                await SendToClientAsync(client, $"{i + 1}. {topRates[i].Username} - {winRate.ToString("F1")}% ({topRates[i].GamesWon}/{topRates[i].GamesPlayed})");
            }

            // Most active players
            await SendToClientAsync(client, "Most Active Players:");
            var mostActive = users.OrderByDescending(u => u.GamesPlayed).Take(5).ToList();
            for (int i = 0; i < mostActive.Count; i++)
            {
                await SendToClientAsync(client, $"{i + 1}. {mostActive[i].Username} - {mostActive[i].GamesPlayed} games");
            }

            // Recent big winners
            await SendToClientAsync(client, "Recent Big Wins:");
            var bigWins = _gameHistory.Where(g => g.IsWin && g.WinAmount >= 100)
                                    .OrderByDescending(g => g.PlayedAt)
                                    .Take(5);

            foreach (var win in bigWins)
            {
                string gameType = win.GameType.ToString();
                await SendToClientAsync(client, $"{win.Player} won {win.WinAmount} credits playing {gameType} at {win.PlayedAt.ToString("MM/dd HH:mm")}");
            }
        }

        #endregion

        private static async Task ShowGamblingAdminMenu()
        {
            Console.WriteLine("\nGambling Administration");
            Console.WriteLine("----------------------");
            Console.WriteLine("1. Create new gambling pot");
            Console.WriteLine("2. List active pots");
            Console.WriteLine("3. End pot early (pick winner now)");
            Console.WriteLine("4. View gambling history");
            Console.WriteLine("5. Back to main menu");
            Console.Write("\nSelect option (1-5): ");

            string option = Console.ReadLine();

            switch (option)
            {
                case "1":
                    Console.Write("Pot name: ");
                    string name = Console.ReadLine();

                    Console.Write("Description: ");
                    string description = Console.ReadLine();

                    Console.Write("Duration in minutes: ");
                    if (int.TryParse(Console.ReadLine(), out int minutes) && minutes > 0)
                    {
                        await CreateGamblingPotAsync(name, description, minutes);
                        Console.WriteLine("Pot created successfully!");
                    }
                    else
                    {
                        Console.WriteLine("Invalid duration. Please enter a positive number of minutes.");
                    }
                    break;

                case "2":
                    ListActivePots();
                    break;

                case "3":
                    ListActivePots();
                    Console.Write("Enter pot ID to end early: ");
                    string potId = Console.ReadLine();
                    await EndPotEarly(potId);
                    break;

                case "4":
                    ShowGamblingHistoryAdmin();
                    break;

                case "5":
                    return;

                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }

        private static async Task CreateGamblingPotAsync(string name, string description, int durationMinutes)
        {
            var pot = new GamblingPot
            {
                Name = name,
                Description = description,
                CreatedAt = DateTime.Now,
                EndsAt = DateTime.Now.AddMinutes(durationMinutes),
                IsActive = true
            };

            lock (_lockGambling)
            {
                _activePots.Add(pot);
            }

            // Set up timer for pot ending
            var timer = new Timer(async state => await EndPotAsync((string)state), pot.Id, durationMinutes * 60 * 1000, Timeout.Infinite);

            lock (_lockGambling)
            {
                _potTimers[pot.Id] = timer;
            }

            await BroadcastMessageAsync($"[GAMBLING] New pot '{name}' created! Use 'GAMBLE bet amount' to join. Ends in {durationMinutes} minutes.");

            Console.WriteLine($"Created gambling pot '{name}' (ID: {pot.Id}) ending at {pot.EndsAt}");
        }

        private static void ListActivePots()
        {
            Console.WriteLine("\nActive Gambling Pots:");
            Console.WriteLine("--------------------");

            List<GamblingPot> pots;
            lock (_lockGambling)
            {
                pots = _activePots.Where(p => p.IsActive).ToList();
            }

            if (pots.Count == 0)
            {
                Console.WriteLine("No active pots.");
            }
            else
            {
                foreach (var pot in pots)
                {
                    TimeSpan remaining = pot.EndsAt - DateTime.Now;
                    Console.WriteLine($"ID: {pot.Id} - {pot.Name}");
                    Console.WriteLine($"  Total: {pot.TotalAmount} credits, Participants: {pot.Participants.Count}");
                    Console.WriteLine($"  Ends in: {(remaining.TotalMinutes > 0 ? $"{remaining.Hours}h {remaining.Minutes}m {remaining.Seconds}s" : "Ending soon...")}");
                }
            }

            Console.WriteLine();
        }

        private static async Task ListActivePots(ClientConnection client)
        {
            List<GamblingPot> pots;
            lock (_lockGambling)
            {
                pots = _activePots.Where(p => p.IsActive).ToList();
            }

            if (pots.Count == 0)
            {
                await SendToClientAsync(client, "No active gambling pots currently available.");
                return;
            }

            await SendToClientAsync(client, "Active Gambling Pots:");

            foreach (var pot in pots)
            {
                TimeSpan remaining = pot.EndsAt - DateTime.Now;
                string timeRemaining = remaining.TotalMinutes > 0
                    ? $"{remaining.Hours}h {remaining.Minutes}m {remaining.Seconds}s"
                    : "Ending soon...";

                await SendToClientAsync(client, $"ID: {pot.Id} - {pot.Name}");
                await SendToClientAsync(client, $"  Description: {pot.Description}");
                await SendToClientAsync(client, $"  Current pot size: {pot.TotalAmount} credits");
                await SendToClientAsync(client, $"  Participants: {pot.Participants.Count}");
                await SendToClientAsync(client, $"  Time remaining: {timeRemaining}");

                // Show user's current bet if they have one
                if (pot.Participants.ContainsKey(client.Username))
                {
                    decimal userBet = pot.Participants[client.Username];
                    decimal winChance = pot.TotalAmount > 0 ? (userBet / pot.TotalAmount) * 100 : 0;
                    await SendToClientAsync(client, $"  Your bet: {userBet} credits (Win chance: {winChance:F2}%)");
                }

                await SendToClientAsync(client, "");
            }

            await SendToClientAsync(client, "Use 'GAMBLE bet amount' to place a bet. Example: GAMBLE bet 50");
        }

        private static async Task GetPotInfo(ClientConnection client, string potId)
        {
            GamblingPot pot;
            lock (_lockGambling)
            {
                pot = _activePots.FirstOrDefault(p => p.Id.Equals(potId, StringComparison.OrdinalIgnoreCase) && p.IsActive);
            }

            if (pot == null)
            {
                await SendToClientAsync(client, $"Pot with ID '{potId}' not found or is no longer active.");
                return;
            }

            TimeSpan remaining = pot.EndsAt - DateTime.Now;
            string timeRemaining = remaining.TotalMinutes > 0
                ? $"{remaining.Hours}h {remaining.Minutes}m {remaining.Seconds}s"
                : "Ending soon...";

            await SendToClientAsync(client, $"Pot: {pot.Name} (ID: {pot.Id})");
            await SendToClientAsync(client, $"Description: {pot.Description}");
            await SendToClientAsync(client, $"Total pot: {pot.TotalAmount} credits");
            await SendToClientAsync(client, $"Created: {pot.CreatedAt}");
            await SendToClientAsync(client, $"Ends: {pot.EndsAt} ({timeRemaining} remaining)");

            // Show user's bet and win chance if they have one
            if (pot.Participants.ContainsKey(client.Username))
            {
                decimal userBet = pot.Participants[client.Username];
                decimal winChance = pot.TotalAmount > 0 ? (userBet / pot.TotalAmount) * 100 : 0;
                await SendToClientAsync(client, $"Your bet: {userBet} credits");
                await SendToClientAsync(client, $"Your win chance: {winChance:F2}%");
            }
            else
            {
                await SendToClientAsync(client, "You haven't placed a bet in this pot yet.");
            }

            await SendToClientAsync(client, $"Total participants: {pot.Participants.Count}");

            // List top 5 bettors
            if (pot.Participants.Any())
            {
                await SendToClientAsync(client, "Top bets:");

                var topBets = pot.Participants
                    .OrderByDescending(p => p.Value)
                    .Take(5);

                foreach (var bet in topBets)
                {
                    decimal winChance = pot.TotalAmount > 0 ? (bet.Value / pot.TotalAmount) * 100 : 0;
                    await SendToClientAsync(client, $"  {bet.Key}: {bet.Value} credits ({winChance:F2}% chance)");
                }
            }
        }

        private static async Task ProcessGambleCommand(ClientConnection client, string parameters)
        {
            string[] parts = parameters.Split(new[] { ' ' }, 2);

            if (parts.Length < 2 || !parts[0].Equals("bet", StringComparison.OrdinalIgnoreCase))
            {
                await SendToClientAsync(client, "Usage: GAMBLE bet amount - Places a bet in the active pot");
                return;
            }

            if (!decimal.TryParse(parts[1], out decimal amount) || amount <= 0)
            {
                await SendToClientAsync(client, "Please specify a valid positive amount to bet.");
                return;
            }

            // Round to 2 decimal places
            amount = Math.Round(amount, 2);

            // Get the user
            User user;
            lock (_lockUsers)
            {
                _users.TryGetValue(client.Username, out user);
            }

            if (user == null)
            {
                await SendToClientAsync(client, "Error retrieving your user account.");
                return;
            }

            if (user.Credits < amount)
            {
                await SendToClientAsync(client, $"Insufficient credits. Your balance: {user.Credits}");
                return;
            }

            // Find the most recent active pot
            GamblingPot pot;
            lock (_lockGambling)
            {
                pot = _activePots.Where(p => p.IsActive).OrderByDescending(p => p.EndsAt).FirstOrDefault();
            }

            if (pot == null)
            {
                await SendToClientAsync(client, "No active gambling pots are currently available.");
                return;
            }

            // Check if user already has a bet in this pot
            bool alreadyBet = false;
            decimal previousBet = 0;

            if (pot.Participants.ContainsKey(client.Username))
            {
                alreadyBet = true;
                previousBet = pot.Participants[client.Username];
            }

            // Update the pot with the user's bet
            lock (_lockGambling)
            {
                if (alreadyBet)
                {
                    pot.TotalAmount += amount;
                    pot.Participants[client.Username] += amount;
                }
                else
                {
                    pot.TotalAmount += amount;
                    pot.Participants[client.Username] = amount;
                }
            }

            // Deduct credits from user
            lock (_lockUsers)
            {
                user.Credits -= amount;

                // Record the transaction
                var transaction = new TransactionRecord
                {
                    Timestamp = DateTime.Now,
                    FromUser = user.Username,
                    ToUser = "GamblingSystem",
                    Amount = amount,
                    Description = $"Bet placed in pot '{pot.Name}' (ID: {pot.Id})"
                };

                user.TransactionHistory.Add(transaction);
            }

            SaveUsers();

            // Calculate odds
            decimal winChance = (pot.Participants[client.Username] / pot.TotalAmount) * 100;

            // Inform the user
            if (alreadyBet)
            {
                await SendToClientAsync(client, $"Added {amount} credits to your existing bet in '{pot.Name}'.");
                await SendToClientAsync(client, $"Your total bet is now {pot.Participants[client.Username]} credits.");
            }
            else
            {
                await SendToClientAsync(client, $"Placed bet of {amount} credits in '{pot.Name}'.");
            }

            await SendToClientAsync(client, $"The total pot is now {pot.TotalAmount} credits.");
            await SendToClientAsync(client, $"Your chance of winning is {winChance:F2}%.");
            await SendToClientAsync(client, $"Pot ends at {pot.EndsAt}. Good luck!");

            // Update user's balance display
            await SendToClientAsync(client, $"Your new balance: {user.Credits} credits");

            // Announce big bets to everyone
            if (amount >= 100)
            {
                await BroadcastMessageAsync($"[GAMBLING] {client.Username} just placed a big bet of {amount} credits in '{pot.Name}'!");
            }
        }

        private static async Task EndPotAsync(string potId)
        {
            GamblingPot pot;

            lock (_lockGambling)
            {
                pot = _activePots.FirstOrDefault(p => p.Id == potId && p.IsActive);
                if (pot == null) return;

                // Mark pot as inactive
                pot.IsActive = false;

                // Remove the timer
                if (_potTimers.TryGetValue(potId, out Timer timer))
                {
                    timer.Dispose();
                    _potTimers.Remove(potId);
                }
            }

            // Process the gambling pot if it has participants
            if (pot.Participants.Count > 0)
            {
                // Select a winner based on amount bet (weighted random selection)
                string winner = SelectWinner(pot);
                decimal winAmount = pot.TotalAmount;

                // Update the pot with winner info
                pot.WinnerUsername = winner;

                // Add to gambling history
                var historyEntry = new GamblingHistory
                {
                    PotId = pot.Id,
                    PotName = pot.Name,
                    TotalAmount = pot.TotalAmount,
                    WinnerUsername = winner,
                    ParticipantCount = pot.Participants.Count,
                    CompletedAt = DateTime.Now
                };

                lock (_lockGambling)
                {
                    _gamblingHistory.Add(historyEntry);
                }

                SaveGamblingHistory();

                // Give the winner their credits
                lock (_lockUsers)
                {
                    if (_users.TryGetValue(winner, out User winnerUser))
                    {
                        winnerUser.Credits += winAmount;

                        // Add transaction record
                        var transaction = new TransactionRecord
                        {
                            Timestamp = DateTime.Now,
                            FromUser = "GamblingSystem",
                            ToUser = winner,
                            Amount = winAmount,
                            Description = $"Won pot '{pot.Name}' (ID: {pot.Id})"
                        };

                        winnerUser.TransactionHistory.Add(transaction);
                    }
                }

                SaveUsers();

                // Announce the winner
                await BroadcastMessageAsync($"[GAMBLING] The pot '{pot.Name}' has ended!");
                await BroadcastMessageAsync($"[GAMBLING] And the winner is... {winner}!");
                await BroadcastMessageAsync($"[GAMBLING] {winner} has won {winAmount} credits! Congratulations!");

                // Find the winner's client if they're online
                ClientConnection winnerClient;
                lock (_lockClients)
                {
                    winnerClient = _clients.FirstOrDefault(c =>
                        c.IsAuthenticated &&
                        c.Username.Equals(winner, StringComparison.OrdinalIgnoreCase));
                }

                // Send a special message to the winner if they're online
                if (winnerClient != null)
                {
                    await SendToClientAsync(winnerClient, "╔═════════════════════════════════════════╗");
                    await SendToClientAsync(winnerClient, "║       💰 CONGRATULATIONS! 💰            ║");
                    await SendToClientAsync(winnerClient, "╟─────────────────────────────────────────╢");
                    await SendToClientAsync(winnerClient, $"║ You've won {winAmount} credits in the pot!  ║");
                    await SendToClientAsync(winnerClient, "╚═════════════════════════════════════════╝");

                    // Also update their balance
                    if (_users.TryGetValue(winner, out User winnerUserObj))
                    {
                        await SendToClientAsync(winnerClient, $"Your new balance: {winnerUserObj.Credits} credits");
                    }
                }

                Console.WriteLine($"Gambling pot '{pot.Name}' ended. Winner: {winner}, Amount: {winAmount} credits");
            }
            else
            {
                await BroadcastMessageAsync($"[GAMBLING] The pot '{pot.Name}' has ended with no participants.");
                Console.WriteLine($"Gambling pot '{pot.Name}' ended with no participants.");
            }
        }

        private static async Task EndPotEarly(string potId)
        {
            GamblingPot pot;

            lock (_lockGambling)
            {
                pot = _activePots.FirstOrDefault(p => p.Id.Equals(potId, StringComparison.OrdinalIgnoreCase) && p.IsActive);

                if (pot == null)
                {
                    Console.WriteLine($"Pot with ID '{potId}' not found or is not active.");
                    return;
                }
            }

            Console.WriteLine($"Ending pot '{pot.Name}' (ID: {pot.Id}) early...");

            // Stop the timer
            if (_potTimers.TryGetValue(pot.Id, out Timer timer))
            {
                timer.Dispose();
                _potTimers.Remove(pot.Id);
            }

            // End the pot immediately
            await EndPotAsync(pot.Id);

            Console.WriteLine("Pot ended successfully.");
        }

        private static string SelectWinner(GamblingPot pot)
        {
            // Use weighted random selection based on bet amounts
            decimal totalAmount = pot.TotalAmount;
            decimal randomPoint = (decimal)_random.NextDouble() * totalAmount;

            decimal cumulativeWeight = 0;
            foreach (var participant in pot.Participants)
            {
                cumulativeWeight += participant.Value;
                if (randomPoint <= cumulativeWeight)
                {
                    return participant.Key;
                }
            }

            // Fallback (should never happen unless pot is empty)
            return pot.Participants.First().Key;
        }

        private static async Task ShowGamblingHistory(ClientConnection client)
        {
            List<GamblingHistory> history;
            lock (_lockGambling)
            {
                history = _gamblingHistory.OrderByDescending(h => h.CompletedAt).Take(10).ToList();
            }

            if (history.Count == 0)
            {
                await SendToClientAsync(client, "No gambling history available yet.");
                return;
            }

            await SendToClientAsync(client, "Recent Gambling Winners:");

            foreach (var entry in history)
            {
                await SendToClientAsync(client, $"Pot: {entry.PotName}");
                await SendToClientAsync(client, $"  Winner: {entry.WinnerUsername}");
                await SendToClientAsync(client, $"  Amount: {entry.TotalAmount} credits");
                await SendToClientAsync(client, $"  Participants: {entry.ParticipantCount}");
                await SendToClientAsync(client, $"  Ended: {entry.CompletedAt}");
                await SendToClientAsync(client, "");
            }
        }

        private static void ShowGamblingHistoryAdmin()
        {
            Console.WriteLine("\nGambling History:");
            Console.WriteLine("----------------");

            List<GamblingHistory> history;
            lock (_lockGambling)
            {
                history = _gamblingHistory.OrderByDescending(h => h.CompletedAt).Take(20).ToList();
            }

            if (history.Count == 0)
            {
                Console.WriteLine("No gambling history available yet.");
                return;
            }

            foreach (var entry in history)
            {
                Console.WriteLine($"Pot: {entry.PotName} (ID: {entry.PotId})");
                Console.WriteLine($"  Winner: {entry.WinnerUsername}");
                Console.WriteLine($"  Amount: {entry.TotalAmount} credits");
                Console.WriteLine($"  Participants: {entry.ParticipantCount}");
                Console.WriteLine($"  Ended: {entry.CompletedAt}");
                Console.WriteLine();
            }
        }

        #endregion
    }
}