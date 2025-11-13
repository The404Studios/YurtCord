import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { UserStatus } from '../../types';

interface UserPresence {
  userId: string;
  status: UserStatus;
  lastSeen?: string;
}

interface PresenceState {
  presences: Record<string, UserPresence>; // userId -> presence
}

const initialState: PresenceState = {
  presences: {},
};

const presenceSlice = createSlice({
  name: 'presence',
  initialState,
  reducers: {
    updateUserPresence: (state, action: PayloadAction<UserPresence>) => {
      state.presences[action.payload.userId] = action.payload;
    },
    updateMultiplePresences: (state, action: PayloadAction<UserPresence[]>) => {
      action.payload.forEach(presence => {
        state.presences[presence.userId] = presence;
      });
    },
    removeUserPresence: (state, action: PayloadAction<string>) => {
      delete state.presences[action.payload];
    },
    clearPresences: (state) => {
      state.presences = {};
    },
  },
});

export const {
  updateUserPresence,
  updateMultiplePresences,
  removeUserPresence,
  clearPresences,
} = presenceSlice.actions;

export default presenceSlice.reducer;
