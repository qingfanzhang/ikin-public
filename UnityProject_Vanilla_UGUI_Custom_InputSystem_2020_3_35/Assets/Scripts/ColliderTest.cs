using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ColliderTest : MonoBehaviour
{
	/*/
    public ikinRyzCamera cam;

    // Update is called once per frame
    void OnGUI()
    {
        if (GUI.Button(new Rect(Screen.width / 2f - (Screen.width * .125f), Screen.height / 2f - (Screen.height * 0.0625f), Screen.width * 0.25f, Screen.height * 0.125f), "Cast Ray"))
        {
            Mouse mouse = Mouse.current;
            Touchscreen touchscreen = Touchscreen.current;

            Ray ray = new Ray();

            if (mouse != null)
            {
                Vector2 pos = mouse.position.ReadValue();
                ray = cam.ScreenPointToRay(pos);
            }

            if (touchscreen != null && touchscreen.touches.Count > 0)
            {
                Vector2 pos = touchscreen.touches[0].position.ReadValue();
                ray = cam.ScreenPointToRay(pos);
            }

            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.WindowsEditor)
                Debug.DrawRay(ray.origin, ray.direction * 10, Color.green, 100f);
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = ray.origin + ray.direction * 5f;
            go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        }
    }
	//*/
}
