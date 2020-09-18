﻿using System;
using System.IO;
using System.Xml.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Seqence.Data
{
    [Serializable]
    [Flags]
    public enum AssetType
    {
        None = 0,
        Group = 1,
        Marker = 1 << 1,
        BoneFx = 1 << 2,
        SceneFx = 1 << 3,
        Animation = 1 << 4,
        PostProcess = 1 << 5,
        Transform = 1 << 6,
        LogicValue = 1 << 7,
    }

    [Serializable]
    [XmlInclude(typeof(BindTrackData))]
    [XmlInclude(typeof(TransformTrackData))]
    [XmlInclude(typeof(GroupTrackData))]
    [XmlInclude(typeof(AnimationTrackData))]
    public class TrackData
    {
        public ClipData[] clips;
        public MarkData[] marks;
        public TrackData[] childs;
        public AssetType type;


        public virtual void Write(BinaryWriter writer)
        {
            writer.Write((int) type);
            int len = clips?.Length ?? 0;
            int len2 = childs?.Length ?? 0;
            int len3 = marks?.Length ?? 0;
            writer.Write(len);
            writer.Write(len2);
            writer.Write(len3);
            for (int j = 0; j < len; j++)
            {
                clips[j].Write(writer);
            }
            for (int j = 0; j < len2; j++)
            {
                childs[j].Write(writer);
            }
            for (int i = 0; i < len3; i++)
            {
                marks[i].Write(writer);
            }
        }

        public virtual void Read(BinaryReader reader)
        {
            int len = reader.ReadInt32();
            if (len > 0) clips = new ClipData[len];
            int len2 = reader.ReadInt32();
            if (len2 > 0) childs = new TrackData[len2];
            int len3 = reader.ReadInt32();
            if (len3 > 0) marks = new MarkData[len3];

            for (int j = 0; j < len; j++)
            {
                clips[j] = XSeqenceFactory.CreateClipData(reader);
            }
            for (int j = 0; j < len2; j++)
            {
                childs[j] = new TrackData();
                childs[j].type = (AssetType) reader.ReadInt32();
                childs[j].Read(reader);
            }
            for (int i = 0; i < len3; i++)
            {
                marks[i] = XSeqenceFactory.CreateMarkData(reader);
            }
        }

        public static T DeepCopyByXml<T>(T obj) where T : TrackData
        {
            object retval;
            using (MemoryStream ms = new MemoryStream())
            {
                XmlSerializer xml = new XmlSerializer(typeof(T));
                xml.Serialize(ms, obj);
                ms.Seek(0, SeekOrigin.Begin);
                retval = xml.Deserialize(ms);
                ms.Close();
            }
            return (T) retval;
        }
    }

    [Serializable]
    public class BindTrackData : TrackData
    {
        public string prefab = "";

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(prefab);
        }

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            prefab = reader.ReadString();
        }
    }

    [Serializable]
    public class GroupTrackData : TrackData
    {
        public string comment = "";

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(comment);
        }

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            comment = reader.ReadString();
        }
    }

    [Serializable]
    public class AnimationTrackData : BindTrackData
    {
        public int roleid = 0;
        public Vector3 pos;
        public float rotY;

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            writer.Write(roleid);
            writer.Write(pos);
            writer.Write(rotY);
        }

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            roleid = reader.ReadInt32();
            pos = reader.ReadV3();
            rotY = reader.ReadSingle();
        }
    }

    [Serializable]
    public class TransformTrackData : TrackData
    {
        public float[] time;
        public Vector4[] pos;

#if UNITY_EDITOR
        public bool select;
#endif

        public override void Read(BinaryReader reader)
        {
            base.Read(reader);
            int cnt = reader.ReadInt32();
            time = new float[cnt];
            for (int i = 0; i < cnt; i++)
            {
                time[i] = reader.ReadSingle();
            }
            cnt = reader.ReadInt32();
            pos = new Vector4[cnt];
            for (int i = 0; i < cnt; i++)
            {
                pos[i] = reader.ReadV4();
            }
            cnt = reader.ReadInt32();
        }

        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            int cnt = time?.Length ?? 0;
            for (int i = 0; i < cnt; i++)
            {
                writer.Write(time[i]);
            }
            cnt = pos?.Length ?? 0;
            for (int i = 0; i < cnt; i++)
            {
                writer.Write(pos[i]);
            }
        }
    }

    [Serializable]
    public class TimelineConfig
    {
        public TrackData[] tracks;
        public ushort skillHostTrack;

        public void Write(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                writer.Write(skillHostTrack);
                int cnt = tracks.Length;
                writer.Write(cnt);
                for (int i = 0; i < cnt; i++)
                {
                    tracks[i].Write(writer);
                }
                writer.Flush();

#if UNITY_EDITOR
                AssetDatabase.ImportAsset(path);
#endif
            }
        }

        public void WriteXml(string path)
        {
            XmlSerializer serializer = new XmlSerializer(GetType());
            string content;
            using (StringWriter writer = new StringWriter())
            {
                serializer.Serialize(writer, this);
                content = writer.ToString();
            }
            using (StreamWriter stream_writer = new StreamWriter(path))
            {
                stream_writer.Write(content);
            }
#if UNITY_EDITOR
            AssetDatabase.ImportAsset(path);
#endif
        }

        public void Read(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    skillHostTrack = reader.ReadUInt16();
                    int cnt = reader.ReadInt32();
                    tracks = new TrackData[cnt];
                    for (int i = 0; i < cnt; i++)
                    {
                        var type = (AssetType) reader.ReadInt32();
                        tracks[i] = new TrackData();
                        tracks[i].type = type;
                        tracks[i].Read(reader);
                    }
                }
            }
        }

        public static TimelineConfig ReadXml(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(TimelineConfig));
            using (StreamReader reader = new StreamReader(path))
            {
                return (TimelineConfig) serializer.Deserialize(reader);
            }
        }
    }
}
