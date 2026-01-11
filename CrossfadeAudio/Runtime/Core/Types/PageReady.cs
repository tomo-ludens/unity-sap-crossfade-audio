namespace CrossfadeAudio.Runtime.Core.Types
{
    /// <summary>
    /// Paging/Streaming 用：Control→Realtime に「ページが用意できた」ことを伝えるメッセージ。
    /// </summary>
    internal readonly struct PageReady
    {
        public readonly int Slot;
        public readonly int ValidFrames;
        public readonly int StartFrame;
        public readonly byte Flags; // bit0: EOS

        public PageReady(int slot, int validFrames, int startFrame, byte flags)
        {
            this.Slot = slot;
            this.ValidFrames = validFrames;
            this.StartFrame = startFrame;
            this.Flags = flags;
        }
    }
}
