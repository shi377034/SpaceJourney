using GameBase;

namespace SpaceRTSKit.Messages
{
	/// <summary>
	/// Message dispatched when a item un the BuildQueue of the builder unit has changed.
	/// </summary>
	public class BuildQueueChanged : Message
	{
		private Builder builder;

		/// <summary>
		/// The builder wich its build queue has changed.
		/// </summary>
		public Builder Builder { get { return builder; } }

		public BuildQueueChanged(Builder builder)
		{
			this.builder = builder;
		}
	}
}
