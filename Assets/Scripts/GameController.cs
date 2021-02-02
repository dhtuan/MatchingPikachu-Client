using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Requests;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    public static int rows = 7;
    public static int columns = 12;

    public bool FirstGuess, SecondGuess;
    public string FirstName, SecondName;
    public int FirstXIndex, FirstYIndex;
    public int SecondXIndex, SecondYIndex;

    public List<Button> BtnList = new List<Button>();

    GameObject btn;
    public GameObject Pokemon;
    public Transform PokemonPanel;
    public Button ShuffleBtn;

    public Sprite[] SourceSprites;

    public List<Sprite> GameSprite = new List<Sprite>();
    public int[] SpriteStat = new int[(rows - 2) * (columns - 2)];

    private static int _score = 0;
    public static int score
    {
        get { return _score; }
        private set 
        {
            _score = value;

            List<UserVariable> userVars = new List<UserVariable>();
            userVars.Add(new SFSUserVariable("name", Connector.Username));
            userVars.Add(new SFSUserVariable("score", value));

            Connector.sfs.Send(new SetUserVariablesRequest(userVars));
        }
    }
    
    public void Awake()
    {
        enabled = false;
    }
    public void LoadGame()
    {
        if (SourceSprites.Length == 0)
        {
            SourceSprites = Resources.LoadAll<Sprite>("Sprites/GameImg");
        }

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (i == 0 || i == 6 || j == 0 || j == 11)
                {
                    continue;
                }

                btn = Instantiate(Pokemon);
                btn.name = i + " : " + j;
                btn.transform.SetParent(PokemonPanel, false);
                btn.GetComponentInChildren<Text>().text = "";
                BtnList.Add(btn.GetComponent<Button>());
            }
        }

        if (Connector.sfs.RoomManager.GetUserRooms(Connector.sfs.MySelf).Last().Name.Contains("game 1"))
        {
            AddSprites();
        }
        else
        {

        }

        score = 0;
    }
    public void ResetGameboard()
    {
        for (int i = 0; i < BtnList.Count; i++)
        {
            Destroy(BtnList[i].gameObject);
            Debug.Log("Btn destroyed");
        }
        BtnList.Clear();

        GameSprite.Clear();

        for (int i = 0; i < SpriteStat.Length; i++)
        {
            SpriteStat[i] = 0;
        }
    }
    public void AddSprites()
    {
        for (int i = 0; i < SpriteStat.Length; i++)
        {
            SpriteStat[i] = 1;
        }

        for (int i = 0; i < BtnList.Count / 2; i++)
        {
            int rand = Random.Range(0, 36);
            GameSprite.Add(SourceSprites[rand]);
            GameSprite.Add(SourceSprites[rand]);
            BtnList[i].image.sprite = GameSprite[i];
        }

        Shuffle(GameSprite);
    }
    public void AddSprites2(List<int> spriteList)
    {
        for (int i = 0; i < SpriteStat.Length; i++)
        {
            SpriteStat[i] = 1;
        }

        for (int i = 0; i < BtnList.Count; i++)
        {
            GameSprite.Add(SourceSprites[spriteList[i]]);
            BtnList[i].image.sprite = GameSprite[i];
        }

    }
    public void Shuffle(List<Sprite> spriteList)
    {
        Sprite temp;
        int tempPos;

        for (int i = 0; i < spriteList.Count; i++)
        {
            if(SpriteStat[i] != 0)
            {
                temp = spriteList[i];
                tempPos = i;
                int rand = RandomRangeExcept(i, spriteList.Count);
                spriteList[i] = spriteList[rand];
                SpriteStat[i] = SpriteStat[rand];
                spriteList[rand] = temp;
                SpriteStat[rand] = SpriteStat[tempPos];
                BtnList[i].image.sprite = GameSprite[i];
            }
        }
    }
    public void ShuffleClicked()
    {
        ShuffleBtn.onClick.AddListener(() => Shuffle(GameSprite));
    }
    public int RandomRangeExcept(int min, int max)
    {
        int number;
        do
        {
            number = Random.Range(min, max);
        } while (SpriteStat[number] == 0);

        return number;
    }

    public void OnEnable()
    {
        FirstGuess = SecondGuess = false;

        LoadGame();
        AddListener();
        ShuffleClicked();

        Connector.sfs.AddEventListener(SFSEvent.EXTENSION_RESPONSE, OnExtensionResp);
        Connector.sfs.AddEventListener(SFSEvent.ROOM_VARIABLES_UPDATE, RoomVariableChanged);
    }
    public void OnDisable()
    {
        ResetGameboard();

        Connector.sfs.RemoveEventListener(SFSEvent.EXTENSION_RESPONSE, OnExtensionResp);
        Connector.sfs.RemoveEventListener(SFSEvent.ROOM_VARIABLES_UPDATE, RoomVariableChanged);
    }

    public void AddListener()
    {
        foreach (Button btn in BtnList)
        {
            btn.onClick.AddListener(() => PickPuzzle());
        }
    }
    public void PickPuzzle()
    {
        string thisBtnName = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name;
        if (!FirstGuess)
        {
            FirstGuess = true;

            FirstXIndex = int.Parse(thisBtnName.Substring(0, thisBtnName.IndexOf(" : ")));
            FirstYIndex = int.Parse(thisBtnName.Substring(thisBtnName.LastIndexOf(" ") + 1, thisBtnName.Length - thisBtnName.LastIndexOf(" ") - 1));
            FirstName = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponent<Image>().sprite.name;
        }
        else if (!SecondGuess)
        {
            SecondGuess = true;

            SecondXIndex = int.Parse(thisBtnName.Substring(0, thisBtnName.IndexOf(" : ")));
            SecondYIndex = int.Parse(thisBtnName.Substring(thisBtnName.LastIndexOf(" ") + 1, thisBtnName.Length - thisBtnName.LastIndexOf(" ") - 1));
            SecondName = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponent<Image>().sprite.name;

            ISFSObject move = new SFSObject();
            move.PutUtfString("n1", FirstName);
            move.PutInt("x1", FirstXIndex);
            move.PutInt("y1", FirstYIndex);

            move.PutUtfString("n2", SecondName);
            move.PutInt("x2", SecondXIndex);
            move.PutInt("y2", SecondYIndex);

            Connector.sfs.Send(new ExtensionRequest("move", move, Connector.sfs.RoomManager.GetUserRooms(Connector.sfs.MySelf).Last()));
        }
    }
    public void RoomVariableChanged(BaseEvent e)
    {
        Room r = (Room)e.Params["room"];
        if(r.Name.Contains("game 2"))
        {
            if(r.ContainsVariable("sp"))
            {
                string roomVar = r.GetVariable("sp").Value.ToString();
                string[] SpriteIndexSplit = roomVar.Split(',');
                List<int> NewSpriteIndex = new List<int>();
                for (int i = 0; i < SpriteIndexSplit.Length; i++)
                {
                    NewSpriteIndex.Add(int.Parse(SpriteIndexSplit[i]));
                }
                for (int i = 0; i < GameSprite.Count; i++)
                {
                    GameSprite[i] = SourceSprites[NewSpriteIndex[i]];
                }
            }

            string matchedPos = r.GetVariable("m").Value.ToString();
            string[] IndexXY1XY2 = matchedPos.Split(',');

            ButtonAt(int.Parse(IndexXY1XY2[0]), int.Parse(IndexXY1XY2[1])).interactable = false;
            ButtonAt(int.Parse(IndexXY1XY2[0]), int.Parse(IndexXY1XY2[1])).image.color = new Color(0, 0, 0, 0);
            SpriteStatUpdate(int.Parse(IndexXY1XY2[0]), int.Parse(IndexXY1XY2[1]));

            ButtonAt(int.Parse(IndexXY1XY2[2]), int.Parse(IndexXY1XY2[3])).interactable = false;
            ButtonAt(int.Parse(IndexXY1XY2[2]), int.Parse(IndexXY1XY2[3])).image.color = new Color(0, 0, 0, 0);
            SpriteStatUpdate(int.Parse(IndexXY1XY2[2]), int.Parse(IndexXY1XY2[3]));
        }

    }
    public void OnExtensionResp(BaseEvent e)
    {
        SFSObject param = (SFSObject)e.Params["params"];
        switch ((string)e.Params["cmd"])
        {
            case "ok":
                score++;

                ButtonAt(FirstXIndex, FirstYIndex).interactable = false;
                ButtonAt(FirstXIndex, FirstYIndex).image.color = new Color(0, 0, 0, 0);
                SpriteStatUpdate(FirstXIndex, FirstYIndex);

                ButtonAt(SecondXIndex, SecondYIndex).interactable = false;
                ButtonAt(SecondXIndex, SecondYIndex).image.color = new Color(0, 0, 0, 0);
                SpriteStatUpdate(SecondXIndex, SecondYIndex);

                FirstGuess = SecondGuess = false;

                if (param.GetInt("nvm") == -1)
                {
                    string spriteStatToString = string.Join(",", SpriteStat.ToString());
                    ISFSObject spriteStat = new SFSObject();
                    spriteStat.PutUtfString("ss", spriteStatToString);
                    Connector.sfs.Send(new ExtensionRequest("nvm", spriteStat, Connector.sfs.RoomManager.GetUserRooms(Connector.sfs.MySelf).Last()));
                }

                break;

            case "no":
                FirstGuess = SecondGuess = false;

                break;
        }
    }
    public void SpriteStatUpdate(int x, int y)
    {
        int count = 0;
        for (int i = 1; i < rows - 1; i++)
        {
            for (int j = 1; j < columns - 1; j++)
            {
                if(i == x && j == y)
                {
                    SpriteStat[count] = 0;
                }
                count++;
            }
        }
    }
    public Button ButtonAt(int x, int y)
    {
        int count = 0;
        for (int i = 1; i < rows - 1; i++)
        {
            for (int j = 1; j < columns - 1; j++)
            {
                if (i == x && j == y)
                {
                    return BtnList[count];
                }
                count++;
            }
        }
        return null;
    }



    //public void CheckIfPuzzleMatch(int x1, int y1, int x2, int y2)
    //{
    //    if (FirstName == SecondName && (FirstXIndex != SecondXIndex || FirstYIndex != SecondYIndex))
    //    {
    //        if (PuzzleIsMatch(x1, y1, x2, y2))
    //        {
    //            ButtonAt(x1, y1).interactable = false;
    //            ButtonAt(x2, y2).interactable = false;

    //            ButtonAt(x1, y1).image.color = new Color(0, 0, 0, 0);
    //            ButtonAt(x2, y2).image.color = new Color(0, 0, 0, 0);

    //            BtnStat[x1, y1] = 0;
    //            BtnStat[x2, y2] = 0;

    //            int len = 0;
    //            for (int i = 1; i < rows - 1; i++)
    //            {
    //                for (int j = 1; j < columns - 1; j++)
    //                {
    //                    if (BtnStat[i, j] == 0)
    //                    {
    //                        SpriteStat[len] = 0;
    //                    }
    //                    len++;
    //                }
    //            }

    //            score++;
    //        }
    //        else
    //        {
    //            BtnStat[x1, y1] = 2;
    //            BtnStat[x2, y2] = 2;
    //        }
    //    }
    //    else
    //    {
    //        Debug.Log("They aren't the same");
    //        BtnStat[x1, y1] = 2;
    //        BtnStat[x2, y2] = 2;
    //    }
    //    FirstGuess = SecondGuess = false;
    //}
    //public bool PuzzleIsMatch(int x1, int y1, int x2, int y2)
    //{
    //    try
    //    {
    //        if (x1 == x2)
    //        {
    //            if (CheckLineX(y1, y2, x1))
    //            {
    //                return true;
    //            }
    //            if (CheckMoreLineY(x1, y1, x2, y2, 1) != -1)
    //            {
    //                return true;
    //            }
    //            if (CheckMoreLineY(x1, y1, x2, y2, -1) != -1)
    //            {
    //                return true;
    //            }
    //        }
    //        else if (y1 == y2)
    //        {
    //            if (CheckLineY(x1, x2, y1))
    //            {
    //                return true;
    //            }
    //            if (CheckMoreLineX(x1, y1, x2, y2, 1) != -1)
    //            {
    //                return true;
    //            }
    //            if (CheckMoreLineX(x1, y1, x2, y2, -1) != -1)
    //            {
    //                return true;
    //            }
    //        }
    //        else
    //        {
    //            if (CheckRectY(x1, y1, x2, y2) != -1)
    //            {
    //                return true;
    //            }
    //            if (CheckRectX(x1, y1, x2, y2) != -1)
    //            {
    //                return true;
    //            }
    //            if (CheckMoreLineX(x1, y1, x2, y2, 1) != -1)
    //            {
    //                return true;
    //            }
    //            if (CheckMoreLineX(x1, y1, x2, y2, -1) != -1)
    //            {
    //                return true;
    //            }
    //            if (CheckMoreLineY(x1, y1, x2, y2, 1) != -1)
    //            {
    //                return true;
    //            }
    //            if (CheckMoreLineY(x1, y1, x2, y2, -1) != -1)
    //            {
    //                return true;
    //            }

    //        }
    //        return false;
    //    }
    //    catch(Exception e)
    //    {
    //        Debug.Log(e);
    //    }
    //    return false;
    //}
    //public bool CheckLineX(int y1, int y2, int x)
    //{
    //    int min = Math.Min(y1, y2);
    //    int max = Math.Max(y1, y2);

    //    for (int y = min; y <= max; y++)
    //    {
    //        if (BtnStat[x, y] == 2)
    //        {
    //            return false;
    //        }
    //    }
    //    Debug.Log("LineX");
    //    return true;
    //}
    //public bool CheckLineY(int x1, int x2, int y)
    //{
    //    int min = Math.Min(x1, x2);
    //    int max = Math.Max(x1, x2);

    //    for (int x = min; x <= max; x++)
    //    {
    //        if (BtnStat[x, y] == 2)
    //        {
    //            return false;
    //        }
    //    }
    //    Debug.Log("LineY");
    //    return true;
    //}
    //public int CheckRectX(int x1, int y1, int x2, int y2)
    //{
    //    int Xmin = x1, Ymin = y1;
    //    int Xmax = x2, Ymax = y2;

    //    if (y1 > y2)
    //    {
    //        Xmin = x2; Ymin = y2;
    //        Xmax = x1; Ymax = y1;
    //    }

    //    for (int y = Ymin; y <= Ymax; y++)
    //    {
    //        if (CheckLineX(Ymin, y, Xmin) && CheckLineY(Xmin, Xmax, y) && CheckLineX(y, Ymax, Xmax) && BtnStat[Xmax, y] != 2 && BtnStat[Xmin, y] != 2)
    //        {
    //            Debug.Log("RectX: (" + Xmin + "," + Ymin + ") -> (" + Xmin + "," + y + ") -> (" + Xmax + "," + y + ") -> (" + Xmax + "," + Ymax + ")");
    //            return y;
    //        }
    //    }
    //    return -1;
    //}
    //public int CheckRectY(int x1, int y1, int x2, int y2)
    //{
    //    int Xmin = x1, Ymin = y1;
    //    int Xmax = x2, Ymax = y2;

    //    if (x1 > x2)
    //    {
    //        Xmin = x2; Ymin = y2;
    //        Xmax = x1; Ymax = y1;
    //    }

    //    for (int x = Xmin; x <= Xmax; x++)
    //    {
    //        if (CheckLineY(Xmin, x, Ymin) && CheckLineX(Ymin, Ymax, x) && CheckLineY(x, Xmax, Ymax) && BtnStat[x, Ymax] != 2 && BtnStat[x, Ymin] != 2)
    //        {
    //            Debug.Log("RectY: (" + Xmin + "," + Ymin + ") -> (" + x + "," + Ymin + ") -> (" + x + "," + Ymax + ") -> (" + Xmax + "," + Ymax + ")");
    //            return x;
    //        }
    //    }

    //    return -1;
    //}
    //public int CheckMoreLineX(int x1, int y1, int x2, int y2, int type)
    //{
    //    int Xmin = x1, Ymin = y1;
    //    int Xmax = x2, Ymax = y2;

    //    if (y1 > y2)
    //    {
    //        Xmin = x2; Ymin = y2;
    //        Xmax = x1; Ymax = y1;
    //    }

    //    int y = Ymax;
    //    int row = Xmin;

    //    if (type == -1)
    //    {
    //        y = Ymin;
    //        row = Xmax;
    //    }

    //    if (CheckLineX(Ymin, Ymax, row))
    //    {
    //        while (type == -1 ? y >= 0 : y <= columns)
    //        {
    //            if (CheckLineX(Ymin, y, Xmin) && CheckLineY(Xmin, Xmax, y) && CheckLineX(y, Ymax, Xmax) && BtnStat[Xmin, y] != 2 && BtnStat[Xmax, y] != 2)
    //            {
    //                Debug.Log("MoreLineX: (" + Xmin + "," + Ymin + ") -> (" + Xmin + "," + y + ") -> (" + Xmax + "," + y + ") -> (" + Xmax + "," + Ymax + ")");
    //                return y;
    //            }
    //            y += type;
    //        }
    //    }

    //    return -1;
    //}
    //public int CheckMoreLineY(int x1, int y1, int x2, int y2, int type)
    //{
    //    int Xmin = x1, Ymin = y1;
    //    int Xmax = x2, Ymax = y2;

    //    if (x1 > x2)
    //    {
    //        Xmin = x2; Ymin = y2;
    //        Xmax = x1; Ymax = y1;
    //    }

    //    int x = Xmax;
    //    int col = Ymin;

    //    if (type == -1)
    //    {
    //        x = Xmin;
    //        col = Ymax;
    //    }

    //    if (CheckLineY(Xmin, Xmax, col))
    //    {
    //        while (type == -1 ? x >= 0 : x <= rows)
    //        {
    //            if (CheckLineY(Xmin, x, Ymin) && CheckLineX(Ymin, Ymax, x) && CheckLineY(x, Xmax, Ymax) && BtnStat[x, Ymin] != 2 && BtnStat[x, Ymax] != 2)
    //            {
    //                Debug.Log("MoreLineY: (" + Xmin + "," + Ymin + ") -> (" + x + "," + Ymin + ") -> (" + x + "," + Ymax + ") -> (" + Xmax + "," + Ymax + ")");
    //                return x;
    //            }
    //            x += type;
    //        }
    //    }
    //    return -1;
    //}
}
