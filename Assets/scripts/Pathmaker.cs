using JetBrains.Annotations;
using skner.DualGrid;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;

// INTRO TO PROC GEN LAB
// all students: complete steps 1-6, as listed in this file
// optional: if you're up for a mind safari, complete the "extra tasks" to do at the very bottom

// STEP 1: ======================================================================================
// put this script on a Sphere... it SHOULD move around, and drop a path of floor tiles behind it

public class Pathmaker : MonoBehaviour
{

    // STEP 2: ============================================================================================
    // translate the pseudocode below

    //	DECLARE CLASS MEMBER VARIABLES:
    //	Declare a private integer called counter that starts at 0; 		// counter will track how many floor tiles I've instantiated
    [Header("The value we got from input field")]

    static public int maxFloorCount = 1000;

    public int chanceToChange = 75;

    private readonly int movementDistance = 1;

    [Header("Floor and Wall Prefab Properties")]

    [SerializeField]
    private int floors;

    //	Declare a public Transform called floorPrefab, assign the prefab in inspector;

    //	Declare a public Transform called pathmakerSpherePrefab, assign the prefab in inspector; 		// you'll have to make a "pathmakerSphere" prefab later

    public GameObject playerPrefabObject;

    [Header("Pathmaker Prefab Properties")]

    [SerializeField]
    private List<GameObject> PathObjects;

    public LayerMask pathmakerLayerMask;

    public GameObject pathmakerSpherePrefabObject;

    public const float waitTime = 0.0001f;

    static public int maxPathmakerLife = 50;

    [Header("Pathmaker Script GUI Properties")]

    [SerializeField]
    private GameObject pathmakerInputGUI;

    [SerializeField]
    private TMP_Text maxfloorInput;

    [SerializeField]
    private Slider changeToChangeSlide;

    [SerializeField]
    private TMP_Text changeToChangeInput;

    [SerializeField]
    private GameObject stage0Text;

    [SerializeField]
    private GameObject stage1Text;

    [SerializeField]
    private GameObject stage2Text;

    [SerializeField]
    private GameObject stage3Text;

    [Header("Post Level Generation Camera Properites")]

    [SerializeField]
    private float CameraMovementSpeed = 100f;

    public enum Grid
    {
        FLOOR,
        WALL,
        EMPTY
    }
    // Variables
    public Grid[,] gridHandler;

    public DualGridTilemapModule floorDualGridTilemap;

    public DualGridTilemapModule wallDualGridTilemap;

    void Start()
    {
        if (stage0Text != null && stage1Text != null)
        {
            stage0Text.SetActive(false);
            stage1Text.SetActive(true);
        }

        // Starts Path Generation Sequence
        StartCoroutine(CentralPathmakerControl());
    }

