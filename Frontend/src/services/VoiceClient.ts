/**
 * YurtCord WebRTC Voice Client
 *
 * Handles voice and video communication using WebRTC with SignalR signaling.
 * Supports multiple peer connections in a voice channel (mesh topology).
 */

import * as signalR from '@microsoft/signalr';

export interface VoiceConnectionConfig {
    gatewayUrl: string;
    token: string;
    iceServers?: RTCIceServer[];
}

export interface VoiceUser {
    userId: string;
    username: string;
    discriminator: string;
    avatar?: string;
    connectionId: string;
    sessionId: string;
    muted?: boolean;
    deafened?: boolean;
    speaking?: boolean;
    videoEnabled?: boolean;
}

export interface VoiceChannel {
    channelId: string;
    users: Map<string, VoiceUser>;
}

export class VoiceClient {
    private connection: signalR.HubConnection;
    private currentChannelId: string | null = null;
    private peerConnections: Map<string, RTCPeerConnection> = new Map();
    private localStream: MediaStream | null = null;
    private remoteStreams: Map<string, MediaStream> = new Map();
    private iceServers: RTCIceServer[];
    private isMuted: boolean = false;
    private isDeafened: boolean = false;
    private isVideoEnabled: boolean = false;

    // Event handlers
    public onUserJoined?: (user: VoiceUser) => void;
    public onUserLeft?: (userId: string) => void;
    public onUserSpeaking?: (userId: string, speaking: boolean) => void;
    public onUserMuteChanged?: (userId: string, muted: boolean) => void;
    public onRemoteStream?: (userId: string, stream: MediaStream) => void;
    public onError?: (error: Error) => void;

    constructor(config: VoiceConnectionConfig) {
        // Default ICE servers (Google STUN)
        this.iceServers = config.iceServers || [
            { urls: 'stun:stun.l.google.com:19302' },
            { urls: 'stun:stun1.l.google.com:19302' }
        ];

        // Create SignalR connection
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(config.gatewayUrl, {
                accessTokenFactory: () => config.token
            })
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.setupSignalRHandlers();
    }

    /**
     * Setup SignalR event handlers for WebRTC signaling
     */
    private setupSignalRHandlers(): void {
        // User joined voice channel
        this.connection.on('VoiceUserJoined', async (data: VoiceUser) => {
            console.log('[Voice] User joined:', data.username);

            if (this.onUserJoined) {
                this.onUserJoined(data);
            }

            // If it's not us, create offer to establish connection
            if (data.connectionId !== this.connection.connectionId) {
                await this.createOffer(data.userId);
            }
        });

        // User left voice channel
        this.connection.on('VoiceUserLeft', (data: { userId: string; channelId: string }) => {
            console.log('[Voice] User left:', data.userId);
            this.closePeerConnection(data.userId);

            if (this.onUserLeft) {
                this.onUserLeft(data.userId);
            }
        });

        // Receive WebRTC offer from another peer
        this.connection.on('WebRTCOffer', async (data: { fromUserId: string; fromUsername: string; offer: RTCSessionDescriptionInit }) => {
            console.log('[Voice] Received offer from:', data.fromUsername);
            await this.handleOffer(data.fromUserId, data.offer);
        });

        // Receive WebRTC answer from another peer
        this.connection.on('WebRTCAnswer', async (data: { fromUserId: string; answer: RTCSessionDescriptionInit }) => {
            console.log('[Voice] Received answer from:', data.fromUserId);
            await this.handleAnswer(data.fromUserId, data.answer);
        });

        // Receive ICE candidate from another peer
        this.connection.on('ICECandidate', async (data: { fromUserId: string; candidate: RTCIceCandidateInit }) => {
            console.log('[Voice] Received ICE candidate from:', data.fromUserId);
            await this.handleIceCandidate(data.fromUserId, data.candidate);
        });

        // Voice state updates (mute, deafen, speaking)
        this.connection.on('VoiceStateUpdate', (data: { userId: string; mute?: boolean; deaf?: boolean; speaking?: boolean }) => {
            if (data.mute !== undefined && this.onUserMuteChanged) {
                this.onUserMuteChanged(data.userId, data.mute);
            }
            if (data.speaking !== undefined && this.onUserSpeaking) {
                this.onUserSpeaking(data.userId, data.speaking);
            }
        });

        // Video state updates
        this.connection.on('VideoStateUpdate', (data: { userId: string; enabled: boolean }) => {
            console.log('[Voice] Video state update:', data);
        });

        // Current channel users (sent when joining)
        this.connection.on('VoiceChannelUsers', (data: { channelId: string; users: Array<{ userId: string; connectionId: string }> }) => {
            console.log('[Voice] Current users in channel:', data.users.length);
        });
    }

