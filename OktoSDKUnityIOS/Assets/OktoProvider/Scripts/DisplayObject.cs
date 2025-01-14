using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DisplayObject : MonoBehaviour
{
    [SerializeField] private TMP_Text displayName;
    [SerializeField] private TMP_Text displayAddress;
    [SerializeField] private TMP_Text displayNetwork;
    public void setup(string name, string address, string network)
    {
        displayName.text = name;
        displayAddress.text = address;
        displayNetwork.text = network;
    }
}
