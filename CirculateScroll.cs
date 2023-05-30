/******************************************************
 * FlieName:旋转列表
 * Auth:    Gasol.X
 * Date:    2021.4.23 11:42
 * Version: V1.0
 ******************************************************/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
[RequireComponent(typeof(RectTransform))]
public class CirculateScroll : MonoBehaviour, IInitializePotentialDragHandler, IDragHandler, IBeginDragHandler, IEndDragHandler {
    /// <summary>
    /// 拖拽移动类型
    /// </summary>
    public enum DragMovementType {
        Foward = 1, //前进
        Back = -1,  //后退
    }
    /// <summary>
    /// 拖拽方向类型
    /// </summary>
    public enum DragDirectionType {
        Vertical,               //垂直拖拽
        Horizontal,             //水平拖拽
        VertucalAndHorizontal,  //同时支持水平和垂直 需要调整垂直和水平的占比
    }
    /// <summary>
    /// 转转类型
    /// </summary>
    public enum ScrollMoveType {
        /// <summary>
        /// 限制区间的循环
        /// </summary>
        Clamped,   
        /// <summary>
        /// 有弹性的限制区间循环
        /// </summary>
        Elastic,    
        /// <summary>
        /// 头尾相连接的循环
        /// </summary>
        Loop,       
        /// <summary>
        /// 固定循环
        /// </summary>
        Fixed,
    }

    #region 面板属性
    //拖拽一格最小单位距离
    [HideInInspector]
    public int DragUnit;
    //垂直拖拽开启
    [HideInInspector]
    public DragDirectionType DragType = DragDirectionType.Horizontal;
    //垂直拖拽比率
    [Range(0, 1)]
    [HideInInspector]
    public float VerticalRate = 1;
    //水平拖拽比率
    [Range(0, 1)]
    [HideInInspector]
    public float HorizontalRate = 1;
    //物体拖拽方向
    [HideInInspector]
    public DragMovementType DragMovement = DragMovementType.Back;
    //拖拽移动类型
    [HideInInspector]
    public ScrollMoveType MoveType = ScrollMoveType.Clamped;
    //惯性
    [HideInInspector]
    public bool Inertia = true;
    //需要停靠
    [HideInInspector]
    public bool Stopping = true;
    //惯性减速率
    [HideInInspector]
    public float DecelerationRate = 0.135f;
    //最小停靠速率
    [HideInInspector]
    public float MinVelocityToStopping = 300;
    //前后停靠比率
    [HideInInspector]
    [Range(0, 1)]
    public float StopingRate = 0.5f;
    //弹性最大距离比率
    [HideInInspector]
    [Range(0, 1)]
    public float ElasticOffectRate = 1;
    [HideInInspector]
    //弹性速率
    public float ElasticVelocity = 0.5f;
    //这部分只在DEBUG下显示
    public List<CirculateScrollItem> ScrollItems = new List<CirculateScrollItem>();
    public CirculateScrollItem ScrollStartItem;
    public CirculateScrollItem ScrollEndItem;
    #endregion

    #region 只读属性

