using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Dexmo.UI
{
    public class DexmoUiTool : MonoBehaviour
    {
        private float _initDist = -47.5f;
        private float _intervalDist = -65f;
        private Button[] btns;
        private Button mainBtn;
        [SerializeField]
        private List<UiContent> uiContents;
        private DexmoUIMoveTool moveTool;
        private Dictionary<Button, bool> btnStatus;
        private bool isAniming = false;

        void Start()
        {
            moveTool = transform.Find("Tools/moveTool").GetComponent<DexmoUIMoveTool>();
            btnStatus = new Dictionary<Button, bool>();

            btns = transform.Find("Btns").GetComponentsInChildren<Button>();
            mainBtn = transform.Find("Main").GetComponent<Button>();
            btnStatus.Add(mainBtn, false);
            mainBtn.onClick.AddListener(ShowAndHide);

            foreach (Button btn in btns)
            {
                btnStatus.Add(btn, false);
                btn.onClick.AddListener(() => { ButtonDown(btn); });
            }
            HideBtns();
        }

        public void ShowAndHide()
        {
            //Debug.Log("ShowAndHide  " + Time.realtimeSinceStartup);
            if (isAniming) return;

            isAniming = true;
            var isShow = ChangeBtnStatus(mainBtn);
            if (isShow)
            {
               
                ShowBtns();
            }
            else
            {
                HideBtns();
            }
            _SetUiContentActice(!isShow);
        }

        public void ButtonDown(Button btn)
        {
            //Debug.Log(btn.name + "   btndown  " + Time.realtimeSinceStartup);
            if (btn.name.Equals("Move"))
                Move(btn);
            else if (btn.name.Equals("Lock"))
                Lock();
        }

        private void ShowBtns()
        {
            for (int i = 0; i < btns.Length; i++)
            {
                var index = i;
                var rect = btns[index].GetComponent<RectTransform>();
                var pos = rect.anchoredPosition3D;
                btns[index].transform.DOScale(Vector3.one, 0.5f).OnComplete(() =>
                {
                    DOTween.To((x) => rect.anchoredPosition3D = new Vector3(x, pos.y, pos.z), _initDist, _intervalDist * (index + 1), 1).OnComplete(() =>
                    {
                        //if (index == btns.Length - 1)
                            isAniming = false;
                    });
                });
            }
        }

        private void HideBtns()
        { 
            for (int i = 0; i < btns.Length; i++)
            {
                var index = i;
                var rect = btns[index].GetComponent<RectTransform>();
                var pos = rect.anchoredPosition3D;
                DOTween.To((x) => rect.anchoredPosition3D = new Vector3(x, pos.y, pos.z), pos.x, _initDist, 1).OnComplete(() =>
                {
                    btns[index].transform.DOScale(Vector3.zero, 0.5f).OnComplete(()=> 
                    {
                        //if (index == btns.Length - 1)
                            isAniming = false;
                    });
                });
            }
            moveTool.ShowSphere(false);
        }

        private void Move(Button btn)
        {
            moveTool.ShowSphere(ChangeBtnStatus(btn));
        }

        private void Lock()
        {

        }

        private bool ChangeBtnStatus(Button btn)
        {
            btnStatus[btn] = !btnStatus[btn];
            return btnStatus[btn];
        }

        private void _SetUiContentActice(bool isActive)
        {
            foreach (UiContent item in uiContents)
            {
                item.SetActive(isActive);
            }
        }
    }
}