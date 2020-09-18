﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Seqence;
using CharacterInfo = UnityEngine.Seqence.Data.CharacterInfo;

public class EntityMgr
{
    private static EntityMgr _inst;

    CharacterInfo config;

    Dictionary<uint, Entity> dict = new Dictionary<uint, Entity>();
    

    public static EntityMgr Instance
    {
        get
        {
            if (_inst == null)
            {
                _inst = new EntityMgr();
                var p = SeqenceUtil.chpath;
                _inst.config = XResources.LoadSharedAsset<CharacterInfo>(p);
            }
            return _inst;
        }
    }


    public void Create(uint uid, int confId)
    {
        var chars = config.characters;
        int len = chars.Length;
        for (int i = 0; i < len; i++)
        {
            if (chars[i].id == confId)
            {
                var e = SharedPool<Entity>.Get();
                e.Initial(uid, chars[i]);
                dict.Add(uid, e);
                break;
            }
        }
    }

    public void Update(float delta)
    {
        foreach(var it in dict)
        {
            it.Value.Update(delta);
        }
    }


    public void Destroy(Entity e)
    {
        dict.Remove(e.UID);
        SharedPool<Entity>.Return(e);
    }


    public void SyncPos(uint uid, Vector3 pos, Quaternion rot)
    {
        if (dict.ContainsKey(uid))
        {
            dict[uid].SetPos(pos);
            dict[uid].SetRot(rot);
        }
    }

    public void Play(uint uid, string skill)
    {
        if (dict.ContainsKey(uid))
        {
            dict[uid].PlaySkill(skill);
        }
    }

    public void DestroyEntity(uint uid)
    {
        if (dict.ContainsKey(uid))
        {
            Destroy(dict[uid]);
        }
    }

}
