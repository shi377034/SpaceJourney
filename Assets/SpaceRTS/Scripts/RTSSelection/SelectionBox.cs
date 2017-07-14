using GameBase;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace SpaceRTSKit.UI
{
	/// <summary>
	/// Handles the input for the selection system related to the selection box feature.
	/// </summary>
	public class SelectionBox : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		/// <summary>
		/// Reference to the SelectionInput in the scene.
		/// </summary>
		public SelectionInput selectionInput;
		/// <summary>
		/// Reference to the selection box RectTransform.
		/// </summary>
		public RectTransform selectionBox;

		/// <summary>
		/// Unity event that allows to dispatch the selection box started event
		/// </summary>
		public UnityEvent selectionBoxStarted;
		/// <summary>
		/// Unity event that allows to dispatch the selection box ended event
		/// </summary>
		public UnityEvent selectionBoxEnded;

		/// <summary>
		/// IBeginDragHandler implementation.
		/// </summary>
		/// <param name="eventData">PointerEventData provided by the EventSystem.</param>
		public void OnBeginDrag(PointerEventData eventData)
		{
			if (eventData.button == PointerEventData.InputButton.Left)
			{
				if(selectionBox)
					selectionBox.gameObject.SetActive(true);
				if (selectionInput)
				{
					selectionInput.StartSelectionBox();
					selectionBoxStarted.Invoke();
				}
			}
		}

		/// <summary>
		/// IDragHandler implementation.
		/// </summary>
		/// <param name="eventData">PointerEventData provided by the EventSystem.</param>
		public void OnDrag(PointerEventData eventData)
		{
			if (eventData.button == PointerEventData.InputButton.Left)
			{
				Vector2 min = Vector2.Min(eventData.position, eventData.pressPosition);
				Vector2 max = Vector2.Max(eventData.position, eventData.pressPosition);

				if (selectionBox)
				{
					selectionBox.position = min;
					selectionBox.sizeDelta = max - min;
				}
				if(selectionInput)
					selectionInput.SetupSelectionBox(min, max);
			}
		}

		/// <summary>
		/// IEndDragHandler implementation.
		/// </summary>
		/// <param name="eventData">PointerEventData provided by the EventSystem.</param>
		public void OnEndDrag(PointerEventData eventData)
		{
			if (eventData.button == PointerEventData.InputButton.Left)
			{
				if(selectionBox)
					selectionBox.gameObject.SetActive(false);
				if (selectionInput)
				{
					if (eventData.pointerCurrentRaycast.gameObject == this.gameObject)
						selectionInput.EndSelectionBox();
					else
						selectionInput.CancelSelectionBox();
					selectionBoxEnded.Invoke();
				}
			}
		}
	}
}