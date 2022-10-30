# Quick Setup for Plugin Developers

## First-time Setup

**These CRUCIAL steps need to be done at least once after pulling down on your local machine**

3. Select `iKinRyzProject/LowLevelNativePluginAndroid/` as the project in Android Studio
4. Make any changes to the native plugin if needed. [^1] To force build of libraries:
 a. Press ⌘1 to open `Project` view.
    b. Select `LowLevelPlugin` from the `Project` view.
    c. From the file menu, select `Build` -> `Make Module 'LowLevelPlugin'`
5. Make sure the native libraries `libUnityXrPlugin.so` is copied into the package under `UnityPackage/com.ikin.ryz/Runtime/Android/` in subfolders `arm64-v8a`, `armeabi-v7a`, and `x86` [^2]
6. Make sure the managed libraries `libiKinRyz.aar` is copied into the package under `UnityPackage/com.ikin.ryz/Runtime/Android/`.[^2]

[^1]: will also copy the build output to the Unity Package Manager package [More on what UPM is and how it works](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@2.1/manual/index.html)
[^2]: If there is no accompanying  *.so and *.aar files, the *.so.meta and *.aar.meta files that need to accompanying them will be deleted if you open the Unity project. The settings in that file are needed to recognize the plugin. Adding the *.so and *.aar files after its deleted will only force Unity to create another, for the inverse reason it deleted the original - Unity project files need an accompanying meta file. If this happens, then select the *.so and *.aar files in Unity and modify the value in the Inspector window so that `Android` is checked in `Include Platforms`. For the *.so files, you also need to match which architecture they belong to under `Platflorm Settings -> CPU`.

## Generalized Workflow for Making Changes to the Plugin

**Treat this just as a checklist to help remind of any missing steps in your workflow**

### 1. Building the Native libraries
1. Select `iKinRyzProject/LowLevelNativePluginAndroid/` as the project in Android Studio
2. Make any changes to the native plugin if needed. [^1] To force build of libraries:
 a. Press ⌘1 to open `Project` view.
    b. Select `native_presentation` from the `Project` view.
    c. From the file menu, select `Build` -> `Make Module 'nativepresentation'`
    d. Select `LowLevelPlugin` from the `Project` view.
    e. From the file menu, select `Build` -> `Make Module 'LowLevelPlugin'`

### 2. Setting Up the Managed Library
1. Make sure the native libraries `libUnityXrPlugin.so`, `libCompanionActivity.so`, and `libandroid_native_presentation.so` were copied into the package under `UnityPackage/com.ikin.ryz/Runtime/Android/` in subfolders `arm64-v8a`, `armeabi-v7a`, and `x86` [^2]
2. Make sure the managed libraries `libiKinRyz.aar` is copied into the package under `UnityPackage/com.ikin.ryz/Runtime/Android/`.[^2]
3. Update `CHANGELOG.md` with new changes, if you have one.
4. Update `package.json` file if needed, which is used by UPM to define the deployable package
5. Update `UnitySubsystemsManifest.json` file if needed, which is used by Unity XR SDK to detect the plugin and define what XR functionality it provides Unity
6. Host the package (aka everything within  `UnityPackage/com.ikin.ryz/`) to an online location, if needed


# Quick Setup for App Developers

## Configuring a New Unity Project with the IKIN Ryz Plugin

**Plugin Developers: there is already a testbed project provided. 
These steps are only necessary if you're making an new project from scratch**

1. Make sure that the UPM package is configured like above, and hosted locally or online
2. Create or open a Unity game project
3. Add the IKIN Ryz UPM package to the project, either locally or from online (see [^1])
4. Attach at least two `Camera` components in the scene, with one camera's `Display` property set to `Display 1` for the mobile device screen and the second with `Display 2` set for the Ryz device screen.  Attach a `RyzCamera` Component to any of the Cameras that are going to be used as the peripheral's camera, as this will ensure that the Camera is mirrored properly.
5. If Using UI in your project, locate the GameObject in your scene that has the `EventSystem` component attached. Replace the `StandaloneInputModule` Component with one of the Ryz equivalents that come with the IKIN plugin package. Set the `Canvas` instances in your scene so that they are set to `Screen Space - Overlay` or `Screen Space - Camera`, and set the appropriate screen/camera that the UI will lay on.
6. In build settings, set `Build System` to `Gradle` and Export to a Gradle project.
7. In Android Studio, open the Gradle project. Locate the `Project` tab and select Android from the dropdown menu to navigate the project. Navigate to `unityLibrary\java\com.unity3d.player\UnityPlayerActivity.java`, and locate the class method `onWindowFocusChanged`. Add `hasFocus = true;` as the first line in the function; this will make it so that you can switch between touchscreens on the IKIN Ryz.
8. Build the game to your target device.

## Building the Unity Project

1. Make sure that the game project is configured like above.
2. Build the Unity Project for Android [^3].