    private void Update()
    {
        // Scene Reset Hotkey
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneReset();
        }
    }

    // Problem 1: Not setting script per individual Pathmaker prefab will makes error pops like crazy

    // Problem 2: Setting script per individual Pathmaker prefad will make them continuously and limitlessly produce, duplicate and move until the editor lag out

    // Current objective: Setting up Object Array to work around problems above, whilst maintaining movement between each Pathmaker

    // Basing this project based on Walker Generator worked on past days to complete the script. This is where CentralPathMaker control comes in handy.

    // This is Central Pathmaker Control. Where PathMaker sections is truly executed. Like human with brain, so is Pathmaker with collective processing controls.

    // Room struct
    
    IEnumerator CentralPathmakerControl()
    {
        if (maxFloorCount == 0)
        {
            maxFloorCount = 200;
        }

        gridHandler = new Grid[maxFloorCount * 2 - 1, maxFloorCount * 2 - 1];

        for (int x = 0; x < gridHandler.GetLength(0); x++)
        {
            for (int y = 0; y < gridHandler.GetLength(1); y++)
            {
                gridHandler[x, y] = Grid.EMPTY;
            }
        }

        Vector3Int tileCenter = new Vector3Int(gridHandler.GetLength(0) / 2, gridHandler.GetLength(1) / 2, 0);

        // spawn the first PathMaker
        GameObject myNewPathmaker = Instantiate(pathmakerSpherePrefabObject, tileCenter + new Vector3(0.5f, 0.5f, 0), Quaternion.Euler(0, 0, 0));

        Camera.main.transform.Translate(myNewPathmaker.transform.position.x, myNewPathmaker.transform.position.y, 0);

        // add PathMaker to private PathMaker list
        PathObjects.Add(myNewPathmaker);

        StartCoroutine(LevelSizeScanner());

        float pathChance = (float)chanceToChange / 100f;

        float roomChance = (float)_chanceToCreateRoom / 100f;

        while (floors <= maxFloorCount) // Updates overtime as long as the condition is allowed
        {
            List<GameObject> pathmakerList = PathObjects;

            for (int j = 0; j <= maxPathmakerLife; j++) // Calculate general lifespan of Pathmakers
            {
                //Debug.Log("globalPathmakerLifespan" + j);

                int pathmakerLifeSpan = j;

                int numOfPathObject = PathObjects.Count;

                for (int i = 0; i < PathObjects.Count; i++) // Calculate individual Pathmaker behavior
                {
                    //Debug.Log("pathmakerIndex" + i);

                    GameObject curPathmaker = PathObjects[i];

                    Vector3 curPathmakerPos = curPathmaker.transform.position;

                    bool hasCreatedFloor = false;

                    bool onFloorTile = gridHandler[(int)curPathmakerPos.x, (int)curPathmakerPos.y] == Grid.FLOOR;

                    // If there are other pathmaker in proximity, disable PathmakerChance and force them to rotate other direction.

                    if (Physics.CheckSphere(curPathmakerPos, 0.25f, pathmakerLayerMask))
                    {
                        if (onFloorTile) // If the area below it is FLOOR, DO NOT create floor
                        {
                            Debug.Log("Floor Collided");

                            hasCreatedFloor = true;

                            PathmakerUpdate(i);
                        }

                        if (!onFloorTile) // If the area below it is NOT floor, DO create floor
                        {
                            Debug.Log("Floor Not Collided");

                            hasCreatedFloor |= FloorCreation(i);

                            PathmakerUpdate(i);

                            floors++;
                        }
                        
                        // Occasionally spawn a room
                        if (Random.value < roomChance)
                        {
                            Vector3 pathPos = curPathmaker.transform.position;
                            Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(pathPos.x), Mathf.RoundToInt(pathPos.y));
                            StartCoroutine(RoomSpawner(gridPos));
                        }

                        if (hasCreatedFloor)
                        {
                            yield return new WaitForSeconds(waitTime / PathObjects.Count);
                        }

                    }
                    else 
                    {
                        if (onFloorTile) // If the area below it is FLOOR, DO NOT create floor
                        {
                            Debug.Log("Floor Collided");

                            hasCreatedFloor = true;

                            PathmakerUpdate(i);

                            PathmakerChance(i, numOfPathObject, pathmakerLifeSpan, pathmakerList, pathChance);
                        }

                        if (!onFloorTile) // If the area below it is NOT floor, DO create floor
                        {
                            Debug.Log("Floor Not Collided");

                            hasCreatedFloor |= FloorCreation(i);

                            PathmakerUpdate(i);

                            PathmakerChance(i, numOfPathObject, pathmakerLifeSpan, pathmakerList, pathChance);

                            floors++;
                        }

                        // Occasionally spawn a room
                        if (Random.value < roomChance)
                        {
                            Vector3 pathPos = curPathmaker.transform.position;
                            Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(pathPos.x), Mathf.RoundToInt(pathPos.y));
                            StartCoroutine(RoomSpawner(gridPos));
                        }

                        if (hasCreatedFloor)
                        {
                            yield return new WaitForSeconds(waitTime / PathObjects.Count);
                        }
                    }


                }
            }
        }

        // Starts Pathmaker Self Destruct Sequence
        StartCoroutine(PathmakerSelfDestruct());
    }


    // Back to regular Pathmaker logics

    void PathmakerChance(int i, int numOfPathObject, int distanceLeft, List<GameObject> pathObjectsList, float pathChance)
    {
        const float ROTATE_CHANCE = 0.25f;
        const float DUPLICATE_THRESHOLD = 0.99f;

        if (Random.value <= pathChance)
        {
            float random = Random.value;
            GameObject curPathmaker = pathObjectsList[i];

            if (random <= ROTATE_CHANCE)
            {
                curPathmaker.transform.Rotate(Vector3.forward * 90); // Clockwise
            }
            else if (random <= ROTATE_CHANCE * 2)
            {
                curPathmaker.transform.Rotate(Vector3.forward * -90); // Counter-clockwise
            }
            else if (random > DUPLICATE_THRESHOLD)
            {
                PathDuplicator(i);
            }
            else if (numOfPathObject > 1 && distanceLeft <= maxPathmakerLife / 4)
            {
                PathmakerExpire(curPathmaker, i, pathObjectsList);
            }
        }
    }
    void PathDuplicator(int i)
    {
        float chanceToSplit = Random.value;

        GameObject pathmakerPrefab = pathmakerSpherePrefabObject;

        List<GameObject> pathmakerList = PathObjects;

        Vector3 curPos = PathObjects[i].transform.position;

        Quaternion rotateRight = Quaternion.Euler(0, 0, -90);
        Quaternion rotateLeft = Quaternion.Euler(0, 0, 90);

        if (chanceToSplit <= 0.25f)
        {
            PathmakerSpawner(pathmakerList, pathmakerPrefab, curPos, rotateLeft);
        }
        else if (chanceToSplit <= 0.5f)
        {
            PathmakerSpawner(pathmakerList, pathmakerPrefab, curPos, rotateRight);
        }
        else // Special case: spawns two pathmakers (left and right directions)
        {
            PathmakerSpawner(pathmakerList, pathmakerPrefab, curPos, rotateRight);
            PathmakerSpawner(pathmakerList, pathmakerPrefab, curPos, rotateLeft);
        }
    }

    //			Instantiate a floorPrefab clone at current position;

    bool FloorCreation(int i)
    {
        GameObject curPathmaker = PathObjects[i];
        Vector3Int curPathmakerPos = Vector3Int.FloorToInt(curPathmaker.transform.position);
        floorDualGridTilemap.DataTilemap.SetTile(curPathmakerPos, floorDualGridTilemap.DataTile);
        gridHandler[curPathmakerPos.x, curPathmakerPos.y] = Grid.FLOOR;
        Camera.main.transform.Translate(0, 0, -0.05f * (1f - (floors / maxFloorCount)) / 2);

        return true;
    }

    //			Move forward ("forward", as in, the direction I'm currently facing) by 5 units;
    //			Increment counter;


    void PathmakerUpdate(int i)
    {
        GameObject curPathmaker = PathObjects[i];
        Vector3 curPos = curPathmaker.transform.position;

        // Handle collision
        Collider[] hits = Physics.OverlapSphere(curPos, 0.25f, pathmakerLayerMask);
        foreach (var hit in hits)
        {
            if (hit.gameObject != curPathmaker)
            {
                Debug.Log("Pathmaker collided");
                float random = Random.value;
                curPathmaker.transform.Rotate(0, 0, random <= 0.5f ? -90 : 90);
                break;
            }
        }

        // Predict next move
        Vector3 nextPos = curPathmaker.transform.position + curPathmaker.transform.up * movementDistance;
        int nextX = Mathf.FloorToInt(nextPos.x);
        int nextY = Mathf.FloorToInt(nextPos.y);

        bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < gridHandler.GetLength(0) &&
                   y >= 0 && y < gridHandler.GetLength(1);
        }

        if (IsInBounds(nextX, nextY))
        {
            curPathmaker.transform.Translate(0, movementDistance, 0);
        }
        else
        {
            Debug.Log("Pathmaker hit boundary, rotating");
            curPathmaker.transform.Rotate(0, 0, 180); // Turn around
        }
    }

    //		Else:
    //			Destroy my game object; 		// self destruct if I've made enough tiles already

    // This is where Pathmaker self destructs.
    IEnumerator PathmakerSelfDestruct()
    {
        while (PathObjects.Count > 0)
        {
            for (int i = 0; i < PathObjects.Count; i++)
            {
                int numOfPathObject = PathObjects.Count;

                GameObject curPathmaker = PathObjects[i];

                List<GameObject> pathObjectsList = PathObjects;

                while (numOfPathObject > 0)
                {
                    PathmakerExpire(curPathmaker, i, pathObjectsList);

                    break;
                }

                yield return new WaitForSeconds(waitTime);

            }
        }

        // Starts Wall Generation Sequence
        StartCoroutine(CreateWall());
    }


    // MORE STEPS BELOW!!!........

    // STEP 3: =====================================================================================
    // implement, test, and stabilize the system

    //	IMPLEMENT AND TEST:
    //	- save your scene! the code could potentially be infinite / exponential, and crash Unity
    //	- put Pathmaker.cs on a sphere, configure all the prefabs in the Inspector, and test it to make sure it works
    //	STABILIZE: 
    //	- code it so that all the Pathmakers can only spawn a grand total of 500 tiles in the entire world; how would you do that?
    //	- hint: declare a "public static int" and have each Pathmaker check this "globalTileCount", somewhere in your code? 
    //      -  What is a 'static'?  Static???  Simply speak the password "static" to the instructor and knowledge will flow.
    //	- Perhaps... if there already are enough tiles maybe the Pathmaker could Destroy my game object

    // STEP 4: ======================================================================================
    // tune your values...

    // a. how long should a pathmaker live? etc.  (see: static  ---^)


    // Little section that holds variables for EACH objects in a list. Referencing PathmakerUpdate for deleting pathmaker after certain lifespan.

    static void PathmakerSpawner(List<GameObject> PathmakerObject, GameObject pathmakerSpherePrefabObject, Vector3 curPos, Quaternion rotation)
    {
        GameObject prefabclone1 = Instantiate(pathmakerSpherePrefabObject, curPos, rotation);
        PathmakerObject.Add(prefabclone1);
    }
    static void PathmakerExpire(GameObject walkingPathmaker, int i, List<GameObject> pathObjects)
    {
        Destroy(walkingPathmaker);
        pathObjects.RemoveAt(i);
    }

    // b. how would you tune the probabilities to generate lots of long hallways? does it... work?
    // c. tweak all the probabilities that you want... what % chance is there for a pathmaker to make a pathmaker? is that too high or too low?

    // Where room and corridor system locates
    [Header("Pathmaker Room and Corridor Properties")]
    public int roomAttempts = 10;

    public int roomMinXSize = 2;
    public int roomMaxXSize = 4;
    public int roomMinYSize = 4;
    public int roomMaxYSize = 4;

    public int _chanceToCreateRoom = 5;

    private List<Room> rooms = new List<Room>();
    IEnumerator RoomSpawner(Vector2Int nearPosition)
    {
        int attempts = 5;

        while (attempts-- > 0)
        {
            int roomW = Random.Range(roomMinXSize, roomMaxXSize);
            int roomH = Random.Range(roomMinYSize, roomMaxYSize);
            int offsetX = Random.Range(-6, 6);
            int offsetY = Random.Range(-6, 6);

            Vector2Int roomPos = new Vector2Int(nearPosition.x + offsetX, nearPosition.y + offsetY);
            Room newRoom = new Room(roomPos, roomW, roomH);

            bool overlaps = false;
            foreach (Room room in rooms)
            {
                if (newRoom.Bounds.Overlaps(room.Bounds))
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps && CreateRoom(newRoom))
            {
                if (rooms.Count > 1)
                {
                    CreateCorridor(rooms[rooms.Count - 2].Center, newRoom.Center);
                }

                yield return null;
                break;
            }
        }
    }

    bool CreateRoom(Room room)
    {
        for (int x = room.Bounds.xMin; x < room.Bounds.xMax; x++)
        {
            for (int y = room.Bounds.yMin; y < room.Bounds.yMax; y++)
            {
                if (!IsInBounds(x, y) || gridHandler[x, y] == Grid.FLOOR)
                    return false; // Reject room creation if floor already exists there
            }
        }

        for (int x = room.Bounds.xMin; x < room.Bounds.xMax; x++)
        {
            for (int y = room.Bounds.yMin; y < room.Bounds.yMax; y++)
            {
                gridHandler[x, y] = Grid.FLOOR;
                floorDualGridTilemap.DataTilemap.SetTile(new Vector3Int(x, y, 0), floorDualGridTilemap.DataTile);
                Camera.main.transform.Translate(0, 0, -0.05f * (1f - (floors / maxFloorCount)) / 2);
                floors++;
            }
        }

        rooms.Add(room);
        return true;
    }


    void CreateCorridor(Vector2Int from, Vector2Int to)
    {
        Vector2Int pos = from;

        while (pos.x != to.x)
        {
            int stepX = (to.x - pos.x) > 0 ? 1 : -1;
            pos.x += stepX;

            if (!IsInBounds(pos.x, pos.y)) break;

            if (gridHandler[pos.x, pos.y] != Grid.FLOOR)
            {
                gridHandler[pos.x, pos.y] = Grid.FLOOR;
                floorDualGridTilemap.DataTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), floorDualGridTilemap.DataTile);
                Camera.main.transform.Translate(0, 0, -0.05f * (1f - (floors / maxFloorCount)) / 2);
                floors++;
            }
        }

        while (pos.y != to.y)
        {
            int stepY = (to.y - pos.y) > 0 ? 1 : -1;
            pos.y += stepY;

            if (!IsInBounds(pos.x, pos.y)) break;

            if (gridHandler[pos.x, pos.y] != Grid.FLOOR)
            {
                gridHandler[pos.x, pos.y] = Grid.FLOOR;
                floorDualGridTilemap.DataTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), floorDualGridTilemap.DataTile);
                Camera.main.transform.Translate(0, 0, -0.05f * (1f - (floors / maxFloorCount)) / 2);
                floors++;
            }
        }
    }

    bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < gridHandler.GetLength(0) &&
               y >= 0 && y < gridHandler.GetLength(1);
    }

    public class Room
    {
        public Vector2Int position;
        public int width, height;
        public RectInt Bounds => new RectInt(position.x, position.y, width, height);
        public Vector2Int Center => new Vector2Int(position.x + width / 2, position.y + height / 2);

        public Room(Vector2Int pos, int w, int h)
        {
            position = pos;
            width = w;
            height = h;
        }
    }


    // STEP 5: ===================================================================================
    // maybe randomize it even more?

    // - randomize 2 more variables in Pathmaker.cs for each different Pathmaker... you would do this in Start()
    // - maybe randomize each pathmaker's lifetime? maybe randomize the probability it will turn right? etc. if there's any number in your code, you can randomize it if you move it into a variable



    // STEP 6:  =====================================================================================
    // art pass, usability pass

    // - move the game camera to a position high in the world, and then point it down, so we can see your world get generated
    // - CHANGE THE DEFAULT UNITY COLORS
    // - add more detail to your original floorTile placeholder -- and let it randomly pick one of 3 different floorTile models, etc. so for example, it could randomly pick a "normal" floor tile, or a cactus, or a rock, or a skull
    // - or... make large city tiles and create a city.  Set the camera low so and une the values so the city tiles get clustered tightly together.

    //		- MODEL 3 DIFFERENT TILES IN BLENDER.  CREATE SOMETHING FROM THE DEEP DEPTHS OF YOUR MIND TO PROCEDURALLY GENERATE. 
    //		- THESE TILES CAN BE BASED ON PAST MODELS YOU'VE MADE, OR NEW.  BUT THEY NEED TO BE UNIQUE TO THIS PROJECT AND CLEARLY TILE-ABLE.

    //		- then, add a simple in-game restart button; let us press [R] to reload the scene and see a new level generation
    // - with Text UI, name your proc generation system ("AwesomeGen", "RobertGen", etc.) and display Text UI that tells us we can press [R]


    // EXTRA TASKS TO DO, IF YOU WANT / DARE: ===================================================

    // AVOID SPAWNING A TILE IN THE SAME PLACE AS ANOTHER TILE  https://docs.unity3d.com/ScriptReference/Physics.OverlapSphere.html
    // Check out the Physics.OverlapSphere functionality... 
    //     If the collider is overlapping any others (the tile prefab has one), prevent a new tile from spawning and move forward one space. 

    // DYNAMIC CAMERA:
    // position the camera to center itself based on your generated world...
    // 1. keep a list of all your spawned tiles
    // 2. then calculate the average position of all of them (use a for() loop to go through the whole list) 
    // 3. then move your camera to that averaged center and make sure fieldOfView is wide enough?

    // LevelSizeScanner utilize individual Raycast System on each spawned objects
    // It is primary used to measure potential maximum and minimum size of the level generator, both horizonally and vertically

    IEnumerator LevelSizeScanner()
    {
        while ( true)
        {
            // Legacy code. Obsolete on current codebase. Kept for future references.
            /*

            // Local variable list
            List<Vector3Int> scannedVector = new List<Vector3Int>();

            // scan furthest vector3Int Eastward in X axis. Index 0 on the Vector3Int list.
            for (int x = 0; x < gridHandler.GetLength(0) - 1; x++)
            {
                for (int y = 0; y < gridHandler.GetLength(1) - 1; y++)
                {
                    LevelScanner(x, y, scannedVector);
                }
            }
            // scan furthest vector3Int Westward in X axis. Index 1 on the Vector3Int list.
            for (int x = gridHandler.GetLength(0) - 1; x > 0; x--)
            {
                for (int y = 0; y < gridHandler.GetLength(1) - 1; y++)
                {
                    LevelScanner(x, y, scannedVector);
                }
            }
            // scan lowest vector3Int in Y axis. Index 2 on the Vector3Int list.
            for (int y = 0; y < gridHandler.GetLength(1) - 1; y++)
            {
                for (int x = 0; x < gridHandler.GetLength(0) - 1; x++)
                {
                    LevelScanner(x, y, scannedVector);
                }
            }
            // scan higest vector3Int in Y axis. Index 3 on the Vector3Int list.
            for (int y = gridHandler.GetLength(1) - 1; y > 0; y--)
            {
                for (int x = 0; x < gridHandler.GetLength(0) - 1; x++)
                {
                    LevelScanner(x, y, scannedVector);
                }
            }
            */

            int width = gridHandler.GetLength(0) -1, height = gridHandler.GetLength(1) - 1;
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (gridHandler[x, y] == Grid.FLOOR)
                    {
                        minX = Mathf.Min(minX, x);
                        maxX = Mathf.Max(maxX, x);
                        minY = Mathf.Min(minY, y);
                        maxY = Mathf.Max(maxY, y);
                    }
                }
            }
            if (cameraAllowedToMove == false)
            {
                Vector3 cameraPointToMove = new Vector3
                (
                (minX + maxX) / 2f,
                (minY + maxY) / 2f,
                Camera.main.transform.position.z
                );
                Camera.main.transform.position = Vector3.MoveTowards(Camera.main.transform.position, cameraPointToMove, CameraMovementSpeed / Time.fixedDeltaTime);
            }
            yield return new WaitForSeconds(waitTime);
        }
    }

    bool cameraAllowedToMove = false;

    // BETTER UI:
    // learn how to use UI Sliders (https://unity3d.com/learn/tutorials/topics/user-interface-ui/ui-slider) 
    // let us tweak various parameters and settings of our tech demo
    // let us click a UI Button to reload the scene, so we don't even need the keyboard anymore.  Throw that thing out!

    public void MaxFloorInputField (string input)
    {
        int floorNumInput = int.Parse(input);
        if (floorNumInput < 0)
        {
            maxFloorCount = floorNumInput * -1;
        }
        if (floorNumInput == 0)
        {
            maxFloorCount = 200;
        }
        if (floorNumInput > 0)
        {
            maxFloorCount = floorNumInput;
        }
    }

    public void PathmakerChanceSliderInput()
    {
        chanceToChange = (int)changeToChangeSlide.value;
        string changeNum = changeToChangeSlide.value.ToString();
        //Debug.Log(changeNum);
        changeToChangeInput.text = changeNum;
    }

    public void PathmakerChanceIntInput(string input)
    {
        chanceToChange = int.Parse(input);
        changeToChangeSlide.value = Mathf.Clamp(float.Parse(input), 0, 100);
    }

    public void PathmakerStartTrigger()
    {
        gameObject.SetActive(true);
       
    }

    public void SceneReset()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    static float playerXPos;

    static float playerYPos;

    public void DestructiveTile(float a, float b, int r)
    {
        playerXPos = a;

        playerYPos = b;

        int curX = Mathf.FloorToInt(playerXPos);

        // Minus one to make sure this only happens below the player
        int curY = Mathf.FloorToInt(playerYPos);

        // First clear all wall tiles and fill empty files within the radius
        for (int y = curY + r; y >= curY - r; y--)
        {
            for (int x = curX - r; x <= curX + r; x++)
            {
                if (x >= 0 && x < gridHandler.GetLength(0) && y >= 0 && y < gridHandler.GetLength(1))
                {
                    FloorGenerator(x, y);
                }
            }
        }
        // Once done refill the outer with wall tiles
        for (int y = curY + r; y >= curY - r; y--)
        {
            for (int x = curX - r; x <= curX + r; x++)
            {
                if (gridHandler[x, y] == Grid.FLOOR)
                {
                    // Generate Walls if there are adjustant tiles

                    // boolean is now executed within itself OR the defined bool

                    WallGenerator(x + 1, y);
                    WallGenerator(x - 1, y);
                    WallGenerator(x, y + 1);
                    WallGenerator(x, y - 1);
                    WallGenerator(x + 1, y - 1);
                    WallGenerator(x - 1, y - 1);
                    WallGenerator(x + 1, y + 1);
                    WallGenerator(x - 1, y + 1);
                }
            }
        }
    }
    private bool IsWithinBounds(int x, int y)
    {
        return x >= 0 && x < gridHandler.GetLength(0) &&
               y >= 0 && y < gridHandler.GetLength(1);
    }

    private void PlaceWallTile(Vector3Int pos)
    {
        wallDualGridTilemap.DataTilemap.SetTile(pos, wallDualGridTilemap.DataTile);
        floorDualGridTilemap.DataTilemap.SetTile(pos, null);
        gridHandler[pos.x, pos.y] = Grid.WALL;
    }

    public bool WallTilePlacement(Vector3Int tilePos)
    {
        int x = tilePos.x;
        int y = tilePos.y;

        // Check bounds for all relevant positions
        if (!IsWithinBounds(x, y + 2) ||
            !IsWithinBounds(x - 1, y) ||
            !IsWithinBounds(x + 1, y))
            return false;

        // Check that all involved tiles are FLOOR
        if (gridHandler[x, y] == Grid.FLOOR &&
            gridHandler[x, y + 1] == Grid.FLOOR &&
            gridHandler[x, y + 2] == Grid.FLOOR &&
            gridHandler[x - 1, y] == Grid.FLOOR &&
            gridHandler[x + 1, y] == Grid.FLOOR)
        {
            PlaceWallTile(tilePos);
            PlaceWallTile(tilePos + Vector3Int.left);
            PlaceWallTile(tilePos + Vector3Int.right);
            return true;
        }

        return false;
    }

    public void FloorTilePlacement(Vector3Int tilePos)
    {
        if (gridHandler[tilePos.x, tilePos.y] == Grid.WALL)
        {
            wallDualGridTilemap.DataTilemap.SetTile(tilePos, null);
            floorDualGridTilemap.DataTilemap.SetTile(tilePos, floorDualGridTilemap.DataTile);
            gridHandler[tilePos.x, tilePos.y] = Grid.FLOOR;

            int x = tilePos.x;
            int y = tilePos.y;

            WallGenerator(x + 1, y);
            WallGenerator(x - 1, y);
            WallGenerator(x, y + 1);
            WallGenerator(x, y - 1);
            WallGenerator(x + 1, y - 1);
            WallGenerator(x - 1, y - 1);
            WallGenerator(x + 1, y + 1);
            WallGenerator(x - 1, y + 1);
        }
    }

    // WALL GENERATION
    // add a "wall pass" to your proc gen after it generates all the floors
    // 1. raycast out from each floor tile (that'd be 4 raycasts per floor tile, in a square "ring" around each tile?)
    // 2. if the raycast "fails" that means there's empty void there, so then instantiate a Wall tile prefab
    // 3. ... repeat until walls surround your entire floorplan
    // (technically, you will end up raycasting the same spot over and over... but the "proper" way to do this would involve keeping more lists and arrays to track all this data)

    // hasCreatedWall becomes true if any surrounding tile triggers WallGenerator successfully.
    // Once true, it stays true until the end of this tile's wall generation pass.
    IEnumerator CreateWall()
    {
        cameraAllowedToMove = true;

        int batchSize = 50; // Tune this number based on performance
        int currentBatchCount = 0;

        int width = gridHandler.GetLength(0);
        int height = gridHandler.GetLength(1);

        bool InBounds(int x, int y) =>
            x >= 0 && x < width && y >= 0 && y < height;

        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                if (gridHandler[x, y] == Grid.FLOOR)
                {
                    Vector2Int[] directions = new Vector2Int[]
                    {
                    new Vector2Int(1, 0), new Vector2Int(-1, 0),
                    new Vector2Int(0, 1), new Vector2Int(0, -1),
                    new Vector2Int(1, 1), new Vector2Int(-1, -1),
                    new Vector2Int(-1, 1), new Vector2Int(1, -1)
                    };

                    foreach (var dir in directions)
                    {
                        int nx = x + dir.x;
                        int ny = y + dir.y;

                        if (InBounds(nx, ny))
                        {
                            if(WallGenerator(nx, ny))
                            {
                                currentBatchCount++;
                            }
                        }
                    }

                    if (currentBatchCount >= batchSize)
                    {
                        currentBatchCount = 0;
                        yield return null;
                    }
                }
            }
        }

        StartCoroutine(PlayerSpawner());
    }

    bool WallGenerator(int x, int y)
    {
        if (gridHandler[x, y] == Grid.EMPTY)
        {
            wallDualGridTilemap.DataTilemap.SetTile(new Vector3Int(x, y, 0), wallDualGridTilemap.DataTile);
            gridHandler[x, y] = Grid.WALL;
            return true;
        }
        return false;
    }

    bool FloorGenerator(int x, int y)
    {
        if (gridHandler[x, y] == Grid.EMPTY)
        {
            // Set floor tile in current position
            floorDualGridTilemap.DataTilemap.SetTile(new Vector3Int(x, y, 0), floorDualGridTilemap.DataTile);
            // Redesignate this tile as FLOOR tile
            gridHandler[x, y] = Grid.FLOOR;
            // calls true
            return true;
        }
        if (gridHandler[x, y] == Grid.WALL)
        {

            // Clear wall tile in current position
            wallDualGridTilemap.DataTilemap.SetTile(new Vector3Int(x, y, 0), null);
            // Set floor tile in current position
            floorDualGridTilemap.DataTilemap.SetTile(new Vector3Int(x, y, 0), floorDualGridTilemap.DataTile);
            // Redesignate this tile as FLOOR tile
            gridHandler[x, y] = Grid.FLOOR;
            // calls true
            return true;
        }
        return false;
    }

    public GameObject CursorControllerObject;

    IEnumerator PlayerSpawner()
    {
        CursorControllerObject.SetActive(true);

        bool playerSpawned = false;
        for (int y = gridHandler.GetLength(1) - 1; y > 0; y--)
        {
            for (int x = 0; x < gridHandler.GetLength(0) - 1; x++)
            {
                if (playerSpawned) yield break;

                bool floorCheck = false;

                // Runs a first check if the area it's scanning on is floor
                floorCheck |= FloorCheck(x, y);

                // Run a 2nd check based on the first check if there's a wall tile under a floor tile

                // Inside WallCheck there is additional check for the chance to player to spawn in

                // If all checks satisfied, loop ends

                if (floorCheck == true && playerSpawned == false)
                {
                    playerSpawned |= WallCheckForPlayer(x, y);
                    yield return null;
                }
            }
        }
    }

    public bool FloorCheck(int x, int y)
    {
        if (gridHandler[x, y] == Grid.FLOOR)
        {
            return true;
        }
        return false;
    }


    bool WallCheckForPlayer(int x, int y)
    {
        if (gridHandler[x, y - 1] == Grid.WALL)
        {
            float playerSpawnChance = Random.value;

            if (playerSpawnChance < 0.25f)
            {
                Vector3 curPos = new Vector3(x , y, 0);

                GameObject playerObject = Instantiate(playerPrefabObject, curPos + new Vector3(0.5f, 0.5f, 0), Quaternion.Euler(0,0,0));

                StartCoroutine(PlayerTracker(playerObject));

                return true;
            }
        }
        return false;
    }

    IEnumerator PlayerTracker(GameObject playerObject)
    {
        while (true)
        {
            Vector3 cameraPointToMove = new Vector3
            (
            playerObject.transform.position.x,
            playerObject.transform.position.y,
            Camera.main.transform.position.z
            );

            Camera.main.transform.position = Vector3.MoveTowards(Camera.main.transform.position, cameraPointToMove, CameraMovementSpeed * Time.deltaTime);

            yield return null;
        }
    }
}