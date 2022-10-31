using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class PlayTest
{
    void ClickButton(Button button)
    {
        bool called = false;
        button.onClick.AddListener(() => { called = true; });
        button.OnPointerClick(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
        //Debug.Log(called);
        Assert.AreEqual(called, true);
    }

    void ChangeCanvasRenderMode(Canvas canvas, RenderMode mode)
    {
        canvas.renderMode = mode;
    }

    [Test]
    public void ButtonClickTest()
    {
        var allParent = GameObject.Instantiate(Resources.Load("AllParent"));

        var go = GameObject.Find("Left Eye Canvas Screen Camera").transform.Find("Left Button");
        Button leftButton = go.GetComponent<Button>();
        Assert.IsNotNull(leftButton, "Can not find Left Button!");

        Button rightButton = GameObject.Find("Right Eye Canvas Screen Camera").transform.Find("Right Button").GetComponent<Button>();
        Assert.IsNotNull(leftButton, "Can not find Right Button!");

        Canvas leftEyeCanvas = GameObject.Find("Left Eye Canvas Screen Overlay").GetComponent<Canvas>();
        Assert.IsNotNull(leftEyeCanvas, "Can not find Left Eye Canvas Screen Overlay!");

        Canvas rightEyeCanvas = GameObject.Find("Right Eye Canvas Screen Overlay").GetComponent<Canvas>();
        Assert.IsNotNull(rightEyeCanvas, "Can not find Right Eye Canvas Screen Overlay!");

        //ClickBothButtonOnCanvasOverlayMode
        ChangeCanvasRenderMode(leftEyeCanvas, RenderMode.ScreenSpaceOverlay);
        ChangeCanvasRenderMode(rightEyeCanvas, RenderMode.ScreenSpaceOverlay);
        ClickButton(leftButton);
        ClickButton(rightButton);

        //ClickBothButtonOnCanvasCameraMode
        ChangeCanvasRenderMode(leftEyeCanvas, RenderMode.ScreenSpaceCamera);
        ChangeCanvasRenderMode(rightEyeCanvas, RenderMode.ScreenSpaceCamera);
        ClickButton(leftButton);
        ClickButton(rightButton);

        GameObject.DestroyImmediate(allParent);
    }

    [UnityTest]
    public IEnumerator DragCallbacksDoGetCalled()
    {
        //yield break;
        var playAllParent = GameObject.Instantiate(Resources.Load("PlayAllParent"));

        //var m_Canvas = new GameObject("Canvas").AddComponent<Canvas>();
        //m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        //m_Canvas.gameObject.AddComponent<GraphicRaycaster>();
        var m_Canvas = GameObject.Find("Left Eye Canvas Screen Overlay").GetComponent<Canvas>();
        m_Canvas.targetDisplay = 0;

        var m_Image = new GameObject("Image").AddComponent<Image>();
        m_Image.gameObject.transform.SetParent(m_Canvas.transform);
        RectTransform imageRectTransform = m_Image.GetComponent<RectTransform>();
        imageRectTransform.sizeDelta = new Vector2(100f, 100f);
        imageRectTransform.localPosition = Vector3.zero;

        GameObject go = new GameObject("Event System");
        var m_EventSystem = go.AddComponent<EventSystem>();
        EventSystem.current.pixelDragThreshold = 1; 

        var m_StandaloneInputModule = go.AddComponent<StandaloneInputModule>();
        var m_FakeBaseInput = go.AddComponent<FakeBaseInput>();
        m_FakeBaseInput.image = m_Image;
        m_FakeBaseInput.MousePosition = m_Image.GetComponent<RectTransform>().position; //new Vector2(Screen.width / 2, Screen.height / 2);

        // Override input with FakeBaseInput so we can send fake mouse/keyboards button presses and touches
        m_StandaloneInputModule.inputOverride = m_FakeBaseInput;

        DragCallbackCheck callbackCheck = m_Image.gameObject.AddComponent<DragCallbackCheck>();
        // Setting required input.mousePresent to fake mouse presence
        m_FakeBaseInput.MousePresent = true;
        yield return null; //important!

        // Left mouse button down simulation
        m_FakeBaseInput.MouseButtonDown[0] = true;
        yield return null; //important!

        // Left mouse button down flag needs to reset in the next frame
        m_FakeBaseInput.MouseButtonDown[0] = false;
        for (int i = 0; i < 1000; i++)
            yield return null;

        // Left mouse button up simulation
        m_FakeBaseInput.MouseButtonUp[0] = true;
        yield return null;

        // Left mouse button up flag needs to reset in the next frame
        m_FakeBaseInput.MouseButtonUp[0] = false;
        yield return null;

        Assert.IsTrue(callbackCheck.onBeginDragCalled, "OnBeginDrag not called");
        Assert.IsTrue(callbackCheck.onDragCalled, "OnDragCalled not called");
        Assert.IsTrue(callbackCheck.onEndDragCalled, "OnEndDragCalled not called");

        GameObject.DestroyImmediate(playAllParent);
    }
}

public class FakeBaseInput : BaseInput
{
    public System.String CompositionString = "";

    private IMECompositionMode m_ImeCompositionMode = IMECompositionMode.Auto;
    private Vector2 m_CompositionCursorPos = Vector2.zero;
    public bool MousePresent = false;

    public bool[] MouseButtonDown = new bool[3];

    public bool[] MouseButtonUp = new bool[3];
    public bool[] MouseButton = new bool[3];
    public Vector2 MousePosition = Vector2.zero;
    public Vector2 MouseScrollDelta = Vector2.zero;
    public bool TouchSupported = false;
    public int TouchCount = 0;
    public Touch TouchData;
    public float AxisRaw = 0f;
    public bool ButtonDown = false;

    public Image image;

    void Update()
    {
        MousePosition.x += 1f;
        //image.GetComponent<RectTransform>().anchoredPosition += new Vector2(1, 0);
        image.GetComponent<RectTransform>().position = MousePosition;
    }

    public override string compositionString
    {
        get { return CompositionString; }
    }

    public override IMECompositionMode imeCompositionMode
    {
        get { return m_ImeCompositionMode; }
        set { m_ImeCompositionMode = value; }
    }

    public override Vector2 compositionCursorPos
    {
        get { return m_CompositionCursorPos; }
        set { m_CompositionCursorPos = value; }
    }

    public override bool mousePresent
    {
        get { return MousePresent; }
    }

    public override bool GetMouseButtonDown(int button)
    {
        return MouseButtonDown[button];
    }

    public override bool GetMouseButtonUp(int button)
    {
        return MouseButtonUp[button];
    }

    public override bool GetMouseButton(int button)
    {
        return MouseButton[button];
    }

    public override Vector2 mousePosition
    {
        get { return MousePosition; }
    }

    public override Vector2 mouseScrollDelta
    {
        get { return MouseScrollDelta; }
    }

    public override bool touchSupported
    {
        get { return TouchSupported; }
    }

    public override int touchCount
    {
        get { return TouchCount; }
    }

    public override Touch GetTouch(int index)
    {
        return TouchData;
    }

    public override float GetAxisRaw(string axisName)
    {
        return AxisRaw;
    }

    public override bool GetButtonDown(string buttonName)
    {
        return ButtonDown;
    }
}

public class DragCallbackCheck : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerDownHandler
{
    private bool loggedOnDrag = false;
    public bool onBeginDragCalled = false;
    public bool onDragCalled = false;
    public bool onEndDragCalled = false;
    public bool onDropCalled = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log(1);
        onBeginDragCalled = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (loggedOnDrag)
            return;

        loggedOnDrag = true;
        onDragCalled = true;

        Debug.Log(2);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log(3);
        onEndDragCalled = true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log(4);
        onDropCalled = true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log(5);
        // Empty to ensure we get the drop if we have a pointer handle as well.
    }
}