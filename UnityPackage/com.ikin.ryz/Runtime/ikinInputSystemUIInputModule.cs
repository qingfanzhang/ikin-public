#if PACKAGE_DOCS_GENERATION || UNITY_INPUT_SYSTEM_ENABLE_UI
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Serialization;

////FIXME: The UI is currently not reacting to pointers until they are moved after the UI module has been enabled. What needs to
////       happen is that point, trackedDevicePosition, and trackedDeviceOrientation have initial state checks. However, for touch,
////       we do *not* want to react to the initial value as then we also get presses (unlike with other pointers). Argh.

////REVIEW: I think this would be much better served by having a composite type input for each of the three basic types of input (pointer, navigation, tracked)
////        I.e. there'd be a PointerInput, a NavigationInput, and a TrackedInput composite. This would solve several problems in one go and make
////        it much more obvious which inputs go together.

////REVIEW: The current input model has too much complexity for pointer input; find a way to simplify this.

////REVIEW: how does this/uGUI support drag-scrolls on touch? [GESTURES]

////REVIEW: how does this/uGUI support two-finger right-clicks with touch? [GESTURES]

////TODO: add ability to query which device was last used with any of the actions

namespace ikin.InputSystem.UI
{
    /// <summary>
    /// Input module that takes its input from <see cref="InputAction">input actions</see>.
    /// </summary>
    /// <remarks>
    /// This UI input module has the advantage over other such modules that it doesn't have to know
    /// what devices and types of devices input is coming from. Instead, the actions hide the actual
    /// sources of input from the module.
    /// </remarks>
    [AddComponentMenu("IKIN/Ryz Input System UI Input Module")]
    public class ikinInputSystemUIInputModule : InputSystemUIInputModule
    {
        /*/
        public override void Process()
        {
            ProcessNavigation(ref m_NavigationState);
            for (var i = 0; i < m_PointerStates.length; i++)
            {
                ref var state = ref GetPointerStateForIndex(i);
                ProcessPointer(ref state);

                // If it's a touch and the touch has ended, release the pointer state.
                // NOTE: We have no guarantee that the system reuses touch IDs so the touch ID we used
                //       as a pointer ID may be a one-off thing.
                if (state.pointerType == UIPointerType.Touch && !state.leftButton.isPressed)
                {
                    RemovePointerAtIndex(i);
                    --i;
                }
            }
        }
        //*/
    }
}
#endif