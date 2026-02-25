using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace Extensions.CutsceneEngine
{
    /**
     * <summary>
     * The CinemachineCameraSystem class implements the ICameraSystem interface to provide camera control functionality using Unity's Cinemachine package.
     * It allows for focusing the camera on a target, moving the camera to a specific position over a given duration, and shaking the camera with specified intensity and duration.
     * This class is designed to be used within the cutscene engine to facilitate dynamic camera movements and effects during cutscenes.
     * </summary>
     */
    public class CinemachineCameraSystem : ICameraSystem
    {
        MonoBehaviour runner;
        CinemachineCamera cam;

        public CinemachineCameraSystem(MonoBehaviour coroutineRunner, CinemachineCamera camera)
        {
            runner = coroutineRunner;
            cam = camera;
        }

        public void FocusOn(Transform target)
        {
            cam.Follow = target;
            cam.LookAt = target;
        }

        public void MoveTo(Vector3 position, float duration)
        {
            runner.StartCoroutine(MoveRoutine(position, duration));
        }

        IEnumerator MoveRoutine(Vector3 target, float duration)
        {
            var start = cam.transform.position;
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                cam.transform.position = Vector3.Lerp(start, target, time / duration);
                yield return null;
            }
        }

        public void Shake(float intensity, float duration)
        {
            // TODO: idk how to screen shake rofl
        }
    }
}