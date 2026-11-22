using NegativeCarver.Core;
using UnityEngine;

namespace NegativeCarver.Unity
{
    public sealed class BoardFramer : MonoBehaviour
    {
        private Camera framed;

        private void OnEnable()
        {
            framed = GetComponent<Camera>();
            Apply();
        }

        private void Update()
        {
            Apply();
        }

        private void Apply()
        {
            if (framed == null || framed.aspect <= 0f)
            {
                return;
            }

            var view = BoardFrame.Fit(framed.aspect, PilotRules.HudBottomReserve);
            framed.orthographic = true;
            framed.orthographicSize = view.OrthographicSize;
            var position = framed.transform.position;
            framed.transform.position = new Vector3(view.CameraX, view.CameraY, position.z);
        }
    }
}
