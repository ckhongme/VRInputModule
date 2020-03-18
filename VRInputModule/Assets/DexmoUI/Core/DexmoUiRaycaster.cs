using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dexmo.UI
{
    /// <summary>
    /// 创建Canvas后，替换掉原来的 GraphicRaycaster
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class DexmoUiRaycaster : BaseRaycaster
    {
        /// <summary>
        /// 射线移入的距离
        /// </summary>
        private float rayEnterDist = 0.03f;
        /// <summary>
        /// 
        /// </summary>
        private float reversedRayDist = 0.03f;
        /// <summary>
        /// 是否忽略反向射线
        /// </summary>
        public bool ignoreReversedGraphics = true;

        /// <summary>
        /// 给每个Raycaster对象都设置一个 camera
        /// </summary>
        [NonSerialized]
        private Camera fallbackCam;
        public override Camera eventCamera
        {
            get
            {
                if (fallbackCam == null)
                {
                    var go = new GameObject(name + " FallbackCamera");
                    go.SetActive(false);
                    // place fallback camera at root to preserve world position
                    go.transform.SetParent(EventSystem.current.transform, false);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;

                    fallbackCam = go.AddComponent<Camera>();
                    fallbackCam.clearFlags = CameraClearFlags.Nothing;
                    fallbackCam.cullingMask = 0;
                    fallbackCam.orthographic = true;
                    fallbackCam.orthographicSize = 1;
                    fallbackCam.useOcclusionCulling = false;
#if !(UNITY_5_3 || UNITY_5_2 || UNITY_5_1 || UNITY_5_0)
                    fallbackCam.stereoTargetEye = StereoTargetEyeMask.None;
#endif
                    fallbackCam.nearClipPlane = 0;
                    fallbackCam.farClipPlane = rayEnterDist;
                }
                return fallbackCam;
            }
        }


        private Canvas m_Canvas;
        private Canvas canvas
        {
            get
            {
                if (m_Canvas != null)
                    return m_Canvas;

                m_Canvas = GetComponent<Canvas>();
                return m_Canvas;
            }
        }

        protected override void Start()
        {
            base.Start();
            rayEnterDist = Mathf.Max(DexmoUiInputModule.exitDistance + 0.01f, rayEnterDist);
            reversedRayDist = Mathf.Max(DexmoUiInputModule.overDistance + 0.01f, reversedRayDist);
        }

        [NonSerialized] private List<Graphic> m_RaycastResults = new List<Graphic>();
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (canvas == null) return; 
            if (canvas.renderMode != RenderMode.WorldSpace) return;

            var canvasGraphics = GraphicRegistry.GetGraphicsForCanvas(canvas);
            if (canvasGraphics == null || canvasGraphics.Count == 0)
                return;
   
            var data = (Pointer3DEventData)eventData;

            var rayOrigin = data.Pointer.position - data.Pointer.forward * reversedRayDist;
            Ray ray = new Ray(rayOrigin, data.Pointer.forward);
            eventCamera.transform.position = ray.origin;
            eventCamera.transform.rotation = Quaternion.LookRotation(ray.direction, transform.up);

            m_RaycastResults.Clear();

            var screenCenterPoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            for (int i = 0; i < canvasGraphics.Count; ++i)
            {
                var graphic = canvasGraphics[i];

                // -1 means it hasn't been processed by the canvas, which means it isn't actually drawn
                if (graphic.depth == -1 || !graphic.raycastTarget) continue;

                // 判断 pointerPosition 是否在 graphic 的 Rectangle 区域内
                if (_CheckCamView(graphic, screenCenterPoint, eventCamera) && graphic.Raycast(screenCenterPoint, eventCamera) )
                    m_RaycastResults.Add(graphic);
            }

            //对所有射线检测成功的 graphics 按照深度 depth 从小到大排序。
            m_RaycastResults.Sort((g1, g2) => g2.depth.CompareTo(g1.depth));

            int totalCount = m_RaycastResults.Count;
            for (var index = 0; index < totalCount; index++)
            {
                var graphic = m_RaycastResults[index].GetComponent<Graphic>();
                float dist;
                new Plane(graphic.transform.forward, graphic.transform.position).Raycast(ray, out dist);

                //判断正前方多少距离内有效
                if (dist > (rayEnterDist + reversedRayDist)) continue;

                resultAppendList.Add(new RaycastResult
                {
                    gameObject = graphic.gameObject,
                    module = this,
                    distance = dist,
                    worldPosition = ray.GetPoint(dist),
                    worldNormal = -graphic.transform.forward,
                    screenPosition = screenCenterPoint,
                    index = resultAppendList.Count,
                    depth = graphic.depth,
                    sortingLayer = canvas.sortingLayerID,
                    sortingOrder = canvas.sortingOrder
                });
            }
            data.ignoreReversedGraphics = ignoreReversedGraphics;
            //if (ignoreReversedGraphics && Vector3.Dot(ray.direction, graphic.transform.forward) <= 0f) { continue; }
        }

        private bool _CheckCamView(Graphic graphic, Vector3 screenCenterPoint, Camera camera)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, screenCenterPoint, camera);
        }

        protected override void OnDestroy()
        {
            if (fallbackCam != null)
            {
                GameObject.Destroy(fallbackCam.gameObject);
                fallbackCam = null;
            }
            base.OnDestroy();
        }
    }
}