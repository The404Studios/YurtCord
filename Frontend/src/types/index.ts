// User types
export interface User {
  id: string;
  username: string;
  discriminator: string;
  email: string;
  avatar?: string;
  banner?: string;
  accentColor?: number;
  bot: boolean;
  verified: boolean;
  status: UserStatus;
  customStatus?: string;
}

export enum UserStatus {
  Online = 'online',
  Idle = 'idle',
  DoNotDisturb = 'dnd',
  Offline = 'offline',
}

// Guild types
export interface Guild {
  id: string;
  name: string;
  description?: string;
  icon?: string;
  banner?: string;
  ownerId: string;
  memberCount: number;
  channels: Channel[];
  roles: Role[];
  members: GuildMember[];
}

export interface GuildMember {
  userId: string;
  user: User;
  nickname?: string;
  roles: string[];
  joinedAt: string;
}

export interface Role {
  id: string;
  name: string;
  color: number;
  permissions: number;
  position: number;
  mentionable: boolean;
}

// Channel types
export interface Channel {
  id: string;
  type: ChannelType;
  guildId?: string;
  name: string;
  topic?: string;
  position: number;
  parentId?: string;
  nsfw: boolean;
  rateLimitPerUser?: number;
}

export enum ChannelType {
  GuildText = 0,
  DM = 1,
  GuildVoice = 2,
  GroupDM = 3,
  GuildCategory = 4,
  GuildAnnouncement = 5,
  GuildForum = 15,
}

// Message types
export interface Message {
  id: string;
  channelId: string;
  authorId: string;
  author: User;
  content: string;
  timestamp: string;
  editedTimestamp?: string;
  mentions: string[];
  mentionRoles: string[];
  attachments: Attachment[];
  embeds: Embed[];
  reactions: Reaction[];
  pinned: boolean;
  type: number;
}

export interface Attachment {
  id: string;
  filename: string;
  url: string;
  proxyUrl: string;
  size: number;
  contentType?: string;
  width?: number;
  height?: number;
}

export interface Embed {
  title?: string;
  description?: string;
  url?: string;
  timestamp?: string;
  color?: number;
  footer?: EmbedFooter;
  image?: EmbedImage;
  thumbnail?: EmbedImage;
  author?: EmbedAuthor;
  fields?: EmbedField[];
}

export interface EmbedFooter {
  text: string;
  iconUrl?: string;
}

export interface EmbedImage {
  url: string;
  width?: number;
  height?: number;
}

export interface EmbedAuthor {
  name: string;
  url?: string;
  iconUrl?: string;
}

export interface EmbedField {
  name: string;
  value: string;
  inline?: boolean;
}

export interface Reaction {
  emojiId?: string;
  emojiName: string;
  count: number;
  me: boolean;
}

// Auth types
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}
