using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

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


        /*if (ShouldRotate)
            gameObject.transform.rotation = Quaternion.EulerAngles(0, 90, 0);*/
        //gameObject.transform.Rotate(0, ShouldRotate ? 90 : 0, 0);
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

    public int Width;
    public int Height;

    public float RealWidth;
    public float RealHeight;

    public float XLeft;
    public float ZTop;

    public bool[,] Maze;

    private Dictionary<GameObject, PrefabInfo> PrefabInfos;
    private System.Random _Random = new System.Random();

    // Use this for initialization
    void Start () {
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
        this._PlayerController = Player.GetComponent<PlayerController>();

        MapBuilder builder = new MapBuilder();
        builder.GetRandomMapOfSize(Height, Width);
        this.Maze = builder.Maze;

        this.SpawnMaze();

        int i = 0, j = 0;
        for (; i < Height; ++i)
        {
            for (; j < Width; ++j)
                if(!Maze[i, j])
                    break;

            if (!Maze[i, j])
                break;
        }

        this.Player.transform.position = new Vector3(j * this._GameInfo.TileWidth, -8.94f, i * this._GameInfo.TileHeight);
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

                for (int dj = j; dj < width && Maze[j, dj] && !done[j, dj]; ++dj)
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
