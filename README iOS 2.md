# Developer Notes

## High-level Overview :
This package creates a texture that is twice as wide as a regular phone screen.
It is twice as wide so that it can draw both the phone screen and the iKin display.
All cameras that are designated for left eye will end up on the phone screen, but before it is drawn to the screen it is drawn on the leftmost side of this texture.
All cameras that are designated for right eye will be drawn to the Ryz display, but before it is drawn to the screen it is drawn on the rightmost side of this texture.
The texture is just a buffer to hold the images until they can be displayed on the correct hardware.
After they are drawn to this texture, we arrange the left half of the texture to be drawn to the screen and if the Ryz is connected, the right half of the texture is drawn to a second screen.
We are using Metal Graphics API to accomplish this.

## Low-Level Overview:
Most of the code provided exists in an Xcode project called `LowLevelPluginiOS`. It compiles into an Objective-C++ static library. We have to use Objective C++, which marries C, C++, and Objective-C together because most of the Unity interfaces are assuming C++, but some of our iOS/Metal functionality needs Objective-C.

Also, we are making use of our *newly announced* XR SDK.

### Entry points and code flow
The main entry point in the static library is given a pointer to `UnityInterfaces` object. This is a registry of items that Unity can expose to native plugins. For some of these interfaces, the expectation is that Unity is providing functionality to the developers. For others, the expectation is that you are providing the details for how it will work.
The ones that this plugin makes use of:
- The XR trace interface, which you can use to log to console.
- The Profiler interface, which we can use to add samples to the Unity profiler.
- The Metal interface, which gives us access to the Metal Device for Metal use with Metal API calls.
- The XR Display subsystem, which has callbacks to our implementations. We define how the subsystem works through here.

There are three aspects in the XR Display Subsystem that we need to implement - a Lifecycle provider, a Display provider, and the Graphics thread provider.

#### The Lifecycle provider -
This provides callbacks into the kind of events the subsystem will do throughout its life. When the plugin is loaded in, the Initialize event is handled. If the application is paused (the phone locks, or the user switches to another app), the Start and Stop events are handled. When the app is killed, the Shutdown event is called. The only function that actually does anything here is Initialize, and it only sets up the the Graphics Thread Provider.

#### The Graphics Thread Provider -
We implement three handlers here: Starting the graphics, population of Next Frame descriptor, and Submitting Frame.

In Starting the graphics thread, we allocate the texture that is used for the left and right eyes and we register it as a possible render surface in the XR display. To do this, we create a Metal texture and set its pointer in the Unity XR texture. This ties the representation in Unity together with a texture that exists in GPU RAM.

In the population of Next Frame descriptor, we fill the descriptor with data that tells the graphics thread how it should put together the next frame it’s about to draw. It also tells the thread which texture it's supposed to be draw to. In our case, we set it up to be the texture that we created. There’s also portions of this code that ask about Pose and Projection - this usually matters to other XR tools where we are modifying the camera position, like offsetting the camera to the left or the left eye and such. In this case, since we are needing any offsets, I just populated those with “default” values.

In the Submit Frame handler, we first make sure that the second canvas exists, and that there are no other threads tampering with it. If it is ready, then we take the section of the texture that has the right eye cameras and we are copying it over to the second screen.

### The Display Provider - 
The only handler we are concerned with for this project is the one that populates the Mirror View Blit descriptor. Much like with with the Next Frame descriptor, we are filling the descriptor with data. However this descriptor types tell Unity what should be drawn on the main screen. In our plugin specifically, we are telling it to take the left eye section of the texture and draw it over the main screen.

### Display Notifications
The final piece in the magic trick that makes it all work is that we are making use of the `NSNotificationCenter` class that comes with iOS’ UIKit. This lets us add event handlers for when a new display is connected or disconnected. When the display is connected, the handler attaches a new window to the screen (The default behavior is a full window on the screen we are using). Once the window is made, we create an instance of `MTKView`. The `MTKView` acts as a painting canvas for the window its parented to, and calls to Metal graphics API can paint there. (Submit Frame handler, it’s this canvas that we are copying the right eye camera to). The Disconnect handler does the opposite of setting up the second screen canvas; it destroys the MTKView and the window that were created for the second screen.

## Notes / Gotchas:
### The UPM package
This code is has been provided in a folder structure that makes it easy to host online. When you host it and provide the URL to developers, they can download through the Unity Package Manager. For more information on UPM and hosting, please [see the Unity Manual](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@1.8/manual/index.html). The files that are absolutely necessary for the folder to acknowledged as a UPM package is the package.json, which acts as a module description, and the *.asmdef file, which tells Unity to create a separate code project file. The latter also lets the scripts in the UPM package be acknowledged by Unity, so that you can connect the MonoBehaviours to GameObject in prefabs or the scene.

### The XR SDK plugin
There are a few requirements that this SDK needs in order to acknowledge. The first is that XR Settings and scene cameras need to be configured as described in README iOS. This tells the project to be on the lookout for `UnitySubsystemsManifest.json`, which populates a map of subsystems to be on the lookout for. `iKinRyzLoader`, which is a C# game script, has to be attached to a GameObject so that it looks up our subsystem in this map and tries to load the plugin. In order for that all to work, the package needs the file `UnityXrPlugin.m`, which has a class `UnityXrPlugin` that has the right functions to call to kick off the plugin. (It's looking for this class named like this because we named in in the manifest).

As for the he Xcode project, its already been configured in the project settings to copy the static library over to the UPM package. So don't change the folder structure of the deliverable unless you're ready to change the Xcode project copy location! Aside from that, [all the other rules](https://docs.unity3d.com/Manual/NativePluginInterface.html) for low-level native plugins apply.

#### Lifecycle Provider
Some of the function callbacks are stubbed, because event though we aren't doing anything with them, the callbacks can't be null or the plugin won't work. Don't remove any of the stubs, or it won't register the subsystem!

#### The Submit Frame Handler
Since the display connect/disconnect functions are called in the main thread, but the `MTKView` is used in the graphics thread, we have to provide a thread lock on `MTKView`. The connect/disconnect happens sometimes, but drawing happens every frame, so it was important to have a thread lock that could perform with very little bandwidth. I used a POSIX Read/Write lock for this, since it was designed specifically for this use case.

Since we are sharing `MTKView` between threads, we used the POSIX lock to make sure that `MTKView` wasn't being tampered with. However, just because `MTKView` being in a valid state doesn't ensure that `MTKView.currentDrawable` or  `MTKView.currentDrawable.texture` are ready. Older versions of this code were holding onto a new variables that referenced `MTKView.currentDrawable` and `MTKView.currentDrawable.texture` to reference count them and make sure they didn't slip, but we found holding onto them created problems. Apple suggests that [you don't hold onto them](https://developer.apple.com/library/archive/documentation/3DDrawing/Conceptual/MTLBestPracticesGuide/Drawables.html), either, which is why there are so many redundant null checks in this callback handler and why we didn't just cache them into other variables.

### Other
Unity source code had a bug with blitting that made it so that this plugin wouldn't work as intended. A fix has been made, and we have provided a custom build with this fix for you. Expect the fix to officially appear in Unity 2020.1

Finally, this implementation doesn’t really account for v-sync at 60 FPS. [Here’s](https://www.gamasutra.com/blogs/KwasiMensah/20110211/88949/Game_Loops_on_IOS.php) an article that talks about this problem in native iOS terms and provides some solutions.

Changing the eye dropdown in the Unity Editor Game View will only work in Play mode.
