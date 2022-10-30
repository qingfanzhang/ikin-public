//
//  ikin_ryz_displayer.mm
//  LowLevelNativePlugin
//
//  Created by Joel Barba on 10/8/19.
//  Copyright © 2019 Unity Technologies. All rights reserved.
//

#include "ikin_ryz_displayer.h"
#include <sstream>

#import <IOSurface/IOSurfaceRef.h>
#import <pthread.h>
#import <UIKit/UIKit.h>

#include "../External Headers/Unity/XR/Subsystems/Display/IUnityXRDisplay.h"
#include "../External Headers/Unity/UnityAppController.h"
#include "../External Headers/Unity/DisplayManager.h"
#include "native_to_unity_notifiers.h"
#import "DisplayConnectionNotifier.h"

#undef XR_TRACE

// A macro that allow logging/tracing to easily be stripped out. To include, PROFILE 1. To strip out, PROFILE 0
#if TRACE
#define XR_TRACE(...) traceInterface->Trace(kXRLogTypeLog, __VA_ARGS__)
#else
#define XR_TRACE(...)
#endif

// Macros that allow profile sampling to easily be stripped out. To include, PROFILE 1. To strip out, PROFILE 0
#if PROFILE
#define BEGIN_SAMPLE(identifier) if (isDevelopmentBuild) profilingInterface->BeginSample(identifier ## Marker)
#define END_SAMPLE(identifier) if (isDevelopmentBuild) profilingInterface->EndSample(identifier ## Marker)
#else
#define BEGIN_SAMPLE(identifier)
#define END_SAMPLE(identifier)
#endif

// Placed in an anonymous namespace to avoid these functions being accessed outside this file
namespace
{
    /// @brief: Describes a UnityXRRectf as a string.
    /// @param rect The rectangle.
    /// @returns: A formatted string describing the rectangle.
    const char* rect_description(const UnityXRRectf& rect)
    {
        std::stringstream stringStream;
        stringStream << "{pos: (" << rect.x << ", " << rect.y << 
            "), dim: (" << rect.width << ", " << rect.height << ")}";
        
        return stringStream.str().c_str();
    }

    /// @brief: Describes a CGSize as a string.
    /// @param size  The size.
    /// @returns: A formatted string describing the size.
    const char* size_description(const CGSize& size)
    {
        std::stringstream stringStream;
        stringStream << "(" << size.width << ", " << size.height << ")";

        return stringStream.str().c_str();
    }

    /// @brief: Creates a description of the pose, which is a position that is an offset of the camera.
    /// @returns: The description of the pose.
    UnityXRPose get_pose()
    {
        UnityXRPose pose = { 0 };
        
        pose.position.x = 0.0;
        pose.position.z = 0.0f;
        pose.rotation.w = 1.0f;
        
        return pose;
    }

    /// @brief: Creates a description of the projection matrix.
    /// @returns: The description of the projection matrix.
    UnityXRProjection get_projection(int targetEye, const CGSize& dimension)
    {
        UnityXRProjection ret;

        ret.type = projectionType;

        if (ret.type == kUnityXRProjectionTypeMatrix)
        {
            ret.data.matrix = targetEye == 0 ? leftProjectionMatrix : rightProjectionMatrix;
        }
        else
        {
            float aspectRatio = (dimension.width * 0.5f) / dimension.height;

            ret.data.halfAngles.left = -aspectRatio;
            ret.data.halfAngles.right = aspectRatio;
            ret.data.halfAngles.top = 0.5;
            ret.data.halfAngles.bottom = -0.5;
        }
        
        return ret;
    }

    /// @brief: A singleton reference to the DisplayConnectionNotifier
    static ikin_ryz_displayer ryzDisplayer;

    /// @brief: A reference to the object that registers to display connection events.
    static DisplayConnectionNotifier* displayConnectionNotifier;
}

/// @brief: Lazily initializes a static instance of this class.
/// @returns: The static instance of this class.
ikin_ryz_displayer* ikin_ryz_displayer::get_instance()
{
    // Return a reference to the DisplayConnectionNotifier.
    return &ryzDisplayer;
}

