using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class seri_vector_3
{
    public float x, y, z;

    public seri_vector_3(Vector3 input)
    {
        x = input[0];
        y = input[1];
        z = input[2];
    }

}