    //拖拽区域
    public RectTransform DragContent {
        get {
            if (m_DragContent == null) {
                m_DragContent = GetComponent<RectTransform>();
            }
            return m_DragContent;
        }
        set {
            m_DragContent = value;
        }
    }
    //移动距离百分比
    public float DragPercentage {
        get {
            return Mathf.Abs((m_DragCurrentDistance % DragUnit) / DragUnit);
        }
    }
    //是否拖拽中
    public bool isDraging {
        get => m_IsDraging;
    }
    //是否在自动移动中
    public bool isAutoMove {
        get => m_IsAutoMoving;
    }
    //是否在移动中
    public bool isMoving {
        get {
            return m_DragVelocity != 0;
        }
    }
    //信息总数
    public int DataCount {
        get {
            if (m_GetDataCountFunc != null) {
                m_DataCount = m_GetDataCountFunc();
            }
            return m_DataCount;
        }
    }
    //本次拖拽的方向  1 代表正向  -1 代表反向
    public int DragDirection {
        get {
            if (DragStartDistance > DragCurrentDistance) {
                return -1;
            }
            else if (DragStartDistance < DragCurrentDistance) {
                return 1;
            }
            else
                return 0;
        }
    }
    //移动方向 1 代表正向  -1 代表反向  0代表没有移动
    public int MoveDirection {
        get {
            return m_MoveDirection;
        }
    }
    public int DistanceSign {
        get {
            return (int)Mathf.Sign(DragCurrentDistance);
        }
    }
    //拖拽速率
    public float DragVelocity {
        get => m_DragVelocity;
    }
    //当前鼠标拖拽中位置
    public float CursorDragingPos {
        get => m_CursorDragingPos.x;
    }
    //鼠标开始拖拽位置
    public float CursorStartDragPos {
        get => m_CursorStartDragPos.x;
    }
    //开始拖拽距离
    public float DragStartDistance {
        get => m_DragStartDistance;
    }
    //当前拖拽距离
    public float DragCurrentDistance {
        get => m_DragCurrentDistance;
        protected set {
            if (MoveType == ScrollMoveType.Clamped) {
                m_DragCurrentDistance = Mathf.Clamp(value, 0, DragMaxDistance);
            }
            else if (MoveType == ScrollMoveType.Elastic) {
                m_DragCurrentDistance = Mathf.Clamp(value, -DragUnit * ElasticOffectRate, DragMaxDistance + DragUnit * ElasticOffectRate);
            }
            else if (MoveType == ScrollMoveType.Loop || MoveType == ScrollMoveType.Fixed) {
                m_DragCurrentDistance = Mathf.Clamp(value,int.MinValue,int.MaxValue);
            }
        }
    }
    //上一帧拖拽距离
    public float DragLastDistance {
        get => m_DragLastDistance;
    }
    //拖拽最大距离
    public float DragMaxDistance {
        get {
            //要添加开头和结尾两个节点
            int dataCount = DataCount + 2;
            //如果旋转格子比刷新的数据数量还多 表示一格都不能旋转
            return ScrollItemCount > dataCount ? 0 : (dataCount - ScrollItems.Count) * DragUnit;
        }
    }
    //移动数量
    public int MoveCount {
        get {
            int newMoveCount = (int)(m_DragCurrentDistance / DragUnit);
            bool isNeedRefresh = m_MoveCount != newMoveCount;
            m_MoveCount = newMoveCount;
            //每次因为拖拽导致移动数量变换的时候 就立即刷新一次
            if (isNeedRefresh) {
                RefreshImmediately();
            }
            return m_MoveCount;
        }
    }
    //滑动物体数量
    public int ScrollItemCount {
        get => ScrollItems.Count;
    }
    //拖拽偏移量
    public float DragOffect {
        get => m_DragOffect;
    }

    #endregion

    #region 调用接口

    /// <summary>
    /// 初始化 修改物体位置信息之后 需要调用一次重新计算
    /// </summary>
    public void Init() {
        InitItemInfo();
        //初始化完立即刷新一次
        RefreshImmediately();
    }

    /// <summary>
    /// 移动到目标距离
    /// </summary>
    /// <param name="_distance">目标距离</param>
    /// <param name="_velocity">移动速率</param>
    public void MoveToDistance(float _distance,float _velocity = 0) {
        //弹性模式下不能越界
        m_MoveTargetDistance = MoveType == ScrollMoveType.Elastic ? Mathf.Clamp(_distance, 0, DragMaxDistance) : _distance;
        //已经到达位置 直接返回
        if (m_DragCurrentDistance == m_MoveTargetDistance) {
            RefreshImmediately();
            return;
        }
        //如果速率为0  则立即刷新
        if (_velocity == 0) {
            m_DragCurrentDistance = m_MoveTargetDistance;
            RefreshImmediately();
        }
        //开启自动移动 让他自己动
        else {
            m_AutoMoveVeloctiy = _velocity;
            m_IsAutoMoving = true;
        }
    }
    /// <summary>
    /// 移动到目标索引值 速率为0则直接刷新到目标位置 没有移动过程
    /// </summary>
    /// <param name="_index">目标索引</param>
    /// <param name="_velocity">移动速率</param>
    public void MoveToIndex(int _index, float _velocity = 0) {
        MoveToDistance(_index * DragUnit, _velocity);
    }
    /// <summary>
    /// 移动到上一个
    /// </summary>
    /// <param name="_velocity">速率</param>

