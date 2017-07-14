using GameBase;

namespace SpaceRTSKit
{
	/// <summary>
	/// RTS implementation of a GameSceneSystem
	/// </summary>
	public class RTSSceneSystem : GameSceneSystem
	{
		/// <summary>
		/// Reference to the RTSScene or null if not found.
		/// </summary>
		public RTSScene RTSScene { get { return gameScene as RTSScene; } }
	}
}
