using StarterAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
/*public static class Utility
{
    public static void Invoke(this MonoBehaviour mb, Action f, float delay)
    {
        mb.StartCoroutine(InvokeRoutine(f, delay));
    }
    private static IEnumerator InvokeRoutine(System.Action f, float delay)
    {
        yield return new WaitForSeconds(delay);
        f();
    }
}*/

public class ChessBoard : MonoBehaviour
{
    private ThirdPersonController interaction;

    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;

    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterial;

    private ChessPiece[,] chessPieces;
    private ChessPiece currentPiece;
    private const int Tile_CountX = 8;
    private const int Tile_CountY = 8;
    private GameObject[,] tiles;
    private Vector3 bounds;
    private Camera cam;
    private Vector2Int currentPos;
    private int moveCount = 0;
    private bool wrongMove = false;
    private int returnPosX;
    private int returnPosY;
    private bool Return = false;
    private int count = 0;
    private void Awake()
    {
        GenerateAllTile(tileSize, Tile_CountX, Tile_CountY);
        interaction = GameObject.Find("RobotKyle").GetComponent<ThirdPersonController>();
        chessPieces = new ChessPiece[Tile_CountX, Tile_CountY];
        SpawnAllPieces();
        PositionAllPieces();
    }

    private async void Update()
    {       
        if (!cam)
        {
            cam = Camera.main;
            return;
        }

        if(interaction.pickup)
        {
            SpawnLostPiece();
            interaction.pickup = false;
        }

        RaycastHit hit;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray, out hit, 3, LayerMask.GetMask("Tile", "Hover"))) 
        {
            Vector2Int hitPosition = FindTileIndex(hit.transform.gameObject);

            if(currentPos == -Vector2Int.one)
            {
                currentPos = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
            if(currentPos != hitPosition)
            {
                tiles[currentPos.x, currentPos.y].layer = LayerMask.NameToLayer("Tile");
                currentPos = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (chessPieces[hitPosition.x, hitPosition.y] != null && count == 2)
                {   
                    currentPiece = chessPieces[hitPosition.x, hitPosition.y];
                    if (Return)
                    {
                        returnPosX = currentPos.x;
                        returnPosY = currentPos.y;                       
                    }
                }
            }

            if (currentPiece != null && Input.GetMouseButtonUp(0) && count == 2)
            {
                Vector2Int previousPos = new Vector2Int(currentPiece.CurrentX, currentPiece.CurrentY);
                //Debug.Log(previousPos.ToString());
                //Debug.Log(returnPosX);
                //Debug.Log(returnPosY);
                if (Return)
                {
                    previousPos = new Vector2Int(returnPosX, returnPosY);
                    Return = false;
                }

                //Debug.Log(previousPos.ToString());

                bool correctMove = MoveLocation(currentPiece, hitPosition.x, hitPosition.y);

                /*if(!correctMove)
                {
                    currentPiece.transform.position = new Vector3(previousPos.x * tileSize, yOffset, previousPos.y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
                    currentPiece = null;
                }*/
                if (wrongMove)
                {
                    currentPiece.transform.position = new Vector3(hitPosition.x * tileSize, yOffset, hitPosition.y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
                    await Task.Delay(2000);
                    currentPiece.transform.position = new Vector3(previousPos.x * tileSize, yOffset, previousPos.y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
                    chessPieces[hitPosition.x, hitPosition.y] = null;
                    chessPieces[previousPos.x, previousPos.y] = currentPiece;
                    currentPiece = null;
                    wrongMove = false;                   
                }
                else
                {
                    currentPiece = null;
                }
            }
        }
        else
        {
            if(currentPos != -Vector2Int.one)
            {
                tiles[currentPos.x, currentPos.y].layer = LayerMask.NameToLayer("Tile");
                currentPos = -Vector2Int.one;
            }

            if(currentPiece && Input.GetMouseButtonUp(0) && count == 2)
            {
                currentPiece.transform.position = new Vector3(currentPiece.CurrentX * tileSize, yOffset, currentPiece.CurrentY * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
                currentPiece = null;
            }
        }
        if (currentPiece && Input.GetMouseButton(0) && count == 2)
        {
            Plane horizPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float dist = 0.0f;
            if(horizPlane.Raycast(ray, out dist))
            {
                currentPiece.transform.position = (ray.GetPoint(dist));
            }
        }
    }
    private bool MoveLocation(ChessPiece target, int x, int y)
    {
        Vector2Int previousPos = new Vector2Int(currentPiece.CurrentX, currentPiece.CurrentY);
 
        if (chessPieces[x, y] != null)
        {
            ChessPiece other = chessPieces[x, y];

            if (currentPiece.team == other.team)
            {
                return false;
            }
            /*if(other.team == 0)
            {
                Destroy(other.gameObject);
            }
            else
            {
                Destroy(other.gameObject);
            }*/
        }
        if (moveCount == 0 && currentPiece.CurrentX == 4 && currentPiece.CurrentY == 7 && x == 3 && y == 6)
        {
            chessPieces[x, y] = currentPiece;
            chessPieces[previousPos.x, previousPos.y] = null;
            PositionSinglePiece(x, y);
            moveCount++;
            wrongMove = false;
            Return = false;
            return true;
        }

        if (moveCount == 1 && currentPiece.CurrentX == 1 && currentPiece.CurrentY == 6 && x == 3 && y == 6)
        {
            ChessPiece other = chessPieces[x, y];
            chessPieces[x, y] = currentPiece;
            chessPieces[previousPos.x, previousPos.y] = null;
            PositionSinglePiece(x, y);
            Destroy(other.gameObject);
            moveCount++;
            Return = false;
            wrongMove = false;

            return true;
        }
        if (moveCount == 2 && currentPiece.CurrentX == 4 && currentPiece.CurrentY == 4 && x == 4 && y == 7)
        {
            chessPieces[x, y] = currentPiece;
            chessPieces[previousPos.x, previousPos.y] = null;
            PositionSinglePiece(x, y);
            moveCount++;
            Return = false;
            wrongMove = false;
            return true;
        }
        else
        {
            chessPieces[x, y] = null;
            chessPieces[previousPos.x, previousPos.y] = currentPiece;
            PositionSinglePiece(currentPiece.CurrentX, currentPiece.CurrentY);

            wrongMove = true;
            Return = true;
            
            return false;
        }

        
    }
    
    private void GenerateAllTile(float tileSize, int tileAmountX, int tileAmountY)
    {
        yOffset += transform.position.y;
        bounds = new Vector3((tileAmountX / 2) * tileSize, 0, (tileAmountX / 2) * tileSize) + boardCenter;

        tiles = new GameObject[tileAmountX, tileAmountY];

        for (int i = 0; i < tileAmountX; i++)
        {
            for (int j = 0; j < tileAmountY; j++)
            {
                tiles[i, j] = GenerateTile(tileSize, i, j);
            }
        }
    }

    private GameObject GenerateTile(float tileSize, int i, int j)
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", i, j));
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(i * tileSize, yOffset, j * tileSize) - bounds;
        vertices[1] = new Vector3(i * tileSize, yOffset, (j + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((i + 1) * tileSize, yOffset, j * tileSize) - bounds;
        vertices[3] = new Vector3((i + 1) * tileSize, yOffset, (j + 1) * tileSize) - bounds;

        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;

        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");

        tileObject.AddComponent<BoxCollider> ();

        return tileObject;
    }

    private Vector2Int FindTileIndex(GameObject hitObject)
    {
        for(int i = 0; i < Tile_CountX; i++)
        {
            for (int j = 0; j < Tile_CountY; j++)
            {
                if (tiles[i, j] == hitObject)
                {
                    return new Vector2Int (i, j);
                }               
            }
        }
        return -Vector2Int.one;
    }

    private void SpawnLostPiece()
    {       
        switch (interaction.pieceType)
        {
            case var _ when interaction.pieceType.Contains("Knight"):
                chessPieces[5, 5] = SpawnPiece(ChessPieceType.Knight, 0);
                PositionSinglePiece(5, 5);
                break;
            case var _ when interaction.pieceType.Contains("Rook"):
                chessPieces[1, 6] = SpawnPiece(ChessPieceType.Rook, 1);
                PositionSinglePiece(1, 6);
                count++;
                break;
            case var _ when interaction.pieceType.Contains("Bishop"):
                chessPieces[4, 7] = SpawnPiece(ChessPieceType.Bishop, 0);
                PositionSinglePiece(4, 7);
                count++;
                break;
        }

    }

    private void SpawnAllPieces()
    {
        //chessPieces = new ChessPiece[Tile_CountX, Tile_CountY];

        int whiteTeam = 0;
        int blackTeam = 1;

        //white pieces
        //chessPieces[4, 7] = SpawnPiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[4, 4] = SpawnPiece(ChessPieceType.Queen, whiteTeam);
        chessPieces[7, 1] = SpawnPiece(ChessPieceType.King, whiteTeam);
        chessPieces[6, 2] = SpawnPiece(ChessPieceType.Pawn, whiteTeam);
        chessPieces[5, 3] = SpawnPiece(ChessPieceType.Pawn, whiteTeam);
        chessPieces[0, 3] = SpawnPiece(ChessPieceType.Pawn, whiteTeam);
        chessPieces[7, 5] = SpawnPiece(ChessPieceType.Pawn, whiteTeam);

        //black team
        chessPieces[0, 1] = SpawnPiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[1, 3] = SpawnPiece(ChessPieceType.Knight, blackTeam);
        chessPieces[3, 3] = SpawnPiece(ChessPieceType.Rook, blackTeam);
        chessPieces[2, 4] = SpawnPiece(ChessPieceType.Pawn, blackTeam);
        chessPieces[0, 5] = SpawnPiece(ChessPieceType.Pawn, blackTeam);
        chessPieces[1, 5] = SpawnPiece(ChessPieceType.Pawn, blackTeam);
        chessPieces[6, 5] = SpawnPiece(ChessPieceType.Pawn, blackTeam);
        chessPieces[7, 6] = SpawnPiece(ChessPieceType.Pawn, blackTeam);
        //chessPieces[1, 6] = SpawnPiece(ChessPieceType.Rook, blackTeam);
        chessPieces[6, 7] = SpawnPiece(ChessPieceType.King, blackTeam);

    }
    private void PositionAllPieces()
    {
        for (int x = 0; x < Tile_CountX; x++)
        {
            for(int y = 0; y < Tile_CountY; y++)
            {
                if (chessPieces[x, y] != null)
                {
                    PositionSinglePiece(x, y);
                }
            }
        }
    }
    public void PositionSinglePiece(int x, int y)
    {
        chessPieces[x, y].CurrentX = x;
        chessPieces[x, y].CurrentY = y;
        chessPieces[x, y].transform.position = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }
    public ChessPiece SpawnPiece(ChessPieceType pieceType, int team)
    {
        ChessPiece chessPiece = Instantiate(prefabs[(int)pieceType - 1], transform).GetComponent<ChessPiece>();

        chessPiece.type = pieceType;
        chessPiece.team = team;
        chessPiece.GetComponent<MeshRenderer>().material = teamMaterial[team];

        return chessPiece;
    }
}
