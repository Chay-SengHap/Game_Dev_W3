using UnityEngine;
using UnityEngine.InputSystem; // 1. Add this namespace

public class PlayerMovement : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("Helllo");
        Debug.Log("hello Unity");
    }

    // Update is called once per frame
    void Update()
    {
        if(Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("space bar pressed");
        }
    }
}
