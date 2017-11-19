using UnityEngine;
using System.Collections;

public class WebGLKeyboardFix: MonoBehaviour {
#if UNITY_WEBGL
    private tk2dUITextInput tk2dinput;
    private Callback callback;
    private static int counter;
    private string input_id;
    private bool html_focus;
	void Start ()
	{
        tk2dinput = FindObjectOfType<tk2dUITextInput>();
	    if (null == tk2dinput)
	    {
	        DestroyImmediate(this);
	        return;
	    }
        callback = CallbackPool.instance.getCallback(CallbackType.PERMANENT);
        callback.action = delegate(object o, Callback callback1)
        {
            tk2dinput.Text = (string)o;
            tk2dinput.OnTextChange(tk2dinput);
        };
        input_id = "WebGLKeyboardInput" + counter++;
	    InitHtml();
        
	}
    void OnDestroy()
    {
        if (null == tk2dinput)
        {
            return;
        } 
        CallbackPool.instance.releasePermanentCallback(callback);
        ClearHtml();
    }
    private void InitHtml()
    {
        var eval = @"
            $('body').prepend(""<input id='{0}' type='text' name='WebGLKeyboardInput' value='' style='border: none; position: absolute; margin-top: -100px;'/>"");
            var webGLInput = document.getElementById('{0}');
            var on_input = function (e){{
                if(e.keyCode == 32) {{
                    webGLInput.value = webGLInput.value + ' ';
                }}
                else if(e.keyCode == 8) {{
                    webGLInput.value = webGLInput.value.substring(0, webGLInput.value.length - 1);
                }}
                container.callback({1}, webGLInput.value); 
            }}   
            webGLInput.addEventListener('keyup', on_input);
        ";
        eval = string.Format(eval, input_id, callback.id);
        Application.ExternalEval(eval);
    }
    private void ClearHtml()
    {
        var eval = string.Format("$('#{0}').remove();", input_id);
        Application.ExternalEval(eval);
    }

    void focus()
    {
#if !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = false;
#endif
        var eval = @"
            var webGLInput = document.getElementById('{0}');
            webGLInput.value = '{1}';
            webGLInput.focus();
        ";
        eval = string.Format(eval, input_id, tk2dinput.Text);
        Application.ExternalEval(eval);
        html_focus = true;
    }
    void unfocus()
    {
#if !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = true;
#endif
        html_focus = false;
    }
	void Update () 
    {
	    if (tk2dinput.IsFocus && !html_focus)
	    {
	        focus();
	    }
        else if (!tk2dinput.IsFocus && html_focus)
        {
            unfocus();
        }
	}
#endif
}
