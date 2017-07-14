using GameBase;
using GameBase.AttributeExtension;
using UnityEngine;

namespace SpaceRTSKit
{
	/// <summary>
	/// GameEntityComponent that allows to this GameEntity to be builded.
	/// </summary>
	public class Buildable : GameEntityComponent
	{
		public float speed = 6.0f;
		public float stopDistance = 1.0f;

		private Builder builder = null;
		private FXUnderConstruction fx;
		[SerializeField][ReadOnly]
		private float current = 0.0f;
		private Vector3 expelPoint;
		private Vector3 rallyPoint;
		private bool isConstructionFinished;

		/// <summary>
		/// Returns true if this builable has a builder currently assigned.
		/// </summary>
		public bool HasBuilder { get { return builder; } }
		/// <summary>
		/// Returns true if this builable has a builder currently assigned.
		/// </summary>
		public Builder Builder { get { return builder; } }
		/// <summary>
		/// RTSEntity owner of this Buildable.
		/// </summary>
		public RTSEntity ThisRTSEntity { get { return ThisEntity as RTSEntity; } }
		/// <summary>
		/// UnitConfig for the current type of buildable.
		/// </summary>
		public UnitConfig BuildType { get { return ThisRTSEntity.Config; } }

		private void Start()
		{
			if(fx)
				fx.SetProgress(GetProgress());
		}

		private void Update()
		{
			if( !isConstructionFinished )
				return;

			transform.position = Vector3.Lerp(transform.position, expelPoint, speed * Time.deltaTime);
			if( Vector3.SqrMagnitude(transform.position - expelPoint) < (stopDistance * stopDistance) )
				OnFinalPositionReached();
		}

		/// <summary>
		/// Called when the Visual Module is setted. Here we need to initialize all the component related functionality.
		/// </summary>
		override public void OnVisualModuleSetted()
		{
			fx = GetVisualProxyProperty<FXUnderConstruction>("construct_fx");
		}

		/// <summary>
		/// Called when the Visual Module is removed. Here we need to uninitialize all the component related functionality.
		/// </summary>
		override public void OnVisualModuleRemoved()
		{
            fx = null;
		}

		private void OnFinalPositionReached()
		{
			isConstructionFinished = false;
			ChangeThisEntityController();
		}

		/// <summary>
		/// Sets the current builder for this buildable.
		/// </summary>
		/// <param name="builder">The builder to build this buildable.</param>
		public void SetBuilder(Builder builder)
		{
			this.builder = builder;
			this.expelPoint = transform.position;
		}

		/// <summary>
		/// Set the final position for this buildable once the build is complete.
		/// By default will be the same position where the buildable was created.
		/// </summary>
		/// <param name="finalPos">The world coords point where the unit is out of the Builder.</param>
		/// <param name="rallyPoint">The world coords point where to go once the unit is out of the Builder.</param>
		public void SetFinalMovePosition(Vector3 finalPos, Vector3 rallyPoint)
		{
			this.expelPoint = finalPos;
			this.rallyPoint = rallyPoint;
		}

		/// <summary>
		/// Increases the current value of progress according with the current build time
		/// </summary>
		/// <param name="value">Value to increase the current progress.</param>
		public void ChangeProgress(float value)
		{
			current += value;
			float progress = GetProgress();

			if (fx)
				fx.SetProgress(progress);

			if(progress == 1.0f)
				OnConstructionFinished();
		}

		/// <summary>
		/// Called when the builder is no longer building this buildable.
		/// </summary>
		internal void OnBuilderWorkEnded()
		{
			// Removes the builder reference once no longer working here.
			builder = null;
		}

		/// <summary>
		/// Returns the normalized current percent progress of construction of this buildable. 
		/// </summary>
		/// <returns>Normalized progress of construction. 0 means not started, 1 means completed.</returns>
		public float GetProgress()
		{
			if(BuildType==null)
				return 0.0f;
			if(BuildType.buildTime==0.0f)
				return 1.0f;
			return Mathf.Min( 1.0f,  current / BuildType.buildTime);
		}

		/// <summary>
		/// Called when the construction of this buildable it's completed.
		/// </summary>
		private void OnConstructionFinished()
		{
			isConstructionFinished = true;
			if (builder)
				builder.BuildCompleted();
		}

		private RTSEntity ChangeThisEntityController()
		{
			ComponentProxy visualModule = ThisEntity.VisualProxy;
			RTSEntity ge = GameObject.Instantiate<GameObject>(BuildType.gameplayPrefab, transform.parent).GetComponent<RTSEntity>();
			ge.transform.SetPositionAndRotation(transform.position, transform.rotation);
			ThisEntity.ChangeVisualModule(null);

			// Changing the VisualModule parent to the new entity
			visualModule.transform.parent = ge.transform;

			// Setup of the new GameEntity
			ge.unitConfiguration = ThisRTSEntity.Config;
			ge.controller = ThisRTSEntity.controller;
			ge.ChangeVisualModule(visualModule);
			ge.gameObject.SetActive(true);
			Navigation nav = ge.GetComponent<Navigation>();
			if(nav != null)
			{
				nav.PrepareToMove(rallyPoint, rallyPoint - ge.transform.position);
				nav.EngageMovement();
			}
			GameObject.Destroy(this.gameObject);
			return ge;
		}

		/// <summary>
		/// Draws the progress percent of our current build next to this buildable position.
		/// </summary>
		//public void OnGUI()
		//{
		//	Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
		//	Rect rcLabel = new Rect(screenPos.x-50.0f, Screen.height - screenPos.y - 60.0f, 100.0f, 22.0f);
		//	int percent = Mathf.FloorToInt( 100.0f * GetProgress() );
		//	GUI.color = Color.green;
		//	GUI.Label(rcLabel, ""+percent+"%");
		//}

	}
}