using Dexmo.Unity;
using Dexmo.Unity.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Dexmo.UI
{
    public class DexmoFingerTriggerChecker : MonoBehaviour
    {
        private int handLayer;
        public Action<Handedness, VibrationPos> OnFingerEnter;
        public Action<Handedness, VibrationPos> OnFingerExit;

        protected virtual void Start()
        {
            handLayer = LayerMask.NameToLayer("Hand");
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == handLayer)
            {
                var info = other.GetComponent<VibrationInfo>();
                if (info != null && info.posIndex <= 4)
                {
                    if (OnFingerEnter != null)
                    {
                        OnFingerEnter.Invoke(info.hand, info.pos);
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == handLayer)
            {
                var info = other.GetComponent<VibrationInfo>();
                if (info != null && info.posIndex <= 4)
                {
                    if (OnFingerExit != null)
                    {
                        OnFingerExit.Invoke(info.hand, info.pos);
                    }
                }
            }
        }
    }
}