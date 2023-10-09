using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using MUXR;

public static class XRBaseControllerExtensions
{
    public static InputDevice GetInputDevice(this XRBaseController controller)
    {
        XRNode node;
        if (controller == KernRig.Instance.RightHandController)
        {
            node = XRNode.RightHand;
        }
        else if (controller == KernRig.Instance.LeftHandController)
        {
            node = XRNode.LeftHand;
        }
        else
        {
            return default;
        }

        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(node, devices);
        if (devices.Count > 0)
        {
            return devices[0];
        }

        return default;
    }
}
