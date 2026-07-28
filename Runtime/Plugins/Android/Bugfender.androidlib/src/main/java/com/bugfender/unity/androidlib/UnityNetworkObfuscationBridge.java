package com.bugfender.unity.androidlib;

import com.bugfender.sdk.Bugfender;

import java.lang.reflect.Constructor;
import java.lang.reflect.InvocationHandler;
import java.lang.reflect.Method;
import java.lang.reflect.Proxy;
import java.util.HashMap;
import java.util.Map;

/**
 * Wires Unity obfuscation callbacks into the Android Bugfender SDK.
 * <p>
 * Android SDK 4.0.1 ships R8-obfuscated handler types ({@code s1}/{@code t1}).
 * Resolve them via reflection so this bridge stays compatible with the published Maven artifact.
 */
public final class UnityNetworkObfuscationBridge {
    private UnityNetworkObfuscationBridge() {
    }

    public static void setRequestObfuscationHandler(UnityNetworkRequestObfuscator handler) {
        setObfuscationHandler(
                "setNetworkLoggingRequestObfuscationHandler",
                handler == null ? null : createRequestHandler(handler)
        );
    }

    public static void setResponseObfuscationHandler(UnityNetworkResponseObfuscator handler) {
        setObfuscationHandler(
                "setNetworkLoggingResponseObfuscationHandler",
                handler == null ? null : createResponseHandler(handler)
        );
    }

    private static void setObfuscationHandler(String methodName, Object handler) {
        try {
            Method setter = findBugfenderMethod(methodName, 1);
            if (setter == null) {
                return;
            }
            setter.invoke(null, handler);
        } catch (Exception ignored) {
            // Optional API; ignore if unavailable.
        }
    }

    private static Method findBugfenderMethod(String name, int paramCount) {
        for (Method method : Bugfender.class.getMethods()) {
            if (name.equals(method.getName()) && method.getParameterTypes().length == paramCount) {
                return method;
            }
        }
        return null;
    }

    private static Object createRequestHandler(final UnityNetworkRequestObfuscator unityHandler) {
        Method setter = findBugfenderMethod("setNetworkLoggingRequestObfuscationHandler", 1);
        if (setter == null) {
            return null;
        }
        Class<?> handlerType = setter.getParameterTypes()[0];
        return Proxy.newProxyInstance(
                handlerType.getClassLoader(),
                new Class<?>[]{handlerType},
                new InvocationHandler() {
                    @Override
                    public Object invoke(Object proxy, Method method, Object[] args) throws Throwable {
                        if (method.getDeclaringClass() == Object.class) {
                            return invokeObjectMethod(proxy, method, args);
                        }
                        if (args == null || args.length < 3) {
                            return null;
                        }
                        String url = args[0] instanceof String ? (String) args[0] : "";
                        @SuppressWarnings("unchecked")
                        Map<String, String> headers = args[1] instanceof Map
                                ? (Map<String, String>) args[1]
                                : new HashMap<String, String>();
                        String body = args[2] instanceof String ? (String) args[2] : null;

                        HashMap<String, String> headersCopy = new HashMap<String, String>();
                        if (headers != null) {
                            headersCopy.putAll(headers);
                        }

                        NetworkObfuscationResult result = unityHandler.obfuscate(
                                url != null ? url : "",
                                headersCopy,
                                body
                        );

                        String obfuscatedUrl = url;
                        Map<String, String> obfuscatedHeaders = headersCopy;
                        String obfuscatedBody = body;
                        if (result != null) {
                            if (result.getUrl() != null) {
                                obfuscatedUrl = result.getUrl();
                            }
                            if (result.getHeaders() != null) {
                                obfuscatedHeaders = result.getHeaders();
                            }
                            obfuscatedBody = result.getBody();
                        }
                        return newNetworkData(
                                method.getReturnType(),
                                obfuscatedUrl,
                                obfuscatedHeaders,
                                obfuscatedBody
                        );
                    }
                }
        );
    }

    private static Object createResponseHandler(final UnityNetworkResponseObfuscator unityHandler) {
        Method setter = findBugfenderMethod("setNetworkLoggingResponseObfuscationHandler", 1);
        if (setter == null) {
            return null;
        }
        Class<?> handlerType = setter.getParameterTypes()[0];
        return Proxy.newProxyInstance(
                handlerType.getClassLoader(),
                new Class<?>[]{handlerType},
                new InvocationHandler() {
                    @Override
                    public Object invoke(Object proxy, Method method, Object[] args) throws Throwable {
                        if (method.getDeclaringClass() == Object.class) {
                            return invokeObjectMethod(proxy, method, args);
                        }
                        if (args == null || args.length < 2) {
                            return null;
                        }
                        @SuppressWarnings("unchecked")
                        Map<String, String> headers = args[0] instanceof Map
                                ? (Map<String, String>) args[0]
                                : new HashMap<String, String>();
                        String body = args[1] instanceof String ? (String) args[1] : null;

                        HashMap<String, String> headersCopy = new HashMap<String, String>();
                        if (headers != null) {
                            headersCopy.putAll(headers);
                        }

                        NetworkObfuscationResult result = unityHandler.obfuscate(headersCopy, body);

                        Map<String, String> obfuscatedHeaders = headersCopy;
                        String obfuscatedBody = body;
                        if (result != null) {
                            if (result.getHeaders() != null) {
                                obfuscatedHeaders = result.getHeaders();
                            }
                            obfuscatedBody = result.getBody();
                        }
                        return newNetworkData(
                                method.getReturnType(),
                                null,
                                obfuscatedHeaders,
                                obfuscatedBody
                        );
                    }
                }
        );
    }

    private static Object newNetworkData(
            Class<?> type,
            String url,
            Map<String, String> headers,
            String body
    ) throws Exception {
        for (Constructor<?> constructor : type.getConstructors()) {
            Class<?>[] params = constructor.getParameterTypes();
            if (
                    params.length == 3 &&
                            params[0] == String.class &&
                            Map.class.isAssignableFrom(params[1]) &&
                            params[2] == String.class
            ) {
                return constructor.newInstance(url, headers, body);
            }
            if (
                    params.length == 2 &&
                            Map.class.isAssignableFrom(params[0]) &&
                            params[1] == String.class
            ) {
                return constructor.newInstance(headers, body);
            }
        }
        return null;
    }

    private static Object invokeObjectMethod(Object proxy, Method method, Object[] args) {
        String name = method.getName();
        if ("toString".equals(name)) {
            return "UnityNetworkObfuscationHandlerProxy";
        }
        if ("hashCode".equals(name)) {
            return System.identityHashCode(proxy);
        }
        if ("equals".equals(name)) {
            return proxy == (args != null && args.length > 0 ? args[0] : null);
        }
        return null;
    }
}
