using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class PrefabInfo
{
    public float XSize { get; set; }
    public float ZSize { get; set; }
    public float YSize { get; set; }
    public float MaxSize => Math.Max(XSize, ZSize);
    public float MinSize => Math.Min(XSize, ZSize);

    public float YPosition { get; set; }
}

public class SpawnInfo
{
    public Vector3 Scale { get; set; }
    public float YPosition { get; set; }
    public bool ShouldRotate { get; set; }

    public int XStart { get; set; }
    public int ZStart { get; set; }

    public float XSize { get; set; }
    public float YSize { get; set; }
    public float ZSize { get; set; }

    public int FilledCountWidth { get; set; }
    public int FilledCountHeight { get; set; }

    public void Apply(GameObject gameObject, GameInfo gameInfo)
    {
        gameObject.transform.localScale = new Vector3(
           gameObject.transform.localScale.x * this.Scale.x,
           gameObject.transform.localScale.y * this.Scale.y,
           gameObject.transform.localScale.z * this.Scale.z
        );

        gameObject.transform.position = new Vector3(
            gameInfo.XLeft - this.XStart * gameInfo.TileWidth - this.XSize * (this.Scale.x - 1) * 0.5f,
            this.YPosition,// + this.YSize * (this.Scale.y - 1) * 0.5f,
            gameInfo.ZTop + this.ZStart * gameInfo.TileHeight + this.ZSize * (this.Scale.z - 1) * 0.5f
        );
    }
}

public class GameController : MonoBehaviour {

    public GameInfo _GameInfo { get; set; }
    public GameObject Player { get; set; }
    public PlayerController _PlayerController { get; set; }

    public GameObject[] WallPrefabs;
    public float[] WallPrefabsYs;

    public GameObject[] AnglePrefabs;
    public float[] AnglePrefabsYs;

    public GameObject[] SmallObstacles;
    public float[] SmallObstaclesYs;

    public GameObject[] Details;
    public int DetailsAmount;
    public int FindAmount;

    public int Width;
    public int Height;

    public float RealWidth;
    public float RealHeight;

    public float XLeft;
    public float ZTop;

    public bool[,] Maze;
    public float MaxTakeRange;

    public Text PressEText;
    public Text TasksText;
    public Text WellDoneText;

    private AudioSource PlayerAudioSource;

    public AudioClip StepsAudio;
    public AudioClip DetailAudio;
    public AudioClip LightAudio;

    private Dictionary<GameObject, GameObject> Origins;
    private Dictionary<GameObject, Color> DetailColors;
    private Dictionary<GameObject, PrefabInfo> PrefabInfos;

    private Dictionary<GameObject, int> Taken;
    private Dictionary<GameObject, int> FindTask;
    private Dictionary<GameObject, int> SpawnedCount;

    private System.Random _Random = new System.Random();