/// @brief: Initializes an instance of this class.
/// @param unityInterfaces A registry of the low-level interfaces that Unity provides to low-level plugins.
void ikin_ryz_displayer::subscribe_unity_events(IUnityInterfaces* unityInterfaces)
{
    // Request a reference to the interface Unity provides to expose XR tracing (logging).
    traceInterface = unityInterfaces->Get<IUnityXRTrace>();
    
    // Request a reference to the interface Unity provides to expose the actual graphics device details.
    profilingInterface = unityInterfaces->Get<IUnityProfiler>();
    
    // Request a reference to the interface Unity provides to expose the actual graphics device details.
    metalInterface = unityInterfaces->Get<IUnityGraphicsMetalV1>();
    
    // Request a reference to the interface Unity provides to expose XR display.
    displayInterface = unityInterfaces->Get<IUnityXRDisplayInterface>();
    
    // If the profiler exists, then:
    if (profilingInterface != nullptr)
    {
        // Development build indicator is dependent on whether the profiler is available or not.
        isDevelopmentBuild = profilingInterface->IsAvailable() != 0;
        
        // Set up the profile samples
        profilingInterface->CreateMarker(&onPopulateNextFrameDescriptorMarker, "OnPopulateNextFrameDescriptor", kUnityProfilerCategoryOther, kUnityProfilerMarkerFlagDefault, 0);
        
        profilingInterface->CreateMarker(&onPopulateMirrorViewDescriptorMarker, "OnPopulateMirrorViewDescriptor", kUnityProfilerCategoryOther, kUnityProfilerMarkerFlagDefault, 0);
        
        profilingInterface->CreateMarker(&onSubmitCurrentFrameInGraphicsThreadMarker, "OnSubmitCurrentFrameInGraphicsThread", kUnityProfilerCategoryOther, kUnityProfilerMarkerFlagDefault, 0);
        
        profilingInterface->CreateMarker(&posixRWLockMarker, "Posix Read/Write Lock", kUnityProfilerCategoryOverhead, kUnityProfilerMarkerFlagDefault, 0);
        
        profilingInterface->CreateMarker(&posixRWUnlockMarker, "Posix Read/Write Unlock", kUnityProfilerCategoryOverhead, kUnityProfilerMarkerFlagDefault, 0);
        
        profilingInterface->CreateMarker(&endUnityRenderEncoderMarker, "End Unity Render Encoder", kUnityProfilerCategoryRender, kUnityProfilerMarkerFlagDefault, 0);
        
        profilingInterface->CreateMarker(&getCurrentCommandBufferMarker, "Get Current Command Buffer", kUnityProfilerCategoryRender, kUnityProfilerMarkerFlagDefault, 0);
        
        profilingInterface->CreateMarker(&blitCommandEncoderMarker, "Blit Command Encoding", kUnityProfilerCategoryRender, kUnityProfilerMarkerFlagDefault, 0);
        
        profilingInterface->CreateMarker(&presentDrawableMarker, "Present Drawable", kUnityProfilerCategoryRender, kUnityProfilerMarkerFlagDefault, 0);
    }
    else
    {
        // Otherwise, if no profiler exists, default the profile values.
        isDevelopmentBuild = false;
        onPopulateNextFrameDescriptorMarker = nullptr;
        onPopulateMirrorViewDescriptorMarker = nullptr;
        onSubmitCurrentFrameInGraphicsThreadMarker = nullptr;
        posixRWLockMarker = nullptr;
        posixRWUnlockMarker = nullptr;
        endUnityRenderEncoderMarker = nullptr;
        getCurrentCommandBufferMarker = nullptr;
        blitCommandEncoderMarker = nullptr;
        presentDrawableMarker = nullptr;
    }
    
    // Create the read/write lock.
    if (pthread_rwlockattr_init(&lockAttribute) != 0)
    {
        traceInterface->Trace(kXRLogTypeLog, "Failed to initialize Read/Write lock attributes.\n");
    }
    
    lock = PTHREAD_RWLOCK_INITIALIZER;
    if (pthread_rwlock_init(&lock, &lockAttribute) != 0)
    {
        traceInterface->Trace(kXRLogTypeLog, "Failed to initialize Read/Write lock.\n");
    }
    
    // Get a reference to the application controller.
    UnityAppController* unityAppController = GetAppController();

    // Set the dimension of the second screen to.
    dimension = [[unityAppController mainDisplay] screenSize];
    
#if TRACE
    std::stringstream stringStream;
    stringStream << "Main display screen size " << size_description(dimension);
    XR_TRACE(stringStream.str().c_str());
#endif
    
#if SECOND_UI_SCREEN
    // Subscribe to notifications of changes in new hardware displays.
    subscribe_to_screen_notifications();
#elif SECOND_UI_VIEW

    // Get a reference to the UI window.
    UIWindow* mainUnityWindow = [unityAppController window];
    
    // if we are creating a second view but there is no second screen, create a Metal Kit View that is parented to masin screen.
    create_and_add_metalkitview_to_window(mainUnityWindow);
#endif
    
    // Subscribe to notifications of changes in the lifecycle of a XR display subsystem.
    subscribe_to_lifecycle_notifications();
}

