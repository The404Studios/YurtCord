import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import axios from 'axios';
import type { Message } from '../../types';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

interface MessagesState {
  messages: Record<string, Message[]>; // channelId -> messages
  loading: boolean;
  error: string | null;
}

const initialState: MessagesState = {
  messages: {},
  loading: false,
  error: null,
};

export const fetchMessages = createAsyncThunk(
  'messages/fetchMessages',
  async (channelId: string, { rejectWithValue }) => {
    try {
      const token = localStorage.getItem('token');
      const response = await axios.get<Message[]>(
        `${API_URL}/api/channels/${channelId}/messages?limit=50`,
        {
          headers: { Authorization: `Bearer ${token}` },
        }
      );
      return { channelId, messages: response.data };
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch messages');
    }
  }
);

export const sendMessage = createAsyncThunk(
  'messages/sendMessage',
  async ({ channelId, content }: { channelId: string; content: string }, { rejectWithValue }) => {
    try {
      const token = localStorage.getItem('token');
      const response = await axios.post<Message>(
        `${API_URL}/api/channels/${channelId}/messages`,
        { content },
        {
          headers: { Authorization: `Bearer ${token}` },
        }
      );
      return response.data;
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to send message');
    }
  }
);

const messagesSlice = createSlice({
  name: 'messages',
  initialState,
  reducers: {
    addMessage: (state, action: PayloadAction<Message>) => {
      const channelId = action.payload.channelId;
      if (!state.messages[channelId]) {
        state.messages[channelId] = [];
      }
      // Prevent duplicates
      const exists = state.messages[channelId].some(m => m.id === action.payload.id);
      if (!exists) {
        state.messages[channelId].push(action.payload);
      }
    },
    updateMessage: (state, action: PayloadAction<Message>) => {
      const channelId = action.payload.channelId;
      if (state.messages[channelId]) {
        const index = state.messages[channelId].findIndex(m => m.id === action.payload.id);
        if (index !== -1) {
          state.messages[channelId][index] = action.payload;
        }
      }
    },
    removeMessage: (state, action: PayloadAction<string>) => {
      // messageId
      const messageId = action.payload;
      // Find and remove from all channels
      Object.keys(state.messages).forEach(channelId => {
        state.messages[channelId] = state.messages[channelId].filter(m => m.id !== messageId);
      });
    },
    clearMessages: (state, action: PayloadAction<string>) => {
      delete state.messages[action.payload];
    },
  },
  extraReducers: (builder) => {
    builder.addCase(fetchMessages.pending, (state) => {
      state.loading = true;
      state.error = null;
    });
    builder.addCase(fetchMessages.fulfilled, (state, action) => {
      state.loading = false;
      state.messages[action.payload.channelId] = action.payload.messages;
    });
    builder.addCase(fetchMessages.rejected, (state, action) => {
      state.loading = false;
      state.error = action.payload as string;
    });

    builder.addCase(sendMessage.fulfilled, (state, action) => {
      const channelId = action.payload.channelId;
      if (!state.messages[channelId]) {
        state.messages[channelId] = [];
      }
      // Only add if not already present (might come from SignalR)
      const exists = state.messages[channelId].some(m => m.id === action.payload.id);
      if (!exists) {
        state.messages[channelId].push(action.payload);
      }
    });
  },
});

export const { addMessage, updateMessage, removeMessage, clearMessages } = messagesSlice.actions;
export default messagesSlice.reducer;
