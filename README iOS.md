# Quick Setup for Plugin Developers

## First-time Setup

**These CRUCIAL steps need to be done at least once after pulling down on your local machine**

1. Open `iKinRyzProject/LowLevelNativePluginMacEditor/LowLevelNativePluginMacEditor.xcodeproj` in Xcode
2. Build the project with an processor architecture that matches the target device (on the right side of the `>` that is right of the Run button) [^1]
3. Make sure the native library was copied into the package directory under `UnityPackage/com.ikin.ryz/Editor/Mac/UnityXrPlugin.dylib` [^2]

4. Open `iKinRyzProject/LowLevelNativePluginiOS/LowLevelNativePlugin.xcodeproj` in Xcode
5. Build the project with an processor architecture that matches the target device (on the right side of the `>` that is right of the Run button) [^1]
6. Make sure the native library was copied into the package directory under `UnityPackage/com.ikin.ryz/Runtime/iOS/libUnityXrPlugin.a` [^2]

[^1]: will also copy the build output `libUnityXrPlugin.a` to the Unity Package Manager package [More on what UPM is and how it works](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@2.1/manual/index.html)
[^2]: If there is no accompanying  *.a file, the  `UnityPackage/com.ikin.ryz/Runtime/iOS/libUnityXrPlugin.a.meta` will be deleted if you open the Unity project since meta files need an accompanying file. The settings in that file are needed to recognize the plugin. Adding the *.a file after its deleted will only force Unity to create another, for the inverse reason it deleted the original - Unity project files need an accompanying meta file. If this happens, then select the *.a file in Unity and modify the value in the Inspector window so that `iOS` is checked in `Include Platforms`, and `Metal` and `MetalKit` are checked under `Platform Settings` -> `Rarely used frameworks`.

## Generalized Workflow for Making Changes to the Plugin

**Treat this just as a checklist to help remind of any missing steps in your workflow**

### 1. Building the Native library
1. Open `iKinRyzProject/LowLevelNativePluginiOS/LowLevelNativePlugin.xcodeproj` in Xcode
2. Make any changes to the native plugin if needed
3. Set up the appropriate team signing credentials per usual
4. Build the project with an processor architecture that matches the target device (on the right side of the `>` that is right of the Run button) [^1]

 ### 2. Setting Up the Managed Library
 1. Make sure the native library was copied into the package directory under `UnityPackage/com.ikin.ryz/Runtime/iOS/libUnityXrPlugin.a` [^2]
 3. Update the `CHANGELOG.md` with new changes, if you have one.
 4. Update `package.json` file if needed, which is used by UPM to define the deployable package
 5. Update `UnitySubsystemsManifest.json` file if needed, which is used by Unity XR SDK to detect the plugin and define what XR functionality it provides Unity
 6. Host the package (aka everything within  `Assets/com.ikin.ryz/`) to an online location, if needed


# Quick Setup for App Developers

## Configuring a New Unity Project with the iKin Ryz Plugin

**Plugin Developers: there is already a testbed project provided. 
These steps are only neccessary if you're making an new project from scratch**

1. Make sure that the UPM package is configured like above, and hosted locally or online
2. Create or open a Unity game project
3. Add the iKin Ryz UPM package to the project, either locally or from online (see [^1])
4. In [`iOS Player Settings`](https://docs.unity3d.com/2018.2/Documentation/Manual/class-PlayerSettings.html):
    a. Under `Resolution and Presentation` , set `Default orientation` to `Portrait`
    b. Under `XR Settings`, enable `Virtual Reality Supported`  and add `None` to `VR SDKs`
5. Select `XR Plugin Management` (the same window, lower left). You should see `iKin Ryz Plugin` listed under `Known XR Plugin Providers`.
    a. Under `iOS` and `Standalone` tabs, add `i Kin Ryz Loader` to the `Plugin Providers` reorderable list with the `+` dropdown button.
    b. Select the `iKinRyz` tab and press the `Create` button to create settings for the plugin.
6. Attach a `Ryz Camera` component. When using XR SDK, all the cameras have a `Target Eye` property in the inspector. With the Ryz plugin, `Left` eye is treated as the iPhone camera, and the `Right` is for the Ryz display. Cameras will layer if there is more than one with the same `Target Eye` setting. 
    a. The cameras in the Unity scenes need to have these values setup in one of the following ways:
    - Most Common setup for the Ryz: At Least one camera with `Left` set and at least one camera with `Right` set. 'Right' eye cameras having a larger `Depth` property than the `Left` eye cameras
    - At least one camera with `Both` set (if you want the same image on both screens)
    - At least one camera with `None` set. (If this is the setup, XR SDK plugin won't execute. Becomes regular Unity functionality)
    b. The depth of the cameras should be different.

## Building the Unity Project

1. Make sure that the game project is configured like above.
2. Build the Unity Project for iOS [^3].
3. In Xcode, set up the appropriate team signing credentials per usual.
4. Choose a target device and build and run per usual.

[^3]: This will create `Unity-iPhone.xcodeproj` that you then use to build through Xcode [More info here](https://docs.unity3d.com/Manual/iphone-GettingStarted.html)
