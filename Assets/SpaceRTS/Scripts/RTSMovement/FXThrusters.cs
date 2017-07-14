using System.Collections.Generic;
using UnityEngine;

namespace SpaceRTSKit
{
	/// <summary>
	/// FX component that will handle the thrusters glowing rate according to the progress value.
	/// </summary>
	public class FXThrusters : MonoBehaviour
	{
		/// <summary>
		/// Progress value indicating the current glowing intensity where zero will be the 
		/// minAlpha value and one will be the maxAlpha value.
		/// </summary>
		[Range(0.0f, 1.0f)]
		public float progress = 1.0f;

		/// <summary>
		/// List of MeshRenderers that will be controlled by this component.
		/// </summary>
		public List<MeshRenderer> haloRenderers = new List<MeshRenderer>();
		/// <summary>
		/// Name of the material property that holds the color to change accordiing to the progress value.
		/// </summary>
		public string shaderColorProperty = "_TintColor";

		/// <summary>
		/// Minimum alpha value that wll have the material (when progress is zero).
		/// </summary>
		[Range(0.0f, 1.0f)]
		public float minAlpha = 0.1f;
		/// <summary>
		/// Maximum alpha value that will have the material (when progress is one).
		/// </summary>
		[Range(0.0f, 1.0f)]
		public float maxAlpha = 1.0f;

		private List<Material> haloMaterials = new List<Material>();
		private Vector3 lastForward = Vector3.zero;

		// Use this for initialization
		void Start ()
		{
			CollectMaterials();
		}

		/// <summary>
		/// Sets the progress and refresh all the halo materials.
		/// </summary>
		/// <param name="amount"></param>
		public void RefreshIntensity(float amount)
		{
			progress = Mathf.Clamp01(amount);
			RefreshHalosIntensity();
		}
	
		/// <summary>
		/// Refresh all the materials to reflect the new glowing intensity value.
		/// </summary>
		[ContextMenu("Test")]
		public void RefreshHalosIntensity()
		{
			float intensity = minAlpha + (maxAlpha - minAlpha) * progress;

			foreach (Material mat in haloMaterials)
			{
				Color currColor = mat.GetColor(shaderColorProperty);
				currColor.a = intensity;
				mat.SetColor(shaderColorProperty, currColor);
			}
		}

		/// <summary>
		/// Turns all the halo transforms to face the camera.
		/// (Must be called each frame)
		/// </summary>
		[ContextMenu("RefreshHalosLookAt")]
		public void RefreshCameraLookAt()
		{
			if( haloRenderers.Count == 0 || Camera.main == null )
				return;

			lastForward = Camera.main.transform.forward;
			foreach( MeshRenderer render in haloRenderers)
				render.transform.forward = -lastForward;
		}

		[ContextMenu("FillHaloRenderersWithChilds")]
		private void FillHaloRenderersWithChilds()
		{
			haloRenderers.Clear();
			for(int i=0; i<transform.childCount; i++)
			{
				Transform child = transform.GetChild(i);
				if(child.name.ToLower().Contains("halo") )
				{
					MeshRenderer render = child.GetComponent<MeshRenderer>();
					if(render)
						haloRenderers.Add(render);
				}
			}
		}

		[ContextMenu("CollectMaterials")]
		private void CollectMaterials()
		{
			Dictionary<Material, Material> instancedMaterials = new Dictionary<Material, Material>();
			foreach(MeshRenderer render in haloRenderers)
			{
				List<Material> newSharedMaterials = new List<Material>();
				foreach( Material source in render.sharedMaterials)
				{
					if(source == null)
						continue;
					Material instancedMaterial = null;
					if( !instancedMaterials.TryGetValue(source, out instancedMaterial) )
					{
						instancedMaterial = new Material(source);
						instancedMaterials.Add(source, instancedMaterial);
					}
					newSharedMaterials.Add(instancedMaterial);
				}
				render.sharedMaterials = newSharedMaterials.ToArray();
			}
			HashSet<Material> uniqueMaterials = new HashSet<Material>(instancedMaterials.Values);
			haloMaterials = new List<Material>(uniqueMaterials);
		}
	}
}
