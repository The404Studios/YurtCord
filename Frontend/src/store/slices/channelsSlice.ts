import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import type { Channel } from '../../types';

interface ChannelsState {
  currentChannelId: string | null;
}

const initialState: ChannelsState = {
  currentChannelId: null,
};

const channelsSlice = createSlice({
  name: 'channels',
  initialState,
  reducers: {
    setCurrentChannel: (state, action: PayloadAction<string>) => {
      state.currentChannelId = action.payload;
    },
    clearCurrentChannel: (state) => {
      state.currentChannelId = null;
    },
  },
});

export const { setCurrentChannel, clearCurrentChannel } = channelsSlice.actions;
export default channelsSlice.reducer;
