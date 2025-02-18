using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NestingController : MonoBehaviour
{
    public int boardWidth = 297;
    public int boardHeight = 210;
    public GameObject box;
    public int boxMax = 50;
    public int boxCount = 10;
    public List<GameObject> boxList = new List<GameObject>();

    private List<BoardObject> boardList = new List<BoardObject>();

    // Start is called before the first frame update
    void Start()
    {
        CreateBoxes(boxCount);
        //Debug.Log(boxList[0].width + " x " + boxList[0].height);

        // 테스트 정렬
        float sum = 0;
        foreach (GameObject obj in boxList)
        {
            sum += obj.transform.localScale.x * 0.5f;
            obj.transform.position = new Vector3(sum, 0, 0);
            sum += obj.transform.localScale.x * 0.5f;
        }

        DoNesting();
    }

    // 박스 생성 및 자동 면적 내림차순 정렬
    void CreateBoxes(int count)
    {
        int index = 0;
        List<BoxObject> boxList = new List<BoxObject>();
        while (index < count)
        {
            BoxObject boxObj = new BoxObject();
            boxObj.width = Random.Range(1, boxMax + 1);
            boxObj.height = Random.Range(1, boxMax + 1);
            boxObj.SetSize();

            if (boxObj.width > boardWidth && boxObj.width > boardHeight) break;
            if (boxObj.height > boardWidth && boxObj.height > boardHeight) break;

            boxList.Add(boxObj);
            index++;
        }
        boxList.Sort((x, y) => y.size.CompareTo(x.size));

        float colorUnit = 1f / boxList.Count;

        for (int i = 0; i < boxList.Count; i++)
        {
            GameObject boxInstance = Instantiate(box);
            boxInstance.SetActive(true);
            if (boxInstance.GetComponent<MeshRenderer>())
            {
                boxInstance.GetComponent<MeshRenderer>().material.color = new Color(colorUnit * i, colorUnit * i, 1);
            }
            boxInstance.transform.localScale = new Vector3(boxList[i].width, 1, boxList[i].height);
            boxInstance.name = "box_" + i;
            this.boxList.Add(boxInstance);
        }
    }

    void DoNesting()
    {
        // Board 생성
        if (boardList.Count <= 0)
        {
            CreateBoard(0);
        }

        foreach (GameObject box in boxList)
        {
            int currentBoardIndex = 0;
            bool isRotated = false;

            for(int x=0; x<boardWidth; x++)
            {
                for(int y=0; y<boardHeight; y++)
                {
                    int[] startP = { x, y };

                    // board 필요 시 추가
                    if (boardList.Count <= currentBoardIndex) CreateBoard(currentBoardIndex);

                    bool isPossibleBoard = boardList.Count > 0 && boardList[currentBoardIndex].state != BoardState.Impossible;
                    if (isPossibleBoard)
                    {
                        // startP에서 박스가 선 넘으면 중지
                        // board.matrix, true 하나라도 있어도 중지
                        // 중지 시 회전 시도 1회
                        FindMatrix:
                        int w = (int)box.transform.localScale.x;
                        int h = (int)box.transform.localScale.z;
                        int falseCount = 0;                        
                        for (int i = 0; i < w; i++)
                        {
                            for (int j = 0; j < h; j++)
                            {
                                bool isStop = 
                                    startP[0] + w > boardWidth ||
                                    startP[1] + h > boardHeight || 
                                    boardList[currentBoardIndex].matrix[i + startP[0], j + startP[1]] == true;
                                
                                if (isStop)
                                {
                                    if (isRotated == false) 
                                    {
                                        box.transform.localScale.Set(box.transform.localScale.z, 1, box.transform.localScale.x);
                                        isRotated = true;
                                        goto FindMatrix;
                                    } 
                                    else
                                    {
                                        goto DrawBox;
                                    }
                                }
                                else
                                {
                                    falseCount++;
                                }
                            }
                        }

                        // box 화면에 배치.  board.matrix, box영역 true로 변환
                        DrawBox:
                        if (falseCount == w * h)
                        {
                            box.transform.position = new Vector3(
                                startP[0] + w * 0.5f + (currentBoardIndex * boardWidth), 
                                0,
                                startP[1] + h * 0.5f
                                );
                            for (int i = 0; i < w; i++)
                            {
                                for (int j = 0; j < h; j++)
                                {
                                    boardList[currentBoardIndex].matrix[i + startP[0], j + startP[1]] = true;
                                }
                            }
                            //if(isRotated) Debug.Log("rotation__" + box.name);
                            goto EndDrawing;
                        }
                        else
                        {
                            // 보드 마지막까지 찾아도 자리 없으면 보드 추가
                            bool isOver = 
                                box.transform.localScale.x * box.transform.localScale.z > 1 &&
                                startP[0] >= boardWidth - 1 && 
                                startP[1] >= boardHeight - 1;
                            if (isOver)
                            {
                                currentBoardIndex++;
                                isRotated = false;
                                x = y = 0;
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("error_board count");
                    }
                }
            }
            EndDrawing:
            { }
        }
    }

    void CreateBoard(int index)
    {
        BoardObject tempBoard = new BoardObject();
        tempBoard.matrix = new bool[boardWidth, boardHeight];
        boardList.Add(tempBoard);

        GameObject boardPlane = Instantiate(box);
        boardPlane.transform.localScale = new Vector3(boardWidth, 0.1f, boardHeight);
        boardPlane.transform.position = new Vector3(
            boardWidth * 0.5f + (index * boardWidth),
            -0.5f,
            boardHeight * 0.5f
        );
        boardPlane.SetActive(true);
    }
}
// 보드 추가, 회전