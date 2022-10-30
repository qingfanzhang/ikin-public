# Developer Notes

## High-level Overview:

This project makes use of Unity's multidisplay camera configurations to handle the graphics and touch handling. The plugin provides a way to listen for display connection events.

## Low-Level Overview:
Most of the code provided exists in an Android Studio project called `LowLevelPluginAndroid`. There are two library that comprises the "plugin": One JNM (Java) library and one native (C++) libraries. The reason for the Java library `iKinRyz` is to access the event listener for hardware display connections. The C++ library, `UnityXrPlugin` has the function of sending display connection messages to the C# layer.

### Entry points and code flow
Unity primarily interfaces with `UnityXrPlugin`.  There is one main entry point into it from there `JNI_onLoad` which is automatically called by Java when the dynamic library is loaded in. This entry point in turn calls the Java layer through the JNI, and sets up the event listener for hardware display connections and disconnections. This lets us add event handlers for when a new display is connected or disconnected. The Disconnect handler does the opposite.

## Notes / Gotchas:
### The UPM package
This code is has been provided in a folder structure that makes it easy to host online. When you host it and provide the URL to developers, they can download through the Unity Package Manager. For more information on UPM and hosting, please [see the Unity Manual](https://docs.unity3d.com/Packages/com.unity.package-manager-ui@1.8/manual/index.html). The files that are absolutely necessary for the folder to acknowledged as a UPM package is the package.json, which acts as a module description, and the *.asmdef file, which tells Unity to create a separate code project file. The latter also lets the scripts in the UPM package be acknowledged by Unity, so that you can connect the MonoBehaviours to GameObject in prefabs or the scene.
