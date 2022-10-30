package com.ikin.ryz;

import android.app.Activity;
import android.content.Context;
import android.graphics.Point;
import android.hardware.display.DisplayManager;
import android.os.Handler;
import android.util.Log;
import android.view.Display;

import com.unity3d.player.UnityPlayer;

public class JavaPlugin
{
    public enum DisplayEvent
    {
        CONNECTED  (1),
        DISCONNECTED(2)
        ; // semicolon needed when fields / methods follow

        private final int raw;

        DisplayEvent(int value)
        {
            this.raw = value;
        }

        public int getRaw()
        {
            return this.raw;
        }

    }

    /**
     * A request code for creating the second screen activity.
     */
    private static final int requestCode = 506;

    /**
     * @brief Notifies subscribers on an IKIN Ryz display event.
     */
    public static native void ikinRyzOnDisplayEvent(int eventType, int ryzScreenHeight);

    /**
     * @brief: The current activity.
     * @remarks: Is meant to be populated by the test-driver.
     * **/
    private static Activity currentActivity;


    private static Handler handler;

    /**
     * @brief: The current activity.
     * @param activity The activity to set as current activity.
     * @remarks: Is meant to be invoked by the test-driver.
     */
    public static void setCurrentActivity(Activity activity)
    {
        currentActivity = activity;

        // Manually loads in the native library, which is guaranteed loaded in the Unity environment.
        try
        {
            System.loadLibrary("UnityXrPlugin");
        }
        catch(Exception e)
        {
            Log.e("Managed", "Exception: " + e.getMessage());
        }
    }

    /**
     * @brief: Subscribes to be notified of changes in new hardware displays.
     */
    public static void SubscribeToScreenNotifications()
    {
        Log.d("Managed", "SubscribeToScreenNotifications");

        // If the current activity is null, then the test driver did not set it or we are in the Unity environment (we can assume the latter).
        // If we are in the Unity environment, then:
        if (currentActivity == null)
        {
            // The current activity is the UnityPlayerActivity.
            currentActivity = UnityPlayer.currentActivity;
        }

        Log.d("Managed", "SubscribeToScreenNotifications");

        DisplayManager displayManager = (DisplayManager)currentActivity.getSystemService(Context.DISPLAY_SERVICE);

        // Request the list of screens that the application starts with.
        Display[] startingScreens = displayManager.getDisplays();

        // If there are already two screens at the start, then:
        if (startingScreens.length == 2)
        {
            Display display = startingScreens[1];

            Point outSize = new Point();
            display.getSize(outSize);

            Log.d("Managed", "Calling ikinRyzOnDisplayEvent: "+ DisplayEvent.CONNECTED + ", " + outSize.y);

            // Notify subscribers that there is a connect at the start.
            ikinRyzOnDisplayEvent(DisplayEvent.CONNECTED.getRaw(), outSize.y);
        }
        else
        {
            Log.d("Managed", "Calling ikinRyzOnDisplayEvent: "+ DisplayEvent.DISCONNECTED + ", 0");

            // Otherwise, there is only one screen. Notify subscribers that there is a disconnect at the start.
            ikinRyzOnDisplayEvent(DisplayEvent.DISCONNECTED.getRaw(), 0);
        }

        // Register for screen notifications
        DisplayManager.DisplayListener displayListener = new DisplayManager.DisplayListener()
        {
            @Override
            public void onDisplayAdded(int displayID)
            {
                DisplayManager displayManager = (DisplayManager)currentActivity.getSystemService(Context.DISPLAY_SERVICE);
                Display display = displayManager.getDisplay(displayID);
                Point outSize = new Point();
                display.getSize(outSize);

                // Notify subscribers that there is a connect.
                ikinRyzOnDisplayEvent(DisplayEvent.CONNECTED.getRaw(), outSize.y);
            }

            @Override
            public void onDisplayRemoved(int displayID)
            {
                // Notify subscribers that there is a disconnect.
                ikinRyzOnDisplayEvent(DisplayEvent.DISCONNECTED.getRaw(), 0);
            }

            @Override
            public void onDisplayChanged(int i)
            {
            }
        };

        displayManager.registerDisplayListener(displayListener, new Handler(android.os.Looper.getMainLooper()));
    }
}

