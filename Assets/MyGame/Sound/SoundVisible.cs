using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SouondVisible
{
    [System.Serializable]
    public class SoundVisible : MonoBehaviour
    {
        
        [SerializeField] private float scaleFactor = 1f;
        [SerializeField] private float lerp = 0.5f;

        [SerializeField]private AudioSource source = default;
        [SerializeField] private AudioClip clip1;
        private float[] data = default;
        private Vector3 initialScale = default;
        private int sampleStep = default;

        float time = 0;

        private void Awake()
        {
            this.initialScale = transform.localScale;
            time = 0;
        }

        private void Start()
        {
            //var source = this.source;
            //var clip = source.clip;
            var clip = clip1;
            var data = new float[clip.channels * clip.samples];
            clip1.GetData(data, 0);

            Prepare(clip1, data);
            Reset();
        }

        public void Prepare(AudioClip clip1, float[] monoData)
        {
            this.data = monoData;

            var fps = Mathf.Max(60f, 1f / Time.fixedDeltaTime);
            var clip = clip1;
            this.sampleStep = (int)(clip.frequency / fps);
        }

        private void FixedUpdate()
        {
            time += Time.deltaTime;
            var startIndex = time;
            var endIndex = Mathf.Min(time + sampleStep, data.Length);
            var level = DetectVolumeLevel(data, (int)startIndex, (int)endIndex);
            Render(level);
        }

        private void Render(float size)
        {
            var diff = initialScale * this.scaleFactor * size;
            transform.localScale = Vector3.Lerp(transform.localScale, diff, lerp);
        }

        private void Reset()
        {
            transform.localScale = initialScale;
        }

        private float DetectVolumeLevel(float[] data, int start, int end)
        {
            var max = 0f;
            var min = 0f;

            for (var i = start; i < end; i++)
            {
                if (max < data[i]) max = data[i];
                if (min > data[i]) min = data[i];
            }

            return max - min;
        }
    }
}