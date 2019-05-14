using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportAfterFall : MonoBehaviour
{
	Vector3 startPosition;
    // Start is called before the first frame update
    void Start()
    {
		startPosition = transform.position;
    }

	public void RestartPosition() { transform.position = startPosition; }
	public bool OutOfBounds() { return transform.position.y < -10; }

	// Update is called once per frame
	void Update()
    {
        if(OutOfBounds()) { RestartPosition(); }
	}
}
