using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using Unity.VisualScripting;


public class NetworkManager : MonoBehaviourPunCallbacks
{
	public int see = 0;
    [Header("DisconnectPanel")]
    public InputField NickNameInput;

    [Header("LobbyPanel")]
    public GameObject LobbyPanel;
    public InputField RoomInput;
    public Text WelcomeText;
    public Text LobbyInfoText;
    public Button[] CellBtn;
    public Button PreviousBtn;
    public Button NextBtn;

    [Header("RoomPanel")]
    public GameObject RoomPanel;
    public Text ListText;
    public Text RoomInfoText;
    public Text[] ChatText;
    public InputField ChatInput;
	public Text whosturn;
	public GameObject[] masks;
	public GameObject[] tiles = new GameObject[82];

	[Header("ETC")]
    public PhotonView PV;
	public Sprite Otile;
	public Sprite Xtile;

	public bool isfirst = true;
	public bool isfull;
	public int myturn = 0;
	public int turn = 0; //0=>O   1=>X

	private int[,] map = new int[3, 3];
	
	List<RoomInfo> myList = new List<RoomInfo>();
    int currentPage = 1, maxPage, multiple;


    #region 방리스트 갱신
    // ◀버튼 -2 , ▶버튼 -1 , 셀 숫자
    public void MyListClick(int num)
    {
        if (num == -2) --currentPage;
        else if (num == -1) ++currentPage;
        else PhotonNetwork.JoinRoom(myList[multiple + num].Name);
        MyListRenewal();
    }

