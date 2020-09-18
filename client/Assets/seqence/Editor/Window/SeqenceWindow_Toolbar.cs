﻿using System;
using UnityEngine;
using PlayMode = UnityEngine.Seqence.PlayMode;

namespace UnityEditor.Seqence
{
    public partial class SeqenceWindow
    {
        void TransportToolbarGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginHorizontal(GUILayout.Width(WindowConstants.playmodeWidth));
            {
                PlayModeGUI();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.Width(WindowConstants.sliderWidth));
            {
                GotoBeginingSequenceGUI();
                PreviousEventButtonGUI();
                PlayButtonGUI();
                NextEventButtonGUI();
                GotoEndSequenceGUI();
                GUILayout.FlexibleSpace();
                TimeCodeGUI();
            }
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(state.Name);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal(GUILayout.Width(WindowConstants.sliderWidth));
            {
                NewButtonGUI();
                OpenButtonGUI();
                SaveButtonGUI();
                GUILayout.Space(4);
                InspectGUI();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();
        }

        void PlayModeGUI()
        {
            var pmde = playMode;
            playMode = (PlayMode) EditorGUILayout.EnumPopup(playMode, EditorStyles.toolbarPopup);
            if (timeline)
            {
                if (!string.IsNullOrEmpty(state.path) && playMode != pmde && playMode != timeline.playMode)
                {
                    string tip = "current is editing" + state.path.Substring(7) + ", Are you sure change playmode?";
                    if (EditorUtility.DisplayDialog("warn", tip, "ok", "cancel"))
                    {
                        state.Dispose();
                    }
                    else
                    {
                        playMode = pmde;
                    }
                    GUIUtility.ExitGUI();
                }
                else
                {
                    timeline.playMode = playMode;
                }
            }
        }

        void GotoBeginingSequenceGUI()
        {
            if (GUILayout.Button(SeqenceStyle.gotoBeginingContent, EditorStyles.toolbarButton))
            {
                state.FrameStart();
                rangeX2 = rangeX2 - rangeX1;
                rangeX1 = 0.01f;
                Repaint();
            }
        }

        void GotoEndSequenceGUI()
        {
            if (GUILayout.Button(SeqenceStyle.gotoEndContent, EditorStyles.toolbarButton))
            {
                state.FrameEnd();
                float end = timeline.RecalcuteDuration();
                float len = rangeX2 - rangeX1;
                rangeX2 = end;
                rangeX1 = rangeX2 - len;
                Repaint();
            }
        }


        void PlayButtonGUI()
        {
            EditorGUI.BeginChangeCheck();
            var isPlaying = GUILayout.Toggle(state.playing, SeqenceStyle.playContent, EditorStyles.toolbarButton);
            if (EditorGUI.EndChangeCheck())
            {
                state.SetPlaying(isPlaying);
            }
        }

        void NextEventButtonGUI()
        {
            if (GUILayout.Button(SeqenceStyle.nextFrameContent, EditorStyles.toolbarButton))
            {
                state.NextFrame();
            }
        }

        void PreviousEventButtonGUI()
        {
            if (GUILayout.Button(SeqenceStyle.previousFrameContent, EditorStyles.toolbarButton))
            {
                state.PrevFrame();
            }
        }

        void TimeCodeGUI()
        {
            EditorGUI.BeginChangeCheck();

            string currentTime = state.seqence != null ? state.seqence.Time.ToString("F1") : "0";
            var r = EditorGUILayout.GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.toolbarTextField,
                GUILayout.MinWidth(WindowConstants.minTimeCodeWidth));
            var id = GUIUtility.GetControlID("RenameFieldTextField".GetHashCode(), FocusType.Passive, r);
            var newCurrentTime = EditorGUI.DelayedTextFieldInternal(r, id, GUIContent.none, currentTime, null,
                EditorStyles.toolbarTextField);

            if (EditorGUI.EndChangeCheck())
            {
                state.seqence?.ProcessTo(float.Parse(newCurrentTime));
            }
        }

        void NewButtonGUI()
        {
            if (GUILayout.Button(SeqenceStyle.newContent, EditorStyles.toolbarButton))
            {
                if (state.seqence != null)
                {
                    if (EditorUtility.DisplayDialog("warn", "save current?", "save", "cancel"))
                    {
                        DoSave();
                    }
                    else
                    {
                        state.Dispose();
                    }
                }
                string dir = playMode == PlayMode.Plot ? WindowConstants.plotPath : WindowConstants.skillPath;
                string path = EditorUtility.SaveFilePanel("create timeline", dir, "timeline", "bytes");
                if (!string.IsNullOrEmpty(path))
                {
                    path = path.Substring(path.IndexOf("Assets/", StringComparison.Ordinal));
                    CreateTimeline(path);
                }
                GUIUtility.ExitGUI();
            }
        }

        void CreateTimeline(string path)
        {
            if (path.Contains(WindowConstants.plotPath))
            {
                playMode = PlayMode.Plot;
            }
            if (path.Contains(WindowConstants.skillPath))
            {
                playMode = PlayMode.Skill;
            }
            state.CreateTimeline(path, playMode);
            AssetDatabase.ImportAsset(path);
            AssetDatabase.Refresh();
        }

        void OpenButtonGUI()
        {
            if (GUILayout.Button(SeqenceStyle.openContent, EditorStyles.toolbarButton))
            {
                string dir = playMode == PlayMode.Plot ? WindowConstants.plotPath : WindowConstants.skillPath;
                string path = EditorUtility.OpenFilePanel("open", dir, "xml");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.Contains(WindowConstants.plotPath))
                    {
                        playMode = PlayMode.Plot;
                    }
                    if (path.Contains(WindowConstants.skillPath))
                    {
                        playMode = PlayMode.Skill;
                    }
                    path = "Assets" + path.Replace(Application.dataPath, "");
                    state.Open(path, playMode);
                }
                GUIUtility.ExitGUI();
            }
        }

        void SaveButtonGUI()
        {
            if (GUILayout.Button(SeqenceStyle.saveContent, EditorStyles.toolbarButton))
            {
                if (state.seqence == null)
                {
                    EditorUtility.DisplayDialog("warn", "not create timeline in editor", "ok");
                }
                else
                {
                    DoSave();
                }
                GUIUtility.ExitGUI();
            }
        }

        void InspectGUI()
        {
            if (GUILayout.Button(SeqenceStyle.refreshContent, EditorStyles.toolbarButton, GUILayout.MaxWidth(24)))
            {
                float d = timeline.RecalcuteDuration();
                SetTimeRange(0, d * 1.5f);
            }
            if (GUILayout.Button(SeqenceStyle.inspectBtn, EditorStyles.toolbarButton))
            {
                SeqenceInspector.ShowWindow();
            }
        }

        private void DoSave()
        {
            state.Save();
            AssetDatabase.Refresh();
        }
    }
}
