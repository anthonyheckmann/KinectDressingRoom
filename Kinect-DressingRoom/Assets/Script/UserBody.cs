using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public struct HandPositions{
	public Vector3 left, right; // 3d world positions of hands
	
	public HandPositions (NiteWrapper.SkeletonJointTransformation leftHand, NiteWrapper.SkeletonJointTransformation rightHand) {
		left = new Vector3(leftHand.pos.x, leftHand.pos.y, leftHand.pos.z);
		right = new Vector3(rightHand.pos.x, rightHand.pos.y, rightHand.pos.z);
	}
}

public class BodyJoint {
	
	public Transform transform;
	public float radius;
	
	private GameObject joint;
	private GameObject bone;
	
	public BodyJoint () {
	}
	
	public BodyJoint(string name, Vector3 position, Quaternion rotation, Vector3 direction, float length, float radius) {
		//create joint transform
		joint = new GameObject(name);
		transform = joint.transform;
		transform.position = position;
		transform.rotation = rotation;
		
		//create bone transform
		bone = new GameObject(name + "_Bone");
		bone.transform.parent = transform;
		bone.transform.localPosition = 0.5F * length * direction.normalized;
		bone.transform.localRotation = Quaternion.LookRotation(-Vector3.forward, direction);
		
		
		if (name.Contains("Shoulder") || name.Contains("Elbow")){
			//Only to get the mesh of a capsule
			GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
			MeshFilter meshf = capsule.GetComponent<MeshFilter>();
			
			//create mesh filter
			bone.AddComponent("MeshFilter");
			MeshFilter boneMFilter = bone.GetComponent<MeshFilter>();
			boneMFilter.mesh = meshf.mesh;
			
			GameObject.Destroy(capsule);
			
			bone.AddComponent("MeshRenderer");
			MeshRenderer boneMRenderer = bone.GetComponent<MeshRenderer>();
			boneMRenderer.material = Resources.Load("boneMaterial", typeof(Material)) as Material;
		}
		//convert radius and length to scale
		bone.transform.localScale = new Vector3(2.0F * radius, (length/2.0F) + radius, 2.0F * radius);
		
		//create collider
		//bone.AddComponent("CapsuleCollider");
		//CapsuleCollider boneCollider = bone.GetComponent<CapsuleCollider>();
		//boneCollider.radius = radius;
		//boneCollider.height = length + (2.0F * radius);
		
		//hack for clothe att
		this.radius = radius;
	}
}

public class AdaptableTorso : BodyJoint {
	private GameObject torso;
	private int [] loopIndex = new int[6];
	
	//the actual collider for this object
	private MeshCollider mc;
	
	public float Abs(float x) {
		if (x < 0.0F) return -x;
		else return x;
	}
	
	public AdaptableTorso (Vector3 position, Quaternion rotation, float lengthUpper, float lengthLower, float shoulderRadius) {
		torso = Object.Instantiate(Resources.Load("AdaptableTorso"), position, rotation) as GameObject;
		SkinnedMeshRenderer smr = torso.GetComponentInChildren<SkinnedMeshRenderer>();
		transform = torso.transform;
		
		//find the bones corresponding to each loop
		Transform [] loops = new Transform[6];
		for (int i = 1; i <= 6; i++) {
			Transform t = torso.transform.Find("Loop_"+i.ToString());
			loops[i-1] = t;
		}
		
		//find the index of each bone
		for (int i = 0; i < smr.bones.Length; i++) {
			for (int j = 0; j < loops.Length; j++) {
				if (smr.bones[i] == loops[j]) {
					loopIndex[j] = i;
					break;
				}
			}
		}
		
		smr.gameObject.AddComponent("MeshCollider");
		mc = smr.gameObject.GetComponent<MeshCollider>();
		mc.sharedMesh = smr.sharedMesh;
		mc.convex = true;
//		Object.Destroy(smr);
		
		//adjust the height of each vertex loop (local coordinates)
		Mesh mesh = mc.sharedMesh;
		Vector3 [] vertices = mesh.vertices;
		for (int i = 0; i < vertices.Length; i++) {
			if (Connected(i, loopIndex[0], mesh.boneWeights)){
				vertices[i].y = lengthUpper + shoulderRadius;
			}
			else if (Connected(i, loopIndex[1], mesh.boneWeights)){
				vertices[i].y = 0.5F * lengthUpper;
			}
			else if (Connected(i, loopIndex[2], mesh.boneWeights)){
				vertices[i].y = vertices[i].y;
			}
			else if (Connected(i, loopIndex[3], mesh.boneWeights)){
				vertices[i].y = -0.5F * lengthLower;
			}
			else if (Connected(i, loopIndex[4], mesh.boneWeights)){
				vertices[i].y = -lengthLower;
			}
			else if (Connected(i, loopIndex[5], mesh.boneWeights)){
				vertices[i].y = -lengthLower - 0.1F;
			}
			else {
				Debug.LogWarning("Warning: vertexes outside of loop");	
			}
		}
		
		mesh.vertices = vertices;
		mesh.RecalculateNormals();	
		mesh.RecalculateBounds();
		mc.sharedMesh = null;
		mc.sharedMesh = mesh;
			
	}
	
