using UnityEngine;
using System;

/// <summary>
/// 家具格子类型
/// </summary>
public enum GridType
{
    Floor = 0,     // 黄色格子 - 地板
    Table = 1,     // 灰色格子 - 桌面
    Wall = 2,      // 蓝色格子 - 墙面
    Outdoor = 3,   // 绿色格子 - 户外
    Decoration = 4,// 紫色格子 - 装饰
    Forbidden = 5  // 红色格子 - 禁止
}

/// <summary>
/// 家具数据类
/// </summary>
[System.Serializable]
public class FurnitureData
{
    public string id;                      // 家具ID
    public string name;                    // 家具名称
    public int price;                      // 家具价格
    public Sprite icon;                    // 家具图标
    public string description;             // 家具描述
    public GameObject prefab;              // 家具预制体

    [Header("格子需求")]
    public GridRequirement[] gridRequirements; // 所需格子类型

    [Header("尺寸")]
    public int width = 1;                  // 宽度（格数）
    public int height = 1;                 // 高度（格数）

    [Header("解锁条件")]
    public bool isLocked = false;          // 是否已解锁
    public int unlockLevel = 1;            // 解锁等级
    public int requiredRoomType = 0;       // 所需房间类型（0:客厅,1:书房,2:卧室,3:阳台）

    [Header("特殊属性")]
    public bool providesNewGrids = false;  // 是否提供新的格子
    public GridType providedGridType;      // 提供的格子类型
    public int providedGridCount = 0;      // 提供的格子数量
}

/// <summary>
/// 格子需求
/// </summary>
[System.Serializable]
public class GridRequirement
{
    public GridType requiredType;          // 需要的格子类型
    public int count;                      // 需要的数量
}

/// <summary>
/// 家具实例类
/// </summary>
[System.Serializable]
public class FurnitureInstance
{
    public string furnitureId;             // 家具ID
    public Vector2Int gridPosition;        // 所在格子位置
    public int rotation = 0;               // 旋转角度（0,1,2,3对应0°,90°,180°,270°）
    public bool isPlaced = false;          // 是否已放置
}