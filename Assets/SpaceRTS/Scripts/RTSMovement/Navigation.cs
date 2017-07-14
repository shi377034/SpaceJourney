using UnityEngine;
using GameBase;

namespace SpaceRTSKit
{

	/// <summary>
	/// GameEntityComponent in control of the unit movement.
	/// </summary>
	public class Navigation : GameEntityComponent
	{
		public const float AngularSleepThreshold = 2.0f;

		private enum MoveOrder
		{
			None,
			PreparingToMove,
			GoToTargetDestination,
			LookAtFinalDirection,
			StoppingAllMovement,
		}

		/// <summary>
		/// Movement configuration for the Navigation component
		/// </summary>
		[System.Serializable]
		public class Data
		{
			/// <summary>
			/// Top speed of the unit.
			/// </summary>
			[Header("Steering")]
			public float maxSpeed = 6f;
			/// <summary>
			/// Rotation speed of the unit.
			/// </summary>
			public float rotSpeed = 80f;
			/// <summary>
			/// Acceleration of the unit
			/// </summary>
		    public float accel = 6f;
			/// <summary>
			/// How great must be the rotation arc when the unit needs to turn the direction.
			/// </summary>
			public float maneuverFactor = 0.1f;
			/// <summary>
			/// pseudo friction applied to the unit when is not accelerated to achieve a slow down effect.
			/// </summary>
			[Range(0.0f,1.0f)]
			public float fullStopFactor = 0.95f;
			/// <summary>
			/// Roll effect factor to be applied when the unit turns (hovercraft turn effect).
			/// Requires a Transform called "roll_body" in the ComponentProxy of the VisualModule.
			/// </summary>
			[Range(0.0f,1.0f)]
			public float rollFactor = 0.8f;
			/// <summary>
			/// Angular speed for roll axis rotation.
			/// </summary>
			[Tooltip("Angular speed for roll axis rotation.")]
			public float rollSpeed = 70.0f;
		}
		
		/// <summary>
		/// The relative vertical displacement of the owning GameObject.
		/// </summary>
		[Tooltip("The relative vertical displacement of the owning GameObject.")]
		public float baseOffset = 0.0f;
		/// <summary>
		/// Reference to the child GameObject that'll be used as destination feedback marker.
		/// Must contain a MeshRenderer in order to make it visible or hide it when needed.
		/// </summary>
		public GameObject moveTargetMarker;
		/// <summary>
		/// Square radius of the destination area where the destination target can be considered as reached.
		/// Deprecated. Use stoppingDistance instead.
		/// </summary>
		[System.Obsolete("Use stoppingDistance instead.")]
		[Tooltip("Deprecated. Use StoppingDistance instead.")]
		[HideInInspector]
		public float sqrTargetingPrecision = 1.0f;

		/// <summary>
		/// The movement speed that will be used by this navigation. by default, speed
		/// will be the max speed (defined in moveConfig.maxSpeed). but you can set with this a 
		/// slower velocity or a boost for a short time, etc.
		/// </summary>
		public float speed = 10.0f;
		/// <summary>
		/// Movement configuration to be used by this component.
		/// </summary>
		public Data moveConfig;
		/// <summary>
		/// Stop within this distance from the target position.
		/// </summary>
		[Tooltip("Stop within this distance from the target position.")]
		public float stoppingDistance = 1.0f;

		private Transform moveMarkerTR;
		private MeshRenderer moveMarkerMR;
		private Transform bodyToRoll;
		private Vector3 targetPosition;
		private Vector3 preparePos;
		private Quaternion targetLookAt;
		private MoveOrder currentMoveOrder = MoveOrder.None;

		protected Vector3 moveSpeed = Vector3.zero;
		private float lastAccel = 0.0f;

		public Vector3 BasePosition { get { return transform.position - Vector3.up * baseOffset; } }
		public Vector3 BaseOffset { get { return Vector3.up * baseOffset; } }
		public Vector3 CurrentSpeed { get { return moveSpeed; } }
		public float LastAcceleration { get { return lastAccel; } }

