using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameBase;
using GameBase.Helpers;
using SpaceRTSKit.Messages;

namespace SpaceRTSKit
{
	/// <summary>
	/// UI Panel controller for the build capability of ships and structures.
	/// </summary>
	public class UnitBuildUI : MonoBehaviour
	{
		public RTSScene rtsScene;
		/// <summary>
		/// Reference to the Construction Handler that will let the user choose the next build location.
		/// </summary>
		public BuildGhostControl ghostControl;
		/// <summary>
		/// The group controller keeping track of the selected units.
		/// </summary>
		public GroupController group;
		/// <summary>
		/// Root GameObject for the builder panel that will be shown when a builder is selected.
		/// </summary>
		public GameObject builderPanel;
		/// <summary>
		/// Instantiation handler for the button list of units to build.
		/// </summary>
		public ContentPool builderPool;
		/// <summary>
		/// Panel container for the units queued to build.
		/// </summary>
		public GameObject buildQueuePanel;
		/// <summary>
		/// Instantiation handler for the button list of queued units to build.
		/// </summary>
		public ContentPool buildQueuePool;

		/// <summary>
		/// List of buttons to enable or disable if they are available to build.
		/// </summary>
		[System.Obsolete("Use instead the builderPool to instantiate the buttons.")]
		public List<Button> buttons = new List<Button>();
		/// <summary>
		/// List of Text labels to configure with the names of the units to build.
		/// </summary>
		[System.Obsolete("Use instead the builderPool to instantiate the buttons.")]
		public List<Text> btnTexts = new List<Text>();

		private class QueueItemInfo
		{
			public UnitConfig unit;
			public int queueIndex;
			public int count;
		}

		private Dictionary<GameObject, UnitConfig> buildBtns = new Dictionary<GameObject, UnitConfig>();
		private Dictionary<GameObject, QueueItemInfo> buildQueueBtns = new Dictionary<GameObject, QueueItemInfo>();
		private Builder selectedBuilder;

		// Use this for initialization
		void Start ()
		{
			group.OnShipsChanged += RefreshBuilderPanel;
			if(!builderPool)
				Debug.LogWarning("Must properly set the ContentPool in BuilderPool to control the build buttons.", this.gameObject);
			if(!buildQueuePool)
				Debug.LogWarning("Must properly set the ContentPool in BuildQueuePool to control the build queue buttons.", this.gameObject);
		}

		void OnDestroy ()
		{
			group.OnShipsChanged -= RefreshBuilderPanel;
		}
	
		/// <summary>
		/// Confirms the build requested by OnBuildSlotClicked with a location and look at direction.
		/// </summary>
		/// <param name="toBuild">The unit configuration to build.</param>
		/// <param name="location">The world's position of the unit to build.</param>
		/// <param name="dir">The final look at direction of the unit to build.</param>
		public void OnConstructionConfirmed(UnitConfig toBuild, Vector3 location, Vector3 dir)
		{
			selectedBuilder.AddUnitToBuild(toBuild, location, dir);
		}

		/// <summary>
		/// Refresh the builder ui panel with the current selected builder info.
		/// </summary>
		private void RefreshBuilderPanel()
		{
			if (group.EntitiesCount == 0)
				OnCurrentBuilderChanged(null);
			else
				OnCurrentBuilderChanged( group.GetBestBuilder() );
		}

		private void OnCurrentBuilderChanged( Builder newBuilder )
		{
			if(selectedBuilder == newBuilder)
				return;
			if(selectedBuilder != null)
			{
				selectedBuilder.Messages.UnregisterObserver<BuildQueueChanged>(OnBuildQueueChanged);
			}

			selectedBuilder = newBuilder;

			if(selectedBuilder != null && selectedBuilder.Config)
			{
				selectedBuilder.Messages.RegisterObserver<BuildQueueChanged>(OnBuildQueueChanged);
				SetupBuilderPanel();
				SetupBuildQueuePanel();

				ShowPanel(builderPanel, selectedBuilder.Config.buildables.Count > 0);
				ShowPanel(buildQueuePanel, selectedBuilder.BuildsInQueue > 0 && !selectedBuilder.RequiresBuildLocation);
			}
			else
			{
				ShowPanel(builderPanel, false);
				ShowPanel(buildQueuePanel, false);
			}
		}

		private void OnBuildQueueChanged(BuildQueueChanged msg)
		{
			SetupBuildQueuePanel();
			ShowPanel(buildQueuePanel, msg.Builder.BuildsInQueue > 0 && !selectedBuilder.RequiresBuildLocation);
		}

