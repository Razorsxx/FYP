using StarterAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
//using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
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

    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private GameObject crosshair;
    [SerializeField] private GameObject player;

    private TextMeshPro textObject;

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
    private bool setupComplete = false;
    private bool GameOver = false;
    private bool WhiteTurn = true;
    private void Awake()
    {
        textObject = textObject = GameObject.Find("Text1").GetComponent<TextMeshPro>();
        GenerateAllTile(tileSize, Tile_CountX, Tile_CountY);
        interaction = GameObject.Find("RobotKyle").GetComponent<ThirdPersonController>();
        chessPieces = new ChessPiece[Tile_CountX, Tile_CountY];
        SpawnAllPieces();
        PositionAllPieces();
    }
    private async void Update()
    {
        if (GameOver)
        {
            gameOverUI.SetActive(true);
            crosshair.SetActive(false);
            player.SetActive(true);
            textObject.text = "Well Done\r\nYou Have Completed Every Puzzle";
        }

        if (gameOverUI.activeInHierarchy)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (!cam)
        {
            cam = Camera.main;
            return;
        }

        if (interaction.pickup)
        {
            SpawnLostPiece();
            Debug.Log(count);
            interaction.pickup = false;
        }

        if(count == 1)
        {
            textObject.text = "There is 1 Missing Chess Piece Left.\r\nComplete the Remaining Puzzle To Unlock It.\r\nGood Luck!!";
        }
        if (count == 2 && !setupComplete)
        {
            textObject.text = "You Have Found All the Missing Pieces.\r\nNow Complete The Chess Puzzle To Finish The Game.\r\nWhite To Move:";
            setupComplete = true;
        }

        RaycastHit hit;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray, out hit, 3, LayerMask.GetMask("Tile", "Hover"))) 
        {
            Vector2Int hitPosition = FindTileIndex(hit.transform.gameObject);   //gets the x and y values of the tile hit

            //checks what tile is being hovered after not hovering any tiles and changes it layer to Hover while the mouse is on it
            if(currentPos == -Vector2Int.one)
            {
                currentPos = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
            //if we were already hovering a tile and move to a new tile, changes the previous tile layer to Tile and the new tile to Hover
            if(currentPos != hitPosition)
            {
                tiles[currentPos.x, currentPos.y].layer = LayerMask.NameToLayer("Tile");
                currentPos = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (chessPieces[hitPosition.x, hitPosition.y] != null && count == 2)    //checks where we clicked to see if theres a chess piece in that location
                {   
                    currentPiece = chessPieces[hitPosition.x, hitPosition.y];   //sets our current piece position to be equal to where we clicked
                    if (Return) //checks if return is true if yes sets the return positions equal to the current position of the piece on the board
                    {
                        returnPosX = currentPos.x;
                        returnPosY = currentPos.y;                       
                    }
                }
            }

            if (currentPiece != null && Input.GetMouseButtonUp(0) && count == 2)    //checks to see if there is a piece we are dragging 
            {
                Vector2Int previousPos = new Vector2Int(currentPiece.CurrentX, currentPiece.CurrentY); //saves the previous position of the piece
                //Debug.Log(previousPos.ToString());
                //Debug.Log(returnPosX);
                //Debug.Log(returnPosY);
                if (Return)
                {
                    previousPos = new Vector2Int(returnPosX, returnPosY);
                    Return = false;
                }

                //Debug.Log(previousPos.ToString());

                MoveLocation(currentPiece, hitPosition.x, hitPosition.y);

                if (wrongMove)  //checks if wrong move was made
                {
                    if(WhiteTurn)
                    {
                        textObject.text = "Wrong Move\r\nTry Again!\r\nWhite To Move";
                    }
                    if(!WhiteTurn)
                    {
                        textObject.text = "Wrong Move\r\nTry Again!\r\nBlack To Move";
                    }
                    //places the piece you were dragging into the location you release your mouse
                    currentPiece.transform.position = new Vector3(hitPosition.x * tileSize, yOffset, hitPosition.y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
                    //returns it to its previous location after a 1s delay
                    await Task.Delay(1000);
                    currentPiece.transform.position = new Vector3(previousPos.x * tileSize, yOffset, previousPos.y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
                    chessPieces[hitPosition.x, hitPosition.y] = null;   //sets the piece in the location you released your mouse to null
                    chessPieces[previousPos.x, previousPos.y] = currentPiece;   //sets the piece in the previous location to be equal to the piece we were dragging
                    currentPiece = null;    //sets the current piece we are dragging to be nothing and the wrong move boolean to be false
                    wrongMove = false;                   
                }
                else
                {
                    if (WhiteTurn)
                    {
                        textObject.text = "Well Done That Was The Correct Move\r\nWhite To Move";
                    }
                    if(!WhiteTurn)
                    {
                        textObject.text = "Well Done That Was The Correct Move\r\nBlack To Move";
                    }
                    currentPiece = null;
                }
            }
        }
        else
        {
            //checks if the mouse left the board and sets the previous tile layer to Tile
            if(currentPos != -Vector2Int.one)
            {
                tiles[currentPos.x, currentPos.y].layer = LayerMask.NameToLayer("Tile");
                currentPos = -Vector2Int.one;
            }
            //checks if a piece is dragged outside the board and if so return it back to its original location
            if(currentPiece && Input.GetMouseButtonUp(0) && count == 2)
            {
                currentPiece.transform.position = new Vector3(currentPiece.CurrentX * tileSize, yOffset, currentPiece.CurrentY * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
                currentPiece = null;
            }
        }
        if (currentPiece && Input.GetMouseButton(0) && count == 2)
        {
            Plane horizPlane = new Plane(Vector3.up, Vector3.up * yOffset); //creates a new plane that will be used to show the piece being dragged
            float dist = 0.0f;
            if(horizPlane.Raycast(ray, out dist))
            {
                currentPiece.transform.position = (ray.GetPoint(dist)); //changes the position of the currently dragging piece to be where our mouse is
            }
        }
    }
    private bool MoveLocation(ChessPiece target, int x, int y)
    {
        Vector2Int previousPos = new Vector2Int(currentPiece.CurrentX, currentPiece.CurrentY);
 
        if (chessPieces[x, y] != null)
        {
            ChessPiece other = chessPieces[x, y];

            if (currentPiece.team == other.team)    //checks if the piece we are moving and the piece at the location we moved it to are on the same team
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
        if (moveCount == 0 && currentPiece.CurrentX == 4 && currentPiece.CurrentY == 7 && x == 3 && y == 6)     //checks if its the first move and if the piece we selected and the location are correct
        {
            chessPieces[x, y] = currentPiece;   //sets the piece we are dragging to the position of the place we released our mouse
            chessPieces[previousPos.x, previousPos.y] = null;   //sets the previous position to nothing
            PositionSinglePiece(x, y);  //moves the piece to the location hit
            moveCount++;
            wrongMove = false;
            Return = false;
            WhiteTurn = false;
            return true;
        }

        if (moveCount == 1 && currentPiece.CurrentX == 1 && currentPiece.CurrentY == 6 && x == 3 && y == 6)
        {
            ChessPiece other = chessPieces[x, y];   //sets the variable to be equal to our mouse location
            chessPieces[x, y] = currentPiece;
            chessPieces[previousPos.x, previousPos.y] = null;
            PositionSinglePiece(x, y);
            Destroy(other.gameObject);  //gets rid of the game object that was at the mouse location
            moveCount++;
            Return = false;
            wrongMove = false;
            WhiteTurn = true;
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
            GameOver = true;
            return true;
        }
        else
        {
            //if a wrong move is made set the location of the chess piece where our mouse is to be equal to nothing
            //set our current piece to where the previous location was
            chessPieces[x, y] = null;
            chessPieces[previousPos.x, previousPos.y] = currentPiece;
            PositionSinglePiece(currentPiece.CurrentX, currentPiece.CurrentY);  //move the piece we are dragging back to its original location

            wrongMove = true;
            Return = true;
            
            return false;
        }

        
    }
    
    private void GenerateAllTile(float tileSize, int tileAmountX, int tileAmountY)
    {
        yOffset += transform.position.y;
        bounds = new Vector3((tileAmountX / 2) * tileSize, 0, (tileAmountX / 2) * tileSize) + boardCenter;  //setting the bounds of the board taking into account the center of the board and the tile size
        //setting the size of the field and populating it
        tiles = new GameObject[tileAmountX, tileAmountY];

        for (int i = 0; i < tileAmountX; i++)
        {
            for (int j = 0; j < tileAmountY; j++)
            {
                tiles[i, j] = GenerateTile(tileSize, i, j);
            }
        }
    }
    //function to be called in GenerateAllTile in where we create the individual tiles
    private GameObject GenerateTile(float tileSize, int i, int j)
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", i, j));    //this will generate the gameobject underneath the scene
        tileObject.transform.parent = transform; //this makes it so it scales with the chessboard

        //Adds meshes to the game objects
        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        //create 4 vertices in order to create a square shape taking into account the tilesize and the bounds of the board
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(i * tileSize, yOffset, j * tileSize) - bounds;
        vertices[1] = new Vector3(i * tileSize, yOffset, (j + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((i + 1) * tileSize, yOffset, j * tileSize) - bounds;
        vertices[3] = new Vector3((i + 1) * tileSize, yOffset, (j + 1) * tileSize) - bounds;

        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };    //creates the 2 triangles needed using the vertices above

        //assign triangles and vertices to the mesh
        mesh.vertices = vertices;
        mesh.triangles = tris;

        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");   //set the default layer to Tile

        tileObject.AddComponent<BoxCollider> ();    //add a collider for raycasting

        return tileObject;
    }
    //function to be used in raycasting to find the tile index hit
    private Vector2Int FindTileIndex(GameObject hitObject)
    {
        //using nested for loop to look through each tile and if the object hit correlates to the that tile position return the x and y values
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
    //Function to spawn the missing pieces 
    private void SpawnLostPiece()
    {
        //checks the piece type and spawns it in the on the board and positions it in the correct place
        switch (interaction.pieceType)
        {
            case var _ when interaction.pieceType.Contains("Knight"):
                chessPieces[5, 5] = SpawnPiece(ChessPieceType.Knight, 0);
                PositionSinglePiece(5, 5);
                break;          
            case var _ when interaction.pieceType.Contains("Rook"):
                chessPieces[1, 6] = SpawnPiece(ChessPieceType.Rook, 1);
                PositionSinglePiece(1, 6);
                interaction.pieceType.Remove("Rook");//removes the piece from the list so it doesnt keep spawning the same piece
                count++;
                break;
            case var _ when interaction.pieceType.Contains("Bishop"):
                chessPieces[4, 7] = SpawnPiece(ChessPieceType.Bishop, 0);
                PositionSinglePiece(4, 7);
                interaction.pieceType.Remove("Bishop");
                count++;
                break;
            default:
                break;
        }
    }
    //gets all the pieces needed to spawn and assign it to their locations
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
    //function to position all the pieces in their correct locations
    //uses a nested for loop to check each location to see if not null and calls the function to position that singular piece
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
    //function to be called to position one piece in the correct location
    public void PositionSinglePiece(int x, int y)
    {
        chessPieces[x, y].CurrentX = x;
        chessPieces[x, y].CurrentY = y;
        chessPieces[x, y].transform.position = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);  //moves the piece in the correct location by taking into account the tile center and the bounds of the board
    }
    //function that will be called to spawn the individual pieces in the game world with the correct team and type
    public ChessPiece SpawnPiece(ChessPieceType pieceType, int team)
    {
        ChessPiece chessPiece = Instantiate(prefabs[(int)pieceType - 1], transform).GetComponent<ChessPiece>();

        chessPiece.type = pieceType;
        chessPiece.team = team;
        chessPiece.GetComponent<MeshRenderer>().material = teamMaterial[team];

        return chessPiece;
    }
    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void Quit()
    {
        Application.Quit();
    }
}
