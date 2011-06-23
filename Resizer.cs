using UnityEngine;
using System.Collections;

public class Resizer : MonoBehaviour {

	// Use this for initialization
	void Start () {
		Mesh mesh = gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh;
		Vector3 [] vertices = mesh.vertices;
		vertices[0].y = vertices[0].y + 3;
		gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh.vertices = vertices;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
