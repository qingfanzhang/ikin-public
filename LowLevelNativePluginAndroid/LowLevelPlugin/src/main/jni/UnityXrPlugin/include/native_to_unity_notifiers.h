//
//  native_to_unity_notifiers.h
//  LowLevelNativePlugin
//
//  Created by Joel Barba on 2020-12-19.
//  Copyright © 2019 Unity Technologies. All rights reserved.
//

#ifndef NATIVE_TO_UNITY_NOTIFIERS_HPP
#define NATIVE_TO_UNITY_NOTIFIERS_HPP

#include <functional>

#if _MSC_VER // this is defined when compiling with Visual Studio
#define EXPORT_API __declspec(dllexport) // Visual Studio needs annotating exported functions with this
#else
#define EXPORT_API // XCode does not need annotating exported functions, so define is empty
#endif

enum display_event
{
    connected = 1,
    disconnected = 2
};

// Prevents the functions defined in this block from being name-mangled by C++ compiler.
// This makes them easy to locate by name, which is needed in order to bind them to C# scripts.
#ifdef __cplusplus
extern "C"
{
#endif

    /// @brief Defines the signature for a function that handles display events of the IKIN Ryz.
    /// @param displayEvent The type of display event.
    typedef void (DisplayEventDelegate)(display_event displayEvent);

    /// @brief Adds a function handler that will handle connection of the IKIN Ryz.
    /// @param callback A reference to the function that will handle connection of the IKIN Ryz.
    /// @remarks: This is bound to the method defined in the managed game scripts.
    EXPORT_API int ikinRyzAddOnDisplayEvent(DisplayEventDelegate* callback);

    /// @brief Removes a function handler that will handle connection of the IKIN Ryz.
    /// @param id A reference to the function that will handle connection of the IKIN Ryz.
    /// @remarks: This is bound to the method defined in the managed game scripts.
    EXPORT_API void ikinRyzRemoveOnDisplayEvent(int id);

    /// @brief Notifies subscribers of an IKIN Ryz display event.
    /// @param displayEvent The type of display event.
    EXPORT_API void ikinRyzOnDisplayEvent(display_event displayEvent);

    /// @brief Gets the current display event state.
    EXPORT_API display_event ikinRyzGetDisplayEvent(void);
    
#ifdef __cplusplus
}
#endif

/// @brief Adds a function handler that will handle connection of the IKIN Ryz.
/// @param callback A reference to the function that will handle connection of the IKIN Ryz.
/// @remarks: This is bound to the method defined in the managed game scripts.
int ikinRyzAddOnDisplayEvent(std::function<DisplayEventDelegate> callback);

#endif