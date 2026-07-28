package com.bugfender.unity.androidlib;

import java.util.HashMap;
import java.util.Map;

/**
 * Result of a Unity-side network obfuscation callback.
 */
public final class NetworkObfuscationResult {
    private final String url;
    private final HashMap<String, String> headers;
    private final String body;

    public NetworkObfuscationResult(String url, Map<String, String> headers, String body) {
        this.url = url;
        this.headers = new HashMap<String, String>();
        if (headers != null) {
            this.headers.putAll(headers);
        }
        this.body = body;
    }

    public String getUrl() {
        return url;
    }

    public HashMap<String, String> getHeaders() {
        return headers;
    }

    public String getBody() {
        return body;
    }
}
