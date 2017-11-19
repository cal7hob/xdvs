using MiniJSON;
using UnityEngine;
using System;
using System.Collections;
[RequireComponent(typeof(CallbackPool))]
public class WebAvatarDownloader : MonoBehaviour {
    private CallbackPool callbackPool;
    private static WebAvatarDownloader instance;
	void Awake()
    {
            instance = this;
	        callbackPool = CallbackPool.instance;
	        callbackPool.initialize();
	        initHtmlJS();
    }
    public static WebAvatarDownloader Instance { get { return instance; } }
    void initHtmlJS()
    {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        string commandDownload = @"
            container.downloadImage = function (url, callback){
                console.log('downloading image: '+ url);
                var image = new Image();
                image.crossOrigin = 'Anonymous';
                image.onload = function() {
                    var canvas = document.createElement('canvas');
                    var ctx = canvas.getContext('2d');
                    canvas.width = image.naturalWidth;
                    canvas.height = image.naturalHeight;
                    ctx.drawImage(image, 0, 0);
                    try{
                        var pngBlob = canvas.toDataURL();
                    } catch(e){
                        callback('');
                    }
                    callback(pngBlob);
                };
                if(image.addEventListener) {
                    image.addEventListener('error', function (e) {
                        e.preventDefault();
                        callback('');
                    });
                } else {
                    image.attachEvent('onerror', function (e) {
                        callback('');
                        return false;
                    });
                }
                image.src = url;
            }";

        Application.ExternalEval(commandDownload);
#endif
    }
    public void Download(string url, Action<object, Callback> action)
    {
#if UNITY_WEBPLAYER || UNITY_WEBGL
        Callback callback = callbackPool.getCallback(CallbackType.DISPOSABLE);
        callback.action = action;
        string eval = @"			
			container._callbackCALLBACK_ID = function (result){ 
				container.callback(CALLBACK_ID, result); 
			}		
			container.downloadImage('URL', container._callbackCALLBACK_ID);"
            .Replace("URL", url)
            .Replace("CALLBACK_ID", "" + callback.id);

        Application.ExternalEval(eval);
#endif
    }
}
