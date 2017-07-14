using UnityEngine;
using GameBase;
using GameBase.AttributeExtension;
using GameBase.RTSKit;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace SpaceRTSKit
{
	/// <summary>
	/// Is the Main Controller. The RTS implementation of the GameScene. Handles the input 
	/// according to the current state and send it to the game systems.
	/// </summary>
	public class RTSScene : GameScene, ISelectableCriteria
	{
		[Header("Players Config")]
		public RTSPlayer humanPlayer;

		/// <summary>
		/// Reference to the PlaceMarker that can be used to mark a destination point in the map.
		/// </summary>
		[Header("RTSScene Config")]
		public PlaceMarker placeMarker;
		/// <summary>
		/// The Gameplay GameEntity PRefab that will used when a structure is build in the map until it's complete.
		/// </summary>
		public RTSEntity constructPrefab;
		/// <summary>
		/// Reference to the SelectionInput
		/// </summary>
		public SelectionInput selectionInput;
		/// <summary>
		/// Reference to the SceneBounds
		/// </summary>
		public SceneBounds sceneBounds;
		/// <summary>
		/// Transform of the GameObject that will act as a container to all created game entities.
		/// </summary>
		public Transform entitiesContainer;

		[SerializeField][ReadOnly]
		private bool isPointerOverGame = true;
		private bool isDoubleClick = false;

		private GroupController groupController;
		private BuildGhostControl builderGhost;
		private SelectionSystem selectionSystem;

		/// <summary>
		/// Remarks if the cursor is over the game client area
		/// </summary>
		public bool IsPointerOverGame {	get	{ return isPointerOverGame; } set {	isPointerOverGame = value; } }

		public void OnPointerClickEvent(BaseEventData data)
		{
			PointerEventData pointerData = (PointerEventData) data;
			isDoubleClick = pointerData.button == PointerEventData.InputButton.Left && pointerData.clickCount >= 2;
		}

		void Start()
		{
			groupController = Get<GroupController>();
			builderGhost = Get<BuildGhostControl>();
			selectionSystem = Get<SelectionSystem>();
			selectionInput.selectionCriteria = this;
			if(groupController && selectionSystem)
			{
				selectionSystem.selectionAddedEntity += groupController.AddShip;
				selectionSystem.selectionRemovedEntity += groupController.RemoveShip;
				selectionSystem.selectionRemovedAll += groupController.RemoveAll;
			}
		}

		void OnDestroy()
		{
			if(groupController && selectionSystem)
			{
				selectionSystem.selectionAddedEntity -= groupController.AddShip;
				selectionSystem.selectionRemovedEntity -= groupController.RemoveShip;
				selectionSystem.selectionRemovedAll -= groupController.RemoveAll;
			}
		}

		void Update()
		{
			if(isPointerOverGame)
			{
				// Left mouse button up (click performed)
				if (Input.GetMouseButtonUp(0))
				{
					// Is trying to stablish a build position?
					if (builderGhost && builderGhost.IsGhostRequested())
						builderGhost.Confirm();
					// There's a double click over a selectable unit?
					if (selectionSystem.HasHoveredEntities && isDoubleClick )
						SelectAllVisibleFromSameType(selectionSystem.OrderedHoveringEntities[0]);
					// If not the click will be delivered to the selection input system.
					else if(selectionInput)
						selectionInput.ProcessLeftClickEvent();
					
				}
				// Right mouse button down (starting to drag)
				if (Input.GetMouseButtonDown(1))
				{
					// Is trying to stablish a build position?
					if (builderGhost && builderGhost.IsGhostRequested())
						builderGhost.Cancel();
					// IS over an game entity? Try to perform an action over that entity... 
					// in this case try to continue the build if it's possible.
					else if (selectionSystem.HasHoveredEntities)
						groupController.DoContinueBuildOrder(selectionSystem.Hoverings);
					else // Is trying to move the current selected units, starts the move handler.
					{
						groupController.MoveHandlerBegin();
						if( !groupController.IsMoveHandlerReadyToMove() )
						{
							// None of the selected units can move. if structures are selected
							// then the order must be a change of the rally point position.
							if( groupController.ChangeBuildersRallyPoint(sceneBounds.CursorLookPoint) )
								placeMarker.ShowAt(sceneBounds.CursorLookPoint);
						}
					}
				}
			}
			// If it's the move handler enabled because we are trying to meve the selected ships...
			if (groupController.TryingToMove)
			{
				// Drags the move handler every frame
				groupController.MoveHandlerDrag();
				// Is the right mouse up in this frame?
				if (Input.GetMouseButtonUp(1))
					groupController.MoveHandlerEnd();
			}
		}

		private void SelectAllVisibleFromSameType(Selectable selectable)
		{
			RTSEntity rtsEntity = (RTSEntity) selectable.ThisEntity;
			if(rtsEntity == null)
				return;
			List<Selectable> sameTypeUnits = new List<Selectable>();
			foreach( RTSEntity inViewEntity in rtsEntity.controller.InViewEntities )
			{
				Selectable sel = inViewEntity.GetComponent<Selectable>();
				if( sel && inViewEntity.unitConfiguration == rtsEntity.unitConfiguration )
					sameTypeUnits.Add(sel);
			}
			selectionSystem.RemoveAllHovering();
			selectionSystem.SetAsHovering(sameTypeUnits);
			selectionSystem.SetAsHighlighted(sameTypeUnits);
			selectionSystem.ConfirmAllHighlightsAsSelected();
			selectionSystem.RemoveAllHovering();
		}

		/// <summary>
		/// Must be called when the cursor enters the game client area of screen of it that
		/// area is no longer covered by another panel.
		/// </summary>
		public void OnPointerEnterGameArea()
		{
			isPointerOverGame = true;

			if (builderGhost && builderGhost.IsGhostRequested())
				builderGhost.Show(true);
		}

		/// <summary>
		/// Must be called when the cursor exits the game client area of screen or if that 
		/// area is covered by another panel.
		/// </summary>
		public void OnPointerExitGameArea()
		{
			isPointerOverGame = false;

			if (builderGhost && builderGhost.IsGhostRequested())
				builderGhost.Show(false);
		}

		public IEnumerable<Selectable> FilteringSelectablesByBox(IEnumerable<Selectable> source)
		{
			if( IsHoveringAShip(source) )
				return SelectOnlyShips(source);
			else
				return source;
		}

		private IEnumerable<Selectable> SelectOnlyShips(IEnumerable<Selectable> source)
		{
			foreach(Selectable sel in source)
			{
				RTSEntity rtsEnt = sel.ThisEntity as RTSEntity;
				if( rtsEnt.ShipConfig is ShipConfig)
					yield return sel;
			}
			yield break;
		}

		private bool IsHoveringAShip(IEnumerable<Selectable> source)
		{
			foreach(Selectable sel in source)
			{
				RTSEntity rtsEnt = sel.ThisEntity as RTSEntity;
				if( rtsEnt.ShipConfig is ShipConfig)
					return true;

			}
			return false;
		}

		public IEnumerable<Selectable> FilteringSelectablesByRay(IEnumerable<Selectable> source)
		{
			if( IsHoveringAShip(source) )
				return SelectOnlyShips(source);
			else
				return source;
		}
	}
}
