using GameBase;
using System.Collections.Generic;
using UnityEngine;
using SpaceRTSKit.UI;
using System;

namespace SpaceRTSKit
{
	/// <summary>
	/// Keeps track of each GameEntity belonging to the player 
	/// </summary>
	public class RTSPlayer : MonoBehaviour
	{
		/// <summary>
		/// Interface that must be implemented by the class that want to listen when a RTSEntity
		/// has is and out of the camera view frustrum.
		/// </summary>
		public interface IInViewListener
		{
			void BecomeInsideOfView(RTSEntity entity);
			void BecomeOutsideOfView(RTSEntity entity);
		}

		private List<RTSEntity> ownUnits = new List<RTSEntity>();
		// For each registered RTSEntity collects the ones that are within 
		// the camera look at position.
		private HashSet<RTSEntity> inViewUnits = new HashSet<RTSEntity>();

		private List<IInViewListener> inViewListeners = new List<IInViewListener>();

		/// <summary>
		/// The Collection of GameEntity that belongs to this player
		/// </summary>
		public IEnumerable<RTSEntity> Entities { get { return ownUnits; } }
		/// <summary>
		/// The Collection of GameEntity that belongs to this player and are currently
		/// inside of the camera view.
		/// </summary>
		public IEnumerable<RTSEntity> InViewEntities { get { return inViewUnits; } }

		/// <summary>
		/// Registration method for game entities in order to become controlled by this player.
		/// </summary>
		/// <param name="unitToRegister"></param>
		internal void RegisterGameEntity(RTSEntity unitToRegister)
		{
			ownUnits.Add(unitToRegister);
			CheckInViewStatus(unitToRegister, IsInsideView(unitToRegister, Camera.main));
		}

		/// <summary>
		/// Unregister method for game entities in order to become no longer controlled by this player.
		/// </summary>
		/// <param name="unitToUnregister"></param>
		internal void UnregisterGameEntity(RTSEntity unitToUnregister)
		{
			CheckInViewStatus(unitToUnregister, false);
			ownUnits.Remove(unitToUnregister);
		}

		/// <summary>
		/// Registration method for game entities in order to become controlled by this player.
		/// </summary>
		/// <param name="unitToRegister"></param>
		internal void RegisterInViewListener(IInViewListener listener)
		{
			inViewListeners.Add(listener);
		}

		/// <summary>
		/// Unregister method for game entities in order to become no longer controlled by this player.
		/// </summary>
		/// <param name="unitToUnregister"></param>
		internal void UnregisterInViewListener(IInViewListener listener)
		{
			inViewListeners.Remove(listener);
		}

		public void Update()
		{
			Camera cam = Camera.main;
			
			foreach(RTSEntity ent in ownUnits)
				CheckInViewStatus(ent, IsInsideView(ent, Camera.main));
		}

		private bool IsInsideView(RTSEntity ent, Camera cam)
		{
			Vector3 screenPoint = cam.WorldToViewportPoint(ent.transform.position);
			bool currOnScreen = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
			return currOnScreen;
		}

		private void CheckInViewStatus(RTSEntity ent, bool currentlyOnScreen)
		{
			bool prevOnScreen = inViewUnits.Contains(ent);
			if (!prevOnScreen && currentlyOnScreen)
			{
				inViewUnits.Add(ent);
				foreach (IInViewListener listener in inViewListeners)
					listener.BecomeInsideOfView(ent);
			}
			else if (prevOnScreen && !currentlyOnScreen)
			{
				inViewUnits.Remove(ent);
				foreach (IInViewListener listener in inViewListeners)
					listener.BecomeOutsideOfView(ent);
			}
		}
	}
}
