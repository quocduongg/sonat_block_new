using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hand : MonoBehaviour
{
    // Start is called before the first frame update
    public Camera cam;
    public GameObject hand;
    // Update is called once per frame

    public bool turnOff;
    public bool neverTurnOff = true;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
            turnOff = !turnOff;
        if (turnOff)
        {
            hand.gameObject.SetActive(neverTurnOff);
            return;
        }
        if (Input.GetMouseButtonDown(0))
            hand.gameObject.SetActive(neverTurnOff);
        if (Input.GetMouseButtonUp(0))
            hand.gameObject.SetActive(neverTurnOff);
        
//        if (Input.GetMouseButton(0))
        {
            Vector2 origin =(cam != null ? cam :  Camera.main).ScreenToWorldPoint(Input.mousePosition);
            hand.transform.position = origin;
        }
    }
}
