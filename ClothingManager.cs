using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

public class ClothingManager {
	private string clothingAttachmentsFile;
	
	private List<GameObject> addedClothing = new List<GameObject>();
	
//	public string [] clothing = {"Dress", "Hippy Trousers", "T-Shirt"};
//	public string [] clothing = {"Dress", "Pants", "Jacket"};
	public string [] clothing = {"Jacket", "Pants", "Dress"};
	
	private NiteController niteController;
	
	public ClothingManager(NiteController nc) {
		niteController = nc;
	}
	
	public void ToggleClothing(string clothingLabel) {
		Vector3 location;
		niteController.GetJointPosition(NiteWrapper.SkeletonJoint.TORSO_CENTER, out location);
		Debug.Log("Toggling " + clothingLabel);
		
		GameObject clothing = FindActiveClothing(clothingLabel);
		
		if (clothing == null) {
			clothing = Object.Instantiate(Resources.Load(clothingLabel), location, Quaternion.identity) as GameObject;
			clothing.name = clothingLabel;
			if (niteController.RegisterRig(clothing)) {
				//Add clothing to list of worn clothing
				addedClothing.Add(clothing);
			} else {
				Object.Destroy(clothing);
				Debug.LogWarning("Clothing rig not valid: " + clothingLabel);
			}
		} else {
			addedClothing.Remove(clothing);
			Object.Destroy(clothing);
		}
		//Debug.LogError("pause");
	}
	
	public GameObject FindActiveClothing(string label) {
		GameObject targetClothing = null;
		foreach (GameObject clothing in addedClothing) {
			if (clothing.name == label) {
				targetClothing = clothing;
				break;
			}
		}
		return targetClothing;
	}
	
	public void RemoveAllClothing() {
		Debug.Log("Removing all clothing");
		
		foreach (GameObject clothing in addedClothing) {
			Object.Destroy(clothing);
		}
		
		addedClothing.Clear();
	}
}

