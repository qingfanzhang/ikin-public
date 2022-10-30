//
//  native_to_unity_notifiers.cpp
//  LowLevelNativePlugin
//
//  Created by Joel Barba on 2020-12-19.
//  Copyright © 2019 Unity Technologies. All rights reserved.
//

#include "include/native_to_unity_notifiers.h"

#include <functional>
#include <vector>
#include "include/android_logbuffer.h"

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
