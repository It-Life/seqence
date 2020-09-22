﻿using System;
using System.Collections.Generic;
using UnityEngine.Seqence.Data;

namespace UnityEngine.Seqence
{
    [Flags]
    public enum TrackMode
    {
        Normal = 1,
        Mute = 1 << 1,
        Record = 1 << 2,
        Lock = 1 << 3,
    }


    public abstract class XTrack : XSeqenceObject
    {
        public readonly uint ID;

        public XTrack[] childs;

        public IClip[] clips;

        public XMarker[] marks;

        public List<MixClip> mixs;

        protected TrackMode mode;

        public XSeqence seqence;

        public TrackData data;

        public XTrack parent { get; set; }

        public virtual bool cloneable
        {
            get { return true; }
        }

        public abstract AssetType AssetType { get; }

        public abstract XTrack Clone();

        public bool hasChilds
        {
            get { return childs != null && childs.Length > 0; }
        }

        public bool hasMix
        {
            get { return mixs != null && mixs.Count > 0; }
        }

        public XTrack root
        {
            get
            {
                XTrack track = this;
                while (track.parent != null)
                {
                    track = track.parent;
                }
                return track;
            }
        }

        public bool mute
        {
            get { return GetFlag(TrackMode.Mute); }
        }

        public bool parentMute
        {
            get { return parent ? parent.mute : mute; }
        }

        public bool record
        {
            get { return GetFlag(TrackMode.Record); }
        }

        public bool locked
        {
            get { return GetFlag(TrackMode.Lock); }
        }

        public bool parentLocked
        {
            get { return parent ? parent.locked : locked; }
        }

        protected XTrack()
        {
            ID = XSeqence.IncID;
            mode = TrackMode.Normal;
        }

        public void Initial(TrackData data, XSeqence tl, XTrack parent)
        {
            this.data = data;
            this.seqence = tl;
            this.parent = parent;
            if (data != null)
            {
                if (data.clips != null)
                {
                    int len = data.clips.Length;
                    clips = new IClip[len];
                    for (int i = 0; i < len; i++)
                    {
                        clips[i] = BuildClip(data.clips[i]);
                    }
                }
                if (data.marks != null)
                {
                    int len = data.marks.Length;
                    marks = new XMarker[len];
                    for (int i = 0; i < len; i++)
                    {
                        marks[i] = XSeqenceFactory.GetMarker(this, data.marks[i]);
                    }
                }
                if (data.childs != null)
                {
                    int len = data.childs.Length;
                    childs = new XTrack[len];
                    for (int i = 0; i < len; i++)
                    {
                        childs[i] = XSeqenceFactory.GetTrack(data.childs[i], seqence, this);
                    }
                }
            }
            OnPostBuild();
        }

        public bool GetFlag(TrackMode mode)
        {
            return (this.mode & mode) > 0;
        }

        public void SetFlag(TrackMode mode, bool flag)
        {
            if (flag)
            {
                this.mode |= mode;
            }
            else
            {
                this.mode &= ~(mode);
            }
        }

        public bool IsChild(XTrack p, bool gradsonContains)
        {
            XTrack tmp = this;
            if (gradsonContains)
            {
                while (tmp)
                {
                    if (tmp.parent != null)
                        if (tmp.parent.Equals(p))
                            return true;
                        else
                            tmp = tmp.parent;
                    else
                        break;
                }
            }
            else
            {
                return p != null && this.parent.Equals(p);
            }
            return false;
        }

        protected void Foreach(Action<XTrack> track, Action<IClip> clip)
        {
            ForeachClip(clip);
            ForeachTrack(track);
        }

        public void ForeachMark(Action<XMarker> marker)
        {
            if (marks != null)
            {
                int len = marks.Length;
                for (int i = 0; i < len; i++)
                {
                    marker(marks[i]);
                }
            }
        }

        public void ForeachClip(Action<IClip> clip)
        {
            if (clips != null)
            {
                int len = clips.Length;
                for (int i = 0; i < len; i++)
                {
                    clip(clips[i]);
                }
            }
        }

        public void ForeachTrack(Action<XTrack> track)
        {
            if (childs != null)
            {
                for (int i = 0; i < childs.Length; i++)
                {
                    track(childs[i]);
                }
            }
        }

        public void ForeachHierachyTrack(Action<XTrack> track)
        {
            if (childs != null)
            {
                for (int i = 0; i < childs.Length; i++)
                {
                    track(childs[i]);
                    childs[i].ForeachHierachyTrack(track);
                }
            }
            track(this);
        }

