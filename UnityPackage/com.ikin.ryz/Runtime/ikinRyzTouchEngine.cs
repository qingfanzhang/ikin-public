#undef TRACE

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
#if UNITY_EDITOR
using UnityEditor;
#endif

using InputDevice = UnityEngine.InputSystem.InputDevice;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using Pointer = UnityEngine.InputSystem.Pointer;

/// <summary>
/// Adds a <see cref="Touchscreen"/> with input simulated from other types of <see cref="Pointer"/> devices (e.g. <see cref="Mouse"/>
/// or <see cref="Pen"/>).
/// </summary>
[AddComponentMenu("IKIN/Ryz Touch Engine")]

public class ikinRyzTouchEngine : MonoBehaviour
#if UNITY_EDITOR
    , IInputStateChangeMonitor
#endif
{
    #region Constants
    /// <summary>
    /// The value of a touch ID that is considered invalid.
    /// </summary>
    public const int InvalidTouchId = 0;

    /// <summary>
    /// The value of an index in the simulated touch array that is considered invalid.
    /// </summary>
    public const int InvalidTouchIndex = -1;
    #endregion

    #region Static Methods
    private static void AddChangeMonitor(InputControl control, IInputStateChangeMonitor monitor, long monitorIndex = -1)
    {
        InputState.AddChangeMonitor(control, monitor, monitorIndex, 0);
    }

    #region Touchscreen Registry
    private static void RemoveTouchScreen(Touchscreen touchscreen)
    {
        // If there is a simulated touch screen and it has been added to the input system, then:
        if (touchscreen != null && touchscreen.added)
        {
            // Remove the simulated touchscreen from the input system.
            InputSystem.RemoveDevice(touchscreen);
        }
    }
    #endregion

    private static string ToString(Vector2 vector)
    {
        return $"({vector.x.ToString("N7")}, {vector.y.ToString("N7")})";
    }

    public static string ToString(TouchState touchState)
    {
        return $"touchId:{{{ touchState.touchId}}}," +
            $" position:{{{ToString(touchState.position)}}}," +
            $" delta:{{{ToString(touchState.delta)}}}," +
            $" pressure:{{{touchState.pressure}}}" +
            $" radius:{{{ToString(touchState.radius)}}}," +
            $" tapCount:{{{touchState.tapCount}}}," +
            $" startTime:{{{touchState.startTime}}}," +
            $" startPosition:{{{ToString(touchState.startPosition)}}}," +
            $" phase:{{{touchState.phase}}}";
    }

    public static string FormatDevice(InputDevice device)
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

    #region Static Properties

    /// <summary>
    /// Gets the touchscreen for the main device.
    /// </summary>
    public static Touchscreen MainTouchscreen => mainTouchscreen;

    /// <summary>
    /// Gets the touchscreen for the Ryz device.
    /// </summary>
    public static Touchscreen RyzTouchscreen => ryzTouchscreen;
    #endregion

    #region Static Fields
    /// <summary>
    /// The touchscreen for the main device.
    /// </summary>
    private static Touchscreen mainTouchscreen;

    /// <summary>
    /// The touchscreen for the Ryz device.
    /// </summary>
    private static Touchscreen ryzTouchscreen;

    private static List<Touchscreen> mainTouchscreens;
    #endregion

    #region Methods
    #region Unity Messages
    private void OnEnable()
    {
#if UNITY_EDITOR
        // Add the device to the system and cache it.
        InputSystem.AddDevice<Touchscreen>("Touchscreen1");
        InputSystem.AddDevice<Touchscreen>("Touchscreen2");
#endif

#if TRACE
        Debug.Log($"Current Touchscreen at OnEnable: {FormatDevice(Touchscreen.current)}.");
        Debug.Log("Input Devices at OnEnable:");
#endif

        mainTouchscreens = new List<Touchscreen>(2);

        // For every device in the input system, then:
        foreach (var device in InputSystem.devices)
        {
#if TRACE
            Debug.Log(FormatDevice(device));
#endif
            // Call on device change to ensure that an action is performed when the simulation is enabled.
            OnDeviceChange(device, InputDeviceChange.Added);
        }

        // Subscribe uniquely to be notified whenever a device changes.
        InputSystem.onDeviceChange -= OnDeviceChange;
        InputSystem.onDeviceChange += OnDeviceChange;

#if UNITY_EDITOR
        // If there are no touches cached, then:
        if (simulatedMainTouches == null)
        {
            // Create the array.
            simulatedMainTouches = new SimulatedTouch[MainTouchscreen.touches.Count];
        }

        // If there are no touches cached, then:
        if (simulatedRyzTouches == null)
        {
            // Create the array.
            simulatedRyzTouches = new SimulatedTouch[RyzTouchscreen.touches.Count];
        }

        // First index is the display area. The second index is the region on that display.
        targetsByXRRenderMode = new InputTarget[][]
        {
            // Display 0
            new InputTarget[]
            {
                // Region 0
                new InputTarget
                {
                    Touchscreen = mainTouchscreen,
                    SimulatedTouches = simulatedMainTouches,
                    Matrix = Matrix4x4.identity,
                    DeltaMatrix = Matrix4x4.identity,
                    HomogenousMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                        new Vector3(1.0f/Screen.width, 1.0f/Screen.height, 1.0f))
                }
            },
            // Display 1
            new InputTarget[]
            {
                // Region 0
                new InputTarget
                {
                    Touchscreen = ryzTouchscreen,
                    SimulatedTouches = simulatedRyzTouches,
                    Matrix = Matrix4x4.identity,
                    DeltaMatrix = Matrix4x4.identity,
                    HomogenousMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                        new Vector3(1.0f/Screen.width, 1.0f/Screen.height, 1.0f))
                }
            },
        };
