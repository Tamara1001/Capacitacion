using System.Collections;
using UnityEngine;

namespace LotoSim.Core
{
    /// <summary>Animaciones simples de transform sin dependencias externas.</summary>
    public static class Tween
    {
        public static Coroutine RotateLocal(MonoBehaviour host, Transform target, Quaternion to, float duration)
        {
            return host.StartCoroutine(RotateRoutine(target, to, duration));
        }

        private static IEnumerator RotateRoutine(Transform target, Quaternion to, float duration)
        {
            var from = target.localRotation;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                target.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            target.localRotation = to;
        }
    }
}
