using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceRTSKit
{
	/// <summary>
	/// ScriptableObject that will be used as base configuration for every unit in the game.
	/// </summary>
	[CreateAssetMenu()]
	public class UnitConfig : ScriptableObject
	{
		/// <summary>
		/// Name to be displayed in the ui.
		/// </summary>
		[Header("Basic Configuration")]
		public string userName;
		/// <summary>
		/// Image to be displayed in the ui.
		/// </summary>
		public Sprite uiImage;
		/// <summary>
		/// Construction time of the unit.
		/// </summary>
		public float buildTime;

		/// <summary>
		/// Prefab to be used when creating new units of this type. This will be the gameplay part of the unit.
		/// </summary>
		[Header("Visual Configuration")]
		public GameObject gameplayPrefab;
		/// <summary>
		/// Prefab to be used when creating new units of this type. This will be the VisualModule part of the unit.
		/// </summary>
		public GameObject visualPrefab;
		/// <summary>
		/// Prefab to be used by the GhostControl component when the builders requires a location for this structure
		/// when trying to build it.
		/// </summary>
		public GameObject placementPrefab;
		/// <summary>
		/// Base radius of the unit to prevent collisions.
		/// </summary>
		public float radius = 2.4f;

		/// <summary>
		/// List of Units that this builder is capable to build. Must stay without units if the unit isn't a builder.
		/// </summary>
		[Header("Buildable Units")]
		public List<UnitConfig> buildables = new List<UnitConfig>();
		/// <summary>
		/// Tells to the system that this unit requires a build location to start building a unit.
		/// </summary>
		public bool requiresBuildLocation = false;
		/// <summary>
		/// Tells to the system that the buinding unit must be removed if this builder is interrupted on its work.
		/// </summary>
		public bool removeOnInterrupt = true;
		/// <summary>
		/// The minimum distance required to start working on a buildable unit.
		/// </summary>
		public float buildDistance = 2.0f;
	}
}