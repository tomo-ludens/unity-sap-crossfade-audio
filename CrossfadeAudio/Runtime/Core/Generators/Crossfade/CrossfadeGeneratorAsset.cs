using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.Audio;
using static UnityEngine.Audio.ProcessorInstance;
using TomoLudens.CrossfadeAudio.Runtime.Core.Types;

namespace TomoLudens.CrossfadeAudio.Runtime.Core.Generators.Crossfade
{
    [CreateAssetMenu(fileName = "CrossfadeGenerator", menuName = "TomoLudens/CrossfadeAudio/Generators/CrossfadeGenerator", order = 20)]
    public sealed class CrossfadeGeneratorAsset : ScriptableObject, IAudioGenerator
    {
        [Header(header: "Sources (must implement IAudioGenerator)")]
        public ScriptableObject sourceA;

        public ScriptableObject sourceB;

        [Header(header: "Initial State")]
        [Range(min: 0.0f, max: 1.0f)]
        public float initialPosition01;

        public CrossfadeCurve initialCurve = CrossfadeCurve.EqualPower;

        [Min(min: 0.0f)]
        public float defaultFadeSeconds = 0.25f;

        public bool isFinite => false;
        public bool isRealtime => false;
        public DiscreteTime? length => null;

        public GeneratorInstance CreateInstance(
            ControlContext context,
            AudioFormat? nestedConfiguration,
            CreationParameters creationParameters)
        {
            IAudioGenerator generatorA = sourceA as IAudioGenerator;
            IAudioGenerator generatorB = sourceB as IAudioGenerator;

            GeneratorInstance childA = default;
            GeneratorInstance childB = default;

            if (generatorA != null)
            {
                childA = generatorA.CreateInstance(
                    context: context,
                    nestedFormat: nestedConfiguration,
                    creationParameters: creationParameters);
            }

            if (generatorB != null)
            {
                childB = generatorB.CreateInstance(
                    context: context,
                    nestedFormat: nestedConfiguration,
                    creationParameters: creationParameters);
            }

            float pos = Mathf.Clamp01(value: initialPosition01);

            var realtime = new CrossfadeGeneratorRealtime
            {
                ChildA = childA,
                ChildB = childB,

                BufferDataA = default,
                BufferDataB = default,
                BufferChannelCount = 0,

                FadePosition01 = pos,
                TargetPosition01 = pos,
                FadeIncrementPerFrame = 0.0f,
                CurrentCurve = initialCurve,
                SampleRate = 0.0f,

                ChildAFinished = false,
                ChildBFinished = false,

                ChildAFormatCompatible = true,
                ChildBFormatCompatible = true
            };

            var control = new CrossfadeGeneratorControl
            {
                InitialPosition01 = pos,
                InitialCurve = initialCurve,
                DefaultFadeSeconds = defaultFadeSeconds
            };

            return context.AllocateGenerator(
                realtimeState: realtime,
                controlState: control,
                nestedFormat: nestedConfiguration,
                creationParameters: creationParameters);
        }
    }
}
