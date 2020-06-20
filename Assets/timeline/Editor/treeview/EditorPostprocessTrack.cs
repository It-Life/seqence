using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Timeline.Data;

namespace UnityEditor.Timeline
{
    [TimelineEditor(typeof(XPostprocessTrack))]
    public class EditorPostprocessTrack : EditorTrack
    {
        protected override Color trackColor
        {
            get { return Color.magenta; }
        }

        protected override string trackHeader
        {
            get { return "后处理" + ID; }
        }

        protected override void OnAddClip(float t)
        {
            PostprocessData data = new PostprocessData();
            data.start = t;
            data.duration = 20;
            XPostprocessClip clip = new XPostprocessClip((XPostprocessTrack) track, data);
            track.AddClip(clip, data);
        }
    }
}
