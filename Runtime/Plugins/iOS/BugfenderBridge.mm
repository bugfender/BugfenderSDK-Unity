#import <Foundation/Foundation.h>
#import <BugfenderSDK/BugfenderSDK.h>
#include <stdlib.h>
#include <string.h>

NSString* convertCStringToNSString(const char* s)
{
    if(s == NULL) {
        return nil;
    }
    return [NSString stringWithUTF8String:s];
}

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
void BugfenderSetSDKType(const char* sdkType, int version) {
    SEL selector = NSSelectorFromString(@"setSDKType:version:");
    if ([Bugfender respondsToSelector:selector]) {
        typedef void (*SetSDKTypeFunc)(id, SEL, NSString*, int);
        SetSDKTypeFunc invoke = (SetSDKTypeFunc)[Bugfender methodForSelector:selector];
        invoke((id)[Bugfender class], selector, convertCStringToNSString(sdkType), version);
    }
}

void BugfenderActivateLogger(const char* key, bool printToConsole, bool hideDeviceName, const char* apiURL, const char* baseURL) {
    NSString* apiURLString = convertCStringToNSString(apiURL);
    if(apiURLString.length > 0) {
        [Bugfender setApiURL:[NSURL URLWithString:apiURLString]];
    }
    NSString* baseURLString = convertCStringToNSString(baseURL);
    if(baseURLString.length > 0) {
        [Bugfender setBaseURL:[NSURL URLWithString:baseURLString]];
    }
    if(hideDeviceName)
        [Bugfender overrideDeviceName:@"Unknown"];
    [Bugfender activateLogger:convertCStringToNSString(key)];
    [Bugfender setPrintToConsole:printToConsole];
}

void BugfenderEnableUIEventLogging() {
    [Bugfender enableUIEventLogging];
}

void BugfenderEnableCrashReporting() {
    [Bugfender enableCrashReporting];
}

void BugfenderEnableNSLogLogging(void) {
    if (@available(iOS 15.0, *)) {
        [Bugfender enableNSLogLogging];
    }
}

void BugfenderSetDeviceString(const char* key, const char* value) {
    [Bugfender setDeviceString:convertCStringToNSString(value) forKey:convertCStringToNSString(key)];
}

void BugfenderRemoveDeviceKey(const char* key) {
    [Bugfender removeDeviceKey:convertCStringToNSString(key)];
}

void BugfenderLog(int logLevel, const char* tag, const char* message) {
    [Bugfender logWithLineNumber:0 method:@"" file:@"" level:(BFLogLevel)logLevel tag:convertCStringToNSString(tag) message:convertCStringToNSString(message)];
}

char* BugfenderSendCrash(const char* title, const char* text) {
    NSURL* url = [Bugfender sendCrashWithTitle:convertCStringToNSString(title) text:convertCStringToNSString(text)];
    return convertNSStringToCString([url absoluteString]);
}

char* BugfenderSendIssue(const char* title, const char* text) {
    NSURL* url = [Bugfender sendIssueReturningUrlWithTitle:convertCStringToNSString(title) text:convertCStringToNSString(text)];
    return convertNSStringToCString([url absoluteString]);
}

