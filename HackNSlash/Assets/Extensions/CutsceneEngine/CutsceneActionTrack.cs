using UnityEngine.Timeline;

namespace Extensions.CutsceneEngine
{
    /**
     * <summary>
     * The CutsceneActionTrack class represents a track in Unity's Timeline that can contain CutsceneActionClips.
     * This class allows for organizing and managing cutscene actions within the Timeline, enabling designers to create
     * complex cutscenes with custom actions.
     * </summary>
     */
    [TrackClipType(typeof(CutsceneActionClip))]
    public class CutsceneActionTrack : TrackAsset { }
}