
using UnityEngine;

public abstract class StateMachine : MonoBehaviour
{

    protected string strState = "IDLE";

    protected abstract string UpdateState();
    protected abstract void UpdateHandle();

    protected void FixedUpdate()
    {

        UpdateHandle();
        strState = UpdateState();
    }
}