		// Use this for initialization
		protected virtual void Start ()
		{
			if(moveTargetMarker)
			{
				moveMarkerTR = moveTargetMarker.GetComponent<Transform>();
				moveMarkerMR = moveTargetMarker.GetComponent<MeshRenderer>();
			}
			speed = moveConfig.maxSpeed;
		}

		/// <summary>
		/// Called when the Visual Module is setted. Here we need to initialize all the component related functionality.
		/// </summary>
		public override void OnVisualModuleSetted()
		{
			base.OnVisualModuleSetted();
			// Gets the transform that will be used for the hovercraft turn effect.
			bodyToRoll = VisualProxy.GetPropertyValue<Transform>("roll_body");
		}

		/// <summary>
		/// Called when the Visual Module is removed. Here we need to remove all the component references 
		/// from the visual module because we are no longer in control of that components.
		/// </summary>
		public override void OnVisualModuleRemoved()
		{
			base.OnVisualModuleRemoved();
			bodyToRoll = null;
		} 

		/// <summary>
		/// Initialize the movement configuration and show the destination marker at its final position.
		/// </summary>
		/// <param name="position">Final destination of the movement.</param>
		/// <param name="direction">Final look at direction for the unit once the destination is reached.</param>
		public virtual void PrepareToMove(Vector3 position, Vector3 direction)
		{
			preparePos = position;
			if(direction != Vector3.zero)
				targetLookAt = Quaternion.LookRotation(direction.normalized);
			else
				targetLookAt = transform.rotation;

			if(moveMarkerTR)
				moveMarkerTR.position = preparePos;
			if(moveMarkerMR)
				moveMarkerMR.enabled = true;
		}

		/// <summary>
		/// Confirms the destination setted at PrepareToMove and starts the movement.
		/// </summary>
		public virtual void EngageMovement()
		{
			if(moveMarkerMR)
				moveMarkerMR.enabled = false;
			currentMoveOrder = MoveOrder.GoToTargetDestination;
			OnMovementOrderEngage(preparePos);
		}

		/// <summary>
		/// Stops the current movement at the current position.
		/// </summary>
		public virtual void StopMovement()
		{
			if(moveMarkerMR)
				moveMarkerMR.enabled = false;
			if(currentMoveOrder != MoveOrder.None)
				currentMoveOrder = MoveOrder.StoppingAllMovement;
			OnMovementOrderStop();	
		}

		/// <summary>
		/// Called after a EngageMovement order.
		/// Override to add special behaviours.
		/// </summary>
		/// <param name="destination">The target point in world coordinates where to move the unit.</param>
		protected virtual void OnMovementOrderEngage(Vector3 destination)
		{
			targetPosition = preparePos;
		}

		/// <summary>
		/// Called when a full stop order was given to this navigation.
		/// Override to add special behaviours.
		/// </summary>
		protected virtual void OnMovementOrderStop()
		{
		}

		/// <summary>
		/// Determines whether the movement order is completed because the unit has reached the target position.
		/// </summary>
		/// <returns>True if the target position was reached by this unit.</returns>
		public virtual bool IsFinalDestinationReached()
		{
			return (targetPosition - BasePosition).sqrMagnitude < (stoppingDistance * stoppingDistance);
		}

		// Update is called once per frame
		void Update ()
		{
			lastAccel = 0.0f;
			if (currentMoveOrder == MoveOrder.GoToTargetDestination)
			{
				// I'm already at the location ?
				if (IsFinalDestinationReached())
				{
					// Yes. Target reached! Change move order to finaly look at the desired direction.
					currentMoveOrder = MoveOrder.LookAtFinalDirection;
					OnMoveDestinationReached();
				}
				else
					OnMovingToDestination();
			}
			else if (currentMoveOrder == MoveOrder.LookAtFinalDirection)
			{
				Advance(targetLookAt, 0.0f);
				if( DoRoll(targetLookAt) )
				{
					if(moveSpeed.sqrMagnitude > 0)
						currentMoveOrder = MoveOrder.StoppingAllMovement;
					else
						currentMoveOrder = MoveOrder.None;
				}
			}
			else if (currentMoveOrder == MoveOrder.StoppingAllMovement)
			{
				Advance(transform.rotation, 0.0f);
				if( moveSpeed.sqrMagnitude == 0 && DoRoll(transform.rotation) )
					currentMoveOrder = MoveOrder.None;
			}
		}