	public void AdaptToUser(NiteController niteController) {
		Vector3 [] vertices = mc.sharedMesh.vertices;
		Mesh mesh = mc.sharedMesh;
		
		int leftmostDepth, rightmostDepth, depth;

		// set the correct width of each loop
		for (int i = 0; i < vertices.Length; i++) {
			for (int j = 1; j < 5; j++) {
				int sliceIndex = j + (int) NiteWrapper.BodySlice.TORSO_1 - 1;
				if (Connected(i, loopIndex[j], mesh.boneWeights)){
					if (Abs(vertices[i].x - -0.2F) < 0.0001F) {
						vertices[i].x = -0.5F * niteController.diameter[sliceIndex];
					}
					else if (Abs(vertices[i].x - -0.15F) < 0.0001F) {
						vertices[i].x = -0.375F * niteController.diameter[sliceIndex];
						if (vertices[i].z > 0.0F) {
							leftmostDepth = niteController.sizeData[sliceIndex][0];
							depth = niteController.sizeData[sliceIndex][niteController.sizeData[sliceIndex].Length / 8];
							vertices[i].z = -(float)(depth - leftmostDepth) / 1000.0F; 
						}
					}
					else if (Abs(vertices[i].x - 0.0F) < 0.0001F) {
						if (vertices[i].z > 0.0F) {
							leftmostDepth = niteController.sizeData[sliceIndex][0];
							depth = niteController.sizeData[sliceIndex][niteController.sizeData[sliceIndex].Length / 2];
							vertices[i].z = -(float)(depth - leftmostDepth) / 1000.0F; 
						}
					}
					else if (Abs(vertices[i].x - 0.15F) < 0.0001F) {
						vertices[i].x = 0.375F * niteController.diameter[sliceIndex];
						if (vertices[i].z > 0.0F) {
							rightmostDepth = niteController.sizeData[sliceIndex][niteController.sizeData[sliceIndex].Length-1];
							depth = niteController.sizeData[sliceIndex][(niteController.sizeData[sliceIndex].Length / 8)*7];
							vertices[i].z = -(float)(depth - rightmostDepth) / 1000.0F; 
						}
					}
					else if (Abs(vertices[i].x - 0.2F) < 0.0001F) {
						vertices[i].x = 0.5F * niteController.diameter[sliceIndex];
					}
				}
			}
		}
		
		mesh.vertices = vertices;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		
		mc.sharedMesh = null;
		mc.sharedMesh = mesh;
	}
			    
	private bool Connected(int vertex, int boneIndex, BoneWeight [] boneweights) {
		bool connected = ((boneweights[vertex].boneIndex0 == boneIndex && boneweights[vertex].weight0 > 0.0F) ||
		        (boneweights[vertex].boneIndex1 == boneIndex && boneweights[vertex].weight1 > 0.0F) ||
		        (boneweights[vertex].boneIndex2 == boneIndex && boneweights[vertex].weight2 > 0.0F) ||
		        (boneweights[vertex].boneIndex3 == boneIndex && boneweights[vertex].weight3 > 0.0F));
		return connected;
	}
}



public class UserBody {
	// hand positions updated each frame
	public HandPositions handPositions;
	public Vector3 location;
	
	//public atm just for clothing att hack
	public BodyJoint leftShoulder,leftElbow,rightShoulder,rightElbow,leftHip,leftKnee,rightHip,rightKnee,torsoCenter,neck;
	
	// pause update (e.g. for adding clothing)
	public bool pauseUpdate = false;
	
	// root of the body transform hierarchy and joint mappings
	private Transform userBody;
	private Dictionary<NiteWrapper.SkeletonJoint, BodyJoint> skeletonJointMapping;
	public int size = -1;
	