    public void MoveToLast(float _velocity = 0) {
        int moveCount = DistanceSign > 0 ? MoveDirection > 0 ? 0 : 1 : MoveDirection < 0 ? 2 : 1;
        MoveToIndex(MoveCount - moveCount, _velocity);
    }
    /// <summary>
    /// 移动到下一个
    /// </summary>
    /// <param name="_velocity">速率</param>
    public void MoveToNext(float _velocity = 0) {
        int moveCount = DistanceSign > 0 ? MoveDirection > 0 ? 2 : 1 : MoveDirection < 0 ? 0 : 1;
        MoveToIndex(MoveCount + moveCount, _velocity);
    }

  

    /// <summary>
    /// 立即刷新所有物体
    /// </summary>
    public void RefreshImmediately() {
        foreach (var item in ScrollItems) {
            m_OnRefreshItem?.Invoke(item, item.DataIndex);
        }
    }
    /// <summary>
    /// 添加刷新数量的方法
    /// </summary>
    /// <param name="_dateCount"></param>
    public void AddGetDataCountListener(Func<int> _resetDataCount) {
        m_GetDataCountFunc = _resetDataCount;
    }
    /// <summary>
    /// 添加刷新方法
    /// </summary>
    /// <param name="_onRefresh"></param>
    public void AddRefreshListener(Action<CirculateScrollItem, int> _onRefresh) {
        m_OnRefreshItem = _onRefresh;
    }
    /// <summary>
    /// 添加能否拖拽监听
    /// </summary>
    /// <param name="_canDrag"></param>
    public void AddCanDragListener(Func<bool> _canDrag) {
        m_CanDragFunc = _canDrag;
    }
    /// <summary>
    /// 添加一个移动结束监听
    /// </summary>
    /// <param name="_onComplete"></param>
    public void AddOnMoveCompleteListener(Action _onComplete) {
        m_OnMoveComplete = _onComplete;
    }
    /// <summary>
    /// 通过在旋转列表中的索引 获取旋转物体
    /// </summary>
    /// <param name="_index"></param>
    /// <returns></returns>
    public CirculateScrollItem GetScrollItemByIndex(int _index) {
        foreach (var item in ScrollItems) {
            if (item.CurrentScrollIndex == _index) {
                return item;
            }
        }
        return null;
    }

    //获取物体信息
    public CirulateScrollItemInfo GetScrollItemInfoByIndex(int _index) {
        //通过索引在列表中找到对应位置                    1 => 2 => 3 => 4 => 5 => 6
        //这个方法将传入的索引计算成列表中正确的位置      0 => 1 => 2 => 0 => 1 => 2
        //这个计算方法在拖拽物体类中实现
        if (_index < 0 || _index >= m_ScrollItemInfos.Count) {
            return new CirulateScrollItemInfo();
        }
        return m_ScrollItemInfos[_index];
    }
    #endregion

    #region 内部实现

