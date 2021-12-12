/// <summary>
/// Author: Mayur Chauhan
/// Email: mayurchauhan1995@gmail.com
/// </summary>

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MCISMc.Lockable
{
    [CustomPropertyDrawer(typeof(LockableAttribute))]
    internal class LockableAttributeDrawer : PropertyDrawer
    {

        #region Variables
        private const string ID_SAVE_NAME = "LockableAttributes_IDList";
        private const string VALUES_SAVE_NAME = "LockableAttribute_ValuesList";
        private const string LOCKED_ICON = "IN LockButton on";
        private const string UNLOCKED_ICON = "IN LockButton act@2x";

        private readonly Dictionary<SerializedProperty, LockableAttribute> _propertyLockableAttributesPair = new Dictionary<SerializedProperty, LockableAttribute>();
        private readonly Dictionary<Scene, List<SerializedProperty>> _sceneProperties = new Dictionary<Scene, List<SerializedProperty>>();
        private readonly Dictionary<string, LockableState> _savedLockableState = new Dictionary<string, LockableState>();
        private LockableAttribute _lockableAttribute;
        private SerializedProperty _currentProperty;

        private bool IsLocked { get { return (this._lockableAttribute.IsLocked == LockableState.Locked) ? true : false; } }
        private string LockableStateIcon { get { return IsLocked ? UNLOCKED_ICON : LOCKED_ICON; } }
        private string LockableStateMessage { get { return IsLocked ? "Unlock Selected Property" : "Lock Current Value"; } }
        private string ShowIconMessage { get { return LockableAttribute.ShowIcon ? "Hide LockableAttribute Icons" : "Show LockableAttribute Icons"; } }

        #endregion Variables


        #region Overriden PropertyDrawer Methods
        /// <summary>
        /// Override the OnGUI method to draw the custom property drawer for LockableAttribute.
        /// </summary>
        /// <param name="position">Current Property Position</param>
        /// <param name="property">Current Property</param>
        /// <param name="label">Current Property Label</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            this._lockableAttribute = attribute as LockableAttribute;
            this._currentProperty = property;

            // Context menu of the LockableAttribute on Mouse Right Click 
            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && position.Contains(Event.current.mousePosition))
            {
                GenericMenu context = new GenericMenu();

                // Add only relevant Lock/ Unlock context menu item
                switch (this._lockableAttribute.IsLocked)
                {
                    case LockableState.Locked:
                        context.AddItem(new GUIContent(LockableStateMessage), false, Update_LockableState(LockableState.Unlocked));
                        break;
                    case LockableState.Unlocked:
                        context.AddItem(new GUIContent(LockableStateMessage), false, Update_LockableState(LockableState.Locked));
                        break;
                }

                context.AddSeparator("");

                // Context menu item to toggle Show/Hide LockableAttribute Icons
                context.AddItem(new GUIContent(ShowIconMessage), false, Toggle_ShowIcon());

                // Context menu item to Reset LockableAttribute to default value
                context.AddItem(new GUIContent("Reset LockableAttribute"), false, Reset_LockableAttribute());

                context.ShowAsContext();
            }

            // Fetch Current LockableAttribute's Lockable state from saved data
            ulong targetObjectID = GetId(property.serializedObject.targetObject);
            string propertySaveName = $"{targetObjectID}_{property.name}";

            if (!this._savedLockableState.ContainsKey(propertySaveName))
                Fetch_SavedPropertyStateData();

            if (this._savedLockableState.ContainsKey(propertySaveName))
                this._lockableAttribute.Update_LockableState(_savedLockableState[propertySaveName]);

            // Lock/ Unlock the current property based on the LockableAttribute's LockableState
            GUI.enabled = !IsLocked;

            // Draw LockableAttribute Icon if ShowIcon is true
            if (LockableAttribute.ShowIcon) label.image = EditorGUIUtility.IconContent(LockableStateIcon).image;
            else label.image = null;

            EditorGUI.PropertyField(position, property, label, true);

            // Reset GUI Enabled State
            GUI.enabled = true;

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Override the GetPropertyHeight method to set the height of the custom property drawer for LockableAttribute.
        /// </summary>
        /// <param name="property">Current Property</param>
        /// <param name="label">Current Property Label</param>
        /// <returns></returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true) + EditorGUIUtility.standardVerticalSpacing;
        }

        #endregion Overriden PropertyDrawer Methods


        #region Context Menu Methods
        /// <summary>
        //// Todo Reset the LockableAttribute to default value
        /// </summary>
        /// <returns>GenericMenu.MenuFunction for Context Menu Item</returns>
        private GenericMenu.MenuFunction Reset_LockableAttribute()
        {
            return () =>
            {
                // Reset EditorPrefs Values
                EditorPrefs.SetString(ID_SAVE_NAME, string.Empty);
                EditorPrefs.SetString(VALUES_SAVE_NAME, string.Empty);

                string[] IDList = JsonHelper.FromJson<string>(EditorPrefs.GetString(ID_SAVE_NAME));
                LockableState[] valueList = JsonHelper.FromJson<LockableState>(EditorPrefs.GetString(VALUES_SAVE_NAME));

                if (IDList == null || valueList == null) { return; }

                for (int i = 0; i < IDList.Length; i++)
                {
                    string ID = IDList[i];
                    this._savedLockableState[ID] = LockableState.Unlocked;
                }

                SavePropertiesStateData();
            };
        }

        /// <summary>
        /// Update the LockableAttribute's Lockable state
        /// </summary>
        /// <param name="updatedState">Updated LockableAttribute's Lockablestate</param>
        /// <returns>GenericMenu.MenuFunction for Context Menu Item</returns>
        private GenericMenu.MenuFunction Update_LockableState(LockableState updatedState)
        {
            return () =>
            {
                this._lockableAttribute.Update_LockableState(updatedState);
                UpdateTracked(this._currentProperty);
                SavePropertiesStateData();
            };
        }

        /// <summary>
        /// Toggle the Show/Hide Icons for LockableAttribute Properties
        /// </summary>
        /// <returns>GenericMenu.MenuFunction for Context Menu Item</returns>
        private GenericMenu.MenuFunction Toggle_ShowIcon()
        {
            return () =>
            {
                LockableAttribute.Toggle_ShowIcon();
                UpdateTracked(this._currentProperty);
                SavePropertiesStateData();
            };
        }

        #endregion Context Menu Methods


        #region Save/Fetch Methods
        /// <summary>
        /// Save the current LockableAttribute's Lockable state to saved data after scene save.
        /// </summary>
        /// <param name="scene">Current Scene</param>
        private void UpdateOnSceneSave(Scene scene)
        {
            // Return if the sceneProperties dictionary does not contain the current scene
            if (!this._sceneProperties.ContainsKey(scene)) return;

            // Update the LockableAttribute's Lockable state for each tracked property
            foreach (SerializedProperty property in this._sceneProperties[scene])
            {
                // Continue execution if value is null
                if (property?.serializedObject?.targetObject == null) continue;

                ulong targetObjectID = GetId(property.serializedObject.targetObject);
                string propertySaveName = $"{targetObjectID}_{property.name}";
                this._savedLockableState[propertySaveName] = this._propertyLockableAttributesPair[property].IsLocked;
                // Debug.Log($"{property.name} - {propertySaveName} - {this._propertyLockableAttributesPair[property].IsLocked}");

                // Save Data in to EditorPrefs
                SavePropertiesStateData();
            }

            // Remove the current scene from the sceneProperties dictionary
            this._sceneProperties.Remove(scene);
        }

        /// <summary>
        /// Save the current LockableAttribute's Lockable state to saved data in-to EditorPrefs.
        /// </summary>
        private void SavePropertiesStateData()
        {
            // Store Key and Value pair data into arrays
            string[] IDList = this._savedLockableState.Keys.ToArray();
            LockableState[] lockableStateValues = this._savedLockableState.Values.ToArray();

            // Convert arrays to Json string
            string str_IDList = JsonHelper.ToJson(IDList);
            string str_lockableStateValues = JsonHelper.ToJson(lockableStateValues);

            // Save data to EditorPrefs
            EditorPrefs.SetString(ID_SAVE_NAME, str_IDList);
            EditorPrefs.SetString(VALUES_SAVE_NAME, str_lockableStateValues);
        }

        /// <summary>
        /// Fetch Saved Properties's LockableState from EditorPrefs
        /// </summary>
        private void Fetch_SavedPropertyStateData()
        {
            // Convert Json string to arrays
            string[] IDList = JsonHelper.FromJson<string>(EditorPrefs.GetString(ID_SAVE_NAME));
            LockableState[] lockableStateValues = JsonHelper.FromJson<LockableState>(EditorPrefs.GetString(VALUES_SAVE_NAME));

            // return if no saved properties
            if (IDList == null || lockableStateValues == null) return;

            for (int i = 0; i < IDList.Length; i++)
            {
                string targetObjectID = IDList[i];
                this._savedLockableState[targetObjectID] = lockableStateValues[i];
            }
        }

        /// <summary>
        /// Update the tracked properties dictionary with the current property
        /// </summary>
        /// <param name="property">Current LockableAttribute Property</param>
        private void UpdateTracked(SerializedProperty property)
        {
            this._propertyLockableAttributesPair[this._currentProperty] = this._lockableAttribute;
            ulong targetObjectID = GetId(this._currentProperty.serializedObject.targetObject);
            string propertySaveName = $"{targetObjectID}_{ this._currentProperty.name}";
            this._savedLockableState[propertySaveName] = this._lockableAttribute.IsLocked;

            GameObject go = (property.serializedObject.targetObject as MonoBehaviour)?.gameObject;

            // Return if GameObject is null or Scene is set to dirty
            if (go == null || !go.scene.isDirty) return;

            // Save Scene Data if not already saved
            if (this._sceneProperties.Keys.Count == 0)
                EditorSceneManager.sceneSaved += UpdateOnSceneSave;

            // If Scene Data is saved, update the sceneProperties dictionary
            if (this._sceneProperties.ContainsKey(go.scene))
            {
                if (!this._sceneProperties[go.scene].Contains(property))
                {
                    this._sceneProperties[go.scene].Add(property);
                }
            }
            else
            {
                this._sceneProperties[go.scene] = new List<SerializedProperty>() { property };
            }
        }

        /// <summary>
        /// Get GlobalObjectId of the given object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static ulong GetId(Object obj)
        {
            return (obj != null) ? GlobalObjectId.GetGlobalObjectIdSlow(obj).targetObjectId : 0;
        }

        #endregion Save/Fetch Methods
    }

    #region Helper Classes
    /// <summary>
    /// Json Helper Class to convert arrays to Json string and vice versa
    /// </summary>
    public static class JsonHelper
    {

        /// <summary>
        /// Convert JSON string to array of type T
        /// </summary>
        /// <param name="json">String data value stored in json format</param>
        /// <typeparam name="T">Object Type <T></typeparam>
        /// <returns>Array of Given Type <T></returns>
        public static T[] FromJson<T>(string json)
        {
            var wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            return wrapper?.items;
        }

        /// <summary>
        /// Convert array of type T to JSON string
        /// </summary>
        /// <param name="array">Array of type <T></param>
        /// <param name="prettyPrint"></param>
        /// <typeparam name="T">Object Type <T></typeparam>
        /// <returns>JSON string</returns>
        public static string ToJson<T>(T[] array, bool prettyPrint = false)
        {
            var wrapper = new Wrapper<T>
            {
                items = array
            };
            return JsonUtility.ToJson(wrapper, prettyPrint);
        }

        [Serializable]
        internal class Wrapper<T>
        {
            public T[] items;
        }
    }

    #endregion Helper Classes

}