﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License

using Microsoft.MixedReality.Toolkit.UI.Interaction;
using Microsoft.MixedReality.Toolkit.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Editor
{
    /// <summary>
    /// Custom inspector for the StateVisualizer component
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(StateVisualizer))]
    public class StateVisualizerInspector : UnityEditor.Editor
    {
        private StateVisualizer instance;
        private SerializedProperty interactiveElement;
        private SerializedProperty animator;
        private SerializedProperty stateContainers;

        private bool inPlayMode;

        private static GUIContent RemoveButtonLabel;
        private static GUIContent AddButtonLabel;

        private AnimatableProperty animatableProperty;

        protected virtual void OnEnable()
        {
            instance = (StateVisualizer)target;
            interactiveElement = serializedObject.FindProperty("interactiveElement");
            animator = serializedObject.FindProperty("animator");

            stateContainers = serializedObject.FindProperty("stateContainers");

            RemoveButtonLabel = new GUIContent(InspectorUIUtility.Minus, "Remove");
            AddButtonLabel = new GUIContent(InspectorUIUtility.Plus, "Add");
        }

        public override void OnInspectorGUI()
        {
            inPlayMode = EditorApplication.isPlaying || EditorApplication.isPaused;

            serializedObject.Update();

            RenderExperimentalWarning();

            RenderInitialProperties();

            if (instance.GetComponent<Animator>().runtimeAnimatorController != null)
            {
                RenderStateContainers();
            }

            RenderEndingButtons();

            serializedObject.ApplyModifiedProperties();
        }

        private void RenderExperimentalWarning()
        {
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("The State Visualizer is an experimental feature\n" +
                                    "Parts of the MRTK appear to have a lot of value even if the details " +
                                    "haven’t fully been fleshed out. For these types of features, we want " +
                                    "the community to see them and get value out of them early. Because " +
                                    "they are early in the cycle, we label them as experimental to indicate " +
                                    "that they are still evolving, and subject to change over time.", MessageType.Warning);
        }


        private void RenderInitialProperties()
        {
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(interactiveElement);

            EditorGUILayout.PropertyField(animator);

            EditorGUILayout.Space();
        }

        private void RenderStateContainers()
        {
            InspectorUIUtility.DrawTitle("State Animations");

            for (int i = 0; i < stateContainers.arraySize; i++)
            {
                SerializedProperty stateContainer = stateContainers.GetArrayElementAtIndex(i);
                SerializedProperty stateContainerName = stateContainer.FindPropertyRelative("stateName");
                SerializedProperty animationTargetsList = stateContainer.FindPropertyRelative("animationTargets");
                SerializedProperty stateContainerAnimationClip = stateContainer.FindPropertyRelative("animationClip");
                SerializedProperty animationTransitionDuration = stateContainer.FindPropertyRelative("animationTransitionDuration");

                Color previousGUIColor = GUI.color;

                using (new EditorGUILayout.HorizontalScope())
                {
                    string stateFoldoutID = stateContainerName.stringValue + "StateContainer" + "_" + target.name;

                    if (inPlayMode)
                    {
                        BaseInteractiveElement baseInteractiveElement = interactiveElement.objectReferenceValue as BaseInteractiveElement;

                        if (baseInteractiveElement.isActiveAndEnabled)
                        {
                            if (baseInteractiveElement.IsStateActive(stateContainerName.stringValue))
                            {
                                GUI.color = Color.cyan;
                            }
                        }
                    }

                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.Space();

                        if (InspectorUIUtility.DrawSectionFoldoutWithKey(stateContainerName.stringValue, stateFoldoutID, MixedRealityStylesUtility.TitleFoldoutStyle, false))
                        {
                            using (new EditorGUI.IndentLevelScope())
                            {
                                using (var check = new EditorGUI.ChangeCheckScope())
                                {
                                    EditorGUILayout.PropertyField(stateContainerAnimationClip);
                                    EditorGUILayout.PropertyField(animationTransitionDuration);
                                    
                                    if (check.changed)
                                    {
                                        instance.SetAnimationTransitionDuration(stateContainerName.stringValue, animationTransitionDuration.floatValue);
                                        instance.SetAnimationClip(stateContainerName.stringValue, stateContainerAnimationClip.objectReferenceValue as AnimationClip);
                                    }
                                }

                                RenderAnimationTargetList(animationTargetsList, stateContainerName);
                            }
                        }

                        EditorGUILayout.Space();
                    }

                    GUI.color = previousGUIColor;

                    if (!inPlayMode)
                    {
                        if (InspectorUIUtility.SmallButton(RemoveButtonLabel))
                        {
                            instance.RemoveAnimatorState(instance.RootStateMachine, stateContainerName.stringValue);
                            stateContainers.DeleteArrayElementAtIndex(i);
                            break;
                        }
                    }
                }
            }
        }

        private void RenderAnimationTargetList(SerializedProperty animationTargetList, SerializedProperty stateContainerName)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                for (int j = 0; j < animationTargetList.arraySize; j++)
                {
                    SerializedProperty animationTarget = animationTargetList.GetArrayElementAtIndex(j);
                    SerializedProperty targetObj = animationTarget.FindPropertyRelative("target");
                    SerializedProperty animatablePropertyList = animationTarget.FindPropertyRelative("stateAnimatableProperties");

                    EditorGUILayout.Space();
                 
                    using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                    {
                        EditorGUILayout.Space();

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.PropertyField(targetObj);

                            if (InspectorUIUtility.SmallButton(RemoveButtonLabel))
                            {
                                // Clear keyframes of a deleted target
                                for (int k = 0; k < animatablePropertyList.arraySize; k++)
                                {
                                    SerializedProperty animatableProperty = animatablePropertyList.GetArrayElementAtIndex(k);
                                    SerializedProperty animatablePropertyName = animatableProperty.FindPropertyRelative("animatablePropertyName");
                                    
                                    if (animatableProperty != null)
                                    {
                                        RemoveKeyFrames(stateContainerName.stringValue, animatablePropertyName.stringValue, k);
                                    }
                                }

                                animationTargetList.DeleteArrayElementAtIndex(j);
                                break;
                            }
                        }

                        using (new EditorGUILayout.VerticalScope())
                        {
                            if (targetObj.objectReferenceValue != null)
                            {
                                InspectorUIUtility.DrawDivider();

                                GameObject targetGameObject = targetObj.objectReferenceValue as GameObject;

                                // Ensure the target game object is valid
                                if (IsTargetObjectValid(targetGameObject))
                                {
                                    string animatablePropertiesFoldoutID = stateContainerName.stringValue + "AnimatableProperties" + "_" + targetGameObject.name + target.name;

                                    if (InspectorUIUtility.DrawSectionFoldoutWithKey(targetGameObject.name + " Animatable Properties", animatablePropertiesFoldoutID, MixedRealityStylesUtility.BoldFoldoutStyle, false))
                                    {
                                        using (new EditorGUI.IndentLevelScope())
                                        {
                                            RenderAnimatablePropertyList(animatablePropertyList, stateContainerName, j);
                                        }
                                    }
                                }
                                else
                                {
                                    targetObj.objectReferenceValue = null;
                                    Debug.LogError("The target object must be itself or a child object");
                                }
                            }

                            EditorGUILayout.Space();
                        }
                    }
                }

                EditorGUILayout.Space();

                RenderAddTargetButton(animationTargetList);
            }
        }

        private void RenderAddTargetButton(SerializedProperty animationTargetList)
        {
            if (GUILayout.Button("Add Target"))
            {
                animationTargetList.InsertArrayElementAtIndex(animationTargetList.arraySize);

                serializedObject.ApplyModifiedProperties();

                SerializedProperty newAnimationTarget = animationTargetList.GetArrayElementAtIndex(animationTargetList.arraySize - 1);
                newAnimationTarget.FindPropertyRelative("target").objectReferenceValue = null;

                SerializedProperty stateAnimatablePropertiesList = newAnimationTarget.FindPropertyRelative("stateAnimatableProperties");

                // Clear the new list
                for (int k = 0; k < stateAnimatablePropertiesList.arraySize; k++)
                {
                    stateAnimatablePropertiesList.DeleteArrayElementAtIndex(k);
                }

                serializedObject.ApplyModifiedProperties();
            }
        }

        private void RenderAnimatablePropertyList(SerializedProperty animatablePropertyList, SerializedProperty stateContainerName, int animationTargetIndex)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                for (int k = 0; k < animatablePropertyList.arraySize; k++)
                {
                    SerializedProperty animatableProperty = animatablePropertyList.GetArrayElementAtIndex(k);
                    SerializedProperty animatablePropertyName = animatableProperty.FindPropertyRelative("animatablePropertyName");

                    using (new EditorGUILayout.VerticalScope())
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            RenderAnimatableProperty(animatableProperty, animationTargetIndex);
                            serializedObject.ApplyModifiedProperties();

                            if (InspectorUIUtility.SmallButton(RemoveButtonLabel))
                            {
                                RemoveKeyFrames(stateContainerName.stringValue, animatablePropertyName.stringValue, animationTargetIndex);
                                animatablePropertyList.DeleteArrayElementAtIndex(k);
                                break;
                            }
                        }
                    }
                }

                InspectorUIUtility.DrawDivider();
            }

            RenderAddAnimatablePropertyMenuButton(stateContainerName.stringValue, animationTargetIndex);
        }

        private void RenderAddAnimatablePropertyMenuButton(string stateContainerName, int animationTargetIndex)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                animatableProperty = (AnimatableProperty)EditorGUILayout.EnumPopup(animatableProperty);

                if (GUILayout.Button("Add the " + animatableProperty.ToString() + " Animatable Property"))
                {
                    CreateAnimatablePropertyInstance(stateContainerName, animatableProperty.ToString(), animationTargetIndex);

                    serializedObject.ApplyModifiedProperties();
                }
            }
        }


        private void RenderAnimatableProperty(SerializedProperty animatableProperty, int animationTargetIndex)
        {
            SerializedProperty animatablePropertyName = animatableProperty.FindPropertyRelative("animatablePropertyName");
            SerializedProperty stateName = animatableProperty.FindPropertyRelative("stateName");
            SerializedProperty targetObj = animatableProperty.FindPropertyRelative("target");

            GameObject targetGameObject = targetObj.objectReferenceValue as GameObject;

            if (targetGameObject != null)
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUILayout.PropertyField(animatableProperty, true);

                    if (check.changed)
                    {
                        instance.SetKeyFrames(stateName.stringValue, animationTargetIndex);
                    }
                }
            }
        }

        private void RemoveKeyFrames(string stateName, string animatablePropertyName, int animationTargetIndex)
        {
            instance.RemoveKeyFrames(stateName, animationTargetIndex, animatablePropertyName);
        }

        private void CreateAnimatablePropertyInstance(string stateName, string propertyName, int animationTargetIndex)
        {
            instance.CreateAnimatablePropertyInstance(animationTargetIndex, propertyName, stateName);
        }

        private void RenderSetStateMachineButton()
        {
            if (GUILayout.Button("Generate New Animation Clips"))
            {
                instance.InitializeAnimatorControllerAsset();
            }
        }

        private void RenderSyncStatesButton()
        {
            if (GUILayout.Button("Sync States with Interactive Element"))
            {
                instance.UpdateStateContainerStates();
            }
        }

        private void RenderEndingButtons()
        {
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                RenderSetStateMachineButton();

                if (instance.EditorAnimatorController != null)
                {
                    RenderSyncStatesButton();
                }
            }
        }

        // A target game object is one that is itself or a child of the root
        private bool IsTargetObjectValid(GameObject targetObj)
        {
            Transform startTransform = targetObj.transform;
            Transform initialTransform = targetObj.transform;

            // If this game object has the State Visualizer attached 
            if (targetObj.GetComponent<StateVisualizer>() != null)
            {
                return true;
            }

            // If the current object is a root and does not have a parent 
            if (startTransform.parent != null)
            {
                // Traverse parents until the State Visualizer is found to determine if the current target is a valid child object
                while (startTransform.parent != initialTransform)
                {
                    if (startTransform.GetComponent<StateVisualizer>() != null)
                    {
                        return true;
                    }

                    startTransform = startTransform.parent;
                }
            }

            return false;
        }
    }
}
