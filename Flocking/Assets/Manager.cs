using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour {

    public static Vector3 targetPosition;
    public GameObject targetObjectPrefab;
    GameObject targetObject;

	public int numBirds = 1;
	public GameObject prefabBird;

    public FlyType birdFlyType;

    float basePosY = 20;

    // Use this for initialization
    void Start () {
        baseCamPos = Camera.main.transform.position;
		for (int i = 0; i < numBirds; ++i) {
			GameObject objBird = Instantiate(prefabBird, null);
			objBird.transform.position = Random.insideUnitSphere+new Vector3(0, basePosY, 0);
            objBird.GetComponent<Bird>().flyType = birdFlyType;
		}
        targetPosition = new Vector3(1, basePosY, 1);
        targetObject = Instantiate(targetObjectPrefab, targetPosition, Quaternion.identity);
	}
	
	// Update is called once per frame
	void Update () {
        InputChangeTarget();
        MoveCamera();
	}

    void InputChangeTarget()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 10.0f +  Random.Range(0.0f,10.0f);
            Vector3 pos = Camera.main.ScreenToWorldPoint(mousePos);

            targetPosition = pos;
            targetObject.transform.position = pos;
        }
        float inputY = Input.GetAxis("Vertical");
        camDist -= inputY * 0.1f;
    }

    Vector3 baseCamPos;
    float camDist = 8.0f;
    void MoveCamera()
    {
        Vector3 center = Vector3.zero;
        int num = 0;
        GameObject[] birds = GameObject.FindGameObjectsWithTag("Bird");
        foreach (var bird in birds)
        {
            center += bird.transform.position;
            num++;
        }
        center /= num;
        Camera.main.transform.position = center - Camera.main.transform.forward * camDist;
        Camera.main.transform.LookAt(center);
    }
}
