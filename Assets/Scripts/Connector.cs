using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Requests;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sfs2X.Entities;
using System.Linq;
using Sfs2X.Entities.Variables;
using Sfs2X.Entities.Data;

public class Connector : MonoBehaviour
{
    public GameObject Scripts;
    UIDisplayer UI;

    public string ServerIP = "127.0.0.1";
    public int ServerPort = 9933;
    public string ZoneName = "BasicExamples";
    public static string Username;

    public static SmartFox sfs;

    public bool FirstJoin = true;

    public float Timer = 60;
    public static bool IsGame = false;
    bool Mode2 = false;
    bool Mode1 = false;

    public void Start()
    {
        UI = Scripts.GetComponent<UIDisplayer>();
    }
    public void Connect()
    {
        InputField userName = UI.LoginPanel.GetComponentInChildren<InputField>();
        Username = userName.text;

        sfs = new SmartFox();
        sfs.ThreadSafeMode = true;

        sfs.AddEventListener(SFSEvent.CONNECTION, OnConnection);
        sfs.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
        sfs.AddEventListener(SFSEvent.LOGIN, OnLogin);
        sfs.AddEventListener(SFSEvent.LOGIN_ERROR, OnLoginError);

        sfs.AddEventListener(SFSEvent.ROOM_JOIN, OnRoomJoin);
        sfs.AddEventListener(SFSEvent.ROOM_JOIN_ERROR, OnRoomJoinError);
        sfs.AddEventListener(SFSEvent.PUBLIC_MESSAGE, OnPublicMessage);
        sfs.AddEventListener(SFSEvent.USER_ENTER_ROOM, OnUserEnterRoom);
        sfs.AddEventListener(SFSEvent.USER_EXIT_ROOM, OnUserExitRoom);
        sfs.AddEventListener(SFSEvent.ROOM_ADD, OnRoomAdd);
        sfs.AddEventListener(SFSEvent.ROOM_REMOVE, OnRoomRemove);
        sfs.AddEventListener(SFSEvent.EXTENSION_RESPONSE, OnExtensionResp);
        sfs.AddEventListener(SFSEvent.USER_VARIABLES_UPDATE, OnVariablesUpdate);

        sfs.Connect(ServerIP, ServerPort);
    }

    public void OnConnection(BaseEvent e)
    {
        if ((bool)e.Params["success"])
        {
            Debug.Log("Successfully Connected");

            sfs.Send(new LoginRequest(Username, "", ZoneName));
            UI.DisplayMessengerUI();
        }
        else
        {
            Debug.Log("Connection Failed");
        }
    }
    public void OnConnectionLost(BaseEvent e)
    {
        Debug.Log("Disconnected");
        UI.DisplayLoginUI();
        ResetConnect();
    }
    public void OnLogin(BaseEvent e)
    {
        Debug.Log("Logged In: " + e.Params["user"]);

        User user = (User)e.Params["user"];
        if (user.IsGuest())
        {
            Username = user.Name;
        }

        List<UserVariable> userVars = new List<UserVariable>();
        userVars.Add(new SFSUserVariable("name", Username));
        userVars.Add(new SFSUserVariable("score", 0));
        sfs.Send(new SetUserVariablesRequest(userVars));

        sfs.Send(new JoinRoomRequest("The Lobby"));
        UI.PopulateRoomList(sfs.RoomList);

        if (sfs.RoomList.Count > 0)
        {
            sfs.Send(new JoinRoomRequest(sfs.RoomList[0].Name));
        }
    }
    public void OnLoginError(BaseEvent e)
    {
        sfs.Disconnect();
        Debug.Log("Login error (" + e.Params["errorCode"] + "): " + e.Params["errorMessage"]);
    }
    public void OnRoomJoin(BaseEvent e)
    {
        Room room = (Room)e.Params["room"];

        if (room.IsGame)
        {
            if (room.Name.Contains("game 1"))
            {
                UI.DisplayGameUI();
                Timer = (DateTime.Parse(room.GetVariable("time").Value.ToString()) - DateTime.Now).Seconds + 1;
                Mode1 = true;
            }
            else
            {
                UI.DisplayWaitUI();
            }

            UI.PopulateScoreRank(room.UserList);
            IsGame = true;
        }

        if (room.Name == "The Lobby")
        {
            UI.PopulateUserList(room.UserList);

            if (!FirstJoin)
            {
                Text ChatTxt = UI.ChatPanel.GetComponentInChildren<Text>();
                ChatTxt.text = "";
            }
            FirstJoin = false;

            UI.PrintSystemMessage("\nYou joined room '" + room.Name + "'\n");

            CanvasGroup ChatCtrl = UI.ChatPanel.GetComponentInChildren<CanvasGroup>();
            ChatCtrl.interactable = true;
        }

        Debug.Log("Got in room " + room.Name);
    }
    public void OnRoomJoinError(BaseEvent e)
    {
        UI.PrintSystemMessage("Room join failed: " + e.Params["errorMessage"]);
    }
    public void OnPublicMessage(BaseEvent e)
    {
        User sender = (User)e.Params["sender"];
        string mess = (string)e.Params["message"];
        UI.PrintUserMessage(sender, mess);
    }
    public void OnUserEnterRoom(BaseEvent e)
    {
        User user = (User)e.Params["user"];
        Room room = (Room)e.Params["room"];

        UI.PrintSystemMessage("User " + user.Name + " entered the room");

        if (room.Name == "The Lobby")
        {
            UI.PopulateUserList(room.UserList);
        }
        else
        {
            UI.PopulateScoreRank(room.UserList);
        }
    }
    public void OnUserExitRoom(BaseEvent e)
    {
        User user = (User)e.Params["user"];

        if (user != sfs.MySelf)
        {
            Room room = (Room)e.Params["room"];
            UI.PrintSystemMessage("User " + user.Name + " left the room");

            if (room.Name == "The Lobby")
            {
                UI.PopulateUserList(room.UserList);
            }
            else
            {
                UI.PopulateScoreRank(room.UserList);
            }
        }
        else
        {
            Room r = (Room)e.Params["room"];
            if (r.Name != "The Lobby")
            {
                sfs.Send(new JoinRoomRequest("The Lobby"));
                UI.PopulateRoomList(sfs.RoomList);
                IsGame = false;
                Timer = 60;
                Mode1 = false;
                Mode2 = false;
            }
        }
    }
    public void OnRoomAdd(BaseEvent e)
    {
        UI.PopulateRoomList(sfs.RoomList);
    }
    public void OnRoomRemove(BaseEvent e)
    {
        UI.PopulateRoomList(sfs.RoomList);
    }
    public void OnExtensionResp(BaseEvent e)
    {      
        switch((string)e.Params["cmd"])
        {
            case "stop":
                Result();

                Debug.Log("Time up!!");
                break;
            case "start":
                SFSObject cmd = new SFSObject();
                cmd.PutBool("g", true);
                sfs.Send(new ExtensionRequest("g", cmd, sfs.RoomManager.GetUserRooms(sfs.MySelf).Last()));

                SFSObject data = (SFSObject)e.Params["params"];
                string[] posList = data.GetUtfString("il").Split(',');
                List<int> List = new List<int>();
                for (int i = 0; i < posList.Length; i++)
                {
                    List.Add(int.Parse(posList[i]));
                }

                GetComponent<GameController>().enabled = true;
                GetComponent<GameController>().AddSprites2(List);
                Mode2 = true;
                UI.DisplayGameUI();

                Debug.Log("Start game");
                break;
        }
    }
    public void OnVariablesUpdate(BaseEvent e)
    {
        Room room = sfs.RoomManager.GetUserRooms(sfs.MySelf).Last();
        UI.PopulateScoreRank(room.UserList);
    }

