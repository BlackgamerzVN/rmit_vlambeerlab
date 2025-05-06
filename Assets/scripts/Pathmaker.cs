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

    private int movementDistance = 1;

    [Header("Floor and Wall Prefab Properties")]

    [SerializeField]
    private int floors;

    [SerializeField]
    private LayerMask floorLayerMask;

    [SerializeField]
    private LayerMask wallLayerMask;

    [SerializeField]
    private LayerMask floorAndWallLayerMask;

    //	Declare a public Transform called floorPrefab, assign the prefab in inspector;

    //	Declare a public Transform called pathmakerSpherePrefab, assign the prefab in inspector; 		// you'll have to make a "pathmakerSphere" prefab later

    public GameObject scannerPrefabObject;

    public GameObject playerPrefabObject;

    [Header("Pathmaker Prefab Properties")]

    [SerializeField]
    private List<GameObject> PathObjects;

    [SerializeField]
    private LayerMask pathmakerLayerMask;

    public GameObject pathmakerSpherePrefabObject;

    [SerializeField]
    private float waitTime = 0.01f;

    [SerializeField]
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

    [Header("Sound Properites")]

    [SerializeField]
    private AudioSource inputSound;
    
    [SerializeField]
    private AudioSource buttonSound;

    [SerializeField]
    private AudioSource genCompletedSound;

    [SerializeField]
    private List<AudioSource> floorGenSoundList;
    
    [SerializeField]
    private List<AudioSource> wallGenSoundList;

    [Header("Post Level Generation Camera Properites")]

    [SerializeField]
    private float CameraMovementSpeed = 100f;

    private bool CameraMovement = false;

    [SerializeField]
    private KeyCode UpKey = KeyCode.W;

    [SerializeField]
    private KeyCode DownKey = KeyCode.S;

    [SerializeField]
    private KeyCode LeftKey = KeyCode.A;

    [SerializeField]
    private KeyCode RightKey = KeyCode.D;

    [SerializeField]
    private KeyCode AltUpKey = KeyCode.UpArrow;

    [SerializeField]
    private KeyCode AltDownKey = KeyCode.DownArrow;

    [SerializeField]
    private KeyCode AltLeftKey = KeyCode.LeftArrow;

    [SerializeField]
    private KeyCode AltRightKey = KeyCode.RightArrow;
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

        // set the place to spawn
        Vector3 spawmPos = transform.position;

        

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

        // Where primary Camera control is located
        if (CameraMovement == true)
        {
            // Move Up control
            if (Input.GetKey(UpKey) || Input.GetKey(AltUpKey))
            {
                Camera.main.transform.Translate(0, CameraMovementSpeed * Time.deltaTime, 0);
                // Happens once after the key is pressed down
                HotkeyDebug(UpKey, AltUpKey);
            }

            // Move Down control
            if (Input.GetKey(DownKey) || Input.GetKey(AltDownKey))
            {
                Camera.main.transform.Translate(0, -CameraMovementSpeed * Time.deltaTime, 0);
                // Happens once after the key is pressed down
                HotkeyDebug(DownKey, AltDownKey);
            }

            // Move Left control
            if (Input.GetKey(LeftKey) || Input.GetKey(AltLeftKey))
            {
                Camera.main.transform.Translate(-CameraMovementSpeed * Time.deltaTime, 0, 0);
                // Happens once after the key is pressed down
                HotkeyDebug(LeftKey, AltLeftKey);
            }

            // Move Right control
            if (Input.GetKey(RightKey) || Input.GetKey(AltRightKey))
            {
                Camera.main.transform.Translate(CameraMovementSpeed * Time.deltaTime, 0, 0);
                // Happens once after the key is pressed down
                HotkeyDebug(RightKey, AltRightKey);
            }

            // Camera Sprint
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                CameraMovementSpeed = CameraMovementSpeed * 2;
            }
            
            // Camera Cancel Sprint
            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                CameraMovementSpeed = CameraMovementSpeed / 2;
            }

            // Camera Zoom in
            if (Input.GetKey(KeyCode.E))
            {
                Camera.main.transform.Translate(0,0,CameraMovementSpeed * Time.deltaTime);
            }
            
            // Camera Zoom Out
            if (Input.GetKey(KeyCode.Q))
            {
                Camera.main.transform.Translate(0, 0, -CameraMovementSpeed * Time.deltaTime);
            }
        }
    }

    static void HotkeyDebug(KeyCode mainkey, KeyCode altkey)
    {
        if (Input.GetKeyDown(mainkey))
        {
            //Sent in the log to confirm the movement check
            Debug.Log("Player pressed " + mainkey);
        }
        if (Input.GetKeyDown(altkey))
        {
            //Sent in the log to confirm the movement check
            Debug.Log("Player pressed " + altkey);
        }
    }

    // Problem 1: Not setting script per individual Pathmaker prefab will makes error pops like crazy

    // Problem 2: Setting script per individual Pathmaker prefad will make them continuously and limitlessly produce, duplicate and move until the editor lag out

    // Current objective: Setting up Object Array to work around problems above, whilst maintaining movement between each Pathmaker

    // Basing this project based on Walker Generator worked on past days to complete the script. This is where CentralPathMaker control comes in handy.

    // This is Central Pathmaker Control. Where PathMaker sections is truly executed. Like human with brain, so is Pathmaker with collective processing controls.
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

        Vector3Int TileCenter = new Vector3Int(gridHandler.GetLength(0) / 2, gridHandler.GetLength(1) / 2, 0);

        // spawn the first PathMaker
        GameObject myNewPathmaker = Instantiate(pathmakerSpherePrefabObject, TileCenter + new Vector3(0.5f, 0.5f, 0), Quaternion.Euler(0, 0, 0));

        // add PathMaker to private PathMaker list
        PathObjects.Add(myNewPathmaker);

        // Value finalization
        int maxFloor = maxFloorCount;

        float pathChance = (float)chanceToChange / 100f;

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

                    // First, checks for other Pathmaker in proximity. If there is, forces Pathmaker to rotate to other direction

                    // After that, it checks whenever if it's sitting up top of Floor prefab. If true they don't generate floor

                    // Result: Overlapping pathmaker paths is fixed. Pathmakers now moving more randomized and coherent.

                    // Problem: Overlap tile is still appearing during duplication. However minimal. Inaccuracies is now only in 1 digits.

                    if (Physics.CheckSphere(curPathmakerPos, 0f, pathmakerLayerMask)) // If there are other pathmaker in proximity, disable PathmakerChance and force them to rotate other direction.
                    {
                        if (gridHandler[(int)curPathmakerPos.x, (int)curPathmakerPos.y] == Grid.FLOOR) // If the area below it is FLOOR, DO NOT create floor
                        {
                            Debug.Log("Floor Collided");

                            hasCreatedFloor = true;

                            PathmakerUpdate(i, numOfPathObject);

                            if (hasCreatedFloor)
                            {
                                yield return new WaitForSeconds(waitTime / numOfPathObject);
                            }
                        }

                        if (gridHandler[(int)curPathmakerPos.x, (int)curPathmakerPos.y] != Grid.FLOOR) // If the area below it is NOT floor, DO create floor
                        {
                            Debug.Log("Floor Not Collided");

                            FloorCreation(hasCreatedFloor = true, floors++);

                            PathmakerUpdate(i, numOfPathObject);

                            if (hasCreatedFloor)
                            {
                                yield return new WaitForSeconds(waitTime / numOfPathObject);
                            }
                        }
                    }
                    else // If there are no other pathmaker in proximity, PathmakerChance operates as normal
                    {
                        if (gridHandler[(int)curPathmakerPos.x, (int)curPathmakerPos.y] == Grid.FLOOR) // If the area below it is FLOOR, DO NOT create floor
                        {
                            Debug.Log("Floor Collided");

                            hasCreatedFloor = true;

                            PathmakerUpdate(i, numOfPathObject);

                            PathmakerChance(i, numOfPathObject, pathmakerLifeSpan, pathmakerList, pathChance);

                            if (hasCreatedFloor)
                            {
                                yield return new WaitForSeconds(waitTime / numOfPathObject);
                            }
                        }

                        if (gridHandler[(int)curPathmakerPos.x, (int)curPathmakerPos.y] != Grid.FLOOR) // If the area below it is NOT floor, DO create floor
                        {
                            Debug.Log("Floor Not Collided");

                            FloorCreation(hasCreatedFloor = true, floors++);

                            PathmakerUpdate(i, numOfPathObject);

                            PathmakerChance(i, numOfPathObject, pathmakerLifeSpan, pathmakerList, pathChance);

                            if (hasCreatedFloor)
                            {
                                yield return new WaitForSeconds(waitTime / numOfPathObject);
                            }
                        }
                    }
                }
            }
        }

        // Starts Pathmaker Self Destruct Sequence
        StartCoroutine(PathmakerSelfDestruct());
    }
    void PathmakerChance(int i, int numOfPathObject, int distanceLeft, List<GameObject> pathObjectsList, float pathChance)
    {
        //		If counter is less than 50, then:
        //			Generate a random number from 0.0f to 1.0f;
        //			If random number is less than 0.25f, then rotate myself 90 degrees;
        //				... Else if number is 0.25f-0.5f, then rotate myself -90 degrees;
        //				... Else if number is 0.99f-1.0f, then instantiate a pathmakerSpherePrefab clone at my current position;
        //			// end elseIf

        float chance = Random.value;

        if (chance <= pathChance)
        {
            float random = Random.value;

            GameObject curPathmaker = PathObjects[i];

            if (random <= 0.25f) // first 1/4 chance rotate it clockwise
            {
                curPathmaker.transform.Rotate(new Vector3(0, 0, 90));
            }
            else if (random > 0.25f && random <= 0.5f) // 2nd 1/4 chance rotate it counterclockwise
            {
                curPathmaker.transform.Rotate(new Vector3(0, 0, -90));
            }
            else if (random > 0.99f && random < 1.0f) // fairly rare chance of duplicating itself.
            {
                PathDuplicator(i);
            }
            else // if it hits 1.0f
            {
                while (numOfPathObject > 1 && distanceLeft <= maxPathmakerLife/4)
                {
                    PathmakerExpire(curPathmaker, i, pathObjectsList); // Erase cur Pathmaker given the chance
                    break;
                }
            }
        }
    }
    void PathDuplicator(int i)
    {
        float chanceToSplit = Random.value;

        GameObject pathmakerPrefab = pathmakerSpherePrefabObject;

        List<GameObject> pathmakerList = PathObjects;

        Vector3 curPos = PathObjects[i].transform.position;

        Quaternion none = Quaternion.Euler(0, 0, 0);
        Quaternion right = Quaternion.Euler(0, 0, -90);
        Quaternion left = Quaternion.Euler(0, 0, 90);

        if (chanceToSplit <= 0.25f)
        {
            PathmakerSpawner(pathmakerList, pathmakerPrefab, curPos, left);
        }
        else if (chanceToSplit > 0.25f && chanceToSplit <= 0.5f)
        {
            PathmakerSpawner(pathmakerList, pathmakerPrefab, curPos, right);
        }
        else // this is a special case, as it spawns two more other object of its kind 
        {
            PathmakerSpawner(pathmakerList, pathmakerPrefab, curPos, right);
            PathmakerSpawner(pathmakerList, pathmakerPrefab, curPos, left);
        }
    }

    //			Instantiate a floorPrefab clone at current position;

    void FloorCreation(bool v, int e)
    {
        List<AudioSource> floorGenudioSources = floorGenSoundList;

        foreach (GameObject curPathmaker in PathObjects)
        {
            Vector3Int curPathmakerPos = Vector3Int.FloorToInt(curPathmaker.transform.position);
            floorDualGridTilemap.DataTilemap.SetTile(Vector3Int.FloorToInt(curPathmakerPos), floorDualGridTilemap.DataTile);
            gridHandler[curPathmakerPos.x, curPathmakerPos.y] = Grid.FLOOR;
            Camera.main.transform.Translate(0, 0, -0.75f * (1f - (floors / maxFloorCount)) / 2);

            /*
            int a = Random.Range(0, floorGenudioSources.Count);
            AudioSource curFloorAudioSource = floorGenudioSources[a];
            curFloorAudioSource.Play();
            */
        }
    }

    //			Move forward ("forward", as in, the direction I'm currently facing) by 5 units;
    //			Increment counter;


    void PathmakerUpdate(int i, int numOfPathObject) // Dictates the Pathmaker to moveforward
    {
        // Local variables
        int moveDistance = movementDistance;

        int updatedPathmakerCount = numOfPathObject;

        GameObject curPathmaker = PathObjects[i];

        Vector3 curPathmakerPos = curPathmaker.transform.position;

        if (Physics.CheckSphere(curPathmakerPos, 1.5f, pathmakerLayerMask))
        {

            Debug.Log("Pathmaker collided");

            float random = Random.value;
            if (random <= 0.5f)
            {
                curPathmaker.transform.Rotate(new Vector3(0, 0, -90));
                curPathmaker.transform.Translate(0, moveDistance, 0);
            }
            else
            {
                curPathmaker.transform.Rotate(new Vector3(0, 0, 90));
                curPathmaker.transform.Translate(0, moveDistance, 0);
            }
        }
        else
        {
            curPathmaker.transform.Translate(0, moveDistance, 0);
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
        // Finalize max floor count to use as max height
        int maxHeight = maxFloorCount;

        // Scanner Walk Distance
        int moveDistance = movementDistance;

        // Boolean used for the entire action sequence

        bool playerSpawned = false;

        List<GameObject> prefabScannerList = new List<GameObject>();

        // spawn Scanner Object Group
        GameObject prefabScannerGroup = Instantiate(scannerPrefabObject, this.transform.position, Quaternion.Euler(0, 0, 0));

        // It will first climb up from top to bottom of level gen on the Y axis

        // Spawns prefab Scanner Objects that also serves as the medium for where the scanner "moves" by Raycasting

        // Once their purposes are done, remove all prefab Scanner Objects.

        for (int i = maxHeight * moveDistance; i >= -maxHeight * moveDistance; i -= moveDistance)
        {
            bool floorcheck = false;

            int curheight = i;

            Vector3 spawnPos = new Vector3(-maxHeight * moveDistance, curheight, 0);

            // spawn Scanner Object
            GameObject prefabScanner = Instantiate(scannerPrefabObject, spawnPos, Quaternion.Euler(0, 0, 0), prefabScannerGroup.transform);
            prefabScannerList.Add(prefabScanner);

            Vector3 rightscan = prefabScanner.transform.TransformDirection(Vector3.right);

            // list of floor collision the raycast hit
            RaycastHit[] floorHits;

            floorHits = Physics.RaycastAll(spawnPos, rightscan, maxHeight * moveDistance * 2, floorLayerMask);

            for (int j = 0; j < floorHits.Length; j++)
            {
                RaycastHit curFloorHit = floorHits[j];

                // Collect current floor Object amongst the list of objects hit by raycast
                GameObject curFloorSelected = curFloorHit.transform.gameObject;

                Vector3 curFloorPosition = curFloorSelected.transform.position;

                Vector3 down = curFloorSelected.transform.TransformDirection(Vector3.down);

                // Checks if there is wall below the selected floor object 
                if (Physics.Raycast(curFloorPosition, down, moveDistance, wallLayerMask))
                {
                    floorcheck = true;
                }

                if (floorcheck == true && playerSpawned == false) 
                {
                    yield return new WaitForSeconds(waitTime);
                    playerSpawned = true;
                    GameObject prefabfloor = Instantiate(playerPrefabObject, curFloorSelected.transform.position, Quaternion.Euler(0, 0, 0));
                    Debug.Log("Spanwed Player");
                }
            }
        }
        while (prefabScannerList.Count > 0)
        {
            PrefabScannerTermination(prefabScannerList);
        }
        Destroy(prefabScannerGroup);
    }

    void PrefabScannerTermination(List<GameObject> prefabScannerList)
    {
        for (int i = 0; i < prefabScannerList.Count; i++)
        {
            GameObject curPrefabScanner = prefabScannerList[i];
            Destroy(curPrefabScanner);
            prefabScannerList.RemoveAt(i);
        }
    }

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
        if (inputSound != null)
        {
            inputSound.Play();
        }
    }

    public void PathmakerChanceSliderInput()
    {
        chanceToChange = (int)changeToChangeSlide.value;
        string changeNum = changeToChangeSlide.value.ToString();
        //Debug.Log(changeNum);
        changeToChangeInput.text = changeNum;
        if (inputSound != null)
        {
            inputSound.Play();
        }
    }

    public void PathmakerChanceIntInput(string input)
    {
        chanceToChange = int.Parse(input);
        changeToChangeSlide.value = Mathf.Clamp(float.Parse(input), 0, 100);
        if (inputSound != null)
        {
            inputSound.Play();
        }
    }

    public void PathmakerStartTrigger()
    {
        gameObject.SetActive(true);
        if (buttonSound != null)
        {
            buttonSound.Play();
        }
    }

    public void SceneReset()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    public void ExitGame()
    {
        if (buttonSound != null)
        {
            buttonSound.Play();
        }
        Application.Quit();
    }

    // WALL GENERATION
    // add a "wall pass" to your proc gen after it generates all the floors
    // 1. raycast out from each floor tile (that'd be 4 raycasts per floor tile, in a square "ring" around each tile?)
    // 2. if the raycast "fails" that means there's empty void there, so then instantiate a Wall tile prefab
    // 3. ... repeat until walls surround your entire floorplan
    // (technically, you will end up raycasting the same spot over and over... but the "proper" way to do this would involve keeping more lists and arrays to track all this data)

    // This is where Walls are created
    IEnumerator CreateWall()
    {
        for (int x = 0; x < gridHandler.GetLength(0) - 1; x++)
        {
            for (int y = 0; y < gridHandler.GetLength(1) - 1; y++)
            {
                if (gridHandler[x, y] == Grid.FLOOR)
                {
                    bool hasCreatedWall = false;

                    //Don't generate collision wall if there are diagonal corner calculated
                    if (gridHandler[x + 1, y] == Grid.EMPTY)
                    {
                        wallDualGridTilemap.DataTilemap.SetTile(new Vector3Int(x + 1, y, 0), floorDualGridTilemap.DataTile);
                        gridHandler[x + 1, y] = Grid.WALL;
                        hasCreatedWall = true;
                    }
                    if (gridHandler[x - 1, y] == Grid.EMPTY)
                    {
                        wallDualGridTilemap.DataTilemap.SetTile(new Vector3Int(x - 1, y, 0), floorDualGridTilemap.DataTile);
                        gridHandler[x - 1, y] = Grid.WALL;
                        hasCreatedWall = true;
                    }
                    if (gridHandler[x, y + 1] == Grid.EMPTY)
                    {
                        wallDualGridTilemap.DataTilemap.SetTile(new Vector3Int(x, y + 1, 0), floorDualGridTilemap.DataTile);
                        gridHandler[x, y + 1] = Grid.WALL;
                        hasCreatedWall = true;
                    }
                    if (gridHandler[x, y - 1] == Grid.EMPTY)
                    {
                        wallDualGridTilemap.DataTilemap.SetTile(new Vector3Int(x, y - 1, 0), floorDualGridTilemap.DataTile);
                        gridHandler[x, y - 1] = Grid.WALL;
                        hasCreatedWall = true;
                    }

                    if (hasCreatedWall)
                    {
                        yield return new WaitForSeconds(waitTime);
                    }
                }
            }
        }

        /*
        while (prefabScannerList.Count > 0)
        {
            PrefabScannerTermination(prefabScannerList);
        }
        Destroy(prefabScannerGroup);
        */
    }

    static void WallSpawner(GameObject wallPrefab, GameObject curFloor, Vector3 curFloorPos, Vector3 direction, Quaternion rotation, float distance, LayerMask floorAndWall, Transform thisTransform, List<AudioSource> wallAudioSourceList)
    {
        if (Physics.Raycast(curFloorPos, direction, distance, floorAndWall))
        {
            Debug.Log("Wall Not Buildable");
        }
        else
        {
            GameObject prefabclone = Instantiate(wallPrefab, curFloorPos, rotation, thisTransform);
            prefabclone.transform.Translate(0, 5, 0);
            Debug.Log("Wall Built");

            int a = Random.Range(0, wallAudioSourceList.Count);
            AudioSource curWallAudioSource = wallAudioSourceList[a];
            curWallAudioSource.Play();
        }
    }
}