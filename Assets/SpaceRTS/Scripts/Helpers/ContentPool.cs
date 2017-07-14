using System.Collections.Generic;
using UnityEngine;

namespace GameBase.Helpers
{
	/// <summary>
	/// Helper class to maintain a constant buffer of GameObjects ready to use, and reuse.
	/// </summary>
	public class ContentPool : MonoBehaviour
	{
		/// <summary>
		/// GameObject to use as a template for the buffer.
		/// </summary>
		public GameObject template;
		/// <summary>
		/// Transform container for the instanced and alive GameObjects
		/// </summary>
		[ContextMenuItem("Use this transform", "UseThisTransformAsAlivesContainer")]
		public Transform alivesContainer;
		/// <summary>
		/// Transform container for the unused GameObjects
		/// </summary>
		[ContextMenuItem("Use this transform", "UseThisTransformAsBufferContainer")]
		public Transform bufferContainer;
		/// <summary>
		/// Minimal amount of created GameObjects for the buffer
		/// </summary>
		public int minBuffer = 10;
		/// <summary>
		/// If the quantity of requested game object is reached, the buffer 
		/// will expand by this quantity.
		/// </summary>
		public int expandBuffer = 4;

		[ContextMenuItem("Fill with Childs", "FillWithChilds")]
		public List<GameObject> buffer = new List<GameObject>();

		// Use this for initialization
		void Start ()
		{
			if(template !=null && buffer.Count < minBuffer)
				ExpandBuffer(minBuffer - buffer.Count);
		}

		/// <summary>
		/// Takes an element from the buffer and returns it. if there is no more
		/// elements in the buffer an expansion will be produced.
		/// </summary>
		/// <returns>The usable GameObject returned by the buffer.</returns>
		public GameObject Instantiate()
		{
			if(buffer.Count == 0)
				ExpandBuffer(expandBuffer);
			return BufferPop();
		}

		/// <summary>
		/// Returns the GameObject to the buffer.
		/// </summary>
		/// <param name="itemToDestroy">The GameObject to be returned to the buffer.</param>
		public void Destroy(GameObject itemToDestroy)
		{
			BufferPush(itemToDestroy);
		}

		/// <summary>
		/// Returns all the GameObjects to the Buffer
		/// </summary>
		/// <param name="itemsToDestroy">The list of GameObjects to return to the Buffer.</param>
		public void Destroy(IEnumerable<GameObject> itemsToDestroy)
		{
			foreach(GameObject itemToDestroy in itemsToDestroy)
				BufferPush(itemToDestroy);
		}

		#region Helper Methods

		private void ExpandBuffer(int count)
		{
			for (int i = 0; i < count; i++)
				buffer.Add(InstantiateFromTemplate(bufferContainer));
		}

		private GameObject BufferPop()
		{
			GameObject result = null;
			result = buffer[0];
			buffer.RemoveAt(0);
			result.transform.SetParent(alivesContainer);
			result.SetActive(true);
			return result;
		}

		private void BufferPush(GameObject toPush)
		{
			toPush.SetActive(false);
			toPush.transform.SetParent(bufferContainer);
			buffer.Add(toPush);
		}

		private GameObject InstantiateFromTemplate(Transform parent)
		{
			return GameObject.Instantiate<GameObject>(template, parent);
		}

		private void UseThisTransformAsBufferContainer()
		{
			bufferContainer = this.transform;
		}

		private void UseThisTransformAsAlivesContainer()
		{
			alivesContainer = this.transform;
		}

		private void FillWithChilds()
		{
			for( int i=0; i<transform.childCount;i++)
				buffer.Add(transform.GetChild(i).gameObject);
		}

		#endregion Helper Methods
	}

}