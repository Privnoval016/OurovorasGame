using Sirenix.OdinInspector;
using UnityEngine;

namespace Extensions.Utils
{
    /** <summary>
     * A simple component used to bake a camera render texture to a png image at the click of a button.
     * </summary>
     */
    public class ImageRenderer : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Camera to render from. If left unassigned, the script will log an error and do nothing when RenderImage is called.")]
        [SerializeField] private Camera renderCamera;
        [Tooltip("Name of the rendered image file. Should include the .png extension. Will be overwritten if a file with the same name already exists in the target directory.")]
        [SerializeField] private string fileName = "rendered_image.png";
        [Tooltip("Directory within the Assets folder to save the rendered image to. Will be created if it doesn't exist. Formatted as 'Directory/Subdirectory' (without leading or trailing slashes), and relative to the Assets folder. For example, 'RenderedImages' or 'Textures/Renders'.")]
        [SerializeField] private string directory = "RenderedImages";
        [Header("Render Settings")]
        [Tooltip("Whether to render the image with a transparent background (ARGB32) or a solid background (RGB24). Note that the camera's clear flags and background color will still affect the final image.")]
        [SerializeField] private bool transparentBackground = true;

        [Button]
        public void RenderImage()
        {
            if (renderCamera == null)
            {
                Debug.LogError("Render Camera is not assigned.");
                return;
            }

            int width = renderCamera.pixelWidth;
            int height = renderCamera.pixelHeight;
            RenderTexture rt = new RenderTexture(width, height, 24);
            renderCamera.targetTexture = rt;
            Texture2D screenShot = new Texture2D(width, height,
                transparentBackground ? TextureFormat.ARGB32 : TextureFormat.RGB24, false);
            renderCamera.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenShot.Apply();
            renderCamera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(rt);

            byte[] bytes = screenShot.EncodeToPNG();
            string fullPath = System.IO.Path.Combine(Application.dataPath, directory);
            if (!System.IO.Directory.Exists(fullPath))
            {
                System.IO.Directory.CreateDirectory(fullPath);
            }

            string filePath = System.IO.Path.Combine(fullPath, fileName);
            System.IO.File.WriteAllBytes(filePath, bytes);

            Debug.Log($"Image rendered and saved to: {filePath}");

        }
    }
}