using System.Collections.Generic;
using UnityEngine;

namespace SpaceRTSKit
{
	/// <summary>
	/// Component that will change progressively the sharedMaterials of all of its childrens to
	/// produce the effect of being built step by step according to the progress value.
	/// </summary>
	public class FXUnderConstruction : MonoBehaviour
	{
		/// <summary>
		/// Base class structure to hold the data for each MeshRendererand its materials that will be changed.
		/// </summary>
		[System.Serializable]
		public class MeshDataInfo
		{
			/// <summary>
			/// Symbolic name for the mesh. usually will be the GameObject name of the renderer.
			/// </summary>
			public string name;
			/// <summary>
			/// Mesh renderer owner of the materials that will be changed.
			/// </summary>
			public MeshRenderer renderer;
			/// <summary>
			/// The saved list of material of the MeshRenderer, needed for the complete restoration of each material.
			/// </summary>
			public List<Material> materials = new List<Material>();
		}

		/// <summary>
		/// Current build progress. Value range between (0, 1)
		/// </summary>
		[ContextMenuItem("Test", "Refresh")]
		[Range(0f,1f)]
		public float progress = 1.0f;

		/// <summary>
		/// Material to use when unit part is not built yet.
		/// </summary>
		public Material constructMaterial;
		/// <summary>
		/// List of meshrenderers that contains the materials to change.
		/// Deprecated. Use CollectedInfo instead.
		/// </summary>
		[Tooltip("Renderers to change. Use scan in Childs context menu.")]
		[HideInInspector]
		[System.Obsolete("Use CollectedInfo instead.")]
		public List<MeshRenderer> renderers = new List<MeshRenderer>();
		/// <summary>
		/// The list of materials that will be changed
		/// Deprecated. Use CollectedInfo instead.
		/// </summary>
		[Tooltip("materials to change. Use scan in Childs context menu.")]
		[HideInInspector]
		[System.Obsolete("Use CollectedInfo instead.")]
		public List<Material> materials = new List<Material>();

		[Header("MeshData In Childs")]
		public bool scanAllDownInHierarchy = true;
		public List<MeshDataInfo> collectedInfo = new List<MeshDataInfo>();

		void Start()
		{
			Refresh();
		}

		private void OnValidate()
		{
			TransferOldMethodToMeshDataInfo();
		}

		/// <summary>
		/// Sets the current progress and refresh the materials to reflect that.
		/// </summary>
		/// <param name="newProgress"></param>
		public void SetProgress(float newProgress)
		{
			newProgress = Mathf.Clamp01(newProgress);
			if(newProgress != progress)
			{
				progress = newProgress;
				Refresh();
			}
		}

		[ContextMenu ("Scan In Childs")]
		void ScanMeshDataInChilds ()
		{
			collectedInfo.Clear();
			ScanMeshDataInChilds(transform);
		}

		private void ScanMeshDataInChilds(Transform parent)
		{
			for( int i=0; i<parent.childCount; i++)
			{
				Transform tr = parent.GetChild(i);
				if(tr==null)
					continue;

				if (scanAllDownInHierarchy)
					ScanMeshDataInChilds(tr);

				MeshRenderer mr = tr.GetComponent<MeshRenderer>(); 
				if(mr==null)
					continue;

				MeshDataInfo mdi = new MeshDataInfo();
				mdi.name = tr.name;
				mdi.renderer = mr;
				mdi.materials.AddRange(mr.sharedMaterials);
				collectedInfo.Add(mdi);
			}
		}

		[ContextMenu ("Test")]
		void Refresh()
		{
			// We need to see at least one piece
			int materialsToChange = Mathf.FloorToInt( progress * GetMaterialsCount() );
			foreach(MeshDataInfo mdi in collectedInfo)
			{
				if(mdi.renderer == null)
					continue;

				Material [] newMaterials = new Material[mdi.renderer.sharedMaterials.Length];
				for(int i=0; i<mdi.renderer.sharedMaterials.Length; i++)
				{
					if(i<mdi.materials.Count)
						newMaterials[i] = materialsToChange>0 ? mdi.materials[i] : constructMaterial;
					materialsToChange--;
				}
				mdi.renderer.sharedMaterials = newMaterials;
			}
		}

		int GetMaterialsCount()
		{
			int result = 0;
			foreach(MeshDataInfo mdi in collectedInfo)
			{
				if( mdi.renderer == null )
					continue;
				result += mdi.materials.Count;
			}
			return result;
		}

		#pragma warning disable 618
		private void TransferOldMethodToMeshDataInfo()
		{
			if(renderers.Count != 0 && collectedInfo.Count == 0)
			{
				for(int i=0; i<renderers.Count; i++)
				{
					if(renderers[i]==null || materials[i] == null)
						continue;

					MeshDataInfo mdi = new MeshDataInfo();
					mdi.name = renderers[i].name;
					mdi.renderer = renderers[i];
					mdi.materials.Add(materials[i]);
					collectedInfo.Add(mdi);
				}
				renderers.Clear();
				materials.Clear();
			}
		}
		#pragma warning restore 618
	}
}