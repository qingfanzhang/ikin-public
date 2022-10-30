using UnityEngine;

public class DisplayEnabler : MonoBehaviour
{
#if !UNITY_EDITOR
	private void Start()
	{
#if UNITY_STANDALONE_WIN
		for (int i = 1; i < 2; i++)
		{
			Display.displays[i].Activate();
		}
#endif
	}
#endif
}