#if SECOND_UI_VIEW
/// @brief: Creates the Metal Kit View and adds it as a subview to the window provided.
/// @param window The window that the Metal Kit View will be a child of.
/// @remarks: If the Metal Kit View is parented to the main Unity window, then the image is drawn to the first screen.
/// If it is parented to the window created when the display connects, then it is drawn to the second screen.
void ikin_ryz_displayer::create_and_add_metalkitview_to_window(UIWindow* window)
{
    // Lock the usage of the Metal Kit View.
    if (pthread_rwlock_trywrlock(&lock) != 0)
    {
        XR_TRACE("Failed to lock Read/Write lock\n");
    }
    
    // Get a reference to the metal device.
    id<MTLDevice> device = metalInterface->MetalDevice();

    // Create a Metal Kit UI View, and make match the window bounds. Tell it which
    metalKitView = [[MTKView alloc] initWithFrame : window.bounds
                                           device : device];
    
    // Get the FPS of the Metal Kit View to match the one coming from Unity.
    NSInteger framesPerSecond = GetAppController().unityDisplayLink.preferredFramesPerSecond;
    [metalKitView setPreferredFramesPerSecond : framesPerSecond];
    
    // Set the view’s autoresizing mask so that it is not translated into Auto Layout constraints.
    metalKitView.translatesAutoresizingMaskIntoConstraints = false;
    
    // Set the size of the drawable texture to match the window bounds.
    [metalKitView setDrawableSize : window.bounds.size];
    
    // Notify the Metal Kit View that the frame buffer isn't just read-only.
    metalKitView.framebufferOnly = NO;
    
    // Add this as a sub-view of the window.
    [window addSubview : metalKitView];
    [window sizeToFit];

    // Unlock usage of the Metal Kit View.
    if (pthread_rwlock_unlock(&lock) != 0)
    {
        XR_TRACE("Failed to unlock Read/Write lock\n");
    }
}
#endif

/// @brief: Subscribes to be notified of changes in the lifecycle of a XR display subsystem.
void ikin_ryz_displayer::subscribe_to_lifecycle_notifications()
{
    // Creates a structure that acts as a handler.
    // This takes pointers to C-style functions, so we can't directly pass in an Objective C method and expect it to be "called".
    // So, we will be passing in wrapper functions, and those functions will call the actual methods.
    UnityLifecycleProvider subsystemLifecycleProvider;
    subsystemLifecycleProvider.userData = this;
    subsystemLifecycleProvider.Initialize = [](UnitySubsystemHandle subsystemHandle, void* voidPtr) -> UnitySubsystemErrorCode
    {
        // Convert the void pointer into a DisplayConnectionNotifier.
        ikin_ryz_displayer* ryzDisplayer = (ikin_ryz_displayer*)voidPtr;

        return ryzDisplayer->on_display_subsystem_initialized(subsystemHandle);
    };

    subsystemLifecycleProvider.Start = [](UnitySubsystemHandle subsystemHandle, void* voidPtr) -> UnitySubsystemErrorCode
    {
        // Convert the void pointer into a DisplayConnectionNotifier.
        ikin_ryz_displayer* ryzDisplayer = (ikin_ryz_displayer*)voidPtr;

        return ryzDisplayer->on_display_subsystem_started(subsystemHandle);
    };

    subsystemLifecycleProvider.Stop = [](UnitySubsystemHandle subsystemHandle, void* voidPtr) -> void
    {
        // Convert the void pointer into a DisplayConnectionNotifier.
        ikin_ryz_displayer* ryzDisplayer = (ikin_ryz_displayer*)voidPtr;

        return ryzDisplayer->on_display_subsystem_stopped(subsystemHandle);
    };

    subsystemLifecycleProvider.Shutdown = [](UnitySubsystemHandle subsystemHandle, void* voidPtr) -> void
    {
        // Convert the void pointer into a DisplayConnectionNotifier.
        ikin_ryz_displayer* ryzDisplayer = (ikin_ryz_displayer*)voidPtr;

        return ryzDisplayer->on_display_subsystem_shutdown(subsystemHandle);
    };
    
    // Register the handlers with the display interface that Unity has exposed.
    UnitySubsystemErrorCode result = displayInterface->RegisterLifecycleProvider("iKinRyz", "libiKinRyz-Display", &subsystemLifecycleProvider);

    if (result != kUnitySubsystemErrorCodeSuccess)
    {
        XR_TRACE("Unable to register input lifecyle provider: [%i]\n", result);

    }
}

#if SECOND_UI_SCREEN
/// @brief: Creates the second window and parents the Metal Kit View to it.
/// @param screen The screen that the window will be shown on.
void ikin_ryz_displayer::create_and_add_second_window(UIScreen* screen)
{
    // Request the bounds of the new screen.
    CGRect screenBounds = screen.bounds;
    
    // If there is no second window already allocated, then:
    if (secondWindow == nil)
    {
        // Request that a window be created that fills the entire screen bounds.
        secondWindow = [[UIWindow alloc] initWithFrame : screenBounds];
        
        // Set this surface to be on the new screen.
        secondWindow.screen = screen;
        
#if SECOND_UI_VIEW
        // if we are creating a second view but there is no second screen, create a Metal Kit View that is parented to masin screen.
        create_and_add_metalkitview_to_window(secondWindow);
#endif
        
        // Request that the second window become the key window.
        // This also positions it in front of all other windows at the same level or lower.
        [secondWindow makeKeyAndVisible];
    }
    
    // Notify the C# layer than the display has been connected.
    ikinRyzOnDisplayEvent(display_event::connected);
}

