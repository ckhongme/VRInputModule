using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour {

    public RectTransform r;
    public Camera cam; 

	void Start ()
    {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
        Vector2 v;

        Vector2 aa = new Vector2(Screen.width / 2f, Screen.height / 2f);
        //aa = new Vector2(cam.rect.width / 2f, cam.rect.height / 2f);


        RectTransformUtility.ScreenPointToLocalPointInRectangle(r, aa, cam, out v);
        Debug.Log(v);
    }

    public void ButtonDown()
    {
        Debug.Log(transform.name + "  " + Time.realtimeSinceStartup);
    }
}
