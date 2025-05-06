using System.Collections;
using System.Collections.Generic;
using skner.DualGrid;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WalkerGenerator : MonoBehaviour
{
    // Code ain't mine, got it from someone top down level generator code and impletemented it for Duel Grid system

    // Added several comments for distinguisting purposes.

    // Also the "walker" are automatically generated. I know Toby taught us the fish school code but idk how to use it for level generation.

    // Duel Grid Variables

    public DualGridTilemapModule floorDualGridTilemap;

    public DualGridTilemapModule wallDualGridTilemap;

    public enum Grid
    {
        FLOOR,
        WALL,
        EMPTY
    }

    // Variables
    public Grid[,] gridHandler;
    public List<WalkerObject> Walkers;

    //commented out original variable of the code. It uses the classic tile rule which clashes with the Duel Tile one. Now prefers to Duel Grid Variable

    /*
    public Tilemap tileMap;
    public Tile Floor;
    public Tile Wall;
    */

    public int MapWidth = 30;
    public int MapHeight = 30;

    public int MaximumWalkers = 10;
    public int TileCount = default;
    public float FillPercentage = 0.4f;
    public float WaitTime = 0.05f;

    void Start()
    {
        InitializeGrid();
    }

    // Establish the Unity Grid system

    // note: components for the tile generation is temporary hardcoded to be compatible with the duel grid system. Possible need critise.

    void InitializeGrid()
    {
        gridHandler = new Grid[MapWidth, MapHeight];

        for (int x = 0; x < gridHandler.GetLength(0); x++)
        {
            for (int y = 0; y < gridHandler.GetLength(1); y++)
            {
                gridHandler[x, y] = Grid.EMPTY;
            }
        }

        Walkers = new List<WalkerObject>();

        Vector3Int TileCenter = new Vector3Int(gridHandler.GetLength(0) / 2, gridHandler.GetLength(1) / 2, 0);
        
        // this is where initial Walker spawns
        WalkerObject curWalker = new WalkerObject(new Vector2(TileCenter.x, TileCenter.y), GetDirection(), 0.5f);
        gridHandler[TileCenter.x, TileCenter.y] = Grid.FLOOR;
        floorDualGridTilemap.DataTilemap.SetTile(TileCenter, floorDualGridTilemap.DataTile);
        Walkers.Add(curWalker);

        TileCount++;

        StartCoroutine(CreateFloors());
    }

    // Random direction
    Vector2 GetDirection()
    {
        int choice = Mathf.FloorToInt(UnityEngine.Random.value * 3.99f);

        // How walker chose their initial walking direction
        // Dev note: I should do this code instead of array of ifs ngl
        switch (choice)
        {
            case 0:
                return Vector2.down;
            case 1:
                return Vector2.left;
            case 2:
                return Vector2.up;
            case 3:
                return Vector2.right;
            default:
                return Vector2.zero;
        }
    }

    // Floor generation begins here!
    IEnumerator CreateFloors()
    {
        while ((float)TileCount / (float)gridHandler.Length < FillPercentage)
        {
            bool hasCreatedFloor = false;
            foreach (WalkerObject curWalker in Walkers)
            {
                Vector3Int curPos = new Vector3Int((int)curWalker.Position.x, (int)curWalker.Position.y, 0);

                if (gridHandler[curPos.x, curPos.y] != Grid.FLOOR)
                {
                    floorDualGridTilemap.DataTilemap.SetTile(curPos, floorDualGridTilemap.DataTile);
                    TileCount++;
                    gridHandler[curPos.x, curPos.y] = Grid.FLOOR;
                    hasCreatedFloor = true;
                }
            }

            //Walker Methods
            ChanceToRemove();
            ChanceToRedirect();
            ChanceToCreate();
            UpdatePosition();

            if (hasCreatedFloor)
            {
                yield return new WaitForSeconds(WaitTime);
            }
        }

        StartCoroutine(CreateWalls());
    }

    // Chance for the walker to automatically be removed
    void ChanceToRemove()
    {
        int updatedCount = Walkers.Count;
        for (int i = 0; i < updatedCount; i++)
        {
            if (UnityEngine.Random.value < Walkers[i].ChanceToChange && Walkers.Count > 1)
            {
                Walkers.RemoveAt(i);
                break;
            }
        }
    }

    // Chance for the walker to redirect its position
    void ChanceToRedirect()
    {
        for (int i = 0; i < Walkers.Count; i++)
        {
            if (UnityEngine.Random.value < Walkers[i].ChanceToChange)
            {
                WalkerObject curWalker = Walkers[i];
                curWalker.Direction = GetDirection();
                Walkers[i] = curWalker;
            }
        }
    }

    // Chance for the walker to generate new walker
    void ChanceToCreate()
    {
        int updatedCount = Walkers.Count;
        for (int i = 0; i < updatedCount; i++)
        {
            if (UnityEngine.Random.value < Walkers[i].ChanceToChange && Walkers.Count < MaximumWalkers)
            {
                Vector2 newDirection = GetDirection();
                Vector2 newPosition = Walkers[i].Position;

                WalkerObject newWalker = new WalkerObject(newPosition, newDirection, 0.5f);
                Walkers.Add(newWalker);
            }
        }
    }

    // Update position for the walkers
    void UpdatePosition()
    {
        for (int i = 0; i < Walkers.Count; i++)
        {
            WalkerObject FoundWalker = Walkers[i];
            FoundWalker.Position += FoundWalker.Direction;
            FoundWalker.Position.x = Mathf.Clamp(FoundWalker.Position.x, 1, gridHandler.GetLength(0) - 2);
            FoundWalker.Position.y = Mathf.Clamp(FoundWalker.Position.y, 1, gridHandler.GetLength(1) - 2);
            Walkers[i] = FoundWalker;
        }
    }

    // Generating Walls after completing the Floor
    IEnumerator CreateWalls()
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
                        yield return new WaitForSeconds(WaitTime);
                    }
                }
            }
        }
    }
}