char* BugfenderSendUserFeedback(const char* subject, const char* message) {
    NSURL* url = [Bugfender sendUserFeedbackReturningUrlWithSubject:convertCStringToNSString(subject) message:convertCStringToNSString(message)];
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

char* BugfenderGetSessionIdentifier() {
    // sessionIdentifier is deprecated but still the reliable UUID source for correlation headers.
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    NSString* sessionId = [Bugfender sessionIdentifier];
#pragma clang diagnostic pop
    return convertNSStringToCString(sessionId);
}

void BugfenderSetNetworkLoggingEnabled(bool enabled) {
    if ([Bugfender respondsToSelector:@selector(setNetworkLoggingEnabled:)]) {
        [Bugfender setNetworkLoggingEnabled:enabled];
    }
}

void BugfenderSetNetworkLoggingCaptureBodies(bool capture) {
    if ([Bugfender respondsToSelector:@selector(setNetworkLoggingCaptureBodies:)]) {
        [Bugfender setNetworkLoggingCaptureBodies:capture];
    }
}

void BugfenderSetNetworkLoggingCaptureErrorResponseBodies(bool capture) {
    if ([Bugfender respondsToSelector:@selector(setNetworkLoggingCaptureErrorResponseBodies:)]) {
        [Bugfender setNetworkLoggingCaptureErrorResponseBodies:capture];
    }
}

NSArray<NSString *>* patternsFromJoinedString(const char* joined) {
    NSString* text = convertCStringToNSString(joined);
    if (text.length == 0) {
        return nil;
    }
    NSArray<NSString *>* parts = [text componentsSeparatedByString:@"\n"];
    NSMutableArray<NSString *>* patterns = [NSMutableArray array];
    for (NSString* part in parts) {
        NSString* trimmed = [part stringByTrimmingCharactersInSet:[NSCharacterSet whitespaceAndNewlineCharacterSet]];
        if (trimmed.length > 0) {
            [patterns addObject:trimmed];
        }
    }
    return patterns.count > 0 ? patterns : nil;
}

void BugfenderSetNetworkLoggingURLFilter(const char* allowlistJoined, const char* denylistJoined) {
    if (![Bugfender respondsToSelector:@selector(setNetworkLoggingURLFilterWithAllowlist:denylist:)]) {
        return;
    }
    [Bugfender setNetworkLoggingURLFilterWithAllowlist:patternsFromJoinedString(allowlistJoined)
                                              denylist:patternsFromJoinedString(denylistJoined)];
}

void BugfenderSetNetworkLoggingMaxRequestsPerMinute(int countOrNegative) {
    if (![Bugfender respondsToSelector:@selector(setNetworkLoggingMaxRequestsPerMinute:)]) {
        return;
    }
    NSNumber* value = countOrNegative < 0 ? nil : @(countOrNegative);
    [Bugfender setNetworkLoggingMaxRequestsPerMinute:value];
}

typedef char* (*BugfenderUnityRequestObfuscationCallback)(const char* url, const char* headersJson, const char* body);
typedef char* (*BugfenderUnityResponseObfuscationCallback)(const char* headersJson, const char* body);

static BugfenderUnityRequestObfuscationCallback s_requestObfuscationCallback = NULL;
static BugfenderUnityResponseObfuscationCallback s_responseObfuscationCallback = NULL;

static NSString* jsonFromDictionary(NSDictionary<NSString *, NSString *>* headers) {
    NSDictionary* source = headers ?: @{};
    NSData* data = [NSJSONSerialization dataWithJSONObject:source options:0 error:nil];
    if (data == nil) {
        return @"{}";
    }
    return [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding] ?: @"{}";
}

static NSDictionary* parseObfuscationPayload(const char* jsonCString) {
    if (jsonCString == NULL) {
        return nil;
    }
    NSString* json = [NSString stringWithUTF8String:jsonCString];
    free((void*)jsonCString);
    if (json.length == 0) {
        return nil;
    }
    NSData* data = [json dataUsingEncoding:NSUTF8StringEncoding];
    if (data == nil) {
        return nil;
    }
    id object = [NSJSONSerialization JSONObjectWithData:data options:0 error:nil];
    return [object isKindOfClass:[NSDictionary class]] ? (NSDictionary*)object : nil;
}

void BugfenderRegisterNetworkRequestObfuscationCallback(BugfenderUnityRequestObfuscationCallback callback) {
    s_requestObfuscationCallback = callback;
}

void BugfenderRegisterNetworkResponseObfuscationCallback(BugfenderUnityResponseObfuscationCallback callback) {
    s_responseObfuscationCallback = callback;
}

void BugfenderSetNetworkLoggingRequestObfuscationHandlerEnabled(bool enabled) {
    if (![Bugfender respondsToSelector:@selector(setNetworkLoggingRequestObfuscationHandler:)]) {
        return;
    }
    if (!enabled) {
        [Bugfender setNetworkLoggingRequestObfuscationHandler:nil];
        return;
    }
    [Bugfender setNetworkLoggingRequestObfuscationHandler:^BFNetworkRequestData * _Nonnull(NSString * _Nonnull url, NSDictionary<NSString *,NSString *> * _Nonnull headers, NSString * _Nullable body) {
        if (s_requestObfuscationCallback == NULL) {
            return [[BFNetworkRequestData alloc] initWithURL:url headers:headers body:body];
        }
        NSString* headersJson = jsonFromDictionary(headers);
        char* resultC = s_requestObfuscationCallback(
            [url UTF8String],
            [headersJson UTF8String],
            body != nil ? [body UTF8String] : NULL);
        NSDictionary* payload = parseObfuscationPayload(resultC);
        if (payload == nil) {
            return [[BFNetworkRequestData alloc] initWithURL:url headers:headers body:body];
        }
        NSString* obfuscatedUrl = [payload[@"url"] isKindOfClass:[NSString class]] ? payload[@"url"] : url;
        NSDictionary<NSString *, NSString *>* obfuscatedHeaders = headers;
        id headersValue = payload[@"headers"];
        if ([headersValue isKindOfClass:[NSDictionary class]]) {
            NSMutableDictionary<NSString *, NSString *>* mapped = [NSMutableDictionary dictionary];
            for (id key in (NSDictionary*)headersValue) {
                if (![key isKindOfClass:[NSString class]]) {
                    continue;
                }
                id value = ((NSDictionary*)headersValue)[key];
                mapped[(NSString*)key] = [value isKindOfClass:[NSString class]] ? (NSString*)value : (value ? [value description] : @"");
            }
            obfuscatedHeaders = mapped;
        }
        NSString* obfuscatedBody = body;
        id bodyValue = payload[@"body"];
        if (bodyValue == nil || bodyValue == [NSNull null]) {
            obfuscatedBody = nil;
        } else if ([bodyValue isKindOfClass:[NSString class]]) {
            obfuscatedBody = (NSString*)bodyValue;
        }
        return [[BFNetworkRequestData alloc] initWithURL:obfuscatedUrl headers:obfuscatedHeaders body:obfuscatedBody];
    }];
}

void BugfenderSetNetworkLoggingResponseObfuscationHandlerEnabled(bool enabled) {
    if (![Bugfender respondsToSelector:@selector(setNetworkLoggingResponseObfuscationHandler:)]) {
        return;
    }
    if (!enabled) {
        [Bugfender setNetworkLoggingResponseObfuscationHandler:nil];
        return;
    }
    [Bugfender setNetworkLoggingResponseObfuscationHandler:^BFNetworkResponseData * _Nonnull(NSDictionary<NSString *,NSString *> * _Nonnull headers, NSString * _Nullable body) {
        if (s_responseObfuscationCallback == NULL) {
            return [[BFNetworkResponseData alloc] initWithHeaders:headers body:body];
        }
        NSString* headersJson = jsonFromDictionary(headers);
        char* resultC = s_responseObfuscationCallback(
            [headersJson UTF8String],
            body != nil ? [body UTF8String] : NULL);
        NSDictionary* payload = parseObfuscationPayload(resultC);
        if (payload == nil) {
            return [[BFNetworkResponseData alloc] initWithHeaders:headers body:body];
        }
        NSDictionary<NSString *, NSString *>* obfuscatedHeaders = headers;
        id headersValue = payload[@"headers"];
        if ([headersValue isKindOfClass:[NSDictionary class]]) {
            NSMutableDictionary<NSString *, NSString *>* mapped = [NSMutableDictionary dictionary];
            for (id key in (NSDictionary*)headersValue) {
                if (![key isKindOfClass:[NSString class]]) {
                    continue;
                }
                id value = ((NSDictionary*)headersValue)[key];
                mapped[(NSString*)key] = [value isKindOfClass:[NSString class]] ? (NSString*)value : (value ? [value description] : @"");
            }
            obfuscatedHeaders = mapped;
        }
        NSString* obfuscatedBody = body;
        id bodyValue = payload[@"body"];
        if (bodyValue == nil || bodyValue == [NSNull null]) {
            obfuscatedBody = nil;
        } else if ([bodyValue isKindOfClass:[NSString class]]) {
            obfuscatedBody = (NSString*)bodyValue;
        }
        return [[BFNetworkResponseData alloc] initWithHeaders:obfuscatedHeaders body:obfuscatedBody];
    }];
}

}
