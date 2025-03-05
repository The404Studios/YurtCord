using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace EnhancedRelayClient
{
    enum ClientState
    {
        Connecting,
        Authentication,
        Registration,
        Connected
    }

    class Program
    {
        // Configuration
        private const int Port = 8080;
        private const string DefaultServerHost = "127.0.0.1"; // Local host by default

        private static TcpClient _client;
        private static NetworkStream _stream;
        private static bool _connected = false;
        private static CancellationTokenSource _cts;
        private static string _username = "";
        private static string _currentRoom = "lobby";
        private static decimal _credits = 0;
        private static ClientState _state = ClientState.Connecting;
        private static readonly List<string> _messageHistory = new List<string>();
        private static readonly List<string> _shoutboxMessages = new List<string>();
        private static readonly List<string> _roomMessages = new List<string>();
        private static readonly int _maxHistorySize = 100;
        private static readonly object _consoleLock = new object();
        private static string _serverHost;

        static async Task Main(string[] args)
        {
            Console.Title = "YurtCord";
            _cts = new CancellationTokenSource();

            try
            {
                // Ask for server address
                Console.Write($"Enter server address (or press Enter for {DefaultServerHost}): ");
                _serverHost = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(_serverHost))
                {
                    _serverHost = DefaultServerHost;
                }

                await ConnectToServerAsync();

                // Start a background task to receive messages
                Task receiveTask = ReceiveMessagesAsync(_cts.Token);

                // Show authentication options
                await ShowAuthenticationOptions();

                // Main loop for sending messages
                while (_connected)
                {
                    try
                    {
                        string input = Console.ReadLine();

                        if (string.IsNullOrEmpty(input))
                            continue;

                        await ProcessUserInput(input);
                    }
                    catch (Exception ex)
                    {
                        lock (_consoleLock)
                        {
                            AddToMessageHistory($"Error: {ex.Message}", ConsoleColor.Red);
                            RefreshDisplay();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                // Clean up
                _cts.Cancel();
                CloseConnection();

                Console.Clear();
                Console.WriteLine("Disconnected from server. Press any key to exit.");
                Console.ReadKey();
            }
        }

        private static async Task ShowAuthenticationOptions()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    Welcome to YurtCord                               ║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════════════════╣");
            Console.WriteLine("║ 1. Login with existing account                                        ║");
            Console.WriteLine("║ 2. Register a new account                                             ║");
            Console.WriteLine("║ 3. Exit                                                               ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();

            Console.Write("Select an option (1-3): ");
            string option = Console.ReadLine();

            switch (option)
            {
                case "1":
                    await ShowLoginForm();
                    break;
                case "2":
                    await ShowRegistrationForm();
                    break;
                case "3":
                    _connected = false;
                    break;
                default:
                    Console.WriteLine("Invalid option. Press any key to continue...");
                    Console.ReadKey();
                    await ShowAuthenticationOptions();
                    break;
            }
        }

        private static async Task ShowLoginForm()
        {
            Console.Clear();
            Console.WriteLine("=== Login ===");
            Console.Write("Username: ");
            string username = Console.ReadLine();

            Console.Write("Password: ");
            string password = ReadPassword();

            _state = ClientState.Authentication;
            _username = username;

            // Send login command
            await SendMessageAsync($"LOGIN {username} {password}");
        }

        private static async Task ShowRegistrationForm()
        {
            Console.Clear();
            Console.WriteLine("=== Register New Account ===");
            Console.Write("Username (letters and numbers only): ");
            string username = Console.ReadLine();

            Console.Write("Password: ");
            string password = ReadPassword();

            Console.Write("Confirm Password: ");
            string confirmPassword = ReadPassword();

            if (password != confirmPassword)
            {
                Console.WriteLine("Passwords do not match. Press any key to try again...");
                Console.ReadKey();
                await ShowRegistrationForm();
                return;
            }

            Console.Write("Email: ");
            string email = Console.ReadLine();

            _state = ClientState.Registration;

            // Send registration command
            await SendMessageAsync($"REGISTER {username} {password} {email}");

            // Wait for server response
            await Task.Delay(1000);

            // Go back to auth options (server will tell user to login if registration was successful)
            await ShowAuthenticationOptions();
        }

        private static string ReadPassword()
        {
            string password = "";
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                if (key.Key != ConsoleKey.Enter && key.Key != ConsoleKey.Backspace)
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password = password.Substring(0, password.Length - 1);
                    Console.Write("\b \b");
                }
            } while (key.Key != ConsoleKey.Enter);

            Console.WriteLine();
            return password;
        }

        private static void InitializeInterface()
        {
            Console.Clear();
            Console.CursorVisible = true;

            // Set console height to ensure sufficient space
            try
            {
                Console.WindowHeight = Math.Max(Console.WindowHeight, 40);
                Console.BufferHeight = Math.Max(Console.WindowHeight, 1000);
            }
            catch
            {
                // Ignore if we can't set the window size
            }

            // Draw header
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  YurtCord Client - Connected as: {_username.PadRight(42)}║");
            Console.WriteLine($"║  Current Room: {_currentRoom.PadRight(52)}║");
            Console.WriteLine($"║  Credits: {_credits.ToString("0.00").PadRight(56)}║");
            Console.WriteLine("╠════════════════════════════════════════════════════════════════════════════╣");
            Console.WriteLine("║ Type /help for commands | /rooms to list rooms | /users to see online users ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();

            // Tabs for different views
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("[ Room Chat ]");
            Console.ResetColor();
            Console.Write(" | ");
            Console.Write("[ Shoutbox ]");
            Console.Write(" | ");
            Console.Write("[ Gambling ]");
            Console.WriteLine();
            Console.WriteLine(new string('─', Console.WindowWidth - 1));

            // Draw chat area
            for (int i = 0; i < 16; i++)
            {
                Console.WriteLine();
            }

            // Draw input separator
            Console.WriteLine(new string('─', Console.WindowWidth - 1));

            // Reserve fixed space for input area (3 lines)
            Console.WriteLine(new string(' ', Console.WindowWidth - 1));
            Console.WriteLine(new string(' ', Console.WindowWidth - 1));
            Console.WriteLine(new string(' ', Console.WindowWidth - 1));

            // Move cursor back up to input area
            Console.SetCursorPosition(0, Console.WindowHeight - 4);
            Console.Write("> ");
        }

        private static void RefreshDisplay()
        {
            lock (_consoleLock)
            {
                // Save the current input
                int inputLinePosition = Console.WindowHeight - 4;
                string currentInput = "";

                // Only try to read input if cursor is on the input line
                if (Console.CursorTop >= inputLinePosition && Console.CursorLeft >= 2)
                {
                    // Save the exact cursor position within the input
                    int inputCursorPosition = Console.CursorLeft - 2;

                    // Get the current input text
                    Console.SetCursorPosition(2, inputLinePosition);

                    // Read the current input (this is a bit hacky but works)
                    StringBuilder input = new StringBuilder();
                    int maxWidth = Console.WindowWidth - 3;

                    // Create a temporary buffer to read what's on screen
                    char[] buffer = new char[maxWidth];
                    for (int i = 0; i < maxWidth; i++)
                    {
                        if (Console.CursorLeft < Console.WindowWidth - 1)
                        {
                            try
                            {
                                // Try to read the character at the current position
                                char c = ' ';
                                Console.CursorLeft++;
                                buffer[i] = c;
                            }
                            catch
                            {
                                // If we can't read, just assume it's a space
                                buffer[i] = ' ';
                            }
                        }
                        else
                        {
                            buffer[i] = ' ';
                        }
                    }

                    currentInput = new string(buffer).TrimEnd();
                }

                // Update header with latest information
                Console.SetCursorPosition(0, 0);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("╔════════════════════════════════════════════════════════════════════════════╗");
                Console.WriteLine($"║  Yurt Cord Client - Connected as: {_username.PadRight(42)}║");
                Console.WriteLine($"║  Current Room: {_currentRoom.PadRight(52)}║");
                Console.WriteLine($"║  Credits: {_credits.ToString("0.00").PadRight(56)}║");
                Console.WriteLine("╠════════════════════════════════════════════════════════════════════════════╣");
                Console.WriteLine("║ Type /help for commands | /rooms to list rooms | /users to see online users ║");
                Console.WriteLine("╚════════════════════════════════════════════════════════════════════════════╝");
                Console.ResetColor();

                // Position cursor at the start of the message area
                Console.SetCursorPosition(0, 9);

                // Clear message area (from line 9 to input area start)
                for (int i = 9; i < inputLinePosition - 1; i++)
                {
                    Console.SetCursorPosition(0, i);
                    Console.Write(new string(' ', Console.WindowWidth - 1));
                }

                // Display messages
                Console.SetCursorPosition(0, 9);

                // Determine which message list to show
                List<string> messagesToShow = _roomMessages;

                // Calculate how many messages we can display
                int maxMessagesToShow = inputLinePosition - 10;

                // Display messages
                int startIdx = Math.Max(0, messagesToShow.Count - maxMessagesToShow);
                for (int i = startIdx; i < messagesToShow.Count; i++)
                {
                    string msg = messagesToShow[i];

                    if (msg.StartsWith("[PM"))
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                    }
                    else if (msg.StartsWith("*"))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                    else if (msg.StartsWith("System:"))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    else if (msg.StartsWith("[SERVER") || msg.StartsWith("Available commands"))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else if (msg.StartsWith("[SHOUTBOX]"))
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                    }
                    else if (msg.StartsWith("[GAMBLING]"))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }

                    Console.WriteLine(msg);
                    Console.ResetColor();
                }

                // Clear the input area
                Console.SetCursorPosition(0, inputLinePosition);
                Console.Write(new string(' ', Console.WindowWidth - 1));
                Console.SetCursorPosition(0, inputLinePosition + 1);
                Console.Write(new string(' ', Console.WindowWidth - 1));
                Console.SetCursorPosition(0, inputLinePosition + 2);
                Console.Write(new string(' ', Console.WindowWidth - 1));

                // Restore input line prompt and text
                Console.SetCursorPosition(0, inputLinePosition);
                Console.Write("> ");
                Console.Write(currentInput);

                // Set cursor to the right place
                Console.SetCursorPosition(2 + currentInput.Length, inputLinePosition);
            }
        }

        private static void DisplayShoutbox()
        {
            lock (_consoleLock)
            {
                // Save the current input
                int inputLinePosition = Console.WindowHeight - 4;
                string currentInput = "";

                if (Console.CursorTop >= inputLinePosition && Console.CursorLeft >= 2)
                {
                    // Save the exact cursor position within the input
                    int inputCursorPosition = Console.CursorLeft - 2;

                    // Get current input text
                    Console.SetCursorPosition(2, inputLinePosition);

                    // Read the current input (this is a bit hacky but works)
                    StringBuilder input = new StringBuilder();
                    int maxWidth = Console.WindowWidth - 3;

                    // Create a temporary buffer to read what's on screen
                    char[] buffer = new char[maxWidth];
                    for (int i = 0; i < maxWidth; i++)
                    {
                        if (Console.CursorLeft < Console.WindowWidth - 1)
                        {
                            try
                            {
                                // Try to read the character at the current position
                                char c = ' ';
                                Console.CursorLeft++;
                                buffer[i] = c;
                            }
                            catch
                            {
                                // If we can't read, just assume it's a space
                                buffer[i] = ' ';
                            }
                        }
                        else
                        {
                            buffer[i] = ' ';
                        }
                    }

                    currentInput = new string(buffer).TrimEnd();
                }

                // Update tabs
                Console.SetCursorPosition(0, 7);
                Console.Write("[ Room Chat ]");
                Console.Write(" | ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("[ Shoutbox ]");
                Console.ResetColor();
                Console.Write(" | ");
                Console.Write("[ Gambling ]");
                Console.WriteLine();
                Console.WriteLine(new string('─', Console.WindowWidth - 1));

                // Position cursor at the start of the message area
                Console.SetCursorPosition(0, 9);

                // Clear message area
                for (int i = 9; i < inputLinePosition - 1; i++)
                {
                    Console.SetCursorPosition(0, i);
                    Console.Write(new string(' ', Console.WindowWidth - 1));
                }

                // Display messages
                Console.SetCursorPosition(0, 9);

                // Calculate how many messages we can display
                int maxMessagesToShow = inputLinePosition - 10;

                // Display shoutbox messages
                int startIdx = Math.Max(0, _shoutboxMessages.Count - maxMessagesToShow);
                for (int i = startIdx; i < _shoutboxMessages.Count; i++)
                {
                    string msg = _shoutboxMessages[i];
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(msg);
                    Console.ResetColor();
                }

                // Clear the input area
                Console.SetCursorPosition(0, inputLinePosition);
                Console.Write(new string(' ', Console.WindowWidth - 1));
                Console.SetCursorPosition(0, inputLinePosition + 1);
                Console.Write(new string(' ', Console.WindowWidth - 1));
                Console.SetCursorPosition(0, inputLinePosition + 2);
                Console.Write(new string(' ', Console.WindowWidth - 1));

                // Restore input line prompt and text
                Console.SetCursorPosition(0, inputLinePosition);
                Console.Write("> ");
                Console.Write(currentInput);

                // Set cursor to the right place
                Console.SetCursorPosition(2 + currentInput.Length, inputLinePosition);
            }
        }

        private static void DisplayRoomChat()
        {
            lock (_consoleLock)
            {
                // Save the current input
                int inputLinePosition = Console.WindowHeight - 4;
                string currentInput = "";

                if (Console.CursorTop >= inputLinePosition && Console.CursorLeft >= 2)
                {
                    // Save the exact cursor position within the input
                    int inputCursorPosition = Console.CursorLeft - 2;

                    // Get current input text
                    Console.SetCursorPosition(2, inputLinePosition);

                    // Read the current input (this is a bit hacky but works)
                    StringBuilder input = new StringBuilder();
                    int maxWidth = Console.WindowWidth - 3;

                    // Create a temporary buffer to read what's on screen
                    char[] buffer = new char[maxWidth];
                    for (int i = 0; i < maxWidth; i++)
                    {
                        if (Console.CursorLeft < Console.WindowWidth - 1)
                        {
                            try
                            {
                                // Try to read the character at the current position
                                char c = ' ';
                                Console.CursorLeft++;
                                buffer[i] = c;
                            }
                            catch
                            {
                                // If we can't read, just assume it's a space
                                buffer[i] = ' ';
                            }
                        }
                        else
                        {
                            buffer[i] = ' ';
                        }
                    }

                    currentInput = new string(buffer).TrimEnd();
                }

                // Update tabs
                Console.SetCursorPosition(0, 7);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("[ Room Chat ]");
                Console.ResetColor();
                Console.Write(" | ");
                Console.Write("[ Shoutbox ]");
                Console.Write(" | ");
                Console.Write("[ Gambling ]");
                Console.WriteLine();
                Console.WriteLine(new string('─', Console.WindowWidth - 1));

                // Position cursor at the start of the message area
                Console.SetCursorPosition(0, 9);

                // Clear message area
                for (int i = 9; i < inputLinePosition - 1; i++)
                {
                    Console.SetCursorPosition(0, i);
                    Console.Write(new string(' ', Console.WindowWidth - 1));
                }

                // Display messages
                Console.SetCursorPosition(0, 9);

                // Calculate how many messages we can display
                int maxMessagesToShow = inputLinePosition - 10;

                // Display room messages
                int startIdx = Math.Max(0, _roomMessages.Count - maxMessagesToShow);
                for (int i = startIdx; i < _roomMessages.Count; i++)
                {
                    string msg = _roomMessages[i];

                    if (msg.StartsWith("[PM"))
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                    }
                    else if (msg.StartsWith("*"))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                    else if (msg.StartsWith("System:"))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    else if (msg.StartsWith("[SERVER") || msg.StartsWith("Available commands"))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else if (msg.StartsWith("[GAMBLING]"))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }

                    Console.WriteLine(msg);
                    Console.ResetColor();
                }

                // Clear the input area
                Console.SetCursorPosition(0, inputLinePosition);
                Console.Write(new string(' ', Console.WindowWidth - 1));
                Console.SetCursorPosition(0, inputLinePosition + 1);
                Console.Write(new string(' ', Console.WindowWidth - 1));
                Console.SetCursorPosition(0, inputLinePosition + 2);
                Console.Write(new string(' ', Console.WindowWidth - 1));

                // Restore input line prompt and text
                Console.SetCursorPosition(0, inputLinePosition);
                Console.Write("> ");
                Console.Write(currentInput);

                // Set cursor to the right place
                Console.SetCursorPosition(2 + currentInput.Length, inputLinePosition);
            }
        }

        private static void AddToMessageHistory(string message, ConsoleColor color = ConsoleColor.White)
        {
            lock (_messageHistory)
            {
                _messageHistory.Add(message);

                // Also add to appropriate specialized list
                if (message.StartsWith("[SHOUTBOX]"))
                {
                    _shoutboxMessages.Add(message);
                    if (_shoutboxMessages.Count > _maxHistorySize)
                    {
                        _shoutboxMessages.RemoveAt(0);
                    }
                }
                else
                {
                    _roomMessages.Add(message);
                    if (_roomMessages.Count > _maxHistorySize)
                    {
                        _roomMessages.RemoveAt(0);
                    }
                }

                if (_messageHistory.Count > _maxHistorySize * 2)
                {
                    _messageHistory.RemoveAt(0);
                }
            }
        }

        private static async Task ProcessUserInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;

            // Handle client-side commands
            if (input.StartsWith("/"))
            {
                string[] parts = input.Split(new[] { ' ' }, 2);
                string command = parts[0].ToLower();
                string args = parts.Length > 1 ? parts[1] : "";

                switch (command)
                {
                    case "/exit":
                    case "/quit":
                        await SendMessageAsync("QUIT");
                        _connected = false;
                        break;

                    case "/whisper":
                    case "/w":
                        if (string.IsNullOrWhiteSpace(args) || !args.Contains(" "))
                        {
                            AddToMessageHistory("System: Usage: /whisper username message", ConsoleColor.Red);
                            RefreshDisplay();
                        }
                        else
                        {
                            string[] whisperParts = args.Split(new[] { ' ' }, 2);
                            string target = whisperParts[0];
                            string message = whisperParts[1];
                            await SendMessageAsync($"WHISPER {target} {message}");
                        }
                        break;

                    case "/users":
                        await SendMessageAsync("USERS");
                        break;

                    case "/help":
                        await SendMessageAsync("HELP");
                        break;

                    case "/rooms":
                        await SendMessageAsync("ROOMS");
                        break;

                    case "/join":
                        if (string.IsNullOrWhiteSpace(args))
                        {
                            AddToMessageHistory("System: Usage: /join roomName", ConsoleColor.Red);
                            RefreshDisplay();
                        }
                        else
                        {
                            await SendMessageAsync($"JOIN {args}");
                        }
                        break;

                    case "/leave":
                        await SendMessageAsync("LEAVE");
                        break;

                    case "/create":
                    case "/createroom":
                        if (string.IsNullOrWhiteSpace(args))
                        {
                            AddToMessageHistory("System: Usage: /create roomName [description]", ConsoleColor.Red);
                            RefreshDisplay();
                        }
                        else
                        {
                            await SendMessageAsync($"CREATEROOM {args}");
                        }
                        break;

                    case "/balance":
                        await SendMessageAsync("BALANCE");
                        break;

                    case "/transfer":
                        if (string.IsNullOrWhiteSpace(args) || !args.Contains(" "))
                        {
                            AddToMessageHistory("System: Usage: /transfer username amount [description]", ConsoleColor.Red);
                            RefreshDisplay();
                        }
                        else
                        {
                            await SendMessageAsync($"TRANSFER {args}");
                        }
                        break;

                    case "/shout":
                        if (string.IsNullOrWhiteSpace(args))
                        {
                            AddToMessageHistory("System: Usage: /shout message", ConsoleColor.Red);
                            RefreshDisplay();
                        }
                        else
                        {
                            await SendMessageAsync($"SHOUT {args}");
                        }
                        break;

                    case "/shoutbox":
                        await SendMessageAsync("SHOUTBOX");
                        DisplayShoutbox();
                        break;

                    case "/roomchat":
                        DisplayRoomChat();
                        break;

                    case "/transactions":
                        await SendMessageAsync("TRANSACTIONS");
                        break;

                    // Gambling commands
                    case "/gamble":
                        if (string.IsNullOrWhiteSpace(args) || !args.Contains(" "))
                        {
                            AddToMessageHistory("System: Usage: /gamble amount - Places a bet in the active pot", ConsoleColor.Red);
                            RefreshDisplay();
                        }
                        else
                        {
                            await SendMessageAsync($"GAMBLE bet {args}");
                        }
                        break;

                    case "/pots":
                        await SendMessageAsync("POTS");
                        break;

                    case "/potinfo":
                        if (string.IsNullOrWhiteSpace(args))
                        {
                            AddToMessageHistory("System: Usage: /potinfo potId - Get details about a specific pot", ConsoleColor.Red);
                            RefreshDisplay();
                        }
                        else
                        {
                            await SendMessageAsync($"POTINFO {args}");
                        }
                        break;

                    case "/gambling":
                    case "/gamblinghistory":
                        await SendMessageAsync("GAMBLINGHISTORY");
                        break;

                    case "/gambling-view":
                        DisplayGamblingView();
                        break;

                    // Casino games
                    case "/dice":
                        if (string.IsNullOrWhiteSpace(args))
                        {
                            AddToMessageHistory("System: Usage: /dice amount [target] - Roll dice with optional target", ConsoleColor.Red);
                            RefreshDisplay();
                        }
                        else
                        {
                            await SendMessageAsync($"DICE {args}");
                        }
                        break;

                    case "/flip":
                        if (string.IsNullOrWhiteSpace(args) || !args.Contains(" "))
                        {
                            AddToMessageHistory("System: Usage: /flip amount choice - Flip a coin (HEADS or TAILS)", ConsoleColor.Red);
                            RefreshDisplay();
                        }
                        else
                        {
                            await SendMessageAsync($"FLIP {args}");
                        }
                        break;

                    case "/slots":
                        if (string.IsNullOrWhiteSpace(args))
                        {
                            AddToMessageHistory("System: Usage: /slots amount - Play the slot machine", ConsoleColor.Red);
                            RefreshDisplay();
                        }
                        else
                        {
                            await SendMessageAsync($"SLOTS {args}");
                        }
                        break;

                    case "/stats":
                        await SendMessageAsync("STATS");
                        break;

                    case "/leaderboard":
                        await SendMessageAsync("LEADERBOARD");
                        break;

                    default:
                        AddToMessageHistory($"System: Unknown command: {command}. Type /help for available commands.", ConsoleColor.Red);
                        RefreshDisplay();
                        break;
                }
            }
            else
            {
                // Regular chat message
                await SendMessageAsync(input);
            }
        }

        private static async Task ConnectToServerAsync()
        {
            Console.WriteLine($"Connecting to server at {_serverHost}:{Port}...");

            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(_serverHost, Port);
                _stream = _client.GetStream();
                _connected = true;

                Console.WriteLine("Connected to server successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection failed: {ex.Message}");
                throw;
            }
        }

        private static async Task SendMessageAsync(string message)
        {
            if (!_connected)
            {
                Console.WriteLine("Not connected to server.");
                return;
            }

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                await _stream.WriteAsync(data, 0, data.Length);

                // Special handling for some commands
                if (message.StartsWith("JOIN "))
                {
                    string roomName = message.Substring(5);
                    _currentRoom = roomName;
                    RefreshDisplay();
                }
                else if (message == "LEAVE")
                {
                    _currentRoom = "lobby";
                    RefreshDisplay();
                }
            }
            catch (Exception ex)
            {
                lock (_consoleLock)
                {
                    Console.WriteLine($"Error sending message: {ex.Message}");
                }
                _connected = false;
            }
        }

        private static async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[4096];

            try
            {
                while (_connected && !cancellationToken.IsCancellationRequested)
                {
                    // Check if data is available to read
                    if (_client.Available > 0 || _stream.DataAvailable)
                    {
                        int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        if (bytesRead > 0)
                        {
                            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                            // Split multiple messages if needed
                            string[] messages = message.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (string msg in messages)
                            {
                                // Handle authentication response
                                if (_state == ClientState.Authentication && msg.Contains("Authentication successful"))
                                {
                                    _state = ClientState.Connected;
                                    InitializeInterface();
                                }

                                // Extract room name if changed
                                if (msg.StartsWith("You have joined room:"))
                                {
                                    string[] roomParts = msg.Split(':');
                                    if (roomParts.Length > 1)
                                    {
                                        _currentRoom = roomParts[1].Trim();
                                    }
                                }

                                // Extract credit balance if provided
                                if (msg.StartsWith("Your current credit balance:"))
                                {
                                    string[] balanceParts = msg.Split(':');
                                    if (balanceParts.Length > 1)
                                    {
                                        string balanceText = balanceParts[1].Trim();
                                        balanceText = balanceText.Replace("credits", "").Trim();
                                        if (decimal.TryParse(balanceText, out decimal balance))
                                        {
                                            _credits = balance;
                                        }
                                    }
                                }

                                // Handle responses based on state
                                if (_state == ClientState.Connected)
                                {
                                    AddToMessageHistory(msg);
                                    RefreshDisplay();
                                }
                                else
                                {
                                    lock (_consoleLock)
                                    {
                                        Console.WriteLine(msg);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Server closed the connection
                            _connected = false;
                            lock (_consoleLock)
                            {
                                Console.WriteLine("Server disconnected.");
                            }
                            break;
                        }
                    }

                    // Small delay to prevent high CPU usage
                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, no need to report
            }
            catch (Exception ex)
            {
                lock (_consoleLock)
                {
                    Console.WriteLine($"Error receiving messages: {ex.Message}");
                }
                _connected = false;
            }
        }

        private static void CloseConnection()
        {
            _connected = false;

            if (_stream != null)
            {
                _stream.Close();
                _stream = null;
            }

            if (_client != null)
            {
                _client.Close();
                _client = null;
            }
        }
        private static readonly List<string> _gamblingMessages = new List<string>();

        private static void DisplayGamblingView()
        {
            lock (_consoleLock)
            {
                // Save the current input
                int inputLinePosition = Console.WindowHeight - 4;
                string currentInput = "";

                if (Console.CursorTop >= inputLinePosition && Console.CursorLeft >= 2)
                {
                    // Save the exact cursor position within the input
                    int inputCursorPosition = Console.CursorLeft - 2;

                    // Get current input text
                    Console.SetCursorPosition(2, inputLinePosition);

                    // Read the current input
                    StringBuilder input = new StringBuilder();
                    int maxWidth = Console.WindowWidth - 3;

                    // Create a temporary buffer
                    char[] buffer = new char[maxWidth];
                    for (int i = 0; i < maxWidth; i++)
                    {
                        if (Console.CursorLeft < Console.WindowWidth - 1)
                        {
                            try
                            {
                                char c = ' ';
                                Console.CursorLeft++;
                                buffer[i] = c;
                            }
                            catch
                            {
                                buffer[i] = ' ';
                            }
                        }
                        else
                        {
                            buffer[i] = ' ';
                        }
                    }

                    currentInput = new string(buffer).TrimEnd();
                }

                // Update tabs
                Console.SetCursorPosition(0, 7);
                Console.Write("[ Room Chat ]");
                Console.Write(" | ");
                Console.Write("[ Shoutbox ]");
                Console.Write(" | ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("[ Gambling ]");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine(new string('─', Console.WindowWidth - 1));

                // Position cursor at the start of the message area
                Console.SetCursorPosition(0, 9);

                // Clear message area
                for (int i = 9; i < inputLinePosition - 1; i++)
                {
                    Console.SetCursorPosition(0, i);
                    Console.Write(new string(' ', Console.WindowWidth - 1));
                }

                // Display messages
                Console.SetCursorPosition(0, 9);

                // Extract gambling-related messages from all messages
                var gamblingMessages = _messageHistory
                    .Where(msg =>
                        msg.StartsWith("[GAMBLING]") ||
                        msg.Contains("gambling") ||
                        msg.Contains("Gambling") ||
                        msg.Contains("bet") ||
                        msg.Contains("pot") ||
                        msg.Contains("Pot") ||
                        msg.Contains("Bet"))
                    .ToList();

                // Show help information at the top
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("== Gambling Commands ==");
                Console.WriteLine("/gamble amount - Place a bet in the active pot");
                Console.WriteLine("/pots - List all active gambling pots");
                Console.WriteLine("/potinfo ID - Get detailed information about a specific pot");
                Console.WriteLine("/gambling - View gambling history");
                Console.WriteLine();
                Console.ResetColor();

                // Calculate how many messages we can display
                int maxMessagesToShow = inputLinePosition - 15;

                // Display gambling messages
                int startIdx = Math.Max(0, gamblingMessages.Count - maxMessagesToShow);
                for (int i = startIdx; i < gamblingMessages.Count; i++)
                {
                    string msg = gamblingMessages[i];

                    if (msg.StartsWith("[GAMBLING]"))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                    else if (msg.Contains("win") || msg.Contains("Win") || msg.Contains("won") || msg.Contains("Won"))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                    }

                    Console.WriteLine(msg);
                    Console.ResetColor();
                }

                // Clear the input area
                Console.SetCursorPosition(0, inputLinePosition);
                Console.Write(new string(' ', Console.WindowWidth - 1));
                Console.SetCursorPosition(0, inputLinePosition + 1);
                Console.Write(new string(' ', Console.WindowWidth - 1));
                Console.SetCursorPosition(0, inputLinePosition + 2);
                Console.Write(new string(' ', Console.WindowWidth - 1));

                // Restore input line prompt and text
                Console.SetCursorPosition(0, inputLinePosition);
                Console.Write("> ");
                Console.Write(currentInput);

                // Set cursor to the right place
                Console.SetCursorPosition(2 + currentInput.Length, inputLinePosition);
            }
        }
    }
}