		private void SetupBuilderPanel()
		{
			if(builderPool)
				builderPool.Destroy(buildBtns.Keys);
			buildBtns.Clear();

			if( builderPool && selectedBuilder.Config)
			{
				foreach (UnitConfig config in selectedBuilder.Config.buildables)
				{
					GameObject obj = builderPool.Instantiate();
					buildBtns.Add(obj, config);
					ComponentProxy proxy = obj.GetComponent<ComponentProxy>();
					Image img = proxy.GetPropertyValue<Image>("content_img");
					obj.name = config.name + " Item";
					img.sprite = config.uiImage;
				}
			}
		}

		private void SetupBuildQueuePanel()
		{
			if(buildQueuePool)
				buildQueuePool.Destroy(buildQueueBtns.Keys);

			buildQueueBtns.Clear();

			if (buildQueuePool && selectedBuilder.BuildsInQueue > 0 && !selectedBuilder.RequiresBuildLocation)
			{
				List<QueueItemInfo> itemsList = new List<QueueItemInfo>();
				QueueItemInfo current = null;
				int index = 0;
				foreach (UnitConfig config in selectedBuilder.QueuedUnits)
				{
					if(current == null || current.unit != config)
					{
						current = new QueueItemInfo();
						current.unit = config;
						current.count = 1;
						current.queueIndex = index;
						itemsList.Add(current);
					}
					else
						current.count = current.count + 1;
					
					index++;
				}

				for(index = 0; index < itemsList.Count; index++)
				{
					QueueItemInfo queueItem = itemsList[index];
					GameObject obj = buildQueuePool.Instantiate();
					ComponentProxy proxy = obj.GetComponent<ComponentProxy>();
					Image img = proxy.GetPropertyValue<Image>("content_img");
					Text txt = proxy.GetPropertyValue<Text>("content_count");
					Button btnRemove = proxy.GetPropertyValue<Button>("content_remove");
					img.sprite = queueItem.unit.uiImage;
					txt.text = queueItem.count.ToString();
					btnRemove.onClick.RemoveAllListeners();
					btnRemove.onClick.AddListener(() => { OnRemoveFromQueue(obj); } );
					obj.name = queueItem.unit.name + " Item";
					obj.transform.SetSiblingIndex(index);
					buildQueueBtns.Add(obj, queueItem);
				}
				ShowPanel(buildQueuePanel, true);
			}
			else
				ShowPanel(buildQueuePanel, false);
		}

		/// <summary>
		/// Called when a build selection button is clicked and setup the ghost builder controller
		/// for structures that requires a build location calling next to OnConstructionConfirmed.
		/// </summary>
		/// <param name="btn"></param>
		public void OnBuildSlotClicked(Button btn)
		{
			UnitConfig toBuild = null;
			
			if(!buildBtns.TryGetValue(btn.gameObject, out toBuild))
				return; // some error here

			if( selectedBuilder.RequiresBuildLocation )
			{
				if( ghostControl )
					ghostControl.SetupGhost(toBuild, OnConstructionConfirmed);
			}
			else
				OnConstructionConfirmed(toBuild, selectedBuilder.BuildLocation, selectedBuilder.BuildExpelLocation-selectedBuilder.BuildLocation);
		}

		/// <summary>
		/// Removes a specific build item from the queue
		/// </summary>
		/// <param name="clickedObject">The refrence to the queue item that made the remove from queue request.</param>
		public void OnRemoveFromQueue(GameObject clickedObject)
		{
			QueueItemInfo itemInfo;
			if( !buildQueueBtns.TryGetValue(clickedObject, out itemInfo) )
			{
				Debug.LogError("The clicked object is not tracked by the system.");
				return;
			}
			selectedBuilder.CancelBuildAt(itemInfo.queueIndex+itemInfo.count-1);
		}

		/// <summary>
		/// Shows or hide the builder panel.
		/// </summary>
		/// <param name="show"></param>
		[System.Obsolete("Use ShowPanel() instead.")]
		private void ShowBuilderPanel(bool show)
		{
			if( builderPanel && builderPanel.activeSelf != show)
				builderPanel.SetActive(show);
		}

		/// <summary>
		/// Shows or hide a generic panel.
		/// </summary>
		/// <param name="panel">The panel to show/hide.</param>
		/// <param name="show">Must be true to show the panel, false otherwise.</param>
		private void ShowPanel(GameObject panel, bool show)
		{
			if( panel && panel.activeSelf != show)
				panel.SetActive(show);
		}

		/// <summary>
		/// From a list of RTSEntity filters which one is a Builder and returns that list of builders.
		/// </summary>
		/// <param name="units">A list of RTSEntity to filter.</param>
		/// <returns>The filtered list of Builder entities.</returns>
		private List<RTSEntity> CollectBuilders(List<RTSEntity> units)
		{
			List<RTSEntity> result = new List<RTSEntity>();
			foreach(RTSEntity ge in units)
			{
				Builder builder = ge.GetComponent<Builder>();
				if(builder && ge.Config && ge.Config.buildables.Count > 0)
					result.Add(ge);
			}
			return result;
		}
	}
}