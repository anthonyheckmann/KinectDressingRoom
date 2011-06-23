using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

public class ClothingManager {
	private string clothingAttachmentsFile;
	
	private List<GameObject> addedClothing = new List<GameObject>();
	
//	public string [] clothing = {"Dress", "Hippy Trousers", "T-Shirt"};
	public string [] clothing = {"Dress", "Cape"};
//	public string [] clothing = {};


	public ClothingManager(string xmlFile) {
		clothingAttachmentsFile = xmlFile;
	}
	
	public List<string> GetAttachers(string clothingLabel) {
		
		List<string> attachers = new List<string>();
		
		XmlReader reader = XmlReader.Create(clothingAttachmentsFile);
		Debug.Log("Created XMLReader");
		while (reader.Read()) {
			if (reader.Name == "clothing" && reader["label"] == clothingLabel) {
				Debug.Log("Found " + reader.Name + " start");
				while (reader.Read()) {
					if (reader.Name == "clothing") {
						Debug.Log("Found " + reader.Name + " end");
						break;
					}
					if (reader.Name == "attachment" && reader.IsStartElement()) {
							string attacher = reader.ReadString();
							attachers.Add(attacher);
							Debug.Log("Adding " + attacher + " to attachers");
					}
				}
				break;
			}
		}
		
		return attachers;
	}		
	
	public void AddClothing(string clothingLabel, UserBody userBody) {
		Vector3 location = userBody.location;
		Debug.Log("Adding " + clothingLabel);
		
		GameObject clothing = Object.Instantiate(Resources.Load(clothingLabel), location, Quaternion.identity) as GameObject;
		
		//Attach clothing
		List<string> attachers = GetAttachers(clothingLabel);
		foreach (string attacher in attachers) {
			Debug.Log("Attaching " + clothingLabel + " to " + attacher);
			
			//make collider to attach to
			GameObject att = new GameObject("Att_"+attacher);
			Transform transform = att.transform;
			
			if (attacher == "Right_Shoulder") {
				Vector3 position = userBody.rightShoulder.transform.position;
				position.y += userBody.rightShoulder.radius + 0.02F;
				transform.position = position;
				transform.parent = userBody.rightShoulder.transform;
				clothing.active = false;
				Vector3 clothingPosition = clothing.transform.position;
				clothingPosition.y = position.y;
				clothing.transform.position = clothingPosition;
				clothing.active = true;
			}
			if (attacher == "Left_Shoulder") {
				Vector3 position = userBody.leftShoulder.transform.position;
				position.y += userBody.leftShoulder.radius + 0.02F;
				transform.position = position;
				transform.parent = userBody.leftShoulder.transform;
				clothing.active = false;
				Vector3 clothingPosition = clothing.transform.position;
				clothingPosition.y = position.y;
				clothing.transform.position = clothingPosition;
				clothing.active = true;
			}
			
			//create collider
			att.AddComponent("SphereCollider");
			SphereCollider attCollider = att.GetComponent<SphereCollider>();
			attCollider.radius = 0.02F;
			
			clothing.GetComponent<InteractiveCloth>().AttachToCollider(attCollider);
		}
		
		//Add clothing to list of worn clothing
		addedClothing.Add(clothing);
		
		Debug.LogError("pause");
		
		
	}
	
	public void RemoveAllClothing() {
		Debug.Log("Removing all clothing");
		
		foreach (GameObject clothing in addedClothing) {
			Object.Destroy(clothing);
		}
		
		addedClothing.Clear();
	}
}

