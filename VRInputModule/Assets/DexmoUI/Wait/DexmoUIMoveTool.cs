using UnityEngine;
using DG.Tweening;
using Dexmo.Unity;
using Dexmo.Unity.Model;
using Dexmo.Unity.Interaction;

namespace Dexmo.UI
{
    [RequireComponent(typeof(TouchableObject))]
    public class DexmoUIMoveTool : DexmoHandTriggerChecker
    {
        public Transform leftHand;
        public Transform rightHand;
        public Transform movingUI;

        private TouchableObject touchable;
        private Transform followTarget;
        private Vector3 _moveOffsetPos;
        private Vector3 _moveOffsetDir;
        
        private GameObject sphere;
        private Material sphereMat;
        private Color movingColor = UiTool.GetColor("079A9419");
        private Color normalColor = UiTool.GetColor("D9310F19");
        private string graspSphere = "Audio/TouchableUI_Btn";

        [SerializeField]
        private GestureHandData graspHandData;

        private Transform enterHandObj;
        private VibrationFeedBackData vibrationData;

        protected override void Start()
        {
            base.Start();
            var _handModelManager = GameObject.FindObjectOfType<HandModelManager>();
            leftHand = _handModelManager.GetHandTransform(Handedness.Left);
            rightHand = _handModelManager.GetHandTransform(Handedness.Right);

            touchable = GetComponent<TouchableObject>();
            touchable.EnableForceFeedback = false;
            touchable.TouchObjType = TouchObjType.Penetrable;
            touchable.VibrationIntensity = 0.1f;
            touchable.EnableVibrationFeedback = false;

            OnOneHandEnter += _HandEnter;
            OnOneHandExit += _HandExit;

            sphere = transform.Find("Sphere").gameObject;
            sphereMat = sphere.GetComponent<MeshRenderer>().material;

            vibrationData = new VibrationFeedBackData(true, Waveform.Smooth, VibrationCycle.Loop, 0.25f);
            transform.localScale = Vector3.zero;
        }

        private void FixedUpdate()
        {
            _Checking();
            _Moving();
        }

        public void ShowSphere(bool isShow)
        {
            var scale = isShow ? Vector3.one : Vector3.zero;
            transform.DOScale(scale, 1);
        }

        private void _MovingState(bool isMoving)
        {
            if (isMoving)
            {
                K.SoundTool.Instance.PlayAudio(graspSphere);
                //sphereMat.color = movingColor;
                sphereMat.SetColor("_RimColor", movingColor);
                //Debug.Log(enterHand + "Start Vibration");
                FeedbackBridge.Instance.SetAllVibrationFeedback(enterHand, vibrationData);
                FeedbackBridge.Instance.StartHandFeedback(enterHand, true, FeedBackType.VIBRATION);
                //touchable.EnableVibrationFeedback = true;
            }
            else
            {
                //sphereMat.color = normalColor;
                sphereMat.SetColor("_RimColor", normalColor);
                //touchable.EnableVibrationFeedback = false;
                //Debug.Log(enterHand +  "Stop Vibration");
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
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
                    //Debug.Log(angle);
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
    }
}