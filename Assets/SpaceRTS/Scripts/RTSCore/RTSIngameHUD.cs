using System;
using System.Collections.Generic;
using UnityEngine;
using GameBase.Helpers;

namespace SpaceRTSKit.UI
{
	/// <summary>
	/// Controls all the InGame HUD.
	/// Currently we have only the Construction progress bars to show but the idea is to place here 
	/// all the additional HUD overlays, like the units life when the cursor is over a unit, etc.
	/// </summary>
	public class RTSIngameHUD : MonoBehaviour, RTSPlayer.IInViewListener
	{
		/// <summary>
		/// Reference to the user's player controller wich has all its own entities information.
		/// </summary>
		public RTSPlayer mainPlayer;
		/// <summary>
		/// ContentPool reference for the progress bar buffer.
		/// </summary>
		public ContentPool buildablesProgress;
		/// <summary>
		/// Max distance to the camera where the progress bar is allowed to be seen.
		/// </summary>
		public float visibilityRange = 100.0f;

		Dictionary<Buildable, UIProgressBar> inScreenBuildables = new Dictionary<Buildable, UIProgressBar>();

		// Use this for initialization
		void Start ()
		{
			mainPlayer.RegisterInViewListener(this);
			foreach( RTSEntity ent in mainPlayer.InViewEntities)
				BecomeInsideOfView(ent);
		}

		private void OnDestroy()
		{
			mainPlayer.UnregisterInViewListener(this);
		}

		private void LateUpdate()
		{
			// Refresh the progress bar screen positions and information.
			foreach(KeyValuePair<Buildable, UIProgressBar> pair in inScreenBuildables)
			{
				Builder builder = pair.Key.Builder;
				if(builder==null || !builder.IsBuilderAtCorrectDistance(pair.Key))
				{
					pair.Value.gameObject.SetActive(false);
					continue;
				}
				float radius_world = builder.Config.radius;
				float dist = Vector3.Distance(pair.Key.transform.position, Camera.main.transform.position);
				if(dist > visibilityRange)
				{
					pair.Value.gameObject.SetActive(false);
					continue;
				}
				float frustumHeight = 2.0f * dist * Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);
				float radius_pixels = radius_world * Screen.height / frustumHeight;
				pair.Value.name = pair.Key.BuildType.name + radius_pixels;
				pair.Value.transform.position = Camera.main.WorldToScreenPoint(builder.transform.position);
				pair.Value.transform.position += Vector3.up * radius_pixels;
				pair.Value.SizeDelta = new Vector2(radius_pixels * 2, pair.Value.SizeDelta.y);
				pair.Value.FillAmount = pair.Key.GetProgress();
				pair.Value.gameObject.SetActive(true);
			}
		}

		/// <summary>
		/// Called when a RTSEntity becomes inside of the current camera view frustrum.
		/// </summary>
		/// <param name="entity">The entity that is now inside of the camera view frustrum.</param>
		public void BecomeInsideOfView(RTSEntity entity)
		{
			Buildable buildable = entity.GetComponent<Buildable>();
			if(buildable)
				AddInScreenBuildable(buildable);
		}

		/// <summary>
		/// Called when a RTSEntity becomes outside of the current camera view frustrum.
		/// </summary>
		/// <param name="entity">The entity that is now outside of the camera view frustrum.</param>
		public void BecomeOutsideOfView(RTSEntity entity)
		{
			Buildable buildable = entity.GetComponent<Buildable>();
			if(buildable)
				RemoveInScreenBuildable(buildable);
		}

		private void AddInScreenBuildable(Buildable buildable)
		{
			UIProgressBar uiBar = buildablesProgress.Instantiate().GetComponent<UIProgressBar>();
			uiBar.FillAmount = buildable.GetProgress();
			inScreenBuildables.Add(buildable, uiBar);
		}

		private void RemoveInScreenBuildable(Buildable buildable)
		{
			UIProgressBar uiBar = null;
			if( !inScreenBuildables.TryGetValue(buildable, out uiBar) )
				return;
			buildablesProgress.Destroy(uiBar.gameObject);
			inScreenBuildables.Remove(buildable);
		}
	}
}