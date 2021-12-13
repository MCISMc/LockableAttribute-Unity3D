/// <summary>
/// Author: Mayur Chauhan
/// Email: mayurchauhan1995@gmail.com
/// </summary>

using UnityEngine;

namespace MCISMc.Lockable
{
    /// <summary>
    /// LockableAttribute is a custom attribute that can be used to Lock/Unlock Component properties in the inspector.
    /// </summary>
    public class LockableAttribute : PropertyAttribute
    {
        public LockableState IsLocked { get; private set; } = LockableState.Unlocked;
        public static bool ShowIcon { get; private set; } = true;

        /// <summary>
        /// LockableAttribute Property Constructor to set the locked state of the property
        /// </summary>
        /// <param name="lockState">Default State of the LockableAttribute</param>
        public LockableAttribute(LockableState lockState = LockableState.Unlocked)
        {
            this.IsLocked = lockState;
        }

        /// <summary>
        /// Toggle Show/Hide Icon State of the Lockable AttributeProperty in the inspector
        /// </summary>
        public static void Toggle_ShowIcon()
        {
            ShowIcon = !ShowIcon;
        }

        /// <summary>
        /// Update the LockableState of the LockableAttribute
        /// </summary>
        /// <param name="updatedState">UpdatedState of the LockableAttribute</param>
        public void Update_LockableState(LockableState updatedState)
        {
            this.IsLocked = updatedState;
        }

    }


    /// <summary>
    /// Lockable Attribute Property Lock/Unlock State
    /// </summary>
    public enum LockableState
    {
        Locked = 0,
        Unlocked = 1
    }

}