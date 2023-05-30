/******************************************************
 * FlieName:旋转列表编辑器
 * Auth:    Gasol.X
 * Date:    2021.4.23 11:42
 * Version: V1.0
 ******************************************************/
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(CirculateScroll))]
public class CirculateScrollEditor : Editor
{ 
    public override void OnInspectorGUI() {

        var circulateScroll = target as CirculateScroll;
        if (m_IsDebug) {
            base.OnInspectorGUI();
        }
        GUILayout.Space(20);

        EditorGUILayout.PropertyField(m_DragUnit);
        EditorGUILayout.PropertyField(m_DragType);
        if (m_DragType.enumValueIndex == 2) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_VerticalRate);
            EditorGUILayout.PropertyField(m_HorizontalRate);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.PropertyField(m_DragMovement);
        EditorGUILayout.PropertyField(m_MoveType);
        if (m_MoveType.enumValueIndex == 1) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_ElasticVelocity);
            EditorGUILayout.PropertyField(m_ElasticOffectRate);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.PropertyField(m_IsInertia);
        if (m_IsInertia.boolValue) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_DecelerationRate);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.PropertyField(m_IsStopping);
        if (m_IsStopping.boolValue) {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_MinVelocityToStopping);
            EditorGUILayout.PropertyField(m_StopingRate);
            EditorGUI.indentLevel--;
        }

        GUILayout.Space(20);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("AddStartItem", GUILayout.Height(40))) {
            GameObject itemGo = new GameObject("ScrollItem(Start)");
            itemGo.transform.SetParent(circulateScroll.transform, false);
            CirculateScrollItem scrollItem = itemGo.AddComponent<CirculateScrollItem>();
            circulateScroll.ScrollStartItem = scrollItem;
            Selection.activeGameObject = itemGo;
        }
        if (GUILayout.Button("AddEndItem", GUILayout.Height(40))) {
            GameObject itemGo = new GameObject("ScrollItem(End)");
            itemGo.transform.SetParent(circulateScroll.transform, false);
            CirculateScrollItem scrollItem = itemGo.AddComponent<CirculateScrollItem>();
            circulateScroll.ScrollEndItem = scrollItem;
            Selection.activeGameObject = itemGo;
        }
        if (GUILayout.Button("AddNormalItem", GUILayout.Height(40))) {
            int count = circulateScroll.ScrollItemCount;
            GameObject itemGo = new GameObject(string.Format("ScrollItem({0})",count + 1));
            itemGo.transform.SetParent(circulateScroll.transform, false);
            CirculateScrollItem scrollItem = itemGo.AddComponent<CirculateScrollItem>();
            circulateScroll.ScrollItems.Add(scrollItem);
            Selection.activeGameObject = itemGo;
        }
        EditorGUILayout.EndHorizontal();

        

        GUILayout.Space(20);
        //是否开启测试信息
        if (GUILayout.Button(m_DebugTtile,GUILayout.Height(40)))
            m_IsDebug = !m_IsDebug;        
        //测试信息
        if (m_IsDebug) {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("是否移动中", circulateScroll.isMoving.ToString());
            EditorGUILayout.LabelField("是否拖拽", circulateScroll.isDraging.ToString());
            EditorGUILayout.LabelField("是否自动移动", circulateScroll.isAutoMove.ToString());

            EditorGUILayout.LabelField("鼠标开始移动位置", circulateScroll.CursorStartDragPos.ToString());
            EditorGUILayout.LabelField("鼠标拖拽移动距离", circulateScroll.CursorDragingPos.ToString());

            EditorGUILayout.LabelField("拖拽速率", circulateScroll.DragVelocity.ToString());
            EditorGUILayout.LabelField("拖拽方向", circulateScroll.DragDirection.ToString());
            EditorGUILayout.LabelField("移动方向", circulateScroll.MoveDirection.ToString());
            EditorGUILayout.LabelField("拖拽偏移量", circulateScroll.DragOffect.ToString());
            EditorGUILayout.LabelField("百分比", circulateScroll.DragPercentage.ToString());

            EditorGUILayout.LabelField("拖拽开始距离", circulateScroll.DragStartDistance.ToString());
            EditorGUILayout.LabelField("当前拖拽距离", circulateScroll.DragCurrentDistance.ToString());
            EditorGUILayout.LabelField("上一帧拖拽距离", circulateScroll.DragLastDistance.ToString());
            EditorGUILayout.LabelField("最大拖拽距离", circulateScroll.DragMaxDistance.ToString());

            EditorGUILayout.LabelField("数据数量", circulateScroll.DataCount.ToString());
            EditorGUILayout.LabelField("移动格数", circulateScroll.MoveCount.ToString());

            GUILayout.Space(20);
            if (GUILayout.Button("RefreshImmediately")) {
                circulateScroll.RefreshImmediately();
            }
            EditorGUILayout.BeginHorizontal();
            m_DebugItemRadius = EditorGUILayout.FloatField("Radius",m_DebugItemRadius);
            if (GUILayout.Button("AddRefreshListener")) {
                circulateScroll.AddRefreshListener((item, index) => {
                    item.GizmosRadius = m_DebugItemRadius * (index + 1) + 0.1f;
                });
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("TargetIndex", GUILayout.Width(80));
            m_DebugAutoMoveTargetIndex = EditorGUILayout.IntField(m_DebugAutoMoveTargetIndex);
            EditorGUILayout.LabelField("Velocity", GUILayout.Width(60));
            m_DebugAutoMoveVelocity = EditorGUILayout.FloatField(m_DebugAutoMoveVelocity);
            if (GUILayout.Button("AutoMoveToIndex")) {
                circulateScroll.MoveToIndex(m_DebugAutoMoveTargetIndex, m_DebugAutoMoveVelocity);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("MoveToLast")) {
                circulateScroll.MoveToLast(m_DebugAutoMoveVelocity);
            }
            if (GUILayout.Button("MoveToNext")) {
                circulateScroll.MoveToNext(m_DebugAutoMoveVelocity);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            m_DebugTestCanDrag = EditorGUILayout.Toggle("CanDrag",m_DebugTestCanDrag); 
            if (GUILayout.Button("AddCanDragFunc")) {
                circulateScroll.AddCanDragListener(()=> {
                    return m_DebugTestCanDrag;
                });
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            m_DebugDataCount = EditorGUILayout.IntField("DataCount", m_DebugDataCount);
            if (GUILayout.Button("AddGetDataCountFunc")) {
                circulateScroll.AddGetDataCountListener(() => {
                    return m_DebugDataCount;
                });
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(20);
            EditorUtility.SetDirty(target);
        }


        serializedObject.ApplyModifiedProperties();
    }


    private SerializedProperty m_DragUnit;
    private SerializedProperty m_DragType;
    private SerializedProperty m_VerticalRate;
    private SerializedProperty m_HorizontalRate;
    private SerializedProperty m_DragMovement;
    private SerializedProperty m_MoveType;

    private SerializedProperty m_IsInertia;
    private SerializedProperty m_IsStopping;
    private SerializedProperty m_DecelerationRate;
    private SerializedProperty m_MinVelocityToStopping;
    private SerializedProperty m_StopingRate;
    private SerializedProperty m_ElasticOffectRate;
    private SerializedProperty m_ElasticVelocity;

    private bool m_IsDebug = false;
    private float m_DebugItemRadius = 0.5f;
    private float m_DebugAutoMoveVelocity = 0.1f;
    private int m_DebugAutoMoveTargetIndex;
    private int m_DebugDataCount = 10;
    private bool m_DebugTestCanDrag = false;
    private string m_DebugTtile {
        get {
            return m_IsDebug ? "CloseDebug" : "OpenDebug";
        }
    }

    private void OnEnable() {
        m_DragUnit = serializedObject.FindProperty("DragUnit");
        m_DragType = serializedObject.FindProperty("DragType");
        m_VerticalRate = serializedObject.FindProperty("VerticalRate");
        m_HorizontalRate = serializedObject.FindProperty("HorizontalRate");
        m_DragMovement = serializedObject.FindProperty("DragMovement");
        m_MoveType = serializedObject.FindProperty("MoveType");

        m_IsInertia = serializedObject.FindProperty("Inertia");
        m_IsStopping = serializedObject.FindProperty("Stopping");
        m_DecelerationRate = serializedObject.FindProperty("DecelerationRate");
        m_MinVelocityToStopping = serializedObject.FindProperty("MinVelocityToStopping");
        m_StopingRate = serializedObject.FindProperty("StopingRate");
        m_ElasticOffectRate = serializedObject.FindProperty("ElasticOffectRate");
        m_ElasticVelocity = serializedObject.FindProperty("ElasticVelocity");
    }
}

[CustomEditor(typeof(CirculateScrollItem))]
public class CirculateScrollItemEidotr: Editor {

    public override void OnInspectorGUI() {

        var item = target as CirculateScrollItem;
        base.OnInspectorGUI();

        EditorGUILayout.LabelField("初始列表索引", item.OriginalScrollIndex.ToString());
        EditorGUILayout.LabelField("当前列表索引", item.CurrentScrollIndex.ToString());
        EditorGUILayout.LabelField("目标列表索引", item.TargetScrollIndex.ToString());

        EditorGUILayout.LabelField("当前数据索引", item.DataIndex.ToString());

        EditorUtility.SetDirty(target);
    }
}


