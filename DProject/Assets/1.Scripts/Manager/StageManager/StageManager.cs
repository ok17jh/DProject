using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 담당자 : 이재환
/// 수정시 간략 설명과 수정 날짜 
/// {
///   Ex : 함수명 변경 18/07/15
///   Context 부분 
///   
/// }
/// </summary>


namespace RedTheSettlers.GameSystem
{
    public enum StageType
    {
        TitleStage,
        LoadingStage,
        BoardStage
    }
    public enum StateType
    {
        BoardState,
        BattleState,
        TutorialState
    }

    public class StageManager : Singleton<StageManager>
    {
        private FSM fsm;
        public FSM Fsm { get { return fsm; } }

        public void DiscriminateDataLoading()
        {

        }


        public IEnumerator ChangeStage(StageType stageType)
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(stageType.ToString());
            loadOperation.allowSceneActivation = false;

            while (!loadOperation.isDone)
            {
                yield return new WaitForSeconds(0.5f);
                if (loadOperation.progress >= 0.9f)
                {
                    loadOperation.allowSceneActivation = true;
                }
            }
           

        }
    }
}


