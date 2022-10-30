// Using statements
using UnityEngine;

/// <summary>
/// Rotates the cube
/// </summary>
public class RotateCube : MonoBehaviour
{
    // x,y,z values for rotating cube
    public float x, y, z;

    // Update is called once per frame
    void Update()
    {
        // Rotate the cube by the x,y,z values
        transform.Rotate(x, y, z);
    }
}
