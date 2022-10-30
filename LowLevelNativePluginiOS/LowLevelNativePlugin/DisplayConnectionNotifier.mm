//
//  DisplayConnectionNotifier.mm
//  LowLevelNativePlugin
//
//  Created by Joel Barba on 10/8/19.
//  Copyright © 2019 Unity Technologies. All rights reserved.
//

#include "DisplayConnectionNotifier.h"

#include "native_to_unity_notifiers.h"

@implementation DisplayConnectionNotifier

// Private Fields
{
    ikin_ryz_displayer* ryzDisplayer;
}

/// @brief: Initializes an instance of this class.
/// @param displayConnectionNotifier A registry of the low-level interfaces that Unity provides to low-level plugins.
/// @returns: A reference to the initialized instance of this class.
- (id) initWith : (ikin_ryz_displayer*) ryzDisplayer;
{
    // Ask the base class to init (there's no overriding in Obj-C, so it has to be done manually.
    // Obj-C's 'super' is like C#'s 'base', and 'self' is like 'this')
    // If the result is valid, then perform the following:
    if (self = [super init])
    {
        self->ryzDisplayer = ryzDisplayer;
    }
    
    // Return the reference, just like 'super init' does.
    return self;
}

/// @brief: Handles when a display is connected.
/// @param aNotification A context with details about the event that occurred.
- (void) handleDisplayConnect : (NSNotification*) aNotification
{
    // Request the object that's related to the notification. In this case, it is the new UI screen.
    UIScreen* newScreen = [aNotification object];

    // Setup the new display.
    ryzDisplayer->create_and_add_second_window(newScreen);
}

/// @brief: Handles when a display is disconnected.
/// @param aNotification A context with details about the event that occurred.
- (void) handleDisplayDisconnect : (NSNotification*) aNotification
{
    ryzDisplayer->destroy_and_remove_second_window();
}

@end
