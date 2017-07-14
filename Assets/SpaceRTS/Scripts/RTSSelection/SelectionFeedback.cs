using GameBase;
using UnityEngine;

namespace SpaceRTSKit
{
	/// <summary>
	/// Changes the selection feedback of the GameEntity when its required by the Selectable component.
	/// </summary>
	public class SelectionFeedback : GameEntityComponent
	{
		/// <summary>
		/// Color to use in the selection marker when the unit is selected.
		/// </summary>
		public Color selectedColor = new Color(0.2f, 1.0f, 0.0f, 1.0f);
		/// <summary>
		/// Color to use in the selection marker when the unit is highlighted.
		/// </summary>
		public Color highlightedColor = new Color(1.0f, 1.0f, 0.0f, 0.2f);

		private MeshRenderer selectionMarker;

		// Use this for initialization
		void Start ()
		{
			Messages.RegisterObserver<SelectableChanged>(OnSelectableChanged);
		}

		/// <summary>
		/// Called when the Visual Module is setted. Here we need to initialize all the component related functionality.
		/// </summary>
		override public void OnVisualModuleSetted()
		{
			selectionMarker = GetVisualProxyProperty<MeshRenderer>("select_marker");
			if(selectionMarker)
				selectionMarker.enabled = false;
		}

		/// <summary>
		/// Called when the Visual Module is removed. Here we need to uninitialize all the component related functionality.
		/// </summary>
		override public void OnVisualModuleRemoved()
		{
            selectionMarker = null;
		}

		void OnDestroy()
		{
			Messages.UnregisterObserver<SelectableChanged>(OnSelectableChanged);
		}

		void OnSelectableChanged(SelectableChanged msg)
		{
			if (selectionMarker == null)
				return;

			if (msg.IsSelected)
			{
				selectionMarker.enabled = true;
				selectionMarker.material.SetColor("_Color", selectedColor);
			}
			else if(msg.IsHighlighted)
			{
				selectionMarker.enabled = true;
				selectionMarker.material.SetColor("_Color", highlightedColor);
			}
			else
				selectionMarker.enabled = false;
		}
	}
}