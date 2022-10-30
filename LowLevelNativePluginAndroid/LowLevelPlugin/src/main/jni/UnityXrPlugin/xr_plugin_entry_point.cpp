//
//  xr_plugin_entry_point.cpp
//  LowLevelNativePlugin
//
// Created by Joel Barba on 2020-12-18.
// Copyright © 2019 Unity Technologies. All rights reserved.
//

#define TRACE 0

#define PROFILE 0

#include <jni.h>

#include "include/android_logbuffer.h"
#include "include/native_to_unity_notifiers.h"

// Prevents the functions defined in this block from being name-mangled by C++ compiler.
// This makes them easy to locate by name, which is needed in order to bind them to C# scripts.
#ifdef __cplusplus
extern "C"
{
#endif

    /// @brief: Handles when the low-level native plugin is first loaded by the Java system.
    /// @param javaVirtualMachine An object reference that represents the JVM.
    /// @remarks: The entry point into the plugin.
    jint JNI_OnLoad(JavaVM* javaVirtualMachine, void*)
    {
        // Get the object that will be handling the actual detection of additional screens and graphics rendering.

        // Get the Java Native Interface environment from the JVM.
        JNIEnv* jniEnvironment = 0;
        javaVirtualMachine->AttachCurrentThread(&jniEnvironment, 0);

        // Get the Java class that we use to subscribe to display events.
        jclass nativeActivityClassType = jniEnvironment->FindClass("com/ikin/ryz/JavaPlugin");

        android::log_debug << "JNI_OnLoad: " << nativeActivityClassType << std::endl;

        // Get the method that subscribes to display events.
        jmethodID subscribeToScreenNotificationsMethod = jniEnvironment->GetStaticMethodID(nativeActivityClassType, "SubscribeToScreenNotifications", "()V");

        // Invoke the method as a static method.
        jniEnvironment->CallStaticVoidMethod(nativeActivityClassType, subscribeToScreenNotificationsMethod);

        return JNI_VERSION_1_6;
    }

    /// @brief A JNIEXPORT function that is tied to @see JavaPlugin.ikinRyzOnDisplayEvent.
    /// @remarks @see JNI_OnLoad calls JavaPlugin on the Java side, which establishes hardware display listen events.
    /// When these are triggered on the Java side, these functions are invoked through JNI to this side.
    /// Any pointers from C# game scripts that are set to listen will be invoked.
    /// TLDR; C++ -> Java -> C++ -> C#
    JNIEXPORT void JNICALL Java_com_ikin_ryz_JavaPlugin_ikinRyzOnDisplayEvent(JNIEnv*, jobject, display_event displayEvent, jint ryzScreenHeight)
    {
        // Notify the C# layer than the display has been connected.
        ikinRyzOnDisplayEvent(displayEvent);
    }

#ifdef __cplusplus
}
#endif

