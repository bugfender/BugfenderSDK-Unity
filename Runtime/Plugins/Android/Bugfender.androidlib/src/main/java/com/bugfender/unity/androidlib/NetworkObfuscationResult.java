package com.bugfender.unity.androidlib;

import java.util.HashMap;

/**
 * Result of a Unity-side network obfuscation callback.
 */
public final class NetworkObfuscationResult {
    private final String url;
    private final HashMap<String, String> headers;
    private final String body;

    public NetworkObfuscationResult(String url, HashMap<String, String> headers, String body) {
        this.url = url;
        this.headers = new HashMap<String, String>();
        if (headers != null) {
            this.headers.putAll(headers);
        }
        this.body = body;
    }

    /** Preferred from C#: avoids HashMap.put(Object,Object) signature mismatches. */
    public static NetworkObfuscationResult create(String url, HashMap<String, String> headers, String body) {
        return new NetworkObfuscationResult(url, headers, body);
    }

    public static HashMap<String, String> newHeaderMap() {
        return new HashMap<String, String>();
    }

    public static void putHeader(HashMap<String, String> map, String key, String value) {
        if (map == null || key == null) {
            return;
        }
        map.put(key, value != null ? value : "");
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
