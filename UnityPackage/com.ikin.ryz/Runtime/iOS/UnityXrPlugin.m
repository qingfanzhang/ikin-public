#include "IUnityInterface.h"

#ifdef __cplusplus
extern "C"
{
#endif

    // Forward declaration
    void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad (IUnityInterfaces* unityInterfaces);
    
#ifdef __cplusplus
}
#endif

@interface UnityXrPlugin : NSObject

+ (void) loadPlugin;

@end

@implementation UnityXrPlugin

+ (void)loadPlugin
{
    UnityRegisterRenderingPluginV5(UnityPluginLoad, NULL);
}

@end
