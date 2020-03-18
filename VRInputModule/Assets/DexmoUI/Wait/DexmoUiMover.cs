﻿using UnityEngine;
using DG.Tweening;
using Dexmo.Unity;
using Dexmo.Unity.Model;
using Dexmo.Unity.Interaction;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Dexmo.UI
{
    public class DexmoUiMover : DexmoHandTriggerChecker
    {
        public Transform leftHand;
        public Transform rightHand;
        public Transform movingUI;
        public Button btn;
        [SerializeField]
        private List<UiContent> uiContents;

        private Transform followTarget;
        private Vector3 _moveOffsetPos;
        private Vector3 _moveOffsetDir;

        private float thumbMinValue = 0.3f;
        private float otherFingerMinValue = 0.6f;
        private bool isBtnDown;

        private UiMoverEffect effect;
        private GestureHandData graspHandData;
        private Transform enterHandObj;
        private VibrationFeedBackData vibrationData;

        protected override void Start()
        {
            base.Start();
            _InitGraspHandData();

            var _handModelManager = GameObject.FindObjectOfType<HandModelManager>();
            leftHand = _handModelManager.GetHandTransform(Handedness.Left);
            rightHand = _handModelManager.GetHandTransform(Handedness.Right);

            OnOneHandEnter += _HandEnter;
            OnOneHandExit += _HandExit;

            effect = GetComponentInChildren<UiMoverEffect>();
            btn.onClick.AddListener(ShowAndHide);
            vibrationData = new VibrationFeedBackData(true, Waveform.Smooth, VibrationCycle.Loop, 0.2f);
            transform.localScale = Vector3.zero;
        }

        private void FixedUpdate()
        {
            if (!isBtnDown) return;

            _Checking();
            _Moving();
        }

        private void ShowAndHide()
        {
            ShowSphere(!isBtnDown);
        }

        public void ShowSphere(bool isShow)
        {
            isBtnDown = isShow;
            var scale = isShow ? Vector3.one : Vector3.zero;
            transform.DOScale(scale, 1);
            _SetUiContentActice(!isShow);

            if (effect)
            {
                if (isShow)
                {
                    btn.GetComponent<Image>().color = UiTool.GetColor("F8293B");
                    effect.WaitingMode(enterHand);
                }
                else
                {
                    btn.GetComponent<Image>().color = UiTool.GetColor("E79400");
                    effect.NormalMode(enterHand);
                }
            }
        }

        #region Move

        private void _MovingState(bool isMoving)
        {
            if (isMoving)
            {
                effect.MovingMode(enterHand);             
                FeedbackBridge.Instance.SetAllVibrationFeedback(enterHand, vibrationData);
                FeedbackBridge.Instance.StartHandFeedback(enterHand, true, FeedBackType.VIBRATION);
            }
            else
            {
                effect.WaitingMode(enterHand);
                FeedbackBridge.Instance.StopHandFeedback(enterHand, FeedBackType.VIBRATION);
                followTarget = null;
            }
        }

        private void _HandEnter(Handedness hand)
        {
            var target = hand == Handedness.Left ? leftHand : rightHand;

            enterHandObj = target;
            if (followTarget == target) return;
            followTarget = target;

            if (_CanMove(target))
            {
                _StartMove();
            }
            else
            {
                _MovingState(false);
            }
        }

        private void _HandExit(Handedness hand)
        {
            var target = hand == Handedness.Left ? leftHand : rightHand;

            enterHandObj = null;
            if (followTarget != null && followTarget == target)
            {
                _MovingState(false);
            }
        }

        private void _Checking()
        {
            if (followTarget == null && _CanMove(enterHandObj))
            {
                followTarget = enterHandObj;
                _StartMove();
            }
        }

        private void _Moving()
        {
            if (followTarget != null && movingUI != null)
            {
                if (_CanMove(followTarget))
                {
                    movingUI.position = followTarget.TransformPoint(_moveOffsetPos);
                    _RotateY(followTarget);
                }
                else
                {
                    _MovingState(false);
                }
            }
        }

        private bool _CanMove(Transform target)
        {
            //判断是否 手背是否向上
            return target != null && _isGrasping() && Vector3.Dot(target.up, Vector3.up) > 0.5f;
        }

        private bool _isGrasping()
        {
            if (enterHandObj == null) return false;
            var handdata = DexmoManager.Instance.DexmoDataReader.GetDexmoDeviceHandData(enterHand);
            return graspHandData.Constains(handdata);
        }

        private void _RotateY(Transform target)
        {
            var dir = Vector3.ProjectOnPlane(followTarget.forward, Vector3.up);
            var angle = Vector3.SignedAngle(dir, _moveOffsetDir, Vector3.up);
            movingUI.Rotate(0, -angle, 0);
            _moveOffsetDir = dir;
        }

        private void _StartMove()
        {
            _MovingState(true);
            _moveOffsetPos = followTarget.InverseTransformPoint(movingUI.position);
            _moveOffsetDir = Vector3.ProjectOnPlane(followTarget.forward, Vector3.up);
        }

        #region Rotate for Child

        private Transform _rotatedChild;
        private float _lastRotateTime;
        private Vector3 _targetUpDir;
        private Vector3 _rotatedChildAngle;

        private void _SetRotateChildInfo()
        {
            _targetUpDir = followTarget.up;
            _lastRotateTime = Time.realtimeSinceStartup;
        }

        private void _RotateChild()
        {
            if (followTarget != null)
            {
                if (Time.realtimeSinceStartup - _lastRotateTime > 0.1f)
                {
                    var angle = Vector3.SignedAngle(_targetUpDir, followTarget.up, followTarget.right);
                    if (Mathf.Abs(angle) > 1)
                    {
                        var x = _rotatedChild.localEulerAngles.x;
                        x += angle;
                        if (x > 180) x -= 360;

                        if (x > 15) x = 15;
                        else if (x < -15) x = -15;

                        _rotatedChildAngle.x = x;
                        _rotatedChild.localEulerAngles = _rotatedChildAngle;
                        _targetUpDir = followTarget.up;
                    }
                    _lastRotateTime = Time.realtimeSinceStartup;
                }
            }
        }

        #endregion

        #endregion

        /// <summary>
        /// 初始化抓取的判断数值
        /// </summary>
        private void _InitGraspHandData()
        {
            graspHandData = new GestureHandData();
            graspHandData[FingerType.THUMB].Bend[JointType.MCP].ValueMin = thumbMinValue;
            graspHandData[FingerType.THUMB].Bend[JointType.MCP].ValueMax = 1f;
            for(int i = 1; i<5; i++)
            {
                graspHandData[(FingerType)i].Bend[JointType.MCP].ValueMin = otherFingerMinValue;
                graspHandData[(FingerType)i].Bend[JointType.MCP].ValueMax = 1f;
            }
        }

        #region UiContent

        private void _SetUiContentActice(bool isActive)
        {
            foreach (UiContent item in uiContents)
            {
                item.SetActive(isActive);
            }
        }

        #endregion
    }
}