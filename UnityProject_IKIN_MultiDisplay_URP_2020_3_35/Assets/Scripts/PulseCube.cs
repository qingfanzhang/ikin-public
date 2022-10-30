// Using statements
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// Moves cube up and down to be in the view of the two cameras
/// </summary>
public class PulseCube : MonoBehaviour
{
    private bool playing = false;

    private float timeElapsed = 0.0f;

    private float endTime = 1.0f;

    [SerializeField]
    private AnimationCurve curve = null;

    // Update is called once per frame
    private void Update()
    {
#if UNITY_STANDALONE_WIN
        Pointer pointer = Mouse.current;
#else
        Pointer pointer = Touchscreen.current;
#endif

        if (pointer == null)
        {
            return;
        }

        bool clicked = pointer.press.wasPressedThisFrame;

        if (clicked)
        {
            if (!playing)
            {
                timeElapsed = 0.0f;

                if (curve.keys.Length != 0)
                {
                    Keyframe lastKey = curve.keys[curve.keys.Length - 1];
                    endTime = lastKey.time;
                }
                else
                {
                    endTime = 0.0f;
                }
            }

            playing = true;
        }

        if (playing)
        {
            float t = Mathf.Min(timeElapsed, endTime);
            float scale = curve.Evaluate(t);

            // Move the cube up and down from y position of 0 - 14
            transform.localScale = new Vector3(scale, scale, scale);

            if (timeElapsed > endTime)
            {
                playing = false;
            }

            timeElapsed += Time.deltaTime;
        }
    }
}