    /**
     * Connect to the SignalR gateway
     */
    public async connect(): Promise<void> {
        try {
            await this.connection.start();
            console.log('[Voice] Connected to gateway');
        } catch (error) {
            console.error('[Voice] Connection failed:', error);
            if (this.onError) {
                this.onError(error as Error);
            }
            throw error;
        }
    }

    /**
     * Join a voice channel
     */
    public async joinChannel(channelId: string, audioOnly: boolean = false): Promise<void> {
        if (this.currentChannelId) {
            await this.leaveChannel();
        }

        try {
            // Get local media stream
            this.localStream = await navigator.mediaDevices.getUserMedia({
                audio: {
                    echoCancellation: true,
                    noiseSuppression: true,
                    autoGainControl: true,
                    sampleRate: 48000
                },
                video: audioOnly ? false : {
                    width: { ideal: 1280 },
                    height: { ideal: 720 },
                    frameRate: { ideal: 30 }
                }
            });

            console.log('[Voice] Got local media stream');

            // Join via SignalR
            await this.connection.invoke('JoinVoiceChannel', channelId);

            this.currentChannelId = channelId;

        } catch (error) {
            console.error('[Voice] Failed to join channel:', error);
            if (this.onError) {
                this.onError(error as Error);
            }
            throw error;
        }
    }

    /**
     * Leave current voice channel
     */
    public async leaveChannel(): Promise<void> {
        if (!this.currentChannelId) return;

        try {
            // Notify server
            await this.connection.invoke('LeaveVoiceChannel', this.currentChannelId);

            // Close all peer connections
            for (const userId of this.peerConnections.keys()) {
                this.closePeerConnection(userId);
            }

            // Stop local stream
            if (this.localStream) {
                this.localStream.getTracks().forEach(track => track.stop());
                this.localStream = null;
            }

            this.currentChannelId = null;
            this.remoteStreams.clear();

            console.log('[Voice] Left channel');

        } catch (error) {
            console.error('[Voice] Failed to leave channel:', error);
        }
    }

    /**
     * Create WebRTC offer to connect to a peer
     */
    private async createOffer(targetUserId: string): Promise<void> {
        try {
            const peerConnection = this.createPeerConnection(targetUserId);

            // Add local stream tracks
            if (this.localStream) {
                this.localStream.getTracks().forEach(track => {
                    peerConnection.addTrack(track, this.localStream!);
                });
            }

            // Create and set local description (offer)
            const offer = await peerConnection.createOffer({
                offerToReceiveAudio: true,
                offerToReceiveVideo: true
            });

            await peerConnection.setLocalDescription(offer);

            // Send offer to peer via SignalR
            await this.connection.invoke('SendWebRTCOffer', targetUserId, this.currentChannelId, offer);

            console.log('[Voice] Sent offer to:', targetUserId);

        } catch (error) {
            console.error('[Voice] Failed to create offer:', error);
        }
    }

