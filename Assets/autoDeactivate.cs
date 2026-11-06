using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class autoDeactivate : MonoBehaviour
{
    
    private void OnEnable()
    {
        
        Invoke("Deactivate", 3f);
    }

    void Deactivate()
    {
        this.gameObject.SetActive(false);
    }
}
