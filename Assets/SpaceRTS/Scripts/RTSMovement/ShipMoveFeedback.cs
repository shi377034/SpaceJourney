using GameBase;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace SpaceRTSKit
{
	/// <summary>
	/// Handles the movement feedback for the unit. in this case, the thrusters glowing.
	/// </summary>
	[RequireComponent(typeof(Navigation))]
	public class ShipMoveFeedback : GameEntityComponent
	{
		/// <summary>
		/// Name of the property in the visual module that access the FXThrusters component
		/// </summary>
		public string thrusterProperty = "thrusters";

		//public 
		private Navigation nav;
		private FXThrusters thrusters;

		// Use this for initialization
		void Start ()
		{
			nav = GetComponent<Navigation>();
		}

		/// <summary>
		/// overrided implementation to configure the component with the VisualModule properties.
		/// </summary>
		public override void OnVisualModuleSetted()
		{
			thrusters = VisualProxy.GetPropertyValue<FXThrusters>(thrusterProperty);
			base.OnVisualModuleSetted();
		}

		/// <summary>
		/// Removes all references to the VisualModule.
		/// </summary>
		public override void OnVisualModuleRemoved()
		{
			thrusters = null;
			base.OnVisualModuleRemoved();
		}

		// Update is called once per frame
		void LateUpdate ()
		{
			if(nav == null || thrusters == null)
				return;

			thrusters.RefreshCameraLookAt();
			thrusters.RefreshIntensity(nav.CurrentSpeed.magnitude / nav.moveConfig.maxSpeed);
		}
	}
}
