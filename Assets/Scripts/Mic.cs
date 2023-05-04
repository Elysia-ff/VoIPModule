using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace Elysia.VoIPModule
{
    [RequireComponent(typeof(AudioSource))]
    public class Mic : MonoBehaviour
    {
        private AudioSource micSource;
        private Coroutine startCaptureRoutine;
        private string currentDeviceName = null;

        public event Action<NativeArray<float>> OnCaptured;

        public bool IsPlaying => Microphone.IsRecording(currentDeviceName);
        public float CurrentVolume { get; private set; } = 0f;

        public void Initialize()
        {
            micSource = GetComponent<AudioSource>();
            AudioMixer muteMixer = Resources.Load<AudioMixer>("AudioMixers/MuteMixer");
            micSource.outputAudioMixerGroup = muteMixer.FindMatchingGroups("Master")[0];
        }

        public void StartCapture()
        {
            StopCapture();

            startCaptureRoutine = StartCoroutine(StartCaptureRoutine(null));
        }

        private IEnumerator StartCaptureRoutine(string deviceName)
        {
            int sampleRate = AudioSettings.outputSampleRate;
            if (micSource.clip != null)
            {
                Destroy(micSource.clip);
            }

            currentDeviceName = deviceName;
            micSource.clip = Microphone.Start(deviceName, true, 10, sampleRate);
            micSource.loop = true;

            while (Microphone.GetPosition(deviceName) <= 0)
            {
                yield return null;
            }

            micSource.Play();

            startCaptureRoutine = null;
        }

        public void StopCapture()
        {
            if (startCaptureRoutine != null)
            {
                StopCoroutine(startCaptureRoutine);
                startCaptureRoutine = null;
            }

            if (Microphone.IsRecording(currentDeviceName))
            {
                Microphone.End(currentDeviceName);
                micSource.Stop();
            }
        }

        // Note that the sample rate is equal to {AudioSettings.outputSampleRate}
        private void OnAudioFilterRead(float[] data, int channels)
        {
            CurrentVolume = 0f;

            using NativeArray<float> samples = MergeChannels(data, channels, out float volume);
            CurrentVolume = volume;

            // Resample & Encode {samples} then send it via network
            OnCaptured?.Invoke(samples);
        }

        private NativeArray<float> MergeChannels(float[] data, int channels, out float volume)
        {
            volume = 0f;

            int count = data.Length / channels;
            NativeArray<float> samples = new NativeArray<float>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < count; ++i)
            {
                float v = 0;
                for (int k = 0; k < channels; ++k)
                {
                    v += data[i * channels + k];
                }

                float mergedValue = v / channels;
                volume = Mathf.Max(Mathf.Abs(mergedValue), volume);

                samples[i] = mergedValue;
            }

            return samples;
        }
    }
}
