using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;


public class DragDropSystem : MonoBehaviour {

	public enum Alignment {
		TopLeft, TopCenter, TopRight,
		MiddelLeft, MiddelCenter, MiddelRight,
		BottomLeft, BottomCenter, BottomRight
	};

	public enum RelAlignment {
		DragLeft_DropRight, DragRight_DropLeft, 
		DragTop_DropBottom, DragBottom_DropTop
	};

	public enum DragDropSeparation {
		UsePadding, FillScreen 
	};

	public enum DragDropOrigin {
		DragBox, DropBox, None
	};

	public enum DragDropDirection {
		Bidirectional, DragToDropOnly
	};

	public enum DropBehavior {
		MakeRoomIfPossible, DropOnlyIfEmpty
	};

	/********************/
	/* Public variables */
	/********************/
	public string dragDropName = "My DragDrop";		//Instance name. Used to load/save object.
	public bool showDragDrop = true;				//Main flag to show hide drag-drop boxes
	public DragDropDirection direction;				//Set drop direction. Only from drag to drop or bidirectional
	public DropBehavior	dropBehavior;				//Set drop behavior. Drop only on empty slots or make room if possible
	public bool autosave=false;						//Save automatically any changes. Also load on start.

	public string dragTitle="Drag box";				//Drag box title. Leave blank to erase title 
	[Range (1,10)]
	public int dragRows=4;							//Number of drag rows
	[Range (1,15)]
	public int dragCols=3;							//Number of drag columns
	public Texture2D dragEmptySlot;					//Empty slot background texture
	public Texture2D dragFilledSlot;				//Fille slot background texture
	public bool boxedDrag=true;						//Set true to display a surrounding box for the drag box

	public string dropTitle="Drop box";				//Drop box title. Leave blank to erase title 
	[Range (1,10)]
	public int dropRows=4;							//Number of drop rows
	[Range (1,15)]
	public int dropCols=3;							//Number of drop columns
	public Texture2D dropEmptySlot;					//Empty slot background texture
	public Texture2D dropFilledSlot;				//Fille slot background texture
	public bool boxedDrop=true;						//Set true to display a surrounding box for the drop box

	public Alignment globalAlignment;				//Set global drag and drop alignment
	public RelAlignment relativeAlignment;			//Set relative aligment of drag to drop boxes

	public bool showHints=false;					//Flag to display hint box on each item
	public GUISkin skin;							//Tooltip skin

	[Range (0.5f,1.5f)]
	public float iconScale=1.0f;					//Icon scale relative to cell

	public int dragDropPadding=20;					//Separation between drag and drop boxes

	public DragDropSeparation dragDropSeparation;	//Separation mode

	public int cellSize=50;							//Cell size
	public int cellPadding=4;						//Cell padding

	public List<DragDropItem> dragDropItems=new List<DragDropItem>();	//All available items

	/****************************/
	/* Public Trigger Variables */
	/****************************/
	public int originSlot { get; set; }
	public int destinationSlot { get; set; }
	public int droppedItemIndex { get; set; }
	public bool originIsDrag { get; set; }
	public bool destinationIsDrag { get; set; }
	public enum DropErrorCode {
		noError, 		//Drop successfully compelted
		notAllowedArea,	//Try to drop out of drag or drop areas
		notAllowedBox,	//Try to drop in the drag area but no Bidirectional direction set
		notAllowedSlot,	//Try to drop in a filled slot but no MakeRoomfPossible behavior set
		noRoomForDrop,	//Try to drop in a completely filled drop area so cannot make any room
	};
	public DropErrorCode dropErrorCode { get; set; }


	/*********************/
	/* Private variables */
	/*********************/
	private int[] dragSlots=new int[150];			//Drag slots
	private int[] dropSlots=new int[150];			//Drop slots
	private bool draggingItem = false;				//Dragging flag
	private int draggedSlotIndex = -1;				//Dragged slot index
	private int draggedIndex = -1;					//Dragged item index
	private DragDropOrigin itemOrigin;				//Dragged item origin

	/******************/
	/* Hint variables */
	/******************/
	private bool showTooltip=false;
	private string tooltip;
	private float deltaYTooltip;
	private float tooltipWidth=200f;
	private float tooltipHeight=200f;

