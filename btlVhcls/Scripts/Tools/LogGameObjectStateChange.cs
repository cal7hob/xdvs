using UnityEngine;
using System.Collections;

/// <summary>
/// Типа вешаешь на объект который хз кем выключается - и смотришь стек трейс
/// </summary>
public class LogGameObjectStateChange : MonoBehaviour
{
	void OnEnable ()
    {
        Debug.LogErrorFormat("{0} OnEnable", MiscTools.GetFullTransformName(transform));
	}
	
	void OnDisable ()
    {
        Debug.LogErrorFormat("{0} OnDisable", MiscTools.GetFullTransformName(transform));
    }
}
