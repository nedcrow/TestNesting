using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BoardState{
    Empty,
    Using,
    Impossible
}
public class BoardObject
{
    public int width = 1;
    public int height = 1;
    public BoardState state = BoardState.Empty;
    public bool[,] matrix = null;
}
