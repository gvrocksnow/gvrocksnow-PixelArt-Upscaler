using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Button_Quit : MonoBehaviour {

    public GameObject quitButton;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        if (Screen.fullScreen)
        {
            quitButton.SetActive(true);
        }
        else
        {
            quitButton.SetActive(false);
        }
	}


    public void CloseApp()
    {
        Application.Quit();
    }
}
