//
//  Starter.m
//  Unity-iPhone
//
//  Created by Admin on 25.05.15.
//
//

#import "VKForUnity.h"
#import "VKSdk.h"

static VKForUnity *_instance = [VKForUnity sharedInstance];

@interface VKForUnity ()<UIAlertViewDelegate, VKSdkUIDelegate>
  
@end

@implementation VKForUnity

+(VKForUnity *)sharedInstance
{
    return _instance;
}

+ (void)initialize
{
    if(!_instance) {
        _instance = [[VKForUnity alloc] init];
    }
}

- (id)init
{
    if(_instance != nil) {
        return _instance;
    }
    
    if ((self = [super init])) {
        _instance = self;
        
    }
    return self;
}

-(void)vkSdkAccessAuthorizationFinishedWithResult:(VKAuthorizationResult *) result
{
    VKAccessToken *token = result.token;
    NSError *error = result.error;
    NSMutableDictionary *dict = [[NSMutableDictionary alloc] initWithCapacity:2];
    if(error)
    {
        NSString *errorString = [error.userInfo objectForKey:NSLocalizedDescriptionKey];
        [dict setObject:errorString forKey:@"error"];
    }
    if(token)
    {
        [dict setObject:token.accessToken forKey:@"token"];
    }
    
    NSError *writeError = nil;
    NSData *jsonData = [NSJSONSerialization dataWithJSONObject:dict
                                                       options:NSJSONReadingMutableContainers
                                                         error:&writeError];
    NSString *jsonString = [[NSString alloc] initWithData:jsonData
                                                 encoding:NSUTF8StringEncoding];
    UnitySendMessage("VkMessageHandler", "OnAuthorizationFinished",[jsonString cStringUsingEncoding:NSASCIIStringEncoding]);
}

-(void)vkSdkUserAuthorizationFailed
{
    UnitySendMessage("VkMessageHandler", "OnAuthorizationFailed","");
}

-(void)vkSdkAccessTokenUpdated:(VKAccessToken *)newToken oldToken:(VKAccessToken *)oldToken
{
    NSMutableDictionary *dict = [[NSMutableDictionary alloc] initWithCapacity:2];
    if(newToken)
    {
        [dict setObject:newToken.accessToken forKey:@"newToken"];
    }
    if(oldToken)
    {
        [dict setObject:oldToken.accessToken forKey:@"oldToken"];
    }
    
    NSError *writeError = nil;
    NSData *jsonData = [NSJSONSerialization dataWithJSONObject:dict
                                                       options:NSJSONReadingMutableContainers
                                                         error:&writeError];
    NSString *jsonString = [[NSString alloc] initWithData:jsonData
                                                 encoding:NSUTF8StringEncoding];
    UnitySendMessage("VkMessageHandler", "OnTokenUpdated",[jsonString cStringUsingEncoding:NSASCIIStringEncoding]);
}

-(void)vkSdkTokenHasExpired:(VKAccessToken *)expiredToken
{
    UnitySendMessage("VkMessageHandler", "OnTokenExpired",[expiredToken.accessToken cStringUsingEncoding:NSASCIIStringEncoding]);
}

- (void)vkSdkNeedCaptchaEnter:(VKError *)captchaError
{
    VKCaptchaViewController *vc = [VKCaptchaViewController captchaControllerWithError:captchaError];
    [vc presentIn:UnityGetGLViewController()];
}

- (void)vkSdkShouldPresentViewController:(UIViewController *)controller
{
    [UnityGetGLViewController() presentViewController:controller animated:YES completion:nil];
}

-(void)alertView:(UIAlertView *)alertView didDismissWithButtonIndex:(NSInteger)buttonIndex
{
}

