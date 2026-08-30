using UnityEngine;

public class OnRebornClicked : MonoBehaviour
{

public void RebornClicked()
{
    GameManager.Instance.CancelInvoke();
    
}

}