	/************************/
	/* Calculated variables */
	/************************/
	private Rect totalRect = new Rect ();			//Calculated total rect containing drag and drop boxes
	private Rect dragRect = new Rect ();			//Calculated drag box rect
	private Rect dropRect = new Rect ();			//Calculated drop box rect
	private float realDragDropSeparation;			//Calculated drag and drop separation
	private float dragTitleSize=0f;					//Calculated drag title size
	private float dropTitleSize=0f;					//Calculated drop title size


	/*****************/
	/* Functionality */
	/*****************/

	//Public function to get drag slots
	public int[] getDragSlots() {
		return dragSlots;
	}

	//Public function to get drop slots
	public int[] getDropSlots() {
		return dropSlots;
	}

	void OnGUI() {
		//If main flag is not set, return
		if(!showDragDrop)
			return;

		tooltip = "";
		GUI.skin = skin;

		//Calculate all box sizes
		CalculateSizes ();

		//Draw drag box if needed
		if(boxedDrag)
			GUI.Box (dragRect, dragTitle);
		//Draw drop box if needed
		if(boxedDrop)
			GUI.Box (dropRect, dropTitle);

		//Draw and manage drag and drop icons
		DrawDragDropIcons ();

		if (showTooltip)
			DrawTooltip();
	}

	//Draw generic cell for drag or drop. Used when dragging items
	void drawCell(DragDropOrigin origin, Rect cellPos, Rect iconPos, int Index) {
		if (origin == DragDropOrigin.DragBox)
			drawDragCell (cellPos, iconPos, Index);
		if (origin == DragDropOrigin.DropBox)
			drawDropCell (cellPos, iconPos, Index);
	}

	//Draw drag cell
	void drawDragCell(Rect cellPos, Rect iconPos, int Index) {
		if(dragSlots[Index]!=-1) {
			GUI.DrawTexture(cellPos, dragFilledSlot);
			GUI.DrawTexture(iconPos, dragDropItems[dragSlots[Index]].itemIcon);
		}
		else
			GUI.DrawTexture(cellPos, dragEmptySlot);
	}

	//Draw drop cell
	void drawDropCell(Rect cellPos, Rect iconPos, int Index) {
		if(dropSlots[Index]!=-1) {
			GUI.DrawTexture(cellPos, dropFilledSlot);
			GUI.DrawTexture(iconPos, dragDropItems[dropSlots[Index]].itemIcon);
		}
		else
			GUI.DrawTexture(cellPos, dropEmptySlot);
	}

	//Drop an item in the drag area
	void dropItemInDrag(int index) {
		//Setting trigger variables
		originSlot=draggedSlotIndex;
		destinationSlot=index;
		droppedItemIndex=draggedIndex;

		//If move in same box swap items and return
		if (itemOrigin == DragDropOrigin.DragBox) {
			dragSlots[draggedSlotIndex]=dragSlots[index];
			dragSlots[index]=draggedIndex;

			//Setting trigger variables
			originIsDrag=true;
			destinationIsDrag=true;
			dropErrorCode=DropErrorCode.noError;
			gameObject.SendMessage("DropTrigger", this);

			return;
		}

		//If moves from drop to drag
		originIsDrag=false;
		destinationIsDrag=true;
		if (itemOrigin == DragDropOrigin.DropBox && direction==DragDropDirection.Bidirectional) {
			//If destination is not empty
			if(dragSlots[index]!=-1) {
				//If behavior is move to empty only... returns
				if(dropBehavior==DropBehavior.DropOnlyIfEmpty) {
					//Setting trigger variables
					dropErrorCode=DropErrorCode.notAllowedSlot;
					gameObject.SendMessage("DropTrigger", this);
					return;
				}

				//Look for an empty slot to move destination item
				int destIndex=0;
				for(int i=0; i<dragRows*dragCols; i++) {
					if(dragSlots[i]==-1) {
						destIndex=i;
						break;
					}
				}
				//Oops... no room for drop
				if(dragSlots[destIndex]!=-1) {
					//Setting trigger variables
					dropErrorCode=DropErrorCode.noRoomForDrop;
					gameObject.SendMessage("DropTrigger", this);
					return;
				}

				//Move droped destination to new position
				dragSlots[destIndex]=dragSlots[index];
				//Erase origin in drop area
				dropSlots[draggedSlotIndex]=-1;
				//Put drop in their slot
				dragSlots[index]=draggedIndex;

				//Setting trigger variables
				dropErrorCode=DropErrorCode.noError;
				gameObject.SendMessage("DropTrigger", this);
				return;
			}

			//Simpe drop
			dropSlots[draggedSlotIndex]=-1;
			dragSlots[index]=draggedIndex;

			//Setting trigger variables
			dropErrorCode=DropErrorCode.noError;
			gameObject.SendMessage("DropTrigger", this);
			return;
		}

		//Setting trigger variables. Error no bidirectional
		dropErrorCode=DropErrorCode.notAllowedBox;
		gameObject.SendMessage("DropTrigger", this);
	}

