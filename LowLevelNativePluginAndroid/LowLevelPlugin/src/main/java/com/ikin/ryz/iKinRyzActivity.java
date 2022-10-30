package com.ikin.ryz;

import android.app.NativeActivity;
import android.os.Bundle;
import android.util.Log;

import java.lang.ref.WeakReference;

public class iKinRyzActivity extends NativeActivity
{
    /**
     * A reference to the instance that acts as a singleton cache.
     * A weak reference so that there is no referencing counting and the acticity can be released normally.
     */
    public static WeakReference<iKinRyzActivity> instance = new WeakReference<>(null);

    /**
     * Initialize the activity.
     * @param icicle  If the activity is being re-initialized after previously being shut down,
     *               then this Bundle contains the data it most recently supplied in
     *               onSaveInstanceState(Bundle). Note: Otherwise it is null.
     */
    @Override
    protected void onCreate(Bundle icicle)
    {
        super.onCreate(icicle);

        // Cache the instance globally.
        instance = new WeakReference<>(this);
    }

    /**
     * The final call you receive before your activity is destroyed.
     */
    @Override
    protected void onDestroy ()
    {
        super.onDestroy();

        // Clear the global instance.
        if (instance.get() != null)
        {
            instance.clear();
        }
    }
}
