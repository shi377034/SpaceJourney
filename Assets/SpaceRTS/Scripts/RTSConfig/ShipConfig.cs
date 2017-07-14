using SpaceRTSKit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceRTSKit
{
	/// <summary>
	/// UnitConfig implementation for ships units (capable of movement)
	/// </summary>
	[CreateAssetMenu()]
	public class ShipConfig : UnitConfig
	{
		/// <summary>
		/// Configuration for the ships movement
		/// </summary>
		[Header("Movement")]
		public Navigation.Data movementData;
	

	}
}