	//Drop an item in the drop area
	void dropItemInDrop(int index) {
		//Setting trigger variables
		originSlot=draggedSlotIndex;
		destinationSlot=index;
		droppedItemIndex=draggedIndex;

		//If move in same box swap items and return
		if (itemOrigin == DragDropOrigin.DropBox) {
			dropSlots[draggedSlotIndex]=dropSlots[index];
			dropSlots[index]=draggedIndex;

			//Setting trigger variables
			originIsDrag=false;
			destinationIsDrag=false;
			dropErrorCode=DropErrorCode.noError;
			gameObject.SendMessage("DropTrigger", this);

			return;
		}

		//If moves from drag to drop
		originIsDrag = true;
		destinationIsDrag=false;
		if (itemOrigin == DragDropOrigin.DragBox) {
			//If destination is not empty
			if(dropSlots[index]!=-1) {
				Debug.Log("Valor "+dropSlots[index]+" index "+index);
				//If behavior is move to empty only... returns
				if(dropBehavior==DropBehavior.DropOnlyIfEmpty) {
					//Setting trigger variables
					dropErrorCode=DropErrorCode.notAllowedSlot;
					gameObject.SendMessage("DropTrigger", this);
					return;
				}
				
				//Look for an empty slot to move destination item
				int destIndex=0;
				for(int i=0; i<dropRows*dropCols; i++) {
					if(dropSlots[i]==-1) {
						destIndex=i;
						break;
					}
				}
				//Oops... no room for drop
				if(dropSlots[destIndex]!=-1) {//Setting trigger variables
					dropErrorCode=DropErrorCode.noRoomForDrop;
					gameObject.SendMessage("DropTrigger", this);
					return;
				}
				
				//Move droped destination to new position
				dropSlots[destIndex]=dropSlots[index];
				//Erase origin
				dragSlots[draggedSlotIndex]=-1;
				//Put drop in their slot
				dropSlots[index]=draggedIndex;

				//Setting trigger variables
				dropErrorCode=DropErrorCode.noError;
				gameObject.SendMessage("DropTrigger", this);
				return;
			}
			
			//Simpe drop
			dragSlots[draggedSlotIndex]=-1;
			dropSlots[index]=draggedIndex;

			//Setting trigger variables
			dropErrorCode=DropErrorCode.noError;
			gameObject.SendMessage("DropTrigger", this);
			return;
		}

		//This point should never be reached
		Debug.Log ("Error in STATE dropping in drop area");
	}

