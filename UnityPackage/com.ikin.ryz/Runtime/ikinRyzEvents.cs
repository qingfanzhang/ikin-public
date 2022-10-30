#undef TRACE

using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

public enum DisplayEvent
{
    Connected = 1,
    Disconnected = 2
}

public class ikinRyzEvents
{
    #region Types
    /// <summary>
    /// Defines the signature for a function that handles connection of the iKin Ryz.
    /// </summary>
    public delegate void DisplayEventDelegate(DisplayEvent displayEvent);
    #endregion

    #region Static Methods
#if !UNITY_EDITOR && !UNITY_STANDALONE
    #region External
    /// <summary>
    /// Sets the function handler that will handle connection of the iKin Ryz.
    /// This is bound to the method defined in the native plugin.
    /// </summary>
    /// <param name="callback">A reference to the function that will handle connection of the iKin Ryz.</param>
#if UNITY_IOS
    [DllImport("__Internal")]
#else
    [DllImport("UnityXrPlugin")]
#endif
    private static extern int ikinRyzAddOnDisplayEvent(DisplayEventDelegate callback);

    /// <summary>
    /// Sets the function handler that will handle disconnection of the iKin Ryz.
    /// This is bound to the method defined in the native plugin.
    /// </summary>
    /// <param name="callback">A reference to the function that will handle disconnection of the iKin Ryz.</param>
#if UNITY_IOS
    [DllImport("__Internal")]
#else
    [DllImport("UnityXrPlugin")]
#endif
    private static extern DisplayEvent ikinRyzGetDisplayEvent();
    #endregion
#endif

#if UNITY_EDITOR || UNITY_STANDALONE
    public static DisplayEvent GetDisplayEvent()
    {
        return DisplayEvent.Connected;
    }
#else
    public static DisplayEvent GetDisplayEvent()
    {
        return ikinRyzGetDisplayEvent();
    }
#endif

    /// <summary>
    /// Handles the connection of the iKin Ryz.
    /// </summary>
    /// <remarks><see cref="MonoPInvokeCallback"/> attribute allow the function pointer to be coerced into a C function pointer.</remarks>
    [MonoPInvokeCallback(typeof(DisplayEventDelegate))]
    private static void OnDisplayEvent(DisplayEvent value)
    {
#if TRACE
        Debug.Log("IKIN Ryz Display Event!");
#endif
        // If there are subscribers, then:
        if (onDisplayEvent != null)
        {
            // Notify subscribers that the display has been connected.
            onDisplayEvent(value);
        }
    }

    /// <summary>
    /// Handles when Unity is loaded.
    /// </summary>
    [RuntimeInitializeOnLoadMethod]
    private static void OnSetDisplayConnectionSubscribers()
    {
#if !UNITY_EDITOR && !UNITY_STANDALONE
#if TRACE
        Debug.Log("Setting iKin Ryz Connection Subscribers.");
#endif
        // Sets the handler in the native plugin so that it's called when the iKin Ryz connects.
        ikinRyzAddOnDisplayEvent(OnDisplayEvent);
#endif
    }
    #endregion

    #region Static Events
    /// <summary>
    /// Notifies subscribers when the iKin Ryz connects.
    /// </summary>
    public static event DisplayEventDelegate onDisplayEvent = null;
    #endregion
}
