#import <Foundation/Foundation.h>
#import <BugfenderSDK/BugfenderSDK.h>

char* convertNSStringToCString(const NSString* nsString)
{
    if (nsString == NULL)
        return NULL;

    const char* nsStringUtf8 = [nsString UTF8String];
    //create a null terminated C string on the heap so that our string's memory isn't wiped out right after method's return
    char* cString = (char*)malloc(strlen(nsStringUtf8) + 1);
    strcpy(cString, nsStringUtf8);

    return cString;
}

extern "C" {
void BugfenderActivateLogger(const char* key, bool printToConsole, bool hideDeviceName, const char* apiURL, const char* baseURL) {
    NSString* apiURLString = [NSString stringWithUTF8String:apiURL];
    if(apiURLString.length > 0) {
        [Bugfender setApiURL:[NSURL URLWithString:apiURLString]];
    }
    NSString* baseURLString = [NSString stringWithUTF8String:baseURL];
    if(baseURLString.length > 0) {
        [Bugfender setBaseURL:[NSURL URLWithString:baseURLString]];
    }
    if(hideDeviceName)
        [Bugfender overrideDeviceName:@"Unknown"];
    [Bugfender activateLogger:[NSString stringWithUTF8String:key]];
    [Bugfender setPrintToConsole:printToConsole];
}

void BugfenderEnableUIEventLogging() {
    [Bugfender enableUIEventLogging];
}

void BugfenderEnableCrashReporting() {
    [Bugfender enableCrashReporting];
}

void BugfenderSetDeviceString(const char* key, const char* value) {
    [Bugfender setDeviceString:[NSString stringWithUTF8String:value] forKey:[NSString stringWithUTF8String:key]];
}

void BugfenderRemoveDeviceKey(const char* key) {
    [Bugfender removeDeviceKey:[NSString stringWithUTF8String:key]];
}

void BugfenderLog(int logLevel, const char* tag, const char* message) {
    [Bugfender logWithLineNumber:0 method:@"" file:@"" level:(BFLogLevel)logLevel tag:[NSString stringWithUTF8String:tag] message:[NSString stringWithUTF8String:message]];
}

char* BugfenderSendCrash(const char* title, const char* text) {
    NSURL* url = [Bugfender sendCrashWithTitle:[NSString stringWithUTF8String:title] text:[NSString stringWithUTF8String:text]];
    return convertNSStringToCString([url absoluteString]);
}

char* BugfenderSendIssue(const char* title, const char* markdown) {
    NSURL* url = [Bugfender sendIssueReturningUrlWithTitle:[NSString stringWithUTF8String:title] text:[NSString stringWithUTF8String:markdown]];
    return convertNSStringToCString([url absoluteString]);
}

char* BugfenderSendUserFeedback(const char* subject, const char* message) {
    NSURL* url = [Bugfender sendUserFeedbackReturningUrlWithSubject:[NSString stringWithUTF8String:subject] message:[NSString stringWithUTF8String:message]];
    return convertNSStringToCString([url absoluteString]);
}

void BugfenderSetMaximumLocalStorageSize(unsigned long maximumLocalStorageSizeBytes) {
    [Bugfender setMaximumLocalStorageSize:maximumLocalStorageSizeBytes];
}

char* BugfenderGetDeviceIdentifierUrl() {
    NSURL* url = [Bugfender deviceIdentifierUrl];
    return convertNSStringToCString([url absoluteString]);
}

char* BugfenderGetSessionIdentifierUrl() {
    NSURL* url = [Bugfender sessionIdentifierUrl];
    return convertNSStringToCString([url absoluteString]);
}

void BugfenderSetForceEnabled(bool enabled) {
    [Bugfender setForceEnabled:enabled];
}

void BugfenderForceSendOnce() {
    [Bugfender forceSendOnce];
}

}
