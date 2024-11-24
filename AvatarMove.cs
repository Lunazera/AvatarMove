using System;
using VNyanInterface;
using UnityEngine;
using VNyanExtra;
using System.Collections.Generic;

namespace AvatarMoveLayer
{
    public class AvatarMoveSettings
    {
        // This allows for some methods to be accessible by the unity loop, if we wanted to change settings by a UI or by parameter settings.

        // Layer On/Off Setting
        public static bool layerActive = true; // flag for layer being active or not (when inactive, vnyan will stop reading from the rotations entirely)
        public static void setLayerOnOff(float val) => layerActive = (val == 1f) ? true : false;

        // Settings for Movement
        public static VNyanVector3 avaDirection = new VNyanVector3();
        public static float avaDirectionAngle = 0.0f;
        public static VNyanVector3 avaPosition = new VNyanVector3();
        public static float avaCurrentSpeed = 0;
        public static float avaCurrentStrafe = 0;
        public static float avaForwardAccel = 0.05f;
        public static float avaBackwardAccel = 0.03f;
        public static float avaDeAccel = 0.1f;

        // Settings for Rotation
        public static float avaRotation = 0f;
        public static float avaCurrentRotationSpeed = 0;
        public static float avaRotationAccel = 0.25f;
    }

    public class AvatarMoveLayer : IPoseLayer
    {
        // Set up our frame-by-frame information
        public PoseLayerFrame AvatarMoveFrame = new PoseLayerFrame();

        // Create containers to load pose data each frame
        public Dictionary<int, VNyanQuaternion> BoneRotations;
        public Dictionary<int, VNyanVector3> BonePositions;
        public Dictionary<int, VNyanVector3> BoneScales;
        public VNyanVector3 RootPos;
        public VNyanQuaternion RootRot;

        public Quaternion UnityRootRotation;

        // VNyan Get Methods, VNyan uses these to get the pose after doUpdate()
        VNyanVector3 IPoseLayer.getBonePosition(int i)
        {
            return BonePositions[i];
        }
        VNyanQuaternion IPoseLayer.getBoneRotation(int i)
        {
            return BoneRotations[i];
        }
        VNyanVector3 IPoseLayer.getBoneScaleMultiplier(int i)
        {
            return BoneScales[i];
        }
        VNyanVector3 IPoseLayer.getRootPosition()
        {
            return RootPos;
        }
        VNyanQuaternion IPoseLayer.getRootRotation()
        {
            return RootRot;
        }

        // Pose Toggle Method, can be used to activate
        bool IPoseLayer.isActive()
        {
            // return AvatarMoveSettings.layerActive;
            return true;
        }

