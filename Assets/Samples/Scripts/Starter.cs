using Elysia.VoIPModule.Components;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Elysia.VoIPModule.Samples
{
    public class Starter : MonoBehaviour
    {
        public Mic mic;
        public AudioComponent audioComponent;

        private ConcurrentQueue<NativeArray<float>> queue = new ConcurrentQueue<NativeArray<float>>();

        private void Awake()
        {
            mic.Initialize();
            mic.OnCaptured += OnMicCaptured;
            audioComponent.Initialize(AudioSettings.outputSampleRate);
        }

        private void OnDestroy()
        {
            while (queue.TryDequeue(out NativeArray<float> s))
            {
                s.Dispose();
            }
        }

        private void Update()
        {
            while (queue.TryDequeue(out NativeArray<float> s))
            {
                try
                {
                    audioComponent.ReceiveSamples(s);
                }
                catch (Exception e)
                {
                    Debug.LogError(e.ToString());
                }
                finally
                {
                    s.Dispose();
                }
            }
        }

        private void OnMicCaptured(NativeArray<float> samples)
        {
            NativeArray<float> s = new NativeArray<float>(samples, Allocator.Persistent);

            queue.Enqueue(s);
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Start", GUILayout.Width(200)))
            {
                mic.StartCapture();
            }

            if (GUILayout.Button("End", GUILayout.Width(200)))
            {
                mic.StopCapture();
            }

            GUILayout.Label($"Volume : {mic.CurrentVolume}", GUILayout.Width(200));
        }
    }
}