        public abstract IClip BuildClip(ClipData data);

        protected virtual void OnPostBuild()
        {
        }

        protected virtual void OnMixer(float time, MixClip mix)
        {
        }

        protected void AddMix(MixClip mix)
        {
            if (mixs == null)
            {
                mixs = new List<MixClip>();
            }
            if (!mixs.Contains(mix))
            {
                mixs.Add(mix);
            }
        }

        public virtual void OnBind()
        {
            ForeachClip(x => x.OnBind());
        }

        protected int clipA, clipB;

        public virtual void Process(float time, float prev)
        {
            ForeachTrack(track => track.Process(time, prev));
            clipA = -1;
            clipB = -1;
            if (!mute)
            {
                bool mix = MixTriger(time, out var mixClip);
                if (clips != null)
                    for (int i = 0; i < clips.Length; i++)
                    {
                        if (clips[i].Update(time, prev, mix))
                        {
                            var clip = clips[i] as XAnimationClip;
                            if (clip)
                            {
                                if (clipA == -1)
                                    clipA = clip.port;
                                else
                                    clipB = clip.port;
                            }
                        }
                    }
                MarkTriger(time, prev);
                if (mix) OnMixer(time, mixClip);
            }
        }

        public void RebuildMix()
        {
            mixs?.Clear();
            if (clips != null)
            {
                float tmp = clips[0].end;
                for (int i = 1; i < clips.Length; i++)
                {
                    if (clips[i].start < tmp)
                    {
                        float start = clips[i].start;
                        float duration = tmp - clips[i].start;
                        BuildMix(start, duration, clips[i - 1], clips[i]);
                    }
                    tmp = clips[i].end;
                }
            }
        }

        protected void BuildMix(float start, float duration, IClip clip1, IClip clip2)
        {
            var mix = SharedPool<MixClip>.Get();
            mix.Initial(start, duration, clip1, clip2);
            AddMix(mix);
        }


        private bool MixTriger(float time, out MixClip mixClip)
        {
            if (mixs != null)
            {
                int cnt = mixs.Count;
                for (int i = 0; i < cnt; i++)
                {
                    if (mixs[i].IsIn(time))
                    {
                        mixClip = mixs[i];
                        return true;
                    }
                }
            }
            mixClip = null;
            return false;
        }

        private void MarkTriger(float time, float prev)
        {
            if (marks != null)
            {
                for (int i = 0; i < marks.Length; i++)
                {
                    var mark = marks[i];
                    if (mark.time > prev && mark.time <= time)
                    {
                        mark.OnTriger();
                    }
                    if (mark.reverse && mark.time >= time && mark.time < prev)
                    {
                        mark.OnTriger();
                    }
                }
            }
        }

        protected TrackData CloneData() //deep clone
        {
            return TrackData.DeepCopyByXml(data);
        }


        public IClip GetPlayingPlayable(out float tick)
        {
            float t = seqence.Time;
            if (mixs != null)
                for (int i = 0; i < mixs.Count; i++)
                {
                    float end = mixs[i].duration + mixs[i].start;
                    float start = mixs[i].start;
                    if (start < t && end > t)
                    {
                        bool lve = t - start > end - t;
                        tick = t - start;
                        return lve ? mixs[i].blendA : mixs[i].blendB;
                    }
                }
            if (clips != null)
                for (int i = 0; i < clips.Length; i++)
                {
                    if (clips[i].start < t && clips[i].end > t)
                    {
                        tick = t - clips[i].start;
                        return clips[i];
                    }
                }
            tick = 0;
            return null;
        }

        public virtual void OnDestroy()
        {
            Foreach(track => track.OnDestroy(), clip => clip.OnDestroy());
            ForeachMark(mark => mark.OnDestroy());

            childs = null;
            marks = null;
            parent = null;
            clips = null;
            mode = 0;
            if (mixs != null)
            {
                for (int i = 0; i < mixs.Count; i++)
                {
                    SharedPool<MixClip>.Return(mixs[i]);
                }
                mixs.Clear();
                mixs = null;
            }
        }


        public override bool Equals(object obj)
        {
            return Equals(obj as XTrack);
        }

        public bool Equals(XTrack other)
        {
            return other != null && ID == other.ID;
        }

        public override int GetHashCode()
        {
            return ((ID << 4) + 375).GetHashCode();
        }

        public static implicit operator bool(XTrack track)
        {
            return track != null;
        }

        public override string ToString()
        {
            return "track " + ID;
        }
    }
}
