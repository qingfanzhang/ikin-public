//
//  native_to_unity_notifiers.cpp
//  LowLevelNativePlugin
//
//  Created by Joel Barba on 2020-12-19.
//  Copyright © 2019 Unity Technologies. All rights reserved.
//

#include "native_to_unity_notifiers.h"

#include <functional>
#include <vector>
UnityXRProjectionType projectionType = kUnityXRProjectionTypeHalfAngles;

/// @brief: The projection matrix for the left eye.
UnityXRMatrix4x4 leftProjectionMatrix;

/// @brief: The projection matrix for the right eye.
UnityXRMatrix4x4 rightProjectionMatrix;

namespace
{
    display_event displayEvent;

    /// @brief A function pointer that handles connection of the iKin Ryz.
    std::vector<std::function<DisplayEventDelegate>> displayEventVector;

}

#ifdef __cplusplus
extern "C"
{
#endif
    /// @brief Adds a function handler that will handle connection of the IKIN Ryz.
    /// @param callback A reference to the function that will handle connection of the IKIN Ryz.
    /// @remarks: This is bound to the method defined in the managed game scripts.
    EXPORT_API int ikinRyzAddOnDisplayEvent(DisplayEventDelegate* callback)
    {
        return ikinRyzAddOnDisplayEvent(std::function<DisplayEventDelegate>(callback));
    }

    EXPORT_API void ikinRyzRemoveOnDisplayEvent(int id)
    {
        if (id < 0 || id >= displayEventVector.size())
        {
            return;
        }

        displayEventVector[id] = nullptr;
    }

    /// @brief Notifies subscribers of an IKIN Ryz display event.
    /// @param displayEvent The type of display event.
    EXPORT_API void ikinRyzOnDisplayEvent(display_event value)
    {
        for (auto displayEventCallback : displayEventVector)
        {
            // If a valid callback is cached, then:
            if (displayEventCallback != nullptr)
            {
                // Have the callback handle the event.
                displayEventCallback(value);
            }
        }

        displayEvent = value;
    }

    /// @brief Gets the current display event state.
    EXPORT_API display_event ikinRyzGetDisplayEvent(void)
    {
        return displayEvent;
    }

    EXPORT_API void ikinRyzSetCameraMatrix(int stereoTargetMask,
            float m00, float m01, float m02, float m03,
            float m10, float m11, float m12, float m13,
            float m20, float m21, float m22, float m23,
            float m30, float m31, float m32, float m33)
    {
        projectionType = kUnityXRProjectionTypeMatrix;

        if (stereoTargetMask == 0 || stereoTargetMask == 1)
        {
            leftProjectionMatrix.columns[0].x = m00;
            leftProjectionMatrix.columns[1].x = m01;
            leftProjectionMatrix.columns[2].x = m02;
            leftProjectionMatrix.columns[3].x = m03;

            leftProjectionMatrix.columns[0].y = m10;
            leftProjectionMatrix.columns[1].y = m11;
            leftProjectionMatrix.columns[2].y = m12;
            leftProjectionMatrix.columns[3].y = m13;

            leftProjectionMatrix.columns[0].z = m20;
            leftProjectionMatrix.columns[1].z = m21;
            leftProjectionMatrix.columns[2].z = m22;
            leftProjectionMatrix.columns[3].z = m23;

            leftProjectionMatrix.columns[0].w = m30;
            leftProjectionMatrix.columns[1].w = m31;
            leftProjectionMatrix.columns[2].w = m32;
            leftProjectionMatrix.columns[3].w = m33;
        }

        if (stereoTargetMask == 0 || stereoTargetMask == 2)
        {
            rightProjectionMatrix.columns[0].x = m00;
            rightProjectionMatrix.columns[1].x = m01;
            rightProjectionMatrix.columns[2].x = m02;
            rightProjectionMatrix.columns[3].x = m03;

            rightProjectionMatrix.columns[0].y = m10;
            rightProjectionMatrix.columns[1].y = m11;
            rightProjectionMatrix.columns[2].y = m12;
            rightProjectionMatrix.columns[3].y = m13;

            rightProjectionMatrix.columns[0].z = m20;
            rightProjectionMatrix.columns[1].z = m21;
            rightProjectionMatrix.columns[2].z = m22;
            rightProjectionMatrix.columns[3].z = m23;

            rightProjectionMatrix.columns[0].w = m30;
            rightProjectionMatrix.columns[1].w = m31;
            rightProjectionMatrix.columns[2].w = m32;
            rightProjectionMatrix.columns[3].w = m33;
        }
    }

#ifdef __cplusplus
}
#endif

int ikinRyzAddOnDisplayEvent(std::function<DisplayEventDelegate> callback)
{
    if (callback == nullptr)
    {
        // Do nothing else.
        return -1;
    }

    int i = 0;
    for (; i < displayEventVector.size(); ++i)
    {
        auto& displayEventCallback = displayEventVector[i];

        if (displayEventCallback == nullptr)
        {
            displayEventCallback = callback;

            return i;
        }
    }

    displayEventVector.push_back(callback);

    return i;
}
