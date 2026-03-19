using UnityEngine;

namespace Extensions.Other
{
    public class Mover : MonoBehaviour
    {
        [SerializeField] private Vector3 moveDirection = Vector3.forward;
        [SerializeField] private float moveSpeed = 5f;

        private void Update()
        {
            transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;
        }
    }
}