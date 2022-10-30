//
//  DisplayConnectionNotifier.h
//  LowLevelNativePlugin
//
//  Created by Joel Barba on 10/8/19.
//  Copyright © 2019 Unity Technologies. All rights reserved.
//

#ifndef DISPLAYCONNECTIONNOTIFIER_H
#define DISPLAYCONNECTIONNOTIFIER_H

#import <Foundation/Foundation.h>

#include "ikin_ryz_displayer.h"

@interface DisplayConnectionNotifier : NSObject

/// @brief: Initializes an instance of this class.
/// @param ryzDisplayer A registry of the low-level interfaces that Unity provides to low-level plugins.
/// @returns: A reference to the initialized instance of this class.
- (id) initWith : (ikin_ryz_displayer*) ryzDisplayer;

/// @brief: Handles when a display is connected.
/// @param aNotification A context with details about the event that occurred.
- (void) handleDisplayConnect : (NSNotification*) aNotification;

/// @brief: Handles when a display is disconnected.
/// @param aNotification A context with details about the event that occurred.
- (void) handleDisplayDisconnect : (NSNotification*) aNotification;

@end

#endif
