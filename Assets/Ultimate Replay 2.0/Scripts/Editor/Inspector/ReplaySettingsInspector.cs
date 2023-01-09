using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UltimateReplay.Core;
using UltimateReplay.Core.StatePreparer;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions.Must;

namespace UltimateReplay
{
	[CustomEditor(typeof(UltimateReplay))]
	public class ReplaySettingsInspector : Editor
	{
		// Private
		private int selection = 0;
		private UltimateReplay instance = null;
		private Stack<ReplayObject> removePrefabs = new Stack<ReplayObject>();
		private Stack<SerializableType> removeTypes = new Stack<SerializableType>();

		// Methods
		public override void OnInspectorGUI()
		{
			// Get settings instance
			if(instance == null)
				instance = target as UltimateReplay;


			// Display a toolbar
			selection = GUILayout.Toolbar(selection, new string[] { "General", "State Preparation" });
			GUILayout.Space(10);

			// General settings
			if (selection == 0)
			{
				// Record options
				GUILayout.BeginVertical(EditorStyles.helpBox);
				{
					// Label
					GUILayout.Label("Record Options", EditorStyles.largeLabel);
					DrawUnderLine();

					// Properties
					EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(UltimateReplay.recordOptions)).FindPropertyRelative("recordFPS"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(UltimateReplay.recordOptions)).FindPropertyRelative("recordUpdateMethod"));
				}
				GUILayout.EndVertical();
				GUILayout.Space(10);

				// Playback options
				GUILayout.BeginVertical(EditorStyles.helpBox);
				{
					// Label
					GUILayout.Label("Playback Options", EditorStyles.largeLabel);
					DrawUnderLine();

					// Properties
					EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(UltimateReplay.playbackOptions)).FindPropertyRelative("playbackFPS"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(UltimateReplay.playbackOptions)).FindPropertyRelative("playbackUpdateMethod"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(UltimateReplay.playbackOptions)).FindPropertyRelative("playbackEndBehaviour"));
				}
				GUILayout.EndVertical();
				GUILayout.Space(10);

				// Prefabs
				GUILayout.BeginVertical(EditorStyles.helpBox);
				{
					// Label
					GUILayout.Label("Replay Prefabs", EditorStyles.largeLabel);
					DrawUnderLine();

					// Display all prefabs
					for(int i = 0; i < instance.prefabs.Prefabs.Count; i++)
                    {
						GUILayout.BeginHorizontal();
                        {
							GUILayout.Label((i + 1).ToString() + ". ");

							// Display object field
							instance.prefabs.Prefabs[i] = EditorGUILayout.ObjectField(instance.prefabs.Prefabs[i], typeof(ReplayObject), false, GUILayout.MaxWidth(500)) as ReplayObject;

							// Clear button
							GUILayout.FlexibleSpace();

							if(GUILayout.Button("X", GUILayout.Width(20)) == true)
                            {
								// Mark for removal
								removePrefabs.Push(instance.prefabs.Prefabs[i]);

                                // Mark as dirty
                                EditorUtility.SetDirty(instance);
                            }
                        }
						GUILayout.EndHorizontal();
                    }

					// Remove dead objects
					while (removePrefabs.Count > 0)
						instance.prefabs.Prefabs.Remove(removePrefabs.Pop());
				}
				GUILayout.EndVertical();

				// Add new prefab
				GUILayout.Space(-4);
				GUILayout.BeginHorizontal();
				{
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Add Prefab", EditorStyles.toolbarButton) == true)
					{
						instance.prefabs.Prefabs.Add(null);

                        // Mark as dirty
                        EditorUtility.SetDirty(instance);
					}
				}
				GUILayout.EndHorizontal();


				// Apply changes
				serializedObject.ApplyModifiedProperties();

				//base.OnInspectorGUI();
			}
			// State preparation settings
			else if(selection == 1)
            {
				GUILayout.Label("Default Replay Preparer Configuration");
				GUILayout.Space(10);

				// Useful hint
				EditorGUILayout.HelpBox("This configuration only applies when using the included 'Default Replay Preparer' for replay state preparation", MessageType.Info);

				GUILayout.BeginVertical(EditorStyles.helpBox);
				{
					// Title
					GUILayout.Label("Ignore Component Types", EditorStyles.largeLabel);
					DrawUnderLine();


					// Display all serialized types
					foreach (SerializableType type in instance.defaultReplayPreparer.SkipTypes)
					{
						// Get the system type
						string typeName = type.SystemType.FullName;

						GUILayout.BeginHorizontal();
						{
							// Display the name info
							GUILayout.Label(typeName);

							// Clear button
							GUILayout.FlexibleSpace();

							if (GUILayout.Button("X", GUILayout.Width(20)) == true)
							{
								removeTypes.Push(type);

								// Mark as dirty
								EditorUtility.SetDirty(instance);
							}
						}
						GUILayout.EndHorizontal();
					}

					// Clear dead types
					while (removeTypes.Count > 0)
						instance.defaultReplayPreparer.SkipTypes.Remove(removeTypes.Pop());
				}
				GUILayout.EndVertical();

				// Add new ignore type
				GUILayout.Space(-4);
				GUILayout.BeginHorizontal();
				{
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Add Type", EditorStyles.toolbarButton) == true)
					{
						// Show the menu
						ShowAddTypeContextMenu();
					}
				}
				GUILayout.EndHorizontal();


				// Component preparer settings
				GUILayout.Space(10);
				GUILayout.Label("Component Processors");

				// Display all
				foreach(DefaultReplayPreparer.ComponentPreparerSettings preparerSetting in instance.defaultReplayPreparer.PreparerSettings)
                {
					GUILayout.BeginVertical(EditorStyles.helpBox);
                    {
						// Label
						GUILayout.Label(preparerSetting.componentPreparerType.SystemType.FullName, EditorStyles.largeLabel);
						DrawUnderLine();

						// Enabled property
						GUILayout.BeginHorizontal();
						{
							GUILayout.Label("Enabled", GUILayout.Width(EditorGUIUtility.labelWidth));
							bool result = EditorGUILayout.Toggle(preparerSetting.enabled);

							if (result != preparerSetting.enabled)
							{
								preparerSetting.enabled = result;

								// Mark as dirty
								EditorUtility.SetDirty(instance);
							}
						}
						GUILayout.EndHorizontal();
                    }
					GUILayout.EndVertical();
                }
			}
		}

		private void ShowAddTypeContextMenu()
        {
			GenericMenu menu = new GenericMenu();

			// Get all component types
			foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
				foreach(Type type in asm.GetTypes())
                {
					// Check for component
					if(typeof(Component).IsAssignableFrom(type) == true)
                    {
						// Check for already added
						if(instance.defaultReplayPreparer.HasSkipType(type) == false)
                        {
							if (string.IsNullOrEmpty(type.Namespace) == true)
							{
								menu.AddItem(new GUIContent(type.Name), false, OnAddTypeContextMenuClicked, type);
							}
							else
							{
								menu.AddItem(new GUIContent(string.Concat(type.Namespace, "/", type.Name)), false, OnAddTypeContextMenuClicked, type);
							}
                        }
                    }
                }
            }

			menu.ShowAsContext();
        }

		private void OnAddTypeContextMenuClicked(object item)
        {
			// Add the new type
			instance.defaultReplayPreparer.SkipTypes.Add((Type)item);

			// Mark as dirty
			EditorUtility.SetDirty(instance);
        }

		private void DrawUnderLine()
        {
			Rect last = GUILayoutUtility.GetLastRect();
			last.y += last.height;
			last.height = 1;

			EditorGUI.DrawRect(last, Color.gray);
		}
	}
}
