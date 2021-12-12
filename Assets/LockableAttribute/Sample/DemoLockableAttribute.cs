/// <summary>
/// Author: Mayur Chauhan
/// Email: mayurchauhan1995@gmail.com
/// </summary>

using UnityEngine;
using MCISMc.Lockable;

public class DemoLockableAttribute : MonoBehaviour
{
    [LockableAttribute] public int intValue = 0;
    [LockableAttribute] public float floatValue = 0.0f;
    [LockableAttribute] public string stringValue = "";
    [LockableAttribute] public bool boolValue = false;
    [LockableAttribute] public Vector3 vector3Value = Vector3.zero;
    [LockableAttribute] public Transform transformCurrent = null;
    [LockableAttribute] public GameObject prefabObject = null;
}

