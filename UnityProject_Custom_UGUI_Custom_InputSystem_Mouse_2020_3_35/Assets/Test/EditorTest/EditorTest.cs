using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class EditorTest
{
    //StandaloneInputModule iput1;


    GameObject redCube;
    GameObject PingPongCube;

    Button leftButton;
    Button rightButton;

    Canvas leftEyeCanvas;
    Canvas rightEyeCanvas;

    [SetUp]
    public void TestSetup()
    {
        //BaseInput abc;
        //iput1.inputOverride

        redCube = GameObject.Find("Red Cube");
        Assert.IsNotNull(redCube, "Can not find Red Cube!");

        PingPongCube = GameObject.Find("Ping Pong Cube");
        Assert.IsNotNull(redCube, "Can not find Ping Pong Cube!");

        var go = GameObject.Find("Left Eye Canvas Screen Camera").transform.Find("Left Button");
        leftButton = go.GetComponent<Button>();
        Assert.IsNotNull(leftButton, "Can not find Left Button!");

        rightButton = GameObject.Find("Right Eye Canvas Screen Camera").transform.Find("Right Button").GetComponent<Button>();
        Assert.IsNotNull(leftButton, "Can not find Right Button!");

        leftEyeCanvas = GameObject.Find("Left Eye Canvas Screen Overlay").GetComponent<Canvas>();
        Assert.IsNotNull(leftEyeCanvas, "Can not find Left Eye Canvas Screen Overlay!");

        rightEyeCanvas = GameObject.Find("Right Eye Canvas Screen Overlay").GetComponent<Canvas>();
        Assert.IsNotNull(rightEyeCanvas, "Can not find Right Eye Canvas Screen Overlay!");

        //Debug.Log(EventSystem.current); 
    }

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
    public void ClickBothButtonOnCanvasOverlayMode()
    {
        ChangeCanvasRenderMode(leftEyeCanvas, RenderMode.ScreenSpaceOverlay);
        ChangeCanvasRenderMode(rightEyeCanvas, RenderMode.ScreenSpaceOverlay);

        ClickButton(leftButton);
        ClickButton(rightButton);
    }

    [Test]
    public void ClickBothButtonOnCanvasCameraMode()
    {
        ChangeCanvasRenderMode(leftEyeCanvas, RenderMode.ScreenSpaceCamera);
        ChangeCanvasRenderMode(rightEyeCanvas, RenderMode.ScreenSpaceCamera);

        ClickButton(leftButton);
        ClickButton(rightButton);
    }
}
