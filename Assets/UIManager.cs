using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public TMP_Dropdown.DropdownEvent onValueChangedTMP;
    
    TMP_Dropdown dropdown;

    private void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        dropdown.onValueChanged = onValueChangedTMP;
        
    }
    void Start()
    {
        //Initialise the Text to say the first value of the Dropdown
        dropdown.options.Add(new TMP_Dropdown.OptionData("Calculate via pointIndex"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("Permutate all, find shortest path"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("Ant Colony Optimazation"));
    }

    //Ouput the new value of the Dropdown into Text
    void OnDropdownValueChanged(int index)
    {
        // Handle the dropdown value change
        Debug.Log($"Selected option: {dropdown.options[index].text}");
    }
}
