#if UNITY_EDITOR
using UnityEditor;
using SapCrossfadeAudio.Runtime.Core.Foundation;

namespace SapCrossfadeAudio.Editor
{
    [InitializeOnLoad]
    internal static class NativeBufferPoolEditorCleanup
    {
        static NativeBufferPoolEditorCleanup()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                NativeBufferPool.Clear();
            }
        }

        private static void OnBeforeAssemblyReload()
        {
            NativeBufferPool.Clear();
        }
    }
}
#endif
