using UnityEngine;
using UnityEngine.Rendering;

[AddComponentMenu("Test/Camera Flip")]
[RequireComponent(typeof(Camera))]
public class ikinRyzCamera : MonoBehaviour
{
	private Camera cam;

	private void Start()
	{
		cam = GetComponent<Camera>();

		/// If we found our camera just flip the projection matrix here so it's cached for either pipeline
		if (cam)
		{
			cam.projectionMatrix = cam.projectionMatrix * Matrix4x4.Scale(new Vector3(1f, -1f, 1f));
		}
	}

	/// BEGIN UNIVERSAL RENDER PIPELINE LOGIC
	/// UNITY_URP is defined by the pressence of the URP unity package as laid out in a rule in the ikinRyz package Runtime asmdef
	/// This define helps limit the code compiled based on which render pipeline is being used to make sure no extraneous render pipeline delegates are subscribed
	/// and there are no erroneous/additional inversions to culling that are necessary when the camera's projection matrix has been inverted.
#if UNITY_URP
	/// OnPreRender and OnPostRender are not called in URP
	private void OnEnable()
	{
		RenderPipelineManager.beginCameraRendering -= InvertCulling;
        RenderPipelineManager.beginCameraRendering += InvertCulling;
		RenderPipelineManager.endCameraRendering -= RevertCulling;
        RenderPipelineManager.endCameraRendering += RevertCulling;
	}

	private void OnDisable()
	{
        RenderPipelineManager.beginCameraRendering -= InvertCulling;
        RenderPipelineManager.endCameraRendering -= RevertCulling;
	}

	void InvertCulling(ScriptableRenderContext context, Camera camera)
	{
        /// Need to make sure the camera passed in is actually this camera (the ikin external display camera)
        /// Otherwise URP will make calls to invert culling on every camera in the scene
		if(camera == cam)
		{
			GL.invertCulling = true;
		}
	}

	void RevertCulling(ScriptableRenderContext context, Camera camera)
	{
        /// Need to make sure the camera passed in is actually this camera (the ikin external display camera)
        /// Otherwise URP will make calls to invert culling on every camera in the scene
		if(camera == cam)
		{
			GL.invertCulling = false;
		}
	}
	/// END UNIVERSAL RENDER PIPELINE LOGIC
#else
	/// BUILT IN RENDER PIPELINE LOGIC

	// Set it to true so we can watch the flipped Objects
	private void OnPreRender()
	{
/// There is a chance that exists where the URP package may exist in a project but may not be getting utilized, this can be denoted by the pressence of pipeline asset in Graphics Settings
/// In these cases BIRP will be used, and we want to make sure the Graphics Settings currentRenderPipeline is null before inverting culling 
#if UNITY_URP
        if(GraphicsSettings.currentRenderPipeline == null)
#endif
		    GL.invertCulling = true;
	}

	// Set it to false again because we dont want to affect all other cammeras.
	private void OnPostRender()
	{
/// There is a chance that exists where the URP package may exist in a project but may not be getting utilized, this can be denoted by the pressence of pipeline asset in Graphics Settings
/// In these cases BIRP will be used, and we want to make sure the Graphics Settings currentRenderPipeline is null before inverting culling 
#if UNITY_URP
        if(GraphicsSettings.currentRenderPipeline == null)
#endif
	    	GL.invertCulling = false;
	}
	/// END BUILT IN RENDER PIPELINE LOGIC
#endif
}