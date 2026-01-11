using Unity.Burst;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.Audio;
using static UnityEngine.Audio.ProcessorInstance;

namespace CrossfadeAudio.Runtime.Smoke
{
    internal readonly struct FrequencyEvent
    {
        public readonly float Value;

        public FrequencyEvent(float value)
            => this.Value = value;
    }

    [BurstCompile(CompileSynchronously = true)]
    internal struct SineSmokeRealtime : GeneratorInstance.IRealtime
    {
        private const float TwoPi = 2.0f * Mathf.PI;

        private float _phase; // [0,1)
        internal float Frequency;
        internal float SampleRate;
        internal float Amplitude;

        public bool isFinite => false;
        public bool isRealtime => false;
        public DiscreteTime? length => null;

        public void Update(UpdatedDataContext context, Pipe pipe)
        {
            foreach (var element in pipe.GetAvailableData(context: context))
            {
                if (element.TryGetData(data: out FrequencyEvent evt))
                {
                    Frequency = evt.Value;
                }
            }
        }

        public GeneratorInstance.Result Process(
            in RealtimeContext context,
            Pipe pipe,
            ChannelBuffer buffer,
            GeneratorInstance.Arguments args)
        {
            float phaseIncrement = Frequency / SampleRate;

            for (int frame = 0; frame < buffer.frameCount; frame++)
            {
                float s = Mathf.Sin(f: _phase * TwoPi) * Amplitude;

                for (int ch = 0; ch < buffer.channelCount; ch++)
                {
                    buffer[channel: ch, frame: frame] = s;
                }

                _phase += phaseIncrement;
                if (_phase >= 1.0f)
                {
                    _phase -= 1.0f;
                }
            }

            // 公式例同様、書き込んだ frame 数を返す。
            return buffer.frameCount;
        }
    }

    internal struct SineSmokeControl : GeneratorInstance.IControl<SineSmokeRealtime>
    {
        public void Dispose(ControlContext context, ref SineSmokeRealtime realtime)
        {
        }

        public void Update(ControlContext context, Pipe pipe)
        {
        }

        public Response OnMessage(ControlContext context, Pipe pipe, Message message)
        {
            if (!message.Is<FrequencyEvent>()) return Response.Unhandled;

            pipe.SendData(context: context, data: message.Get<FrequencyEvent>());
            return Response.Handled;

        }

        public void Configure(
            ControlContext context,
            ref SineSmokeRealtime realtime,
            in AudioFormat format,
            out GeneratorInstance.Setup setup,
            ref GeneratorInstance.Properties properties)
        {
            realtime.SampleRate = format.sampleRate;

            // ホストフォーマットに寄せるのが推奨。
            setup = new GeneratorInstance.Setup(speakerMode: format.speakerMode, sampleRate: format.sampleRate);
        }
    }
}
