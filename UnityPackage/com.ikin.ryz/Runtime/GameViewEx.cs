using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
public struct ikinRyzGameView
{
    #region Constants
    /// <summary>
    /// The type of <see cref="UnityEditor.GameView"/>.
    /// </summary>
    public static readonly Type UnityGameViewType = Type.GetType("UnityEditor.GameView, UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

    private static readonly Type UnityPlayModeViewType = Type.GetType("UnityEditor.PlayModeView, UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

    private static readonly FieldInfo TargetDisplayFieldInfo = UnityPlayModeViewType.GetField("m_TargetDisplay", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo GameViewParentFieldInfo = UnityPlayModeViewType.GetField("m_Parent", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly Type UnityDockAreaType = Type.GetType("UnityEditor.DockArea, UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken = null");
    private static readonly FieldInfo OriginalDragSourceFieldInfo = UnityDockAreaType.GetField("s_OriginalDragSource", BindingFlags.Static | BindingFlags.NonPublic);

    private static readonly Type SplitViewType = Type.GetType("UnityEditor.SplitView, UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken = null");
    private static readonly MethodInfo DragOverMethodInfo = SplitViewType.GetMethod("DragOver", BindingFlags.Instance | BindingFlags.Public);
    private static readonly MethodInfo PerformDropMethodInfo = SplitViewType.GetMethod("PerformDrop", BindingFlags.Instance | BindingFlags.Public);

    private static readonly PropertyInfo ViewPositionPropertyInfo = SplitViewType.GetProperty("position", BindingFlags.Instance | BindingFlags.Public);


    private static readonly System.Type ViewType = Type.GetType("UnityEditor.View, UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken = null");
    private static readonly FieldInfo ViewParentFieldInfo = ViewType.GetField("m_Parent", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo ViewChildrenFieldInfo = ViewType.GetField("m_Children", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly System.Type DropInfoType = Type.GetType("UnityEditor.DropInfo, UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken = null");
    private static readonly System.Type IDropAreaType = Type.GetType("UnityEditor.IDropArea, UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken = null");
    private static readonly ConstructorInfo DropInfoConstructorInfo = DropInfoType.GetConstructor(new Type[] { IDropAreaType });

    // Types within types are resolved using '+' instead of '.'
    private static readonly System.Type ExtraDropInfoType = Type.GetType("UnityEditor.SplitView+ExtraDropInfo, UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken = null");
    private static readonly System.Type ViewEdgeEnum = Type.GetType("UnityEditor.SplitView+ViewEdge, UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken = null");
    private static readonly ConstructorInfo ExtraDropInfoConstructorInfo = ExtraDropInfoType.GetConstructor(new Type[] { typeof(bool), ViewEdgeEnum, typeof(int) });
    private static readonly object ViewEdgeEnumTop = System.Enum.Parse(ViewEdgeEnum, "Top");
    private static readonly object ViewEdgeEnumBottom = System.Enum.Parse(ViewEdgeEnum, "Bottom");

    private static readonly FieldInfo UserDataFieldInfo = DropInfoType.GetField("userData");

    private static readonly System.Type DropInfoTypeEnum = Type.GetType("UnityEditor.DropInfo+Type, UnityEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken = null");
    private static readonly object DropInfoTypeEnumPane = System.Enum.Parse(DropInfoTypeEnum, "Pane");
    private static readonly FieldInfo DropInfoTypeFieldInfo = DropInfoType.GetField("type");

    private static readonly FieldInfo DropInfoRectFieldInfo = DropInfoType.GetField("rect");
    private static readonly PropertyInfo SplitViewScreenPositionPropertyInfo = SplitViewType.GetProperty("screenPosition");
    #endregion

    #region Static Properties
    public static ikinRyzGameView[] instances
    {
        get
        {
            UnityEngine.Object[] unityInstances = Resources.FindObjectsOfTypeAll(UnityGameViewType);

            var instances = new ikinRyzGameView[unityInstances.Length];

            for (int i = 0; i < unityInstances.Length; ++i)
            {
                instances[i] = new ikinRyzGameView(unityInstances[i] as EditorWindow);
            }

            return instances;
        }
    }
    #endregion

    #region Static Methods
    [MenuItem("Window/IKIN/Ryz/GameViews")]
    private static void SetupRyzWindows()
    {
        var instances = ikinRyzGameView.instances;

        int gameViewCount = instances.Length;


        if(gameViewCount == 0)
        {
            // don't want to just create the game views in a random spot undocked, so make sure that at least one exists so that
            // we know the position will seem reasonable
            EditorUtility.DisplayDialog("No Game Views present", "The current layout has no Game View to figure out where to dock to.  You can create one by entering and exiting Play Mode.", "Okay");
            return;
        }


        int gameViewToModify = 2 - gameViewCount;

        if (gameViewToModify < 0)
        {
            gameViewToModify = Mathf.Abs(gameViewToModify);

            // Remove excess GameView instances.
            for (int i = 0; i < gameViewToModify; ++i)
            {
                DestroyImmediate(instances[2 + i]);
            }
        }
        else if (gameViewToModify > 0)
        {
            // Add more GameView instances equal to the amount missing.
            for (int i = 0; i < gameViewToModify; ++i)
            {
                ScriptableObject.CreateInstance(UnityGameViewType);
            }
        }

        // There should only be two GameView instances now.
        instances = ikinRyzGameView.instances;


        ikinRyzGameView anchor = instances[1];
        ikinRyzGameView docked = instances[0];

        if (anchor.Docked == false && docked.Docked == true)
        {
            ikinRyzGameView temp = anchor;
            anchor = docked;
            docked = temp;
        }


        // phone view is on the bottom, hologram view is on the top
        // we always dock above the anchor, so we need to make sure the anchor is the phone view (view 0)
        anchor.TargetDisplay = 0;
        anchor.unityInstance.Show();

        docked.TargetDisplay = 1;
        docked.unityInstance.Show();


        DockWindow(anchor, docked);  // can also be ran while already docked to resize the windows again
    }

    public static void DestroyImmediate(ikinRyzGameView instance)
    {
        EditorWindow.DestroyImmediate(instance.unityInstance);
    }

    private static void DockWindow(ikinRyzGameView anchor, ikinRyzGameView docked)
    {
        // The rough idea of this was taken from this gist:  https://gist.github.com/Thundernerd/5085ec29819b2960f5ff2ee32ad57cbb
        // However, it didn't immediately work, so a lot of research was done here:  https://github.com/Unity-Technologies/UnityCsReference
        // in order to figure out a way to get it to work without calling the expected functions due to some unknown error with the arg values
        // 
        // The given example also seems to only dock to the overall editor frame (i.e. it'll go above or below *everything* else, hence
        // its use of the `rootSplitView` property) rather than just on the game view, so also had to adjust that a bit to be able to
        // find the correct spot to dock to.



        // Essentially we are going to be simulating a drag and drop input.

        // This comes from the gist, the +20 is some offset to get Unity to accept the drag and drop (some sort of min distance threshold).
        Vector2 screenPoint = Vector2.zero;
        screenPoint.x = anchor.unityInstance.position.x + (anchor.unityInstance.position.size.x / 2);
        screenPoint.y = anchor.unityInstance.position.y + 20;


        // The editor interface is composed of several Views.  One type of view is a SplitView, which is pretty much a div of divs in html and
        // can divide a group of elements vertically or horizontally.
        // 
        // We are going to be docking to the anchor window's parent SplitView.  The original gist instead used the top-level `rootSplitView` and
        // would insert the docking window above everything else rather than just onto the given anchor window.

        object splitView = anchor.ViewParent;

        // The child index that we provide determines where the docking window is going to be inserted.  Depending on layout, there can be
        // multiple other windows within the SplitView, so we have to make sure we find the right one and don't insert somewhere random, in
        // this case we want the child that corresponds to the anchor window (the property handles this).
        int childIndex = anchor.ViewParentChildIndex;


        // Swap these to insert on the bottom edge instead of the top edge.
        object viewEdge = ViewEdgeEnumTop;
        //object viewEdge = ViewEdgeEnumBottom;


        // The original gist would invoke the DragOver() method on `splitView`, but there was seemingly some error with the screen point that it
        // was passing in that would cause nothing to happen since it couldn't find anything at that point to dock to.  Unable to figure out a
        // nice way to solve this beyond bruteforcing a valid screen point, we instead reimplemented the DragOver() method found here:
        //  - UnityCsReference/Editor/Mono/GUI/SplitView.cs
        // 
        // This function doesn't really do anything except set up a DropInfo object that is then passed into the PerformDrop() method to actually
        // resolve the drag-and-drop input.
        // 
        // DragOver() also does some extra handling for picking an edge if you try to drag onto a corner, but we don't have to worry about that.

        object dropInfo = DropInfoConstructorInfo.Invoke(new object[] { splitView });
        DropInfoTypeFieldInfo.SetValue(dropInfo, DropInfoTypeEnumPane);

        // Different kinds of Views can supply different type-specific parameters on the DropInfo, which is done here.
        object extraDropInfo = ExtraDropInfoConstructorInfo.Invoke(new object[] { false, viewEdge, childIndex });
        UserDataFieldInfo.SetValue(dropInfo, extraDropInfo);


        // Not really sure what these do, just taking from the reference source.
        Rect childRect = (Rect) SplitViewScreenPositionPropertyInfo.GetValue(splitView);
        float thickness = Mathf.Max(100f, childRect.height / 3);
        float offset = 0f;

        // Swap these to insert on the bottom edge instead of the top edge.  These actually come from the SplitView.RectFromEdge() function.
        Rect rect = new Rect(childRect.x, childRect.y - offset, childRect.width, thickness);
        //Rect rect = new Rect(childRect.x, childRect.yMax - thickness + offset, childRect.width, thickness);


        // This comes from the gist, seems to be set up in a DockArea input handler rather than in DragOver(), but it's referenced in
        // the PerformDrop() method, so we have to set it up before that.
        docked.OriginalDragSource = docked.Parent;

        // The SplitView implementation of this method doesn't actually make use of `screenPoint` despite the interface requiring it, so it being
        // possibly wrong when we tried using it for DragOver() doesn't matter for now.  Other View types might make use of it.
        PerformDropMethodInfo.Invoke(splitView, new object[] { docked.unityInstance, dropInfo, screenPoint });



        // At this point, the windows should now be docked together, but the newly-spawned window will likely be whatever default size Unity gave it, so
        // we want to make sure the two windows are set to equal sizes now.


        // Dock size appears to be 1:1 with no sort of offsets.
        // 
        // Also note that docking the new window can create a new split view, which means that the original window's position can potentially
        // now be relative to a different split, but this doesn't matter as we only need to check its size.


        // Had issues with trying to force the new windows into positions smaller than expected, which would completely break that section of the editor.
        // 
        // So instead, we'll just take whatever space the editor decides to carve out for us on its own, and then equally divide that among the two windows.
        // This will cause some of the surrounding views to be potentially shifted, but will save the editor from breaking.

        Rect newDockedPosition = docked.ViewPosition;
        Rect newAnchorPosition = anchor.ViewPosition;

        float totalHeight = newDockedPosition.height + newAnchorPosition.height;
        float halfHeight = totalHeight / 2;
        newDockedPosition.height = halfHeight;
        newAnchorPosition.height = halfHeight;
        newAnchorPosition.y = newDockedPosition.y + halfHeight;  // Second split starts right where the first split ends.

        docked.ViewPosition = newDockedPosition;
        anchor.ViewPosition = newAnchorPosition;

        // Now the windows are docked and equal sizes.
    }

    #endregion

    #region Fields
    public EditorWindow unityInstance;
    #endregion

    #region Properties
    public int TargetDisplay
    {
        get
        {
            return (int)TargetDisplayFieldInfo.GetValue(unityInstance);
        }
        set
        {
            TargetDisplayFieldInfo.SetValue(unityInstance, value);
        }
    }

    public object Parent
    {
        get
        {
            return GameViewParentFieldInfo.GetValue(unityInstance);
        }
    }

    public object OriginalDragSource
    {
        get
        {
            return OriginalDragSourceFieldInfo.GetValue(null);
        }
        set
        {
            OriginalDragSourceFieldInfo.SetValue(null, value);  // this is actually a static field, so it takes a null instance
        }
    }

    public Rect ViewPosition
    {
        get
        {
            return (Rect) ViewPositionPropertyInfo.GetValue(this.Parent);
        }
        set
        {
            ViewPositionPropertyInfo.SetValue(this.Parent, value);
        }
    }

    public object ViewParent
    {
        get
        {
            return ViewParentFieldInfo.GetValue(this.Parent);
        }
    }

    public bool Docked
    {
        get
        {
            return this.unityInstance.docked;
        }
    }

    public int ViewParentChildIndex
    {
        get
        {
            System.Array viewChildren = (System.Array) ViewChildrenFieldInfo.GetValue(this.ViewParent);  // This is technically a View[] but the reflection requires untyped Arrays.
            int childIndex = -1;
            for (int i = 0; i < viewChildren.Length; i++)
            {
                object viewChild = viewChildren.GetValue(i);
                if (viewChild == this.Parent)
                {
                    childIndex = i;
                    break;
                }
            }

            return childIndex;
        }
    }
    #endregion

    #region Methods
    private ikinRyzGameView(EditorWindow unityInstance)
    {
        this.unityInstance = unityInstance;
    }
    #endregion
}
#endif

