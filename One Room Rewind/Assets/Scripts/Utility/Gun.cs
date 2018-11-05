using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Gun : MonoBehaviour, IInteractable {

    bool holding = false;
    Rigidbody rigidBody;
    GameObject hand;

    public void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        hand = GameObject.Find("Hand Slot");
    }

    public void Interact()
    {
        if (holding)
        {
            fire();
        }
        else
        {
            holding = true;
            rigidBody.isKinematic = true;
            transform.SetParent(hand.transform);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            GetComponent<BoxCollider>().enabled = false;
        }
    }

    private void fire()
    {
        throw new NotImplementedException();
    }
}
