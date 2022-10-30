// Using statements
using UnityEngine;

/// <summary>
/// Moves cube up and down to be in the view of the two cameras
/// </summary>
public class MoveCube : MonoBehaviour
{    
    // Update is called once per frame
    void Update()
    {
        // Move the cube up and down from y position of 0 - 14
        transform.position = new Vector3(0, Mathf.PingPong(Time.time * 6, 14), 0);
    }
}
