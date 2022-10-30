//
//  xr_plugin_entry_point.mm
//  LowLevelNativePlugin
//
// Created by Joel Barba on 2020-12-18.
// Copyright © 2019 Unity Technologies. All rights reserved.
//

#define TRACE 0

#define PROFILE 0
#include "../External Headers/Unity/IUnityInterface.h"

#include "ikin_ryz_displayer.h"

// Prevents the functions defined in this block from being name-mangled by C++ compiler.
// This makes them easy to locate by name, which is needed in order to bind them to C# scripts.
#ifdef __cplusplus
extern "C"
{
#endif
    
    /// @brief: Handles when the low-level native plugin is first loaded by the Unity system.
    /// @param unityInterfaces A registry of the low-level interfaces that Unity provides to low-level plugins.
    /// @remarks: The entry point into the plugin.
    void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces)
    {
        // Get the object that will be handling the actual detection of additional screens and graphics rendering.
        // Subscribe to unity events.
        ikin_ryz_displayer::get_instance()->subscribe_unity_events(unityInterfaces);
    }

#ifdef __cplusplus
}
#endif