#endif
#if !UNITY_EDITOR && UNITY_ANDROID
        InputSystem.onEvent -= OnEvent;
        InputSystem.onEvent += OnEvent;
#endif
    }

    private void OnApplicationQuit()
    {
        RemoveTouchScreen(mainTouchscreen);
        RemoveTouchScreen(ryzTouchscreen);

#if UNITY_EDITOR
        // Unsubscribe from being noticed of state changes to the device monitor.
        UninstallStateChangeMonitors();

        // Clear out the first N sources and positions.
        inputSources.Clear(inputSourceCount);
        inputSourcePositions.Clear(inputSourceCount);

        // Clear all the simulated touches
        simulatedMainTouches.Clear();
        simulatedRyzTouches.Clear();

        // Clear the number of sources.
        inputSourceCount = 0;

        // Invalidate the last touch ID and the primary touch index.
        lastTouchId = InvalidTouchId;
        primaryTouchIndex = InvalidTouchIndex;

        // Unsubscribe from being notified by changes to the device.
        InputSystem.onDeviceChange -= OnDeviceChange;
#endif
    }

    private void OnDisable()
    {
        RemoveTouchScreen(mainTouchscreen);
        RemoveTouchScreen(ryzTouchscreen);

#if UNITY_EDITOR
        // Unsubscribe from being noticed of state changes to the device monitor.
        UninstallStateChangeMonitors();

        // Clear out the first N sources and positions.
        inputSources.Clear(inputSourceCount);
        inputSourcePositions.Clear(inputSourceCount);

        // Clear all the simulated touches
        simulatedMainTouches.Clear();
        simulatedRyzTouches.Clear();

        // Clear the number of sources.
        inputSourceCount = 0;

        // Invalidate the last touch ID and the primary touch index.
        lastTouchId = InvalidTouchId;
        primaryTouchIndex = InvalidTouchIndex;

        // Unsubscribe from being notified by changes to the device.
        InputSystem.onDeviceChange -= OnDeviceChange;
#endif
    }
#endregion

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
#if TRACE
        Debug.Log($"A device was {change}: {FormatDevice(device)}.");
