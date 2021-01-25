using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MetaPointHandle
{
    // internal state for DragHandle()
    static int s_DragHandleHash = "DragHandleHash".GetHashCode();
    static Vector2 s_DragHandleMouseStart;
    static Vector2 s_DragHandleMouseCurrent;
    static Vector3 s_DragHandleWorldStart;
    static bool s_DragHandleHasMoved;

    public static Vector3 DragHandle(Vector3 position, Vector3 direction, float handleSize, Color colorSelected)
    {
        int id = GUIUtility.GetControlID(s_DragHandleHash, FocusType.Passive);

        Vector3 screenPosition = Handles.matrix.MultiplyPoint(position);
        Matrix4x4 cachedMatrix = Handles.matrix;

        switch (Event.current.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (HandleUtility.nearestControl == id && (Event.current.button == 0 || Event.current.button == 1))
                {
                    GUIUtility.hotControl = id;
                    s_DragHandleMouseCurrent = s_DragHandleMouseStart = Event.current.mousePosition;
                    s_DragHandleWorldStart = position;
                    s_DragHandleHasMoved = false;

                    Event.current.Use();
                    EditorGUIUtility.SetWantsMouseJumping(1);

                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == id && (Event.current.button == 0 || Event.current.button == 1))
                {
                    GUIUtility.hotControl = 0;
                    Event.current.Use();
                    EditorGUIUtility.SetWantsMouseJumping(0);

                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == id)
                {
                    s_DragHandleMouseCurrent += new Vector2(Event.current.delta.x, -Event.current.delta.y);
                    Vector3 position2 = Camera.current.WorldToScreenPoint(Handles.matrix.MultiplyPoint(s_DragHandleWorldStart))
                        + (Vector3)(s_DragHandleMouseCurrent - s_DragHandleMouseStart);
                    
                    position = Handles.matrix.inverse.MultiplyPoint(Camera.current.ScreenToWorldPoint(position2));
                    
                    /*
                    if (Camera.current.transform.forward == Vector3.forward || Camera.current.transform.forward == -Vector3.forward)
                        position.z = s_DragHandleWorldStart.z;
                    if (Camera.current.transform.forward == Vector3.up || Camera.current.transform.forward == -Vector3.up)
                        position.y = s_DragHandleWorldStart.y;
                    if (Camera.current.transform.forward == Vector3.right || Camera.current.transform.forward == -Vector3.right)
                        position.x = s_DragHandleWorldStart.x;
                        */
                    position = Vector3.Project(position - s_DragHandleWorldStart, direction) + s_DragHandleWorldStart;

                    s_DragHandleHasMoved = true;

                    GUI.changed = true;
                    Event.current.Use();
                }
                break;

            case EventType.Repaint:
                Color currentColour = Handles.color;
                if (id == GUIUtility.hotControl && s_DragHandleHasMoved)
                    Handles.color = colorSelected;

                Handles.matrix = Matrix4x4.identity;
                Handles.ArrowHandleCap(id, screenPosition, Quaternion.LookRotation(direction, Vector3.up), handleSize, EventType.Repaint);
                Handles.matrix = cachedMatrix;

                Handles.color = currentColour;
                break;

            case EventType.Layout:
                Handles.matrix = Matrix4x4.identity;
                HandleUtility.AddControl(id, HandleUtility.DistanceToLine(position, position + direction * handleSize) / 2f);
                Handles.matrix = cachedMatrix;
                break;
        }

        return position;
    }
}
