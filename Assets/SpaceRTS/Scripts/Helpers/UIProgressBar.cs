using UnityEngine;
using UnityEngine.UI;

namespace GameBase.Helpers
{
	/// <summary>
	/// Simple class to manage a tileable Progress bar for the ui.
	/// </summary>
	public class UIProgressBar : MonoBehaviour
	{
		/// <summary>
		/// The Background image for the Progressbar
		/// </summary>
		public Image background;
		/// <summary>
		/// The foreground image that will be filled according to the fillAmount.
		/// </summary>
		public Image filler;
		/// <summary>
		/// The normalized value that indicates how much "filled" will be the ProgressBar.
		/// Use the FillAmount property if you want to change its value during gameplay.
		/// </summary>
		[Range(0.0f, 1.0f)]
		public float fillAmount;

		private Vector2 size;

		public float FillAmount 
		{
			set { fillAmount = Mathf.Clamp01(value); OnFillAmountChanged(); }
			get { return fillAmount; }
		}

		/// <summary>
		/// Total size of the ProgressBar.
		/// </summary>
		public Vector2 SizeDelta 
		{
			get { return background.rectTransform.sizeDelta; }
			set { background.rectTransform.sizeDelta = value; }
		}

		[ContextMenu("Test")]
		private void OnFillAmountChanged()
		{
			size.x = background.rectTransform.sizeDelta.x * fillAmount;
			size.y = filler.rectTransform.sizeDelta.y;
			filler.rectTransform.sizeDelta = size;
		}
	}
}