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
    #endregion

    #region Fields
    public float dampening = 0.997f;

    public float transitionThreshold = 0.8f;

    public Vector3 devicePosition;

    public Vector3 ryzPosition;

    private float y;

    private float yaw = 0;

    private CubeState state = CubeState.OnDevice;

    private bool isDisplayConnected = false;

    private CubeState lastState;

    private Vector3 targetPosition;

    private CubeState targetState;
    #endregion

    #region Methods
    private void Awake()
    {
        var displayEvent = ikinRyzEvents.GetDisplayEvent();

        isDisplayConnected = displayEvent == DisplayEvent.Connected;
    }

#if UNITY_STANDALONE_WIN
    private void OnEnable()
    {
        ikinRyzEvents.onDisplayEvent -= OnRyzDisplayEvent;
        ikinRyzEvents.onDisplayEvent += OnRyzDisplayEvent;
    }

    private void OnRyzDisplayEvent(DisplayEvent value)
    {
#if TRACE
        Debug.Log("OnRyzDisplayEvent: " + value);
#endif

        isDisplayConnected = value == DisplayEvent.Connected;

        // If the cube is on the Ryz when the display is disconnected, then:
        if (isDisplayConnected == false && state == CubeState.OnRyz)
        {
            // Transition the cube back down to the phone screen.
            state = CubeState.Transitioning;
            targetPosition = devicePosition;
            targetState = CubeState.OnDevice;
        }
    }
#endif
	
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

#if UNITY_STANDALONE_WIN
        Pointer pointer = Mouse.current;
#else
        Pointer pointer = Touchscreen.current;

		if (state == CubeState.OnDevice)
        {
            pointer = ikinRyzTouchEngine.MainTouchscreen;
        }
        else if (state == CubeState.OnRyz)
        {
            pointer = ikinRyzTouchEngine.RyzTouchscreen;
        }
#endif


#if TRACE
        Debug.Log($"TouchCube Pointer: {ikinRyzTouchEngine.FormatDevice(pointer)} and Current Pointer: {ikinRyzTouchEngine.FormatDevice(Pointer.current)}");
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
            else if (ended && isDisplayConnected)
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

