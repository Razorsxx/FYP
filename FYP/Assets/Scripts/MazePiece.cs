using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazePiece : MonoBehaviour
{
    [SerializeField]
    private GameObject _leftWall;

    [SerializeField]
    private GameObject _rightWall;

    [SerializeField]
    private GameObject _frontWall;

    [SerializeField]
    private GameObject _backWall;

    public bool IsVisited { get; private set; }

    public void Visit()
    {
        IsVisited = true;
    }
    
    public void ClearLeft()
    {
        _leftWall.SetActive(false);
        _leftWall.GetComponent<BoxCollider>().enabled = false;
    }
    public void ClearRight()
    {
        _rightWall.SetActive(false);
        _rightWall.GetComponent<BoxCollider>().enabled = false;
    }
    public void ClearFront()
    {
        _frontWall.SetActive(false);
        _frontWall.GetComponent<BoxCollider>().enabled = false;
    }
    public void ClearBack()
    {
        _backWall.SetActive(false);
        _backWall.GetComponent<BoxCollider>().enabled = false;
    }
}