#endif
        // The next three conditions narrow the sieve so that the only items acted upon are 
        // If the device changed is either of the two touchscreens and its being removed, then:
        if ((device == mainTouchscreen || device == ryzTouchscreen) && change == InputDeviceChange.Removed)
        {
            // Do nothing else.
            return;
        }

        // Otherwise, the device is one of the touchscreens,
        // both touchscreens haven't been assigned, or the device is being added.

        // If the pointer is a touchscreen, then:
        if (device is Touchscreen touchscreen)
        {
#if TRACE
            Debug.Log($"Device {FormatDevice(device)} is a Touchscreen.");
#endif
            if (change == InputDeviceChange.Added)
            {
#if TRACE
                Debug.Log($"Touchscreen {FormatDevice(device)} was added.");
#endif
                if (touchscreen.name.Equals("Touchscreen", StringComparison.Ordinal) ||
                    touchscreen.name.Equals("Touchscreen1", StringComparison.Ordinal))
                {
#if TRACE
                    Debug.Log($"Touchscreen {FormatDevice(device)} will be Main Touchscreen.");
#endif

#if UNITY_EDITOR
                    mainTouchscreen = touchscreen;
#endif
                    mainTouchscreens.Add(touchscreen);

                    if (mainTouchscreen != null)
                    {
                        mainTouchscreen.MakeCurrent();
                    }
#if TRACE
                    Debug.Log($"Current Touchscreen is: {FormatDevice(Touchscreen.current)}.");
#endif
                }
                else if(touchscreen.name.Equals("Touchscreen2", StringComparison.Ordinal))
                {
#if TRACE
                    Debug.Log($"Touchscreen {FormatDevice(device)} will be Ryz Touchscreen.");
#endif
                    ryzTouchscreen = touchscreen;

                    mainTouchscreens.Add(touchscreen);

                    if (ryzTouchscreen != null)
                    {
                        ryzTouchscreen.MakeCurrent();
                    }
#if TRACE
                    Debug.Log($"Current Touchscreen is: {FormatDevice(Touchscreen.current)}.");
#endif
                }
            }
            else if (change == InputDeviceChange.Removed)
            {
#if TRACE
                Debug.Log($"Touchscreen {FormatDevice(device)} was Removed.");
#endif
                if (mainTouchscreen != null && mainTouchscreen.deviceId == touchscreen.deviceId)
                {
#if TRACE
                    Debug.Log($"Touchscreen {FormatDevice(device)} no longer Main Touchscreen.");
#endif
                    mainTouchscreen = null;
                }
                else if (ryzTouchscreen != null && ryzTouchscreen.deviceId == touchscreen.deviceId)
                {
#if TRACE
                    Debug.Log($"Touchscreen {FormatDevice(device)} no longer Ryz Touchscreen.");
#endif
                    ryzTouchscreen = null;
                }
            }

            // Ignore it. Do nothing else.
            return;
        }

#if UNITY_EDITOR
        // If the device is not a pointer (and cast is cached), then:
        if (!(device is Pointer pointer))
        {
            // Do nothing else.
            return;
        }

        // Otherwise, the device is a  

        // If the change is of the following:
        switch (change)
        {
            // If the device was recently added, then:
            case InputDeviceChange.Added:
                // Add the pointer to the list of pointers.
                AddPointer(pointer);
                break;

            // If the device was recently removed, then:
            case InputDeviceChange.Removed:
                // Remove the pointer from the list of pointers.
                RemovePointer(pointer);
                break;
        }
    #endif
    }

#if !UNITY_EDITOR && UNITY_ANDROID
    private void OnEvent(InputEventPtr eventPtr, InputDevice inputDevice)
    {
        for (int i = 0; i < mainTouchscreens.Count; ++i)
        {
            if (inputDevice == mainTouchscreens[i])
            {
                mainTouchscreen = mainTouchscreens[i];
                
                InputSystem.onEvent -= OnEvent;
                return;
            }
        }
    }
#endif
    #endregion

