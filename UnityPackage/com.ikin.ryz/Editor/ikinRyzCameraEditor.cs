using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ikinRyzCamera))]
public class ikinRyzCameraEditor : Editor
{
    /// <summary>
    /// The array of <see cref="ikinRyzCamera"/> instances that are being presented by this inspector.
    /// </summary>
    private ikinRyzCamera[] ikinRyzCameras;

    /// <summary>
    /// The array of <see cref="Camera"/> instances that are attached to each element in <see cref="ikinRyzCameras"/>.
    /// </summary>
    private Camera[] cameras;

    /// <summary>
    /// Handles as the inspector is being set up before any drawing.
    /// </summary>
    void OnEnable()
    {
        // Allocate space for the instances that equal the amount of objects the inspector is representing.
        ikinRyzCameras = new ikinRyzCamera[targets.Length];
        cameras = new Camera[targets.Length];
        
        // Iterate through each object that the inspector represents.
        for(int i = 0; i < targets.Length; i ++)
        {
            // Cast it rightfully into its proper type.
            ikinRyzCameras[i] = targets[i] as ikinRyzCamera;

            // Get the camera that is attached to the instance and fill the array at the element.
            cameras[i] = ikinRyzCameras[i].GetComponent<Camera>();

            // Set the target display to 1.
            cameras[i].targetDisplay = 1;
        }
    }
}
