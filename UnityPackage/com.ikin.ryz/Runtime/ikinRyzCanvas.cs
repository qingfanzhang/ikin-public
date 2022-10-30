using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("IKIN/Ryz Canvas")]
[RequireComponent(typeof(Canvas))]
public class ikinRyzCanvas : MonoBehaviour
{
    private static bool isDisplayConnected;

    class Helper : MonoBehaviour
    {
        static Helper()
        {
            ikinRyzEvents.onDisplayEvent -= OnRyzDisplayEvent;
            ikinRyzEvents.onDisplayEvent += OnRyzDisplayEvent;

            DisplayEvent displayEvent = ikinRyzEvents.GetDisplayEvent();

            isDisplayConnected = displayEvent == DisplayEvent.Connected;
        }

        private static bool lastDisplayConnected;

        public List<ikinRyzCanvas> Canvases { get; private set;}

        private static void OnRyzDisplayEvent(DisplayEvent value)
        {
            isDisplayConnected = value == DisplayEvent.Connected;
        }

        private void Awake()
        {
            Canvases = new List<ikinRyzCanvas>(2);
        }

        private void Update()
        {
            if (isDisplayConnected != lastDisplayConnected)
            {
                for (int i = 0; i < Canvases.Count; ++i)
                {
                    Canvases[i].UpdateCanvas(isDisplayConnected);
                }

                lastDisplayConnected = isDisplayConnected;
            }
        }
    }

    private static Helper instance;

    #region Fields
    private Canvas canvas;
    #endregion

    #region Methods
    private void Awake()
    {
        canvas = GetComponent<Canvas>();

        if (instance == null)
        {
            var gameObject = new GameObject("IKIN Ryz Canvas Helper");
            instance = gameObject.AddComponent<Helper>();
            gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        instance.Canvases.Add(this);

        UpdateCanvas(isDisplayConnected);
    }

    private void OnDestroy()
    {
        instance.Canvases.Remove(this);
    }

    private void UpdateCanvas(bool value)
    {
        if (canvas.targetDisplay != 0)
        {
            canvas.gameObject.SetActive(value);
        }
    }
    #endregion
}