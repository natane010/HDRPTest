using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace TK.CustomTimeLine
{
    public class LightTL : ScriptableObject
    {

    }


    public class TimelineLightCustomTrack : BaseBehaviour<LightTL>
    {

    }

    #region Base
    [Serializable]
    public class BaseBehaviour<T> : PlayableBehaviour
    {
        public T Component;
        public float weight;
    }

    public class BaseClip<T> : PlayableAsset, ITimelineClipAsset
    {
        public BaseBehaviour<T> behaviour = new BaseBehaviour<T>();
        public ClipCaps clipCaps
        {
            get
            {
                return ClipCaps.Blending | ClipCaps.SpeedMultiplier;
            }
        }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<BaseBehaviour<T>>.Create(graph);
        }
    }

    public class BaseMixerBehaviour<T> : PlayableBehaviour
    {
        public TimelineClip[] Clips { get; set; }
        public PlayableDirector Director { get; set; }
    }

    [TrackBindingType(typeof(ScriptableObject))]
    [TrackColor(1,0,0)]
    public class BaseTrack : TrackAsset
    {

    }
    #endregion
}