using System;
using UnityEngine;

public class DebugMover : MonoBehaviour
{
    [Header("Movement Settings")] [SerializeField]
    private float moveSpeed = 5f;

    [SerializeField] private float rotationSpeed = 100f;

    [Header("Keybindings")] [SerializeField]
    private KeyCode moveUpKey = KeyCode.UpArrow;

    [SerializeField] private KeyCode moveDownKey = KeyCode.DownArrow;
    [SerializeField] private KeyCode moveLeftKey = KeyCode.LeftArrow;
    [SerializeField] private KeyCode moveRightKey = KeyCode.RightArrow;
    [SerializeField] private KeyCode moveUpVerticalKey = KeyCode.Quote;
    [SerializeField] private KeyCode moveDownVerticalKey = KeyCode.Slash;
    [SerializeField] private KeyCode rotateLeftKey = KeyCode.Semicolon;
    [SerializeField] private KeyCode rotateRightKey = KeyCode.P;
    [SerializeField] private KeyCode rotateUpKey = KeyCode.LeftBracket;
    [SerializeField] private KeyCode rotateDownKey = KeyCode.RightBracket;

    private void Update()
    {
        Move();
        Rotate();
    }

    private void Move()
    {
        Vector3 moveDirection = Vector3.zero;

        if (Input.GetKey(moveLeftKey))
        {
            moveDirection -= transform.right;
        }

        if (Input.GetKey(moveRightKey))
        {
            moveDirection += transform.right;
        }

        if (Input.GetKey(moveUpKey))
        {
            moveDirection += transform.forward;
        }

        if (Input.GetKey(moveDownKey))
        {
            moveDirection -= transform.forward;
        }

        if (Input.GetKey(moveUpVerticalKey))
        {
            moveDirection += transform.up;
        }

        if (Input.GetKey(moveDownVerticalKey))
        {
            moveDirection -= transform.up;
        }

        if (moveDirection == Vector3.zero) return;

        transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;
    }

    private void Rotate()
    {
        Vector3 rotationDirection = Vector3.zero;
        if (Input.GetKey(rotateUpKey))
        {
            rotationDirection += transform.up;
        }

        if (Input.GetKey(rotateDownKey))
        {
            rotationDirection -= transform.up;
        }

        if (Input.GetKey(rotateLeftKey))
        {
            rotationDirection -= transform.right;
        }

        if (Input.GetKey(rotateRightKey))
        {
            rotationDirection += transform.right;
        }

        if (rotationDirection == Vector3.zero) return;

        transform.Rotate(rotationDirection.normalized * rotationSpeed * Time.deltaTime, Space.World);


    }
}