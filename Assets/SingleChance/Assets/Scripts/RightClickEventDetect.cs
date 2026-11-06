using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SingleChance
{
    public class RightClickEventDetect : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                Debug.Log("Left click");
            else if (eventData.button == PointerEventData.InputButton.Middle)
                Debug.Log("Middle click");
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                GetPoints.getPoints.ClearParticularObject(this.gameObject);
                Debug.Log("Right click");
            }

        }
    }
}