using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

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

    [SerializeField]
    private int counter = 0;

    //	Declare a public Transform called floorPrefab, assign the prefab in inspector;

    public Transform floorPrefab;

    //	Declare a public Transform called pathmakerSpherePrefab, assign the prefab in inspector; 		// you'll have to make a "pathmakerSphere" prefab later

    public Transform pathmakerSpherePrefab;

    public GameObject pathmakerSpherePrefabObject;

    public GameObject floorPrefabObject;

    public List<GameObject> PathObjects;

    public float waitTime = 0.01f;

    public int maximumAmoundOfPathmaker = 10;

    public int maxPathmakerLife = 5;

    void Start()
    {
        // Counter automatically set its value to 0
        counter = 0;

        // set the place to spawn
        Vector3 spawmPos = new Vector3(0, 0, 0);

        // spawn the first PathMaker
        GameObject myNewPathmaker = Instantiate(pathmakerSpherePrefabObject, spawmPos, Quaternion.Euler(0, 0, 0));

        // add PathMaker to private PathMaker list
        PathObjects.Add(myNewPathmaker);

        StartCoroutine(CentralPathmakerControl());
    }


    // Problem 1: Not setting script per individual Pathmaker prefab will makes error pops like crazy

    // Problem 2: Setting script per individual Pathmaker prefad will make them continuously and limitlessly produce, duplicate and move until the editor lag out

    // Current objective: Setting up Object Array to work around problems above, whilst maintaining movement between each Pathmaker

    // Basing this project based on Walker Generator worked on past days to complete the script. This is where CentralPathMaker control comes in handy.

    // This is Central Pathmaker Control. Where PathMaker sections is truly executed. The mastermind of this script.
    IEnumerator CentralPathmakerControl()
    {
        int maxPathmakerAllowed = maximumAmoundOfPathmaker;

        int designatedDistance = 0;

        while (counter <= 500)
        {
            int numOfPathObject = PathObjects.Count;

            bool hasCreatedFloor = false;

            FloorCreation(hasCreatedFloor = true);

            PathmakerUpdate(designatedDistance++, maxPathmakerAllowed, numOfPathObject);

            PathmakerChance(numOfPathObject, maxPathmakerAllowed);

            if (hasCreatedFloor)
            {
                yield return new WaitForSeconds(waitTime);
            }

        }

        if (counter >= 500)
        {
            SelfDestruct();
        }

        // StartCoroutine(SelfDestruct());
    }

    void PathmakerChance(int numOfPathObject, int maxPathmaker)
    {
        //		If counter is less than 50, then:
        //			Generate a random number from 0.0f to 1.0f;
        //			If random number is less than 0.25f, then rotate myself 90 degrees;
        //				... Else if number is 0.25f-0.5f, then rotate myself -90 degrees;
        //				... Else if number is 0.99f-1.0f, then instantiate a pathmakerSpherePrefab clone at my current position;
        //			// end elseIf
        for (int i = 0; i < numOfPathObject; i++)
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
            else if (random > 0.5f && random <= 0.75f) // 3nd 1/4 chance of nothing happened
            {
                curPathmaker.transform.Rotate(new Vector3(0, 0, 0));
            }
            else if (random > 0.99f && random < 1.0f) // fairly rare chance of duplicating itself.
            {
                PathDuplicator(i, numOfPathObject, maxPathmaker);
            }
            else // if it hits 1.0f
            {
                if (numOfPathObject > 6)
                {
                    //PathDestroyer();
                }
            }

        }
    }
    void PathDuplicator(int i, int CurPathmakerCount, int maxPathmakerAllowDuplicating)
    {
        float chanceToSplit = Random.value;

        Vector3 curPos = PathObjects[i].transform.position;

        Quaternion none = Quaternion.Euler(0, 0, 90);
        Quaternion right = Quaternion.Euler(0, 0, 90);
        Quaternion left = Quaternion.Euler(0, 0, 90);

        if (chanceToSplit <= 0.25f)
        {
            GameObject prefabclone0 = Instantiate(pathmakerSpherePrefabObject, curPos, none);
            PathObjects.Add(prefabclone0);
        }
        else if (chanceToSplit > 0.25f && chanceToSplit <= 0.5f)
        {
            GameObject prefabclone0 = Instantiate(pathmakerSpherePrefabObject, curPos, right);
            PathObjects.Add(prefabclone0);
        }
        else if (chanceToSplit > 0.25f && chanceToSplit <= 0.75f)
        {
            GameObject prefabclone0 = Instantiate(pathmakerSpherePrefabObject, curPos, left);
            PathObjects.Add(prefabclone0);
        }
        else // this is a special case, as it spawns two more other object of its kind 
        {
            GameObject prefabclone0 = Instantiate(pathmakerSpherePrefabObject, curPos, right);
            PathObjects.Add(prefabclone0);
            GameObject prefabclone1 = Instantiate(pathmakerSpherePrefabObject, curPos, left);
            PathObjects.Add(prefabclone1);
        }
    }

    //			Instantiate a floorPrefab clone at current position;

    void FloorCreation(bool v)
    {
        foreach (GameObject curPathmaker in PathObjects)
        {
            Vector3 curPathmakerPos = curPathmaker.transform.position;
            float curPathmakerPosX = curPathmakerPos.x;
            float curPathmakerPosY = curPathmakerPos.y;
            GameObject prefabfloor = Instantiate(floorPrefabObject, curPathmaker.transform.position, Quaternion.Euler(0, 0, 0), this.transform);
            counter++;
        }
    }

    //			Move forward ("forward", as in, the direction I'm currently facing) by 5 units;
    //			Increment counter;


    void PathmakerUpdate(int walkedDistance, int maxPathmaker, int numOfPathObject) // Dictates the Pathmaker to moveforward
    {
        // Local variables
        float moveDistance = 5.0f;

        int updatedPathmakerCount = numOfPathObject;

        for (int i = 0; i < updatedPathmakerCount; i++)
        {
            GameObject walkingPathmaker = PathObjects[i];

            walkingPathmaker.transform.Translate(0, moveDistance, 0);

            List<GameObject> pathObjects = PathObjects;

            int PathmakerLife = maxPathmakerLife;

            PathmakerExpire(walkingPathmaker, walkedDistance++, i, maxPathmaker, numOfPathObject, pathObjects, PathmakerLife);
        }
    }


    //		Else:
    //			Destroy my game object; 		// self destruct if I've made enough tiles already
    void PathDestroyer()
    {
        float random = Random.value;
        for (int i = 0; i < PathObjects.Count; i++)
        {
            if (random < 0.1f && PathObjects.Count > 1)
            {
                PathObjects.RemoveAt(i);
                break;
            }
        }
    }

    IEnumerator SelfDestruct()
    {
        while (PathObjects.Count > 0)
        {
            for (int i = PathObjects.Count; i < 1; i++)
            {
                GameObject curPathmaker = PathObjects[i];
                Destroy(curPathmaker);
                PathObjects.RemoveAt(i);
            }
        }
        if (PathObjects.Count == 0)
        {
            yield return new WaitForSeconds(0.01f);
        }
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
    static void PathmakerExpire(GameObject walkingPathmaker, int walkedDistance, int i, int walkerNeededToExpire, int curPathObjectCount, List<GameObject> pathObjects, int maxPathmakerLife)
    {
        int distanceWalked = walkedDistance;

        int maxLife = maxPathmakerLife;

        Debug.Log(distanceWalked);

        if (distanceWalked > maxLife && curPathObjectCount > walkerNeededToExpire)
        {
            Destroy(walkingPathmaker);
            pathObjects.RemoveAt(i);
        }
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

    // BETTER UI:
    // learn how to use UI Sliders (https://unity3d.com/learn/tutorials/topics/user-interface-ui/ui-slider) 
    // let us tweak various parameters and settings of our tech demo
    // let us click a UI Button to reload the scene, so we don't even need the keyboard anymore.  Throw that thing out!

    // WALL GENERATION
    // add a "wall pass" to your proc gen after it generates all the floors
    // 1. raycast out from each floor tile (that'd be 4 raycasts per floor tile, in a square "ring" around each tile?)
    // 2. if the raycast "fails" that means there's empty void there, so then instantiate a Wall tile prefab
    // 3. ... repeat until walls surround your entire floorplan
    // (technically, you will end up raycasting the same spot over and over... but the "proper" way to do this would involve keeping more lists and arrays to track all this data)
}