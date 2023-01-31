using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UniRx;
using System;

public class RXNetWorkManager : MonoBehaviour
{
   public static RXNetWorkManager Instance { private set; get; }
     Subject<string> subject = new Subject<string>();
    public IObserver<string> GetObserver()
    {
        return subject;
    }
    private void Awake()
    {
        Instance = this;
    }
    public void Request(string url) 
    {
        StartCoroutine(RequestImpl(url));
    }
    IEnumerator RequestImpl(string url)
    {
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();
        string text = www.downloadHandler.text;
        subject.OnNext(text);
        //äÆóπÇ∑ÇÈÇÃÇ≈çƒìxåƒÇŒÇÍÇ»Ç¢
        //subject.OnCompleted();
    }

}

