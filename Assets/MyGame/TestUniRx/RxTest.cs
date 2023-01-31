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
        //w“Ç‚ÍƒV[ƒ“‘JˆÚ‚µ‚Ä‚à‰½‚à‚µ‚È‚¯‚ê‚Î“o˜^‚ª‚³‚ê‚Á‚Ï‚È‚µ‚É‚È‚é
        //AddToiThisj‚Å“o˜^‚·‚é‘åÜƒIƒuƒWƒFƒNƒg‚ð“o˜^‚Å‚«‚éB
        //MonoŒp³‚µ‚È‚¢ê‡‚ÍƒV[ƒ“ƒRƒ“ƒgƒ[ƒ‰[‚ÅƒV[ƒ“‚ª”jŠü‚³‚ê‚½ê‡‚ÌƒV[ƒ“‚É“o˜^‚·‚éB
        score.Subscribe(_observe => UpdateScoreText()).AddTo(this);
        score.TakeUntilDestroy(this).Subscribe(_ => Debug.Log("’l‚ÌXV")); //”jŠü‚³‚ê‚é
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