		public void LateUpdate()
		{
			if(moveMarkerMR && moveMarkerMR.enabled)
				moveMarkerTR.position = preparePos;
		}

		protected virtual void OnMovingToDestination()
		{
			Vector3 totalMoveVector = targetPosition - BasePosition;
			Quaternion targetLookAt = Quaternion.LookRotation(totalMoveVector.normalized);
			Advance(targetLookAt, moveConfig.accel);
			DoRoll(targetLookAt);
		}

		protected virtual void OnMoveDestinationReached()
		{
		}

		public Vector3 Advance(Quaternion targetLookAt, float accel, bool applyMovement=true)
		{
			if(transform.rotation != targetLookAt)
				transform.rotation = Quaternion.RotateTowards(transform.rotation, targetLookAt, moveConfig.rotSpeed * Time.deltaTime);

			float faceDeltaAngle = Quaternion.Angle(transform.rotation, targetLookAt);
			if( faceDeltaAngle < AngularSleepThreshold )
			{
				transform.rotation = targetLookAt;
				faceDeltaAngle = 0.0f;
			}

			Vector3 initSpeed = transform.forward * moveSpeed.magnitude;
			Vector3 finalSpeed = initSpeed;

			if( accel != 0.0f )
			{
				float turnFactor = 1;
				if(faceDeltaAngle != 0.0f)
					turnFactor = turnFactor - moveConfig.maneuverFactor * (Mathf.Abs(faceDeltaAngle) / 180.0f);
				finalSpeed += transform.forward * accel * Time.deltaTime;
				finalSpeed *= turnFactor;
				finalSpeed = Vector3.ClampMagnitude(finalSpeed, speed);
			}
			else
			{
				finalSpeed *= moveConfig.fullStopFactor;
				if (finalSpeed.sqrMagnitude <= Physics.sleepThreshold)
					finalSpeed = Vector3.zero;
			}
			Vector3 moveDistance = (initSpeed + finalSpeed) * 0.5f * Time.deltaTime;
			if(applyMovement)
				transform.position += moveDistance;
			moveSpeed = finalSpeed;
			lastAccel = accel;
			return moveDistance;
		}

		public bool DoRoll(Quaternion lookAt)
		{
			float faceDeltaAngle = Quaternion.Angle(transform.rotation, lookAt);
			float rollDeltaAngle = 0.0f;

			if (bodyToRoll && moveConfig.rollFactor != 0.0f)
			{
				//if(faceDeltaAngle > 0)
				//	faceDeltaAngle = Mathf.MoveTowards(faceDeltaAngle, 90 * moveConfig.rollFactor, 6.0f );
				float faceRotSign = Vector3.Dot(transform.right, lookAt * Vector3.forward) >= 0 ? -1.0f : 1.0f;
				Quaternion rollAngle = Quaternion.AngleAxis(faceRotSign * moveConfig.rollFactor * faceDeltaAngle * 0.5f, Vector3.forward);
				bodyToRoll.localRotation = Quaternion.RotateTowards(bodyToRoll.localRotation, rollAngle, moveConfig.rollSpeed * Time.deltaTime);

				rollDeltaAngle = Quaternion.Angle(bodyToRoll.localRotation, Quaternion.identity);
			}
			if(faceDeltaAngle < 1 && rollDeltaAngle < 1.0f)
			{
				if(bodyToRoll)
					bodyToRoll.localRotation = Quaternion.identity;
				return true;
			}
			return false;
		}
	}
}