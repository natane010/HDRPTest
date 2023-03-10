using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;

public class RxTest : MonoBehaviour
{
    [SerializeField] Text scoreText;
    [SerializeField] Button itemButon;
    [SerializeField] Button enemyButton;
    //IntReactiveProperty score;
    [SerializeField] ReactiveProperty<int> score = new ReactiveProperty<int>(0);
    
    private void Awake()
    {
        itemButon.onClick.AddListener(OnItem);
        enemyButton.onClick.AddListener(OnEnemy);
        //購読はシーン遷移しても何もしなければ登録がされっぱなしになる
        //AddTo（This）で登録する大賞オブジェクトを登録できる。
        //Mono継承しない場合はシーンコントローラーでシーンが破棄された場合のシーンに登録する。
        score.Subscribe(_observe => UpdateScoreText()).AddTo(this);
        score.TakeUntilDestroy(this).Subscribe(_ => Debug.Log("値の更新")); //破棄される
    }
    private void Start()
    {
        //RXNetWorkManager.Instance.GetObserver().TakeUntilDestroy(this)
        //    .Subscribe(
        //    _response => Debug.Log(_response),
        //    _ => Debug.Log("error"),
        //    () => Debug.Log("complite"));
        RXNetWorkManager.Instance.Request("http://localhost/sample.json");
    }
    void OnEnemy()
    {
        score.Value += 5;
    }
    void OnItem()
    {
        score.Value += 1;
    }
    void UpdateScoreText()
    {
        scoreText.text = $"SCORE:{score.Value}";
    }

    
    private void OnDestroy()
    {
        
    }
}
