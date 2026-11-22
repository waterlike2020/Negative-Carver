using System.Collections;
using NegativeCarver.Core;
using UnityEngine;

namespace NegativeCarver.Unity
{
    public sealed class CarveFxManager : MonoBehaviour
    {
        [SerializeField] private GridManager grid;
        [SerializeField] private AudioClip smallClip;
        [SerializeField] private AudioClip bigClip;
        [SerializeField] private AudioClip collapseClip;

        private Camera framedCamera;
        private AudioSource voice;
        private Vector3 framedPosition;
        private int shakeSerial;
        private Coroutine shakeRoutine;

        private void OnEnable()
        {
            RememberFrame();
            EnsureVoice();
            smallClip = Ready(smallClip, "Carve/small");
            bigClip = Ready(bigClip, "Carve/big");
            collapseClip = Ready(collapseClip, null);
        }

        private void EnsureVoice()
        {
            if (voice == null)
            {
                voice = GetComponent<AudioSource>();
            }

            if (voice == null)
            {
                voice = gameObject.AddComponent<AudioSource>();
            }

            voice.enabled = true;
            voice.playOnAwake = false;
            voice.loop = false;
            voice.mute = false;
            voice.spatialBlend = 0f;
            voice.volume = 1f;
            voice.dopplerLevel = 0f;
        }

        private static AudioClip Ready(AudioClip clip, string resource)
        {
            if (clip == null && resource != null)
            {
                clip = Resources.Load<AudioClip>(resource);
            }

            if (clip != null && clip.loadState != AudioDataLoadState.Loaded)
            {
                clip.LoadAudioData();
            }

            return clip;
        }

        private void OnDisable()
        {
            RestoreFrame();
        }

        public void PlayCarveEffect(Vector3 worldPos)
        {
            try
            {
                var burst = new GameObject("CarveBurst");
                burst.transform.SetParent(null);
                burst.transform.position = worldPos;
                var particles = burst.AddComponent<ParticleSystem>();
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particles.main;
                main.playOnAwake = false;
                main.loop = false;
                main.duration = 0.35f;
                main.startLifetime = 0.45f;
                main.startSpeed = 1.6f;
                main.startSize = 0.12f;
                main.maxParticles = 24;
                main.stopAction = ParticleSystemStopAction.Destroy;
                var emission = particles.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });
                var shape = particles.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.2f;
                particles.Play();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        public void PlayCameraShake()
        {
            try
            {
                if (framedCamera == null)
                {
                    RememberFrame();
                }

                if (framedCamera == null)
                {
                    return;
                }

                shakeSerial++;
                if (shakeRoutine != null)
                {
                    StopCoroutine(shakeRoutine);
                }

                shakeRoutine = StartCoroutine(Shake(shakeSerial));
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                RestoreFrame();
            }
        }

        private IEnumerator Shake(int serial)
        {
            var duration = 0.16f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (serial != shakeSerial)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                var damper = 1f - (elapsed / duration);
                var offset = Random.insideUnitCircle * (0.08f * damper);
                framedCamera.transform.position = framedPosition + new Vector3(offset.x, offset.y, 0f);
                yield return null;
            }

            if (serial == shakeSerial)
            {
                RestoreFrame();
            }
        }

        private void RememberFrame()
        {
            framedCamera = Camera.main;
            if (framedCamera == null)
            {
                return;
            }

            framedPosition = framedCamera.transform.position;
        }

        private void RestoreFrame()
        {
            if (framedCamera == null)
            {
                return;
            }

            framedCamera.transform.position = framedPosition;
            shakeRoutine = null;
        }

        public void PlayCarveSound(CarveImpact impact)
        {
            if (impact == CarveImpact.Small)
            {
                PlayClip(smallClip);
                return;
            }

            if (impact == CarveImpact.Big)
            {
                PlayClip(bigClip);
            }
        }

        public void PlayCollapseSound()
        {
            PlayClip(collapseClip);
        }

        private void PlayClip(AudioClip clip)
        {
            try
            {
                EnsureVoice();
                if (clip == null || voice == null)
                {
                    return;
                }

                if (clip.loadState != AudioDataLoadState.Loaded)
                {
                    clip.LoadAudioData();
                }

                voice.mute = false;
                voice.spatialBlend = 0f;
                voice.volume = 1f;
                voice.PlayOneShot(clip);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        public void PlayCollapseEffect(int lineY)
        {
            try
            {
                if (lineY < 0)
                {
                    return;
                }

                PlayCollapseSound();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