#if UNITY_EDITOR
    #region Types
    /// <summary>
    /// Abstracts the interface of registering for notifications when input controls change state.
    /// </summary>
    /// <param name="control">The input control tthat notifies of state changes.</param>
    /// <param name="monitor">The object that monitors changes.</param>
    /// <param name="monitorIndex">A special value used to parse information from the state change.</param>
    private delegate void StateChangeMonitorAction(InputControl control, IInputStateChangeMonitor monitor, long monitorIndex);

    /// <summary>
    /// Used to retain the state of the touches coming from the source inputs.
    /// </summary>
    private struct SimulatedTouch
    {
        /// <summary>
        /// The index associated with the source device that the touch came from.
        /// </summary>
        public int sourceIndex;

        /// <summary>
        /// The index associated with the pointer button that activated the touch.
        /// </summary>
        public int buttonIndex;

        /// <summary>
        /// The unique identifier for this touch.
        /// </summary>
        public int touchId;
    }

    /// <summary>
    /// Used to store a receiver of source input data and how the data transforms to it.
    /// </summary>
    private struct InputTarget
    {
        /// <summary>
        /// The touchscreen that receives touches from source inputs.
        /// </summary>
        public Touchscreen Touchscreen;

        /// <summary>
        /// The cache of touches and how they are simulated.
        /// </summary>
        public SimulatedTouch[] SimulatedTouches;

        /// <summary>
        /// The matrix used to transform the position from source device to the destination.
        /// </summary>
        public Matrix4x4 Matrix;

        /// <summary>
        /// The matrix used to transform the delta position from source device to the destination.
        /// </summary>
        public Matrix4x4 DeltaMatrix;

        /// <summary>
        /// The matrix used to transform the pointer position from the source device into a ([0, 1], [0, 1]) coordinate. 
        /// </summary>
        public Matrix4x4 HomogenousMatrix;
    }
    #endregion

    #region Static Methods
    /// <summary>
    /// Initializes the static members of the <see cref="iKinRyzTouchEngine"/> class.
    /// </summary>
    static ikinRyzTouchEngine()
    {
        // The MonoBehaviour cctor may get called as part of the MonoBehaviour being created.
        // Delay-execute the code here to avoid triggering InputSystem initialization.
        EditorApplication.delayCall += () =>
        {
            InputSystem.onSettingsChange += OnSettingsChanged;
            InputSystem.onBeforeUpdate += ReEnableAfterDomainReload;
        };
    }

    private static void ReEnableAfterDomainReload()
    {
        OnSettingsChanged();

        // Remove itself after the first call.
        InputSystem.onBeforeUpdate -= ReEnableAfterDomainReload;
    }

    private static void OnSettingsChanged()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        FieldInfo tapTimeFieldInfo = typeof(Touchscreen).GetField("s_TapTime", flags);
        FieldInfo tapRadiusSquaredFieldInfo = typeof(Touchscreen).GetField("s_TapRadiusSquared", flags);

        tapTime = (float)tapTimeFieldInfo.GetValue(null);
        tapRadiusSquared = (float)tapRadiusSquaredFieldInfo.GetValue(null);
    }

    private static bool Intersects(InputTarget currentTarget, Vector2 position)
    {
        // If the matrix of the current target is the identity matrix, then the current target is the whole Editor screen.
        // If current target is the whole editor screen, then:
        if (currentTarget.Matrix == Matrix4x4.identity)
        {
            // We are always intersecting with the whole Editor screen.
            return true;
        }

        // Otherwise, we are intersecting with the a section of the screen.

        // Take the position in the Editor screen space and transform it into the coorinates for the subregion.
        // These coordinates are homogenous (aka unit square)
        var homogenousPosition = currentTarget.HomogenousMatrix.MultiplyPoint(position);

        Debug.Log($"Intersects homogenousPosition: {homogenousPosition}");

        // Check to see that the X and Y coordinates are within unit values.
        return (homogenousPosition.x >= 0.0f && homogenousPosition.x <= 1.0f) &&
            (homogenousPosition.y >= 0.0f && homogenousPosition.y <= 1.0f);
    }
    #endregion

    #region Static Fields
    private static float tapTime;

    private static float tapRadiusSquared;
    #endregion

    #region Fields
    /// <summary>
    /// The number of input sources that relay to the Touchscreen.
    /// </summary>
    [NonSerialized]
    private int inputSourceCount;

    /// <summary>
    /// The list of pointers whose input is captured to relay to the touchscreens.
    /// </summary>
    [NonSerialized]
    private Pointer[] inputSources;

    /// <summary>
    /// The list of current positions of each pointer. Their location in the qarray corresponds to inputSources. 
    /// </summary>
    [NonSerialized]
    private Vector2[] inputSourcePositions;

    /// <summary>
    /// A cache of the latest simulated touches going to the main screen.
    /// </summary>
    [NonSerialized]
    private SimulatedTouch[] simulatedMainTouches;

    /// <summary>
    /// The number of simulated touches.
    /// </summary>
    [NonSerialized]
    private SimulatedTouch[] simulatedRyzTouches;

    /// <summary>
    /// A collection of target array values associated by render mode keys.
    /// </summary>
    /// <remarks>Used to cache the target mapping data upfront.</remarks>
    [NonSerialized]
    private InputTarget[][] targetsByXRRenderMode;

    /// <summary>
    /// The ID of the last touch.
    /// </summary>
    [NonSerialized]
    private int lastTouchId;

    /// <summary>
    /// The index of the primary touch.
    /// </summary>
    [NonSerialized]
    private int primaryTouchIndex = InvalidTouchIndex;

    /// <summary>
    /// The last known XR render mode that the Game View window was in.
    /// </summary>
    [NonSerialized]
    private int lastTargetDisplay = -1;
    #endregion

    #region Methods
    #region Pointer Registry
    private void AddPointer(Pointer pointer)
    {
        // If the pointer is invalid, then:
        if (pointer == null)
        {
            // Throw an error.
            throw new ArgumentNullException(nameof(pointer));
        }

        // Otherwise, there is a valid pointer.

        // If the sources array already contains the pointer, then:
        if (ikinRyzArrayHelpers.ContainsReference(inputSources, inputSourceCount, pointer))
        {
            // Ignore if already added. Do nothing else.
            return;
        }

        // Otherwise, the pointer does not exist in the pointer map.

        // Cache the number of sources locally so that we don't increment the index just yet.
        var numPositions = inputSourceCount;

        // Append a new default current position onto the array
        ikinRyzArrayHelpers.AppendWithCapacity(ref inputSourcePositions, ref numPositions, Vector2.zero);

        // Append a new pointer in to the list of source pointers.
        var index = ikinRyzArrayHelpers.AppendWithCapacity(ref inputSources, ref inputSourceCount, pointer);

        // Subscribe to be notified of when the new pointer changes state.
        InstallStateChangeMonitors(index);
    }

    private void RemovePointer(Pointer pointer)
    {
        // If the pointer is invalid, then:
        if (pointer == null)
        {
            // Throw an error.
            throw new ArgumentNullException(nameof(pointer));
        }

        // Otherwise, there is a valid pointer.

        // Try and get the index of the pointer as it exists in the list.
        var index = ikinRyzArrayHelpers.IndexOfReference(inputSources, pointer, inputSourceCount);

        if (index == InvalidTouchIndex)
        {
            // Ignore if already added. Do nothing else.
            return;
        }

        // Removing the pointer will shift indices of all pointers coming after it. So we uninstall all
        // monitors starting with the device we're about to remove and then re-install whatever is left
        // starting at the same index.

        // Unsubscribe from being notified of when the new pointer changes state.
        UninstallStateChangeMonitors(index);

        // Cancel all ongoing touches that came from the removed pointer by sending a touch state that ends the touch.
        CancelTouch(mainTouchscreen, simulatedMainTouches, index);
        CancelTouch(ryzTouchscreen, simulatedRyzTouches, index);

        // Remove from arrays.
        var numPositions = inputSourceCount;
        ikinRyzArrayHelpers.EraseAtWithCapacity(inputSourcePositions, ref numPositions, index);
        ikinRyzArrayHelpers.EraseAtWithCapacity(inputSources, ref inputSourceCount, index);

        if (index != inputSourceCount)
        {
            InstallStateChangeMonitors(index);
        }
    }

    private void CancelTouch(Touchscreen touchscreen, SimulatedTouch[] simulatedTouches, int index)
    {
        // Do this by first iterating through all the simulated touches.
        for (var i = 0; i < simulatedTouches.Length; ++i)
        {
            // If the touch ID is invalid or the source index does not match the index removed, then:
            if (simulatedTouches[i].touchId == InvalidTouchId || simulatedTouches[i].sourceIndex != index)
            {
                // Do nothing else.
                continue;
            }

            // Otherwise, the touch ID is valid and the pointer has not been removed.

            // Create a new touch state object that defaults to a canceled phase and sets the position to the current position of the removed pointer.
            var touch = new TouchState
            {
                phase = TouchPhase.Canceled,
                position = inputSourcePositions[index],
                touchId = simulatedTouches[i].touchId,
            };

            // If the touch is the primary touch index, then: 
            if (primaryTouchIndex == i)
            {
                // Norify the input system that the primary touch control was changed with the touch value.
                InputState.Change(touchscreen.primaryTouch, touch);

                // Invalidate the primary touch index.
                primaryTouchIndex = InvalidTouchIndex;
            }

            // Notify the input system that the touch control was changed with the touch value.
            InputState.Change(touchscreen.touches[i], touch);

            // Reset the touch and the source index for this touch.
            simulatedTouches[i].touchId = InvalidTouchId;
            simulatedTouches[i].sourceIndex = 0;
        }
    }
    #endregion

    #region State Change Monitor Registry
    /// <summary>
    /// Subscribes to the state change notification for all pointers after an index in the map.
    /// </summary>
    /// <param name="startIndex">The starting index.</param>
    private void InstallStateChangeMonitors(int startIndex = 0)
    {
        StateChangeMonitorRegistry(AddChangeMonitor, startIndex);
    }
    
    /// <summary>
    /// Unsubscribes from the state change notification for all pointers after an index in the map.
    /// </summary>
    /// <param name="startIndex">The starting index.</param>
    private void UninstallStateChangeMonitors(int startIndex = 0)
    {
        StateChangeMonitorRegistry(InputState.RemoveChangeMonitor, startIndex);
    }

    private void StateChangeMonitorRegistry(StateChangeMonitorAction action, int startIndex)
    {
        // Iterates through all pointers after the start index, including the start index.
        for (var i = startIndex; i < inputSourceCount; ++i)
        {
            // Get the pointer.
            var pointer = inputSources[i];

            // Perform action for state change monitoring on the position control.
            action(pointer.position, this, i);

            // Monitor any button that isn't synthetic by doing the folowing:

            // Default the button to zero.
            var buttonIndex = 0;

            // Iterate through every control in  all the pointer's controls.
            foreach (var control in pointer.allControls)
            {
                // If the control is a button and the button is not synthetic, then:
                if (!(control is ButtonControl button) || button.synthetic)
                {
                    continue;
                }

                // The code is Unique by shifting the button index into the upper bits to create a unique hash index.
                // By doing so, we are able to store information about the source device and about the button itself.
                long monitorIndex = ((long)(uint)buttonIndex << 32) | (uint)i;

                // Perform action for state change monitoring for the button controls as well.
                action(button, this, monitorIndex);

                // Increase the number of buttons indexed so that the monitor Index packs different values.
                ++buttonIndex;
            }
        }
    }
