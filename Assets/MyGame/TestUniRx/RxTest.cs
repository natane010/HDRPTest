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
        //�w�ǂ̓V�[���J�ڂ��Ă��������Ȃ���Γo�^��������ςȂ��ɂȂ�
        //AddTo�iThis�j�œo�^�����܃I�u�W�F�N�g��o�^�ł���B
        //Mono�p�����Ȃ��ꍇ�̓V�[���R���g���[���[�ŃV�[�����j�����ꂽ�ꍇ�̃V�[���ɓo�^����B
        score.Subscribe(_observe => UpdateScoreText()).AddTo(this);
        score.TakeUntilDestroy(this).Subscribe(_ => Debug.Log("�l�̍X�V")); //�j�������
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
