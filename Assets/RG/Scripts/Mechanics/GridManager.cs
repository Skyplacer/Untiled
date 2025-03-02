using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class GridManager : MonoBehaviour
{
    // Mechanic Variables
    public int rows = 6;
    public int columns = 6;
    public float space = 1.5f;
    public GameObject[] tiles;
    public GameObject[,] grid;
    private bool checking = false;
    public float delayTime = 0.2f;
    public bool debug = true;

    // UI variables
    public GameObject levelCompleteUI;
    public Button mainMenuButton;
    public Text completeText; 

    void Start() {
        GenerateGrid();
        CenterCamera();
        levelCompleteUI.SetActive(false); //Makes UI hidden at start
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    void GenerateGrid() {
        grid = new GameObject[rows, columns];

        for(int i = 0; i < rows; i++)
        {
            for(int j = 0; j < columns; j++)
            {
                Vector3 position = new Vector3(j * space, -i * space, 0);
                int random;
                // Prevent immediate 3 match tiles at start
                do{
                    random = Random.Range(0, tiles.Length);
                }
                while(j >=2 &&
                        grid[i, j - 1] != null && grid[i, j - 2] != null &&
                        grid[i, j - 1].tag == tiles[random].tag &&
                        grid[i, j - 2].tag == tiles[random].tag);

                GameObject tile = Instantiate(tiles[random], position, Quaternion.identity);
                grid[i,j] = tile;
            }
        }
    }

    void CenterCamera() {
        float gridWidth = (columns - 1) * space;
        float gridHeight = (rows - 1) * space;
        Vector3 gridCenter = new Vector3(gridWidth / 2, -gridHeight / 2, 0);

        // Adjust camera to fit the entire grid
        float cameraDistance = Mathf.Max(gridWidth, gridHeight) / Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);
        Camera.main.transform.position = new Vector3(gridCenter.x, gridCenter.y, -cameraDistance);
        Camera.main.transform.LookAt(gridCenter);
    }

    IEnumerator DropTile() {

        for(int i = rows - 2; i >= 0; i--) {
            for(int j = 0; j < columns; j++) {
                if(grid[i, j] != null) {
                    int dropRow = i;
                    // Find lowest available slot in column
                    while(dropRow + 1 < rows && grid[dropRow + 1, j] == null) {
                            dropRow++;
                    }
                    if(dropRow != i) {
                        grid[dropRow, j] = grid[i, j];
                        grid[i, j] = null;
                        grid[dropRow, j].transform.position = new Vector3(j * space, -dropRow * space, 0);
                    }
                }
            }
        }

        yield return new WaitForSeconds(delayTime);
        if(debug) {
            Debug.Log("After Drop:");
            PrintGrid();
        }
        checking = true;
    }

    IEnumerator CheckMatch() {
        List<GameObject> tilesToRemove = new List<GameObject>();
        for(int i = 0; i < rows; i++) {
            int count = 1;

            for(int j = 0; j < columns - 1; j++) {
                if(grid[i, j] != null && grid[i, j + 1] != null && grid[i, j].tag == grid[i, j + 1].tag) {
                        count++;
                    }
                    else{   
                        // Remove matched tiles if sequence is 3 or more
                        if(count >= 3) {
                            for(int k = 0; k < count; k++) {
                                if(grid[i, j - k] != null) {
                                    tilesToRemove.Add(grid[i, j - k]);
                                    grid[i, j - k] = null;
                                }
                            }
                        }
                        count = 1;
                    }
                }
                if(count >= 3) {
                    for(int k = 0; k < count; k++) {
                        if(grid[i, columns - 1 - k] != null) {
                            tilesToRemove.Add(grid[i, columns - 1 - k]);
                            grid[i, columns - 1 - k] = null;
                        }
                    }
                }
            int removeCount = tilesToRemove.Count; 
            if(removeCount > 0) {
                for (int t = 0; t < removeCount; t++) {
                    GameObject tile = tilesToRemove[t];

                    if(tile != null) {
                        Renderer tileRender = tile.GetComponent<Renderer>();
                        if(tileRender != null) {
                            tileRender.material.color = Color.red;
                        }
                    }
                }
                yield return new WaitForSeconds(delayTime);
                // Destroy matched tiles
                for(int t = 0; t < removeCount; t++) 
                {
                    GameObject tile = tilesToRemove[t];

                    if(tile != null) {

                        Destroy(tile);
                    }
                }

                tilesToRemove.Clear();
                yield return StartCoroutine(DropTile());
                checking = true;
            }
            yield return new WaitForSeconds(0.3f);
            CheckLevelComplete();
        }
    }

    void CheckLevelComplete() {
        bool isEmpty = true;

        for (int i = 0; i < rows; i++) {
            for (int j = 0; j < columns; j++) {
                if (grid[i, j] != null) {
                    isEmpty = false; // If any tiles exist grid is not yet cleared
                    break;
                }
            }
        }
        if (isEmpty) {
            ShowLevelComplete();
        }
    }

    void ShowLevelComplete() {
        levelCompleteUI.SetActive(true);
    }

    void ReturnToMainMenu() {
        SceneManager.LoadScene("MainMenu");
    }
    
    void PrintGrid() {
        for(int i = 0; i < rows; i++) {
            string row = "";

            for(int j = 0; j < columns; j++) {
                if(grid[i, j] != null) {
                    row += grid[i, j].tag + "";
                }
                else{
                    row += "null";
                }
            }
            Debug.Log(row);
        }
    }

    void Update() {

        if(Input.GetMouseButtonDown(0)) {
            Ray detectClick = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit pressedClick;

            if(Physics.Raycast(detectClick, out pressedClick)) {
                GameObject clickedObject = pressedClick.collider.gameObject;
                // Find clicked tile and remove it
                for(int i = 0; i < rows; i++) {
                    for(int j = 0; j < columns; j++) {
                        if(grid[i, j] == clickedObject) {
                            grid[i, j] = null;
                            Destroy(clickedObject);
                            StartCoroutine(DropTile());
                            return;
                        }
                    }
                }
            }
        }
        if(checking) {
            checking = false;
            StartCoroutine(CheckMatch());
        }    
    }
}