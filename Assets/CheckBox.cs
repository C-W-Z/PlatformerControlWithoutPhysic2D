using System.Collections.Generic;
using UnityEngine;

public class CheckBox : MonoBehaviour
{
    private enum Type
    {
        Rectangle,
        Circle,
        Ray
    }

    [SerializeField] private Transform tf;
    [SerializeField] private Type type = Type.Rectangle;
    [SerializeField] private Color color = Color.red;
    [Header("Rectangle")]
    [SerializeField] private Vector2 size;
    [Header("Circle")]
    [SerializeField] private float radius;
    [Header("Ray")]
    [SerializeField] private Vector2 direction;
    [SerializeField] private float distance = 1f;
    public Vector2 Center => tf.position;

    void OnDrawGizmos()
    {
        Gizmos.color = this.color;
        switch (type)
        {
            case Type.Rectangle:
                Gizmos.DrawWireCube(tf.position, size);
                break;
            case Type.Circle:
                Gizmos.DrawWireSphere(tf.position, radius);
                break;
            case Type.Ray:
                Gizmos.DrawLine(tf.position, tf.position + (Vector3)(direction * distance));
                break;
        }
    }

    public bool Detect(LayerMask layer)
    {
        return type switch
        {
            Type.Rectangle => (bool)Physics2D.OverlapBox(tf.position, size, 0, layer),
            Type.Circle => (bool)Physics2D.OverlapCircle(tf.position, radius, layer),
            Type.Ray => (bool)Physics2D.Raycast(tf.position, direction, distance, layer),
            _ => false
        };
    }

    public Vector2 GetHitPoint(LayerMask layer, Vector2 defaultPos)
    {
        if (type != Type.Ray)
            return defaultPos;
        RaycastHit2D hit = Physics2D.Raycast(tf.position, direction, distance, layer);
        if (hit.collider != null)
            return hit.point;
        return defaultPos;
    }

    public void FlipDirX()
    {
        direction.x = -direction.x;
    }

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
    public static Vector2 GetHitPoint(CheckBox[] checkboxArray, LayerMask layer, Vector2 defaultPos)
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
    public static Vector2 GetHitPoint(List<CheckBox> checkboxList, LayerMask layer, Vector2 defaultPos)
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