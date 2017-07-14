using GameBase;
using SpaceRTSKit.Messages;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SpaceRTSKit
{
	/// <summary>
	/// GameEntityComponent that gives to the GameEntity the property of been able to build buildable entities.
	/// </summary>
	[RequireComponent(typeof(RTSEntity))]
	public class Builder : GameEntityComponent
	{
		/// <summary>
		/// Struct info for each build item in the queue
		/// </summary>
		protected struct BuildItem
		{
			public UnitConfig toBuild;
			public Vector3 pos;
			public Vector3 dir;
			public BuildItem(UnitConfig config, Vector3 pos, Vector3 dir)
			{
				this.toBuild = config;
				this.pos = pos;
				this.dir = dir;
			}
		}
		/// <summary>
		/// Name for the control point defined in the ComponentProxy in the VisualModule.
		/// Defines the build position.
		/// </summary>
		[Tooltip("ControlPoint name defined in ComponentProxy with the build position.")]
		public string buildPointName = "cp_spawn";
		/// <summary>
		/// Name for the control point defined in the ComponentProxy in the VisualModule.
		/// Defines the expel position.
		/// </summary>
		[Tooltip("ControlPoint name defined in ComponentProxy with the expel position.")]
		public string expelPointName = "cp_buildout";
		/// <summary>
		/// Name for the control point defined in the ComponentProxy in the VisualModule.
		/// Defines the rally point.
		/// </summary>
		[Tooltip("ControlPoint name defined in ComponentProxy with the rally point.")]
		public string rallyPointName = "cp_rallypoint";
		/// <summary>
		/// The transform holding the buildable spawn position.
		/// </summary>
		[Tooltip("Buildable spawn position.")]
		public Transform buildLocation;
		/// <summary>
		///The transform holding the position where the buildable will be expelled once finished.
		/// </summary>
		[Tooltip("Expel point once the build is complete.")]
		public Transform buildExpelPoint;
		/// <summary>
		/// The transform holding the rally point position for the builded units once expelled.
		/// </summary>
		[Tooltip("Rally point for the builded units once expelled.")]
		public Transform rallyPoint;
		/// <summary>
		/// Indicates that expel and rally point must be inside of the nav mesh.
		/// </summary>
		[Tooltip("Indicates that expel and rally point must be inside of the nav mesh.")]
		public bool useNavMesh;

		private Buildable target;
		private PlaceMarker moveMarker;
		private bool started = false;

		/// <summary>
		/// Build queue holding the list of units to build.
		/// </summary>
		protected List<BuildItem> buildQueue = new List<BuildItem>();

		/// <summary>
		/// has a buildable target assigned?
		/// </summary>
		public bool HasTarget { get { return target != null; } }
		/// <summary>
		/// RTSEntity attached to this GameObject's component.
		/// </summary>
		public RTSEntity ThisRTSEntity { get { return ThisEntity as RTSEntity; } }
		/// <summary>
		/// UnitConfig that belongs to this GameEntity.
		/// </summary>
		public UnitConfig Config { get { return ThisRTSEntity.Config; } }
		/// <summary>
		/// This builder requires a location for each build?
		/// </summary>
		public bool RequiresBuildLocation { get { return ThisRTSEntity.Config.requiresBuildLocation; } }
		/// <summary>
		/// This builder must delete the builded unit when interrupted.
		/// </summary>
		public bool RemoveOnInterrupt { get { return ThisRTSEntity.Config.removeOnInterrupt; } }
		/// <summary>
		/// Returns the position where the units will be built in world coordinates.
		/// </summary>
		public Vector3 BuildLocation { get { return buildLocation.position; } }
		/// <summary>
		/// Returns the position in world coordinates where the units will be expelled once built.
		/// </summary>
		public Vector3 BuildExpelLocation { get { return buildExpelPoint.position; } }
		/// <summary>
		/// The position of the rally point that will be used by the units once completed. 
		/// </summary>
		public Vector3 RallyPointPosition { get { return rallyPoint.position; } }
		/// <summary>
		/// Offset from the Builder position to the rally point.
		/// Deprecated. Use BuildExpelLocation instead.
		/// </summary>
		[System.Obsolete("Use BuildExpelLocation instead.")]
		public Vector3 RallyPointOffset { get { return Vector3.zero; } }
		/// <summary>
		/// reference to the RTScene.
		/// </summary>
		public RTSScene Scene { get { return ThisEntity.GameScene as RTSScene; } }

		public int BuildsInQueue { get { return buildQueue.Count; } }

		/// <summary>
		/// Enumerates the units to build in queue.
		/// </summary>
		public IEnumerable<UnitConfig> QueuedUnits 
		{
			get
			{
				foreach(BuildItem item in buildQueue)
					yield return item.toBuild;
				yield break;
			}
		}

		protected void Start()
		{
			ResetLocationPoints();
		}

		private void Update()
		{
			if(target==null)
				return;
			bool inPlace = IsBuilderAtCorrectDistance(target);
			if(!started && inPlace)
				started = true;
			if(started && !inPlace)
				CurrentBuildInterrupted();
			if(started)
				target.ChangeProgress(Time.deltaTime);
		}

		/// <summary>
		/// Called when the Visual Module is setted. Here we need to initialize all the component related functionality.
		/// </summary>
		override public void OnVisualModuleSetted()
		{
			ResetLocationPoints();

			Transform trIn = VisualProxy.GetPropertyValue<Transform>(buildPointName);
			Transform trOut = VisualProxy.GetPropertyValue<Transform>(expelPointName);
			Transform trRally = VisualProxy.GetPropertyValue<Transform>(rallyPointName);

			if(trIn!=null)
				buildLocation.position = trIn.position;
			if(trOut!=null)
				buildExpelPoint.position = trOut.position;
			if(trRally!=null)
				rallyPoint.position = trRally.position;
			else
				rallyPoint.position = buildExpelPoint.position;

			if(useNavMesh)
			{
				NavMeshHit hit;
				if( NavMesh.SamplePosition(buildExpelPoint.position, out hit, 5.0f, NavMesh.AllAreas) )
					buildExpelPoint.position = hit.position;
				if( NavMesh.SamplePosition(rallyPoint.position, out hit, 5.0f, NavMesh.AllAreas) )
					rallyPoint.position = hit.position;
			}

			//NavMeshHit hit;
			//// Returns a valid point in the nav mesh to move along when the construction is finished.
			//if( NavMesh.SamplePosition(BuildExpelLocation, out hit, 1.0f, NavMesh.AllAreas) )


			moveMarker = Scene.placeMarker;
		}

		/// <summary>
		/// Called when the Visual Module is removed. Here we need to uninitialize all the component related functionality.
		/// </summary>
		override public void OnVisualModuleRemoved()
		{
		}

		/// <summary>
		/// Adds a Unit of type UnitConfig to the build queue.
		/// </summary>
		/// <param name="toBuild">Unit tye to be builded.</param>
		/// <param name="location">location where be built the unit.</param>
		/// <param name="dir">Face direction that will have the builded unit once completed.</param>
		public void AddUnitToBuild(UnitConfig toBuild, Vector3 location, Vector3 dir)
		{
			if(RequiresBuildLocation && buildQueue.Count == 1)
				CancelCurrentBuild();

			buildQueue.Add(new BuildItem(toBuild, location, dir));
			if(!started)
				CheckNextBuild();
			try
			{
				Messages.Dispatch(new BuildQueueChanged(this));
			}
			catch(System.Exception e)
			{
				Debug.LogException(e, this.gameObject);
			}
		}

		/// <summary>
		/// If there's a item in the build queue then start its construction.
		/// </summary>
		private void CheckNextBuild()
		{
			if( buildQueue.Count == 0 )
				return;

			BuildItem current = buildQueue[0];
			// Prepares the rotation for the next builded object.
			Quaternion rotation = current.dir != Vector3.zero ? Quaternion.LookRotation(current.dir, Vector3.up) : transform.rotation;
			// The Buildable is the temporal object during its construction (with all the visual properties)
			// that later will be replaced with the real controller.
			Buildable buildable = CreateConstruct(current.toBuild, current.pos, rotation);
			SetupBuild(buildable);
		}

		/// <summary>
		/// Is this builder at the correct distance to work in the given buildable?
		/// </summary>
		/// <param name="buildable">the reference to the buildable to be checked.</param>
		/// <returns>true if the buildable is at the correct distance, otherwide false will be returned.</returns>
		public bool IsBuilderAtCorrectDistance(Buildable buildable)
		{
			float sqrDistance = (transform.position - buildable.transform.position).sqrMagnitude;
			float validDist = GetValidDistanceToBuild(buildable.BuildType);
			return sqrDistance <= (validDist * validDist);
		}

		/// <summary>
		/// Creates the Buildable object with visual module of the final object.
		/// This object should control the build process until it's finished.
		/// </summary>
		/// <param name="toBuild">The config info of the object to build.</param>
		/// <param name="location">The world coords of the object builded.</param>
		/// <param name="rotation">The rotation that will have the object during its build process.</param>
		/// <returns>The Buildable component of the object to build.</returns>
		internal Buildable CreateConstruct(UnitConfig toBuild, Vector3 location, Quaternion rotation)
		{
			RTSScene rtsScene = ThisEntity.GameScene as RTSScene;
			RTSEntity ge = GameObject.Instantiate<RTSEntity>(rtsScene.constructPrefab, location, rotation, this.transform.parent);
			ge.unitConfiguration = toBuild;
			ge.controller = ThisRTSEntity.controller;
			ge.ChangeVisualModule( ThisRTSEntity.CreateVisual(toBuild, ge.transform) );
			ge.gameObject.SetActive(true);
			return ge.GetComponent<Buildable>();
		}

		internal void SetupBuild(Buildable buildable)
		{
			target = buildable;
			buildable.SetBuilder(this);
			if(!RequiresBuildLocation)
				buildable.SetFinalMovePosition(BuildExpelLocation, RallyPointPosition);

			Navigation nav = GetComponent<Navigation>();
			if(nav)
			{
				Vector3 navPos, navDir;
				GetValidBuildLocation(buildable.transform.position, out navPos, out navDir );
				nav.PrepareToMove(navPos, navDir);
				nav.EngageMovement();

				if(moveMarker)
					moveMarker.ShowAt(navPos);
			}
		}

		private void ResetLocationPoints()
		{
			if (buildLocation == null)
			{
				buildLocation = (new GameObject()).transform;
				buildLocation.name = "BuildLocation";
				buildLocation.parent = this.transform;
				buildLocation.localPosition = Vector3.zero;
			}
			if (buildExpelPoint == null)
			{
				buildExpelPoint = (new GameObject()).transform;
				buildExpelPoint.name = "ExpelPoint";
				buildExpelPoint.parent = this.transform;
				buildExpelPoint.localPosition = Vector3.zero;
			}
			if (rallyPoint == null)
			{
				rallyPoint = (new GameObject()).transform;
				rallyPoint.name = "RallyPoint";
				rallyPoint.parent = this.transform;
				rallyPoint.localPosition = Vector3.zero;
			}
		}

		/// <summary>
		/// Cancels the current build.
		/// </summary>
		public void CancelCurrentBuild()
		{
			CancelBuildAt(0);
		}

		/// <summary>
		/// Removes the build item from the queue and cancel the build if index is zero.
		/// </summary>
		/// <param name="index"></param>
		public void CancelBuildAt(int index)
		{
			if(index==0)
			{
				if(RemoveOnInterrupt)
					GameObject.DestroyImmediate(target.gameObject);
				CurrentBuildInterrupted();
			}
			else
				RemoveQueueItemAt(index);
		}

		internal void BuildCompleted()
		{
			if(!RequiresBuildLocation && target)
				target.SetFinalMovePosition(BuildExpelLocation, RallyPointPosition);
			CurrentBuildInterrupted();
		}

		internal void CurrentBuildInterrupted()
		{
			if (target)
				target.OnBuilderWorkEnded();
			RemoveQueueItemAt(0);
			target = null;
			started = false;
			CheckNextBuild();
		}

		private void RemoveQueueItemAt(int index)
		{
			if (index < 0 || index >= buildQueue.Count)
				return;

			buildQueue.RemoveAt(index);
			try
			{
				Messages.Dispatch(new BuildQueueChanged(this));
			}
			catch (System.Exception e)
			{
				Debug.LogException(e, this.gameObject);
			}
		}

		internal float GetValidDistanceToBuild(UnitConfig toBuild)
		{
			return toBuild.radius + Config.buildDistance + Config.radius;
		}

		internal void GetValidBuildLocation(Vector3 buildPos, out Vector3 pos, out Vector3 dir)
		{
			dir = (buildPos - this.transform.position).normalized;
			float radius = GetValidDistanceToBuild(target.BuildType);
			pos = buildPos - dir * radius;
		}
	}
}
