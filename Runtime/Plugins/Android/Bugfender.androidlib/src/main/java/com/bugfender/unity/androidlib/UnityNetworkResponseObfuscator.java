package com.bugfender.unity.androidlib;

import java.util.HashMap;

/**
 * Implemented from C# via {@code AndroidJavaProxy} so native OkHttp capture can invoke Unity obfuscation handlers.
 */
public interface UnityNetworkResponseObfuscator {
    NetworkObfuscationResult obfuscate(HashMap<String, String> headers, String body);
}
