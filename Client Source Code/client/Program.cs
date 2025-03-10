﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        private const int WebInterfacePort = 8081;
        private const string DefaultServerHost = "162.248.94.105";

        // Console client state
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

        // Web interface state
        private static HttpListener _httpListener;
        private static bool _runningWebInterface = false;
        private static readonly string _webContentHtml = GetWebInterfaceHtml();

        static async Task Main(string[] args)
        {
            _cts = new CancellationTokenSource();

            try
            {
                // Show interface selection
                if (!args.Contains("--web") && !args.Contains("--console"))
                {
                    Console.WriteLine("Choose your preferred interface:");
                    Console.WriteLine("1. Console Interface");
                    Console.WriteLine("2. Web Interface");
                    Console.Write("Enter your choice (1 or 2): ");

                    string choice = Console.ReadLine();

                    if (choice == "2")
                    {
                        await RunWebInterfaceAsync();
                    }
                    else
                    {
                        await RunConsoleInterfaceAsync();
                    }
                }
                else if (args.Contains("--web"))
                {
                    await RunWebInterfaceAsync();
                }
                else
                {
                    await RunConsoleInterfaceAsync();
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

                if (!_runningWebInterface)
                {
                    Console.Clear();
                    Console.WriteLine("Disconnected from server. Press any key to exit.");
                    Console.ReadKey();
                }
            }
        }

        #region Web Interface Implementation

        private static async Task RunWebInterfaceAsync()
        {
            _runningWebInterface = true;
            Console.Title = "YurtCord Web Interface Server";

            try
            {
                Console.WriteLine("Starting YurtCord Web Interface...");

                // Ask for server address
                Console.Write($"Enter server address (or press Enter for {DefaultServerHost}): ");
                _serverHost = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(_serverHost))
                {
                    _serverHost = DefaultServerHost;
                }

                // Start HTTP server for web interface
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add($"http://localhost:{WebInterfacePort}/");
                _httpListener.Start();

                Console.WriteLine($"Web interface started on http://localhost:{WebInterfacePort}/");
                Console.WriteLine("Opening browser...");

                // Open the web interface in browser
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = $"http://localhost:{WebInterfacePort}/",
                    UseShellExecute = true
                });

                // Handle HTTP requests
                await HandleHttpRequestsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Web interface error: {ex.Message}");
            }
            finally
            {
                _httpListener?.Close();
                Console.WriteLine("Web interface server stopped. Press any key to exit.");
                Console.ReadKey();
            }
        }

        private static async Task HandleHttpRequestsAsync()
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    var context = await _httpListener.GetContextAsync();

                    if (context.Request.IsWebSocketRequest)
                    {
                        ProcessWebSocketRequest(context);
                    }
                    else if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/switch-to-console")
                    {
                        // Handle switch to console mode
                        HandleSwitchToConsoleRequest(context);
                    }
                    else
                    {
                        // Serve HTML content
                        await ServeHtmlContentAsync(context);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling HTTP requests: {ex.Message}");
            }
        }

        private static void ProcessWebSocketRequest(HttpListenerContext context)
        {
            Task.Run(async () =>
            {
                WebSocketContext webSocketContext = null;
                WebSocket webSocket = null;
                TcpClient serverClient = null;
                NetworkStream serverStream = null;

                try
                {
                    // Accept WebSocket connection
                    webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
                    webSocket = webSocketContext.WebSocket;

                    Console.WriteLine("WebSocket connection established");

                    // Connect to YurtCord server
                    serverClient = new TcpClient();
                    await serverClient.ConnectAsync(_serverHost, Port);
                    serverStream = serverClient.GetStream();

                    Console.WriteLine($"Connected to YurtCord server at {_serverHost}:{Port}");

                    // Start receiving from server
                    var serverToClientTask = ForwardServerToClientAsync(serverStream, webSocket);

                    // Handle client to server communication
                    byte[] buffer = new byte[4096];
                    WebSocketReceiveResult receiveResult;

                    while (webSocket.State == WebSocketState.Open)
                    {
                        receiveResult = await webSocket.ReceiveAsync(
                            new ArraySegment<byte>(buffer), CancellationToken.None);

                        if (receiveResult.MessageType == WebSocketMessageType.Close)
                        {
                            await webSocket.CloseAsync(
                                WebSocketCloseStatus.NormalClosure,
                                "Connection closed by client",
                                CancellationToken.None);
                        }
                        else if (receiveResult.MessageType == WebSocketMessageType.Text)
                        {
                            string message = Encoding.UTF8.GetString(
                                buffer, 0, receiveResult.Count);

                            // Forward to server
                            byte[] serverData = Encoding.UTF8.GetBytes(message);
                            await serverStream.WriteAsync(serverData, 0, serverData.Length);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WebSocket error: {ex.Message}");
                }
                finally
                {
                    // Clean up resources
                    webSocket?.Dispose();
                    serverStream?.Dispose();
                    serverClient?.Dispose();
                }
            });
        }

        private static async Task ForwardServerToClientAsync(NetworkStream serverStream, WebSocket webSocket)
        {
            byte[] buffer = new byte[4096];

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    // Use a small delay to prevent high CPU usage
                    await Task.Delay(50);

                    // Check if there is data available to read
                    if (serverStream.DataAvailable)
                    {
                        int bytesRead = await serverStream.ReadAsync(buffer, 0, buffer.Length);

                        if (bytesRead > 0)
                        {
                            // Forward to WebSocket client
                            await webSocket.SendAsync(
                                new ArraySegment<byte>(buffer, 0, bytesRead),
                                WebSocketMessageType.Text,
                                true,
                                CancellationToken.None);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error forwarding data: {ex.Message}");
            }
        }

        private static void HandleSwitchToConsoleRequest(HttpListenerContext context)
        {
            try
            {
                string response = "{\"success\":true,\"message\":\"Switching to console mode\"}";
                byte[] buffer = Encoding.UTF8.GetBytes(response);

                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();

                // Stop the web interface and start console interface
                Task.Run(async () =>
                {
                    // Give the browser time to receive the response
                    await Task.Delay(500);

                    // Stop web server
                    _httpListener.Stop();
                    _runningWebInterface = false;

                    // Start console interface
                    await RunConsoleInterfaceAsync();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling switch to console: {ex.Message}");
                context.Response.StatusCode = 500;
                context.Response.OutputStream.Close();
            }
        }

        private static async Task ServeHtmlContentAsync(HttpListenerContext context)
        {
            try
            {
                string responseString = _webContentHtml;
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);

                context.Response.ContentType = "text/html";
                context.Response.ContentLength64 = buffer.Length;
                await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serving HTML: {ex.Message}");
            }
            finally
            {
                context.Response.OutputStream.Close();
            }
        }

        private static string GetWebInterfaceHtml()
        {
            // This is the entire web interface HTML (including embedded CSS and JavaScript)
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>YurtCord</title>
    <link href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css"" rel=""stylesheet"">
    <style>
        :root {
            --primary-color: #6a5acd;
            --secondary-color: #9370db;
            --accent-color: #00bfff;
            --background-color: #0f0f1a;
            --card-bg-color: #1a1a2e;
            --text-color: #f0f0f0;
            --text-secondary: #a0a0a0;
            --success-color: #4caf50;
            --warning-color: #ff9800;
            --danger-color: #f44336;
            --info-color: #2196f3;
            --border-radius: 8px;
        }

        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
        }

        body {
            background-color: var(--background-color);
            color: var(--text-color);
            height: 100vh;
            display: grid;
            grid-template-rows: auto 1fr;
            overflow: hidden;
        }

        header {
            background-color: var(--card-bg-color);
            padding: 1rem;
            box-shadow: 0 2px 10px rgba(0, 0, 0, 0.3);
            position: relative;
            z-index: 10;
        }

        .header-content {
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        .logo {
            font-size: 1.8rem;
            font-weight: bold;
            color: var(--accent-color);
            display: flex;
            align-items: center;
        }

        .logo i {
            margin-right: 10px;
        }

        .user-info {
            display: flex;
            align-items: center;
            gap: 1rem;
        }

        .user-badge {
            background-color: var(--primary-color);
            padding: 0.5rem 1rem;
            border-radius: var(--border-radius);
            font-weight: bold;
        }

        .credits {
            background-color: var(--secondary-color);
            padding: 0.5rem 1rem;
            border-radius: var(--border-radius);
            display: flex;
            align-items: center;
            gap: 0.5rem;
        }

        .room-info {
            background-color: var(--card-bg-color);
            padding: 0.5rem 1rem;
            margin-top: 0.5rem;
            border-radius: var(--border-radius);
            display: flex;
            justify-content: space-between;
        }

        main {
            display: grid;
            grid-template-columns: 250px 1fr 300px;
            gap: 1rem;
            padding: 1rem;
            height: 100%;
            overflow: hidden;
        }

        .sidebar {
            background-color: var(--card-bg-color);
            border-radius: var(--border-radius);
            padding: 1rem;
            display: flex;
            flex-direction: column;
            overflow: hidden;
        }

        .sidebar-title {
            font-size: 1.2rem;
            margin-bottom: 1rem;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        .sidebar-title button {
            background: none;
            border: none;
            color: var(--accent-color);
            cursor: pointer;
            font-size: 1rem;
        }

        .tab-content {
            flex: 1;
            overflow-y: auto;
            scrollbar-width: thin;
        }

        .tab-content::-webkit-scrollbar {
            width: 6px;
        }

        .tab-content::-webkit-scrollbar-thumb {
            background-color: var(--secondary-color);
            border-radius: 3px;
        }

        .room-list, .user-list {
            list-style: none;
        }

        .room-item, .user-item {
            padding: 0.75rem;
            margin: 0.25rem 0;
            border-radius: var(--border-radius);
            cursor: pointer;
            transition: background-color 0.2s;
            display: flex;
            align-items: center;
            gap: 0.5rem;
        }

        .room-item:hover, .user-item:hover {
            background-color: rgba(255, 255, 255, 0.1);
        }

        .room-item.active {
            background-color: var(--primary-color);
        }

        .room-item .room-icon, .user-item .user-status {
            font-size: 0.8rem;
            width: 24px;
            height: 24px;
            display: flex;
            align-items: center;
            justify-content: center;
            border-radius: 50%;
        }

        .room-icon {
            background-color: var(--secondary-color);
        }

        .user-status {
            background-color: var(--success-color);
        }

        .user-status.away {
            background-color: var(--warning-color);
        }

        .user-status.offline {
            background-color: var(--text-secondary);
        }

        .chat-container {
            display: flex;
            flex-direction: column;
            height: 100%;
        }

        .tabs {
            display: flex;
            background-color: var(--card-bg-color);
            border-radius: var(--border-radius) var(--border-radius) 0 0;
            overflow: hidden;
        }

        .tab {
            padding: 0.75rem 1.5rem;
            background-color: transparent;
            border: none;
            color: var(--text-color);
            cursor: pointer;
            transition: background-color 0.2s;
            font-weight: bold;
        }

        .tab:hover {
            background-color: rgba(255, 255, 255, 0.1);
        }

        .tab.active {
            background-color: var(--primary-color);
        }

        .chat-view {
            flex: 1;
            background-color: var(--card-bg-color);
            border-radius: 0 0 var(--border-radius) var(--border-radius);
            padding: 1rem;
            display: flex;
            flex-direction: column;
            overflow: hidden;
        }

        .messages {
            flex: 1;
            overflow-y: auto;
            padding-right: 0.5rem;
            scrollbar-width: thin;
            margin-bottom: 1rem;
        }

        .messages::-webkit-scrollbar {
            width: 6px;
        }

        .messages::-webkit-scrollbar-thumb {
            background-color: var(--secondary-color);
            border-radius: 3px;
        }

        .message {
            margin-bottom: 1rem;
            animation: fadeIn 0.2s ease-in-out;
        }

        @keyframes fadeIn {
            from { opacity: 0; transform: translateY(10px); }
            to { opacity: 1; transform: translateY(0); }
        }

        .message-header {
            display: flex;
            align-items: center;
            margin-bottom: 0.25rem;
        }

        .message-sender {
            font-weight: bold;
            margin-right: 0.5rem;
        }

        .message-time {
            font-size: 0.8rem;
            color: var(--text-secondary);
        }

        .message-content {
            padding: 0.5rem 0;
            word-wrap: break-word;
        }

        .system-message {
            color: var(--warning-color);
            font-style: italic;
        }

        .private-message {
            color: var(--accent-color);
        }

        .message-form {
            display: flex;
            gap: 0.5rem;
        }

        .message-input {
            flex: 1;
            padding: 0.75rem;
            background-color: rgba(255, 255, 255, 0.1);
            border: 1px solid rgba(255, 255, 255, 0.2);
            border-radius: var(--border-radius);
            color: var(--text-color);
            resize: none;
            height: 80px;
        }

        .message-input:focus {
            outline: none;
            border-color: var(--accent-color);
        }

        .message-actions {
            display: flex;
            flex-direction: column;
            gap: 0.5rem;
        }

        .send-btn {
            background-color: var(--primary-color);
            color: white;
            border: none;
            border-radius: var(--border-radius);
            padding: 0.75rem;
            cursor: pointer;
            font-weight: bold;
            flex: 1;
            display: flex;
            align-items: center;
            justify-content: center;
        }

        .send-btn:hover {
            background-color: var(--secondary-color);
        }

        .features-panel {
            background-color: var(--card-bg-color);
            border-radius: var(--border-radius);
            padding: 1rem;
            display: flex;
            flex-direction: column;
            overflow: hidden;
        }

        .panel-tab-container {
            display: flex;
            overflow: hidden;
            border-bottom: 1px solid rgba(255, 255, 255, 0.1);
            margin-bottom: 1rem;
        }

        .panel-tab {
            padding: 0.5rem 1rem;
            background: none;
            border: none;
            color: var(--text-color);
            cursor: pointer;
            transition: background-color 0.2s;
            font-weight: bold;
        }

        .panel-tab:hover {
            background-color: rgba(255, 255, 255, 0.1);
        }

        .panel-tab.active {
            color: var(--accent-color);
            border-bottom: 2px solid var(--accent-color);
        }

        .panel-content {
            flex: 1;
            overflow-y: auto;
            scrollbar-width: thin;
        }

        .panel-content::-webkit-scrollbar {
            width: 6px;
        }

        .panel-content::-webkit-scrollbar-thumb {
            background-color: var(--secondary-color);
            border-radius: 3px;
        }

        .gambling-container, .shoutbox-container, .transactions-container {
            display: none;
        }

        .gambling-container.active, .shoutbox-container.active, .transactions-container.active {
            display: block;
        }

        .shoutbox-messages {
            margin-bottom: 1rem;
        }

        .shout-message {
            padding: 0.5rem;
            border-radius: var(--border-radius);
            background-color: rgba(255, 255, 255, 0.05);
            margin-bottom: 0.5rem;
        }

        .shout-sender {
            font-weight: bold;
            color: var(--accent-color);
        }

        .shout-form {
            display: flex;
            gap: 0.5rem;
        }

        .shout-input {
            flex: 1;
            padding: 0.5rem;
            background-color: rgba(255, 255, 255, 0.1);
            border: 1px solid rgba(255, 255, 255, 0.2);
            border-radius: var(--border-radius);
            color: var(--text-color);
        }

        .shout-input:focus {
            outline: none;
            border-color: var(--accent-color);
        }

        .shout-btn {
            background-color: var(--accent-color);
            color: white;
            border: none;
            border-radius: var(--border-radius);
            padding: 0.5rem;
            cursor: pointer;
        }

        .gambling-options {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 0.5rem;
            margin-bottom: 1rem;
        }

        .gambling-card {
            background-color: rgba(255, 255, 255, 0.05);
            border-radius: var(--border-radius);
            padding: 1rem;
            cursor: pointer;
            transition: transform 0.2s;
            text-align: center;
        }

        .gambling-card:hover {
            transform: translateY(-2px);
            background-color: rgba(255, 255, 255, 0.1);
        }

        .gambling-icon {
            font-size: 2rem;
            margin-bottom: 0.5rem;
            color: var(--accent-color);
        }

        .pot-container {
            background-color: rgba(255, 255, 255, 0.05);
            border-radius: var(--border-radius);
            padding: 1rem;
            margin-bottom: 1rem;
        }

        .pot-header {
            display: flex;
            justify-content: space-between;
            margin-bottom: 0.5rem;
        }

        .pot-name {
            font-weight: bold;
        }

        .pot-amount {
            color: var(--success-color);
            font-weight: bold;
        }

        .pot-timer {
            color: var(--warning-color);
            margin-bottom: 0.5rem;
            font-size: 0.9rem;
        }

        .pot-form {
            display: flex;
            gap: 0.5rem;
        }

        .pot-input {
            flex: 1;
            padding: 0.5rem;
            background-color: rgba(255, 255, 255, 0.1);
            border: 1px solid rgba(255, 255, 255, 0.2);
            border-radius: var(--border-radius);
            color: var(--text-color);
        }

        .pot-input:focus {
            outline: none;
            border-color: var(--accent-color);
        }

        .pot-btn {
            background-color: var(--success-color);
            color: white;
            border: none;
            border-radius: var(--border-radius);
            padding: 0.5rem;
            cursor: pointer;
        }

        .transaction-item {
            background-color: rgba(255, 255, 255, 0.05);
            border-radius: var(--border-radius);
            padding: 0.75rem;
            margin-bottom: 0.5rem;
        }

        .transaction-header {
            display: flex;
            justify-content: space-between;
            margin-bottom: 0.25rem;
        }

        .transaction-date {
            font-size: 0.8rem;
            color: var(--text-secondary);
        }

        .transaction-amount {
            font-weight: bold;
        }

        .transaction-amount.positive {
            color: var(--success-color);
        }

        .transaction-amount.negative {
            color: var(--danger-color);
        }

        .transaction-description {
            font-size: 0.9rem;
        }

        /* Command modal */
        .modal {
            display: none;
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background-color: rgba(0, 0, 0, 0.7);
            z-index: 100;
            justify-content: center;
            align-items: center;
        }

        .modal.active {
            display: flex;
        }

        .modal-content {
            background-color: var(--card-bg-color);
            border-radius: var(--border-radius);
            width: 100%;
            max-width: 500px;
            padding: 1.5rem;
        }

        .modal-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 1rem;
        }

        .modal-title {
            font-size: 1.5rem;
            font-weight: bold;
        }

        .close-modal {
            background: none;
            border: none;
            color: var(--text-color);
            font-size: 1.5rem;
            cursor: pointer;
        }

        .modal-body {
            margin-bottom: 1.5rem;
        }

        .input-group {
            margin-bottom: 1rem;
        }

        .input-label {
            display: block;
            margin-bottom: 0.5rem;
            font-weight: bold;
        }

        .input-field {
            width: 100%;
            padding: 0.75rem;
            background-color: rgba(255, 255, 255, 0.1);
            border: 1px solid rgba(255, 255, 255, 0.2);
            border-radius: var(--border-radius);
            color: var(--text-color);
        }

        .input-field:focus {
            outline: none;
            border-color: var(--accent-color);
        }

        .modal-footer {
            display: flex;
            justify-content: flex-end;
            gap: 0.5rem;
        }

        .modal-btn {
            padding: 0.75rem 1.5rem;
            border-radius: var(--border-radius);
            font-weight: bold;
            cursor: pointer;
        }

        .cancel-btn {
            background-color: rgba(255, 255, 255, 0.1);
            color: var(--text-color);
            border: none;
        }

        .confirm-btn {
            background-color: var(--primary-color);
            color: white;
            border: none;
        }

        .auth-container {
            display: flex;
            flex-direction: column;
            height: 100vh;
            justify-content: center;
            align-items: center;
            background-color: var(--background-color);
        }

        .auth-box {
            background-color: var(--card-bg-color);
            border-radius: var(--border-radius);
            padding: 2rem;
            width: 100%;
            max-width: 400px;
            box-shadow: 0 10px 25px rgba(0, 0, 0, 0.5);
        }

        .auth-title {
            font-size: 2rem;
            font-weight: bold;
            color: var(--accent-color);
            text-align: center;
            margin-bottom: 2rem;
            display: flex;
            align-items: center;
            justify-content: center;
        }

        .auth-title i {
            margin-right: 0.5rem;
        }

        .auth-form {
            margin-bottom: 1.5rem;
        }

        .auth-submit {
            width: 100%;
            padding: 0.75rem;
            background-color: var(--primary-color);
            color: white;
            border: none;
            border-radius: var(--border-radius);
            font-weight: bold;
            cursor: pointer;
            margin-top: 1rem;
        }

        .auth-submit:hover {
            background-color: var(--secondary-color);
        }

        .auth-toggle {
            text-align: center;
            margin-top: 1rem;
        }

        .auth-toggle-btn {
            background: none;
            border: none;
            color: var(--accent-color);
            cursor: pointer;
        }

        /* Loading indicator */
        .loading {
            display: inline-block;
            width: 80px;
            height: 80px;
            position: fixed;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            z-index: 9999;
        }

        .loading:after {
            content: "" "";
            display: block;
            width: 64px;
            height: 64px;
            margin: 8px;
            border-radius: 50%;
            border: 6px solid var(--primary-color);
            border-color: var(--primary-color) transparent var(--primary-color) transparent;
            animation: loading 1.2s linear infinite;
        }

        @keyframes loading {
            0% {
                transform: rotate(0deg);
            }
            100% {
                transform: rotate(360deg);
            }
        }

        .loading-overlay {
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background-color: rgba(0, 0, 0, 0.7);
            z-index: 9998;
            display: none;
        }

        .loading-overlay.active {
            display: block;
        }

        /* Additional features */
        .quick-actions {
            display: flex;
            gap: 0.5rem;
            margin-top: 0.5rem;
        }

        .action-btn {
            padding: 0.5rem;
            background-color: rgba(255, 255, 255, 0.1);
            border: none;
            border-radius: var(--border-radius);
            color: var(--text-color);
            cursor: pointer;
            display: flex;
            align-items: center;
            gap: 0.25rem;
        }

        .action-btn:hover {
            background-color: rgba(255, 255, 255, 0.2);
        }

        .notification-badge {
            position: absolute;
            top: -5px;
            right: -5px;
            background-color: var(--danger-color);
            color: white;
            border-radius: 50%;
            width: 16px;
            height: 16px;
            font-size: 0.7rem;
            display: flex;
            align-items: center;
            justify-content: center;
        }

        /* Tooltip */
        .tooltip {
            position: relative;
            display: inline-block;
        }

        .tooltip .tooltip-text {
            visibility: hidden;
            width: 120px;
            background-color: #333;
            color: #fff;
            text-align: center;
            border-radius: 6px;
            padding: 5px;
            position: absolute;
            z-index: 1;
            bottom: 125%;
            left: 50%;
            margin-left: -60px;
            opacity: 0;
            transition: opacity 0.3s;
            font-size: 0.8rem;
        }

        .tooltip:hover .tooltip-text {
            visibility: visible;
            opacity: 1;
        }

        /* Responsive adjustments */
        @media (max-width: 1200px) {
            main {
                grid-template-columns: 200px 1fr 250px;
            }
        }

        @media (max-width: 992px) {
            main {
                grid-template-columns: 180px 1fr;
            }
            
            .features-panel {
                display: none;
            }
        }

        @media (max-width: 768px) {
            main {
                grid-template-columns: 1fr;
            }
            
            .sidebar {
                display: none;
            }
            
            .mobile-menu {
                display: block;
            }
        }

        .toggle-view-btn {
            position: fixed;
            bottom: 1rem;
            right: 1rem;
            background-color: var(--primary-color);
            color: white;
            border: none;
            border-radius: 50%;
            width: 50px;
            height: 50px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.5rem;
            cursor: pointer;
            box-shadow: 0 2px 10px rgba(0, 0, 0, 0.3);
            z-index: 99;
        }

        .game-animation {
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background-color: rgba(0, 0, 0, 0.9);
            z-index: 999;
            display: flex;
            flex-direction: column;
            justify-content: center;
            align-items: center;
            display: none;
        }

        .game-animation.active {
            display: flex;
        }

        .dice-container {
            display: flex;
            gap: 2rem;
            margin-bottom: 2rem;
        }

        .dice {
            width: 100px;
            height: 100px;
            background-color: white;
            border-radius: 10px;
            display: flex;
            justify-content: center;
            align-items: center;
            font-size: 3rem;
            font-weight: bold;
            animation: rollDice 1s ease-out;
        }

        @keyframes rollDice {
            0% { transform: rotateX(0deg) rotateY(0deg); }
            25% { transform: rotateX(90deg) rotateY(45deg); }
            50% { transform: rotateX(180deg) rotateY(90deg); }
            75% { transform: rotateX(270deg) rotateY(135deg); }
            100% { transform: rotateX(360deg) rotateY(180deg); }
        }

        .coin-container {
            width: 150px;
            height: 150px;
            position: relative;
            margin-bottom: 2rem;
            perspective: 1000px;
        }

        .coin {
            width: 100%;
            height: 100%;
            position: absolute;
            transform-style: preserve-3d;
            animation: flipCoin 2s ease-out forwards;
        }

        .coin-front, .coin-back {
            width: 100%;
            height: 100%;
            border-radius: 50%;
            position: absolute;
            backface-visibility: hidden;
            display: flex;
            justify-content: center;
            align-items: center;
            font-size: 2rem;
            font-weight: bold;
        }

        .coin-front {
            background-color: gold;
            color: black;
        }

        .coin-back {
            background-color: silver;
            color: black;
            transform: rotateY(180deg);
        }

        @keyframes flipCoin {
            0% { transform: rotateY(0); }
            100% { transform: rotateY(1800deg); }
        }

        .slots-container {
            display: flex;
            gap: 1rem;
            margin-bottom: 2rem;
        }

        .slot {
            width: 80px;
            height: 100px;
            background-color: white;
            border-radius: 10px;
            display: flex;
            justify-content: center;
            align-items: center;
            font-size: 3rem;
            overflow: hidden;
        }

        .slot-reel {
            animation: spinReel 2s ease-out;
        }

        @keyframes spinReel {
            0% { transform: translateY(0); }
            25% { transform: translateY(-500px); }
            50% { transform: translateY(-1000px); }
            75% { transform: translateY(-1500px); }
            100% { transform: translateY(0); }
        }

        .game-result {
            font-size: 2rem;
            font-weight: bold;
            margin-bottom: 2rem;
            text-align: center;
        }

        .game-result.win {
            color: var(--success-color);
        }

        .game-result.lose {
            color: var(--danger-color);
        }

        .close-animation {
            background-color: rgba(255, 255, 255, 0.1);
            color: white;
            border: none;
            border-radius: var(--border-radius);
            padding: 0.75rem 1.5rem;
            cursor: pointer;
            font-weight: bold;
        }
    </style>
</head>
<body>
    <!-- Auth views -->
    <div id=""auth-view"">
        <div class=""auth-container"">
            <div class=""auth-box"">
                <div class=""auth-title"">
                    <i class=""fas fa-comments""></i>
                    YurtCord
                </div>
                
                <div id=""login-form"" class=""auth-form"">
                    <div class=""input-group"">
                        <label class=""input-label"">Username</label>
                        <input type=""text"" id=""login-username"" class=""input-field"" placeholder=""Enter your username"">
                    </div>
                    
                    <div class=""input-group"">
                        <label class=""input-label"">Password</label>
                        <input type=""password"" id=""login-password"" class=""input-field"" placeholder=""Enter your password"">
                    </div>
                    
                    <button id=""login-btn"" class=""auth-submit"">Login</button>
                    
                    <div class=""auth-toggle"">
                        Don't have an account? <button id=""show-register"" class=""auth-toggle-btn"">Register</button>
                    </div>
                </div>
                
                <div id=""register-form"" class=""auth-form"" style=""display: none;"">
                    <div class=""input-group"">
                        <label class=""input-label"">Username</label>
                        <input type=""text"" id=""register-username"" class=""input-field"" placeholder=""Choose a username"">
                    </div>
                    
                    <div class=""input-group"">
                        <label class=""input-label"">Password</label>
                        <input type=""password"" id=""register-password"" class=""input-field"" placeholder=""Choose a password"">
                    </div>
                    
                    <div class=""input-group"">
                        <label class=""input-label"">Confirm Password</label>
                        <input type=""password"" id=""register-confirm"" class=""input-field"" placeholder=""Confirm your password"">
                    </div>
                    
                    <div class=""input-group"">
                        <label class=""input-label"">Email</label>
                        <input type=""email"" id=""register-email"" class=""input-field"" placeholder=""Enter your email"">
                    </div>
                    
                    <button id=""register-btn"" class=""auth-submit"">Register</button>
                    
                    <div class=""auth-toggle"">
                        Already have an account? <button id=""show-login"" class=""auth-toggle-btn"">Login</button>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- Main app view -->
    <div id=""app-view"" style=""display: none;"">
        <header>
            <div class=""header-content"">
                <div class=""logo"">
                    <i class=""fas fa-comments""></i>
                    YurtCord
                </div>
                
                <div class=""user-info"">
                    <div class=""user-badge"">
                        <i class=""fas fa-user""></i>
                        <span id=""username-display"">Username</span>
                    </div>
                    
                    <div class=""credits"">
                        <i class=""fas fa-coins""></i>
                        <span id=""credits-display"">0.00</span>
                    </div>
                </div>
            </div>
            
            <div class=""room-info"">
                <div>
                    Current Room: <span id=""current-room"">lobby</span>
                </div>
                
                <div class=""quick-actions"">
                    <button class=""action-btn"" id=""help-btn"">
                        <i class=""fas fa-question-circle""></i>
                        Help
                    </button>
                    
                    <button class=""action-btn"" id=""create-room-btn"">
                        <i class=""fas fa-plus-circle""></i>
                        Create Room
                    </button>
                    
                    <button class=""action-btn"" id=""transfer-btn"">
                        <i class=""fas fa-exchange-alt""></i>
                        Transfer
                    </button>
                    
                    <button class=""action-btn"" id=""logout-btn"">
                        <i class=""fas fa-sign-out-alt""></i>
                        Logout
                    </button>
                </div>
            </div>
        </header>
        
        <main>
            <div class=""sidebar"">
                <div class=""sidebar-title"">
                    Rooms
                    <button id=""refresh-rooms""><i class=""fas fa-sync-alt""></i></button>
                </div>
                
                <div class=""tab-content"">
                    <ul class=""room-list"" id=""room-list"">
                        <li class=""room-item active"">
                            <div class=""room-icon""><i class=""fas fa-home""></i></div>
                            <span>lobby</span>
                        </li>
                    </ul>
                </div>
            </div>
            
            <div class=""chat-container"">
                <div class=""tabs"">
                    <button class=""tab active"" data-view=""room-chat"">Room Chat</button>
                    <button class=""tab"" data-view=""shoutbox"">Shoutbox</button>
                    <button class=""tab"" data-view=""gambling-view"">Gambling</button>
                </div>
                
                <div class=""chat-view"">
                    <div class=""messages"" id=""messages"">
                        <div class=""message system-message"">
                            <div class=""message-content"">Welcome to YurtCord! Type /help to see available commands.</div>
                        </div>
                    </div>
                    
                    <div class=""message-form"">
                        <textarea id=""message-input"" class=""message-input"" placeholder=""Type a message or command...""></textarea>
                        
                        <div class=""message-actions"">
                            <button id=""send-btn"" class=""send-btn"">
                                <i class=""fas fa-paper-plane""></i>
                            </button>
                        </div>
                    </div>
                </div>
            </div>
            
            <div class=""sidebar"">
                <div class=""sidebar-title"">
                    Online Users
                    <button id=""refresh-users""><i class=""fas fa-sync-alt""></i></button>
                </div>
                
                <div class=""tab-content"">
                    <ul class=""user-list"" id=""user-list"">
                        <!-- User list will be populated dynamically -->
                    </ul>
                </div>
            </div>
        </main>
        
        <!-- Command modals -->
        <div id=""help-modal"" class=""modal"">
            <div class=""modal-content"">
                <div class=""modal-header"">
                    <div class=""modal-title"">Help</div>
                    <button class=""close-modal"">&times;</button>
                </div>
                
                <div class=""modal-body"">
                    <h3>Available Commands</h3>
                    <p><strong>/help</strong> - Display this help message</p>
                    <p><strong>/users</strong> - List all connected users</p>
                    <p><strong>/balance</strong> - Check your credit balance</p>
                    <p><strong>/rooms</strong> - List all available chat rooms</p>
                    <p><strong>/createroom name [description]</strong> - Create a new chat room</p>
                    <p><strong>/join roomName</strong> - Join a chat room</p>
                    <p><strong>/leave</strong> - Leave current room and return to lobby</p>
                    <p><strong>/whisper username message</strong> - Send a private message</p>
                    <p><strong>/transfer username amount [description]</strong> - Send credits to another user</p>
                    <p><strong>/shout message</strong> - Post a message to the global shoutbox</p>
                    <p><strong>/shoutbox</strong> - View recent shoutbox messages</p>
                    <p><strong>/transactions</strong> - View your recent transactions</p>
                    
                    <h3>Gambling Commands</h3>
                    <p><strong>/gamble bet amount</strong> - Place a bet in the active pot</p>
                    <p><strong>/pots</strong> - List all active gambling pots</p>
                    <p><strong>/potinfo potId</strong> - Get detailed information about a specific pot</p>
                    <p><strong>/gamblinghistory</strong> - View recent gambling winners</p>
                    
                    <h3>Casino Games</h3>
                    <p><strong>/dice amount [target]</strong> - Roll dice (target 2-12 is optional)</p>
                    <p><strong>/flip amount choice</strong> - Flip a coin (choice: HEADS or TAILS)</p>
                    <p><strong>/slots amount</strong> - Play the slot machine</p>
                    <p><strong>/stats</strong> - See your gambling statistics</p>
                    <p><strong>/leaderboard</strong> - View top gamblers</p>
                </div>
                
                <div class=""modal-footer"">
                    <button class=""modal-btn confirm-btn"">OK</button>
                </div>
            </div>
        </div>
        
        <div id=""create-room-modal"" class=""modal"">
            <div class=""modal-content"">
                <div class=""modal-header"">
                    <div class=""modal-title"">Create New Room</div>
                    <button class=""close-modal"">&times;</button>
                </div>
                
                <div class=""modal-body"">
                    <div class=""input-group"">
                        <label class=""input-label"">Room Name</label>
                        <input type=""text"" id=""room-name"" class=""input-field"" placeholder=""Enter room name"">
                    </div>
                    
                    <div class=""input-group"">
                        <label class=""input-label"">Description (optional)</label>
                        <input type=""text"" id=""room-description"" class=""input-field"" placeholder=""Enter room description"">
                    </div>
                </div>
                
                <div class=""modal-footer"">
                    <button class=""modal-btn cancel-btn"">Cancel</button>
                    <button id=""create-room-confirm"" class=""modal-btn confirm-btn"">Create</button>
                </div>
            </div>
        </div>
        
        <div id=""transfer-modal"" class=""modal"">
            <div class=""modal-content"">
                <div class=""modal-header"">
                    <div class=""modal-title"">Transfer Credits</div>
                    <button class=""close-modal"">&times;</button>
                </div>
                
                <div class=""modal-body"">
                    <div class=""input-group"">
                        <label class=""input-label"">Recipient</label>
                        <input type=""text"" id=""transfer-recipient"" class=""input-field"" placeholder=""Enter username"">
                    </div>
                    
                    <div class=""input-group"">
                        <label class=""input-label"">Amount</label>
                        <input type=""number"" id=""transfer-amount"" class=""input-field"" placeholder=""Enter amount"">
                    </div>
                    
                    <div class=""input-group"">
                        <label class=""input-label"">Description (optional)</label>
                        <input type=""text"" id=""transfer-description"" class=""input-field"" placeholder=""Enter description"">
                    </div>
                </div>
                
                <div class=""modal-footer"">
                    <button class=""modal-btn cancel-btn"">Cancel</button>
                    <button id=""transfer-confirm"" class=""modal-btn confirm-btn"">Transfer</button>
                </div>
            </div>
        </div>
        
        <div id=""dice-modal"" class=""modal"">
            <div class=""modal-content"">
                <div class=""modal-header"">
                    <div class=""modal-title"">Play Dice</div>
                    <button class=""close-modal"">&times;</button>
                </div>
                
                <div class=""modal-body"">
                    <div class=""input-group"">
                        <label class=""input-label"">Bet Amount</label>
                        <input type=""number"" id=""dice-amount"" class=""input-field"" placeholder=""Enter bet amount"">
                    </div>
                    
                    <div class=""input-group"">
                        <label class=""input-label"">Target Number (optional, 2-12)</label>
                        <input type=""number"" id=""dice-target"" class=""input-field"" placeholder=""Enter target number"" min=""2"" max=""12"">
                    </div>
                </div>
                
                <div class=""modal-footer"">
                    <button class=""modal-btn cancel-btn"">Cancel</button>
                    <button id=""dice-confirm"" class=""modal-btn confirm-btn"">Roll Dice</button>
                </div>
            </div>
        </div>
        
        <div id=""flip-modal"" class=""modal"">
            <div class=""modal-content"">
                <div class=""modal-header"">
                    <div class=""modal-title"">Flip Coin</div>
                    <button class=""close-modal"">&times;</button>
                </div>
                
                <div class=""modal-body"">
                    <div class=""input-group"">
                        <label class=""input-label"">Bet Amount</label>
                        <input type=""number"" id=""flip-amount"" class=""input-field"" placeholder=""Enter bet amount"">
                    </div>
                    
                    <div class=""input-group"">
                        <label class=""input-label"">Your Choice</label>
                        <select id=""flip-choice"" class=""input-field"">
                            <option value=""HEADS"">HEADS</option>
                            <option value=""TAILS"">TAILS</option>
                        </select>
                    </div>
                </div>
                
                <div class=""modal-footer"">
                    <button class=""modal-btn cancel-btn"">Cancel</button>
                    <button id=""flip-confirm"" class=""modal-btn confirm-btn"">Flip Coin</button>
                </div>
            </div>
        </div>
        
        <div id=""slots-modal"" class=""modal"">
            <div class=""modal-content"">
                <div class=""modal-header"">
                    <div class=""modal-title"">Play Slots</div>
                    <button class=""close-modal"">&times;</button>
                </div>
                
                <div class=""modal-body"">
                    <div class=""input-group"">
                        <label class=""input-label"">Bet Amount</label>
                        <input type=""number"" id=""slots-amount"" class=""input-field"" placeholder=""Enter bet amount"">
                    </div>
                </div>
                
                <div class=""modal-footer"">
                    <button class=""modal-btn cancel-btn"">Cancel</button>
                    <button id=""slots-confirm"" class=""modal-btn confirm-btn"">Spin</button>
                </div>
            </div>
        </div>
        
        <div id=""gamble-modal"" class=""modal"">
            <div class=""modal-content"">
                <div class=""modal-header"">
                    <div class=""modal-title"">Place Bet</div>
                    <button class=""close-modal"">&times;</button>
                </div>
                
                <div class=""modal-body"">
                    <div id=""pot-info"">
                        <h3 id=""pot-name"">Daily Jackpot</h3>
                        <p id=""pot-description"">Daily jackpot that ends in 24 hours</p>
                        <p>Total pot: <span id=""pot-total"">0.00</span> credits</p>
                        <p>Ends in: <span id=""pot-time-remaining"">23h 59m</span></p>
                    </div>
                    
                    <div class=""input-group"">
                        <label class=""input-label"">Bet Amount</label>
                        <input type=""number"" id=""gamble-amount"" class=""input-field"" placeholder=""Enter bet amount"">
                    </div>
                </div>
                
                <div class=""modal-footer"">
                    <button class=""modal-btn cancel-btn"">Cancel</button>
                    <button id=""gamble-confirm"" class=""modal-btn confirm-btn"">Place Bet</button>
                </div>
            </div>
        </div>
        
        <div id=""whisper-modal"" class=""modal"">
            <div class=""modal-content"">
                <div class=""modal-header"">
                    <div class=""modal-title"">Whisper Message</div>
                    <button class=""close-modal"">&times;</button>
                </div>
                
                <div class=""modal-body"">
                    <div class=""input-group"">
                        <label class=""input-label"">To</label>
                        <input type=""text"" id=""whisper-recipient"" class=""input-field"" placeholder=""Enter username"">
                    </div>
                    
                    <div class=""input-group"">
                        <label class=""input-label"">Message</label>
                        <textarea id=""whisper-message"" class=""input-field"" placeholder=""Enter your message""></textarea>
                    </div>
                </div>
                
                <div class=""modal-footer"">
                    <button class=""modal-btn cancel-btn"">Cancel</button>
                    <button id=""whisper-confirm"" class=""modal-btn confirm-btn"">Send</button>
                </div>
            </div>
        </div>
        
        <!-- Game animations -->
        <div id=""dice-animation"" class=""game-animation"">
            <div class=""dice-container"">
                <div class=""dice"" id=""dice1"">6</div>
                <div class=""dice"" id=""dice2"">6</div>
            </div>
            
            <div class=""game-result"" id=""dice-result"">Rolling...</div>
            
            <button class=""close-animation"" id=""close-dice"">Continue</button>
        </div>
        
        <div id=""coin-animation"" class=""game-animation"">
            <div class=""coin-container"">
                <div class=""coin"" id=""coin"">
                    <div class=""coin-front"">H</div>
                    <div class=""coin-back"">T</div>
                </div>
            </div>
            
            <div class=""game-result"" id=""coin-result"">Flipping...</div>
            
            <button class=""close-animation"" id=""close-coin"">Continue</button>
        </div>
        
        <div id=""slots-animation"" class=""game-animation"">
            <div class=""slots-container"">
                <div class=""slot""><div class=""slot-reel"" id=""slot1"">🍒</div></div>
                <div class=""slot""><div class=""slot-reel"" id=""slot2"">🍋</div></div>
                <div class=""slot""><div class=""slot-reel"" id=""slot3"">🍇</div></div>
            </div>
            
            <div class=""game-result"" id=""slots-result"">Spinning...</div>
            
            <button class=""close-animation"" id=""close-slots"">Continue</button>
        </div>
        
        <!-- Loading indicator -->
        <div class=""loading-overlay"" id=""loading-overlay"">
            <div class=""loading""></div>
        </div>
        
        <!-- Toggle view button -->
        <button id=""switch-to-console"" class=""toggle-view-btn"" title=""Switch to Console View"">
            <i class=""fas fa-terminal""></i>
        </button>
    </div>

    <script>
        // Socket connection
        let socket;
        let isConnected = false;
        let username = '';
        let credits = 0;
        let currentRoom = 'lobby';
        let activeTab = 'room-chat';
        
        // DOM elements
        const authView = document.getElementById('auth-view');
        const appView = document.getElementById('app-view');
        const loginForm = document.getElementById('login-form');
        const registerForm = document.getElementById('register-form');
        const showRegisterBtn = document.getElementById('show-register');
        const showLoginBtn = document.getElementById('show-login');
        const loginBtn = document.getElementById('login-btn');
        const registerBtn = document.getElementById('register-btn');
        const messagesContainer = document.getElementById('messages');
        const messageInput = document.getElementById('message-input');
        const sendBtn = document.getElementById('send-btn');
        const usernameDisplay = document.getElementById('username-display');
        const creditsDisplay = document.getElementById('credits-display');
        const currentRoomDisplay = document.getElementById('current-room');
        const roomList = document.getElementById('room-list');
        const userList = document.getElementById('user-list');
        const helpBtn = document.getElementById('help-btn');
        const createRoomBtn = document.getElementById('create-room-btn');
        const transferBtn = document.getElementById('transfer-btn');
        const logoutBtn = document.getElementById('logout-btn');
        const refreshRoomsBtn = document.getElementById('refresh-rooms');
        const refreshUsersBtn = document.getElementById('refresh-users');
        const switchToConsoleBtn = document.getElementById('switch-to-console');
        const loadingOverlay = document.getElementById('loading-overlay');
        
        // Modals
        const helpModal = document.getElementById('help-modal');
        const createRoomModal = document.getElementById('create-room-modal');
        const transferModal = document.getElementById('transfer-modal');
        const diceModal = document.getElementById('dice-modal');
        const flipModal = document.getElementById('flip-modal');
        const slotsModal = document.getElementById('slots-modal');
        const gambleModal = document.getElementById('gamble-modal');
        const whisperModal = document.getElementById('whisper-modal');
        
        // Game animations
        const diceAnimation = document.getElementById('dice-animation');
        const coinAnimation = document.getElementById('coin-animation');
        const slotsAnimation = document.getElementById('slots-animation');
        
        // Initialize WebSocket connection
        function initSocket() {
            // Create WebSocket connection to our local server
            // This will be handled by the C# application proxy
            socket = new WebSocket(`ws://${window.location.hostname}:8081`);
            
            socket.onopen = function(event) {
                console.log('Connected to server');
                isConnected = true;
            };
            
            socket.onmessage = function(event) {
                const message = event.data;
                handleServerMessage(message);
            };
            
            socket.onclose = function(event) {
                console.log('Disconnected from server');
                isConnected = false;
                
                // Show reconnect message
                addSystemMessage('Connection lost. Attempting to reconnect...');
                
                // Try to reconnect after 3 seconds
                setTimeout(initSocket, 3000);
            };
            
            socket.onerror = function(error) {
                console.error('WebSocket error:', error);
            };
        }
        
        // Send message to server
        function sendToServer(message) {
            if (isConnected) {
                socket.send(message);
            } else {
                addSystemMessage('Not connected to server. Please try again later.');
            }
        }
        
        // Handle messages from server
        function handleServerMessage(message) {
            console.log('Received:', message);
            
            // Check for authentication response
            if (message.includes('Authentication successful')) {
                // Extract username
                const match = message.match(/Welcome, (.+)!/);
                if (match && match[1]) {
                    username = match[1];
                    usernameDisplay.textContent = username;
                    
                    // Hide auth view, show app view
                    authView.style.display = 'none';
                    appView.style.display = 'block';
                    
                    // Focus message input
                    messageInput.focus();
                }
            }
            
            // Check for credit balance
            if (message.startsWith('Your current credit balance:')) {
                const match = message.match(/Your current credit balance: ([\d.]+)/);
                if (match && match[1]) {
                    credits = parseFloat(match[1]);
                    creditsDisplay.textContent = credits.toFixed(2);
                }
            }
            
            // Check for room change
            if (message.startsWith('You have joined room:')) {
                const match = message.match(/You have joined room: (.+)/);
                if (match && match[1]) {
                    currentRoom = match[1];
                    currentRoomDisplay.textContent = currentRoom;
                    
                    // Update room list
                    updateRoomListSelection();
                }
            }
            
            // Add message to chat
            if (message.startsWith('[PM')) {
                addPrivateMessage(message);
            } else if (message.startsWith('*')) {
                addSystemMessage(message);
            } else if (message.startsWith('System:')) {
                addSystemMessage(message.substring(7).trim());
            } else if (message.startsWith('[SERVER')) {
                addServerMessage(message);
            } else if (message.startsWith('[SHOUTBOX]')) {
                addShoutboxMessage(message);
            } else if (message.startsWith('[GAMBLING]')) {
                addGamblingMessage(message);
            } else if (message.startsWith('Available commands')) {
                // Help command output - show in help modal
                showHelpModal();
                
                // Also add to chat
                addSystemMessage(message);
            } else {
                // Regular message
                addChatMessage(message);
            }
            
            // Handle dice game result
            if (message.includes('You rolled') && message.includes('dice')) {
                const dice1Match = message.match(/You rolled (\d+)/);
                const dice2Match = message.match(/and (\d+) for a total/);
                
                if (dice1Match && dice2Match) {
                    const dice1 = parseInt(dice1Match[1]);
                    const dice2 = parseInt(dice2Match[1]);
                    
                    // Show dice animation
                    showDiceAnimation(dice1, dice2, message);
                }
            }
            
            // Handle coin flip result
            if (message.includes('The coin shows')) {
                const resultMatch = message.match(/The coin shows (HEADS|TAILS)!/);
                if (resultMatch) {
                    const coinResult = resultMatch[1];
                    showCoinAnimation(coinResult, message);
                }
            }
            
            // Handle slots result
            if (message.includes('Spinning the reels')) {
                // Get the next message which contains the slots result
                const slotsLine = message.split('\n').find(line => line.includes('[') && line.includes(']'));
                if (slotsLine) {
                    const symbolsMatch = slotsLine.match(/\[ (.+) \| (.+) \| (.+) \]/);
                    if (symbolsMatch) {
                        const symbol1 = symbolsMatch[1];
                        const symbol2 = symbolsMatch[2];
                        const symbol3 = symbolsMatch[3];
                        
                        // Show slots animation
                        showSlotsAnimation(symbol1, symbol2, symbol3, message);
                    }
                }
            }
            
            // Hide loading overlay when we receive a response
            hideLoading();
        }
        
        // Message display functions
        function addChatMessage(message) {
            // Try to extract username and content from messages like ""[Username] Message""
            const usernameMatch = message.match(/^\[(.+?)\] (.+)$/);
            
            if (usernameMatch) {
                const sender = usernameMatch[1];
                const content = usernameMatch[2];
                
                const messageElement = document.createElement('div');
                messageElement.className = 'message';
                
                const headerElement = document.createElement('div');
                headerElement.className = 'message-header';
                
                const senderElement = document.createElement('div');
                senderElement.className = 'message-sender';
                senderElement.textContent = sender;
                
                const timeElement = document.createElement('div');
                timeElement.className = 'message-time';
                timeElement.textContent = new Date().toLocaleTimeString();
                
                headerElement.appendChild(senderElement);
                headerElement.appendChild(timeElement);
                
                const contentElement = document.createElement('div');
                contentElement.className = 'message-content';
                contentElement.textContent = content;
                
                messageElement.appendChild(headerElement);
                messageElement.appendChild(contentElement);
                
                messagesContainer.appendChild(messageElement);
            } else {
                // Fallback for messages that don't match the expected format
                const messageElement = document.createElement('div');
                messageElement.className = 'message';
                
                const contentElement = document.createElement('div');
                contentElement.className = 'message-content';
                contentElement.textContent = message;
                
                messageElement.appendChild(contentElement);
                
                messagesContainer.appendChild(messageElement);
            }
            
            scrollToBottom();
        }
        
        function addPrivateMessage(message) {
            const messageElement = document.createElement('div');
            messageElement.className = 'message private-message';
            
            const contentElement = document.createElement('div');
            contentElement.className = 'message-content';
            contentElement.textContent = message;
            
            messageElement.appendChild(contentElement);
            
            messagesContainer.appendChild(messageElement);
            scrollToBottom();
        }
        
        function addSystemMessage(message) {
            const messageElement = document.createElement('div');
            messageElement.className = 'message system-message';
            
            const contentElement = document.createElement('div');
            contentElement.className = 'message-content';
            contentElement.textContent = message;
            
            messageElement.appendChild(contentElement);
            
            messagesContainer.appendChild(messageElement);
            scrollToBottom();
        }
        
        function addServerMessage(message) {
            const messageElement = document.createElement('div');
            messageElement.className = 'message server-message';
            
            const contentElement = document.createElement('div');
            contentElement.className = 'message-content';
            contentElement.textContent = message;
            
            messageElement.appendChild(contentElement);
            
            messagesContainer.appendChild(messageElement);
            scrollToBottom();
        }
        
        function addShoutboxMessage(message) {
            const messageElement = document.createElement('div');
            messageElement.className = 'message shoutbox-message';
            
            const contentElement = document.createElement('div');
            contentElement.className = 'message-content';
            contentElement.textContent = message;
            
            messageElement.appendChild(contentElement);
            
            messagesContainer.appendChild(messageElement);
            scrollToBottom();
        }
        
        function addGamblingMessage(message) {
            const messageElement = document.createElement('div');
            messageElement.className = 'message gambling-message';
            
            const contentElement = document.createElement('div');
            contentElement.className = 'message-content';
            contentElement.textContent = message;
            
            messageElement.appendChild(contentElement);
            
            messagesContainer.appendChild(messageElement);
            scrollToBottom();
        }
        
        function scrollToBottom() {
            messagesContainer.scrollTop = messagesContainer.scrollHeight;
        }
        
        // Authentication
        function login() {
            const username = document.getElementById('login-username').value;
            const password = document.getElementById('login-password').value;
            
            if (!username || !password) {
                alert('Please enter both username and password.');
                return;
            }
            
            showLoading();
            sendToServer(`LOGIN ${username} ${password}`);
        }
        
        function register() {
            const username = document.getElementById('register-username').value;
            const password = document.getElementById('register-password').value;
            const confirm = document.getElementById('register-confirm').value;
            const email = document.getElementById('register-email').value;
            
            if (!username || !password || !confirm || !email) {
                alert('Please fill in all fields.');
                return;
            }
            
            if (password !== confirm) {
                alert('Passwords do not match.');
                return;
            }
            
            showLoading();
            sendToServer(`REGISTER ${username} ${password} ${email}`);
        }
        
        // Room functions
        function updateRoomList(rooms) {
            roomList.innerHTML = '';
            
            rooms.forEach(room => {
                const roomItem = document.createElement('li');
                roomItem.className = 'room-item';
                if (room === currentRoom) {
                    roomItem.classList.add('active');
                }
                
                const roomIcon = document.createElement('div');
                roomIcon.className = 'room-icon';
                
                const icon = document.createElement('i');
                icon.className = room === 'lobby' ? 'fas fa-home' : 'fas fa-comment';
                
                roomIcon.appendChild(icon);
                
                const roomName = document.createElement('span');
                roomName.textContent = room;
                
                roomItem.appendChild(roomIcon);
                roomItem.appendChild(roomName);
                
                roomItem.addEventListener('click', () => {
                    joinRoom(room);
                });
                
                roomList.appendChild(roomItem);
            });
        }
        
        function updateRoomListSelection() {
            const roomItems = roomList.querySelectorAll('.room-item');
            roomItems.forEach(item => {
                const roomNameElement = item.querySelector('span');
                if (roomNameElement && roomNameElement.textContent === currentRoom) {
                    item.classList.add('active');
                } else {
                    item.classList.remove('active');
                }
            });
        }
        
        function joinRoom(roomName) {
            if (roomName === currentRoom) return;
            
            showLoading();
            sendToServer(`JOIN ${roomName}`);
        }
        
        function createRoom() {
            const roomName = document.getElementById('room-name').value;
            const description = document.getElementById('room-description').value;
            
            if (!roomName) {
                alert('Please enter a room name.');
                return;
            }
            
            hideModal(createRoomModal);
            showLoading();
            
            if (description) {
                sendToServer(`CREATEROOM ${roomName} ${description}`);
            } else {
                sendToServer(`CREATEROOM ${roomName}`);
            }
        }
        
        // User functions
        function updateUserList(users) {
            userList.innerHTML = '';
            
            users.forEach(user => {
                const userItem = document.createElement('li');
                userItem.className = 'user-item';
                
                const userStatus = document.createElement('div');
                userStatus.className = 'user-status';
                
                const icon = document.createElement('i');
                icon.className = 'fas fa-circle';
                
                userStatus.appendChild(icon);
                
                const userName = document.createElement('span');
                userName.textContent = user;
                
                userItem.appendChild(userStatus);
                userItem.appendChild(userName);
                
                // Add context menu for user items
                userItem.addEventListener('contextmenu', (e) => {
                    e.preventDefault();
                    showUserContextMenu(user, e.clientX, e.clientY);
                });
                
                // Add click for whisper
                userItem.addEventListener('click', () => {
                    showWhisperModal(user);
                });
                
                userList.appendChild(userItem);
            });
        }
        
        function showUserContextMenu(user, x, y) {
            // Remove existing context menu if any
            const existingMenu = document.querySelector('.user-context-menu');
            if (existingMenu) {
                existingMenu.remove();
            }
            
            // Create context menu
            const menu = document.createElement('div');
            menu.className = 'user-context-menu';
            menu.style.position = 'fixed';
            menu.style.left = `${x}px`;
            menu.style.top = `${y}px`;
            menu.style.backgroundColor = 'var(--card-bg-color)';
            menu.style.border = '1px solid rgba(255, 255, 255, 0.2)';
            menu.style.borderRadius = 'var(--border-radius)';
            menu.style.padding = '0.5rem';
            menu.style.zIndex = '1000';
            menu.style.boxShadow = '0 2px 10px rgba(0, 0, 0, 0.3)';
            
            // Whisper option
            const whisperOption = document.createElement('div');
            whisperOption.textContent = 'Whisper';
            whisperOption.style.padding = '0.5rem 1rem';
            whisperOption.style.cursor = 'pointer';
            whisperOption.style.borderRadius = 'var(--border-radius)';
            whisperOption.addEventListener('mouseover', () => {
                whisperOption.style.backgroundColor = 'rgba(255, 255, 255, 0.1)';
            });
            whisperOption.addEventListener('mouseout', () => {
                whisperOption.style.backgroundColor = 'transparent';
            });
            whisperOption.addEventListener('click', () => {
                menu.remove();
                showWhisperModal(user);
            });
            
            // Transfer option
            const transferOption = document.createElement('div');
            transferOption.textContent = 'Transfer Credits';
            transferOption.style.padding = '0.5rem 1rem';
            transferOption.style.cursor = 'pointer';
            transferOption.style.borderRadius = 'var(--border-radius)';
            transferOption.addEventListener('mouseover', () => {
                transferOption.style.backgroundColor = 'rgba(255, 255, 255, 0.1)';
            });
            transferOption.addEventListener('mouseout', () => {
                transferOption.style.backgroundColor = 'transparent';
            });
            transferOption.addEventListener('click', () => {
                menu.remove();
                showTransferModal(user);
            });
            
            menu.appendChild(whisperOption);
            menu.appendChild(transferOption);
            
            // Add menu to body
            document.body.appendChild(menu);
            
            // Close menu when clicking outside
            document.addEventListener('click', function closeMenu(e) {
                if (!menu.contains(e.target)) {
                    menu.remove();
                    document.removeEventListener('click', closeMenu);
                }
            });
        }
        
        function showWhisperModal(recipient = '') {
            if (recipient) {
                document.getElementById('whisper-recipient').value = recipient;
            } else {
                document.getElementById('whisper-recipient').value = '';
            }
            document.getElementById('whisper-message').value = '';
            showModal(whisperModal);
        }
        
        function sendWhisper() {
            const recipient = document.getElementById('whisper-recipient').value;
            const message = document.getElementById('whisper-message').value;
            
            if (!recipient || !message) {
                alert('Please enter both recipient and message.');
                return;
            }
            
            hideModal(whisperModal);
            sendToServer(`WHISPER ${recipient} ${message}`);
        }
        
        // Transfer functions
        function showTransferModal(recipient = '') {
            if (recipient) {
                document.getElementById('transfer-recipient').value = recipient;
            } else {
                document.getElementById('transfer-recipient').value = '';
            }
            document.getElementById('transfer-amount').value = '';
            document.getElementById('transfer-description').value = '';
            showModal(transferModal);
        }
        
        function transferCredits() {
            const recipient = document.getElementById('transfer-recipient').value;
            const amount = document.getElementById('transfer-amount').value;
            const description = document.getElementById('transfer-description').value;
            
            if (!recipient || !amount) {
                alert('Please enter both recipient and amount.');
                return;
            }
            
            if (parseFloat(amount) <= 0) {
                alert('Please enter a positive amount.');
                return;
            }
            
            hideModal(transferModal);
            showLoading();
            
            if (description) {
                sendToServer(`TRANSFER ${recipient} ${amount} ${description}`);
            } else {
                sendToServer(`TRANSFER ${recipient} ${amount}`);
            }
        }
        
        // Gambling functions
        function showDiceModal() {
            document.getElementById('dice-amount').value = '';
            document.getElementById('dice-target').value = '';
            showModal(diceModal);
        }
        
        function playDice() {
            const amount = document.getElementById('dice-amount').value;
            const target = document.getElementById('dice-target').value;
            
            if (!amount) {
                alert('Please enter a bet amount.');
                return;
            }
            
            if (parseFloat(amount) <= 0) {
                alert('Please enter a positive amount.');
                return;
            }
            
            hideModal(diceModal);
            showLoading();
            
            if (target) {
                sendToServer(`DICE ${amount} ${target}`);
            } else {
                sendToServer(`DICE ${amount}`);
            }
        }
        
        function showDiceAnimation(dice1, dice2, resultMessage) {
            const dice1Element = document.getElementById('dice1');
            const dice2Element = document.getElementById('dice2');
            const resultElement = document.getElementById('dice-result');
            
            dice1Element.textContent = dice1;
            dice2Element.textContent = dice2;
            
            // Extract win/lose info
            const isWin = resultMessage.includes('Winner!') || resultMessage.includes('You win');
            const isLose = resultMessage.includes('You lose');
            
            if (isWin) {
                resultElement.className = 'game-result win';
                resultElement.textContent = 'You Win!';
            } else if (isLose) {
                resultElement.className = 'game-result lose';
                resultElement.textContent = 'You Lose!';
            } else {
                resultElement.className = 'game-result';
                resultElement.textContent = 'Push!';
            }
            
            diceAnimation.classList.add('active');
        }
        
        function showFlipModal() {
            document.getElementById('flip-amount').value = '';
            showModal(flipModal);
        }
        
        function playCoinFlip() {
            const amount = document.getElementById('flip-amount').value;
            const choice = document.getElementById('flip-choice').value;
            
            if (!amount) {
                alert('Please enter a bet amount.');
                return;
            }
            
            if (parseFloat(amount) <= 0) {
                alert('Please enter a positive amount.');
                return;
            }
            
            hideModal(flipModal);
            showLoading();
            
            sendToServer(`FLIP ${amount} ${choice}`);
        }
        
        function showCoinAnimation(result, resultMessage) {
            const coinElement = document.getElementById('coin');
            const resultElement = document.getElementById('coin-result');
            
            // Set the final state of the coin
            if (result === 'HEADS') {
                coinElement.style.transform = 'rotateY(1800deg)';
            } else {
                coinElement.style.transform = 'rotateY(1980deg)';
            }
            
            // Extract win/lose info
            const isWin = resultMessage.includes('was correct') || resultMessage.includes('You win');
            const isLose = resultMessage.includes('was incorrect') || resultMessage.includes('You lose');
            
            if (isWin) {
                resultElement.className = 'game-result win';
                resultElement.textContent = `${result}! You Win!`;
            } else {
                resultElement.className = 'game-result lose';
                resultElement.textContent = `${result}! You Lose!`;
            }
            
            coinAnimation.classList.add('active');
        }
        
        function showSlotsModal() {
            document.getElementById('slots-amount').value = '';
            showModal(slotsModal);
        }
        
        function playSlotsGame() {
            const amount = document.getElementById('slots-amount').value;
            
            if (!amount) {
                alert('Please enter a bet amount.');
                return;
            }
            
            if (parseFloat(amount) <= 0) {
                alert('Please enter a positive amount.');
                return;
            }
            
            hideModal(slotsModal);
            showLoading();
            
            sendToServer(`SLOTS ${amount}`);
        }
        
        function showSlotsAnimation(symbol1, symbol2, symbol3, resultMessage) {
            const slot1Element = document.getElementById('slot1');
            const slot2Element = document.getElementById('slot2');
            const slot3Element = document.getElementById('slot3');
            const resultElement = document.getElementById('slots-result');
            
            slot1Element.textContent = symbol1;
            slot2Element.textContent = symbol2;
            slot3Element.textContent = symbol3;
            
            // Extract win/lose info
            const isWin = resultMessage.includes('JACKPOT') || 
                          resultMessage.includes('You win') || 
                          resultMessage.includes('matching symbols');
            
            if (isWin) {
                resultElement.className = 'game-result win';
                if (resultMessage.includes('JACKPOT')) {
                    resultElement.textContent = 'JACKPOT!';
                } else {
                    resultElement.textContent = 'You Win!';
                }
            } else {
                resultElement.className = 'game-result lose';
                resultElement.textContent = 'No Match. You Lose!';
            }
            
            slotsAnimation.classList.add('active');
        }
        
        function showGambleModal() {
            document.getElementById('gamble-amount').value = '';
            showModal(gambleModal);
        }
        
        function placeBet() {
            const amount = document.getElementById('gamble-amount').value;
            
            if (!amount) {
                alert('Please enter a bet amount.');
                return;
            }
            
            if (parseFloat(amount) <= 0) {
                alert('Please enter a positive amount.');
                return;
            }
            
            hideModal(gambleModal);
            showLoading();
            
            sendToServer(`GAMBLE bet ${amount}`);
        }
        
        // Modal handling
        function showModal(modal) {
            modal.classList.add('active');
            
            // Focus first input field
            setTimeout(() => {
                const firstInput = modal.querySelector('input, textarea, select');
                if (firstInput) firstInput.focus();
            }, 100);
        }
        
        function hideModal(modal) {
            modal.classList.remove('active');
        }
        
        function showHelpModal() {
            showModal(helpModal);
        }
        
        // Loading indicator
        function showLoading() {
            loadingOverlay.classList.add('active');
        }
        
        function hideLoading() {
            loadingOverlay.classList.remove('active');
        }
        
        // Handle command messages
        function handleCommand(command) {
            const parts = command.split(' ');
            const cmd = parts[0].toLowerCase();
            
            switch (cmd) {
                case '/help':
                    showLoading();
                    sendToServer('HELP');
                    break;
                case '/users':
                    showLoading();
                    sendToServer('USERS');
                    break;
                case '/balance':
                    showLoading();
                    sendToServer('BALANCE');
                    break;
                case '/rooms':
                    showLoading();
                    sendToServer('ROOMS');
                    break;
                case '/join':
                    if (parts.length > 1) {
                        showLoading();
                        sendToServer(`JOIN ${parts.slice(1).join(' ')}`);
                    } else {
                        addSystemMessage('Usage: /join roomName');
                    }
                    break;
                case '/leave':
                    showLoading();
                    sendToServer('LEAVE');
                    break;
                case '/create':
                case '/createroom':
                    if (parts.length > 1) {
                        showLoading();
                        sendToServer(`CREATEROOM ${parts.slice(1).join(' ')}`);
                    } else {
                        addSystemMessage('Usage: /create roomName [description]');
                    }
                    break;
                case '/whisper':
                case '/w':
                    if (parts.length > 2) {
                        const target = parts[1];
                        const message = parts.slice(2).join(' ');
                        showLoading();
                        sendToServer(`WHISPER ${target} ${message}`);
                    } else {
                        addSystemMessage('Usage: /whisper username message');
                    }
                    break;
                case '/transfer':
                    if (parts.length > 2) {
                        const target = parts[1];
                        const amount = parts[2];
                        const description = parts.length > 3 ? parts.slice(3).join(' ') : '';
                        
                        showLoading();
                        if (description) {
                            sendToServer(`TRANSFER ${target} ${amount} ${description}`);
                        } else {
                            sendToServer(`TRANSFER ${target} ${amount}`);
                        }
                    } else {
                        addSystemMessage('Usage: /transfer username amount [description]');
                    }
                    break;
                case '/shout':
                    if (parts.length > 1) {
                        showLoading();
                        sendToServer(`SHOUT ${parts.slice(1).join(' ')}`);
                    } else {
                        addSystemMessage('Usage: /shout message');
                    }
                    break;
                case '/shoutbox':
                    showLoading();
                    sendToServer('SHOUTBOX');
                    break;
                case '/transactions':
                    showLoading();
                    sendToServer('TRANSACTIONS');
                    break;
                case '/dice':
                    if (parts.length > 1) {
                        const amount = parts[1];
                        const target = parts.length > 2 ? parts[2] : '';
                        
                        showLoading();
                        if (target) {
                            sendToServer(`DICE ${amount} ${target}`);
                        } else {
                            sendToServer(`DICE ${amount}`);
                        }
                    } else {
                        addSystemMessage('Usage: /dice amount [target]');
                    }
                    break;
                case '/flip':
                    if (parts.length > 2) {
                        const amount = parts[1];
                        const choice = parts[2].toUpperCase();
                        
                        if (choice !== 'HEADS' && choice !== 'TAILS') {
                            addSystemMessage('Choice must be HEADS or TAILS');
                            return;
                        }
                        
                        showLoading();
                        sendToServer(`FLIP ${amount} ${choice}`);
                    } else {
                        addSystemMessage('Usage: /flip amount choice');
                    }
                    break;
                case '/slots':
                    if (parts.length > 1) {
                        const amount = parts[1];
                        
                        showLoading();
                        sendToServer(`SLOTS ${amount}`);
                    } else {
                        addSystemMessage('Usage: /slots amount');
                    }
                    break;
                case '/gamble':
                    if (parts.length > 2 && parts[1].toLowerCase() === 'bet') {
                        const amount = parts[2];
                        
                        showLoading();
                        sendToServer(`GAMBLE bet ${amount}`);
                    } else {
                        addSystemMessage('Usage: /gamble bet amount');
                    }
                    break;
                case '/pots':
                    showLoading();
                    sendToServer('POTS');
                    break;
                case '/potinfo':
                    if (parts.length > 1) {
                        showLoading();
                        sendToServer(`POTINFO ${parts[1]}`);
                    } else {
                        addSystemMessage('Usage: /potinfo potId');
                    }
                    break;
                case '/gambling':
                case '/gamblinghistory':
                    showLoading();
                    sendToServer('GAMBLINGHISTORY');
                    break;
                case '/stats':
                    showLoading();
                    sendToServer('STATS');
                    break;
                case '/leaderboard':
                    showLoading();
                    sendToServer('LEADERBOARD');
                    break;
                case '/quit':
                case '/exit':
                    showLoading();
                    sendToServer('QUIT');
                    // Redirect to login page
                    setTimeout(() => {
                        window.location.reload();
                    }, 1000);
                    break;
                default:
                    addSystemMessage(`Unknown command: ${cmd}. Type /help for available commands.`);
                    break;
            }
        }
        
        // Event listeners
        document.addEventListener('DOMContentLoaded', function() {
            // Initialize WebSocket
            initSocket();
            
            // Auth form toggle
            showRegisterBtn.addEventListener('click', function() {
                loginForm.style.display = 'none';
                registerForm.style.display = 'block';
            });
            
            showLoginBtn.addEventListener('click', function() {
                registerForm.style.display = 'none';
                loginForm.style.display = 'block';
            });
            
            // Login form submission
            loginBtn.addEventListener('click', login);
            
            // Register form submission
            registerBtn.addEventListener('click', register);
            
            // Send message button
            sendBtn.addEventListener('click', function() {
                const message = messageInput.value.trim();
                
                if (!message) return;
                
                // Clear input
                messageInput.value = '';
                
                // Check if it's a command
                if (message.startsWith('/')) {
                    handleCommand(message);
                } else {
                    // Regular chat message
                    sendToServer(message);
                }
                
                // Focus input
                messageInput.focus();
            });
            
            // Send on Enter key (but allow Shift+Enter for new lines)
            messageInput.addEventListener('keydown', function(event) {
                if (event.key === 'Enter' && !event.shiftKey) {
                    event.preventDefault();
                    sendBtn.click();
                }
            });
            
            // Header buttons
            helpBtn.addEventListener('click', showHelpModal);
            createRoomBtn.addEventListener('click', function() {
                showModal(createRoomModal);
            });
            transferBtn.addEventListener('click', function() {
                showTransferModal();
            });
            logoutBtn.addEventListener('click', function() {
                sendToServer('QUIT');
                // Redirect to login page
                setTimeout(() => {
                    window.location.reload();
                }, 1000);
            });
            
            // Refresh buttons
            refreshRoomsBtn.addEventListener('click', function() {
                showLoading();
                sendToServer('ROOMS');
            });
            
            refreshUsersBtn.addEventListener('click', function() {
                showLoading();
                sendToServer('USERS');
            });
            
            // Modal close buttons
            document.querySelectorAll('.close-modal').forEach(button => {
                button.addEventListener('click', function() {
                    const modal = this.closest('.modal');
                    hideModal(modal);
                });
            });
            
            // Modal cancel buttons
            document.querySelectorAll('.cancel-btn').forEach(button => {
                button.addEventListener('click', function() {
                    const modal = this.closest('.modal');
                    hideModal(modal);
                });
            });
            
            // Help modal confirm button
            helpModal.querySelector('.confirm-btn').addEventListener('click', function() {
                hideModal(helpModal);
            });
            
            // Create room confirm button
            document.getElementById('create-room-confirm').addEventListener('click', createRoom);
            
            // Transfer confirm button
            document.getElementById('transfer-confirm').addEventListener('click', transferCredits);
            
            // Dice confirm button
            document.getElementById('dice-confirm').addEventListener('click', playDice);
            
            // Flip confirm button
            document.getElementById('flip-confirm').addEventListener('click', playCoinFlip);
            
            // Slots confirm button
            document.getElementById('slots-confirm').addEventListener('click', playSlotsGame);
            
            // Gamble confirm button
            document.getElementById('gamble-confirm').addEventListener('click', placeBet);
            
            // Whisper confirm button
            document.getElementById('whisper-confirm').addEventListener('click', sendWhisper);
            
            // Game animation close buttons
            document.getElementById('close-dice').addEventListener('click', function() {
                diceAnimation.classList.remove('active');
            });
            
            document.getElementById('close-coin').addEventListener('click', function() {
                coinAnimation.classList.remove('active');
            });
            
            document.getElementById('close-slots').addEventListener('click', function() {
                slotsAnimation.classList.remove('active');
            });
            
            // Switch to console button
            switchToConsoleBtn.addEventListener('click', function() {
                // Send a special message to the local server to switch to console mode
                fetch('/switch-to-console', {
                    method: 'POST'
                }).then(response => {
                    // The backend will handle closing this window
                    console.log('Switching to console mode');
                }).catch(error => {
                    console.error('Error switching to console:', error);
                });
            });
            
            // Chat tabs
            document.querySelectorAll('.tab').forEach(tab => {
                tab.addEventListener('click', function() {
                    // Remove active class from all tabs
                    document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
                    
                    // Add active class to clicked tab
                    this.classList.add('active');
                    
                    // Get view to show
                    const view = this.getAttribute('data-view');
                    activeTab = view;
                    
                    // Handle tab switch
                    switch (view) {
                        case 'shoutbox':
                            showLoading();
                            sendToServer('SHOUTBOX');
                            break;
                        case 'gambling-view':
                            showLoading();
                            sendToServer('POTS');
                            sendToServer('GAMBLINGHISTORY');
                            break;
                        default:
                            // Do nothing for room chat
                            break;
                    }
                });
            });
        });
    </script>
</body>
</html>";
        }

        #endregion

        #region Console Interface Implementation

        private static async Task RunConsoleInterfaceAsync()
        {
            Console.Title = "YurtCord";

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

                    case "/web":
                        // Switch to web interface
                        await RunWebInterfaceAsync();
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

        #endregion
    }
}