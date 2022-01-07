using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestBehaviour : MonoBehaviour
{
    public void OnButtonPress()
    {
        // Testing all Bugfender SDK entry points
        Debug.Log("Bugfender test starts");
        Bugfender.Log("testing testing");
        Bugfender.SetDeviceString("key", "value");
        Bugfender.SetDeviceString("deletedkey", "value");
        Bugfender.RemoveDeviceKey("deletedkey");
        Debug.Log("crash: " + Bugfender.SendCrash("test", "test"));
        Debug.Log("issue: " + Bugfender.SendIssue("test", "test"));
        Debug.Log("feedback: " + Bugfender.SendUserFeedback("test", "test"));
        Bugfender.SetMaximumLocalStorageSize(10*1024*1024);
        Debug.Log("device: " + Bugfender.DeviceIdentifierUrl());
        Debug.Log("session: " + Bugfender.SessionIdentifierUrl());
        Bugfender.SetForceEnabled(true);
        Bugfender.ForceSendOnce();
        Debug.Log("Bugfender test done");
    }
}
