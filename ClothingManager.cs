using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

public class ClothingManager {
	private string clothingAttachmentsFile;
	
	private List<GameObject> addedClothing = new List<GameObject>();
	
//	public string [] clothing = {"Dress", "Hippy Trousers", "T-Shirt"};
	public string [] clothing = {"Dress"};
//	public string [] clothing = {};
	
	private NiteController niteController;
	
	public ClothingManager(NiteController nc) {
		niteController = nc;
	}
	
	public void AddClothing(string clothingLabel) {
		Vector3 location;
		niteController.GetJointPosition(NiteWrapper.SkeletonJoint.TORSO_CENTER, out location);
		Debug.Log("Adding " + clothingLabel);
		
		GameObject clothing = Object.Instantiate(Resources.Load(clothingLabel), location, Quaternion.identity) as GameObject;
		if (niteController.RegisterRig(clothing)) {
			//Add clothing to list of worn clothing
			addedClothing.Add(clothing);
		} else {
			Object.Destroy(clothing);
		}
		
		//Debug.LogError("pause");
	}
	
	public void RemoveAllClothing() {
		Debug.Log("Removing all clothing");
		
		foreach (GameObject clothing in addedClothing) {
			Object.Destroy(clothing);
		}
		
		addedClothing.Clear();
	}
}