	public void InitBody(NiteController niteController) {
		Debug.Log("Initializing the user body");
		niteController.Update(); // NiteWrapper does not have the new transforms yet
		
		// TODO: get radius
		float upperLegRadius = 0.075F, lowerLegRadius = 0.06F, neckRadius = 0.05F;

		//get positions of left arm parts
		Vector3 leftShoulderPos, leftElbowPos, leftHandPos;
		Quaternion leftShoulderRot, leftElbowRot;
		niteController.GetJointPosition(NiteWrapper.SkeletonJoint.LEFT_SHOULDER, out leftShoulderPos);
		niteController.GetJointPosition(NiteWrapper.SkeletonJoint.LEFT_ELBOW, out leftElbowPos);
		niteController.GetJointPosition(NiteWrapper.SkeletonJoint.LEFT_HAND, out leftHandPos);
		niteController.GetJointOrientation(NiteWrapper.SkeletonJoint.LEFT_SHOULDER, out leftShoulderRot);
		niteController.GetJointOrientation(NiteWrapper.SkeletonJoint.LEFT_ELBOW, out leftElbowRot);
		
		//intialize left arm components
		float leftArmUpperRadius = 0.5F * niteController.diameter[(int)NiteWrapper.BodySlice.LEFT_ARM_UPPER_2];
		float leftArmLowerRadius = 0.5F * niteController.diameter[(int)NiteWrapper.BodySlice.LEFT_ARM_LOWER_2];
		if (leftArmUpperRadius > 0.07F){
			leftArmUpperRadius = 0.07F;
		}
		if (leftArmLowerRadius > 0.06F){
			leftArmLowerRadius = 0.06F;
		}
		leftShoulder = new BodyJoint("Left_Shoulder", leftShoulderPos, leftShoulderRot, Vector3.left, (leftElbowPos - leftShoulderPos).magnitude, leftArmUpperRadius);
		leftElbow = new BodyJoint("Left_Elbow", leftElbowPos, leftElbowRot, Vector3.left, (leftHandPos - leftElbowPos).magnitude, leftArmLowerRadius);
		leftElbow.transform.parent = leftShoulder.transform;
		
		//get positions of right arm parts
		Vector3 rightShoulderPos, rightElbowPos, rightHandPos;
		Quaternion rightShoulderRot, rightElbowRot;
		niteController.GetJointPosition(NiteWrapper.SkeletonJoint.RIGHT_SHOULDER, out rightShoulderPos);
		niteController.GetJointPosition(NiteWrapper.SkeletonJoint.RIGHT_ELBOW, out rightElbowPos);
		niteController.GetJointPosition(NiteWrapper.SkeletonJoint.RIGHT_HAND, out rightHandPos);
		niteController.GetJointOrientation(NiteWrapper.SkeletonJoint.RIGHT_SHOULDER, out rightShoulderRot);
		niteController.GetJointOrientation(NiteWrapper.SkeletonJoint.RIGHT_ELBOW, out rightElbowRot);
		
		//intialize right arm components
		float rightArmUpperRadius = 0.5F * niteController.diameter[(int)NiteWrapper.BodySlice.RIGHT_ARM_UPPER_2];
		float rightArmLowerRadius = 0.5F * niteController.diameter[(int)NiteWrapper.BodySlice.RIGHT_ARM_LOWER_2];
		if (rightArmUpperRadius > 0.07F){
			rightArmUpperRadius = 0.07F;
		}
		if (rightArmLowerRadius > 0.06F){
			rightArmLowerRadius = 0.06F;
		}		
		rightShoulder = new BodyJoint("Right_Shoulder", rightShoulderPos, rightShoulderRot, Vector3.right, (rightElbowPos - rightShoulderPos).magnitude, rightArmUpperRadius);
		rightElbow = new BodyJoint("Right_Elbow", rightElbowPos, rightElbowRot, Vector3.right, (rightHandPos - rightElbowPos).magnitude, rightArmLowerRadius);
		rightElbow.transform.parent = rightShoulder.transform;

		//get positions of left leg parts
		Vector3 leftHipPos, leftKneePos, leftFootPos;
		Quaternion leftHipRot, leftKneeRot;
		niteController.GetJointPosition(NiteWrapper.SkeletonJoint.LEFT_HIP, out leftHipPos);
		niteController.GetJointPosition(NiteWrapper.SkeletonJoint.LEFT_KNEE, out leftKneePos);
		niteController.GetJointPosition(NiteWrapper.SkeletonJoint.LEFT_FOOT, out leftFootPos);
		niteController.GetJointOrientation(NiteWrapper.SkeletonJoint.LEFT_HIP, out leftHipRot);
		niteController.GetJointOrientation(NiteWrapper.SkeletonJoint.LEFT_KNEE, out leftKneeRot);
		
		//intialize left leg components
		leftHip = new BodyJoint("Left_Hip", leftHipPos, leftHipRot, Vector3.down, (leftKneePos - leftHipPos).magnitude, upperLegRadius);
		leftKnee = new BodyJoint("Left_Knee", leftKneePos, leftKneeRot, Vector3.down, (leftFootPos - leftKneePos).magnitude, lowerLegRadius);
		leftKnee.transform.parent = leftHip.transform;
		
		//get positions of right leg parts
		Vector3 rightHipPos, rightKneePos, rightFootPos;
		Quaternion rightHipRot, rightKneeRot;
		niteController.GetJointPosition(NiteWrapper.SkeletonJoint.RIGHT_HIP, out rightHipPos);
		niteController.GetJointPosition(NiteWrapper.SkeletonJoint.RIGHT_KNEE, out rightKneePos);
		niteController.GetJointPosition(NiteWrapper.SkeletonJoint.RIGHT_FOOT, out rightFootPos);
		niteController.GetJointOrientation(NiteWrapper.SkeletonJoint.RIGHT_HIP, out rightHipRot);
		niteController.GetJointOrientation(NiteWrapper.SkeletonJoint.RIGHT_KNEE, out rightKneeRot);
		
		//intialize right leg components
		rightHip = new BodyJoint("Right_Hip", rightHipPos, rightHipRot, Vector3.down, (rightKneePos - rightHipPos).magnitude, upperLegRadius);
		rightKnee = new BodyJoint("Right_Knee", rightKneePos, rightKneeRot, Vector3.down, (rightFootPos - rightKneePos).magnitude, lowerLegRadius);
		rightKnee.transform.parent = rightHip.transform;
		
		//get positions of neck and head
		Vector3 neckPos, headPos;
		Quaternion neckRot;
		niteController.GetJointPosition(NiteWrapper.SkeletonJoint.NECK, out neckPos);
		niteController.GetJointPosition(NiteWrapper.SkeletonJoint.HEAD, out headPos);
		niteController.GetJointOrientation(NiteWrapper.SkeletonJoint.NECK, out neckRot);
		
		//initialize neck joint
		neck = new BodyJoint("Neck", neckPos, neckRot, Vector3.up, (headPos - neckPos).magnitude, neckRadius); 
		
		//get positions of torso
		Vector3 torsoPos;
		Quaternion torsoRot;
		niteController.GetJointPosition(NiteWrapper.SkeletonJoint.TORSO_CENTER, out torsoPos);
		niteController.GetJointOrientation(NiteWrapper.SkeletonJoint.TORSO_CENTER, out torsoRot);
		
		//initialize torso
		float shoulderRadius = (0.5F * niteController.diameter[(int)NiteWrapper.BodySlice.LEFT_ARM_UPPER_1] +
			0.5F * niteController.diameter[(int)NiteWrapper.BodySlice.RIGHT_ARM_UPPER_1]) / 2.0F;
		float upperLength, lowerLength;
		upperLength = (neckPos - torsoPos).magnitude;
		lowerLength = (leftHipPos + 0.5F * (rightHipPos - leftHipPos) - torsoPos).magnitude; //from centerpoint between the two hips to torso_center
		AdaptableTorso torso = new AdaptableTorso(torsoPos, torsoRot, upperLength, lowerLength, shoulderRadius);
		torso.AdaptToUser(niteController);
		torsoCenter = (BodyJoint)torso;
		neck.transform.parent = torsoCenter.transform;
		leftShoulder.transform.parent = torsoCenter.transform;
		rightShoulder.transform.parent = torsoCenter.transform;
		leftHip.transform.parent = torsoCenter.transform;
		rightHip.transform.parent = torsoCenter.transform;
			
		skeletonJointMapping = new Dictionary<NiteWrapper.SkeletonJoint, BodyJoint>();
//		skeletonJointMapping.Add(NiteWrapper.SkeletonJoint.HEAD, new BodyJoint());
		skeletonJointMapping.Add(NiteWrapper.SkeletonJoint.NECK, neck);
		skeletonJointMapping.Add(NiteWrapper.SkeletonJoint.LEFT_SHOULDER, leftShoulder);
		skeletonJointMapping.Add(NiteWrapper.SkeletonJoint.LEFT_ELBOW, leftElbow);
//		skeletonJointMapping.Add(NiteWrapper.SkeletonJoint.LEFT_HAND, new BodyJoint());
		skeletonJointMapping.Add(NiteWrapper.SkeletonJoint.RIGHT_SHOULDER, rightShoulder);
		skeletonJointMapping.Add(NiteWrapper.SkeletonJoint.RIGHT_ELBOW, rightElbow);
//		skeletonJointMapping.Add(NiteWrapper.SkeletonJoint.RIGHT_HAND, new BodyJoint());
		skeletonJointMapping.Add(NiteWrapper.SkeletonJoint.TORSO_CENTER, torsoCenter);
		skeletonJointMapping.Add(NiteWrapper.SkeletonJoint.LEFT_HIP, leftHip);
		skeletonJointMapping.Add(NiteWrapper.SkeletonJoint.LEFT_KNEE, leftKnee);
//		skeletonJointMapping.Add(NiteWrapper.SkeletonJoint.LEFT_FOOT, new BodyJoint());
		skeletonJointMapping.Add(NiteWrapper.SkeletonJoint.RIGHT_HIP, rightHip);
		skeletonJointMapping.Add(NiteWrapper.SkeletonJoint.RIGHT_KNEE, rightKnee);
		size = skeletonJointMapping.Count;
//		skeletonJointMapping.Add(NiteWrapper.SkeletonJoint.RIGHT_FOOT, new BodyJoint());
	}
	
	
//  Not needed, turns out kinect skeleton always puts torso_center in line between hips and shoulders
//	public bool GetLowerTorsoTransformation(NiteController niteController, 
//	                                       out Quaternion rotation, out Vector3 position) {
//		Vector3 positionLeftHip, positionRightHip, positionCenter;
//		bool succes = true;
//		
//		if (!niteController.GetJointPosition(NiteWrapper.SkeletonJoint.LEFT_HIP, out positionLeftHip)) {
//			succes = false;
//		}
//		if (!niteController.GetJointPosition(NiteWrapper.SkeletonJoint.RIGHT_HIP, out positionRightHip)) {
//			succes = false;
//		}
//		if (!niteController.GetJointPosition(NiteWrapper.SkeletonJoint.TORSO_CENTER, out positionCenter)) {
//			succes = false;
//		}
//		
//		if (succes) {
//			position = positionRightHip + (0.5F * (positionLeftHip - positionRightHip));
//			Vector3 forward = positionCenter - position;
//			Vector3 upward = position - positionRightHip;
//			rotation = Quaternion.LookRotation(forward, upward);
//			rotation.eulerAngles = rotation.eulerAngles + new Vector3(0,0,90);
//		}
//		else {
//			position = Vector3.zero;
//			rotation = Quaternion.identity;
//		}
//		
//		return succes;
//	}
	