//-(void)dealloc{
//    [super dealloc];
//}
-(void) Initialize:(const char*)appId
{
    NSArray *scope = @[VK_PER_NOTIFICATIONS,VK_PER_FRIENDS,VK_PER_PHOTOS,VK_PER_WALL,VK_PER_GROUPS,VK_PER_OFFLINE];
    NSString *appIdString = [[NSString alloc] initWithUTF8String:appId];
    VKSdk *sdkInstance = [VKSdk initializeWithAppId:appIdString];
    [sdkInstance registerDelegate:self];
    [sdkInstance setUiDelegate:self];
    [VKSdk wakeUpSession:scope completeBlock:^(VKAuthorizationState state, NSError *error) {
        UnitySendMessage("VkMessageHandler", "OnInitializationFinished", "xyn");
        if(state == VKAuthorizationAuthorized)
        {
            NSMutableDictionary *dict = [[NSMutableDictionary alloc] initWithCapacity:2];
            
            [dict setObject:VKSdk.accessToken.accessToken forKey:@"token"];
            
            
            NSError *writeError = nil;
            NSData *jsonData = [NSJSONSerialization dataWithJSONObject:dict
                                                               options:NSJSONReadingMutableContainers
                                                                 error:&writeError];
            NSString *jsonString = [[NSString alloc] initWithData:jsonData
                                                         encoding:NSUTF8StringEncoding];
            UnitySendMessage("VkMessageHandler", "OnAuthorizationFinished",[jsonString cStringUsingEncoding:NSASCIIStringEncoding]);
        }
    }];
}

-(void) Login
{
    NSArray *scope = @[VK_PER_NOTIFICATIONS,VK_PER_FRIENDS,VK_PER_PHOTOS,VK_PER_WALL,VK_PER_GROUPS,VK_PER_OFFLINE];
    [VKSdk authorize: scope];
}

-(void) ShowShareDialog:(const char*)text
                    img:(const char*)img
                photoId:(const char*)photoId
         attachmentText:(const char*)attachmentText
         attachmentLink:(const char*)attachmentLink
{
    VKShareDialogController *shareDialog = [VKShareDialogController new];
    shareDialog.text = [[NSString alloc] initWithUTF8String:text];
    if(photoId != NULL)
    {
        shareDialog.vkImages = @[[[NSString alloc]initWithUTF8String:photoId]];
    }
    if(img != NULL)
    {
        NSString *b64img = [[NSString alloc] initWithUTF8String:img];
        NSData *decodedData = [[NSData alloc] initWithBase64EncodedString:b64img options:NSDataBase64DecodingIgnoreUnknownCharacters];
        UIImage *image = [UIImage imageWithData: decodedData];
        shareDialog.uploadImages = @[[VKUploadImage uploadImageWithImage:image andParams:[VKImageParameters pngImage]]];
    }
    shareDialog.shareLink = [[VKShareLink alloc] initWithTitle:[[NSString alloc] initWithUTF8String:attachmentText] link:[NSURL URLWithString:[[NSString alloc] initWithUTF8String:attachmentLink]]];
    [shareDialog setCompletionHandler:^(VKShareDialogController *controller,VKShareDialogControllerResult result) {
        [UnityGetGLViewController() dismissViewControllerAnimated:YES completion:nil];
    }];
    [UnityGetGLViewController() presentViewController:shareDialog animated:YES completion:nil];
}

-(bool) IsLoggedIn
{
    return [VKSdk isLoggedIn];
}

-(void) Logout
{    
    [VKSdk forceLogout];
}

@end

//Unity C# interface

extern "C"
{
    char* cStringCopy(const char* string)
    {
        if(string == NULL)
            return NULL;
        char* res = (char*)malloc(strlen(string)+1);
        strcpy(res, string);
        return res;
        
    }
    
    void _Initialize(const char* appId){
        [[VKForUnity sharedInstance] Initialize:appId];
        
    }
    
    bool _IsLoggedIn()
    {
        return [[VKForUnity sharedInstance] IsLoggedIn];
    }
    
    void _Login()
    {
        [[VKForUnity sharedInstance] Login];
    }
    
    void _Logout()
    {
        [[VKForUnity sharedInstance] Logout];
    }
    
    void _ShowShareDialog(const char* text, const char* img, const char* photoId, const char* attachmentText, const char* attachmentLink)
    {
        [[VKForUnity sharedInstance] ShowShareDialog:text img:img photoId:photoId attachmentText:attachmentText attachmentLink:attachmentLink];
    }
    
    const char* _GetAccessToken()
    {
        const char *token = [[[VKSdk accessToken] accessToken] cStringUsingEncoding:NSUTF8StringEncoding];
        return cStringCopy(token);
    }
	const char* _GetUid()
    {
        const char *uid = [[[VKSdk accessToken] userId] cStringUsingEncoding:NSUTF8StringEncoding];
        return cStringCopy(uid);
    }
}
