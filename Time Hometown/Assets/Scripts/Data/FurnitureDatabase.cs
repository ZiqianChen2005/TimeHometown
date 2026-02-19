using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class FurnitureDatabase : MonoBehaviour
{
    public static FurnitureDatabase Instance { get; private set; }

    [Header("JSON配置")]
    [SerializeField] private TextAsset furnitureJsonFile;     // 家具JSON文件
    [SerializeField] private string jsonFileName = "furniture_data.json"; // JSON文件名（用于持久化）

    [Header("家具数据")]
    [SerializeField] private List<FurnitureData> allFurniture = new List<FurnitureData>();

    // 按房间类型分组的家具
    private Dictionary<int, List<FurnitureData>> furnitureByRoom = new Dictionary<int, List<FurnitureData>>();

    // 按ID索引的家具
    private Dictionary<string, FurnitureData> furnitureById = new Dictionary<string, FurnitureData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadFurnitureData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 加载家具数据
    /// </summary>
    private void LoadFurnitureData()
    {
        allFurniture.Clear();
        furnitureByRoom.Clear();
        furnitureById.Clear();

        // 优先从Resources加载
        if (furnitureJsonFile != null)
        {
            LoadFromTextAsset(furnitureJsonFile);
        }
        else
        {
            // 尝试从持久化路径加载
            LoadFromPersistentPath();
        }

        // 构建索引
        BuildIndices();

        Debug.Log($"家具数据库加载完成，共 {allFurniture.Count} 件家具");
    }

    /// <summary>
    /// 从TextAsset加载
    /// </summary>
    private void LoadFromTextAsset(TextAsset jsonAsset)
    {
        try
        {
            FurnitureDataArray wrapper = JsonUtility.FromJson<FurnitureDataArray>(jsonAsset.text);
            if (wrapper != null && wrapper.furniture != null)
            {
                allFurniture.AddRange(wrapper.furniture);
                Debug.Log($"从TextAsset加载了 {wrapper.furniture.Length} 件家具");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"解析家具JSON失败: {e.Message}");
        }
    }

    /// <summary>
    /// 从持久化路径加载
    /// </summary>
    private void LoadFromPersistentPath()
    {
        string path = Path.Combine(Application.persistentDataPath, jsonFileName);

        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                FurnitureDataArray wrapper = JsonUtility.FromJson<FurnitureDataArray>(json);
                if (wrapper != null && wrapper.furniture != null)
                {
                    allFurniture.AddRange(wrapper.furniture);
                    Debug.Log($"从持久化路径加载了 {wrapper.furniture.Length} 件家具: {path}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"读取家具JSON文件失败: {e.Message}");
            }
        }
        else
        {
            // 如果文件不存在，加载默认数据
            LoadDefaultFurniture();
        }
    }

    /// <summary>
    /// 加载默认家具数据（用于测试）
    /// </summary>
    private void LoadDefaultFurniture()
    {
        Debug.Log("加载默认家具数据 - 时光家园全系列家具");

        #region 时光小镇 - 室内装饰

        // 默认解锁家具
        allFurniture.Add(CreateFurniture(
            "town_default_chair", "平平无奇的椅子", 0,
            "这把椅子从上到下每一处都很普通，即便是这把椅子的名字。",
            1, 1, 2, GridType.Floor, 1  // 客厅，地板
        ));

        allFurniture.Add(CreateFurniture(
            "town_default_table", "平平无奇的桌子", 0,
            "这张桌子从上到下每一处都很普通，放在哪里都很合适。",
            1, 1, 2, GridType.Floor, 1  // 客厅，地板
        ));

        allFurniture.Add(CreateFurniture(
            "town_oak_cabinet", "橡木橱柜", 20,
            "一个橡木柜子，双开门，空间很大，还散发着淡淡的木香。",
            2, 2, 2, GridType.Floor, 4  // 客厅，地板
        ));

        allFurniture.Add(CreateFurniture(
            "town_clock", "老座钟", 45,
            "实木钟身，黄铜指针，发出沉稳的“滴答”声。每逢整点，会奏响一段悠扬的报时曲，不疾不徐，是小镇时光流淌最忠实的记录者。",
            1, 2, 2, GridType.Wall, 2  // 客厅，墙面
        ));

        allFurniture.Add(CreateFurniture(
            "town_cotton_curtain", "印花棉布窗帘", 35,
            "素色的亚麻底布上，印着淡雅的小碎花或条纹图案。阳光透过时，会在室内地板上投下温柔的光影，随着时间推移缓缓移动。",
            2, 1, 3, GridType.Wall, 2  // 卧室，墙面
        ));

        allFurniture.Add(CreateFurniture(
            "town_tea_set", "陶瓷茶具套组", 20,
            "白底蓝纹或暖色调的手绘陶瓷，壶身圆润，茶杯精巧。用它泡上一壶红茶，伴随着袅袅热气，便是最安逸的午后时光。",
            1, 1, 2, GridType.Table, 1  // 客厅，桌面
        ));

        allFurniture.Add(CreateFurniture(
            "town_woven_basket", "编织篮", 40,
            "用柳条或藤条手工编织的篮子，可能装着新鲜的果蔬、毛线团，或是几本旧书。随意摆放，便充满了生活气息与手作的温度。",
            1, 1, 2, GridType.Floor, 1  // 客厅，地板
        ));

        allFurniture.Add(CreateFurniture(
            "town_vintage_mirror", "复古梳妆镜", 30,
            "带有可掀盖的木框镜子，内部贴着花纹衬布。镜面或许因年代久远而略有模糊，却恰恰映照出一种褪色照片般的温柔与怀旧感。",
            1, 2, 3, GridType.Table, 2  // 卧室，桌面
        ));

        allFurniture.Add(CreateFurniture(
            "town_key_plate", "铸铁钥匙盘", 20,
            "门边一个厚重的铸铁盘，浮雕着简单的葡萄藤或鸢尾花纹。回家后，将钥匙随手放入盘中那一声轻响，是每日安稳归家的仪式。",
            1, 1, 2, GridType.Table, 1  // 客厅，桌面
        ));

        allFurniture.Add(CreateFurniture(
            "town_memory_plates", "记忆回廊挂盘", 20,
            "墙上悬挂着一系列手绘陶瓷盘，每个盘子描绘着小镇不同历史时期的标志性场景：丰收节、老磨坊、初代火车进站……它们按时间顺序排列，组成一幅可视化的城镇编年史。",
            2, 1, 1, GridType.Wall, 2  // 书房，墙面
        ));

        allFurniture.Add(CreateFurniture(
            "town_breathing_pot", "会呼吸的陶罐", 20,
            "表面粗糙、带有细微孔隙的陶罐，用来储存谷物或干货。据说这些陶罐能在日夜温差间进行微弱的“呼吸”，使储存物的味道随着时间流逝变得更加醇厚柔和。",
            1, 1, 1, GridType.Floor, 1  // 书房，地板
        ));

        allFurniture.Add(CreateFurniture(
            "town_past_radio", "往昔之声收音机", 80,
            "一台老式木质外壳收音机。除了接收寻常频道，在雷雨天气或特定的静谧午夜，偶尔会收到模糊不清的、仿佛来自过去的广播片段——几十年前的新闻、老歌，或是某个家庭聚会的嘈杂背景音。",
            1, 1, 1, GridType.Table, 1  // 书房，桌面
        ));

        allFurniture.Add(CreateFurniture(
            "town_guardian_doll", "沉睡的守护神娃娃", 100,
            "由祖母缝制的布娃娃，穿着代代相传的旧衣服碎片缝成的衣裳。被孩子们相信拥有守护的力量，安静地坐在阁楼或窗台，身上积累着家庭的安宁记忆。",
            1, 1, 3, GridType.Table, 1  // 卧室，桌面
        ));

        #endregion

        #region 时光小镇 - 室外装饰

        allFurniture.Add(CreateFurniture(
            "town_rose_arch", "玫瑰花拱门", 150,
            "木制的拱门，爬满了盛放的蔷薇或玫瑰。随着四季更迭变换姿态，春天的新绿、夏日的繁花、秋日的果实，都是时光赠予的礼物。",
            2, 3, 0, GridType.Outdoor, 6  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "town_bird_bath", "陶瓷鸟浴盆", 120,
            "一个立在石柱上的浅口陶瓷盆，盛着清水，常有小鸟来此啜饮、梳洗。盆边或许还蹲着一只陶瓷小猫装饰，静静守护着这份生机。",
            1, 1, 0, GridType.Outdoor, 1  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "town_mailbox", "信箱与牛奶箱", 70,
            "挂在门边的铁皮信箱，漆皮或许有些斑驳。旁边可能还有一个复古的小木箱，让人想起每日清晨递送新鲜牛奶和报纸的往昔。",
            1, 1, 0, GridType.Outdoor, 1  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "town_street_lamp", "铸铁路灯", 55,
            "庭院小径旁的老式铸铁路灯，夜幕降临时自动亮起暖黄色的光。灯光不算明亮，却足以照亮回家的路，驱散夜晚的微寒与孤寂。",
            1, 2, 0, GridType.Outdoor, 2  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "town_wooden_swing", "老树下的木质秋千椅", 65,
            "挂在老树枝干下的双人秋千椅，随着微风轻轻摇晃。坐在上面看书、喝茶，或只是发呆，感受风的速度与时光的节奏合二为一。",
            2, 1, 0, GridType.Outdoor, 2  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "town_dandelion_path", "蒲公英石板小径", 90,
            "不甚规则的石板铺成蜿蜒小径，缝隙间钻出青草和顽强的蒲公英。不去刻意修剪，任由它们随季节枯荣，与自然共处。",
            3, 1, 0, GridType.Outdoor, 3  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "town_weather_vane", "四季风向标", 30,
            "铁艺风向标不仅是公鸡造型，其尾巴上挂着四片金属牌，分别镂空雕刻着春芽、夏花、秋果、冬枝的图案。风不仅指示方向，更让四季的象征在空中轻轻碰撞，叮咚作响。",
            1, 2, 0, GridType.Outdoor, 2  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "town_story_bench", "故事长椅", 120,
            "公园里一张看起来普通的长椅，但扶手上刻满了历代小镇居民留下的简短词句、名字缩写或小小图案。坐在上面，仿佛能感受到无数过往的休憩者留下的温度与片段人生。",
            2, 1, 0, GridType.Outdoor, 2  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "town_growing_bench", "共生长椅树", 150,
            "一棵大树，其树干天然生长成可容纳两三人并坐的弯曲形状，形成了天然的“长椅”。树木依旧生机勃勃，是自然与人工、生命与休憩完美共生的温柔奇迹。",
            2, 2, 0, GridType.Outdoor, 4  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "town_rusted_wonder", "锈蚀的奇迹", 220,
            "一件被遗弃在花园角落的旧农具（如铁犁、马车轮），任其自然锈蚀，覆上苔藓与攀爬植物。它不再具有实用功能，成为时光雕塑的一部分，见证着从功能到美学的蜕变。",
            1, 1, 0, GridType.Outdoor, 1  // 户外，户外格
        ));

        #endregion


        #region 星穹云纱 - 室内装饰

        allFurniture.Add(CreateFurniture(
            "sky_cloud_dressing_table", "浮云梳妆台", 70,
            "以失重云木打造的梳妆台，镜框流转着星尘，桌面仿佛凝结的晨雾。放置其上的饰品常有被微风轻抚的错觉，营造出高空闺阁的梦幻。",
            2, 2, 3, GridType.Floor, 4  // 卧室，地板
        ));

        allFurniture.Add(CreateFurniture(
            "sky_star_gauze_bed", "星河纱帐床", 90,
            "用星穹特有的“夜光云丝”织就床帐，薄如蝉翼却异常坚韧。入夜后，床的纱帐上会自动显现出缓慢流转的银河光影，守护着云端之上的安眠。",
            3, 2, 3, GridType.Floor, 6  // 卧室，地板
        ));

        allFurniture.Add(CreateFurniture(
            "sky_music_box", "旋音八音盒", 50,
            "一座精致的悬浮八音盒，打开后，会投射出微缩的旋转木马幻影，并奏响空灵如风铃的旋律。传说其音符能吸引星光小精灵短暂驻足。",
            1, 1, 3, GridType.Table, 1  // 卧室，桌面
        ));

        allFurniture.Add(CreateFurniture(
            "sky_star_dust_lamp", "星砾壁灯", 40,
            "灯罩内封存着会自发光的星穹矿物碎屑，光线柔和如月光。当室内完全安静时，碎屑会微微飘浮、旋转，仿佛一幅微型星云。",
            1, 2, 2, GridType.Wall, 2  // 客厅，墙面
        ));

        allFurniture.Add(CreateFurniture(
            "sky_rainbow_mirror", "虹镜", 80,
            "一面边缘镶嵌着彩虹水晶的椭圆镜。照镜时，镜中人的轮廓会带有极淡的虹彩光晕，据说能映照出内心最憧憬的幻想剪影。",
            1, 2, 3, GridType.Wall, 2  // 卧室，墙面
        ));

        allFurniture.Add(CreateFurniture(
            "sky_angel_candlestick", "天使侍烛台", 80,
            "一座小巧的六翼天使雕塑双手捧持着烛台，姿态虔诚。点燃蜡烛后，天使的羽翼会在墙壁上投下巨大而圣洁的光影，仿佛守护神降临。",
            1, 2, 2, GridType.Wall, 2  // 客厅，墙面
        ));

        allFurniture.Add(CreateFurniture(
            "sky_wishing_star", "许愿星瓶", 20,
            "透明的琉璃瓶中装着如同星辰的发光砂砾。每当有流星划过星穹云纱的高空，瓶中便会多出一粒“星光”，汇集足够多时，据说能带来好运。",
            1, 1, 2, GridType.Table, 1  // 客厅，桌面
        ));

        allFurniture.Add(CreateFurniture(
            "sky_star_track", "星轨仪", 80,
            "一座悬浮于磁力基座上的复杂黄铜仪器，由数个嵌套的、刻满星辰符号的圆环构成。它会以极慢的速度自行旋转，模拟星穹之上某种古老星座的运行轨迹，是观测天象与静思的绝佳伴侣。",
            2, 2, 1, GridType.Floor, 4  // 书房，地板
        ));

        allFurniture.Add(CreateFurniture(
            "sky_dream_phonograph", "梦呓留声机", 50,
            "喇叭花形状的纯白留声机，唱片由凝固的“声云”制成。播放出的音乐空灵飘渺，聆听者容易陷入半梦半醒的恍惚状态，据说在梦中能更清晰地听见高天风语的启示。",
            2, 1, 1, GridType.Floor, 2  // 书房，地板
        ));

        allFurniture.Add(CreateFurniture(
            "sky_mica_screen", "云母屏风", 350,
            "由大片半透明的天然云母拼接而成的屏风，每一片都呈现出不同的虹彩与云雾纹理。光线穿透时，会在室内投下斑驳陆离、不断变幻的光影，仿佛将一片活着的霞光禁锢其中。",
            3, 2, 2, GridType.Floor, 6  // 客厅，地板
        ));

        allFurniture.Add(CreateFurniture(
            "sky_stardust_hourglass", "星尘沉降沙漏", 220,
            "沙漏中流淌的不是沙，而是极其细微、闪烁着星光的尘埃。当上方的“星尘”缓缓落入下方，会堆积成一个小巧的、发光的星云状图案，每一次翻转都是独一无二的星空缩影。",
            1, 2, 1, GridType.Wall, 2  // 书房，墙面
        ));

        allFurniture.Add(CreateFurniture(
            "sky_dream_catcher", "捕梦虹网", 60,
            "悬挂在窗边或床角，由彩虹蛛丝编织成的精致网兜。据说能在夜晚捕捉游离的噩梦碎片，并在晨光中将其化为无害的彩色露珠滴落。",
            1, 1, 3, GridType.Wall, 1  // 卧室，墙面
        ));

        allFurniture.Add(CreateFurniture(
            "sky_meteor_pendant", "流星雨吊坠", 180,
            "以坠落的天马座流星雨为原型雕琢的水晶吊坠，悬挂时会自行缓慢旋转，尾端流淌出细碎的银辉，仿佛将一段永恒穿梭的星空定格在窗前。",
            1, 1, 3, GridType.Wall, 1  // 卧室，墙面
        ));

        allFurniture.Add(CreateFurniture(
            "sky_star_lamp", "星空灯", 350,
            "夜晚打开这盏灯的时候，美丽的星河在屋内流转，仿佛置身于梦幻的星穹云纱一样。注意仅限夜晚，白天可没有星空。",
            1, 1, 2, GridType.Table, 1  // 客厅，桌面
        ));

        #endregion

        #region 星穹云纱 - 室外装饰

        allFurniture.Add(CreateFurniture(
            "sky_floating_observatory", "悬空观星台", 500,
            "由反重力浮石砌成的平台，边缘有闪烁的导引星光。站于此地，仿佛置身云海之上，星空触手可及，是冥想与观星的绝佳场所。",
            3, 3, 0, GridType.Outdoor, 9  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "sky_wind_harp", "流风竖琴雕塑", 300,
            "一座白玉雕成的巨型竖琴，琴弦由固化风丝构成。高空永不止息的风拂过时，会奏出非人耳能捕捉、却能安抚心灵的“天之韵律”。",
            2, 3, 0, GridType.Outdoor, 6  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "sky_cloud_fountain", "云涡喷泉", 250,
            "喷泉中心并非水流，而是不断螺旋升腾、如牛奶般洁白的浓缩云气。在特定光照下，云涡中会折射出七彩光晕，宛如凝固的虹。",
            2, 2, 0, GridType.Outdoor, 4  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "sky_angel_sculpture", "六翼天使庭院雕塑", 300,
            "用整块“皎月石”雕刻的六翼天使像，展开的羽翼细腻如生。在星夜下，雕像会吸收星光，通体散发柔和的银辉，成为庭院中圣洁的焦点。",
            2, 3, 0, GridType.Outdoor, 6  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "sky_star_mirror_pond", "星空之镜水池", 350,
            "一方浅池，池底铺设深蓝色釉砖并镶嵌碎星石。无风时，水面如镜，完美倒映夜空星辰，天地在此交汇，界限模糊。",
            3, 1, 0, GridType.Outdoor, 3  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "sky_rainbow_path", "虹桥小径", 150,
            "庭院中用七彩鹅卵石铺就的弯曲小径，在雨后或特定角度光照下，石子会折射出微弱的彩虹光泽，行走其上，宛如漫步于缩小的天虹。",
            4, 1, 0, GridType.Outdoor, 4  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "sky_star_mailbox", "星语信箱", 180,
            "一个挂在云杉枝头、信箱形状的水晶容器。人们可以将写满心事的纸条投入其中，夜晚时，信箱会吸收星光，让纸条上的字迹如星辰般短暂闪烁，仿佛心事已被星空倾听。",
            1, 1, 0, GridType.Outdoor, 1  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "sky_floating_bonsai", "浮空盆景", 120,
            "栽种着发光苔藓和迷你星形花的特制陶盆，在底部镶嵌了微小的反重力石，使得盆景可以漂浮在离地一掌的高度，随着微风轻轻摇摆旋转。",
            1, 1, 0, GridType.Outdoor, 1  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "sky_moon_swing", "月华树与月光秋千", 280,
            "秋千绳仿佛由月光编织而成，发出柔和的银辉，座位是一弯新月形的软垫。在无月之夜，秋千自身的光芒最为明亮，荡起时会在空中留下淡淡的光痕。",
            2, 2, 0, GridType.Outdoor, 4  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "sky_ferris_wheel", "星穹摩天轮", 400,
            "巨大轮辐间镶嵌着模拟星体的琉璃舱，升至最高处时舱壁渐透明，让人恍若悬浮于浩瀚星河中央，伸手可触流转的永恒。",
            4, 4, 0, GridType.Outdoor, 16  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "sky_music_tree", "梦幻音符树", 200,
            "来自星穹云纱的魔法之树。当你轻轻摇晃它时，它会演奏出美妙的音乐。然而，要注意过犹不及，毕竟没人想听一首杂乱无章的乐谱奏出的噪音。",
            2, 3, 0, GridType.Outdoor, 6  // 户外，户外格
        ));

        #endregion


        #region 旭日沙漠 - 室内装饰

        allFurniture.Add(CreateFurniture(
            "desert_hourglass_candlestick", "沙漏烛台", 35,
            "以古老计时工具为灵感设计的烛台，细沙在烛光中缓缓流淌，每一粒都映照着旭日沙漠永不熄灭的热情。",
            1, 2, 2, GridType.Table, 2  // 客厅，桌面
        ));

        allFurniture.Add(CreateFurniture(
            "desert_spice_pot", "彩陶香料罐", 25,
            "绘制着沙漠神兽与繁花的陶罐，密封着炽风带来的神秘香料。打开时，仿佛能听见驼铃在烈日下回响。",
            1, 1, 2, GridType.Table, 1  // 客厅，桌面
        ));

        allFurniture.Add(CreateFurniture(
            "desert_feather_bed", "炽羽床", 120,
            "用沙漠火鸟褪下的羽毛编织而成的床幔，悬挂于床榻四周，夜晚会流淌出微暖的光泽，守护着梦境免受寒夜侵扰。",
            3, 2, 3, GridType.Floor, 6  // 卧室，地板
        ));

        allFurniture.Add(CreateFurniture(
            "desert_sun_carpet", "烈阳织毯", 45,
            "采用烈日晒染的彩线手工编织，花纹如流动的沙丘。赤脚踩上时，总能感受到大地残存的温度。",
            2, 1, 2, GridType.Floor, 2  // 客厅，地板
        ));

        allFurniture.Add(CreateFurniture(
            "desert_wind_chime", "流沙风铃", 20,
            "由风蚀琉璃与铜片制成，当热风穿过，会发出空灵的回响，传说能唤来远方的海市蜃楼。",
            1, 1, 2, GridType.Wall, 1  // 客厅，墙面
        ));

        allFurniture.Add(CreateFurniture(
            "desert_gem_mirror", "宝石魔镜", 60,
            "一面镶嵌珍贵宝石的镜子，传说每个旭日沙漠的自律女孩在照这种镜子时，都会不由自主地说出：“魔镜魔镜看看我，谁是世界上最美的女人？”",
            1, 2, 3, GridType.Wall, 2  // 卧室，墙面
        ));

        allFurniture.Add(CreateFurniture(
            "desert_aladdin_lamp", "阿拉丁神灯", 50,
            "在沙漠中流传着一个故事：像这样的神灯中，都寄宿着拥有实现愿望能力的神灯精灵。只有自律的人们才能成功召唤他们。",
            1, 1, 2, GridType.Table, 1  // 客厅，桌面
        ));

        #endregion

        #region 旭日沙漠 - 室外装饰（黑色-第一弹）

        allFurniture.Add(CreateFurniture(
            "desert_sundial", "镀金日晷", 80,
            "立于庭院的巨大日晷，晷针在沙地上投下锐利的影子，每一刻刻度都铭刻着沙漠部族对太阳的古老崇拜。",
            2, 2, 0, GridType.Outdoor, 4  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "desert_fire_basin", "赤砂火盆", 60,
            "用沙漠深处开采的赤色砂岩雕琢而成，盆中燃烧的火焰永不熄灭，是庆典与集会时凝聚欢乐的象征。",
            2, 1, 0, GridType.Outdoor, 2  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "desert_camel_sculpture", "驼队商雕", 250,
            "一组青铜铸造的骆驼与商人雕塑，驮着丝绸与宝石包裹，再现了旭日沙漠千年贸易之路的辉煌瞬间。",
            3, 2, 0, GridType.Outdoor, 6  // 户外，户外格
        ));

        #endregion


        #region 冰封峡湾 - 室内装饰（黑色-第一弹）

        allFurniture.Add(CreateFurniture(
            "ice_crystal_lamp", "冰晶棱镜灯", 80,
            "由峡湾深处挖掘的天然冰晶打磨而成，内嵌冷光苔。灯光清冷剔透，光线经过多面棱镜折射，在墙壁上洒下如极光般变幻的幽蓝色光斑。",
            1, 1, 2, GridType.Table, 1  // 客厅，桌面
        ));

        allFurniture.Add(CreateFurniture(
            "ice_frost_rug", "霜纹毛皮毯", 50,
            "取自雪原驯鹿腹部的柔软毛皮，以古老冰染技艺印上永不褪色的霜花纹理。",
            2, 1, 2, GridType.Floor, 2  // 客厅，地板
        ));

        allFurniture.Add(CreateFurniture(
            "ice_eternal_vase", "永冻花瓶", 220,
            "看似由永不融化的玄冰雕琢的花瓶，实则是一种特殊的低温琉璃。插入其内的鲜花或枝条，保鲜时间会大大延长，宛如被时光轻柔地冻结了绽放的瞬间。",
            1, 1, 2, GridType.Table, 1  // 客厅，桌面
        ));

        allFurniture.Add(CreateFurniture(
            "ice_fiord_mirror", "峡湾冰魄镜", 180,
            "镜面如同封存了一汪寒冷的峡湾湖水，异常清亮。照镜时，呼出的气息会在镜面凝成转瞬即逝的薄霜。",
            1, 2, 3, GridType.Wall, 2  // 卧室，墙面
        ));

        allFurniture.Add(CreateFurniture(
            "ice_owl_pen", "雪鸮羽毛笔", 50,
            "用成年雪鸮最修长的翎羽制成的羽毛笔，笔杆以霜木包裹。书写时流畅顺滑，据说在书写重要契约或信件时，能赋予文字以冰雪般的冷静与恒久。",
            1, 1, 1, GridType.Table, 1  // 书房，桌面
        ));

        allFurniture.Add(CreateFurniture(
            "ice_fireplace_chair", "暖炉故事椅", 120,
            "一张宽大、铺着厚实绒垫的摇椅，面向小巧的壁炉。椅背雕刻着峡湾传说中的生物，坐在其中被炉火烘暖，最适合阅读或回忆冬夜故事。",
            2, 1, 1, GridType.Floor, 2  // 书房，地板
        ));

        allFurniture.Add(CreateFurniture(
            "ice_aurora_scroll", "极光绘卷", 150,
            "一幅巨大的卷轴画，画布由冰原巨兽的腹膜制成，颜料掺入了极光尘埃。在黑暗中，画中的冰川、雪松与峡湾会幽幽亮起，呈现出流动的极光色彩，宛若将北地至景封印其中。",
            2, 2, 2, GridType.Wall, 4  // 客厅，墙面
        ));

        allFurniture.Add(CreateFurniture(
            "ice_frost_window", "凝霜窗花", 80,
            "并非贴纸，而是通过特殊工艺在玻璃内部形成的永恒霜花结晶。无论室外季节如何，窗上永远盛开着繁复精美的冰霜花纹，将目光过滤成清冷而朦胧的光。",
            1, 2, 1, GridType.Wall, 2  // 书房，墙面
        ));

        allFurniture.Add(CreateFurniture(
            "ice_snow_clock", "雪花时钟", 120,
            "当这个精美的时钟走动的时候，你能够听到冰封峡湾雪花下落的声音。",
            1, 1, 2, GridType.Wall, 1  // 客厅，墙面
        ));

        allFurniture.Add(CreateFurniture(
            "ice_crystal_ball", "凛冬水晶球", 150,
            "这个水晶球内永远保持着下雪的景象，而且没有机关。有时候真该感谢冰封峡湾的魔法师们。",
            1, 1, 3, GridType.Table, 1  // 卧室，桌面
        ));

        #endregion

        #region 冰封峡湾 - 室外装饰

        allFurniture.Add(CreateFurniture(
            "ice_deer_gate", "冰雕鹿铃门廊", 150,
            "用晶莹冰块雕刻的麋鹿立于门廊两侧，鹿角上悬挂着冰风铃。当峡湾的寒风吹过，风铃碰撞发出清脆如碎冰的声响，空灵悦耳。",
            3, 2, 0, GridType.Outdoor, 6  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "ice_bear_sculpture", "冬眠熊石雕", 380,
            "一大一小两座花岗岩熊雕塑，仿佛在洞穴中安然冬眠。它们身上常被孩子们披上小小的毛毯，或堆积上俏皮的雪帽，成为庭院中憨厚可爱的守护者。",
            2, 2, 0, GridType.Outdoor, 4  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "ice_hot_spring_well", "不冻涌泉井", 220,
            "一口看似普通的水井，其下连接着地下温泉脉。即使在最严寒的时节，井口也氤氲着白色暖雾，井水清澈微温，永不封冻。",
            2, 1, 0, GridType.Outdoor, 2  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "ice_aurora_pillar", "极光观测柱", 180,
            "一根高高的黑曜石柱，顶部镶嵌着能感应夜空能量的“极光石”。在极光出现时，石柱会发出与之共鸣的微光，成为指引观赏方向的静谧路标。",
            1, 3, 0, GridType.Outdoor, 3  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "ice_sled_decor", "雪道橇车装饰", 220,
            "一组复古风格的木质雪橇和滑雪板，倚靠在庭院墙边。不仅是对峡湾冬季交通的纪念，也为银装素裹的庭院增添了活力与冒险的气息。",
            2, 1, 0, GridType.Outdoor, 2  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "ice_fisher_sculpture", "冰钓智者雕塑", 180,
            "一尊静坐在冰窟旁的老人冰雕，身披毛皮，手持钓竿，神情安详专注。雕像内部中空，有时会被孩子们偷偷放入发光石头，让它在夜晚从内部透出宁静的蓝光。",
            1, 2, 0, GridType.Outdoor, 2  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "ice_snow_wind_chime", "雪语风铃", 130,
            "用薄如蝉翼的冰片与深海贝壳制成的风铃。寒风敲击时，冰片相撞声清越，贝壳嗡鸣声低沉，合奏出一曲凛冽而孤寂的冬日交响，诉说着雪原的古老故事。",
            1, 1, 0, GridType.Outdoor, 1  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "ice_eternal_fire", "永恒篝火盆", 225,
            "石盆中的“火焰”由永不融化的红色晶体雕刻而成，中心嵌有发光矿物。即使在暴风雪中，这簇“火焰”也依然散发着视觉上的温暖与光明，是精神上的指引与慰藉。",
            2, 1, 0, GridType.Outdoor, 2  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "ice_christmas_tree", "常青圣诞树", 350,
            "这棵圣诞树在每天晚上都会亮起灯火。它的常青不是因为它是一颗假树，而是因为它被施加了青春永驻魔法。",
            2, 3, 0, GridType.Outdoor, 6  // 户外，户外格
        ));

        allFurniture.Add(CreateFurniture(
            "ice_igloo", "冰屋", 950,
            "来自冰封峡湾的礼物，使用特殊的永冻冰块制作而成，能够千年不化。",
            3, 2, 0, GridType.Outdoor, 6  // 户外，户外格
        ));

        #endregion


        Debug.Log($"默认家具数据加载完成，共 {allFurniture.Count} 件家具");



        SaveToJson();
    }

    /// <summary>
    /// 创建家具数据（辅助方法）
    /// </summary>
    private FurnitureData CreateFurniture(
        string id, string name, int price, string description,
        int width, int height, int roomType, GridType gridType, int gridCount,
        bool providesGrids = false, GridType providedType = GridType.Floor, int providedCount = 0)
    {
        FurnitureData data = new FurnitureData();
        data.id = id;
        data.name = name;
        data.price = price;
        data.description = description;
        data.width = width;
        data.height = height;
        data.requiredRoomType = roomType;
        data.providesNewGrids = providesGrids;
        data.providedGridType = providedType;
        data.providedGridCount = providedCount;

        // 设置格子需求
        data.gridRequirements = new GridRequirement[1];
        data.gridRequirements[0] = new GridRequirement()
        {
            requiredType = gridType,
            count = gridCount
        };

        return data;
    }

    /// <summary>
    /// 构建索引
    /// </summary>
    private void BuildIndices()
    {
        furnitureById.Clear();
        furnitureByRoom.Clear();

        foreach (var furniture in allFurniture)
        {
            // 按ID索引
            if (!furnitureById.ContainsKey(furniture.id))
            {
                furnitureById[furniture.id] = furniture;
            }

            // 按房间类型分组
            int roomType = furniture.requiredRoomType;
            if (!furnitureByRoom.ContainsKey(roomType))
            {
                furnitureByRoom[roomType] = new List<FurnitureData>();
            }
            furnitureByRoom[roomType].Add(furniture);
        }

        Debug.Log($"已建立索引: {furnitureById.Count}个ID, {furnitureByRoom.Count}个房间类型");
    }

    /// <summary>
    /// 获取所有家具
    /// </summary>
    public List<FurnitureData> GetAllFurniture()
    {
        return new List<FurnitureData>(allFurniture);
    }

    /// <summary>
    /// 根据房间类型获取家具
    /// </summary>
    public List<FurnitureData> GetFurnitureByRoom(int roomType)
    {
        if (furnitureByRoom.ContainsKey(roomType))
        {
            return new List<FurnitureData>(furnitureByRoom[roomType]);
        }
        return new List<FurnitureData>();
    }

    /// <summary>
    /// 根据ID获取家具
    /// </summary>
    public FurnitureData GetFurnitureById(string id)
    {
        if (furnitureById.ContainsKey(id))
        {
            return furnitureById[id];
        }
        return null;
    }

    /// <summary>
    /// 保存家具数据到JSON文件
    /// </summary>
    public void SaveToJson()
    {
        FurnitureDataArray wrapper = new FurnitureDataArray();
        wrapper.furniture = allFurniture.ToArray();

        string json = JsonUtility.ToJson(wrapper, true);
        string path = Path.Combine(Application.persistentDataPath, jsonFileName);

        try
        {
            File.WriteAllText(path, json);
            Debug.Log($"家具数据已保存到: {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存家具数据失败: {e.Message}");
        }
    }

    /// <summary>
    /// 从JSON文件重新加载
    /// </summary>
    [ContextMenu("重新加载家具数据")]
    public void ReloadFromJson()
    {
        LoadFurnitureData();
    }

    /// <summary>
    /// 保存当前数据到JSON
    /// </summary>
    [ContextMenu("保存到JSON文件")]
    public void SaveToJsonFile()
    {
        SaveToJson();
    }
}

/// <summary>
/// 家具数据数组包装器（用于JSON序列化）
/// </summary>
[System.Serializable]
public class FurnitureDataArray
{
    public FurnitureData[] furniture;
}
