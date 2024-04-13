using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Linq;

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

    private MazePiece[,] mazeGrid;
    public List<Direction> directions = new List<Direction>();

    public ThirdPersonController controller;
    public List<Transform> pieces;
    public int emptyLoc;
    public int size;

    private bool Stop = false;

    void Start()
    {
        directions = new List<Direction> { Direction.Right, Direction.Left, Direction.Down, Direction.Up };

        mazeGrid = new MazePiece[mazeWidth, mazeDepth];
        for (int x = 0; x < mazeWidth; x++)
        {
            for (int z = 0; z < mazeDepth; z++)
            {
                mazeGrid[x, z] = Instantiate(mazePiecePrefab, new Vector3(x, 0, z), Quaternion.identity, scaler);
                mazeGrid[x, z].transform.localPosition = new Vector3(x, 0, z);
            }
        }

        GenerateMaze(null, mazeGrid[0, 0]);
        mazeGrid[0, 0].ClearBack();
        mazeGrid[mazeWidth / 2, mazeDepth - 1].ClearFront();

        pieces = new List<Transform>();
        size = 3;
        CreatePieces(0.01f);
        controller = GameObject.Find("RobotKyle").GetComponent<ThirdPersonController>();
        Shuffle();      
    }
 
    void Update()
    {
        if (Complete() && Stop == false)
        {
            chessPiecePrefab.gameObject.SetActive(true);
            Debug.Log("You Win");
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
                Transform piece = Instantiate(piecePrefab, gameTransform);
                pieces.Add(piece);
                piece.localPosition = new Vector3(-1 + (2 * width * col) + width, +1 - (2 * width * row) - width, 0);
                piece.localScale = ((2 * width) - gapThickness) * Vector3.one;
                piece.name = $"{(row * size) + col}";

                if ((row == size - 1) && (col == size - 1))
                {
                    emptyLoc = (size * size) - 1;
                    piece.gameObject.SetActive(false);
                }
                else
                {
                    float gap = gapThickness / 2;
                    Mesh mesh = piece.GetComponent<MeshFilter>().mesh;
                    Vector2[] uv = new Vector2[4];
                    uv[0] = new Vector2((width * col) + gap, 1 - ((width * (row + 1)) - gap));
                    uv[1] = new Vector2((width * (col + 1)) - gap, 1 - ((width * (row + 1)) - gap));
                    uv[2] = new Vector2((width * col) + gap, 1 - ((width * row) + gap));
                    uv[3] = new Vector2((width * (col + 1)) - gap, 1 - ((width * row) + gap));

                    mesh.uv = uv;
                }
            }
        }
    }
    public bool Complete()
    {
        bool complet = true;
        if (complet)
        {
            return true;
        }
        for (int i = 0; i < pieces.Count; i++)
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
            int rnd = Random.Range(0, size * size);
            if (rnd == last) { continue; }
            last = emptyLoc;
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

    private List<Direction> RandomDirections()
    {
        List<Direction> direc = new List<Direction>(directions);

        List<Direction> rndDirec = new List<Direction>();

        while (direc.Count > 0)
        {
            int rnd = Random.Range(0, direc.Count);
            rndDirec.Add(direc[rnd]);
            direc.RemoveAt(rnd);
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
        currentPiece.Visit();
        ClearWall(previousPiece, currentPiece);
        

        MazePiece nextPiece;

        do
        {
            nextPiece = GetNextUnvisitedPiece(currentPiece);

            if (nextPiece != null)
            {
                GenerateMaze(currentPiece, nextPiece);
            }
        }while (nextPiece != null);       
    }

    private void ClearWall(MazePiece previousPiece, MazePiece currentPiece)
    {
        if (previousPiece == null)
        {
            return;
        }
        if (previousPiece.transform.position.x < currentPiece.transform.position.x)
        {
            previousPiece.ClearRight();
            currentPiece.ClearLeft();
        }
        if (previousPiece.transform.position.x > currentPiece.transform.position.x)
        {
            previousPiece.ClearLeft();
            currentPiece.ClearRight();
        }
        if (previousPiece.transform.position.z < currentPiece.transform.position.z)
        {
            previousPiece.ClearFront();
            currentPiece.ClearBack();
        }
        if (previousPiece.transform.position.z > currentPiece.transform.position.z)
        {
            previousPiece.ClearBack();
            currentPiece.ClearFront();
        }
    }

    private MazePiece GetNextUnvisitedPiece(MazePiece currentPiece)
    {
        var unvisitedPiece = GetUnvisitedPiece(currentPiece);

        return unvisitedPiece.FirstOrDefault();
    }
    
    private IEnumerable<MazePiece> GetUnvisitedPiece(MazePiece currentPiece)
    {
        int x = (int)currentPiece.transform.localPosition.x;
        int z = (int)currentPiece.transform.localPosition.z;

        List<Direction> rndDirec = RandomDirections();

        for (int i = 0; i < rndDirec.Count; i++)
        {
            switch (rndDirec[i])
            {
                case Direction.Up:
                    if (z + 1 < mazeDepth)
                    {
                        var pieceToUp = mazeGrid[x, z + 1];
                        if (pieceToUp.IsVisited == false)
                        {
                            yield return pieceToUp;
                        }
                    }
                    break;
                case Direction.Down:
                    if (z - 1 >= 0)
                    {
                        var pieceToDown = mazeGrid[x, z - 1];
                        if (pieceToDown.IsVisited == false)
                        {
                            yield return pieceToDown;
                        }
                    }
                    break;
                case Direction.Right:
                    if (x + 1 < mazeWidth)
                    {
                        var pieceToRight = mazeGrid[x + 1, z];
                        if (pieceToRight.IsVisited == false)
                        {
                            yield return pieceToRight;
                        }
                    }
                    break;
                case Direction.Left:
                    if (x - 1 >= 0)
                    {
                        var pieceToLeft = mazeGrid[x - 1, z];
                        if (pieceToLeft.IsVisited == false)
                        {
                            yield return pieceToLeft;
                        }
                    }
                    break;
            }
        }
    }
}
