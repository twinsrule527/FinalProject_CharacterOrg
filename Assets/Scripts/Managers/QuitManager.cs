using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Very simple class: if being run on an independent window, allows escape key to quit
public class QuitManager : MonoBehaviour
{
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape)) {
            if(Application.platform == RuntimePlatform.WindowsPlayer) {
                Application.Quit();
            }
        }
    }
}