    // Use this for initialization
    void Start () {
        this.WellDoneText.enabled = false;
        this.PressEText.enabled = false;
        Cursor.visible = false;

        PrefabInfos = new Dictionary<GameObject, PrefabInfo>();
        AddPrefabInfos(PrefabInfos, WallPrefabs, WallPrefabsYs);
        AddPrefabInfos(PrefabInfos, AnglePrefabs, AnglePrefabsYs);
        AddPrefabInfos(PrefabInfos, SmallObstacles, SmallObstaclesYs);

        this._GameInfo = new GameInfo();
        this._GameInfo.Height = this.Height;
        this._GameInfo.Width = this.Width;
        this._GameInfo.RealHeight = this.RealHeight;
        this._GameInfo.RealWidth = this.RealWidth;
        this._GameInfo.XLeft = this.XLeft;
        this._GameInfo.ZTop = this.ZTop;

        this.Player = GameObject.Find("Robot");
        this.PlayerAudioSource = this.Player.GetComponent<AudioSource>();
        this._PlayerController = Player.GetComponent<PlayerController>();

        this._PlayerController.OnColorChanged += _PlayerController_OnColorChanged;
        this._PlayerController.TryTakeDetail += _PlayerController_TryTakeDetail;
        this._PlayerController.OnMoved += _PlayerController_OnMoved;
        this._PlayerController.OnStopped += _PlayerController_OnStopped;

        MapBuilder builder = new MapBuilder();
        builder.GetRandomMapOfSize(Height, Width);
        this.Maze = builder.Maze;

        this.SpawnMaze();

        List<(int x, int y)> emptyPoints = new List<(int x, int y)>();
        for (int i = 0; i < Height; ++i)
        {
            for (int j = 0; j < Width; ++j)
            {
                if (!Maze[i, j])
                    emptyPoints.Add((j, i));
            }
        }

        if (emptyPoints.Count != 0)
        {
            (int x, int y) robotPoint = this.GetRandomItem(emptyPoints);

            this.Player.transform.position = new Vector3(
                this._GameInfo.XLeft - robotPoint.x * this._GameInfo.TileWidth, -6.85f,
                this._GameInfo.ZTop + robotPoint.y * this._GameInfo.TileHeight
            );
        }

        DetailColors = new Dictionary<GameObject, Color>();
        SpawnedCount = new Dictionary<GameObject, int>();
        Origins = new Dictionary<GameObject, GameObject>();
        Taken = new Dictionary<GameObject, int>();

        for(int k = 0; k < DetailsAmount; ++k)
        {
            GameObject randomDetail = GetRandomItem(Details);
            (int x, int z) detailPoint = GetRandomItem(emptyPoints);

            float dx = (float)this._Random.NextDouble();
            float dz = (float)this._Random.NextDouble();

            Vector3 position = new Vector3(
                this._GameInfo.XLeft - (detailPoint.x + dx) * this._GameInfo.TileWidth,
                -5,
                this._GameInfo.ZTop + (detailPoint.z + dz) * this._GameInfo.TileHeight
            );

            int colorRand = this._Random.Next(0, 3);

            Color color = Color.red;
            if (colorRand == 1)
                color = Color.green;
            else if (colorRand == 2)
                color = Color.blue;

            GameObject detail = GameObject.Instantiate(randomDetail);
            detail.transform.position = position;

            DetailColors.Add(detail, color);

            if (!SpawnedCount.ContainsKey(randomDetail))
                SpawnedCount.Add(randomDetail, 0);

            SpawnedCount[randomDetail]++;
            Origins.Add(detail, randomDetail);
        }

        int leftFindAmount = this.FindAmount;
        FindTask = new Dictionary<GameObject, int>();
        foreach(GameObject obj in Details)
        {
            int count = this._Random.Next(0, Math.Min(leftFindAmount, this.SpawnedCount[obj]));
            FindTask.Add(obj, count);
            leftFindAmount -= count;
        }

        this.HandleColorChange();
        this.UpdateTaskList();
	}

    private void _PlayerController_OnStopped(object sender, EventArgs e)
    {
        if (this.PlayerAudioSource.isPlaying && this.PlayerAudioSource.clip == this.StepsAudio)
            this.PlayerAudioSource.Stop();
    }

    private void _PlayerController_OnMoved(object sender, EventArgs e)
    {
        this.PressEText.enabled = this.GetNearestDetail() != null;

        if (!this.PlayerAudioSource.isPlaying)
        {
            this.PlayerAudioSource.clip = this.StepsAudio;
            this.PlayerAudioSource.Play();
        }
    }

    private void _PlayerController_TryTakeDetail(object sender, EventArgs e)
    {
        GameObject nearest = this.GetNearestDetail();

        if (nearest == null)
            return;

        GameObject origin = this.Origins[nearest];

        if (!Taken.ContainsKey(origin))
            Taken.Add(origin, 0);
        Taken[origin]++;

        this.DetailColors.Remove(nearest);
        this.Origins.Remove(nearest);

        GameObject.Destroy(nearest);

        this.UpdateTaskList();
        this.CheckFinish();

        this.PressEText.enabled = this.GetNearestDetail() != null;

        if (!this.PlayerAudioSource.isPlaying)
        {
            this.PlayerAudioSource.clip = this.DetailAudio;
            this.PlayerAudioSource.Play();
        }
    }

