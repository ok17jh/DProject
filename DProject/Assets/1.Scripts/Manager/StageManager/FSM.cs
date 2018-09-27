using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RedTheSettlers.GameSystem
{
    public class FSM
    {
        private State currentState;
        public State CurrentState { get { return currentState; } }

        public void Enter(bool LoadData)
        {
            if (currentState != null)
            {
                StageManager.Instance.DiscriminateDataLoading();
                currentState.Enter(LoadData);
            }
            else
            {
                Debug.Log("어떤 입력값인지 알수없습니다.");
            }
            
        }

        public void Exit(StageType stageType)
        {
            if (currentState != null)
            {
                StageManager.Instance.ChangeStage(stageType);
                currentState.Exit(stageType);
            }
            else
            {
                Debug.Log("Stage를 찾을수 없습니다.");
            }
        }
        
    }
}