    //拖拽区域
    [SerializeField]
    private RectTransform m_DragContent;
    //刷新物体的方法
    private Action<CirculateScrollItem, int> m_OnRefreshItem;
    //能否拖拽的方法
    private Func<bool> m_CanDragFunc;
    //重置数据数量方法
    private Func<int> m_GetDataCountFunc;
    //当移动停止时触发
    private Action m_OnMoveComplete;
    //物体信息列表
    private List<CirulateScrollItemInfo> m_ScrollItemInfos = new List<CirulateScrollItemInfo>();
    //是否拖拽
    private bool m_IsDraging;
    //是否垂直拖拽
    private bool m_IsHorizontalDrag;
    //开始一次移动
    private bool m_IsStartMove;
    //移动数量
    private int m_MoveCount;
    //数据数量
    private int m_DataCount;
    //移动方向
    private int m_MoveDirection;
    //拖拽开始时的距离
    private float m_DragStartDistance;
    //拖拽当前距离
    private float m_DragCurrentDistance;
    //拖拽上一帧的距离
    private float m_DragLastDistance;
    //拖拽速率
    private float m_DragVelocity;
    //拖拽偏移量
    private float m_DragOffect;
    //移动到目标点的距离(自动移动)
    private float m_MoveTargetDistance;
    //移动到目标点速率(自动移动)
    private float m_AutoMoveVeloctiy;
    //是否自动移动
    private bool m_IsAutoMoving;
    //初始化拖拽位置
    private Vector2 m_CursorInitDragPos;
    //触发拖拽位置
    private Vector2 m_CursorStartDragPos;
    //一帧内移动的距离
    private Vector2 m_CursorDragingPos;

    //初始化旋转组件 并生成位置信息
    private void Start() {
        Init();
    }
    //获取所有拖拽点的位置信息
    private void InitItemInfo() {
        m_ScrollItemInfos.Clear();
        //开始位置添加到列表开头
        ScrollItems.Insert(0,ScrollStartItem);
        //结束位置添加到列表末尾
        ScrollItems.Add(ScrollEndItem);
        //添加中间用于显示的物体
        for (int i = 0; i < ScrollItems.Count; i++) {
            var item = ScrollItems[i];
            item.Init(this, i);
            //初始化位置信息
            CirulateScrollItemInfo itemInfo = new CirulateScrollItemInfo {
                Pos = item.transform.position,
                LocalPos = item.transform.localPosition,
                Scale = item.transform.localScale
            };
            m_ScrollItemInfos.Add(itemInfo);
        }    
    }

