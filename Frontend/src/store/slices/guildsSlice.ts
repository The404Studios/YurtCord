import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import axios from 'axios';
import type { Guild } from '../../types';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

interface GuildsState {
  guilds: Guild[];
  currentGuild: Guild | null;
  loading: boolean;
  error: string | null;
}

const initialState: GuildsState = {
  guilds: [],
  currentGuild: null,
  loading: false,
  error: null,
};

export const fetchGuilds = createAsyncThunk(
  'guilds/fetchGuilds',
  async (_, { rejectWithValue }) => {
    try {
      const token = localStorage.getItem('token');
      const response = await axios.get<Guild[]>(`${API_URL}/api/guilds/@me`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      return response.data;
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch guilds');
    }
  }
);

export const fetchGuild = createAsyncThunk(
  'guilds/fetchGuild',
  async (guildId: string, { rejectWithValue }) => {
    try {
      const token = localStorage.getItem('token');
      const response = await axios.get<Guild>(`${API_URL}/api/guilds/${guildId}`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      return response.data;
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch guild');
    }
  }
);

const guildsSlice = createSlice({
  name: 'guilds',
  initialState,
  reducers: {
    setCurrentGuild: (state, action: PayloadAction<string>) => {
      state.currentGuild = state.guilds.find(g => g.id === action.payload) || null;
    },
    clearCurrentGuild: (state) => {
      state.currentGuild = null;
    },
  },
  extraReducers: (builder) => {
    builder.addCase(fetchGuilds.pending, (state) => {
      state.loading = true;
      state.error = null;
    });
    builder.addCase(fetchGuilds.fulfilled, (state, action) => {
      state.loading = false;
      state.guilds = action.payload;
    });
    builder.addCase(fetchGuilds.rejected, (state, action) => {
      state.loading = false;
      state.error = action.payload as string;
    });

    builder.addCase(fetchGuild.fulfilled, (state, action) => {
      state.currentGuild = action.payload;
      const index = state.guilds.findIndex(g => g.id === action.payload.id);
      if (index !== -1) {
        state.guilds[index] = action.payload;
      }
    });
  },
});

export const { setCurrentGuild, clearCurrentGuild } = guildsSlice.actions;
export default guildsSlice.reducer;
