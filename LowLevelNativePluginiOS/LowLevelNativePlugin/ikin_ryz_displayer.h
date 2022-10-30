//
//  ikin_ryz_displayer.h
//  LowLevelNativePlugin
//
//  Created by Joel Barba on 2020-12-19.
//  Copyright © 2019 Unity Technologies. All rights reserved.
//

#ifndef IKIN_RYZ_DISPLAYER_H
#define IKIN_RYZ_DISPLAYER_H

// Determines if a second UI screen is created for the application when a new sisplay monitor is detected. For debugging purposes.
#define SECOND_UI_SCREEN 1

// Determines if a second UI view is created and parented to the Unity window, or instead to the second screen if it exists. For debugging purposes.
#define SECOND_UI_VIEW 1

#import <Foundation/Foundation.h>
#import <Metal/Metal.h>
#import <MetalKit/MetalKit.h>

#include "../External Headers/Unity/IUnityGraphics.h"
#include "../External Headers/Unity/IUnityGraphicsMetal.h"
#include "../External Headers/Unity/IUnityInterface.h"
#include "../External Headers/Unity/IUnityProfiler.h"
#include "../External Headers/Unity/XR/IUnityXRTrace.h"
#include "../External Headers/Unity/XR/Subsystems/UnitySubsystemTypes.h"
#include "../External Headers/Unity/XR/Subsystems/Display/IUnityXRDisplay.h"

/// @brief: Handles the how the iKin Ryz composes its frame buffer and displays it.
class ikin_ryz_displayer
{
public:
    /// @brief: Lazily initializes a static instance of this class.
    /// @returns: The static instance of this class.
    static ikin_ryz_displayer* get_instance();

    /// @brief: Sets up hooks an instance of this class.
    /// @param unityInterfaces A registry of the low-level interfaces that Unity provides to low-level plugins.
    void subscribe_unity_events(IUnityInterfaces* unityInterfaces);
    
    /// @brief: Creates the second window and parents the Metal Kit View to it.
    /// @param screen The screen that the window will be shown on.
    void create_and_add_second_window(UIScreen* screen);
    
    /// @brief: Destroys and cleans up the Metal Kit View and the second window.
    void destroy_and_remove_second_window();
    
private:
    /// @brief: Handles when the XR display subsystem is initialized.
    /// @param subsystemHandle A handle to the Unity subsystem.
    /// @returns: A error code that indicates success or failure of the function.
    UnitySubsystemErrorCode on_display_subsystem_initialized(UnitySubsystemHandle subsystemHandle);

    /// @brief: Handles when the XR display subsystem is started.
    /// @param subsystemHandle A handle to the Unity subsystem.
    /// @returns: A error code that indicates success or failure of the function.
    UnitySubsystemErrorCode on_display_subsystem_started(UnitySubsystemHandle subsystemHandle);

    /// @brief: Handles when the XR display subsystem is stopped.
    /// @param subsystemHandle A handle to the Unity subsystem.
    void on_display_subsystem_stopped(UnitySubsystemHandle subsystemHandle);

    /// @brief: Handles when the XR display subsystem is shut down.
    /// @param subsystemHandle A handle to the Unity subsystem.
    void on_display_subsystem_shutdown(UnitySubsystemHandle subsystemHandle);

    /// @brief: Handles when graphics thread starts.
    /// @param subsystemHandle A handle to the Unity subsystem.
    /// @param renderingCaps The rendering capabilities.
    /// @returns: A error code that indicates success or failure of the function.
    /// @remarks This function runs on the Unity render thread, separate from the main thread.
    UnitySubsystemErrorCode start_in_graphics_thread(UnitySubsystemHandle subsystemHandle, UnityXRRenderingCapabilities* renderingCaps);

    /// @brief: Populates the description of the next XR frame.
    /// @param subsystemHandle A handle to the Unity subsystem.
    /// @param frameHints An object that describes how the XR frame should be composited. This helps inform choices made in this function.
    /// @param nextFrame An object that describes the XR next frame. This is meant to be populated by this function.
    /// @returns: A error code that indicates success or failure of the function.
    /// @remarks This function runs on the Unity render thread, separate from the main thread.
    UnitySubsystemErrorCode on_populate_next_frame_descriptor(UnitySubsystemHandle subsystemHandle,
                                                              const UnityXRFrameSetupHints* frameHints,
                                                              UnityXRNextFrameDesc* nextFrame);

    /// @brief: Populates the description of the mirror game view.
    /// @param subsystemHandle A handle to the Unity subsystem.
    /// @param blitInfo An object that describes the XR mirror view render target. This helps inform choices made in this function.
    /// @param blitDescriptor An object that describes the XR mirror view render target. This is meant to be populated by this function.
    /// @returns: A error code that indicates success or failure of the function.
    UnitySubsystemErrorCode on_populate_mirror_view_descriptor(UnitySubsystemHandle subsystemHandle,
                                                               const UnityXRMirrorViewBlitInfo* blitInfo,
                                                               UnityXRMirrorViewBlitDesc* blitDescriptor);

    /// @brief: Handles any render operations in addition to the usual Unity ones, such as submitting the current frames of the eye textures over to a 3rd party library.
    /// @param subsystemHandle A handle to the Unity subsystem.
    /// @returns: A error code that indicates success or failure of the function.
    /// @remarks This function runs on the Unity render thread, separate from the main thread.
    UnitySubsystemErrorCode on_submit_current_frame_in_graphics_thread(UnitySubsystemHandle subsystemHandle);
    
