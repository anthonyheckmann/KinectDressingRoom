/*****************************************************************************
*                                                                            *
*  Unity Wrapper                                                             *
*  Copyright (C) 2010 PrimeSense Ltd.                                        *
*                                                                            *
*  This file is part of OpenNI.                                              *
*                                                                            *
*  OpenNI is free software: you can redistribute it and/or modify            *
*  it under the terms of the GNU Lesser General Public License as published  *
*  by the Free Software Foundation, either version 3 of the License, or      *
*  (at your option) any later version.                                       *
*                                                                            *
*  OpenNI is distributed in the hope that it will be useful,                 *
*  but WITHOUT ANY WARRANTY; without even the implied warranty of            *
*  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the              *
*  GNU Lesser General Public License for more details.                       *
*                                                                            *
*  You should have received a copy of the GNU Lesser General Public License  *
*  along with OpenNI. If not, see <http://www.gnu.org/licenses/>.            *
*                                                                            *
*****************************************************************************/
//Author: Shlomo Zippel

using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Threading;

public class NiteWrapper
{
	public enum SkeletonJoint
	{ 
		NONE = 0,
		HEAD = 1,
        NECK = 2,
        TORSO_CENTER = 3,
		WAIST = 4,

		LEFT_COLLAR = 5,
		LEFT_SHOULDER = 6,
        LEFT_ELBOW = 7,
        LEFT_WRIST = 8,
        LEFT_HAND = 9,
        LEFT_FINGERTIP = 10,

        RIGHT_COLLAR = 11,
		RIGHT_SHOULDER = 12,
		RIGHT_ELBOW = 13,
		RIGHT_WRIST = 14,
		RIGHT_HAND = 15,
        RIGHT_FINGERTIP = 16,

        LEFT_HIP = 17,
        LEFT_KNEE = 18,
        LEFT_ANKLE = 19,
        LEFT_FOOT = 20,

        RIGHT_HIP = 21,
		RIGHT_KNEE = 22,
        RIGHT_ANKLE = 23,
		RIGHT_FOOT = 24,

		END 
	};
	
	public enum BodySlice
	{
		LEFT_ARM_UPPER_1 = 0,
		LEFT_ARM_UPPER_2 = 1,
		LEFT_ARM_UPPER_3 = 2,
		
		LEFT_ARM_LOWER_1 = 3,
		LEFT_ARM_LOWER_2 = 4,
		LEFT_ARM_LOWER_3 = 5,
		
		RIGHT_ARM_UPPER_1 = 6,
		RIGHT_ARM_UPPER_2 = 7,
		RIGHT_ARM_UPPER_3 = 8,
		
		RIGHT_ARM_LOWER_1 = 9,
		RIGHT_ARM_LOWER_2 = 10,
		RIGHT_ARM_LOWER_3 = 11,
		
		TORSO_1 = 12,
		TORSO_2 = 13,
		TORSO_3 = 14,
		TORSO_4 = 15,
		
		END
	}

