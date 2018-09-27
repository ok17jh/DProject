using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RedTheSettlers.GameSystem
{
    public class LoadingStageState : State
    {
        public override void Enter(bool LoadData)
        {
            
        }

        public override void Exit(StageType stageType)
        {
            StageManager.Instance.ChangeStage(StageType.BoardStage);
        }
    }
}