    /**
     * Handle incoming WebRTC offer from a peer
     */
    private async handleOffer(fromUserId: string, offer: RTCSessionDescriptionInit): Promise<void> {
        try {
            const peerConnection = this.createPeerConnection(fromUserId);

            // Add local stream tracks
            if (this.localStream) {
                this.localStream.getTracks().forEach(track => {
                    peerConnection.addTrack(track, this.localStream!);
                });
            }

            // Set remote description (offer)
            await peerConnection.setRemoteDescription(new RTCSessionDescription(offer));

            // Create and set local description (answer)
            const answer = await peerConnection.createAnswer();
            await peerConnection.setLocalDescription(answer);

            // Send answer to peer via SignalR
            await this.connection.invoke('SendWebRTCAnswer', fromUserId, this.currentChannelId, answer);

            console.log('[Voice] Sent answer to:', fromUserId);

        } catch (error) {
            console.error('[Voice] Failed to handle offer:', error);
        }
    }

    /**
     * Handle incoming WebRTC answer from a peer
     */
    private async handleAnswer(fromUserId: string, answer: RTCSessionDescriptionInit): Promise<void> {
        try {
            const peerConnection = this.peerConnections.get(fromUserId);
            if (!peerConnection) {
                console.error('[Voice] No peer connection for answer from:', fromUserId);
                return;
            }

            await peerConnection.setRemoteDescription(new RTCSessionDescription(answer));

            console.log('[Voice] Set remote description from answer:', fromUserId);

        } catch (error) {
            console.error('[Voice] Failed to handle answer:', error);
        }
    }

    /**
     * Handle incoming ICE candidate from a peer
     */
    private async handleIceCandidate(fromUserId: string, candidate: RTCIceCandidateInit): Promise<void> {
        try {
            const peerConnection = this.peerConnections.get(fromUserId);
            if (!peerConnection) {
                console.error('[Voice] No peer connection for ICE candidate from:', fromUserId);
                return;
            }

            await peerConnection.addIceCandidate(new RTCIceCandidate(candidate));

        } catch (error) {
            console.error('[Voice] Failed to add ICE candidate:', error);
        }
    }

    /**
     * Create a new peer connection for a user
     */
    private createPeerConnection(userId: string): RTCPeerConnection {
        const peerConnection = new RTCPeerConnection({
            iceServers: this.iceServers,
            iceTransportPolicy: 'all',
            bundlePolicy: 'max-bundle',
            rtcpMuxPolicy: 'require'
        });

        // Handle ICE candidates
        peerConnection.onicecandidate = async (event) => {
            if (event.candidate) {
                await this.connection.invoke('SendICECandidate', userId, this.currentChannelId, event.candidate.toJSON());
            }
        };

        // Handle remote tracks (audio/video from peer)
        peerConnection.ontrack = (event) => {
            console.log('[Voice] Received remote track from:', userId);

            const [stream] = event.streams;
            this.remoteStreams.set(userId, stream);

            if (this.onRemoteStream) {
                this.onRemoteStream(userId, stream);
            }
        };

        // Handle connection state changes
        peerConnection.onconnectionstatechange = () => {
            console.log('[Voice] Connection state:', userId, peerConnection.connectionState);

            if (peerConnection.connectionState === 'failed' ||
                peerConnection.connectionState === 'disconnected') {
                this.closePeerConnection(userId);
            }
        };

        this.peerConnections.set(userId, peerConnection);

        return peerConnection;
    }

    /**
     * Close peer connection to a user
     */
    private closePeerConnection(userId: string): void {
        const peerConnection = this.peerConnections.get(userId);
        if (peerConnection) {
            peerConnection.close();
            this.peerConnections.delete(userId);
        }

        this.remoteStreams.delete(userId);
    }

    /**
     * Mute/unmute local microphone
     */
    public async setMute(muted: boolean): Promise<void> {
        if (!this.localStream) return;

        this.localStream.getAudioTracks().forEach(track => {
            track.enabled = !muted;
        });

        this.isMuted = muted;

        // Notify server
        if (this.currentChannelId) {
            await this.connection.invoke('UpdateVoiceState', this.currentChannelId, muted, null, null);
        }
    }