    [StructLayout(LayoutKind.Sequential)]
    public struct SkeletonJointPosition
    {
        public float x, y, z;
        public float confidence;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SkeletonJointOrientation
    {
        public float    m00, m01, m02,
                        m10, m11, m12,
                        m20, m21, m22;
        public float confidence;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct SkeletonJointTransformation
    {
        public SkeletonJointPosition pos;
        public SkeletonJointOrientation ori;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XnVector3D
    {
        public float x, y, z;
    }

	[DllImport("UnityInterface.dll")]
	public static extern uint Init(StringBuilder strXmlPath);
	[DllImport("UnityInterface.dll")]
	public static extern void Update(bool async);
	[DllImport("UnityInterface.dll")]
	public static extern void Shutdown();
	
	[DllImport("UnityInterface.dll")]
	public static extern IntPtr GetStatusString(uint rc);
	[DllImport("UnityInterface.dll")]
	public static extern int GetDepthWidth();
	[DllImport("UnityInterface.dll")]
	public static extern int GetDepthHeight();
	[DllImport("UnityInterface.dll")]
	public static extern IntPtr GetUsersLabelMap();
    [DllImport("UnityInterface.dll")]
    public static extern IntPtr GetUsersDepthMap();
	[DllImport("UnityInterface.dll")]
    public static extern IntPtr getRGB();
    [DllImport("UnityInterface.dll")]
    public static extern int getRGBWidth();
    [DllImport("UnityInterface.dll")]
    public static extern int getRGBHeight();
    [DllImport("UnityInterface.dll")]
	public static extern IntPtr getFatness(int player, int type,ref int size, ref int length, ref double girth); //
    [DllImport("UnityInterface.dll")]
    public static extern double getIntensity(int user);
	
	[DllImport("UnityInterface.dll")]
    public static extern void SetSkeletonSmoothing(double factor);
    [DllImport("UnityInterface.dll")]
    public static extern bool GetJointTransformation(uint userID, SkeletonJoint joint, ref SkeletonJointTransformation pTransformation);

    [DllImport("UnityInterface.dll")]
    public static extern void StartLookingForUsers(IntPtr NewUser, IntPtr CalibrationStarted, IntPtr CalibrationFailed, IntPtr CalibrationSuccess, IntPtr UserLost);
    [DllImport("UnityInterface.dll")]
    public static extern void StopLookingForUsers();
    [DllImport("UnityInterface.dll")]
    public static extern void LoseUsers();
    [DllImport("UnityInterface.dll")]
    public static extern bool GetUserCenterOfMass(uint userID, ref XnVector3D pCenterOfMass);
    [DllImport("UnityInterface.dll")]
    public static extern float GetUserPausePoseProgress(uint userID);

    public delegate void UserDelegate(uint userId);

    public static void StartLookingForUsers(UserDelegate NewUser, UserDelegate CalibrationStarted, UserDelegate CalibrationFailed, UserDelegate CalibrationSuccess, UserDelegate UserLost)
    {
        StartLookingForUsers(
            Marshal.GetFunctionPointerForDelegate(NewUser),
            Marshal.GetFunctionPointerForDelegate(CalibrationStarted),
            Marshal.GetFunctionPointerForDelegate(CalibrationFailed),
            Marshal.GetFunctionPointerForDelegate(CalibrationSuccess),
            Marshal.GetFunctionPointerForDelegate(UserLost));
    }
}

/* NiteGUI
 * Displays the depthmap during callibration and shows the callibration prompts
 */
public class NiteGUI {
	
	private NiteController niteController;
//	public CLNUIDevice kinectDevice;

	Texture2D usersLblTex;
    Color[] usersMapColors;
    Rect usersMapRect;
    int usersMapSize;
    short[] usersLabelMap;
    short[] usersDepthMap;
    float[] usersHistogramMap;
	
	int RGBwidth, RGBheight;
	short [] image;
	Texture2D usersImageTex;
	Color [] usersImageColors;
	Rect usersImageRect;
		
	GUITexture bg;		
//	Thread depth_Thread;
//	Thread image_Thread;
//	short startAngle;
	
	//0 = looking, 1 = calibrating, 2 = calibrated
	public int state;
	public Texture2D stateIcon;
	
	public NiteGUI(NiteController niteController) {	
		//depth_Thread = new Thread(new ThreadStart(this.updateusermap));
		//image_Thread = new Thread(new ThreadStart(this.updatergbimage));
		//depth_Thread.Start();	
		//image_Thread.Start();	
		this.niteController = niteController;
		
//		// Setting the Kinect in certain angle
//		this.startAngle = 2000;
//		this.kinectDevice = new CLNUIDevice(startAngle);
//		
		
		// Init depth & label map related stuff
        usersMapSize = NiteWrapper.GetDepthWidth() * NiteWrapper.GetDepthHeight();
        usersLblTex = new Texture2D(NiteWrapper.GetDepthWidth(), NiteWrapper.GetDepthHeight());
        usersMapColors = new Color[usersMapSize];
        usersMapRect = new Rect(Screen.width - usersLblTex.width / 2, Screen.height - usersLblTex.height / 2, usersLblTex.width / 2, usersLblTex.height / 2);
        usersLabelMap = new short[usersMapSize];
        usersDepthMap = new short[usersMapSize];
        usersHistogramMap = new float[5000];
		
		// Init camera image stuff
		RGBwidth = NiteWrapper.getRGBWidth();
		RGBheight = NiteWrapper.getRGBHeight();
		image = new short[RGBwidth*RGBheight*3];
		usersImageTex = new Texture2D(RGBwidth, RGBheight);
		usersImageColors = new Color[usersMapSize];
		usersImageRect = new Rect(Screen.width - usersImageTex.width / 2, Screen.height - usersImageTex.height / 2, usersImageTex.width / 2, usersImageTex.height / 2);
		
		bg = GameObject.Find("Background Image").GetComponent<GUITexture>();	
		bg.pixelInset = new Rect(-Screen.width / 2, -Screen.height / 2, Screen.width, Screen.height);
		
		stateIcon = Resources.Load("lookingUser") as Texture2D;
	}
	
//	private void updateusermap() {	
//		while(true) 
//		{
//			depth_Thread.Suspend();
//			if(!niteController.kinectConnect){
//				continue;
//			}
//			
//			// copy over the maps
//	        Marshal.Copy(NiteWrapper.GetUsersLabelMap(), usersLabelMap, 0, usersMapSize);
//	        Marshal.Copy(NiteWrapper.GetUsersDepthMap(), usersDepthMap, 0, usersMapSize);
//	
//	        // we will be flipping the texture as we convert label map to color array
//	        int flipIndex, i;
//	        int numOfPoints = 0;
//			Array.Clear(usersHistogramMap, 0, usersHistogramMap.Length);
//	
//	        // calculate cumulative histogram for depth
//	        for (i = 0; i < usersMapSize; i++)
//	        {
//	            // only calculate for depth that contains users
//	            if (usersLabelMap[i] != 0)
//	            {
//	                usersHistogramMap[usersDepthMap[i]]++;
//	                numOfPoints++;
//	            }
//	        }
//	        if (numOfPoints > 0)
//	        {
//	            for (i = 1; i < usersHistogramMap.Length; i++)
//		        {   
//			        usersHistogramMap[i] += usersHistogramMap[i-1];
//		        }
//	            for (i = 0; i < usersHistogramMap.Length; i++)
//		        {
//	                usersHistogramMap[i] = 1.0f - (usersHistogramMap[i] / numOfPoints);
//		        }
//	        }
//	
//	        // create the actual users texture based on label map and depth histogram
//	        for (i = 0; i < usersMapSize; i++)
//	        {
//	            flipIndex = usersMapSize - i - 1;
//	            if (usersLabelMap[i] == 0)
//	            {
//	                usersMapColors[flipIndex] = Color.clear;
//	            }
//	            else
//	            {
//	                // create a blending color based on the depth histogram
//	                Color c = new Color(usersHistogramMap[usersDepthMap[i]], usersHistogramMap[usersDepthMap[i]], usersHistogramMap[usersDepthMap[i]], 0.9f);
//	                switch (usersLabelMap[i] % 4)
//	                {
//	                    case 0:
//	                        usersMapColors[flipIndex] = Color.red * c;
//	                        break;
//	                    case 1:
//	                        usersMapColors[flipIndex] = Color.green * c;
//	                        break;
//	                    case 2:
//	                        usersMapColors[flipIndex] = Color.blue * c;
//	                        break;
//	                    case 3:
//	                        usersMapColors[flipIndex] = Color.magenta * c;
//	                        break;
//	                }
//	            }
//	        }		
//		}
//	}
	
	public void UpdateUserMap() {	
		//depth_Thread.Resume();
		
			if(!niteController.kinectConnect){
				return;
			}
			
			// copy over the maps
	        Marshal.Copy(NiteWrapper.GetUsersLabelMap(), usersLabelMap, 0, usersMapSize);
	        Marshal.Copy(NiteWrapper.GetUsersDepthMap(), usersDepthMap, 0, usersMapSize);
	
	        // we will be flipping the texture as we convert label map to color array
	        int flipIndex, i;
	        int numOfPoints = 0;
			Array.Clear(usersHistogramMap, 0, usersHistogramMap.Length);
	
	        // calculate cumulative histogram for depth
	        for (i = 0; i < usersMapSize; i++)
	        {
	            // only calculate for depth that contains users
	            if (usersLabelMap[i] != 0)
	            {
	                usersHistogramMap[usersDepthMap[i]]++;
	                numOfPoints++;
	            }
	        }
	        if (numOfPoints > 0)
	        {
	            for (i = 1; i < usersHistogramMap.Length; i++)
		        {   
			        usersHistogramMap[i] += usersHistogramMap[i-1];
		        }
	            for (i = 0; i < usersHistogramMap.Length; i++)
		        {
	                usersHistogramMap[i] = 1.0f - (usersHistogramMap[i] / numOfPoints);
		        }
	        }
	
	        // create the actual users texture based on label map and depth histogram
	        for (i = 0; i < usersMapSize; i++)
	        {
	            flipIndex = usersMapSize - i - 1;
	            if (usersLabelMap[i] == 0)
	            {
	                usersMapColors[flipIndex] = Color.clear;
	            }
	            else
	            {
	                // create a blending color based on the depth histogram
	                Color c = new Color(usersHistogramMap[usersDepthMap[i]], usersHistogramMap[usersDepthMap[i]], usersHistogramMap[usersDepthMap[i]], 0.9f);
	                switch (usersLabelMap[i] % 4)
	                {
	                    case 0:
	                        usersMapColors[flipIndex] = Color.red * c;
	                        break;
	                    case 1:
	                        usersMapColors[flipIndex] = Color.green * c;
	                        break;
	                    case 2:
	                        usersMapColors[flipIndex] = Color.blue * c;
	                        break;
	                    case 3:
	                        usersMapColors[flipIndex] = Color.magenta * c;
	                        break;
	                }
	            }
	        }		

        usersLblTex.SetPixels(usersMapColors);
        usersLblTex.Apply();
	}
	
//	public void updatergbimage() {
//		while(true) 
//		{
//			image_Thread.Suspend();
//			Marshal.Copy(NiteWrapper.getRGB(), image, 0, RGBwidth*RGBheight*3);
//			int p = 0;
//			int flipIndex;
//			
//			int usersMapSize = 640*480;		  
//			
//			for (int i = 0; i < usersMapSize; i++) {
//				flipIndex = usersMapSize - i - 1;
//				Color c = new Color((float)image[p++]/255f,(float)image[p++]/255f,(float)image[p++]/255f);
//				usersImageColors[flipIndex] = c;
//	        }
//		}
//	}
	double[] intensity = new double[10];
	int p_intensity = 0;
	double total_intensity = 0;
	
	public void DrawStateIndicator() {
		GUI.DrawTexture(new Rect(Screen.width - 150,0, 150, 100), stateIcon);
	}
	
	public void UpdateRgbImage() {
//		if (image_Thread.ThreadState == ThreadState.Suspended) {
//			image_Thread.Resume();
//		}
		
		Marshal.Copy(NiteWrapper.getRGB(), image, 0, RGBwidth*RGBheight*3);
		int p = 0;
		int flipIndex;
		
		int usersMapSize = 640*480;		  
		
		for (int i = 0; i < usersMapSize; i++) {
			flipIndex = usersMapSize - i - 1;
			Color c = new Color((float)image[p++]/255f,(float)image[p++]/255f,(float)image[p++]/255f);
			usersImageColors[flipIndex] = c;
        }
		
		usersImageTex.SetPixels(usersImageColors);
        usersImageTex.Apply();
		bg.texture = usersImageTex;
		
		double I = NiteWrapper.getIntensity(niteController.getCurrentUser());	
		total_intensity -= intensity[p_intensity];
		total_intensity += I;
		intensity[p_intensity] = I;
		p_intensity = (p_intensity > 8)? 0 : p_intensity+1;
		
		Light light = GameObject.Find("Point light").GetComponent<Light>();	
		light.intensity = 1*(float)(total_intensity/10.0);
		niteController.strIntensity = "user = " + niteController.getCurrentUser() +  ", intensity = " + I;
	}
	
	public void DrawUserMap() {
		GUI.DrawTexture(usersMapRect, usersLblTex);
	}
	
	public void DrawCameraImage() {
		GUI.DrawTexture(usersImageRect, usersImageTex);
	}
}

public struct Rig {
	public GameObject riggedObject;
	public Dictionary<NiteWrapper.SkeletonJoint, Transform> jointMapping;
	public Dictionary<NiteWrapper.SkeletonJoint, Quaternion> referenceOrientation;
	public Transform torsoCenter, neck, leftShoulder, leftElbow, rightShoulder, rightElbow, leftHip, leftKnee, rightHip, rightKnee;
}

/* NiteController
 * A single user controller which takes care of the callbacks and
 * calibration.
 * Adapted from Nite.cs which came with the UnityWrapper example project
 */
public class NiteController {
	
	//Is Kinect initialized and user calibrated?
	public bool kinectConnect = false;
	public bool calibratedUser = false;
	
	//The bodyslice depth maps and realworld diameter (in m), measured during calibtration
	public int[][] sizeData = new int[16][];
	public float[] diameter = new float[16];
	
	//Id of calibrated user, nonsense if calibratedUser is false
	private uint calibratedUserId;
	
	//GUI for calibration process
	private NiteGUI gui;
	
	//Confidence threshold for returning joint pos/ori (a.t.m. Nite seems to only use 0 and 1)
	public float confidenceThreshold = 0.5F;
	
	//Nite callback functions
	private NiteWrapper.UserDelegate NewUser;
    private NiteWrapper.UserDelegate CalibrationStarted;
    private NiteWrapper.UserDelegate CalibrationFailed;
    private NiteWrapper.UserDelegate CalibrationSuccess;
    private NiteWrapper.UserDelegate UserLost;
	
	Thread scan_Thread;
	String clothSize = "";
	public String strIntensity = "";
	
	//Body rigs that are controlled by this controller, have to be registered
	private List<Rig> registeredRigs;
	
	//Callback for when a new user has been calibrated
	public delegate void NewUserCallback();
	private NewUserCallback onNewUser;

	public NiteController(NewUserCallback onNewUser) {
		// initialize the Kinect	
		uint rc = NiteWrapper.Init(new StringBuilder(".\\OpenNI.xml"));
        if (rc != 0)
        {
            Debug.Log(String.Format("Error initializing OpenNI: {0}", Marshal.PtrToStringAnsi(NiteWrapper.GetStatusString(rc))));
			return;
        }
		else kinectConnect = true;
		
		// init user callbacks
        NewUser = new NiteWrapper.UserDelegate(OnNewUser);
        CalibrationStarted = new NiteWrapper.UserDelegate(OnCalibrationStarted);
        CalibrationFailed = new NiteWrapper.UserDelegate(OnCalibrationFailed);
        CalibrationSuccess = new NiteWrapper.UserDelegate(OnCalibrationSuccess);
        UserLost = new NiteWrapper.UserDelegate(OnUserLost);

        // Start looking	
		NiteWrapper.StartLookingForUsers(NewUser, CalibrationStarted, CalibrationFailed, CalibrationSuccess, UserLost);
		Debug.Log("Waiting for users to calibrate");
		
		// set default smoothing
		NiteWrapper.SetSkeletonSmoothing(0.0);
		
		// set new user callback
		this.onNewUser = onNewUser;
		
		// initialize gui
		gui = new NiteGUI(this);
		
		// initialize list of rigs to animate
		registeredRigs = new List<Rig>();
	}
	
	void OnNewUser(uint UserId)
    {
        Debug.Log(String.Format("[{0}] New user", UserId));
		gui.stateIcon = Resources.Load("looking") as Texture2D;
    }   

    void OnCalibrationStarted(uint UserId)
    {
		Debug.Log(String.Format("[{0}] Calibration started", UserId));
		gui.stateIcon = Resources.Load("calibrating") as Texture2D;
    }

    void OnCalibrationFailed(uint UserId)
    {
        Debug.Log(String.Format("[{0}] Calibration failed", UserId));
    }

    void OnCalibrationSuccess(uint UserId)
    {
        Debug.Log(String.Format("[{0}] Calibration success", UserId));
		calibratedUser = true;
		calibratedUserId = UserId;
		
		//Rotate Kinect so center of mass is in the middle		
		//CLNUIDevice device = gui.kinectDevice;
		//device.RotateToCenter(UserId);
		
		Debug.Log("Stopping to look for users");
		NiteWrapper.StopLookingForUsers();
				
		int size = 0;	
		int length = 0;
		double[] girth = new double[16];
		
		for (int i = 0; i < 16; i++) {
			IntPtr raw_data = NiteWrapper.getFatness((int)UserId,i, ref size, ref length, ref girth[i]);
			int[] data = new int[size];	
			Marshal.Copy(raw_data, data, 0, size);
			sizeData[i] = data;
			diameter[i] = (float)length/1000.0F; //convert from mm to m
		}
		
		double[] chest_g = {girth[12], girth[13], girth[14], girth[15]};
		bool done = false;
		while(!done) {
			done = true;
			for (int i = 1; i < 4; i++) {
				if (chest_g[i-1] > chest_g[i]) {
					double temp = chest_g[i];
					chest_g[i] = chest_g[i-1];
					chest_g[i-1] = temp;
					done = false;
				}
			}
		}
						
		double ChestGirth = chest_g[0]/10;	
		
		if (ChestGirth < 70)
			clothSize = ChestGirth + " is too small!";
		else if (ChestGirth < 78)
			clothSize = "XXS : extra extra small (" + ChestGirth + ")";
		else if (ChestGirth < 86)
			clothSize = "XS : extra small (" + ChestGirth + ")";
		else if (ChestGirth < 94)
			clothSize = "S : small (" + ChestGirth + ")";
		else if (ChestGirth < 102)
			clothSize = "M : medium (" + ChestGirth + ")";
		else if (ChestGirth < 110)
			clothSize = "L : large (" + ChestGirth + ")";
		else if (ChestGirth < 118)
			clothSize = "XL : extra large (" + ChestGirth + ")";
		else if (ChestGirth < 129)
			clothSize = "XXL : extra extra large (" + ChestGirth + ")";
		else if (ChestGirth < 141)
			clothSize = "3XL : extra extra extra large (" + ChestGirth + ")";
		else 
			clothSize = "too large! (" + ChestGirth + ")";
		
		Debug.Log(clothSize);		
		
		this.onNewUser();
		
		gui.stateIcon = Resources.Load("calibrated") as Texture2D;
    }

    void OnUserLost(uint UserId)
    {
        Debug.Log(String.Format("[{0}] User lost", UserId));
		calibratedUser = false;
		
		Debug.Log("Starting to look for users");
		NiteWrapper.StartLookingForUsers(NewUser, CalibrationStarted, CalibrationFailed, CalibrationSuccess, UserLost);
		
		gui.stateIcon = Resources.Load("lookingUser") as Texture2D;
    }
	
	public bool GetJointOrientation(NiteWrapper.SkeletonJoint joint, out Quaternion rotation) {
		if (kinectConnect && calibratedUser) {
			NiteWrapper.SkeletonJointTransformation trans = new NiteWrapper.SkeletonJointTransformation();
        	NiteWrapper.GetJointTransformation(calibratedUserId, joint, ref trans);
			
            Vector3 worldZVec = new Vector3(trans.ori.m02, trans.ori.m12, trans.ori.m22);
            Vector3 worldYVec = new Vector3(trans.ori.m01, trans.ori.m11, trans.ori.m21);
            rotation = Quaternion.LookRotation(worldZVec, worldYVec);
			return (trans.ori.confidence > confidenceThreshold);
		}
		else {
			rotation = Quaternion.identity;
			return false;
		}
	}
	
	public bool GetJointPosition(NiteWrapper.SkeletonJoint joint, out Vector3 position) {
		if (kinectConnect && calibratedUser) {
			NiteWrapper.SkeletonJointTransformation trans = new NiteWrapper.SkeletonJointTransformation();
        	NiteWrapper.GetJointTransformation(calibratedUserId, joint, ref trans);
			
			// Nite gives position in mm convert to Unity unit = meters
			position = new Vector3(trans.pos.x/1000.0F, trans.pos.y/1000.0F, trans.pos.z/1000.0F);
			return (trans.pos.confidence > confidenceThreshold);
		}
		else {
			position = new Vector3(0.0F, 0.0F, 0.0F);
			return false;
		}
	}
	
	public void Update() {
		//update the skeleton, depth-, and rgb image
		NiteWrapper.Update(true);
		if (!calibratedUser) {
			//gui.UpdateUserMap();
		}
		gui.UpdateRgbImage();
		
		List<Rig> destroyedRigs = new List<Rig>();
		
		//update the rigs controlled by this controller
		foreach (Rig rig in registeredRigs) {
		         
		    Vector3 position;
			Quaternion rotation;
			
			// Check if rig still exists:
			if (rig.riggedObject != null) {
				
				// Update body location
				if (GetJointPosition(NiteWrapper.SkeletonJoint.TORSO_CENTER, out position)) {
					rig.jointMapping[NiteWrapper.SkeletonJoint.TORSO_CENTER].position = position;
				}
				
				// Update the rotation for each joint
				foreach (KeyValuePair<NiteWrapper.SkeletonJoint, Transform> pair in rig.jointMapping) {
					if (GetJointOrientation(pair.Key, out rotation)) {
					    pair.Value.rotation = rotation * rig.referenceOrientation[pair.Key];
					}
				}
				
				// Update joint positions
				foreach (KeyValuePair<NiteWrapper.SkeletonJoint, Transform> pair in rig.jointMapping) {
					if (GetJointPosition(pair.Key, out position)) {
					    pair.Value.position = position;
					}
				}
			} else {
				destroyedRigs.Add(rig);
			}
		}
		foreach (Rig rig in destroyedRigs) {
			registeredRigs.Remove(rig);	
		}
	}
	
	// finds the correct bones and maps them to the joints
	// returns true if register is succesful
	//(if not, check if your transforms have the correct names and are children of the riggedObject)
	public bool RegisterRig(GameObject riggedObject) {
		// initialize the new rig
		Rig rig = new Rig();
		rig.riggedObject = riggedObject;
		
		// find all the body joint transforms
		rig.torsoCenter = riggedObject.transform.Find("Torso_Center");
		rig.neck = riggedObject.transform.Find("Torso_Center/Neck");
		rig.leftShoulder = riggedObject.transform.Find("Torso_Center/Left_Shoulder");
		rig.leftElbow = riggedObject.transform.Find("Torso_Center/Left_Shoulder/Left_Elbow");
		rig.rightShoulder = riggedObject.transform.Find("Torso_Center/Right_Shoulder");
		rig.rightElbow = riggedObject.transform.Find("Torso_Center/Right_Shoulder/Right_Elbow");
		rig.leftHip = riggedObject.transform.Find("Torso_Center/Left_Hip");
		rig.leftKnee = riggedObject.transform.Find("Torso_Center/Left_Hip/Left_Knee");
		rig.rightHip = riggedObject.transform.Find("Torso_Center/Right_Hip");
		rig.rightKnee = riggedObject.transform.Find("Torso_Center/Right_Hip/Right_Knee");
		
		// map the transforms to the Nite joints
		rig.jointMapping = new Dictionary<NiteWrapper.SkeletonJoint, Transform>();
		rig.jointMapping.Add(NiteWrapper.SkeletonJoint.NECK, rig.neck);
		rig.jointMapping.Add(NiteWrapper.SkeletonJoint.LEFT_SHOULDER, rig.leftShoulder);
		rig.jointMapping.Add(NiteWrapper.SkeletonJoint.LEFT_ELBOW, rig.leftElbow);
		rig.jointMapping.Add(NiteWrapper.SkeletonJoint.RIGHT_SHOULDER, rig.rightShoulder);
		rig.jointMapping.Add(NiteWrapper.SkeletonJoint.RIGHT_ELBOW, rig.rightElbow);
		rig.jointMapping.Add(NiteWrapper.SkeletonJoint.TORSO_CENTER, rig.torsoCenter);
		rig.jointMapping.Add(NiteWrapper.SkeletonJoint.LEFT_HIP, rig.leftHip);
		rig.jointMapping.Add(NiteWrapper.SkeletonJoint.LEFT_KNEE, rig.leftKnee);
		rig.jointMapping.Add(NiteWrapper.SkeletonJoint.RIGHT_HIP, rig.rightHip);
		rig.jointMapping.Add(NiteWrapper.SkeletonJoint.RIGHT_KNEE, rig.rightKnee);
		
		// the orientation to take as origin for the joints
		rig.referenceOrientation = new Dictionary<NiteWrapper.SkeletonJoint, Quaternion>();
		
		// check if all the transforms have been found and set their reference orientation
		bool succes = true;
		foreach (KeyValuePair<NiteWrapper.SkeletonJoint, Transform> pair in rig.jointMapping) {
			if (pair.Value == null) {
				succes = false;
				break;
			} else {
				rig.referenceOrientation.Add(pair.Key, pair.Value.rotation);
			}
		}
		
		if (succes) {
			registeredRigs.Add(rig);
		}
		
		return succes;
	}
	
	public void UpdateGUI () {
//		if (clothSize != "") {
//			GUI.Box(new Rect(100,100,300,200), strIntensity);
//		}
		if (!calibratedUser) {
			//gui.DrawUserMap();
		}
		//gui.DrawCameraImage();
		gui.DrawStateIndicator();
	}
			
	public int getCurrentUser() {
		return (int)calibratedUserId;				
	}
}