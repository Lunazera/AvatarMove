using System;
using UnityEngine;
using VNyanInterface;
using AvatarMoveLayer;
using System.Collections.Generic;

namespace AvatarMovePlugin
{
    public class AvatarMovePlugin : MonoBehaviour
    {
        // Set up pose layer according to our class
        IPoseLayer AvatarMove = new AvatarMoveLayer.AvatarMoveLayer();

        public void Start()
        {
            VNyanInterface.VNyanInterface.VNyanAvatar.registerPoseLayer(AvatarMove);
        }
    }
}
