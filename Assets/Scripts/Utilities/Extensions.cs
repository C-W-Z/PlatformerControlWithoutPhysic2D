#define Extentions

using System.Collections.Generic;
using UnityEngine;

public static class Vector2Extension
{
    public static Vector2 Rotate(this Vector2 v, float delta)
    {
        return new Vector2(
            v.x * Mathf.Cos(delta) - v.y * Mathf.Sin(delta),
            v.x * Mathf.Sin(delta) + v.y * Mathf.Cos(delta)
        );
    }
    public static void RotateSelf(this Vector2 v, float delta)
    {
        v = new Vector2(
            v.x * Mathf.Cos(delta) - v.y * Mathf.Sin(delta),
            v.x * Mathf.Sin(delta) + v.y * Mathf.Cos(delta)
        );
    }
}

public static class CheckBoxExtension
{
    public static bool DetectAny(this CheckBox[] checkboxArray, LayerMask layer)
    {
        foreach (var c in checkboxArray)
            if (c.Detect(layer))
                return true;
        return false;
    }
    public static bool DetectAny(this List<CheckBox> checkboxList, LayerMask layer)
    {
        foreach (var c in checkboxList)
            if (c.Detect(layer))
                return true;
        return false;
    }
    public static Vector2 GetAnyHitPoint(this CheckBox[] checkboxArray, LayerMask layer, Vector2 defaultPos)
    {
        Vector2 hitPoint;
        foreach (var c in checkboxArray)
        {
            hitPoint = c.GetHitPoint(layer, defaultPos);
            if (hitPoint != defaultPos)
                return hitPoint;
        }
        return defaultPos;
    }
    public static Vector2 GetAnyHitPoint(this List<CheckBox> checkboxList, LayerMask layer, Vector2 defaultPos)
    {
        Vector2 hitPoint;
        foreach (var c in checkboxList)
        {
            hitPoint = c.GetHitPoint(layer, defaultPos);
            if (hitPoint != defaultPos)
                return hitPoint;
        }
        return defaultPos;
    }
}