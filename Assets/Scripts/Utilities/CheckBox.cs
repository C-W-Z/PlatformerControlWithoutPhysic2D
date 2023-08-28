using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Extensions;

public class CheckBox : MonoBehaviour
{
    private enum Type
    {
        Rectangle,
        Circle,
        Rays
    }

    [SerializeField] private new Transform transform; // hide the default transform property
    [SerializeField] private Type type = Type.Rectangle;
    [SerializeField] private Color color = Color.red;
    [Header("Rectangle")]
    [SerializeField] private Vector2 size = new(1f, 1f);
    [Header("Circle")]
    [SerializeField] private float radius = 0.5f;
    [Header("Ray")]
    [SerializeField] private Vector2 direction = Vector2.right;
    [SerializeField] private float distance = 1f;
    [Header("Multiple Rays")]
    [SerializeField] private int rayCount = 1;
    [SerializeField] private Vector2 firstRayOffset = Vector2.zero;
    [SerializeField] private Vector2 lastRayOffset = Vector2.zero;
    public Vector2 Center => transform.position;

    void OnDrawGizmos()
    {
        Gizmos.color = this.color;
        switch (type)
        {
            case Type.Rectangle:
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, new(1, 1, 1));
                Gizmos.DrawWireCube(Vector3.zero, size);
                break;
            case Type.Circle:
                Gizmos.DrawWireSphere(transform.position, radius);
                break;
            case Type.Rays:
                if (rayCount == 1)
                    Gizmos.DrawLine(transform.position, transform.position + (Vector3)(direction * distance));
                else
                {
                    for (int i = 0; i <= rayCount - 1; i++)
                    {
                        float t = (float)i / (rayCount - 1);
                        Vector2 pos = transform.position + Vector3.Lerp(firstRayOffset, lastRayOffset, t);
                        Gizmos.DrawLine(pos, pos + direction * distance);
                    }
                }
                break;
        }
    }

    public bool Detect(LayerMask layer)
    {
        switch (type)
        {
            case Type.Rectangle:
                float angle = transform.rotation.z;
                return Physics2D.OverlapBox(transform.position, size, angle, layer);

            case Type.Circle:
                return Physics2D.OverlapCircle(transform.position, radius, layer);

            case Type.Rays:
                // check hit if only one ray
                if (rayCount == 1)
                    return Physics2D.Raycast(transform.position, direction, distance, layer);
                // check if any hit if nultiple rays
                for (int i = 0; i <= rayCount-1; i++)
                {
                    float t = (float)i / (rayCount-1);
                    Vector2 pos = transform.position + Vector3.Lerp(firstRayOffset, lastRayOffset, t);
                    if (Physics2D.Raycast(pos, direction, distance, layer))
                        return true;
                }
                // if no any hit
                return false;
        }
        return false;
    }

    public Vector2 GetHitPoint(LayerMask layer, Vector2 defaultPos)
    {
        CheckTypeIsRays();

        RaycastHit2D hit;
        if (rayCount == 1)
        {
            // get hit point if only one ray
            hit = Physics2D.Raycast(transform.position, direction, distance, layer);
            if (hit.collider != null)
                return hit.point;
        }
        else
        {
            // get any hit point if nultiple rays
            for (int i = 0; i <= rayCount-1; i++)
            {
                float t = (float)i / (rayCount-1);
                Vector2 pos = transform.position + Vector3.Lerp(firstRayOffset, lastRayOffset, t);
                hit = Physics2D.Raycast(pos, direction, distance, layer);
                if (hit.collider != null)
                    return hit.point;
            }
        }
        return defaultPos;
    }

#region Other Methods

    private void CheckTypeIsRays() =>
        Debug.Assert(type == Type.Rays, $"The type of the CheckBox '{name}' is not Type.Rays", this);

    // because the direction will not change as the transform.scale/rotation, use these function manually
    public void SetDir(Vector2 dir)
    {
        CheckTypeIsRays();
        direction = dir;
    }
    public void FlipDirX()
    {
        CheckTypeIsRays();
        direction.x = -direction.x;
    }
    public void FlipDirY()
    {
        CheckTypeIsRays();
        direction.y = -direction.y;
    }
    public void FlipDir()
    {
        CheckTypeIsRays();
        direction = -direction;
    }
    public void RotateDir(float delta)
    {
        CheckTypeIsRays();
        direction.RotateSelf(delta);
    }
    public void NormalizeDir()
    {
        CheckTypeIsRays();
        direction = direction.normalized;
    }

#endregion

#region Static Utilities

    public static bool DetectAny(CheckBox[] checkboxArray, LayerMask layer)
    {
        foreach (var c in checkboxArray)
            if (c.Detect(layer))
                return true;
        return false;
    }
    public static bool DetectAny(List<CheckBox> checkboxList, LayerMask layer)
    {
        foreach (var c in checkboxList)
            if (c.Detect(layer))
                return true;
        return false;
    }
    public static Vector2 GetAnyHitPoint(CheckBox[] checkboxArray, LayerMask layer, Vector2 defaultPos)
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
    public static Vector2 GetAnyHitPoint(List<CheckBox> checkboxList, LayerMask layer, Vector2 defaultPos)
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

#endregion
}

#region Inspector GUI

[CustomEditor(typeof(CheckBox))]
public class CheckBoxEditor : Editor
{
    public override void  OnInspectorGUI ()
    {
        DrawDefaultInspector();
        CheckBox checkbox = (CheckBox)target;
        if (GUILayout.Button("Normalize Direction"))
            checkbox.NormalizeDir();
    }
}

#endregion