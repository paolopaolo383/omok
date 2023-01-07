using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics.Tracing;

public class gamemanager : MonoBehaviour
{
	public NetworkManager networkManager;
	public GameObject[] masks;
	public GameObject[] tiles = new GameObject[82];
	// Start is called before the first frame update
	void Awake()
	{
	}

	void Start()
    {
        
    }
	public void OnButton(int number)
	{
		networkManager.OnButton(number);
	}
}
