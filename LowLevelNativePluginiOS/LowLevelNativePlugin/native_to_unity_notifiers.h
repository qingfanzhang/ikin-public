//
//  native_to_unity_notifiers.h
//  LowLevelNativePlugin
//
//  Created by Joel Barba on 2020-12-19.
//  Copyright © 2019 Unity Technologies. All rights reserved.
//

#ifndef NATIVE_TO_UNITY_NOTIFIERS_HPP
#define NATIVE_TO_UNITY_NOTIFIERS_HPP

#include "UnityXRTypes.h"

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

extern UnityXRProjectionType projectionType;

/// @brief: The projection matrix for the left eye.
extern UnityXRMatrix4x4 leftProjectionMatrix;

/// @brief: The projection matrix for the right eye.
extern UnityXRMatrix4x4 rightProjectionMatrix;

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

    /// @brief Sets the camera matrix in the native plugin.
    /// @param stereoTargetMask The value of the camera XR target.</param>
    /// @param m00 Row 0, Column 0 of the matrix.</param>
    /// @param m01 Row 0, Column 1 of the matrix.</param>
    /// @param m02 Row 0, Column 2 of the matrix.</param>
    /// @param m03 Row 0, Column 3 of the matrix.</param>
    /// @param m10 Row 1, Column 0 of the matrix.</param>
    /// @param m11 Row 1, Column 1 of the matrix.</param>
    /// @param m12 Row 1, Column 2 of the matrix.</param>
    /// @param m13 Row 1, Column 3 of the matrix.</param>
    /// @param m20 Row 2, Column 0 of the matrix.</param>
    /// @param m21 Row 2, Column 1 of the matrix.</param>
    /// @param m22 Row 2, Column 2 of the matrix.</param>
    /// @param m23 Row 2, Column 3 of the matrix.</param>
    /// @param m30 Row 3, Column 0 of the matrix.</param>
    /// @param m31 Row 3, Column 1 of the matrix.</param>
    /// @param m32 Row 3, Column 2 of the matrix.</param>
    /// @param m33 Row 3, Column 3 of the matrix.</param>
    EXPORT_API void ikinRyzSetCameraMatrix(int stereoTargetMask,
            float m00, float m01, float m02, float m03,
            float m10, float m11, float m12, float m13,
            float m20, float m21, float m22, float m23,
            float m30, float m31, float m32, float m33);


    
#ifdef __cplusplus
}
#endif

/// @brief Adds a function handler that will handle connection of the IKIN Ryz.
/// @param callback A reference to the function that will handle connection of the IKIN Ryz.
/// @remarks: This is bound to the method defined in the managed game scripts.
int ikinRyzAddOnDisplayEvent(std::function<DisplayEventDelegate> callback);

#endif
