﻿/*
 * @Author: wangyun
 * @CreateTime: 2022-02-24 22:13:25 463
 * @LastEditor: wangyun
 * @EditTime: 2022-02-24 22:13:23 560
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;

using UObject = UnityEngine.Object;

namespace LuaApp {
	[CustomEditor(typeof(LuaBehaviourWithPath), true)]
	public class LuaBehaviourWithPathInspector : LuaBehaviourInspector {
		protected LuaBehaviourWithPath LuaBehaviour => target as LuaBehaviourWithPath;

		private LuaInjectionDataDrawer m_InjectionDrawer;

		protected override void OnEnable() {
			base.OnEnable();
			m_InjectionDrawer = new LuaInjectionDataDrawer(true);
		}

		public override void OnInspectorGUI() {
			EditorGUI.BeginChangeCheck();
			DrawLuaInjection();
			DrawLuaPath();
			DrawLuaTable();
			DrawLuaRunner();
			if (EditorGUI.EndChangeCheck()) {
				EditorUtility.SetDirty(LuaBehaviour);
			}
		}

		protected void DrawLuaInjection() {
			Type type = LuaBehaviour.GetType();
			FieldInfo field = type.GetField("m_InjectionList", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field?.GetValue(LuaBehaviour) is List<Injection7> injections) {
				EditorGUI.BeginDisabledGroup(Application.isPlaying);
				m_InjectionDrawer.DrawDict(injections, LuaBehaviour);
				EditorGUI.EndDisabledGroup();
			}
		}

		protected void DrawLuaPath() {
			GUILayout.BeginHorizontal();
			EditorGUI.BeginDisabledGroup(Application.isPlaying);

			GUILayout.Label("Lua Script", GUILayout.Width(80F));

			bool settingsExist = File.Exists("ProjectSettings/LuaBehaviourSettings.asset");
			string luaPath = LuaBehaviour.luaPath;
			bool luaPathIsEmpty = string.IsNullOrEmpty(luaPath);
			string luaSrcPath = LuaBehaviourSettings.instance.luaSrcPath;
			string luaFileExtension = LuaBehaviourSettings.instance.luaFileExtension;
			UObject asset = !settingsExist || luaPathIsEmpty ? null : AssetDatabase.LoadAssetAtPath<TextAsset>(luaSrcPath + luaPath.Replace(".", "/") + luaFileExtension);;
			if (settingsExist && (luaPathIsEmpty || asset)) {
				// 已经设置过Lua根目录，且路径没错（路径为空或者能找到Lua文件），则支持文件拖放
				UObject newAsset = EditorGUILayout.ObjectField(asset, typeof(TextAsset), true);
				if (newAsset != asset) {
					// 根据lua文件记录lua路径
					if (!newAsset) {
						asset = null;
						luaPath = string.Empty;
						Undo.RecordObject(LuaBehaviour, "LuaPath");
						LuaBehaviour.luaPath = luaPath;
					} else {
						string newLuaPath = AssetDatabase.GetAssetPath(newAsset);
						if (newLuaPath.StartsWith(luaSrcPath) && newLuaPath.EndsWith(luaFileExtension)) {
							int length = newLuaPath.Length - luaSrcPath.Length - luaFileExtension.Length;
							luaPath = newLuaPath.Substring(luaSrcPath.Length, length).Replace("/", ".");
							asset = newAsset;
							Undo.RecordObject(LuaBehaviour, "LuaPath");
							LuaBehaviour.luaPath = luaPath;
						}
					}
				}
				// 推断按钮
				if (asset && GUILayout.Button("Deduce Fields", GUILayout.Width(100F))) {
					Type type = LuaBehaviour.GetType();
					FieldInfo field = type.GetField("m_InjectionList", BindingFlags.Instance | BindingFlags.NonPublic);
					if (field?.GetValue(target) is List<Injection7> injections) {
						LuaInjectionDataDeduce.DeduceFields(injections, luaPath);
					}
				}
			} else {
				// 尚未设置Lua根目录或找不到lua文件，显示文本框和设置按钮
				string newLuaPath = EditorGUILayout.TextField(luaPath);
				if (newLuaPath != luaPath) {
					Undo.RecordObject(LuaBehaviour, "LuaPath");
					LuaBehaviour.luaPath = newLuaPath;
				}
				// 设置按钮
				if (GUILayout.Button("LuaSrcPathSetting", GUILayout.Width(120F))) {
					SettingsService.OpenProjectSettings("Project/LuaBehaviour");
				}
			}
			EditorGUI.EndDisabledGroup();
			GUILayout.EndHorizontal();
		}
	}
}
