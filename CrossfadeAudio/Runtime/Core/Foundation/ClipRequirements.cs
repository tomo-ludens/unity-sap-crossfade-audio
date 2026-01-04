using UnityEngine;

namespace TomoLudens.CrossfadeAudio.Runtime.Core.Foundation
{
    /// <summary>
    /// AudioClip.GetData 前提の要件（Import設定・Load状態）をガードする。
    /// </summary>
    public static class ClipRequirements
    {
        public static bool CanUseGetData(AudioClip clip)
        {
            if (clip == null) return false;

            // GetData は streamed audio clips では動作しない（仕様）
            if (clip.loadType == AudioClipLoadType.Streaming) return false;

            // 圧縮音源は DecompressOnLoad のみ確実に取得できる（仕様）
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