    /// @brief: Creates the Metal Kit View and adds it as a subview to the window provided.
    /// @param window The window that the Metal Kit View will be a child of.
    /// @remarks: If the Metal Kit View is parented to the main Unity window, then the image is drawn to the first screen.
    /// If it is parented to the window created when the display connects, then it is drawn to the second screen.
    void create_and_add_metalkitview_to_window(UIWindow* window);
    
    /// @brief: Subscribes to be notified of changes in the lifecycle of a XR display subsystem.
    void subscribe_to_lifecycle_notifications();
    
    /// @brief: Subscribes to be notified of changes in new hardware displays.
    void subscribe_to_screen_notifications();
    
    /// @brief: Creates the native textures and assigns them to the Unity texture representation.
    /// @param subsystemHandle A handle to the Unity subsystem.
    void create_textures(UnitySubsystemHandle subsystemHandle);
    
    /// @brief: An interface into the a logging/tracing system for XR.
    IUnityXRTrace* traceInterface;

    /// @brief: An interface into the Unity profiler.
    IUnityProfiler* profilingInterface;

    /// @brief: An interface into the Metal device that Unity creates.
    /// @remarks: Unlike graphicsInterface, which provides higher-level operations that all Unity graphics backends share, this provides operations specific to Metal.
    IUnityGraphicsMetalV1* metalInterface;

    /// @brief: An interface that allows developers to provide functionality for an XR Display subsystem.
    IUnityXRDisplayInterface* displayInterface;

    /// @brief: The resolution of the Unity screen.
    CGSize dimension;

#if SECOND_UI_SCREEN
    /// @brief A reference to the second window.
    UIWindow* secondWindow;
#endif

#if SECOND_UI_VIEW
    /// @brief A reference to the Metal Kit View that acts as a canvas.
    MTKView* metalKitView;
#endif

    /// @brief: Unity XR SDK currently requires that you pass in an I/O Surface pointer.
    /// @remarks: I/O Surfaces can be used to shared resources across multiple processes, making them more flexible. This is is why Unity uses them for iOS.
    /// When Unity is ready to draw its image, it will treat this I/O Surface as if it were the actual screen.
    IOSurfaceRef nativeColorRenderSurface;

    /// @brief: The Metal texture that we are using to interface into Metal operations.
    /// @remarks: This texture wraps around the I/O Surface.
    /// The I/O Surface acts as the "meat" of the texture, aka when you read and write colors to the Metal texture,
    /// you're actually reading and writing from the I/O Surface.
    id<MTLTexture> nativeColorRenderTexture;

    /// @brief: An ID that the XR SDK hands after you request that it create a XR Render Surface texture.
    /// @remarks: This is just a small piece of the system in XR SDK that helps keep track of native textures, so that when rendering is called upon, it can pass the i/O Surface to the parts of Unity that do the screen rendering, and the surface can act as a surrogate for the screen.
    UnityXRRenderTextureId unityColorRenderTextureId;

    /// @brief: A POSIX read/write thread lock for locking down resources shared by the main thread and the render thread.
    /// @remarks: These type of locks are specialized for when you have to read thread-shared a values very often but only change them once in a while.
    /// Which is what we need to do with the Metal Kit View - we create and set new one when the Ryz connects and destroy and set it when the Ryz disconnnects.
    pthread_rwlock_t lock;

    /// @brief: Attributes for the POSIX reader/writer lock.
    /// @remarks: The options set in this struct will be ignored by pthread_rwlock_init().
    pthread_rwlockattr_t lockAttribute;

    /// @brief: A value indicating whether the application is a Development build or not.
    bool isDevelopmentBuild;

    /// @brief: An object that describes the profiler sample for the measuring the method that populates next XR frame.
    const UnityProfilerMarkerDesc* onPopulateNextFrameDescriptorMarker;

    /// @brief: An object that describes the profiler sample for the measuring the method that populates the XR mirror view.
    const UnityProfilerMarkerDesc* onPopulateMirrorViewDescriptorMarker;

    /// @brief: An object that describes the profiler sample for the measuring the method that submits a frame.
    const UnityProfilerMarkerDesc* onSubmitCurrentFrameInGraphicsThreadMarker;
    
    /// @brief: An object that describes the profiler sample for the measuring the POSIX Read/Write locking.
    const UnityProfilerMarkerDesc* posixRWLockMarker;
    
    /// @brief: An object that describes the profiler sample for the measuring the POSIX Read/Write unlocking.
    const UnityProfilerMarkerDesc* posixRWUnlockMarker;
    
    /// @brief: An object that describes the profiler sample for the measuring ending the Metal encoder that Unity uses to render the game world.
    const UnityProfilerMarkerDesc* endUnityRenderEncoderMarker;
    
    /// @brief: An object that describes the profiler sample for the measuring ending the Metal encoder that Unity uses to render the game world.
    const UnityProfilerMarkerDesc* getCurrentCommandBufferMarker;
    
    /// @brief: An object that describes the profiler sample for the measuring the operation that blits the right eye texture to the Metal Kit View.
    const UnityProfilerMarkerDesc* blitCommandEncoderMarker;
    
    /// @brief: An object that describes the profiler sample for the measuring presenting the image in the Metal Kit View to the screen.
    const UnityProfilerMarkerDesc* presentDrawableMarker;
};

#endif
