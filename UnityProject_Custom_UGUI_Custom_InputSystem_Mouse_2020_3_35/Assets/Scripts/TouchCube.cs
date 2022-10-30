#undef TRACE

// Using statements
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

enum CubeState
{
    OnDevice,
    Transitioning,
    OnRyz
}

/// <summary>
/// Rotates the cube
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class TouchCube : MonoBehaviour
{
    #region Static Methods
    private static string ToString(TouchControl touchControl)
    {
        return $"touchId:{{{ touchControl.touchId.ReadValue()}}}," +
            $" position:{{{touchControl.position.ReadValue()}}}," +
            $" delta:{{{touchControl.delta.ReadValue()}}}," +
            $" pressure:{{{touchControl.pressure.ReadValue()}}}" +
            $" radius:{{{touchControl.radius.ReadValue()}}}," +
            $" tapCount:{{{touchControl.tapCount.ReadValue()}}}," +
            $" startTime:{{{touchControl.startTime.ReadValue()}}}," +
            $" startPosition:{{{touchControl.startPosition.ReadValue()}}}," +
            $" phase:{{{touchControl.phase.ReadValue()}}}";
    }

    private static string FormatDevice(InputDevice device)
    {
        if (device == null)
        {
            return "<No Device>";
        }

        string displayIndex = string.Empty;
        if (device is Pointer)
        {
            displayIndex = $", displayIndex: {(device as Pointer).displayIndex.ReadValue()}";
        }

        return $"{{Type: {device.GetType()}, valueType: {device.valueType}, path: {device.path}, name:{device.name}, displayName: {device.displayName}, shortDisplayName: {device.shortDisplayName}, deviceId: {device.deviceId} {displayIndex}}}";
    }
    #endregion

    #region Fields
    public float dampening = 0.997f;

    public float transitionThreshold = 0.8f;

    public Vector3 devicePosition;

    public Vector3 ryzPosition;

    private float y;

    private float yaw = 0;

    private CubeState state = CubeState.OnDevice;

    private CubeState lastState;

    private Vector3 targetPosition;

    private CubeState targetState;
    #endregion

    #region Methods
    // Update is called once per frame
    private void Update()
    {
        /*/
#if TRACE
        {
            if (Touchscreen.current != null && Touchscreen.current.added)
            {
                Debug.Log($"Touchscreen.current: {Touchscreen.current.deviceId},  Touchscreen.current.primaryTouch:{{{ToString(Touchscreen.current.primaryTouch)}}}");
            }

            Touchscreen mainDevice = ikinRyzTouchEngine.MainTouchscreen;
            if (mainDevice != null && mainDevice.added)
            {
                Debug.Log($"mainDevice: {mainDevice.deviceId}, mainDevice.primaryTouch:{{{ToString(mainDevice.primaryTouch)}}}");
            }

            Touchscreen ryzDevice = ikinRyzTouchEngine.RyzTouchscreen;
            if (ryzDevice != null && ryzDevice.added)
            {
                Debug.Log($"ryzDevice: {ryzDevice.deviceId}, ryzDevice.primaryTouch:{{{ToString(ryzDevice.primaryTouch)}}}");
            }
        }
#endif
        //*/

        Pointer pointer = Pointer.current;

#if TRACE
        Debug.Log($"TouchCube Pointer: {FormatDevice(pointer)}");
        Debug.Log($"position: {pointer.position.ReadValue()}");
#endif

        if (pointer != null)
        {
            Vector2 deltaPosition = pointer.delta.ReadValue() / -25.0f;
            bool started = pointer.press.wasPressedThisFrame;
            bool ended = pointer.press.wasReleasedThisFrame;
            Vector2 previousDeltaPosition = pointer.delta.ReadValueFromPreviousFrame();
            bool moved = pointer.press.IsPressed() && deltaPosition.x != previousDeltaPosition.x &&
                deltaPosition.y != previousDeltaPosition.y;

            if (moved)
            {
                yaw = deltaPosition.x;

                if (!started)
                {
                if (state == CubeState.OnDevice)
                {
                        y = Mathf.Max(y, -deltaPosition.y);
                }
                else if (state == CubeState.OnRyz)
                {
                    y = Mathf.Max(y, deltaPosition.y);
                }
            }
            }
            else if (ended)
                {
                if (state == CubeState.OnDevice && y > transitionThreshold)
                    {
                        state = CubeState.Transitioning;
                        targetPosition = ryzPosition;
                        targetState = CubeState.OnRyz;
                    }
                    else if (state == CubeState.OnRyz && y > transitionThreshold)
                    {
                        state = CubeState.Transitioning;
                        targetPosition = devicePosition;
                        targetState = CubeState.OnDevice;
                    }

                y = 0f;
            }
        }

        // Rotate the cube by the x,y,z values
        transform.Rotate(0, yaw, 0);

        if (state == CubeState.Transitioning)
        {
            Vector3 localPosition = transform.localPosition;

            transform.localPosition = Vector3.Lerp(targetPosition, transform.localPosition, 0.95f);

            if (0.01f > Mathf.Abs(transform.localPosition.y - targetPosition.y))
            {
                transform.localPosition = targetPosition;
                state = targetState;
            }
        }

        yaw = yaw * dampening;
        lastState = state;
    }
#endregion
}