	//Draw and manage drag and drop icons
	void DrawDragDropIcons () {
		Rect cellPos = new Rect ();	//Background cell rect
		cellPos.width = cellSize;
		cellPos.height = cellSize;

		Rect iconPos = new Rect ();	//Icon cell rect
		iconPos.width = cellSize*iconScale;
		iconPos.height = cellSize*iconScale;
		float delta = (cellPos.width - iconPos.width) / 2f;

		for(int i=0; i<dragRows; i++) {
			cellPos.y=dragTitleSize + dragRect.y + cellPadding + (i*(cellSize+cellPadding));
			iconPos.y=cellPos.y+delta;
			for(int j=0; j<dragCols; j++) {
				cellPos.x=dragRect.x+cellPadding+(j*(cellSize+cellPadding));
				iconPos.x=cellPos.x+delta;

				if (cellPos.Contains(Event.current.mousePosition) && Event.current.isMouse && Event.current.type == EventType.mouseUp && draggingItem) {
					dropItemInDrag((i*dragCols)+j);
					if (autosave)
						saveSong(dragDropName);
					draggingItem=false;
					draggedSlotIndex=-1;
					draggedIndex=-1;
					itemOrigin=DragDropOrigin.None;
				}

				//If dragging this item => draw empty and continue
				if(draggedSlotIndex==(i*dragCols)+j && itemOrigin==DragDropOrigin.DragBox) {
					GUI.DrawTexture(cellPos, dragEmptySlot);
					continue;
				}

				//If start dragging set the proper variables
				if(cellPos.Contains(Event.current.mousePosition) && dragSlots[(i*dragCols)+j]!=-1) {
					if(Event.current.isMouse && Event.current.button==0 && Event.current.type == EventType.mouseDrag && !draggingItem) {
						draggingItem=true;
						draggedSlotIndex=(i*dragCols)+j;
						draggedIndex=dragSlots[draggedSlotIndex];
						itemOrigin=DragDropOrigin.DragBox;

						//Trigger variables

					}
				}

				//Draw the drag cell
				drawDragCell(cellPos, iconPos, (i*dragCols)+j);

				if(!draggingItem && cellPos.Contains(Event.current.mousePosition) && dragSlots[(i*dragCols)+j]!=-1 && showHints) {
					tooltip= CreateTooltip(dragDropItems[dragSlots[(i*dragCols)+j]]);
					showTooltip=true;
				}
				if(tooltip=="") {
					showTooltip=false;
				}
			}
		}

		//Goes for drop slots
		for(int i=0; i<dropRows; i++) {
			cellPos.y=dropTitleSize + dropRect.y + cellPadding + (i*(cellSize+cellPadding));
			iconPos.y=cellPos.y+delta;
			for(int j=0; j<dropCols; j++) {
				cellPos.x=dropRect.x+cellPadding+(j*(cellSize+cellPadding));
				iconPos.x=cellPos.x+delta;

				if (cellPos.Contains(Event.current.mousePosition) && Event.current.isMouse && Event.current.type == EventType.mouseUp && draggingItem) {
					dropItemInDrop((i*dropCols)+j);
					if (autosave)
						saveSong(dragDropName);
					draggingItem=false;
					draggedSlotIndex=-1;
					draggedIndex=-1;
					itemOrigin=DragDropOrigin.None;
				}

				//If dragging this item => draw empty and continue
				if(draggedSlotIndex==(i*dropCols)+j && itemOrigin==DragDropOrigin.DropBox) {
					GUI.DrawTexture(cellPos, dropEmptySlot);
					continue;
				}

				//If start dragging set the proper variables
				if(cellPos.Contains(Event.current.mousePosition) && dropSlots[(i*dragCols)+j]!=-1) {
					if(Event.current.isMouse && Event.current.button==0 && Event.current.type == EventType.mouseDrag && !draggingItem) {
						draggingItem=true;
						draggedSlotIndex=(i*dropCols)+j;
						draggedIndex=dropSlots[draggedSlotIndex];
						itemOrigin=DragDropOrigin.DropBox;
					}
				}

				//Draw the drop cell
				drawDropCell(cellPos, iconPos, (i*dropCols)+j);

				if(!draggingItem && cellPos.Contains(Event.current.mousePosition) && dropSlots[(i*dropCols)+j]!=-1 && showHints) {
					tooltip= CreateTooltip(dragDropItems[dropSlots[(i*dropCols)+j]]);
					showTooltip=true;
				}
				if(tooltip=="") {
					showTooltip=false;
				}
			}
		}

		//Draw the dragged itemS
		if (draggingItem) {
			cellPos.x=Event.current.mousePosition.x-(cellSize/2);
			cellPos.y=Event.current.mousePosition.y-(cellSize/2);
			iconPos.x=cellPos.x+delta;
			iconPos.y=cellPos.y+delta;
			drawCell(itemOrigin, cellPos, iconPos, draggedSlotIndex);
		}

		//Mouse up in clear area... stops dragging
		if (Event.current.isMouse && Event.current.type == EventType.mouseUp && draggingItem) {
			//Setting trigger variables
			originSlot=draggedSlotIndex;
			destinationSlot=-1;
			droppedItemIndex=draggedIndex;
			originIsDrag = (itemOrigin==DragDropOrigin.DragBox)?true:false;
			destinationIsDrag=false;
			dropErrorCode=DropErrorCode.notAllowedArea;
			gameObject.SendMessage("DropTrigger", this);

			draggingItem=false;
			draggedSlotIndex=-1;
			draggedIndex=-1;
			itemOrigin=DragDropOrigin.None;
		}
	}