/// @brief: Destroys and cleans up the Metal Kit View and the second window.
void ikin_ryz_displayer::destroy_and_remove_second_window()
{
    // Notify the C# layer than the display has been disconnected.
    ikinRyzOnDisplayEvent(display_event::disconnected);
    
#if SECOND_UI_VIEW
    // Lock the usage of the Metal Kit View.
    if (pthread_rwlock_trywrlock(&lock) != 0)
    {
        XR_TRACE("Failed to lock Read/Write lock\n");
    }

    if (metalKitView != nil)
    {

        [metalKitView releaseDrawables];
        
        [metalKitView removeFromSuperview];

        metalKitView = nil;
    }
    
    // Unlock the usage of the Metal Kit View.
    if (pthread_rwlock_unlock(&lock) != 0)
    {
        XR_TRACE("Failed to unlock Read/Write lock\n");
    }
#endif
    
    if (secondWindow != nil)
    {
        // Hide the window.
        secondWindow.hidden = YES;
        
        [secondWindow removeFromSuperview];
        
        // Release the window.
        secondWindow = nil;
    }
}

 /// @brief: Subscribes to be notified of changes in new hardware displays.
 void ikin_ryz_displayer::subscribe_to_screen_notifications()
 {
     // Request the list of screens that the application starts with.
     NSArray<UIScreen*>* startingScreens = [UIScreen screens];
     
     // If there are already two screens at the startarts, then:
     if ([startingScreens count] == 2)
     {
         // Setup the additional display right now.
         create_and_add_second_window(startingScreens[1]);
     }
     
     // Register for screen notifications
     NSNotificationCenter* notificationCenter = [NSNotificationCenter defaultCenter];
 
     displayConnectionNotifier = [[DisplayConnectionNotifier alloc] initWith : this];
     
     // Subscribe to be notified when a display is connected.
     [notificationCenter addObserver : displayConnectionNotifier
                            selector : @selector(handleDisplayConnect:)
                                name : UIScreenDidConnectNotification
                              object : nil];
 
     // Subscribe to be notified when a display is disconnected.
     [notificationCenter addObserver : displayConnectionNotifier
                            selector : @selector(handleDisplayDisconnect:)
                                name : UIScreenDidDisconnectNotification
                              object : nil];
}
#endif

/// @brief: Handles when the XR display subsystem is initialized.
/// @param subsystemHandle A handle to the Unity subsystem.
/// @returns: A error code that indicates success or failure of the function.
UnitySubsystemErrorCode ikin_ryz_displayer::on_display_subsystem_initialized(UnitySubsystemHandle subsystemHandle)
{
    XR_TRACE("A display's subsystem has been initialized!\n");
    XR_TRACE([[[NSThread currentThread] description] UTF8String]);
    
    // Create a structure to contain the graphics thread handlers.
    UnityXRDisplayGraphicsThreadProvider graphicsThreadProvider;
    graphicsThreadProvider.userData = this;
    graphicsThreadProvider.Start = [](UnitySubsystemHandle subsystemHandle,
                                      void* voidPtr,
                                      UnityXRRenderingCapabilities * renderingCaps)
    {
        // Convert the void pointer into a DisplayConnectionNotifier.
        ikin_ryz_displayer* ryzDisplayer = (ikin_ryz_displayer*)voidPtr;

        return ryzDisplayer->start_in_graphics_thread(subsystemHandle, renderingCaps);
    };

    graphicsThreadProvider.Stop = nullptr;
    graphicsThreadProvider.PopulateNextFrameDesc = [](UnitySubsystemHandle subsystemHandle,
                                                      void* voidPtr,
                                                      const UnityXRFrameSetupHints* frameHints,
                                                      UnityXRNextFrameDesc * nextFrame) -> UnitySubsystemErrorCode
    {
        // Convert the void pointer into a DisplayConnectionNotifier.
        ikin_ryz_displayer* ryzDisplayer = (ikin_ryz_displayer*)voidPtr;

        return ryzDisplayer->on_populate_next_frame_descriptor(subsystemHandle, frameHints, nextFrame);
    };

    graphicsThreadProvider.BlitToMirrorViewRenderTarget = nullptr;

    graphicsThreadProvider.SubmitCurrentFrame = [](UnitySubsystemHandle subsystemHandle, void* voidPtr) -> UnitySubsystemErrorCode
    {
        // Convert the void pointer into a DisplayConnectionNotifier.
        ikin_ryz_displayer* ryzDisplayer = (ikin_ryz_displayer*)voidPtr;

        return ryzDisplayer->on_submit_current_frame_in_graphics_thread(subsystemHandle);
    };

    // Register for callbacks on the graphics thread.
    displayInterface->RegisterProviderForGraphicsThread(subsystemHandle, &graphicsThreadProvider);

    // Register for callbacks on the XR display. This acts as a wrapper for the ExampleDisplayProvider instance.
    UnityXRDisplayProvider displayProvider;
    displayProvider.userData = this;
    displayProvider.QueryMirrorViewBlitDesc = [](UnitySubsystemHandle subsystemHandle,
                                                   void* voidPtr,
                                                   const UnityXRMirrorViewBlitInfo* mirrorRtDesc,
                                                   UnityXRMirrorViewBlitDesc * blitDescriptor) -> UnitySubsystemErrorCode
    {
        // Convert the void pointer into a DisplayConnectionNotifier.
        ikin_ryz_displayer* ryzDisplayer = (ikin_ryz_displayer*)voidPtr;

        return ryzDisplayer->on_populate_mirror_view_descriptor(subsystemHandle, mirrorRtDesc, blitDescriptor);
    };

    displayProvider.UpdateDisplayState = nullptr;

    displayInterface->RegisterProvider(subsystemHandle, &displayProvider);
    
    return kUnitySubsystemErrorCodeSuccess;
}