    private void UpdateTaskList()
    {
        string[] tasks = this.FindTask
            .Where(task => task.Value != 0)
            .Select(task => $"{task.Key.name}: {(this.Taken.ContainsKey(task.Key) ? this.Taken[task.Key] : 0)}/{task.Value}")
            .ToArray();

        this.TasksText.text = string.Join(
            "\n", tasks
        );
    }

    private void CheckFinish()
    {
        bool finished = this.FindTask.All(
            task => task.Value <= (this.Taken.ContainsKey(task.Key) ? this.Taken[task.Key] : 0)
        );

        if (finished)
            this.WellDoneText.enabled = true;
    }

    private GameObject GetNearestDetail()
    {
        if (!this._PlayerController.CurrentColor.HasValue)
            return null;

        Dictionary<GameObject, float> orderedDetails = this.DetailColors
            .Where(kvp => this.DetailColors[kvp.Key] == this._PlayerController.CurrentColor.Value)
            .ToDictionary(
                kvp => kvp.Key, 
                kvp => (kvp.Key.transform.position - this.Player.transform.position).magnitude
            ).OrderBy(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return orderedDetails.FirstOrDefault(kvp => kvp.Value <= MaxTakeRange).Key;
    }

    private void _PlayerController_OnColorChanged(object sender, EventArgs e)
    {
        this.HandleColorChange();

        if (!this.PlayerAudioSource.isPlaying)
        {
            this.PlayerAudioSource.clip = this.LightAudio;
            this.PlayerAudioSource.Play();
        }
    }

    private void HandleColorChange()
    {
        List<GameObject> visible = this.DetailColors.Where(
            kvp => this._PlayerController.CurrentColor.HasValue && kvp.Value == this._PlayerController.CurrentColor.Value
        ).Select(kvp => kvp.Key).ToList();

        List<GameObject> invisible = this.DetailColors.Keys.Except(visible).ToList();

        foreach (GameObject obj in visible)
            obj.SetActive(true);

        foreach (GameObject obj in invisible)
            obj.SetActive(false);
    }

    // Update is called once per frame
    void Update () {
        
    }

    private void SpawnMaze()
    {
        int height = Maze.GetLength(0);
        int width = Maze.GetLength(1);

        bool[,] done = new bool[height, width];

        for (int i = 0; i < height; ++i)
        {
            for(int j = 0; j < width; ++j)
            {
                if (!Maze[i, j])
                    done[i, j] = true;

                if (done[i, j])
                    continue;

                int lineWidth = 0;
                int lineHeight = 0;

                for (int di = i; di < height && Maze[di, j] && !done[di, j]; ++di)
                    ++lineHeight;

                for (int dj = j; dj < width && Maze[i, dj] && !done[i, dj]; ++dj)
                    ++lineWidth;

                if (lineWidth > lineHeight)
                {
                    (int doneX, int doneZ) = this.Spawn(j, i, lineWidth, 1);
                    for (int k = 0; k < doneX; ++k)
                        done[i, j + k] = true;
                }
                else
                {
                    (int doneX, int doneZ) = this.Spawn(j, i, 1, lineHeight);
                    for (int k = 0; k < doneZ; ++k)
                        done[i + k, j] = true;
                }
            }
        }
    }

    private (int doneX, int doneZ) Spawn(int x, int z, int sizeX, int sizeZ)
    {
        if (sizeX == 0 || sizeZ == 0)
            return (0, 0);

        IDictionary<GameObject, SpawnInfo> appropriateObjectSpawnInfos = this.GetAppropriateGameObjets(PrefabInfos, sizeX, sizeZ);

        GameObject selectedObject = null; 
        SpawnInfo selectedSpawnInfo = null;

        if(appropriateObjectSpawnInfos.Count != 0)
        {
            selectedObject = GetRandomItem(appropriateObjectSpawnInfos.Keys);
            selectedSpawnInfo = appropriateObjectSpawnInfos[selectedObject];
        }
        else
        {
            selectedObject = GetRandomItem(SmallObstacles);
            selectedSpawnInfo = GetSmallItemSpawnInfo(selectedObject);
        }

        selectedSpawnInfo.XStart = x;
        selectedSpawnInfo.ZStart = z;

        GameObject instantiated = GameObject.Instantiate(selectedObject);
        selectedSpawnInfo.Apply(instantiated, this._GameInfo);

        return (selectedSpawnInfo.FilledCountWidth, selectedSpawnInfo.FilledCountHeight);
    }

    private IDictionary<GameObject, SpawnInfo> GetAppropriateGameObjets(IDictionary<GameObject, PrefabInfo> source, int requiredCountWidth, int requiredCountHeight)
    {
        float requiredRealWidth = requiredCountWidth * this._GameInfo.TileWidth;
        float requiredRealHeight = requiredCountHeight * this._GameInfo.TileHeight;

        Dictionary<GameObject, SpawnInfo> appropriateObjectsSpawnInfo = new Dictionary<GameObject, SpawnInfo>();

        foreach(KeyValuePair<GameObject, PrefabInfo> gameObjectInfo in source)
        {
            bool shouldRotate = (requiredRealWidth > requiredRealHeight) != (gameObjectInfo.Value.XSize > gameObjectInfo.Value.ZSize);

            float width = shouldRotate ? gameObjectInfo.Value.ZSize : gameObjectInfo.Value.XSize;
            float height = shouldRotate ? gameObjectInfo.Value.XSize : gameObjectInfo.Value.ZSize;

            int filledCountWidth = Math.Min(requiredCountWidth, (int)Math.Ceiling(requiredRealWidth / width));
            int filledCountHeight = Math.Min(requiredCountHeight, (int)Math.Ceiling(requiredRealHeight / height));

            float scaleWidth = filledCountWidth * this._GameInfo.TileWidth / width;
            float scaleHeight = filledCountHeight * this._GameInfo.TileHeight / height;

            if (Math.Abs(scaleWidth - scaleHeight) < 0.3)
            {
                SpawnInfo spawnInfo = new SpawnInfo();

                spawnInfo.ShouldRotate = shouldRotate;
                spawnInfo.FilledCountWidth = filledCountWidth;
                spawnInfo.FilledCountHeight = filledCountHeight;
                spawnInfo.Scale = new Vector3(scaleWidth, (scaleWidth + scaleHeight) / 2.0f, scaleHeight);
                spawnInfo.YPosition = gameObjectInfo.Value.YPosition;
                spawnInfo.XSize = width;
                spawnInfo.ZSize = height;
                spawnInfo.YSize = gameObjectInfo.Value.YSize;

                appropriateObjectsSpawnInfo.Add(gameObjectInfo.Key, spawnInfo);
            }
        }

        return appropriateObjectsSpawnInfo;
    }

    private SpawnInfo GetSmallItemSpawnInfo(GameObject obj)
    {
        Vector3 size = this.GetGameObjectSize(obj);

        float scaleWidth = this._GameInfo.TileWidth / size.x;
        float scaleHeight = this._GameInfo.TileHeight / size.z;

        float scaleY = (scaleHeight + scaleWidth) / 2.0f;

        return new SpawnInfo()
        {
            FilledCountHeight = 1,
            FilledCountWidth = 1,
            Scale = new Vector3(scaleWidth, scaleY, scaleHeight),
            ShouldRotate = false,
            YPosition = this.PrefabInfos[obj].YPosition,
            XSize = size.x,
            YSize = size.y,
            ZSize = size.z
        };
    }

    private void AddPrefabInfos(IDictionary<GameObject, PrefabInfo> accumulator, GameObject[] prefabs, float[] ys)
    {
        for (int i = 0; i < prefabs.Length; ++i)
        {
            Vector3 size = this.GetGameObjectSize(prefabs[i]);
            PrefabInfo info = new PrefabInfo
            {
                XSize = size.x,
                YSize = size.y,
                ZSize = size.z,
                YPosition = ys[i]
            };
            accumulator.Add(prefabs[i], info);
        }
    }

    private Vector3 GetGameObjectSize(GameObject obj)
    {
        return obj.GetComponentInChildren<Renderer>().bounds.size;
    }

    private T GetRandomItem<T> (IEnumerable<T> collection)
    {
        return collection.ElementAt(_Random.Next(0, collection.Count()));
    }
}
