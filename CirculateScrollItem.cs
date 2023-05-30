/******************************************************
 * FlieName:旋转列表物体
 * Auth:    Gasol.X
 * Date:    2021.4.23 11:42
 * Version: V1.0
 ******************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class CirculateScrollItem : MonoBehaviour
{

    #region 绘制可视化辅助

    public Color GizmosColor = Color.red;
    public float GizmosRadius = 1f;

    private void OnDrawGizmos() {
        Gizmos.color = GizmosColor;
        Gizmos.DrawSphere(transform.position, GizmosRadius);
    }

    #endregion

    [HideInInspector]
    //初始列表索引
    public int OriginalScrollIndex;
    //当前列表索引
    public int CurrentScrollIndex {
        get {
            if (!isInit)
                return 0;
            var index = DataIndexToScrollIndex(OriginalScrollIndex - m_Scroll.MoveCount);
            return index;
        }
    }
    //目标列表索引
    public int TargetScrollIndex {
        get {
            if (!isInit)
                return 0;
            var targetIndex = CurrentScrollIndex - 1 * (int)Mathf.Sign(m_Scroll.DragCurrentDistance);
            return DataIndexToScrollIndex(targetIndex);
        }
    } 
    //数据索引
    public int DataIndex {
        get {
            if (!isInit)
                return 0;
            if (m_Scroll.MoveType == CirculateScroll.ScrollMoveType.Fixed) {
                return OriginalScrollIndex;
            }
            else if (m_Scroll.MoveType == CirculateScroll.ScrollMoveType.Loop) {
                return (CurrentScrollIndex + m_Scroll.MoveCount) % m_Scroll.DataCount;
            }
            else {
                return CurrentScrollIndex + m_Scroll.MoveCount;
            }
        }
    }
    //是否初始化
    public bool isInit {
        get {
            return m_Scroll != null;
        }
    }
    //初始化
    public void Init(CirculateScroll _scroll,int _index) {
        m_Scroll = _scroll;
        OriginalScrollIndex = _index;
    }
    //拖拽物体 从当前位置出发  向目标位置移动  
    public void Move() {
        if (!isInit)
            return;
        //获取移动百分比
        var percentage = m_Scroll.DragPercentage;
        //获取当前所在的位置信息 以及目标点的位置信息
        var currentInfo = m_Scroll.GetScrollItemInfoByIndex(CurrentScrollIndex);
        var targetInfo = m_Scroll.GetScrollItemInfoByIndex(TargetScrollIndex);

        //移动位置
        var currentPos = currentInfo.Pos;
        var targetPos = targetInfo.Pos;
    
        transform.position = Vector3.Lerp(currentPos, targetPos, percentage);

        //缩放大小
        var currentScale = currentInfo.Scale;
        var targetScale = targetInfo.Scale;

        transform.localScale = Vector3.Lerp(currentScale,targetScale,percentage);
    }



    private CirculateScroll m_Scroll;
    private int DataIndexToScrollIndex(int _dataIndex) {
        var index = _dataIndex % m_Scroll.ScrollItemCount;
        index = index >= 0 ? index : m_Scroll.ScrollItemCount + index;
        return index;
    }
}


//拖拽物体信息
[Serializable]
public struct CirulateScrollItemInfo {
    public Vector3 Pos;
    public Vector3 LocalPos;
    public Vector3 Scale;
}