/// @brief: Creates the native textures and assigns them to the Unity texture representation.
/// @param subsystemHandle A handle to the Unity subsystem.
void ikin_ryz_displayer::create_textures(UnitySubsystemHandle subsystemHandle)
{
    // This texture is twice as wide as the original resolution so that it can store left eye and right eye at the dimension of a full screen each.
    const int width = (int)(dimension.width * 2);
    const int height = (int)(dimension.height);
    
    // Get a reference to the metal device.
    id<MTLDevice> device = metalInterface->MetalDevice();
    
    // Create an object that describes a render texture to the Metal.
    // When the Metal needs to create the render texture, this should have all the information the GPU needs to generate and store it in GPU RAM.
    MTLTextureDescriptor* nativeColorRenderTextureDescriptor = [MTLTextureDescriptor texture2DDescriptorWithPixelFormat : MTLPixelFormatBGRA8Unorm
                                                                                                                  width : width
                                                                                                                 height : height
                                                                                                              mipmapped : NO];
    nativeColorRenderTextureDescriptor.storageMode = MTLStorageModePrivate;
    nativeColorRenderTextureDescriptor.usage = MTLTextureUsageRenderTarget | MTLTextureUsageShaderRead;
    nativeColorRenderTextureDescriptor.arrayLength = 1;
    
    XR_TRACE("Created the native color buffer descriptor.\n");
    
    const NSInteger PixelByteSize = 4;
    
    NSDictionary *surfaceDefinition = @{
                                        (id)kIOSurfaceWidth: @(nativeColorRenderTextureDescriptor.width),
                                        (id)kIOSurfaceHeight: @(nativeColorRenderTextureDescriptor.height),
                                        (id)kIOSurfaceBytesPerElement: @(PixelByteSize),
                                        };
    
    XR_TRACE("Created the definition for the I/O surfaces.\n");
    
    nativeColorRenderSurface = IOSurfaceCreate((CFDictionaryRef) surfaceDefinition);
    
    // This operation is only available in iOS 11 or later. Check it and make sure its available. If it is, then:
    if (@available(iOS 11.0, *))
    {
        // Create a Metal texture that is backed by the I/O Surface.
        nativeColorRenderTexture = [device newTextureWithDescriptor : nativeColorRenderTextureDescriptor iosurface : nativeColorRenderSurface plane : 0];
    }
    else
    {
        // Otherwise, throw a warning that we couldn't make a texture backed by an I/O surface.
        XR_TRACE("Did not create Metal texture with I/O Surface backing.\n");
    }
    
    // Create an object that describes a render texture to the Unity XR SDK. When the XR system needs to use the render texture, this should have all the information needed.
    UnityXRRenderTextureDesc unityRenderTextureDescriptor;
    memset(&unityRenderTextureDescriptor, 0, sizeof(UnityXRRenderTextureDesc));
    
    // Set the width, height, and texture array length of the Unity texture.
    unityRenderTextureDescriptor.flags = kUnityXRRenderTextureFlagsUVDirectionTopToBottom;
    unityRenderTextureDescriptor.width = width;
    unityRenderTextureDescriptor.height = height;
    unityRenderTextureDescriptor.textureArrayLength = 1;
    unityRenderTextureDescriptor.colorFormat = kUnityXRRenderTextureFormatBGRA32;
    
    // Tell Unity to create a texture on the Unity side using the description from the descriptor.
    // Since we passed the native pointer over in this descriptor, this is how Unity knows that is should be rendering everything to this particular buffer instead of to the screen.
    unityRenderTextureDescriptor.color.nativePtr = nativeColorRenderSurface;
    displayInterface->CreateTexture(subsystemHandle, &unityRenderTextureDescriptor, &unityColorRenderTextureId);
}

