using System;
using UnityEngine;

namespace ZeroStarRestaurant.Customers
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CustomerMovement : MonoBehaviour
    {
        [SerializeField] private Transform _entryPoint;
        [SerializeField] private Transform _exitPoint;
        [SerializeField] private Transform[] _arrivalPath = Array.Empty<Transform>();
        private Vector3[] _targets = Array.Empty<Vector3>();
        private int _targetIndex;
        public bool HasValidRoute
        {
            get
            {
                if (_entryPoint == null || _exitPoint == null || _arrivalPath == null || _arrivalPath.Length == 0) return false;
                foreach (Transform point in _arrivalPath) if (point == null) return false;
                return true;
            }
        }
        public void BeginEntering(int customerNumber)
        {
            _targets = new Vector3[_arrivalPath.Length];
            for (int index = 0; index < _targets.Length; index++) _targets[index] = _arrivalPath[index].position;
            _targetIndex = 0; transform.position = _entryPoint.position;
            gameObject.name = "Customer #" + customerNumber.ToString("D3"); gameObject.SetActive(true);
        }
        public void BeginLeaving()
        {
            _targets = new Vector3[_arrivalPath.Length];
            for (int index = 0; index < _targets.Length - 1; index++)
                _targets[index] = _arrivalPath[_arrivalPath.Length - 2 - index].position;
            _targets[_targets.Length - 1] = _exitPoint.position; _targetIndex = 0;
        }
        public bool Advance(double elapsedSeconds, float speed)
        {
            if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds) || elapsedSeconds < 0 ||
                float.IsNaN(speed) || float.IsInfinity(speed) || speed <= 0f) throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            double budget = elapsedSeconds * speed;
            while (_targetIndex < _targets.Length)
            {
                Vector3 offset = _targets[_targetIndex] - transform.position;
                float distance = offset.magnitude;
                if (distance > 0.001f)
                {
                    Vector3 heading = new Vector3(offset.x, 0f, offset.z);
                    if (heading.sqrMagnitude > 0.000001f) transform.rotation = Quaternion.LookRotation(heading);
                    if (budget < distance)
                    { transform.position += offset.normalized * (float)budget; return false; }
                }
                transform.position = _targets[_targetIndex]; budget -= distance; _targetIndex++;
            }
            return true;
        }
        public void Hide() => gameObject.SetActive(false);
    }
}
