using System.Collections;
using System.Collections.Generic;
using skner.DualGrid;
using UnityEngine;

public class CursorController : MonoBehaviour
{
    static Pathmaker pathmaker;

    void Update()
    {
        Vector3 mousePos;
        mousePos = Input.mousePosition;
        //Debug.Log("C1: " + mousePos.ToString());
        //mousePos.z = 10;

        mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        //Debug.Log("C2: " + mousePos.ToString());

        Vector3Int tilePos = GetWorldPosTile(mousePos);
        transform.position = tilePos + new Vector3(0.5f, 0.5f, Camera.main.ScreenToWorldPoint(Input.mousePosition).z);

        GameObject pathmakerObject = GameObject.FindGameObjectWithTag("Pathmaker");

        pathmaker = pathmakerObject.GetComponent<Pathmaker>();

        if (Input.GetKeyDown(KeyCode.Q))
        {
            pathmaker.FloorTilePlacement(tilePos);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            pathmaker.WallTilePlacement(tilePos + new Vector3Int(0, -1, 0));
        }
    }

    public static Vector3Int GetWorldPosTile(Vector3 worldPos)
    {
        int xInt = Mathf.FloorToInt(worldPos.x);
        int yInt = Mathf.FloorToInt(worldPos.y);
        return new(xInt, yInt - 1, 0);
    }
}
