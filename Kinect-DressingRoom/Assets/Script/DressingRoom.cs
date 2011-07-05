/* DressingRoom top-level script.
 * 
 * This script assumes that the GameObject to which it is attached contains all
 * the needed objects as children. These are:
 *  - "User Body" the top level 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DressingRoomGUI {
	
	int MENU_START_x = 50;
	int MENU_START_y = 50;
	int BUTTON_HEIGHT = 100;
	int BUTTON_WIDTH = 200;
	int BUTTON_HOR_GAP = 100;
	
	private class ClothingButton {
		public string label;
		public float currentSelectionTime = 0.0F;
		
		private int pos_x, pos_y, width, height;
		
		public ClothingButton (string label, int x, int y, int w, int h) {
			this.label = label;
			this.pos_x = x;
			this.pos_y = y;
			this.width = w;
			this.height = h;
		}
		
		public void Draw() {
			GUI.Box (new Rect (pos_x,pos_y,width,height), label);
		}
		
		public bool Hover(Vector3 pos) {
			return (pos.x > pos_x && pos.x < pos_x + width && pos.y > pos_y && pos.y < pos_y + height);
		}
	}
	
	// hover time needed to select a button
	public float selectionThreshold = 1.0F;
	
	// true if user has finished selecting one or more of the buttons
	public bool userEvent = false;
	private List<ClothingButton> currentSelections;
	
	// hand positions
	private Vector3 left, right;
	Texture2D handIcon;
	
	private List<ClothingButton> buttons;
	
	private ClothingManager clothingManager;
	private Camera camera;
	
	public DressingRoomGUI (NiteController niteController) {
		clothingManager = new ClothingManager(niteController);//"ClothesAttachments.xml");
		camera = GameObject.Find("Main Camera").GetComponent<Camera>();
		currentSelections = new List<ClothingButton>();
		
		handIcon = Resources.Load("hand") as Texture2D;
		
		buttons = new List<ClothingButton>();
		int currentY = MENU_START_y;
		for (int i = 0; i < clothingManager.clothing.Length; i++) {
			buttons.Add(new ClothingButton(clothingManager.clothing[i], MENU_START_x, currentY, BUTTON_WIDTH, BUTTON_HEIGHT));
			currentY += BUTTON_HOR_GAP;
		}
	}
	
	public void HandleUserEvent (UserBody userBody) {
		//userBody.pauseUpdate = true;
		//userBody.RotateToInitialPosition();
		foreach (ClothingButton button in currentSelections){
			//clothingManager.RemoveAllClothing();
			clothingManager.ToggleClothing(button.label);
			button.currentSelectionTime = 0.0F;
		}
		currentSelections.Clear();
		//userBody.pauseUpdate = false;
		userEvent = false;
	}
	
	public void UpdateSelection (HandPositions handPositions) {
		left = camera.WorldToScreenPoint(handPositions.left);
		left.y = camera.pixelHeight-left.y;
		right = camera.WorldToScreenPoint(handPositions.right);
		right.y = camera.pixelHeight-right.y;
		
		foreach (ClothingButton button in buttons) {
			if (button.Hover(left) || button.Hover(right)) {
				button.currentSelectionTime += Time.deltaTime;
			} else {
				button.currentSelectionTime = 0.0F;
			}
			
			//check if user hovered long enough to select
			if (button.currentSelectionTime > selectionThreshold) {
				userEvent = true;
				currentSelections.Add(button);
			}
		}
	}
	
	public void UpdateGUI () {
		foreach (ClothingButton button in buttons) {
			button.Draw();
		}
		
		if (left != null) {
			GUI.DrawTexture(new Rect(left.x-0.5F*handIcon.width, left.y-0.5F*handIcon.height, handIcon.width, handIcon.height), handIcon);
		}
		if (right != null) {
			GUI.DrawTexture(new Rect(right.x-0.5F*handIcon.width, right.y-0.5F*handIcon.height, handIcon.width, handIcon.height), handIcon);
		}
	}
}

public class DressingRoom : MonoBehaviour {
	
	private DressingRoomGUI gui;
	private UserBody userBody;
	private NiteController niteController;
	private HandPositions handPositions;
	
	void Start () {
		niteController = new NiteController(new NiteController.NewUserCallback(this.OnNewUser));
		gui = new DressingRoomGUI(niteController);
		userBody = new UserBody();
	}
	
	void Update () {
		niteController.Update(); //update the depth image and skeleton tracker
		if (niteController.calibratedUser) {
			userBody.UpdateBody(niteController);
			UpdateHandPositions();
			gui.UpdateSelection(handPositions);
			if (gui.userEvent) {
				gui.HandleUserEvent(userBody);
			}
		}
	}
	
	private void UpdateHandPositions () {
		Vector3 leftHand, rightHand;
		if (niteController.GetJointPosition(NiteWrapper.SkeletonJoint.LEFT_HAND, out leftHand)) {
			handPositions.left = leftHand;
		}
		if (niteController.GetJointPosition(NiteWrapper.SkeletonJoint.RIGHT_HAND, out rightHand)) {
			handPositions.right = rightHand;
		}
	}
	
	void OnGUI () {
		niteController.UpdateGUI();
		gui.UpdateGUI();
	}
	
	void OnApplicationQuit() {
		NiteWrapper.Shutdown();
	}
	
	public void OnNewUser() {
		userBody.InitBody(niteController);
	}
}