#endregion

    #region IInputStateChangeMonitor Interface
    void IInputStateChangeMonitor.NotifyControlStateChanged(InputControl control, double time, InputEventPtr eventPtr, long monitorIndex)
    {
        var mouseControl = control.parent as UnityEngine.InputSystem.Mouse;
        int displayIndex = mouseControl != null ? mouseControl.displayIndex.ReadValue() : 0;

        // Ignore mouse events from the simulator, RYZNativeCallbacks will process those instead
        if(EditorWindow.mouseOverWindow != null)
        {
            OnSourceControlChangedValue(control, time, eventPtr, monitorIndex, displayIndex);
        }
    }

    void IInputStateChangeMonitor.NotifyTimerExpired(InputControl control, double time, long monitorIndex, int timerIndex)
    {
    }
    #endregion

    #region Pointer Registry
    private void OnSourceControlChangedValue(InputControl control, double time, InputEventPtr eventPtr, long sourceDeviceAndButtonIndex, int displayIndex)
    {
        // The index is packed with information about the button pressed and the index of the input device the control comes from.
        // We unpack the device index.
        int sourceDeviceIndex = (int)(sourceDeviceAndButtonIndex & 0xffffffff);

        // If the index is outside of the array range, then:
        if (sourceDeviceIndex < 0 && sourceDeviceIndex >= inputSourceCount)
        {
            // it is not a device index. Throw an exception.
            string messages = $"Index {sourceDeviceIndex} out of range; have {inputSources} sources";
            throw new ArgumentOutOfRangeException(nameof(sourceDeviceIndex), messages);
        }

        var currentTargets = targetsByXRRenderMode[displayIndex];

        // The last render mode is 
        lastTargetDisplay = displayIndex;

        //TODO: this can be simplified a lot if we use events instead of InputState.Change() but doing so requires work on buffering events while processing; also
        //       needs extra handling to not lag into the next frame

        //*/
        // If the control is a button control, then:
        if (control is ButtonControl button)
        {
            // Unpack the button index from the index that was passed in.
            var buttonIndex = (int)(sourceDeviceAndButtonIndex >> 32);

            bool isPressed = button.isPressed;

            // For every current target, perform the following:
            for(int i = 0; i < currentTargets.Length; ++i)
            {
                var currentTarget = currentTargets[i];

                // If the button was pressed, then:
                if (isPressed)
                {
                    // If the position of the source pointer does not lay in the current target region, then:
                    if (!Intersects(currentTarget, inputSourcePositions[sourceDeviceIndex]))
                    {
                        continue;
                    }
                    
                    // Make the current touchscreen the one associated with the target.
                    currentTarget.Touchscreen.MakeCurrent();

                    // Begin a Touch event.
                    SendBeginEvent((byte)i, currentTarget, time, eventPtr, sourceDeviceIndex, buttonIndex);
                }
                else
                {
                    // Otherwise, the button is released.

                    // End ongoing touch.
                    SendEndEvent((byte)i, currentTarget, time, eventPtr, sourceDeviceIndex, buttonIndex);
                }
            }
        }
        else if (control is InputControl<Vector2> positionControl)
        {
            // Otherwise, the control is not a position control. It is a source device that is being monitored.
            // Get the current position.
            var position = positionControl.ReadValue();

            // Calculate the delta position by taking the previous position from the source device.
            var deltaPosition = position - inputSourcePositions[sourceDeviceIndex];

            // Update the current position for the device.
            inputSourcePositions[sourceDeviceIndex] = position;

            for (int i = 0; i < currentTargets.Length; ++i)
            {
                var currentTarget = currentTargets[i];

                if (Intersects(currentTarget, position))
                {
                    // Update position of ongoing touches from this pointer.
                    SendMoveEvent((byte)i, currentTarget, eventPtr, sourceDeviceIndex, position, deltaPosition);
                }
            }
        }
        else 
        {
#if TRACE
            Debug.LogError("Expecting control to be either a button or a position");
#endif
        }
        //*/
    }

    private void SendBeginEvent(byte displayIndex, InputTarget currentTarget, double time, InputEventPtr eventPtr, int sourceDeviceIndex,
        int buttonIndex)
    {
        // Iterate through the different touches.
        for (var i = 0; i < currentTarget.SimulatedTouches.Length; ++i)
        {
            // If a touch is not being used, then:
            if (currentTarget.SimulatedTouches[i].touchId != InvalidTouchId)
            {
                // Ignore it. Move to the next item in the iteration.
                continue;
            }

            // Otherwise, the touch is being used. 

            // Increase the last touch ID.
            ++lastTouchId;

            // Cache the last touch ID.
            var touchId = lastTouchId;

            // Create a simulated touch object.
            // Stores the touch ID, the unpacked button index, and the device that originally created the button press.
            // Cache it for use with the 
            currentTarget.SimulatedTouches[i] = new SimulatedTouch
            {
                touchId = touchId,
                buttonIndex = buttonIndex,
                sourceIndex = (int)sourceDeviceIndex,
            };

            // If the primary touch index is previously invalid, then this touch will be the primary touch. 
#if UNITY_EDITOR
            // only one touch point in unity editor
          //  Debug.Log("primary: " + primaryTouchIndex + " invalid: " + InvalidTouchIndex);
            var isPrimary = true;
#else
            var isPrimary = primaryTouchIndex == InvalidTouchIndex;
#endif

            // Get the current position of this touch index. 
            Vector2 position = inputSourcePositions[sourceDeviceIndex];
            position = currentTarget.Matrix.MultiplyPoint(position);

            // Get the previous touch value from this touch index.
            var oldTouch = currentTarget.Touchscreen.touches[i].ReadValue();

            // Create a new touch state.
            var touchState = new TouchState
            {
                touchId = touchId,
                displayIndex = displayIndex,
                phase = TouchPhase.Began,
                position = position,
                tapCount = oldTouch.tapCount,
                isTap = false,
                startTime = time,
                startPosition = position,
                isPrimaryTouch = true,
            };

            // If the touch screen is primary, then:
            if (isPrimary)
            {
                // Update input state of the primary touch.
                InputState.Change(currentTarget.Touchscreen.primaryTouch, touchState, eventPtr: eventPtr);

                // This touch index is the primary touch index.
                primaryTouchIndex = i;
            }

            // Update input state of the touch.
            InputState.Change(currentTarget.Touchscreen.touches[i], touchState, eventPtr: eventPtr);

            // The touch has been found. Exit the iteration.
            break;
        }
    }

    private void SendEndEvent(byte displayIndex, InputTarget currentTarget, double time, InputEventPtr eventPtr, int sourceDeviceIndex,
        int buttonIndex)
    {
        // Iterate through the different touches.
        for (var i = 0; i < currentTarget.SimulatedTouches.Length; ++i)
        {
            // If the button index of this touch doesn;t match the button index given, or source device of this touch is not the source that is changing its state, or the touch ID of this touch is invalid, then: 
            if (currentTarget.SimulatedTouches[i].buttonIndex != buttonIndex ||
                currentTarget.SimulatedTouches[i].sourceIndex != sourceDeviceIndex ||
                currentTarget.SimulatedTouches[i].touchId == InvalidTouchId)
            {
                // Ignore it. Move to the next item in the iteration.
                continue;
            }

            // Get the current position of this touch index. 
            Vector2 position = inputSourcePositions[sourceDeviceIndex];
            position = currentTarget.Matrix.MultiplyPoint(position);

            // Get the previous touch value from this touch index.
            var oldTouch = currentTarget.Touchscreen.touches[i].ReadValue();
            // Detect taps. If the time was short enough and the position was within the rouch radius, then recognize it as a tap.
            bool isTap = (time - oldTouch.startTime) <= tapTime && (position - oldTouch.startPosition).sqrMagnitude <= tapRadiusSquared;

            // Create a new touch state.
            var touchState = new TouchState
            {
                touchId = currentTarget.SimulatedTouches[i].touchId,
                displayIndex = displayIndex,
                phase = TouchPhase.Ended,
                position = position,
                //*/
                tapCount = (byte)(oldTouch.tapCount + (isTap ? 1 : 0)),
                isTap = isTap,
                startPosition = oldTouch.startPosition,
                startTime = oldTouch.startTime,
                //*/
            };

            ////InputSystem.QueueStateEvent(currentTarget.Touchscreen, touchState);

            // If the touch screen is primary, then:
            if (primaryTouchIndex == i)
            {
                // Update input state of the primary touch.
                InputState.Change(currentTarget.Touchscreen.primaryTouch, touchState, eventPtr: eventPtr);

                ////TODO: check if there's an ongoing touch that can take over
                // Invalidate the prinary touch index.
                primaryTouchIndex = InvalidTouchIndex;
            }

            // Update input state of the touch.
            InputState.Change(currentTarget.Touchscreen.touches[i], touchState, eventPtr: eventPtr);

            // Use an invalid touch ID.
            currentTarget.SimulatedTouches[i].touchId = InvalidTouchId;

            // The touch has been found. Exit the iteration.
            break;
        }
    }

    private void SendMoveEvent(byte displayIndex, InputTarget currentTarget, InputEventPtr eventPtr, int sourceDeviceIndex,
        Vector2 position, Vector2 deltaPosition)
    {
        position = currentTarget.Matrix.MultiplyPoint(position);
        deltaPosition = currentTarget.DeltaMatrix.MultiplyPoint(deltaPosition);

        // Iterate through the different touches.
        for (var i = 0; i < currentTarget.SimulatedTouches.Length; ++i)
        {
            // If the source device of this touch is not the source that is changing its state, or the touch ID of this touch is invalid, then: 
            if (currentTarget.SimulatedTouches[i].sourceIndex != sourceDeviceIndex ||
                currentTarget.SimulatedTouches[i].touchId == InvalidTouchId)
            {
                // Ignore it. Move to the next item in the iteration.
                continue;
            }

            // Get the previous touch value from this touch index.
            var oldTouch = currentTarget.Touchscreen.touches[i].ReadValue();

            // If the primary touch index is this touch, then this touch will be the primary touch. 
            var isPrimary = primaryTouchIndex == i;

            // Create a new touch state.
            var touchState = new TouchState
            {
                touchId = currentTarget.SimulatedTouches[i].touchId,
                displayIndex = displayIndex,
                phase = TouchPhase.Moved,
                position = position,
                //*/
                delta = deltaPosition,
                isPrimaryTouch = isPrimary,
                tapCount = oldTouch.tapCount,
                isTap = false, // Can't be tap as it's a move.
                startPosition = oldTouch.startPosition,
                startTime = oldTouch.startTime,
                //*/
            };

            ////InputSystem.QueueStateEvent(currentTarget.Touchscreen, touchState);

            // If the touch screen is primary, then:
            if (isPrimary)
            {
                // Update input state of the primary touch.
               
                InputState.Change(currentTarget.Touchscreen.primaryTouch, touchState, eventPtr: eventPtr);
            }

            // Update input state of the touch.
            InputState.Change(currentTarget.Touchscreen.touches[i], touchState, eventPtr: eventPtr);
        }
    }
    #endregion
    #endregion
#endif
}
