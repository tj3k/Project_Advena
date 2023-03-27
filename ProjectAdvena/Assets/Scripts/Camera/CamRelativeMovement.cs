using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Camera
{
    public class CamRelativeMovement : MonoBehaviour
    {
        private float _horizontalInput;
        private float _verticalInput;
        private Vector3 _playerInput;
        [SerializeField] private CharacterController _characterController;

        private void Start()
        {
            _characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            // Storing current player input each frame rendered.
            _horizontalInput = Input.GetAxis("Horizontal");
            _verticalInput = Input.GetAxis("Vertical");

            //
            _playerInput.x = _horizontalInput;
            _playerInput.z = _verticalInput;

            // Transform
            _characterController.Move(_playerInput * Time.deltaTime);
        }
    }
}