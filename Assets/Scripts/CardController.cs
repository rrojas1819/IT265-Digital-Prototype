using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardController : MonoBehaviour
{
    public int card_number;
    public Sprite card_sprite;

    void Start()
    {
        this.GetComponent<Image>().sprite = card_sprite;
    }
}