	public void UpdateBody(NiteController niteController) {
		if (!pauseUpdate && niteController.calibratedUser) {
			Vector3 position;
			Quaternion rotation;
			
			int size = skeletonJointMapping.Count;
			// Update the rotation for each joint
			foreach (KeyValuePair<NiteWrapper.SkeletonJoint, BodyJoint> pair in skeletonJointMapping) {
				if (niteController.GetJointOrientation(pair.Key, out rotation)) {
				    pair.Value.transform.rotation = rotation;
				}
			}
//			
//			// Update joint positions
//			foreach (KeyValuePair<NiteWrapper.SkeletonJoint, BodyJoint> pair in skeletonJointMapping) {
//				if (niteController.GetJointPosition(pair.Key, out position)) {
//				    pair.Value.transform.position = position;
//				}
//			}
//			
			// Update body location
			if (niteController.GetJointPosition(NiteWrapper.SkeletonJoint.TORSO_CENTER, out position)) {
				location = position;
				torsoCenter.transform.position = position;
			}
		}
		
		// Update hand positions, done seperately to make GUI work even if skeleton is wrong
		Vector3 leftHand, rightHand;
		if (niteController.GetJointPosition(NiteWrapper.SkeletonJoint.LEFT_HAND, out leftHand)) {
			handPositions.left = leftHand;
		}
		if (niteController.GetJointPosition(NiteWrapper.SkeletonJoint.RIGHT_HAND, out rightHand)) {
			handPositions.right = rightHand;
		}
	}
	
	public void RotateToInitialPosition () {
		foreach (KeyValuePair<NiteWrapper.SkeletonJoint, BodyJoint> pair in skeletonJointMapping) {
			pair.Value.transform.rotation = Quaternion.identity;
		}
	}
}