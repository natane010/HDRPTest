using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UnityEngine.UI;

public class RxTimer : MonoBehaviour
{
    public Button button1;
    public Button button2;

    public Button button3;

    IDisposable disposable;
    bool isButton;
    private void Start()
    {
        button1.onClick.AddListener(OnClickButton1);
        button2.onClick.AddListener(OnClickButton2);
        button3.onClick.AddListener(OnClickButton3);
    }
    private void OnClickButton1()
    {
        Observable.Timer(TimeSpan.FromSeconds(2)).Subscribe(_ =>
        //Debug.Log("ボタン１をクリックしました。")
        { 
            button1.GetComponent<Image>().color = Color.red;
            Observable.Timer(TimeSpan.FromSeconds(2)).Subscribe(_ =>
            button1.GetComponent<Image>().color = Color.blue
            ).AddTo(this);
        }
        ).AddTo(this);

    }

    private void OnClickButton2()
    {
        
        if(isButton)
        {
            isButton = false;
            button2.GetComponentInChildren<Text>().text = "ボタン開始";
            disposable.Dispose();
            return;
        }
        disposable = Observable.Interval(TimeSpan.FromSeconds(2))
            .Subscribe(x => {
                button2.GetComponentInChildren<Text>().text = "ボタン停止";
                Debug.Log("2が押されています。");
                isButton = true;
                }
            )
            .AddTo(this);
    }
    private void OnClickButton3()
    {
        int count = 30;
        button3.GetComponentInChildren<Text>().text = $"{count}";
        Observable.Interval(TimeSpan.FromSeconds(1))
            .Take(30)
            //TakeWhile(value => value < 30)
            .Subscribe(_ =>
            {
                count--;
                button3.GetComponentInChildren<Text>().text = $"{count}";
            },
            () => Debug.Log("完了")
            ).AddTo(this);

        //Observable.Interval(TimeSpan.FromSeconds(1))
        //    .Where(_ => count >= 30)
        //    .Subscribe(_ =>
        //    {
        //        count--;
        //        button3.GetComponentInChildren<Text>().text = $"{count}";
        //    }).AddTo(this);
    }
}