        public void doUpdate(in PoseLayerFrame AvatarMoveFrame)
        {

            // Get all current Bone and Root values up to this point from our Layer Frame, and load them in our holdover values.
            BoneRotations = AvatarMoveFrame.BoneRotation;
            BonePositions = AvatarMoveFrame.BonePosition;
            BoneScales = AvatarMoveFrame.BoneScaleMultiplier;
            RootPos = AvatarMoveFrame.RootPosition;
            RootRot = AvatarMoveFrame.RootRotation;

            // Get from VNyan Parameters
            float avaSpeed = VNyanInterface.VNyanInterface.VNyanParameter.getVNyanParameterFloat("avaSpeed");
            float avaStrafe = VNyanInterface.VNyanInterface.VNyanParameter.getVNyanParameterFloat("avaStrafe");
            float avaRotationSpeed = VNyanInterface.VNyanInterface.VNyanParameter.getVNyanParameterFloat("avaRotationSpeed");
            
            // Use move towards to smoothly ramp up speed set. This will then apply to the position based on the player's forward vector
            // The if/else statements here are just to allow different accelerations depending on speed/direction
            if (avaSpeed == 0)
            {
                AvatarMoveSettings.avaCurrentSpeed = Mathf.MoveTowards(
                    AvatarMoveSettings.avaCurrentSpeed,
                    0,
                    AvatarMoveSettings.avaDeAccel * Time.deltaTime);
            } else if (avaSpeed > 0)
            {
                AvatarMoveSettings.avaCurrentSpeed = Mathf.MoveTowards(
                    AvatarMoveSettings.avaCurrentSpeed,
                    avaSpeed,
                    AvatarMoveSettings.avaForwardAccel * Time.deltaTime);
            } else if (avaSpeed < 0)
            {
                AvatarMoveSettings.avaCurrentSpeed = Mathf.MoveTowards(
                    AvatarMoveSettings.avaCurrentSpeed,
                    avaSpeed,
                    AvatarMoveSettings.avaBackwardAccel * Time.deltaTime);
            }

            // Strafing
            if (avaStrafe == 0)
            {
                AvatarMoveSettings.avaCurrentStrafe = Mathf.MoveTowards(
                    AvatarMoveSettings.avaCurrentStrafe,
                    0,
                    AvatarMoveSettings.avaDeAccel * Time.deltaTime);
            } else
            {
                AvatarMoveSettings.avaCurrentStrafe = Mathf.MoveTowards(
                    AvatarMoveSettings.avaCurrentStrafe,
                    avaStrafe,
                    AvatarMoveSettings.avaForwardAccel * Time.deltaTime);
            }

            // Apply Rotation to Avatar
            if (avaRotationSpeed == 0)
            {
                AvatarMoveSettings.avaCurrentRotationSpeed = Mathf.MoveTowards(
                    AvatarMoveSettings.avaCurrentRotationSpeed,
                    0,
                    AvatarMoveSettings.avaRotationAccel * Time.deltaTime);
            } else
            {
                AvatarMoveSettings.avaCurrentRotationSpeed = Mathf.MoveTowards(
                    AvatarMoveSettings.avaCurrentRotationSpeed,
                    avaRotationSpeed,
                    AvatarMoveSettings.avaRotationAccel * Time.deltaTime);
            }
            
            // Rotation //

            // First get our Current Root Rotation's VNyanQuaternion into Unity Quaternion
            UnityRootRotation = VNyanExtra.QuaternionMethods.convertQuaternionV2U(RootRot);

            // Additively change rotation by speed
            AvatarMoveSettings.avaRotation += avaRotationSpeed;

            // Create a rotation that'll rotate around by our desired speed
            Quaternion UnityRotateTo = Quaternion.AngleAxis(AvatarMoveSettings.avaRotation, Vector3.up);

            // Next, apply rotation
            Quaternion UnityNewRotation = UnityRootRotation * UnityRotateTo;

            // Convert back into VNyanQuaternion and save to Hip bone
            RootRot = VNyanExtra.QuaternionMethods.convertQuaternionU2V(UnityNewRotation);

            // Direction vector //

            // Now, get the current Hip bone rotation to figure out our direction
            // Combine our Root rotation with Hip rotation

            Quaternion combinedRotation = VNyanExtra.QuaternionMethods.convertQuaternionV2U(BoneRotations[0]) * UnityNewRotation;
            AvatarMoveSettings.avaDirectionAngle = combinedRotation.eulerAngles.y;

            // Calculate move direction vector from rotation
            AvatarMoveSettings.avaDirection.X = Mathf.Sin(AvatarMoveSettings.avaDirectionAngle * Mathf.Deg2Rad);
            AvatarMoveSettings.avaDirection.Y = 0;
            AvatarMoveSettings.avaDirection.Z = Mathf.Cos(AvatarMoveSettings.avaDirectionAngle * Mathf.Deg2Rad);

            // Calculate strafing component
            float strafeX = Mathf.Sin((AvatarMoveSettings.avaDirectionAngle - 90) * Mathf.Deg2Rad) * AvatarMoveSettings.avaCurrentStrafe;
            float strafeZ = Mathf.Cos((AvatarMoveSettings.avaDirectionAngle - 90) * Mathf.Deg2Rad) * AvatarMoveSettings.avaCurrentStrafe;

            // Movement //

            // Apply speed to direction (save new position in our position vector)
            AvatarMoveSettings.avaPosition.X += AvatarMoveSettings.avaDirection.X * AvatarMoveSettings.avaCurrentSpeed + strafeX;
            AvatarMoveSettings.avaPosition.Z += AvatarMoveSettings.avaDirection.Z * AvatarMoveSettings.avaCurrentSpeed + strafeZ;

            // Apply movement to Avatar position (apply position vector to hip position)
            RootPos.X += AvatarMoveSettings.avaPosition.X;
            RootPos.Z += AvatarMoveSettings.avaPosition.Z;

            // Save/Report Values //

            // Report current Position and Rotation to VNyan
            VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterFloat("AvatarPosX", RootPos.X);
            VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterFloat("AvatarPosZ", RootPos.Z);
            VNyanInterface.VNyanInterface.VNyanParameter.setVNyanParameterFloat("AvatarRot", AvatarMoveSettings.avaDirectionAngle);
        }
    }
}
