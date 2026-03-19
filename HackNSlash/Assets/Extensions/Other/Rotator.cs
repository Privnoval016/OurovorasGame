using UnityEngine;

namespace Extensions.Other
{
    public class Rotator : MonoBehaviour
    {
        [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 30f, 0f);
        [SerializeField] private bool useLocalRotation = false;

        private void Update()
        {
            Vector3 deltaRotation = rotationSpeed * Time.deltaTime;

            if (useLocalRotation)
                transform.Rotate(deltaRotation, Space.Self);
            else
                transform.Rotate(deltaRotation, Space.World);
        }
    }
}