    public void ResetConnect()
    {
        sfs.RemoveAllEventListeners();
        sfs = null;
        Debug.Log("Reset Connect.");
    }
    public void Exit()
    {
        sfs.Send(new LeaveRoomRequest());
        UI.DisplayMessengerUI();
        UI.PopulateRoomList(sfs.RoomList);
    }
    public void Logout()
    {
        if (sfs.IsConnected)
        {
            UI.ClearRoomList();
            sfs.Disconnect();
        }
    }
    public void CreateRoom()
    {
        RoomSettings settings = new RoomSettings(sfs.MySelf.Name + "'s game 1");
        settings.GroupId = "games";
        settings.IsGame = true;
        settings.MaxUsers = 20;
        settings.MaxSpectators = 0;
        settings.Extension = new RoomExtension("RoomExt", "com.a51integrated.sfs2x.RoomExt");

        List<RoomVariable> roomTime = new List<RoomVariable>
        {
            new SFSRoomVariable("time", DateTime.Now.AddSeconds(Timer).ToString())
        };
        settings.Variables = roomTime;

        sfs.Send(new CreateRoomRequest(settings, true, sfs.LastJoinedRoom));
    }
    public void CreateMode2()
    {
        RoomSettings settings = new RoomSettings(sfs.MySelf.Name + "'s game 2");
        settings.GroupId = "games";
        settings.IsGame = true;
        settings.MaxUsers = 2;
        settings.MaxSpectators = 0;
        settings.Extension = new RoomExtension("Mode2Ext", "com.a51integrated.sfs2x.Mode2Ext");

        sfs.Send(new CreateRoomRequest(settings, true, sfs.LastJoinedRoom));
    }
    public void Result()
    {
        Room room = sfs.RoomManager.GetUserRooms(sfs.MySelf).Last();

        string winner = room.UserList[0].GetVariable("name").Value.ToString();
        int top1Score = int.Parse(room.UserList[0].GetVariable("score").Value.ToString());

        for (int i = 1; i < room.UserList.Count; i++)
        {
            if (int.Parse(room.UserList[i].GetVariable("score").Value.ToString()) > top1Score)
            {
                winner = room.UserList[i].GetVariable("name").Value.ToString();
                top1Score = int.Parse(room.UserList[i].GetVariable("score").Value.ToString());
            }
        }
        if (int.Parse(sfs.MySelf.GetVariable("score").Value.ToString()) == top1Score)
        {
            winner = sfs.MySelf.GetVariable("name").Value.ToString();
        }

        UI.ShowResult(winner);

        IsGame = false;
        sfs.Send(new LeaveRoomRequest());
    }
    public void Update()
    {
        if (Timer > 0 && IsGame == true && Mode1 == true)
        {
            Timer -= Time.deltaTime;
            Text timer = UI.GamePanel.transform.Find("Canvas/Timer").GetComponent<Text>();
            timer.text = Math.Round(Timer).ToString();
        }
        else
        {
            if(Mode2 == true)
            {
                Timer -= Time.deltaTime;
                Text timer = UI.GamePanel.transform.Find("Canvas/Timer").GetComponent<Text>();
                timer.text = Math.Round(Timer).ToString();
            }
        }

        UI.OnSendKeyPressed();

        if (sfs != null)
        {
            sfs.ProcessEvents();
        }
    }
    public void OnApplicationQuit()
    {
        if (sfs.IsConnected)
        {
            sfs.Disconnect();
        }
    }
}
