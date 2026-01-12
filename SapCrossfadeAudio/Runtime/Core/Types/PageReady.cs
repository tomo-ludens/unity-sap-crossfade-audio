namespace SapCrossfadeAudio.Runtime.Core.Types
{
    /// <summary>
    /// Paging/Streaming message: notifies Realtime that a page is ready from Control.
    /// </summary>
    internal readonly struct PageReady
    {
        internal readonly int Slot;
        internal readonly int ValidFrames;
        internal readonly int StartFrame;
        internal readonly byte Flags; // bit0: EOS

        internal PageReady(int slot, int validFrames, int startFrame, byte flags)
        {
            this.Slot = slot;
            this.ValidFrames = validFrames;
            this.StartFrame = startFrame;
            this.Flags = flags;
        }
    }
}
