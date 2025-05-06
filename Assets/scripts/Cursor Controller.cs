using System.Collections;
using System.Collections.Generic;
using skner.DualGrid;
using UnityEngine;

public class CursorController : MonoBehaviour
{
    public DualGridTilemapModule dualGridTilemap;
    void Update()
    {
        var mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Vector3Int tilePos = GetWorldPosTile(mouseWorldPos);
        transform.position = tilePos + new Vector3(0.5f, 0.5f, -1);

        if (Input.GetMouseButton(0))
        {
            dualGridTilemap.DataTilemap.SetTile(tilePos, dualGridTilemap.DataTile);
        }
    }

    public static Vector3Int GetWorldPosTile(Vector3 worldPos)
    {
        int xInt = Mathf.FloorToInt(worldPos.x);
        int yInt = Mathf.FloorToInt(worldPos.y);
        return new(xInt, yInt, 0);
    }
}
