using System;
using UnityEngine;
using GameBase.RTSKit;
using UnityEngine.AI;

namespace SpaceRTSKit
{
	/// <summary>
	/// Construction Handler that will let the user choose the next build location.
	/// Generally will be a semitransparent unit (like a ghost).
	/// </summary>
	public class BuildGhostControl : RTSSceneSystem
	{
		/// <summary>
		/// Reference to the scene bounds where to track the current cursor position.
		/// </summary>
		public SceneBounds sceneBounds;
		/// <summary>
		/// Indicates when a point is queried that must be inside the current nav mesh.
		/// </summary>
		public bool useNavMesh = false;

		/// <summary>
		/// Offset of the GameObject in relation to the CursorLookPoint.
		/// </summary>
		public float baseOffset;

		UnitConfig toBuild;
		GameObject visualGhost;
		Action<UnitConfig, Vector3, Vector3> onConfirmed;

		// Update is called once per frame
		void Update ()
		{
			if(toBuild==null)
				return;
			Vector3 onScenePosition = sceneBounds.CursorLookPoint;
			if(useNavMesh)
			{
				NavMeshHit hit;
				if( NavMesh.SamplePosition(onScenePosition, out hit, 1.0f, NavMesh.AllAreas) )
					onScenePosition = hit.position;
			}
			this.transform.position = onScenePosition + Vector3.up * baseOffset;
		}

		/// <summary>
		/// Has this system something to build?
		/// </summary>
		/// <returns></returns>
		public bool IsGhostRequested() { return toBuild != null; } 

		/// <summary>
		/// Setups the ghost with the unit info to build and a delegate to call when the build point is confirmed.
		/// </summary>
		/// <param name="toBuild">The UnitConfig that belongs to the unit to be constructed.</param>
		/// <param name="onConfirm">Callback delegate to be called once the build location is confirmed.</param>
		public void SetupGhost(UnitConfig toBuild, Action<UnitConfig, Vector3, Vector3> onConfirm)
		{
			if(toBuild!=null)
				ClearGhost();

			this.toBuild = toBuild;
			this.onConfirmed = onConfirm;
			this.visualGhost = GameObject.Instantiate<GameObject>(toBuild.placementPrefab);
			this.visualGhost.transform.parent = this.transform;
			this.visualGhost.transform.localPosition = Vector3.zero;
		}

		/// <summary>
		/// Enable or disable the ghost visualization of the current setted unit.
		/// </summary>
		/// <param name="enabled"></param>
		public void Show(bool enabled)
		{
			if(visualGhost)
				visualGhost.SetActive(enabled);
		}

		/// <summary>
		/// Confirms the current ghost location as the position where to build the unit. Also calls the previously registered delegate.
		/// </summary>
		public void Confirm()
		{
			this.onConfirmed(toBuild, this.transform.position, this.transform.forward);
			ClearGhost();
		}

		/// <summary>
		/// Cancels the ghost
		/// </summary>
		public void Cancel()
		{
			ClearGhost();
		}

		/// <summary>
		/// Clears the ghost, canceling the process.
		/// </summary>
		private void ClearGhost()
		{
			toBuild = null;
			//builder = null;
			if(visualGhost)
			{
				GameObject.Destroy(visualGhost);
				visualGhost = null;
			}
		}
	}
}