	//Perform all boxes and rects calculations
	void CalculateSizes() {
		dragTitleSize=0f;
		dropTitleSize=0f;
		if(dragTitle.Length>0)
			dragTitleSize=20f;
		if(dropTitle.Length>0)
			dropTitleSize=20f;

		//Drag size
		dragRect.width=(cellSize*dragCols)+(cellPadding * (dragCols+1));
		dragRect.height=(cellSize*dragRows)+(cellPadding * (dragRows+1))+dragTitleSize;

		//Drop size
		dropRect.width=(cellSize*dropCols)+(cellPadding * (dropCols+1));
		dropRect.height=(cellSize*dropRows)+(cellPadding * (dropRows+1))+dropTitleSize;

		//Totasl size
		float hPad = 0f;
		float vPad = 0f;
		if(dragDropSeparation==DragDropSeparation.UsePadding)
			realDragDropSeparation=dragDropPadding;

		if(relativeAlignment==RelAlignment.DragLeft_DropRight || relativeAlignment == RelAlignment.DragRight_DropLeft) {
			if(dragDropSeparation==DragDropSeparation.FillScreen)
				realDragDropSeparation=Screen.width-(dragDropPadding + dragRect.width + dropRect.width + dragDropPadding);
			totalRect.width = dragDropPadding + dragRect.width + realDragDropSeparation + dropRect.width + dragDropPadding;
			totalRect.height = dragDropPadding + Mathf.Max( dragRect.height, dropRect.height) + dragDropPadding;
		}
		else {
			if(dragDropSeparation==DragDropSeparation.FillScreen)
				realDragDropSeparation=Screen.height-(dragDropPadding + dragRect.height + dropRect.height + dragDropPadding);
			totalRect.width = dragDropPadding + Mathf.Max(dragRect.width, dropRect.width) + dragDropPadding;
			totalRect.height = dragDropPadding + dragRect.height + realDragDropSeparation + dropRect.height + dragDropPadding;
		}

		float deltaX=0f, deltaY=0f;
		//Total position
		switch (globalAlignment) {
		case Alignment.TopLeft:
			totalRect.x=0;
			totalRect.y=0;
			deltaX=0;
			deltaY=0;
			deltaYTooltip=0;
			break;
		case Alignment.TopCenter:
			totalRect.x=(Screen.width-totalRect.width)/2;
			totalRect.y=0;
			deltaX=Mathf.Abs(dragRect.width-dropRect.width)/2;
			deltaY=0;
			deltaYTooltip=0;
			break;
		case Alignment.TopRight:
			totalRect.x=Screen.width-totalRect.width;
			totalRect.y=0;
			deltaX=Mathf.Abs(dragRect.width-dropRect.width);
			deltaY=0;
			deltaYTooltip=0;
			break;
		case Alignment.MiddelLeft:
			totalRect.x=0;
			totalRect.y=(Screen.height-totalRect.height)/2;
			deltaX=0;
			deltaY=Mathf.Abs(dragRect.height-dropRect.height)/2;
			deltaYTooltip=-tooltipHeight/2;
			break;
		case Alignment.MiddelCenter:
			totalRect.x=(Screen.width-totalRect.width)/2;
			totalRect.y=(Screen.height-totalRect.height)/2;
			deltaX=Mathf.Abs(dragRect.width-dropRect.width)/2;
			deltaY=Mathf.Abs(dragRect.height-dropRect.height)/2;
			deltaYTooltip=-tooltipHeight/2;
			break;
		case Alignment.MiddelRight:
			totalRect.x=Screen.width-totalRect.width;
			totalRect.y=(Screen.height-totalRect.height)/2;
			deltaX=Mathf.Abs(dragRect.width-dropRect.width);
			deltaY=Mathf.Abs(dragRect.height-dropRect.height)/2;
			deltaYTooltip=-tooltipHeight/2;
			break;
		case Alignment.BottomLeft:
			totalRect.x=0;
			totalRect.y=Screen.height-totalRect.height;
			deltaX=0;
			deltaY=Mathf.Abs(dragRect.height-dropRect.height);
			deltaYTooltip=-tooltipHeight;
			break;
		case Alignment.BottomCenter:
			totalRect.x=(Screen.width-totalRect.width)/2;
			totalRect.y=Screen.height-totalRect.height;
			deltaX=Mathf.Abs(dragRect.width-dropRect.width)/2;
			deltaY=Mathf.Abs(dragRect.height-dropRect.height);
			deltaYTooltip=-tooltipHeight;
			break;
		case Alignment.BottomRight:
			totalRect.x=Screen.width-totalRect.width;
			totalRect.y=Screen.height-totalRect.height;
			deltaX=Mathf.Abs(dragRect.width-dropRect.width);
			deltaY=Mathf.Abs(dragRect.height-dropRect.height);
			deltaYTooltip=-tooltipHeight;
			break;
		}

		//Drag and Drop position
		switch(relativeAlignment) {
		case RelAlignment.DragLeft_DropRight:
			dragRect.x=totalRect.x+dragDropPadding;
			dragRect.y=totalRect.y+dragDropPadding+ ((dragRect.height<dropRect.height)?deltaY:0);
			dropRect.x=totalRect.x+dragDropPadding+dragRect.width+realDragDropSeparation;
			dropRect.y=totalRect.y+dragDropPadding+ ((dragRect.height>dropRect.height)?deltaY:0);
			break;
		case RelAlignment.DragRight_DropLeft:
			dragRect.x=totalRect.x+dragDropPadding+dropRect.width+realDragDropSeparation;
			dragRect.y=totalRect.y+dragDropPadding+ ((dragRect.height<dropRect.height)?deltaY:0);
			dropRect.x=totalRect.x+dragDropPadding;
			dropRect.y=totalRect.y+dragDropPadding+ ((dragRect.height>dropRect.height)?deltaY:0);
			break;		
		case RelAlignment.DragTop_DropBottom:
			dragRect.x=totalRect.x+dragDropPadding + ((dragRect.width<dropRect.width)?deltaX:0);
			dragRect.y=totalRect.y+dragDropPadding;
			dropRect.x=totalRect.x+dragDropPadding + ((dragRect.width>dropRect.width)?deltaX:0);;
			dropRect.y=totalRect.y+dragDropPadding+dragRect.height+realDragDropSeparation;
			break;
		case RelAlignment.DragBottom_DropTop:
			dragRect.x=totalRect.x+dragDropPadding + ((dragRect.width<dropRect.width)?deltaX:0);;
			dragRect.y=totalRect.y+dragDropPadding+dropRect.height+realDragDropSeparation;
			dropRect.x=totalRect.x+dragDropPadding + ((dragRect.width>dropRect.width)?deltaX:0);;
			dropRect.y=totalRect.y+dragDropPadding;
			break;
		}
	}

