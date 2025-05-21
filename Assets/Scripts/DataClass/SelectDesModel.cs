using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectDesModel
{
    public string title;
    public Sprite image;
    public string imageUrl;
    public System.Action onClickAction;

    public static List<SelectDesModel> GenerateFakeList()
    {
        var list = new List<SelectDesModel>();
        var selecDes = new SelectDesModel();
        selecDes.title = "test";
        selecDes.imageUrl = "test";
        list.Add(selecDes);
        return list;
    }
}
