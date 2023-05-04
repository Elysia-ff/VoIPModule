using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Elysia.VoIPModule.Components
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioComponent : MonoBehaviour
    {
        private AudioSource audioSource;
        private float[] buffer;
        private int targetTime = 0;
        private double targetDSPTime = 0d;

        private const int CHANNEL_COUNT = 1;
        private const float RECEIVE_THRESHOLD = 0.5f;
        private int START_TIME;
        private float[] NULL_SAMPLES;

        public float Volume
        {
            get => audioSource.volume;
            set => audioSource.volume = value;
        }

        public bool IsMuted
        {
            get => audioSource.mute;
            set => audioSource.mute = value;
        }

        private void OnDestroy()
        {
            if (audioSource.clip != null)
            {
                Destroy(audioSource.clip);
            }
        }

        public void Initialize(int sampleRate)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource.clip != null)
            {
                Destroy(audioSource.clip);
            }

            START_TIME = (int)(sampleRate * 0.5f);
            NULL_SAMPLES = new float[(int)(sampleRate * RECEIVE_THRESHOLD)];

            audioSource.clip = AudioClip.Create("received_audio", 60 * sampleRate, CHANNEL_COUNT, sampleRate, false);
            audioSource.loop = true;
            audioSource.mute = false;
        }

        public unsafe void ReceiveSamples(NativeArray<float> samples)
        {
            if (buffer == null || buffer.Length < samples.Length)
            {
                buffer = new float[samples.Length];
            }

            fixed (float* ptr = buffer)
            {
                UnsafeUtility.MemCpy(ptr, samples.GetUnsafePtr(), samples.Length * sizeof(float));
            }

            ReceiveSamples(buffer, samples.Length);
        }

        public void ReceiveSamples(float[] sample, int len)
        {
            if (audioSource.clip == null)
            {
                return;
            }

            bool shouldStart = !audioSource.isPlaying;
            if (shouldStart)
            {
                audioSource.timeSamples = 0;
                targetTime = START_TIME;
                targetDSPTime = AudioSettings.dspTime + (START_TIME / audioSource.clip.frequency);
            }

            targetDSPTime += (double)len / audioSource.clip.frequency;
            if (AudioSettings.dspTime > targetDSPTime)
            {
                targetTime = audioSource.timeSamples + START_TIME;
                ClampTargetTime();

                targetDSPTime = AudioSettings.dspTime + (START_TIME / audioSource.clip.frequency);
            }

            audioSource.clip.SetData(sample, targetTime);
            audioSource.SetScheduledEndTime(AudioSettings.dspTime + RECEIVE_THRESHOLD);

            targetTime += len / audioSource.clip.channels;
            ClampTargetTime();

            audioSource.clip.SetData(NULL_SAMPLES, targetTime);

            if (shouldStart)
            {
                audioSource.Play();
            }
        }

        private void ClampTargetTime()
        {
            if (targetTime >= audioSource.clip.samples)
            {
                targetTime -= audioSource.clip.samples;
            }
        }
    }
}
