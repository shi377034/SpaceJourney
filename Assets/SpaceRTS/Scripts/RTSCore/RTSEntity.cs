using GameBase;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SpaceRTSKit
{
	/// <summary>
	/// Implements GameEntity and provides extra functionality for the RTS.
	/// </summary>
	public class RTSEntity : GameEntity
	{
		/// <summary>
		/// Controller of this RTSEntity.
		/// </summary>
		public RTSPlayer controller;
		/// <summary>
		/// The UnitConfig of this RTSEntity
		/// </summary>
		public UnitConfig unitConfiguration;

		/// <summary>
		/// UnitConfig of this RTSEntity.
		/// </summary>
		public UnitConfig Config { get { return unitConfiguration; } }
		/// <summary>
		/// ShipConfig of this RTSEntity or null if it can't be casted.
		/// </summary>
		public ShipConfig ShipConfig { get { return unitConfiguration as ShipConfig; } }

		/// <summary>
		/// Registers the GameEntity in the RTSPlayer controller.
		/// Setup all the required components.
		/// </summary>
		protected override void Start()
		{
			base.Start();

			if (controller!=null)
				controller.RegisterGameEntity(this);

			SetupComponents();
		}

		/// <summary>
		/// Unregisters the GameEntity from the RTSPlayer controller.
		/// </summary>
		protected override void OnDestroy()
		{
			base.OnDestroy();
			if(controller!=null)
				controller.UnregisterGameEntity(this);
		}

		void SetupComponents()
		{
			// Setups the navigation component with the info provided by the UnitConfig.
			Navigation nav = GetComponent<Navigation>();
			if(nav && ShipConfig != null)
			{
				nav.moveConfig = ShipConfig.movementData;
				nav.speed = ShipConfig.movementData.maxSpeed;
			}
			// Setups the NavMeshAgent component with the info provided by the UnitConfig.
			NavMeshAgent navAgent = GetComponent<NavMeshAgent>();
			if(nav && ShipConfig != null)
			{
				navAgent.radius = ShipConfig.radius;
			}
		}

		/// <summary>
		/// Creates a VisualModule with the given config and attach it to the given parent.
		/// </summary>
		/// <param name="config">Config of the unit to instantiate.</param>
		/// <param name="parent">Transform that'll become the parent of the created VisualModule.</param>
		/// <returns>Returns the ComponentProxy component attached to the Root of the created VisualModule.</returns>
		public ComponentProxy CreateVisual(UnitConfig config, Transform parent)
		{
			GameObject goVisual = GameObject.Instantiate<GameObject>(config.visualPrefab);
			goVisual.transform.parent = parent;
			goVisual.name = config.userName;
			goVisual.transform.localPosition = Vector3.zero;
			goVisual.transform.localRotation = Quaternion.identity;
			return goVisual.GetComponent<ComponentProxy>();
		}

		[ContextMenu("CreateVisualModule")]
		public void CreateVisualModule()
		{
			if(visualModule != null )
				return;
			if(unitConfiguration==null)
				return;
			visualModule = CreateVisual(unitConfiguration, transform);
		}
	}
}