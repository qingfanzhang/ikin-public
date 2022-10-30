using UnityEngine;

public class MirrorView : MonoBehaviour
{
	private Camera cam;

	private bool initialize;

	private void Start()
	{
		cam = GetComponent<Camera>();
	}

	private void OnPreCull()
	{
        if (initialize)
		{
			return;
		}

		cam.projectionMatrix = cam.projectionMatrix * Matrix4x4.Scale(new Vector3(1f, -1f, 1f));

		initialize = true;
	}

	// Set it to true so we can watch the flipped Objects
	private void OnPreRender()
	{
		GL.invertCulling = true;
	}

	// Set it to false again because we dont want to affect all other cammeras.
	private void OnPostRender()
	{
		GL.invertCulling = false;
	}
}