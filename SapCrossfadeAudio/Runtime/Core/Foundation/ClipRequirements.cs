using UnityEngine;

namespace SapCrossfadeAudio.Runtime.Core.Foundation
{
    /// <summary>
    /// Validates AudioClip requirements for GetData (import settings and load state).
    /// </summary>
    public static class ClipRequirements
    {
        public static bool CanUseGetData(AudioClip clip)
        {
            if (clip == null) return false;

            // GetData does not work with streaming audio clips (Unity specification)
            if (clip.loadType == AudioClipLoadType.Streaming) return false;

            // Compressed audio requires DecompressOnLoad for reliable PCM access
            return clip.loadType == AudioClipLoadType.DecompressOnLoad;
        }

        public static bool EnsureLoaded(AudioClip clip)
        {
            if (clip == null) return false;

            if (clip.loadState == AudioDataLoadState.Unloaded)
                return clip.LoadAudioData();

            return clip.loadState == AudioDataLoadState.Loaded;
        }
    }
}