// @brief: Handles when graphics thread starts.
/// @param subsystemHandle A handle to the Unity subsystem.
/// @param renderingCaps The rendering capabilities.
/// @returns: A error code that indicates success or failure of the function.
/// @remarks This function runs on the Unity render thread, separate from the main thread.
UnitySubsystemErrorCode ikin_ryz_displayer::start_in_graphics_thread(UnitySubsystemHandle subsystemHandle, UnityXRRenderingCapabilities *renderingCaps)
{
    create_textures(subsystemHandle);

    return kUnitySubsystemErrorCodeSuccess;
}

/// @brief: Handles when the XR display subsystem is started.
/// @param subsystemHandle A handle to the Unity subsystem.
/// @returns: A error code that indicates success or failure of the function.
UnitySubsystemErrorCode ikin_ryz_displayer::on_display_subsystem_started(UnitySubsystemHandle subsystemHandle)
{
    XR_TRACE("A display's subsystem has been started!\n");
    XR_TRACE([[[NSThread currentThread] description] UTF8String]);

    return kUnitySubsystemErrorCodeSuccess;
}

/// @brief: Handles when the XR display subsystem is stopped.
/// @param subsystemHandle A handle to the Unity subsystem.
void ikin_ryz_displayer::on_display_subsystem_stopped(UnitySubsystemHandle subsystemHandle)
{
    XR_TRACE("A display's subsystem has been stopped!\n");
    XR_TRACE([[[NSThread currentThread] description] UTF8String]);
}

/// @brief: Handles when the XR display subsystem is shut down.
/// @param subsystemHandle A handle to the Unity subsystem.
void ikin_ryz_displayer::on_display_subsystem_shutdown(UnitySubsystemHandle subsystemHandle)
{
    XR_TRACE("A display's subsystem has been shutdown!\n");
    XR_TRACE([[[NSThread currentThread] description] UTF8String]);
}

