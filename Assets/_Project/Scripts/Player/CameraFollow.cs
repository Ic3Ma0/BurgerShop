using UnityEngine;

namespace BurgerShop.Player
{
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] float followDistance = 16f;
        [SerializeField] float height = 13f;
        [SerializeField] float yaw = 45f;
        [SerializeField] float lookAtHeight = 1.1f;
        [SerializeField] float positionSmooth = 10f;

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;
            Snap();
        }

        public void Snap()
        {
            if (target == null)
                return;

            transform.position = DesiredPosition();
            transform.LookAt(target.position + Vector3.up * lookAtHeight);
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            float t = 1f - Mathf.Exp(-positionSmooth * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, DesiredPosition(), t);
            Quaternion look = Quaternion.LookRotation(
                target.position + Vector3.up * lookAtHeight - transform.position,
                Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, t);
        }

        Vector3 DesiredPosition()
        {
            Vector3 offset = Quaternion.Euler(0f, yaw, 0f) * (Vector3.back * followDistance);
            offset.y = height;
            return target.position + offset;
        }
    }
}
