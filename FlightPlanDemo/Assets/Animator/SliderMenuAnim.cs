using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliderMenuAnim : MonoBehaviour
{
    public GameObject PanelMenu;
    public void ShowHideMenu(){
        if(PanelMenu != null){
            Animator animator = PanelMenu.GetComponent<Animator>();
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            if(animator == null){
                Debug.Log("Animator is NULL");
            }
            if(animator != null){
                Debug.Log("Animator is not NULL");
                bool isOpen = animator.GetBool("show");
                Debug.Log("isOpen = " + isOpen);
                animator.SetBool("show", !isOpen);
            }
        }
    }
}