/// @brief: Populates the description of the next XR frame.
/// @param subsystemHandle A handle to the Unity subsystem.
/// @param frameHints An object that describes how the XR frame should be composited. This helps inform choices made in this function.
/// @param nextFrame An object that describes the XR next frame. This is meant to be populated by this function.
/// @returns: A error code that indicates success or failure of the function.
/// @remarks This function runs on the Unity render thread, separate from the main thread.
UnitySubsystemErrorCode ikin_ryz_displayer::on_populate_next_frame_descriptor(UnitySubsystemHandle subsystemHandle,
                                                                                       const UnityXRFrameSetupHints* frameHints,
                                                                                       UnityXRNextFrameDesc* nextFrame)
{
    XR_TRACE("Handling population of next frame descriptor.\n");
    XR_TRACE([[[NSThread currentThread] description] UTF8String]);

    BEGIN_SAMPLE(onPopulateNextFrameDescriptor);
    
    
#if TRACE
    {
        std::stringstream stringStream;
        stringStream << "Texture Resolution Scale: " << frameHints->appSetup.textureResolutionScale << "\n";
        XR_TRACE(stringStream.str().c_str());
    }
#endif

    // If the frame hint should not use single pass rendering, then:
    if (/* DISABLES CODE */ (false))
    {
        XR_TRACE("Performing multi pass rendering.\n");
        
        // Use multi-pass rendering to render.

        // Can increase render pass count to do wide FOV or to have a separate view into scene.
        nextFrame->renderPassesCount = 2;

#if TRACE
        {
            std::stringstream stringStream;
            stringStream << "Number of render passes: " << nextFrame->renderPassesCount << "\n";
            XR_TRACE(stringStream.str().c_str());
        }
#endif

        // For each pass in the render passes, do the following:
        for (int pass = 0; pass < nextFrame->renderPassesCount; ++pass)
        {
            // Retrieve the render pass.
            auto& renderPass = nextFrame->renderPasses[pass];

            XR_TRACE("The render pass has a texture that is the same for both eyes.\n");

            // They will be drawn side by side, so just use the one Unity texture for both render passes.
            renderPass.textureId = unityColorRenderTextureId;

            // For this pass there is one set of render params.
            renderPass.renderParamsCount = 1;

            // Note: culling is shared between multiple passes by setting this to the same index.
            renderPass.cullingPassIndex = pass;

            // Get the culling pass.
            auto& cullingPass = nextFrame->cullingPasses[pass];
            
            // Fill out the culling pass' separation.
            cullingPass.separation = 0.0;

            // Fill out render params. View, projection, viewport for pass.
            auto& renderParams = renderPass.renderParams[0];

            // Set the pose for each pass.
            renderParams.deviceAnchorToEyePose = cullingPass.deviceAnchorToCullingPose = get_pose();

            // Set the projection matrix for each pass.
            renderParams.projection = cullingPass.projection = get_projection(pass, dimension);

            XR_TRACE("Viewport for each eye is in the same texture, so the viewport is in different sections of the texture.\n");

            XR_TRACE(rect_description(frameHints->appSetup.renderViewport));
            
            renderParams.viewportRect = {
                pass == 0 ? 0.0f : 0.5f,    // x
                0.0f,                       // y
                0.5f,                       // width
                1.0f                        // height
            };
        }
    }
    else
    {
        XR_TRACE("Performing single-pass rendering.\n");

        // Otherwise, they are using single pass rendering.

        // Example of using single-pass stereo to combine the first two render passes.
        nextFrame->renderPassesCount = 1;

#if TRACE
        {
            std::stringstream stringStream;
            stringStream << "Number of render passes: " << nextFrame->renderPassesCount << "\n";
            XR_TRACE(stringStream.str().c_str());
        }
#endif

        UnityXRNextFrameDesc::UnityXRRenderPass& renderPass = nextFrame->renderPasses[0];

        // Texture that unity will render to next frame.  We created it above.
        // You might want to change this dynamically to double / triple buffer.
        renderPass.textureId = unityColorRenderTextureId;

        // Two sets of render params for first pass, view / projection for each eye.  Fill them out next.
        renderPass.renderParamsCount = 2;

        for (int eye = 0; eye < renderPass.renderParamsCount; ++eye)
        {
            UnityXRNextFrameDesc::UnityXRRenderPass::UnityXRRenderParams& renderParams = renderPass.renderParams[eye];

            renderParams.deviceAnchorToEyePose = get_pose();
            renderParams.projection = get_projection(eye, dimension);

#if TRACE
            {
                std::stringstream stringStream;
                stringStream << "Viewport for eye " << eye << "shares in the same texture for all eyes, so the viewport is in different sections of the texture.\n";
                XR_TRACE(stringStream.str().c_str());
            }
#endif
            
            XR_TRACE(rect_description(renderParams.viewportRect));
            XR_TRACE(rect_description(frameHints->appSetup.renderViewport));
            
            renderParams.viewportRect =
            {
                eye == 0 ? 0.0f : 0.5f, // x
                0.0f,                   // y
                0.5f,                   // width
                1.0f                    // height
            };
        }

        renderPass.cullingPassIndex = 0;
        UnityXRNextFrameDesc::UnityXRCullingPass& cullingPass = nextFrame->cullingPasses[0];
        cullingPass.deviceAnchorToCullingPose = get_pose();
        cullingPass.projection = get_projection(0, dimension);
        cullingPass.separation = 0.625f;
    }

    END_SAMPLE(onPopulateNextFrameDescriptor);
    
    return kUnitySubsystemErrorCodeSuccess;
}

/// @brief: Populates the description of the mirror game view.
/// @param subsystemHandle A handle to the Unity subsystem.
/// @param blitInfo An object that describes the XR mirror view render target. This helps inform choices made in this function.
/// @param blitDescriptor An object that describes the XR mirror view render target. This is meant to be populated by this function.
/// @returns: A error code that indicates success or failure of the function.
UnitySubsystemErrorCode ikin_ryz_displayer::on_populate_mirror_view_descriptor(UnitySubsystemHandle subsystemHandle,
                                                                                        const UnityXRMirrorViewBlitInfo* blitInfo,
                                                                                        UnityXRMirrorViewBlitDesc* blitDescriptor)
{
    XR_TRACE("Handling population of mirror view descriptor.\n");
    XR_TRACE([[[NSThread currentThread] description] UTF8String]);

    // If we are not displaying the left eye, then:
    if (blitInfo->mirrorBlitMode != kUnityXRMirrorBlitLeftEye)
    {
        // Do not map the texture to any area on the main screen.
        return kUnitySubsystemErrorCodeFailure;
    }
    
    BEGIN_SAMPLE(onPopulateMirrorViewDescriptor);
    
    blitDescriptor->nativeBlitAvailable = true;
    blitDescriptor->nativeBlitInvalidStates = false;

    // Describe that we are only going to be performing one blit.
    blitDescriptor->blitParamsCount = 1;

    // Get a reference to the object that describes out blit.
    UnityXRMirrorViewBlitDesc::UnityXRBlitParams& blitParam = blitDescriptor->blitParams[0];

    // Define which texture is the one we are blitting from.
    blitParam.srcTexId = unityColorRenderTextureId;

    blitParam.srcTexArraySlice = 0;

    // Define the homogeneous region that the blit is reading from.
    blitParam.srcRect =
    {
        0.0f,
        0.0f,
        0.5f,
        1.0f
    };

    // Define the homogeneous region that the blit is writing from.
    blitParam.destRect = {
        0.0f,
        0.0f,
        1.0f,
        1.0f
    };

    END_SAMPLE(onPopulateMirrorViewDescriptor);

    return kUnitySubsystemErrorCodeSuccess;
}

