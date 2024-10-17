using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class UI : MonoBehaviour
{
    public Action<bool[]> CalculationType;
    public Dropdown dropdown;
    bool[] calculationType = new bool[2]; // 0 - Coroutine, 1 - NonCoroutine
    void Start()
    {
        // Add listener for when the value of the Dropdown changes
        dropdown.onValueChanged.AddListener(delegate {
            DropdownValueChanged(dropdown);
        });
    }

    private void Update()
    {
        
    }
    void DropdownValueChanged(Dropdown change)
    {
        Debug.Log("New Dropdown Value: " + change.value);
    }
}
