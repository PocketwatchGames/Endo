using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.EventSystems;

namespace Endo
{
    public class GameCamera : MonoBehaviour
    {
        public Transform Target;
        public float Distance;
        public float MinDistance;
        public float MaxDistance;
        public float ZoomSpeed;
        public bool RotateAroundPoles;

        private float3 _lastMousePosition;
        private float _pitch;

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void LateUpdate()
        {
            if (Utils.IsMouseOverGameWindow)
            {
                Distance = math.clamp(Distance + Input.mouseScrollDelta.y * ZoomSpeed, MinDistance, MaxDistance);
            }
            if (Input.GetMouseButton(1))
            {
                var diff = (float3)Input.mousePosition - _lastMousePosition;
                if (RotateAroundPoles)
                {
                    _pitch = Mathf.Clamp(_pitch - diff.y, -89.99f, 89.99f);
                    transform.eulerAngles = new Vector3(_pitch, transform.eulerAngles.y + diff.x, 0);
                }
                else
                {
                    transform.Rotate(-diff.y, diff.x, 0);
                }

            }
            _lastMousePosition = Input.mousePosition;

            transform.position = Target.position - transform.forward * Distance;
        }

        public void ResetXZRotation()
        {
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        }
    }
}