using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxObject
{
    public int width = 1;
    public int height = 1;
    public int size = 1;

    public void SetSize()
    {
        size = width * height;
    }
}


