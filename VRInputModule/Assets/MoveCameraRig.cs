using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCameraRig : MonoBehaviour {
    [SerializeField]
    private float speed = 2;
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetKey(KeyCode.PageUp))
        {
            var pos = transform.position;
            pos.y += 0.05f;
            transform.position = pos;
        }
        else if (Input.GetKey(KeyCode.PageDown))
        {
            var pos = transform.position;
            pos.y -= 0.05f;
            transform.position = pos;
        }
	}

    void LateUpdate()
    {
        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")) * speed * Time.deltaTime;
        transform.Translate(move);
    }
}
