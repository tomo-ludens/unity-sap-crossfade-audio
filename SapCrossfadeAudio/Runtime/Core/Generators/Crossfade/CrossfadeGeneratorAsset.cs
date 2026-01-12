using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.Audio;

using SapCrossfadeAudio.Runtime.Core.Foundation;
using SapCrossfadeAudio.Runtime.Core.Types;

using static UnityEngine.Audio.ProcessorInstance;

namespace SapCrossfadeAudio.Runtime.Core.Generators.Crossfade
{
    [CreateAssetMenu(fileName = "CrossfadeGenerator", menuName = "TomoLudens/SapCrossfadeAudio/Generators/CrossfadeGenerator", order = 20)]
    public sealed class CrossfadeGeneratorAsset : ScriptableObject, IAudioGenerator
    {
        [Header(header: "Sources (must implement IAudioGenerator)")]
        public ScriptableObject sourceA;
        public ScriptableObject sourceB;

        [Header(header: "Initial State")]
        [SerializeField]
        private CrossfadeCurve initialCurve = CrossfadeCurve.EqualPower;

        [SerializeField] [Range(min: 0.0f, max: 1.0f)]
        public float initialPosition01;

        [SerializeField] [Min(min: 0.0f)]
        private float defaultFadeSeconds = 0.25f;

        public bool isFinite => false;
        public bool isRealtime => false;
        public DiscreteTime? length => null;

        public GeneratorInstance CreateInstance(
            ControlContext context,
            AudioFormat? nestedConfiguration,
            CreationParameters creationParameters)
        {
            var realtime = new CrossfadeGeneratorRealtime();

            float pos = Mathf.Clamp01(value: initialPosition01);

            var control = new CrossfadeGeneratorControl
            {
                InitialPosition01 = pos,
                InitialCurve = initialCurve,
                DefaultFadeSeconds = defaultFadeSeconds
            };

            // 子へ渡すフォーマット（親がネストされているならそれを優先、そうでなければ現行 AudioSettings）
            AudioFormat childNestedFormat = nestedConfiguration ?? new AudioFormat(config: AudioSettings.GetConfiguration());

            if (sourceA is IAudioGenerator generatorA)
            {
                realtime.ChildA = generatorA.CreateInstance(
                    context: context,
                    nestedFormat: childNestedFormat,
                    creationParameters: creationParameters);
            }
            else
            {
                realtime.ChildA = default;
            }

            if (sourceB is IAudioGenerator generatorB)
            {
                realtime.ChildB = generatorB.CreateInstance(
                    context: context,
                    nestedFormat: childNestedFormat,
                    creationParameters: creationParameters);
            }
            else
            {
                realtime.ChildB = default;
            }

            // バッファを事前割り当て（CreateInstance はメインスレッドで実行されるため Persistent が使用可能）
            // Configure は Job コンテキストで実行されるため、そこでは Temp しか使用できない
            int bufferFrameCount = childNestedFormat.bufferFrameCount > 0
                ? childNestedFormat.bufferFrameCount
                : AudioSettings.GetConfiguration().dspBufferSize;
            int channels = childNestedFormat.channelCount > 0
                ? childNestedFormat.channelCount
                : 2;
            int requiredFloats = bufferFrameCount * channels;

            if (requiredFloats <= 0)
            {
                return context.AllocateGenerator(
                    realtimeState: realtime,
                    controlState: control,
                    nestedFormat: nestedConfiguration,
                    creationParameters: creationParameters
                );
            }

            realtime.BufferDataA = NativeBufferPool.Rent(length: requiredFloats);
            realtime.BufferDataB = NativeBufferPool.Rent(length: requiredFloats);
            realtime.BufferChannelCount = channels;

            return context.AllocateGenerator(
                realtimeState: realtime,
                controlState: control,
                nestedFormat: nestedConfiguration,
                creationParameters: creationParameters
            );
        }
    }
}
