using GameBase.RTSKit;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SpaceRTSKit.UI
{
	/// <summary>
	/// Handles the input for the RTSCamera.
	/// <p>Reads the pan through the middle mouse button and through setting the cursor at the borders of the screen. 
	/// Requires to be attached next to a component capable of raycast the mouse pointer and deliver the BeginDrag, 
	/// Drag and EndDrag events (An Image for example). </p>
	/// </summary>
	public class RTSCameraUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler												
	{
		/// <summary>
		/// Reference to the RTSCamera in the scene.
		/// </summary>
		public RTSCamera cameraController;
		/// <summary>
		/// Border factor applyed to the minimum value between the screen width and height and to be used as a  border to measure the camera panning when the cursor 
		/// reachs that border.
		/// </summary>
		public float viewportPanBorder = 0.14f;
		/// <summary>
		/// Enable or disable the scroll on the borders feature.
		/// </summary>
		public bool allowBorderPan = true;
		/// <summary>
		/// Enable or disable the scroll with the middle mouse button.
		/// </summary>
		public bool allowMiddleButtonPan = true;
		private Vector2 prevCursorPosition;
		private bool panning = false;

		/// <summary>
		/// Getter and setter that enable or disable the scroll on the borders feature.
		/// </summary>
		public bool AllowBorderPan { get { return allowBorderPan; }	set { allowBorderPan = value; } }
		/// <summary>
		/// Getter and setter that enables the middle mouse button scroll feature.
		/// </summary>
		public bool AllowMiddleButtonPan { get { return allowMiddleButtonPan; } set { allowMiddleButtonPan = value;	} }

		void Update()
		{
			if (cameraController && allowBorderPan && !panning)
			{
				float definedBorder = Mathf.Min(Screen.width, Screen.height) * viewportPanBorder;
				Vector3 borderOverride = Vector3.zero;

				if (Input.mousePosition.x >= 0 && Input.mousePosition.y >= 0 &&
					Input.mousePosition.x <= Screen.width && Input.mousePosition.y <= Screen.height)
				{

					if (Input.mousePosition.x < definedBorder )
						borderOverride.x = Input.mousePosition.x - definedBorder;
					if (Input.mousePosition.y < definedBorder )
						borderOverride.y = Input.mousePosition.y - definedBorder;

					if (Input.mousePosition.x > Screen.width - definedBorder)
						borderOverride.x = Input.mousePosition.x - (Screen.width - definedBorder);
					if (Input.mousePosition.y > Screen.height - definedBorder)
						borderOverride.y = Input.mousePosition.y - (Screen.height - definedBorder);

					if (borderOverride != Vector3.zero)
						cameraController.TranslateInCursorDirection(borderOverride.magnitude / definedBorder);
				}
			}
		}

		/// <summary>
		/// IBeginDragHandler implementation that allows to start the tracking of the middle mouse button scroll feature.
		/// </summary>
		/// <param name="eventData">PointerEventData provided by the EventSystem.</param>
		public void OnBeginDrag(PointerEventData eventData)
		{
			if (eventData.button == PointerEventData.InputButton.Middle)
			{
				prevCursorPosition = eventData.position;
				panning = true;
			}
		}

		/// <summary>
		/// IDragHandler implementation that allows to keep tracking of the mouse middle button panning for scroll feature.
		/// </summary>
		/// <param name="eventData">PointerEventData provided by the EventSystem.</param>
		public void OnDrag(PointerEventData eventData)
		{
			if(eventData.button == PointerEventData.InputButton.Middle)
			{
				if(eventData.position != prevCursorPosition && cameraController != null && allowMiddleButtonPan)
					cameraController.TranslateByScreenPosDelta(eventData.position, prevCursorPosition);
				prevCursorPosition = eventData.position;
			}
		}

		/// <summary>
		/// IEndDragHandler implementation that allows to detect the end of the mouse middle button pan for the scroll feature.
		/// </summary>
		/// <param name="eventData"></param>
		public void OnEndDrag(PointerEventData eventData)
		{
			if (eventData.button == PointerEventData.InputButton.Middle)
			{
				if (eventData.pointerCurrentRaycast.gameObject == this.gameObject)
				{
					if (cameraController && allowMiddleButtonPan)
						cameraController.TranslateByScreenPosDelta(eventData.position, prevCursorPosition);
				}
				prevCursorPosition = eventData.position;
				panning = false;
			}
		}
	}
}