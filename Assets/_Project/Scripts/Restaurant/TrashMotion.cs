using System;
using UnityEngine;

namespace BurgerShop.Restaurant
{
    public sealed class TrashMotion : MonoBehaviour
    {
        public const float Duration = 0.35f;

        Transform follow;
        Vector3 followLocal;
        Vector3 origin;
        Vector3 hop;
        Vector3 startScale;
        Vector3 endScale;
        float age;
        Action arrived;
        bool finished;

        public bool IsFinished => finished;

        public void Launch(Transform target, Vector3 targetLocal, Vector3 hopOffset, Vector3 finalScale, Action onArrived)
        {
            transform.SetParent(null, true);
            follow = target;
            followLocal = targetLocal;
            origin = transform.position;
            hop = hopOffset;
            startScale = transform.localScale;
            endScale = finalScale;
            arrived = onArrived;
            age = 0f;
            finished = false;
        }

        public void Advance(float deltaTime)
        {
            if (finished || deltaTime <= 0f) return;
            age += deltaTime;
            float t = Mathf.Clamp01(age / Duration);
            float eased = t * t * (3f - 2f * t);
            Vector3 dest = follow != null ? follow.TransformPoint(followLocal) : followLocal;
            transform.position = Vector3.Lerp(origin, dest, eased) + hop * Mathf.Sin(t * Mathf.PI);
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            transform.rotation = Quaternion.Euler(t * 140f, t * 210f, t * 70f);
            if (t < 1f) return;
            finished = true;
            Action done = arrived;
            arrived = null;
            done?.Invoke();
        }
    }
}
