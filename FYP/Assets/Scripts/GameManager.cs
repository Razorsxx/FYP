using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Linq;
using TMPro;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Transform gameTransform;
    [SerializeField] private Transform piecePrefab;
    [SerializeField] private MazePiece mazePiecePrefab;
    [SerializeField] private int mazeWidth;
    [SerializeField] private int mazeDepth;

    [SerializeField] private int StartX;
    [SerializeField] private int StartZ;
    [SerializeField] private Transform scaler;

    [SerializeField] private GameObject chessPiecePrefab;

    private TextMeshPro textObject;

    private MazePiece[,] mazeGrid;
    public List<Direction> directions = new List<Direction>();

    public ThirdPersonController controller;
    public List<Transform> pieces;
    public int emptyLoc;
    public int size;
    private float gap = 0.01f;

    private bool Stop = false;

    void Start()
    {
        textObject = GameObject.Find("Text2").GetComponent<TextMeshPro>();

        directions = new List<Direction> { Direction.Right, Direction.Left, Direction.Down, Direction.Up };

        mazeGrid = new MazePiece[mazeWidth, mazeDepth];
        for (int x = 0; x < mazeWidth; x++)
        {
            for (int z = 0; z < mazeDepth; z++)
            {
                mazeGrid[x, z] = Instantiate(mazePiecePrefab, new Vector3(x, 0, z), Quaternion.identity, scaler);   //Spawns the maze all the maze cells at a rotation of 0
                mazeGrid[x, z].transform.localPosition = new Vector3(x, 0, z);  //moves the cells to the correct locations
            }
        }

        GenerateMaze(null, mazeGrid[0, 0]); //starts the maze generation from the first cell of the array
        mazeGrid[0, 0].ClearBack(); //makes the entrance to the maze at the first cell
        mazeGrid[mazeWidth / 2, mazeDepth - 1].ClearFront();    //makes the exit of the maze

        pieces = new List<Transform>();
        size = 3;
        CreatePieces(gap);
        controller = GameObject.Find("RobotKyle").GetComponent<ThirdPersonController>();
        Shuffle();      
    }
 
    void Update()
    {
        if (Complete() && Stop == false)
        {
            chessPiecePrefab.gameObject.SetActive(true);
            textObject.text = "Well Done.\nThe Chess Piece Is Located Behind You";
            //Debug.Log("You Win");
            Stop = true;
        }
    }

    private void CreatePieces(float gapThickness)
    {
        float width = 1 / (float)size;
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                Transform piece = Instantiate(piecePrefab, gameTransform);  //instatiate every piece
                pieces.Add(piece);
                piece.localPosition = new Vector3(-1 + (2 * width * col) + width, +1 - (2 * width * row) - width, 0);   //Positions them in a game board starting from -1 going to +1, so it will start from the top left
                piece.localScale = ((2 * width) - gapThickness) * Vector3.one;  //scales the pieces adding a small gap between them
                piece.name = $"{(row * size) + col}";   //give them a name that will later be used to check if the puzzle is complete
                //Make the Bottom Right the empty location and store the location in a variable
                if ((row == size - 1) && (col == size - 1))
                {
                    emptyLoc = (size * size) - 1;
                    piece.gameObject.SetActive(false);
                }
                else
                {
                    Mesh mesh = piece.GetComponent<MeshFilter>().mesh;
                    Vector2[] uv = new Vector2[4];
                    uv[0] = new Vector2(width * col , 1 - (width * (row + 1)));
                    uv[1] = new Vector2(width * (col + 1) , 1 - (width * (row + 1)));
                    uv[2] = new Vector2(width * col , 1 - (width * row));
                    uv[3] = new Vector2(width * (col + 1) , 1 - (width * row));

                    mesh.uv = uv;
                }
            }
        }
    }
    public bool Complete()
    {
        /*bool complet = true;
        if (complet)
        {
            return true;
        }*/
        for (int i = 0; i < pieces.Count; i++)  //iterates through list and checks if they are in the correct position
        {
            if (pieces[i].name != $"{i}")
            {
                return false;
            }
        }
        return true;
    }
    private void Shuffle()
    {
        int count = 0;
        int last = 0;

        while (count < (size * size * size * size))
        {
            int rnd = Random.Range(0, size * size); //get a random location
            if (rnd == last) { continue; }
            last = emptyLoc;
            //Checks the surrouding location to see if it can move there
            if (controller.SwapIsValid(rnd, -size, size))
            {
                count++;
            }
            else if (controller.SwapIsValid(rnd, +size, size))
            {
                count++;
            }
            else if (controller.SwapIsValid(rnd, -1, 0))
            {
                count++;
            }
            else if (controller.SwapIsValid(rnd, +1, size - 1))
            {
                count++;
            }
        }
    }
    //function that randomises the list of directions
    private List<Direction> RandomDirections()
    {
        List<Direction> direc = new List<Direction>(directions);    //make a copy of original list

        List<Direction> rndDirec = new List<Direction>();   //another list to store our randomised directions

        while (direc.Count > 0)
        {
            int rnd = Random.Range(0, direc.Count); //get a random number
            rndDirec.Add(direc[rnd]);   //adds the direciton that correlates to the random number in the original list
            direc.RemoveAt(rnd);    //removes that directon from original list
        }

        return rndDirec;
    }
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }
    private void GenerateMaze(MazePiece previousPiece, MazePiece currentPiece)
    {
        currentPiece.Visit(); //changes the current piece to have been visited
        ClearWall(previousPiece, currentPiece); //checks current piece against previous piece and removes the corresponding wall between them
        

        MazePiece nextPiece;

        do
        {
            nextPiece = GetNextUnvisitedPiece(currentPiece);

            if (nextPiece != null)
            {
                GenerateMaze(currentPiece, nextPiece);  //recursively calling this function while we have unvisited cells
            }
        }while (nextPiece != null);       
    }
    //function that will check the previous cell and the next cell and clear the walls between them
    private void ClearWall(MazePiece previousPiece, MazePiece currentPiece)
    {
        if (previousPiece == null)
        {
            return;
        }
        if (previousPiece.transform.position.x < currentPiece.transform.position.x) //checks the position of previous cell to see if its left of current one, if true it mean it went left to right, so we clear the left wall of current cell and right of the previous
        {
            previousPiece.ClearRight();
            currentPiece.ClearLeft();
        }
        if (previousPiece.transform.position.x > currentPiece.transform.position.x) //checks right to left
        {
            previousPiece.ClearLeft();
            currentPiece.ClearRight();
        }
        if (previousPiece.transform.position.z < currentPiece.transform.position.z) //checks back to front
        {
            previousPiece.ClearFront();
            currentPiece.ClearBack();
        }
        if (previousPiece.transform.position.z > currentPiece.transform.position.z) //checks front to back
        {
            previousPiece.ClearBack();
            currentPiece.ClearFront();
        }
    }
    //funtion that will get the next cell piece to visit
    private MazePiece GetNextUnvisitedPiece(MazePiece currentPiece)
    {
        var unvisitedPiece = GetUnvisitedPiece(currentPiece);

        return unvisitedPiece.FirstOrDefault();
    }
    //function that gets all the unvisited neighbours at random
    private IEnumerable<MazePiece> GetUnvisitedPiece(MazePiece currentPiece)
    {
        int x = (int)currentPiece.transform.localPosition.x;
        int z = (int)currentPiece.transform.localPosition.z;

        List<Direction> rndDirec = RandomDirections();  //gets a list of random directions

        for (int i = 0; i < rndDirec.Count; i++)
        {
            switch (rndDirec[i])
            {
                case Direction.Up:  //up check
                    if (z + 1 < mazeDepth)
                    {
                        var pieceToUp = mazeGrid[x, z + 1];
                        if (pieceToUp.IsVisited == false)
                        {
                            yield return pieceToUp; //returns cell as potential option
                        }
                    }
                    break;
                case Direction.Down:    //down check
                    if (z - 1 >= 0)
                    {
                        var pieceToDown = mazeGrid[x, z - 1];
                        if (pieceToDown.IsVisited == false)
                        {
                            yield return pieceToDown;   //returns cell as potential option
                        }
                    }
                    break;
                case Direction.Right:   //if its to the right checks if the next cell is within the bounds of the grid
                    if (x + 1 < mazeWidth)
                    {
                        var pieceToRight = mazeGrid[x + 1, z];
                        if (pieceToRight.IsVisited == false)    //checks if the cell has been visited
                        {
                            yield return pieceToRight;  //returns cell as potential option
                        }
                    }
                    break;
                case Direction.Left:    //left check
                    if (x - 1 >= 0)
                    {
                        var pieceToLeft = mazeGrid[x - 1, z];
                        if (pieceToLeft.IsVisited == false)
                        {
                            yield return pieceToLeft;   //returns cell as potential option
                        }
                    }
                    break;
            }
        }
    }
}
