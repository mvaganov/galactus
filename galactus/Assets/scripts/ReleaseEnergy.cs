using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ResourceEater))]
public class ReleaseEnergy : MonoBehaviour {


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ResourceEater eat = GetComponent<ResourceEater>();
			float amnt = eat.mass / (ResourceEater.countReleasesPerSprint * 2);
            if (amnt < 1) amnt = 1;
            eat.Eject(false, amnt, transform, 0);
        }
    }
}