	void DrawTooltip () {
		tooltipHeight = skin.box.CalcHeight (new GUIContent (tooltip), tooltipWidth);
		GUI.skin.box.wordWrap = true;
		GUI.Box(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y+deltaYTooltip, tooltipWidth, tooltipHeight), tooltip);
	}
	
	string CreateTooltip(DragDropItem item) {
		return "<color=#b8c7ff><b><size=14>"+item.itemLabel+"</size></b></color>\n\n<b>"+item.itemHint+"</b>";
	}

	public void initializeDrag() {
		for (int i=0; i<150; i++) {
			if(i<dragDropItems.Count)
				dragSlots[i]=i;
			else
				dragSlots[i]=-1;
			
			dropSlots[i]=-1;
		}
	}

	// Use this for initialization
	void Start () {
		initializeDrag ();
		if (autosave)
			loadSong (dragDropName);
	}
	
	//Save song to file
	public void saveSong(string name) {
		BinaryFormatter bf = new BinaryFormatter ();
		FileStream file = File.Open (Application.persistentDataPath + "/" + name, FileMode.Create);
		
		allTheData data = new allTheData ();
		for(int i=0; i<150; i++) {
			data.dragSlots[i]=dragSlots[i];
			data.dropSlots[i]=dropSlots[i];
		}		
		bf.Serialize (file, data);
		file.Close ();
	}

	//Load song fro, file
	public void loadSong(string name) {
		if (File.Exists (Application.persistentDataPath + "/" + name)) {
			BinaryFormatter bf = new BinaryFormatter ();
			FileStream file = File.Open (Application.persistentDataPath + "/" + name, FileMode.Open);
			allTheData data = (allTheData)bf.Deserialize(file);
			file.Close();
			for(int i=0; i<150; i++) {
				dragSlots[i]=data.dragSlots[i];
				dropSlots[i]=data.dropSlots[i];
			}
		}
	}

	[Serializable]
	class allTheData {
		public int[] dragSlots=new int[150];
		public int[] dropSlots=new int[150];
	}
}
