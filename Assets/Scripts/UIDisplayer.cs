using Sfs2X.Entities;
using Sfs2X.Requests;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIDisplayer : MonoBehaviour
{
    public GameObject roomListItem;
    public Transform roomListContent;

    public GameObject LoginPanel;
    public GameObject UsersPanel;
    public GameObject ChatPanel;
    public GameObject GamePanel;
    public GameObject RoomsPanel;
    public GameObject ResultPanel;
    public GameObject GameModePanel;
    public GameObject WaitPanel;

    public GameObject Scripts;
    Connector Connect;

    void Start()
    {
        Connect = Scripts.GetComponent<Connector>();

        StartDisplaying();
      
        OnLoginBtnClicked();
        OnSendBtnClicked();
        OnLogoutBtnClicked();
        OnCreateBtnClicked();
        OnGamePanelExitBtnClicked();
        OnBackToLobbyBtnClicked();
        OnCancelBtnClicked();
        OnMode1BtnClicked();
        OnMode2BtnClicked();
        OnWaitPanelExitBtnClicked();
    }

    public void StartDisplaying()
    {
        DisplayLoginUI();

        InputField inp = LoginPanel.GetComponentInChildren<InputField>();
        inp.ActivateInputField();
    }
    public void DisplayLoginUI()
    {
        UsersPanel.gameObject.SetActive(false);
        ChatPanel.gameObject.SetActive(false);
        GamePanel.gameObject.SetActive(false);
        RoomsPanel.gameObject.SetActive(false);
        ResultPanel.gameObject.SetActive(false);
        GameModePanel.gameObject.SetActive(false);
        WaitPanel.gameObject.SetActive(false);
        LoginPanel.gameObject.SetActive(true);
    }
    public void DisplayMessengerUI()
    {
        GetComponent<GameController>().enabled = false;
        LoginPanel.gameObject.SetActive(false);
        GamePanel.gameObject.SetActive(false);
        ResultPanel.gameObject.SetActive(false);
        GameModePanel.gameObject.SetActive(false);
        WaitPanel.gameObject.SetActive(false);
        UsersPanel.gameObject.SetActive(true);
        ChatPanel.gameObject.SetActive(true);
        RoomsPanel.gameObject.SetActive(true);

        InputField inp = ChatPanel.GetComponentInChildren<InputField>();
        inp.ActivateInputField();
    }
    public void DisplayGameUI()
    {
        GetComponent<GameController>().enabled = true;
        LoginPanel.gameObject.SetActive(false);
        RoomsPanel.gameObject.SetActive(false);
        ChatPanel.gameObject.SetActive(false);
        UsersPanel.gameObject.SetActive(false);
        GameModePanel.gameObject.SetActive(false);
        WaitPanel.gameObject.SetActive(false);
        GamePanel.gameObject.SetActive(true);

        if(Connector.sfs.RoomManager.GetUserRooms(Connector.sfs.MySelf).Last().Name.Contains("game 2"))
        {
            Button shuffleBtn = GamePanel.transform.Find("Canvas/ShuffleBtn").GetComponent<Button>();
            shuffleBtn.interactable = false;
            shuffleBtn.image.color = new Color(0, 0, 0, 0);
            shuffleBtn.GetComponentInChildren<Text>().text = "";
        }

        Debug.Log("Got in Game");
    }
    public void DisplayGameModeSelectUI()
    {
        LoginPanel.gameObject.SetActive(false);
        RoomsPanel.gameObject.SetActive(false);
        ChatPanel.gameObject.SetActive(false);
        UsersPanel.gameObject.SetActive(false);
        WaitPanel.gameObject.SetActive(false);
        GameModePanel.gameObject.SetActive(true);
    }
    public void DisplayWaitUI()
    {
        LoginPanel.gameObject.SetActive(false);
        RoomsPanel.gameObject.SetActive(false);
        ChatPanel.gameObject.SetActive(false);
        UsersPanel.gameObject.SetActive(false);
        GameModePanel.gameObject.SetActive(false);
        WaitPanel.gameObject.SetActive(true);
    }

    public void OnLoginBtnClicked()
    {
        Button btn = LoginPanel.GetComponentInChildren<Button>();
        btn.onClick.AddListener(() => Connect.Connect());
    }
    public void OnSendBtnClicked()
    {
        Button sendBtn = ChatPanel.GetComponentInChildren<Button>();
        sendBtn.onClick.AddListener(() => SendMess());
    }
    public void OnLogoutBtnClicked()
    {
        Button btn = UsersPanel.GetComponentInChildren<Button>();
        btn.onClick.AddListener(() => Connect.Logout());
    }
    public void OnGamePanelExitBtnClicked()
    {
        Button exitBtn = GamePanel.transform.Find("Canvas/ExitBtn").GetComponent<Button>();
        exitBtn.onClick.AddListener(() => Connect.Exit());
    }
    public void OnWaitPanelExitBtnClicked()
    {
        Button exitBtn = WaitPanel.transform.Find("ExitBtn").GetComponent<Button>();
        exitBtn.onClick.AddListener(() => Connect.Exit());
    }
    public void OnCancelBtnClicked()
    {
        Button cancelBtn = GameModePanel.transform.Find("CancelBtn").GetComponent<Button>();
        cancelBtn.onClick.AddListener(() => DisplayMessengerUI());
    }
    public void OnCreateBtnClicked()
    {
        Button CreateBtn = RoomsPanel.transform.Find("Canvas/CreateBtn").GetComponent<Button>();
        CreateBtn.onClick.AddListener(() => DisplayGameModeSelectUI());
    }
    public void OnMode1BtnClicked()
    {
        Button mode1Btn = GameModePanel.transform.Find("Mode1Btn").GetComponent<Button>();
        mode1Btn.onClick.AddListener(() => Connect.CreateRoom());
    }
    public void OnMode2BtnClicked()
    {
        Button mode2Btn = GameModePanel.transform.Find("Mode2Btn").GetComponent<Button>();
        mode2Btn.onClick.AddListener(() => Connect.CreateMode2());
    }
    public void OnBackToLobbyBtnClicked()
    {
        Button backBtn = ResultPanel.GetComponentInChildren<Button>();
        backBtn.onClick.AddListener(() => DisplayMessengerUI());
    }
    public void OnSendKeyPressed()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SendMess();
        }
    }
    public void OnRoomItemClicked(int roomId)
    {
        Connector.sfs.Send(new JoinRoomRequest(roomId));
    }

    public void PopulateUserList(List<User> users)
    {
        List<string> userNames = new List<string>();

        foreach (User user in users)
        {
            string name = user.Name;

            if (user == Connector.sfs.MySelf)
            {
                name += "<color=#808080ff>(you)</color>";
            }
            userNames.Add(name);
        }

        userNames.Sort();

        Text UsersListTxt = UsersPanel.GetComponentInChildren<Text>();
        UsersListTxt.text = "";
        UsersListTxt.text = string.Join("\n", userNames.ToArray());

        Canvas.ForceUpdateCanvases();

        Debug.Log("Populate userlist");
    }
    public void PopulateScoreRank(List<User> users)
    {
        List<string> userNames = new List<string>();

        foreach (User user in users)
        {
            string name = user.GetVariable("name").Value.ToString();

            if (user == Connector.sfs.MySelf)
            {
                name += "<color=#808080ff>(you)</color>";
            }

            name += "     " + user.GetVariable("score").Value.ToString();
            userNames.Add(name);
        }

        if (users.Count > 1)
        {
            string temp = "";
            for (int i = 0; i < userNames.Count; i++)
            {
                int scoreI = int.Parse(users[i].GetVariable("score").Value.ToString());

                for (int j = i + 1; j < userNames.Count; j++)
                {
                    int scoreJ = int.Parse(users[j].GetVariable("score").Value.ToString());

                    if (scoreI <= scoreJ)
                    {
                        temp = userNames[i];
                        userNames[i] = userNames[j];
                        userNames[j] = temp;
                    }
                }
            }
        }

        Text ScoreRankTxt = GamePanel.GetComponentInChildren<ScrollRect>().GetComponentInChildren<Text>();
        ScoreRankTxt.text = "";
        ScoreRankTxt.text = string.Join("\n", userNames.ToArray());

        Canvas.ForceUpdateCanvases();

        Debug.Log("Populate scorerank");
    }
    public void PopulateRoomList(List<Room> rooms)
    {
        ClearRoomList();
        foreach (Room room in rooms)
        {
            if (room.Name == "The Lobby" || room.IsHidden)
            {
                continue;
            }

            int roomId = room.Id;

            GameObject newListItem = Instantiate(roomListItem) as GameObject;
            RoomItem roomItem = newListItem.GetComponent<RoomItem>();
            roomItem.nameLabel.text = room.Name;
            roomItem.maxUsersLabel.text = "[max " + room.MaxUsers + " users]";
            roomItem.roomId = roomId;

            roomItem.button.onClick.AddListener(() => OnRoomItemClicked(roomId));

            newListItem.transform.SetParent(roomListContent, false);
        }
    }

    public void PrintSystemMessage(string mess)
    {
        Text Chattxt = ChatPanel.GetComponentInChildren<Text>();
        Chattxt.text += "<color=#000000ff>" + mess + "</color>\n";

        Canvas.ForceUpdateCanvases();

        ScrollRect ChatScrollView = ChatPanel.GetComponentInChildren<ScrollRect>();
        ChatScrollView.verticalNormalizedPosition = 0;
    }
    public void PrintUserMessage(User sender, string mess)
    {
        Text Chattxt = ChatPanel.GetComponentInChildren<Text>();
        Chattxt.text += "<b>" + (sender == Connector.sfs.MySelf ? "You" : sender.Name) + ":</b>" + mess + "\n";

        ScrollRect ChatScrollView = ChatPanel.GetComponentInChildren<ScrollRect>();
        ChatScrollView.verticalNormalizedPosition = 0;
    }

    public void ClearRoomList()
    {
        foreach (Transform child in roomListContent.transform)
        {
            Destroy(child.gameObject);
        }
    }
    public void SendMess()
    {
        InputField txtField = ChatPanel.GetComponentInChildren<InputField>();

        if (txtField.text != "")
        {
            Connector.sfs.Send(new PublicMessageRequest(txtField.text));
            txtField.text = "";
        }

        txtField.ActivateInputField();
        txtField.Select();
    }
    public void ShowResult(string winner)
    {
        if (winner == Connector.sfs.MySelf.Name)
        {
            Text result = ResultPanel.GetComponentInChildren<Text>();
            result.text = "You won!! Score: " + Connector.sfs.MySelf.GetVariable("score").Value.ToString();
        }
        else
        {
            Text result = ResultPanel.GetComponentInChildren<Text>();
            result.text = "You lose. Score: " + Connector.sfs.MySelf.GetVariable("score").Value.ToString();
        }

        Text rankTxt = ResultPanel.GetComponentInChildren<ScrollRect>().GetComponentInChildren<Text>();
        rankTxt.text = GamePanel.GetComponentInChildren<ScrollRect>().GetComponentInChildren<Text>().text;

        GamePanel.gameObject.SetActive(false);
        ResultPanel.gameObject.SetActive(true);
    }
}
