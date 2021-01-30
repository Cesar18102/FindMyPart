using System.Collections;
using System.Collections.Generic;

using UnityEngine;


public class GameController : MonoBehaviour {

    public GameStateInfo _GameStateInfo { get; set; }
    public GameObject Player { get; set; }
    public PlayerController _PlayerController { get; set; }

	// Use this for initialization
	void Start () {
        _GameStateInfo = new GameStateInfo();
        _GameStateInfo._PlayerStateInfo = new PlayerStateInfo();

        Player = GameObject.Find("Player");
        _PlayerController = Player.GetComponent<PlayerController>();
        _PlayerController._PlayerStateInfo = _GameStateInfo._PlayerStateInfo;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