    /**
     * Deafen/undeafen (mute output)
     */
    public async setDeafen(deafened: boolean): Promise<void> {
        this.isDeafened = deafened;

        // Also mute microphone when deafened
        if (deafened) {
            await this.setMute(true);
        }

        // Mute all remote streams
        for (const stream of this.remoteStreams.values()) {
            stream.getAudioTracks().forEach(track => {
                track.enabled = !deafened;
            });
        }

        // Notify server
        if (this.currentChannelId) {
            await this.connection.invoke('UpdateVoiceState', this.currentChannelId, null, deafened, null);
        }
    }

    /**
     * Enable/disable video
     */
    public async setVideo(enabled: boolean): Promise<void> {
        if (!this.localStream) return;

        const videoTracks = this.localStream.getVideoTracks();

        if (enabled && videoTracks.length === 0) {
            // Request video if not already available
            try {
                const videoStream = await navigator.mediaDevices.getUserMedia({ video: true });
                const videoTrack = videoStream.getVideoTracks()[0];

                this.localStream.addTrack(videoTrack);

                // Add track to all peer connections
                for (const pc of this.peerConnections.values()) {
                    pc.addTrack(videoTrack, this.localStream);
                }

            } catch (error) {
                console.error('[Voice] Failed to get video:', error);
                return;
            }
        }

        videoTracks.forEach(track => {
            track.enabled = enabled;
        });

        this.isVideoEnabled = enabled;

        // Notify server
        if (this.currentChannelId) {
            await this.connection.invoke('UpdateVideoState', this.currentChannelId, enabled);
        }
    }

    /**
     * Start screen sharing
     */
    public async startScreenShare(): Promise<void> {
        if (!this.currentChannelId) return;

        try {
            const screenStream = await navigator.mediaDevices.getDisplayMedia({
                video: {
                    cursor: 'always'
                },
                audio: false
            });

            const screenTrack = screenStream.getVideoTracks()[0];

            // Replace video track in all peer connections
            for (const pc of this.peerConnections.values()) {
                const senders = pc.getSenders();
                const videoSender = senders.find(sender => sender.track?.kind === 'video');

                if (videoSender) {
                    await videoSender.replaceTrack(screenTrack);
                }
            }

            // Handle screen share stop
            screenTrack.onended = () => {
                this.stopScreenShare();
            };

            console.log('[Voice] Started screen share');

        } catch (error) {
            console.error('[Voice] Failed to start screen share:', error);
        }
    }

    /**
     * Stop screen sharing
     */
    public async stopScreenShare(): Promise<void> {
        if (!this.localStream) return;

        const videoTrack = this.localStream.getVideoTracks()[0];

        // Restore camera track in all peer connections
        for (const pc of this.peerConnections.values()) {
            const senders = pc.getSenders();
            const videoSender = senders.find(sender => sender.track?.kind === 'video');

            if (videoSender && videoTrack) {
                await videoSender.replaceTrack(videoTrack);
            }
        }

        console.log('[Voice] Stopped screen share');
    }

    /**
     * Get local media stream
     */
    public getLocalStream(): MediaStream | null {
        return this.localStream;
    }

    /**
     * Get remote stream for a user
     */
    public getRemoteStream(userId: string): MediaStream | undefined {
        return this.remoteStreams.get(userId);
    }

    /**
     * Get current voice state
     */
    public getVoiceState() {
        return {
            channelId: this.currentChannelId,
            muted: this.isMuted,
            deafened: this.isDeafened,
            videoEnabled: this.isVideoEnabled,
            connectedUsers: this.peerConnections.size
        };
    }

    /**
     * Disconnect from gateway
     */
    public async disconnect(): Promise<void> {
        await this.leaveChannel();
        await this.connection.stop();
    }
}

export default VoiceClient;