    //UI拖拽相关
    public void OnInitializePotentialDrag(PointerEventData eventData) {
        m_DragVelocity = 0;
        //初始化的时候传入一个初始位置
        m_CursorInitDragPos = Vector2.zero;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(DragContent, eventData.position, eventData.pressEventCamera, out m_CursorInitDragPos);
    }
    public void OnBeginDrag(PointerEventData eventData) {
        if (m_CanDragFunc != null && !m_CanDragFunc.Invoke()) {
            return;
        }
        m_CursorStartDragPos = Vector2.zero;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(DragContent, eventData.position, eventData.pressEventCamera, out m_CursorStartDragPos);
        m_DragStartDistance = DragCurrentDistance;
        //如果两个都处于开启状态 需要根据第一帧移动的距离大小来判断 拖拽是水平还是垂直
        if (DragType == DragDirectionType.VertucalAndHorizontal) {
            var dargInitDistance = m_CursorInitDragPos - m_CursorStartDragPos;
            m_IsHorizontalDrag = Mathf.Abs(dargInitDistance.x * VerticalRate) > Mathf.Abs(dargInitDistance.y * HorizontalRate);
        }
        else {
            m_IsHorizontalDrag = DragType == DragDirectionType.Horizontal;
        }
        m_IsDraging = true;
        m_IsStartMove = true;
    }
    public void OnDrag(PointerEventData eventData) {
        if (m_CanDragFunc != null && !m_CanDragFunc.Invoke()) {
            return;
        }
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(DragContent, eventData.position, eventData.pressEventCamera, out Vector2 localCursor)) {
            //算出拖拽的距离
            m_CursorDragingPos = localCursor - m_CursorStartDragPos;
            //这里判断是垂直拖拽还是水平拖拽
            var position = m_IsHorizontalDrag ? m_DragStartDistance + m_CursorDragingPos.x * (int)DragMovement : m_DragStartDistance + m_CursorDragingPos.y * (int)DragMovement;
            //根据当前的移动模式来修改当前拖拽距离
            if (MoveType == ScrollMoveType.Clamped || MoveType == ScrollMoveType.Loop || MoveType == ScrollMoveType.Fixed) {
                DragCurrentDistance = position;
            }
            //弹性模式下需要而外计算弹性距离
            else if (MoveType == ScrollMoveType.Elastic) {
                //先计算超出的偏移量
                m_DragOffect = CalculateOffset(position);
                //这里根据超出部分的拖拽偏移量 来计算出弹性距离
                var elasticDistance = m_DragOffect == 0 ? 0 : m_DragOffect - m_DragOffect * 0.95f;
                //拖拽距离等于位置加上弹性距离
                DragCurrentDistance = m_DragOffect == 0 ? position              //没有额外距离直接赋值当前距离
                                        : m_DragOffect < 0                       
                                        ? 0 + elasticDistance                   //最小值弹性距离
                                        : DragMaxDistance + elasticDistance ;   //最大值弹性距离
            }

        }
    }
    public void OnEndDrag(PointerEventData eventData) {
        if (m_CanDragFunc != null && !m_CanDragFunc.Invoke()) {
            return;
        }
        m_IsDraging = false;
    }

    //更新
    private void LateUpdate() {
        //模仿ScrollRect计算速率的方法来做滑动

        //每帧执行时间
        float deltaTime = Time.unscaledDeltaTime;
        //处理自动移动
        if (m_IsAutoMoving) {
            DragCurrentDistance = Mathf.Lerp(m_DragCurrentDistance, m_MoveTargetDistance, m_AutoMoveVeloctiy);
            //到达终点 或则拖拽会终止自动移动
            if (isDraging) {
                m_IsAutoMoving = false;
            }
            float distance = Math.Abs(m_MoveTargetDistance - m_DragCurrentDistance);
            if (distance < 10f) {
                DragCurrentDistance = m_MoveTargetDistance;
                //这里把速率清空了
                m_DragVelocity = 0;
                m_IsAutoMoving = false;
            }
        }
        //如果是在拖拽的过程中 每帧计算速率
        if (isDraging) {
            float newVelocity = (DragCurrentDistance - m_DragLastDistance) / deltaTime;
            //让当前速率以10的速度接近新的速率
            m_DragVelocity = Mathf.Lerp(m_DragVelocity, newVelocity, deltaTime * 10);
        }
        //如果没有拖拽了  开始惯性停靠
        if (!isDraging && !m_IsAutoMoving) {
            //递减速率
            m_DragVelocity *= Mathf.Pow(DecelerationRate, deltaTime);
            //判断距离是否到达最大和最小值 如果不需要继续移动就直接开始停靠逻辑
            bool needMove = MoveType == ScrollMoveType.Loop || MoveType == ScrollMoveType.Fixed ? true : DragCurrentDistance > 0 && DragCurrentDistance < DragMaxDistance;
            //如果有弹性要优先单独处理弹性 [这里偷懒 简单处理]
            if (DragOffect != 0) {
                //处理回弹
                DoElastic();
            }
            //如果有惯性 需要向速率的方向滑动一段距离 并且准确的停靠在对应的位置上面
            else if (Inertia && needMove) {
                //如果需要停靠并且 速率递减到停靠速率的话 就开始执行停靠的逻辑
                if (Mathf.Abs(m_DragVelocity) <= MinVelocityToStopping) {
                    //todo 根据当前的移动比率 来判断需要向哪个节点停靠  这里要注意 停靠的过程中也要实时更新移动距离 所有的数据刷新判断都是根据
                    //移动距离和移动单位距离 的比值来计算的  确保移动距离 实时保证正确就能保证 数据不会被其他的因素影响
                    DoStopping(deltaTime);
                }
                //否则 用速率 继续驱动移动
                else {
                    float distance = DragCurrentDistance += m_DragVelocity * deltaTime;
                    //在区间内需要固定 惯性不能超出最大最小值,如果惯性产生弹性操作感觉会很怪......
                    if (MoveType == ScrollMoveType.Clamped || MoveType == ScrollMoveType.Elastic) {
                        DragCurrentDistance = Mathf.Clamp(distance, 0, DragMaxDistance);
                    }
                    else {
                        DragCurrentDistance = distance;
                    }
                }
            }
            //没有惯性并且 开启停靠逻辑就直接开始停靠
            else{
                DoStopping(deltaTime);
            }
        }

        //记录上一帧的拖拽距离 用于计算拖拽速率
        m_MoveDirection = DragCurrentDistance == m_DragLastDistance
                            ? 0 : (int)Mathf.Sign(DragCurrentDistance - m_DragLastDistance);
        m_DragLastDistance = DragCurrentDistance;

        //移动所有物体
        MoveItems();      
    }
    //弹性逻辑
    private void DoElastic() {
        //如果是因为弹性超过最小值 就往0靠
        if (DragOffect < 0) {
            if (DragCurrentDistance >= -0.01f) {
                DragCurrentDistance = 0;
                m_DragOffect = 0;
            }
            else {
                DragCurrentDistance = Mathf.Lerp(DragCurrentDistance, 0, ElasticVelocity);
            }
        }
        //最大值同理
        else {
            if (DragCurrentDistance <= DragMaxDistance + 0.01f) {
                DragCurrentDistance = DragMaxDistance;
                m_DragOffect = 0;
            }
            else {
                DragCurrentDistance = Mathf.Lerp(DragCurrentDistance, DragMaxDistance, ElasticVelocity);
            }
        }
    }
    //停靠逻辑
    private void DoStopping(float _deltaTime) {
        if (!Stopping) {
            return;
        }
        //这里感觉写的有点问题 后期可以优化
        //如果不是循环模式这里直接不计算距离方向
        int currentDistanceDirection = MoveType == ScrollMoveType.Clamped || MoveType == ScrollMoveType.Elastic ? 1 : DistanceSign;
        if (DragPercentage > 0.01 && DragPercentage < 0.99) {
            //按照最小速率接近目标位置  这里根据前后停靠比率 以及本次拖拽的方向来判断 是向前停靠好事向后停靠
            m_DragVelocity = DragDirection > 0
                 //相对其实位置是正向拖拽
                 ? DragPercentage > StopingRate ? MinVelocityToStopping * currentDistanceDirection : -MinVelocityToStopping 
                 //相对起始位置是反向拖拽 
                 : DragPercentage > 1 - StopingRate ? MinVelocityToStopping : -MinVelocityToStopping * currentDistanceDirection;
            //停靠时判断是否有额外偏移
            DragCurrentDistance += m_DragVelocity * DistanceSign * _deltaTime;
                                                   
        }
        //百分比到达目标位置附近时 直接将目标位置赋值 完成停靠
        else {
            DragCurrentDistance = DragDirection > 0
                ? DragPercentage > StopingRate ? DragUnit * (MoveCount + 1 * currentDistanceDirection) : DragUnit * MoveCount
                : DragPercentage > 1 - StopingRate ? DragUnit * (MoveCount + 1 * currentDistanceDirection) : DragUnit * MoveCount;
            m_DragVelocity = 0;
            //如果有开始移动 并且移动已经停止  那么执行一次移动完成
            if (m_IsStartMove && !isMoving) {
                m_OnMoveComplete?.Invoke();
                m_IsStartMove = false;
            }
        }
    }
    //计算拖拽超出最大或最小值的偏移量
    private float CalculateOffset(float _distance) {
        float min = 0;
        float max = DragMaxDistance;

        if (_distance < min) {
            return _distance;
        }
        else if (_distance > max) {
            return _distance - max;
        }
        return 0;
    }
    //移动所有的拖拽物体
    private void MoveItems() {
        foreach (var item in ScrollItems) {
            item.Move();
        }
    }


    #endregion

    #region 绘制可视化辅助
    //绘制可视化辅助
    private void OnDrawGizmos() {
        for (int i = 0; i < m_ScrollItemInfos.Count; i++) {
            var itemInfo = m_ScrollItemInfos[i];
            Gizmos.color = Color.green;
            if (i == 0) {
                Gizmos.color = Color.yellow;
            }
            else if (i == m_ScrollItemInfos.Count - 1) {
                Gizmos.color = Color.blue;
            }
            Gizmos.DrawSphere(itemInfo.Pos, 0.2f);
        }
    }
    #endregion

}