    void MyListRenewal()
    {
        // 최대페이지
        maxPage = (myList.Count % CellBtn.Length == 0) ? myList.Count / CellBtn.Length : myList.Count / CellBtn.Length + 1;

        // 이전, 다음버튼
        PreviousBtn.interactable = (currentPage <= 1) ? false : true;
        NextBtn.interactable = (currentPage >= maxPage) ? false : true;

        // 페이지에 맞는 리스트 대입
        multiple = (currentPage - 1) * CellBtn.Length;
        for (int i = 0; i < CellBtn.Length; i++)
        {
            CellBtn[i].interactable = (multiple + i < myList.Count) ? true : false;
            CellBtn[i].transform.GetChild(0).GetComponent<Text>().text = (multiple + i < myList.Count) ? myList[multiple + i].Name : "";
            CellBtn[i].transform.GetChild(1).GetComponent<Text>().text = (multiple + i < myList.Count) ? myList[multiple + i].PlayerCount + "/" + myList[multiple + i].MaxPlayers : "";
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        int roomCount = roomList.Count;
        for (int i = 0; i < roomCount; i++)
        {
            if (!roomList[i].RemovedFromList)
            {
                if (!myList.Contains(roomList[i])) myList.Add(roomList[i]);
                else myList[myList.IndexOf(roomList[i])] = roomList[i];
            }
            else if (myList.IndexOf(roomList[i]) != -1) myList.RemoveAt(myList.IndexOf(roomList[i]));
        }
        MyListRenewal();
    }
    #endregion


    #region 서버연결
    void Awake()
	{
		if(PlayerPrefs.HasKey("NAME"))
		{
			NickNameInput.text = PlayerPrefs.GetString("NAME");
			Connect();
		}
	}

    void Update()
    {
        LobbyInfoText.text = (PhotonNetwork.CountOfPlayers - PhotonNetwork.CountOfPlayersInRooms) + "로비 / " + PhotonNetwork.CountOfPlayers + "접속";
    }

    public void Connect() {
		PhotonNetwork.ConnectUsingSettings();
		PlayerPrefs.SetString("NAME", NickNameInput.text);
	}
    public override void OnConnectedToMaster() => PhotonNetwork.JoinLobby();

    public override void OnJoinedLobby()
    {
        LobbyPanel.SetActive(true);
        RoomPanel.SetActive(false);
        PhotonNetwork.LocalPlayer.NickName = NickNameInput.text;
        WelcomeText.text = PhotonNetwork.LocalPlayer.NickName + "님 환영합니다";
        myList.Clear();
    }

    public void Disconnect() => PhotonNetwork.Disconnect();

    public override void OnDisconnected(DisconnectCause cause)
    {
        LobbyPanel.SetActive(false);
        RoomPanel.SetActive(false);
    }
    #endregion


    #region 방
    public void CreateRoom() => PhotonNetwork.CreateRoom(RoomInput.text == "" ? "Room" + Random.Range(0, 100) : RoomInput.text, new RoomOptions { MaxPlayers = 2 });

    public void JoinRandomRoom() => PhotonNetwork.JoinRandomRoom();

    public void LeaveRoom() => PhotonNetwork.LeaveRoom();

    public override void OnJoinedRoom()
    {
		turn = 0;
        RoomPanel.SetActive(true);
        RoomRenewal();
        ChatInput.text = "";
        for (int i = 0; i < ChatText.Length; i++) ChatText[i].text = "";
		if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
		{
			isfull = true;
		}
		else
		{
			isfull = false;
		}
		if (PhotonNetwork.IsMasterClient)
		{
			myturn = 0;
		}
		else
		{
			myturn = 1;
		}

		resetgame();
		
	}

    public override void OnCreateRoomFailed(short returnCode, string message) { RoomInput.text = ""; CreateRoom(); } 

    public override void OnJoinRandomFailed(short returnCode, string message) { RoomInput.text = ""; CreateRoom(); }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
		turn = 0;
        RoomRenewal();
		PV.RPC("ChatRPC", RpcTarget.All, "<color=yellow>" + newPlayer.NickName + "님이 참가하셨습니다</color>");
		if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
		{
			isfull = true;
		}
		else
		{
			isfull = false;
		}
		resetgame();
	}

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
		turn = 0;
        RoomRenewal();
        ChatRPC("<color=yellow>" + otherPlayer.NickName + "님이 퇴장하셨습니다</color>");
		if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
		{
			isfull = true;
		}
		else
		{
			isfull = false;
		}
		resetgame();
	}

    void RoomRenewal()
    {
        ListText.text = "";
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
            ListText.text += PhotonNetwork.PlayerList[i].NickName + ((i + 1 == PhotonNetwork.PlayerList.Length) ? "" : ", ");
        RoomInfoText.text = PhotonNetwork.CurrentRoom.Name + " / " + PhotonNetwork.CurrentRoom.PlayerCount + "명 / " + PhotonNetwork.CurrentRoom.MaxPlayers + "최대";
    }
    #endregion


    #region 채팅
    public void Send()
    {
        PV.RPC("ChatRPC", RpcTarget.All, PhotonNetwork.NickName + " : " + ChatInput.text);
        ChatInput.text = "";
    }

    [PunRPC] // RPC는 플레이어가 속해있는 방 모든 인원에게 전달한다
    void ChatRPC(string msg)
    {
        bool isInput = false;
        for (int i = 0; i < ChatText.Length; i++)
            if (ChatText[i].text == "")
            {
                isInput = true;
                ChatText[i].text = msg;
                break;
            }
        if (!isInput) // 꽉차면 한칸씩 위로 올림
        {
            for (int i = 1; i < ChatText.Length; i++) ChatText[i - 1].text = ChatText[i].text;
            ChatText[ChatText.Length - 1].text = msg;
        }
    }
	#endregion


	#region 게임
	public void resetgame()
	{
		turn = 0;
		map = new int[3, 3];
		for (int i = 0; i < 81; i++)
		{
			tiles[i].GetComponent<Image>().sprite = null;
			tiles[i].GetComponent<Button>().interactable = true;
		}
		for (int i = 0; i < 9; i++)
		{
			masks[i].SetActive(false);
			masks[i].GetComponent<Image>().sprite = null;
		}
		if (PhotonNetwork.IsMasterClient)
			myturn = 0;
		else
			myturn = 1;
		if (myturn == turn)
			whosturn.text = "나의 턴";
		else
			whosturn.text = "상대방 턴";
	}
	// Update is called once per frame
	public void OnButton(int number)
	{
		if (myturn == turn && isfull)
		{
			int gridnum = number / 9;
			int tilenum = number % 9;
			PV.RPC("selectone", RpcTarget.All, number, turn);
		}
	}
	[PunRPC]
	public void selectone(int num, int turna)
	{
		isfirst = true;
		tiles[num].GetComponent<Button>().interactable = false;



		int gridnum = num / 9;
		int tilenum = num % 9;
		see = gridnum*1000+tilenum;
		if (turna == 0)
		{
			tiles[num].GetComponent<Image>().sprite = Otile;
			turn = 1;
		}
		else
		{
			tiles[num].GetComponent<Image>().sprite = Xtile;
			turn = 0;
		}

		for (int i = 0; i < 9; i++)
		{
			masks[i].SetActive(true);
		}
		masks[tilenum].SetActive(false);
		

		if (myturn == turn)
			whosturn.text = "나의 턴";
		else
			whosturn.text = "상대방 턴";
		int winn = 0;
		#region whowin
		int[,] pri = new int[3, 3]; //-1  =>방장    1=> 방원

		if (tiles[gridnum * 9].GetComponent<Image>().sprite != null)
			pri[0, 0] = (tiles[gridnum * 9].GetComponent<Image>().sprite == Otile) ? -1 : 1;   
		if (tiles[gridnum * 9+1].GetComponent<Image>().sprite != null)
			pri[0, 1] = (tiles[gridnum * 9+1].GetComponent<Image>().sprite == Otile) ? -1 : 1;
		if (tiles[gridnum * 9+2].GetComponent<Image>().sprite != null)
			pri[0, 2] = (tiles[gridnum * 9+2].GetComponent<Image>().sprite == Otile) ? -1 : 1;
		if (tiles[gridnum * 9+3].GetComponent<Image>().sprite != null)
			pri[1, 0] = (tiles[gridnum * 9+3].GetComponent<Image>().sprite == Otile) ? -1 : 1;
		if (tiles[gridnum * 9 + 4].GetComponent<Image>().sprite != null)
			pri[1, 1] = (tiles[gridnum * 9 + 4].GetComponent<Image>().sprite == Otile) ? -1 : 1;
		if (tiles[gridnum * 9 + 5].GetComponent<Image>().sprite != null)
			pri[1, 2] = (tiles[gridnum * 9 +5].GetComponent<Image>().sprite == Otile) ? -1 : 1;
		if (tiles[gridnum * 9 + 6].GetComponent<Image>().sprite != null)
			pri[2, 0] = (tiles[gridnum * 9 + 6].GetComponent<Image>().sprite == Otile) ? -1 : 1;
		if (tiles[gridnum * 9 +7].GetComponent<Image>().sprite != null)
			pri[2, 1] = (tiles[gridnum * 9 + 7].GetComponent<Image>().sprite == Otile) ? -1 : 1;
		if (tiles[gridnum * 9 + 8].GetComponent<Image>().sprite != null)
			pri[2, 2] = (tiles[gridnum * 9 + 8 ].GetComponent<Image>().sprite == Otile) ? -1 : 1;
		for (int i = 0; i < 3; i++)
		{
			if (pri[i, 0] != 0&& pri[i, 0]== pri[i, 1]&& pri[i, 1] == pri[i, 2])
			{
				if (pri[i,0]==-1)
				{
					winn = -1;
				}
				else if((pri[i, 0] == 1))
				{
					winn = 1;
				}
			}
			if (pri[0, i] != 0 && pri[0, i] == pri[1, i] && pri[1, i] == pri[2, i])
			{
				if (pri[0, i] == -1)
				{
					winn = -1;
				}
				else if((pri[0, i] == 1))
				{
					winn = 1;
				}
			}
		}
		if (pri[0, 0] != 0 && pri[1, 1] == pri[2, 2] && pri[1, 1] == pri[0, 0])
		{
			if (pri[0, 0] == -1)
			{
				winn = -1;
			}
			else if((pri[0, 0] == 1))
			{
				winn = 1;
			}
		}
		if(pri[0, 2] != 0 && pri[1, 1] == pri[2, 0] && pri[1, 1] == pri[0, 2])
		{
			if (pri[0, 2] == -1)
			{
				winn = -1;
			}
			else if ((pri[0, 2] == 1))
			{
				winn = 1;
			}
		}
		#endregion

		if (winn!=0)
		{
			if ((winn ==-1&&myturn==0)|| (winn == myturn))
			{
				PV.RPC("gridwin", RpcTarget.All, winn,gridnum,tilenum);
			}
		}


		if(!isfirst)
		{
			return;
		}
		if (map[tilenum / 3, tilenum % 3] != 0)
		{
			for (int i = 0; i < 9; i++)
			{

				masks[i].SetActive(false);
				if (map[i/3,i%3]!=0)
				{
					masks[i].SetActive(true);
				}
			}
		}
	}
	[PunRPC]
	public void gridwin(int winner, int gridnum, int tilenum)
	{
		if(winner==-1)
		{
			masks[gridnum].GetComponent<Image>().sprite = Otile;
		}
		else
		{
			masks[gridnum].GetComponent<Image>().sprite = Xtile;
		}
		map[gridnum / 3, gridnum % 3] = winner;
		if (map[tilenum / 3, tilenum % 3] != 0)
		{
			for (int i = 0; i < 9; i++)
			{

				masks[i].SetActive(false);
				if ((map[i / 3, i % 3] != 0))
				{
					masks[i].SetActive(true);
				}
			}
		}
		bool iswin = false;
		for (int i = 0; i < 3; i++)
		{
			if (map[i, 0] != 0 && map[i, 0] == map[i, 1] && map[i, 1] == map[i, 2])
			{
				iswin = true;
			}
			if (map[0, i] != 0 && map[0, i] == map[1, i] && map[1, i] == map[2, i])
			{
				iswin = true;
			}
		}
		if (map[0, 0] != 0 && map[1, 1] == map[2, 2] && map[1, 1] == map[0, 0])
		{
			iswin = true;
		}
		if (map[0, 2] != 0 && map[1, 1] == map[2, 0] && map[1, 1] == map[0, 2])
		{
			iswin = true;
		}
		if(iswin&&( (winner == -1 && myturn == 0) || (winner == myturn)))
		{
			PV.RPC("ChatRPC", RpcTarget.All, "<color=red>" + PhotonNetwork.LocalPlayer.NickName + "님이 승리하셨습니다.</color>");
			PV.RPC("win", RpcTarget.All, winner);
		}
			
			
		
	}
	[PunRPC]
	public void win(int winner)
	{
		isfirst = false;
		resetgame();
	}
	#endregion
}
