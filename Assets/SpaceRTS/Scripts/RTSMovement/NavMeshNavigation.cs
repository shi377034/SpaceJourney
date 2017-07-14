using UnityEngine;
using UnityEngine.AI;

namespace SpaceRTSKit
{
	/// <summary>
	/// Specialization of the Navigation component that allows full control of the NavMeshAgent.
	/// This component will use the pathfinding provided by the navmesh system and also the 
	/// avoidance capabilitys.
	/// </summary>
	[RequireComponent(typeof(NavMeshAgent))]
	public class NavMeshNavigation : Navigation
	{
		private NavMeshAgent navMeshAgent = null;

		protected override void Start()
		{
			base.Start();
			RefreshNavMeshAgentData();
		}

		/// <summary>
		/// Called when the Visual Module is setted. Here we need to initialize all the component related functionality.
		/// </summary>
		public override void OnVisualModuleSetted()
		{
			base.OnVisualModuleSetted();
			RefreshNavMeshAgentData();
		}

		/// <summary>
		/// Called when the Visual Module is removed. Here we need to remove all the component references 
		/// from the visual module because we are no longer in control of that components.
		/// </summary>
		public override void OnVisualModuleRemoved()
		{
			base.OnVisualModuleRemoved();
			navMeshAgent = null;
		} 

		/// <summary>
		/// Called after a EngageMovement order.
		/// Override to add special behaviours.
		/// </summary>
		/// <param name="destination">The target point in world coordinates where to move the unit.</param>
		protected override void OnMovementOrderEngage(Vector3 destination)
		{
			if(navMeshAgent != null)
			{
				navMeshAgent.SetDestination(destination);
				navMeshAgent.isStopped = true;
				navMeshAgent.updateRotation = false;
			}
		}

		/// <summary>
		/// Called when a full stop order was given to this navigation.
		/// Override to add special behaviours.
		/// </summary>
		protected override void OnMovementOrderStop()
		{
			if( navMeshAgent != null )
				navMeshAgent.isStopped = true;
		}

		/// <summary>
		/// Determines whether the movement order is completed because the unit has reached the target position.
		/// </summary>
		/// <returns>True if the target position was reached by this unit.</returns>
		public override bool IsFinalDestinationReached()
		{
			if( navMeshAgent == null )
				return false;
			if( navMeshAgent.pathPending )
				return false;
			if(navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance)
				return false;
			return true;
		}

		/// <summary>
		/// Handles the unit movement. returns true when target destination is reached.
		/// </summary>
		protected override void OnMovingToDestination()
		{
			if( !navMeshAgent.pathPending && navMeshAgent.pathStatus != NavMeshPathStatus.PathComplete)
			{
				StopMovement();
				return;
			}
			Vector3 totalMoveVector = navMeshAgent.steeringTarget - BasePosition;
			Quaternion targetLookAt = totalMoveVector != Vector3.zero ? Quaternion.LookRotation(totalMoveVector.normalized) : transform.rotation;
			Vector3 movement = Advance(targetLookAt, moveConfig.accel, false);
			navMeshAgent.Move(movement);
			DoRoll(targetLookAt);
		}

		/// <summary>
		/// Initializes the navmesh agent with the same configuration that has the navigation.
		/// </summary>
		[ContextMenu("Refresh NavMeshAgent Data")]
		public void RefreshNavMeshAgentData()
		{
			navMeshAgent = GetComponent<NavMeshAgent>();
			if(navMeshAgent != null)
			{
				navMeshAgent.speed = speed;
				navMeshAgent.angularSpeed = moveConfig.rotSpeed;
				navMeshAgent.acceleration = moveConfig.accel;
				navMeshAgent.stoppingDistance = stoppingDistance;
				//navMeshAgent.baseOffset = this.baseOffset;
				navMeshAgent.updateRotation = false;
			}
			else
				Debug.LogWarning("This component requires a NavMeshAgent attached to this GameObject.", this.gameObject);
		}

		/// <summary>
		/// Sets the transform position at the closest point to the navmesh.
		/// </summary>
		[ContextMenu("Locate At Closest NavMesh Point")]
		public void LocateAtClosestNavMeshPoint()
		{
			NavMeshHit hit;
			navMeshAgent = GetComponent<NavMeshAgent>();
			if( NavMesh.SamplePosition(transform.position, out hit, navMeshAgent.height * 4, navMeshAgent.areaMask) )
			{
				transform.position = hit.position + BaseOffset;
			}
		}
	}
}