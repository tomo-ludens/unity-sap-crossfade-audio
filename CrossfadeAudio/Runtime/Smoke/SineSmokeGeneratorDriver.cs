using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.Audio;
using static UnityEngine.Audio.ProcessorInstance;

namespace CrossfadeAudio.Runtime.Smoke
{
    [DisallowMultipleComponent]
    [RequireComponent(requiredComponent: typeof(AudioSource))]
    public sealed class SineSmokeGeneratorDriver : MonoBehaviour, IAudioGenerator
    {
        [Range(min: 20.0f, max: 20000.0f)]
        public float frequency = 440.0f;

        [Range(min: 0.0f, max: 1.0f)]
        public float amplitude = 0.1f;

        private AudioSource _audioSource;
        private float _previousFrequency;

        public bool isFinite => false;
        public bool isRealtime => false;
        public DiscreteTime? length => null;

        public GeneratorInstance CreateInstance(
            ControlContext context,
            AudioFormat? nestedConfiguration,
            CreationParameters creationParameters)
        {
            var realtime = new SineSmokeRealtime
            {
                Frequency = frequency,
                Amplitude = amplitude,
                SampleRate = 48000.0f
            };

            return context.AllocateGenerator(
                realtimeState: realtime,
                controlState: new SineSmokeControl(),
                nestedFormat: nestedConfiguration,
                creationParameters: creationParameters
            );
        }

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            // Generator が刺さった AudioSource を起動
            _audioSource.Play();
        }

        private void Update()
        {
            if (Mathf.Approximately(a: frequency, b: _previousFrequency))
            {
                return;
            }

            var instance = _audioSource.generatorInstance;

            // 公式例のガード。
            if (!ControlContext.builtIn.Exists(processorInstance: instance))
            {
                return;
            }

            var message = new FrequencyEvent(value: frequency);
            ControlContext.builtIn.SendMessage(processorInstance: instance, message: ref message);

            _previousFrequency = frequency;
        }
    }
}