/// @brief: Handles any render operations in addition to the usual Unity ones, such as submitting the current frames of the eye textures over to a 3rd party library.
/// @param subsystemHandle A handle to the Unity subsystem.
/// @returns: A error code that indicates success or failure of the function.
/// @remarks This function runs on the Unity render thread, separate from the main thread.
UnitySubsystemErrorCode ikin_ryz_displayer::on_submit_current_frame_in_graphics_thread(UnitySubsystemHandle subsystemHandle)
{
    XR_TRACE("Handling submitting current frame in graphics thread.\n");

    XR_TRACE([[[NSThread currentThread] description] UTF8String]);

    BEGIN_SAMPLE(onSubmitCurrentFrameInGraphicsThread);

    BEGIN_SAMPLE(posixRWLock);
    
    // Lock the usage of the Metal Kit View.
    if (pthread_rwlock_trywrlock(&lock) != 0)
    {
        XR_TRACE("Failed to lock Read/Write lock\n");
    }
    
    END_SAMPLE(posixRWLock);
    
#if SECOND_UI_SCREEN
    if (metalKitView != nil)
    {
#endif
        
    #if SECOND_UI_VIEW
        
        BEGIN_SAMPLE(endUnityRenderEncoder);
        
        metalInterface->EndCurrentCommandEncoder();
        
        END_SAMPLE(endUnityRenderEncoder);
        
        BEGIN_SAMPLE(getCurrentCommandBuffer);
        
        // Create a new command buffer for each render pass to the current drawable.
        __unsafe_unretained id<MTLCommandBuffer> commandBuffer = metalInterface->CurrentCommandBuffer();
        
        END_SAMPLE(getCurrentCommandBuffer);
         
        // The source texture of the blit is the left eye texture.
        __unsafe_unretained id<MTLTexture> sourceRenderTexture = nativeColorRenderTexture;
        
        // Adding an auto-release pool here to free-up the blit encoder and the drawable
        @autoreleasepool
        {
            BEGIN_SAMPLE(blitCommandEncoder);
//
            // Request the current command buffer from the Metal interface. Request the command encoder for blitting.
            id<MTLBlitCommandEncoder> blitEncoder = [commandBuffer blitCommandEncoder];
//
#if SECOND_UI_SCREEN
            // These may not be ready or free due to the fact that Metal Kit View is created in a different thread.
            if (metalKitView.currentDrawable != nil && metalKitView.currentDrawable.texture != nil)
            {
#endif
//
                NSInteger width = sourceRenderTexture.width / 2;
                // Use the blit command encoder to copy the texture from the source to the destination texture.
                [blitEncoder copyFromTexture : sourceRenderTexture
                                 sourceSlice : 0
                                 sourceLevel : 0
                                sourceOrigin : MTLOriginMake(width, 0, 0)
                                  sourceSize : MTLSizeMake(width, sourceRenderTexture.height, 1)
                                   toTexture : metalKitView.currentDrawable.texture
                            destinationSlice : 0
                            destinationLevel : 0
                           destinationOrigin : MTLOriginMake(0, 0, 0)];
//
                XR_TRACE("Blitting source texture to the destination texture.\n");
                
#if SECOND_UI_SCREEN
            }
#endif
//
            // End the encoding of the bit encoder.
            [blitEncoder endEncoding];
//
            END_SAMPLE(blitCommandEncoder);
         
            // These may not be ready or free due to the fact that Metal Kit View is created in a different thread.
            if (metalKitView.currentDrawable != nil)
            {
                BEGIN_SAMPLE(presentDrawable);
//
                // Schedule a presention once the framebuffer is complete using the current drawable.
                [commandBuffer presentDrawable : metalKitView.currentDrawable];
//
                END_SAMPLE(presentDrawable);
            }
        
            XR_TRACE("Presenting drawable surface to the screen.\n");
            
        } // end of auto-release pool
    #endif
        
#if SECOND_UI_SCREEN
    }
#endif
    
    BEGIN_SAMPLE(posixRWUnlock);
    
    // Unlock the usage of the Metal Kit View.
    if (pthread_rwlock_unlock(&lock) != 0)
    {
        XR_TRACE("Failed to unlock Read/Write lock\n");
    }
    
    END_SAMPLE(posixRWUnlock);
    
    END_SAMPLE(onSubmitCurrentFrameInGraphicsThread);

    return kUnitySubsystemErrorCodeSuccess;
}

#undef XR_TRACE
#undef BEGIN_SAMPLE
#undef END_SAMPLE
