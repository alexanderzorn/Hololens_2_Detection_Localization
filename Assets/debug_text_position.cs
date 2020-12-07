using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class debug_text_position : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth/10, Camera.main.pixelHeight*.9f, 2.5f));
        transform.rotation = Camera.main.transform.rotation;
    }
}
