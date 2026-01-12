using System.Threading.Tasks;

namespace SapCrossfadeAudio.Addressables
{
    /// <summary>
    /// Interface for AudioGenerators that support preloading.
    /// Implemented by generators using Addressables or external resources.
    /// </summary>
    public interface IPreloadableAudioGenerator
    {
        /// <summary>
        /// Whether the asset is loaded and ready for playback.
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// Preloads the asset asynchronously.
        /// Caller should await completion before playback to avoid hitches.
        /// </summary>
        Task PreloadAsync();

        /// <summary>
        /// Releases the loaded asset. This method must be idempotent (safe to call multiple times).
        /// </summary>
        void Release();
    }
}
