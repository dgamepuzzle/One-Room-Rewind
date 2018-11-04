using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using UnityStandardAssets.CrossPlatformInput;
using System.Linq;

public class RewindManager : MonoBehaviour {

    public static RewindManager Instance { get; private set; }

    public float TotalSnaps {
        get
        {
            return snapsPerSecond * secondsSaved;
        }
    }

    private bool rewinding = false;
    public bool IsRewinding {
        get
        {
            return rewinding;
        }
        set
        {
            if(value == true && rewinding == false)
            {
                mainCamera.transform.parent.GetComponent<RigidbodyFirstPersonController>().enabled = false;
                foreach (RewindableObject rewindableObject in rewindableObjects)
                {
                    rewindableObject.Snap();
                }
                //IMPORTANT: MIGHT ERROR IF 1/SNAPS IS LESS THAN TIME ELAPSED
                
                timeElapsed = 1/snapsPerSecond - timeElapsed;
                if(timeElapsed < 0) {
                    timeElapsed = 0;
                    throw new System.Exception("elapsed Less than 0");
                }
            }else if(value == false && rewinding == true)
            {
                mainCamera.transform.parent.GetComponent<RigidbodyFirstPersonController>().enabled = true;
                RigidbodyFirstPersonController controller = GameObject.Find("RigidBodyFPSController").GetComponent<RigidbodyFirstPersonController>();
                controller.mouseLook.m_CharacterTargetRot.eulerAngles = new Vector3(0, controller.gameObject.transform.rotation.eulerAngles.y, 0);
                controller.mouseLook.m_CameraTargetRot.eulerAngles = new Vector3(controller.transform.GetChild(0).transform.localRotation.eulerAngles.x, 0, 0);
            }
            rewinding = value;
        }
    }
    public float snapsPerSecond;
    public float secondsSaved;
    
    LinkedList<RewindableObject> rewindableObjects = new LinkedList<RewindableObject>();
    float timeElapsed;

    GameObject mainCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start () {
        rewindableObjects.AddFirst(new RewindableObject(GameObject.FindGameObjectWithTag("MainCamera")));
        mainCamera = rewindableObjects.First.Value.Object;
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("rewindableObject"))
        {
            rewindableObjects.AddFirst(new RewindableObject(obj));
        }
	}
	
	void Update ()
    {
        IsRewinding = Input.GetKey(KeyCode.R);

        timeElapsed += Time.deltaTime;
        if (!IsRewinding)
        {
            if (timeElapsed > 1 / snapsPerSecond)
            {
                timeElapsed -= 1 / snapsPerSecond;
                foreach (RewindableObject rewindableObject in rewindableObjects)
                {
                    rewindableObject.Snap();
                }
            }
        }
        else
        {
            while (timeElapsed > 1 / snapsPerSecond)
            {
                timeElapsed -= 1 / snapsPerSecond;
                foreach (RewindableObject rewObj in rewindableObjects)
                {
                    rewObj.Pop();
                }
            }
            // Percent of the way to target time Distance
            float timeDistance = (timeElapsed)/(1 / snapsPerSecond);
            
            //Apply Lerp
            var rewindableObject = rewindableObjects.First;
            for (; rewindableObject != null; )
            {
                var next = rewindableObject.Next;
                rewindableObject.Value.ApplyData(timeDistance);
                rewindableObject = next;
            }
        }
	}


    public void RegisterGameObject(GameObject gameObject)
    {
        RewindableObject rewindableObject = new RewindableObject(gameObject);
        rewindableObject.OnDeath = () =>
        {
            Destroy(gameObject);
            rewindableObjects.Remove(rewindableObject);
        };
        rewindableObjects.AddFirst(rewindableObject);

        rewindableObjects.First.Value.Snap();
    }
}

struct RewindableObject
{
    public GameObject Object;

    public LinkedList<Vector3> positions;
    public LinkedList<Quaternion> rotations;

    private Rigidbody rigidbody;
    public LinkedList<Vector3> velocities;

    public Action OnDeath;

    public RewindableObject(GameObject Object)
    {
        this.Object = Object;
        positions = new LinkedList<Vector3>();
        rotations = new LinkedList<Quaternion>();
        rigidbody = Object.GetComponent<Rigidbody>();
        velocities = new LinkedList<Vector3>();
        OnDeath = null;
        
    }
    public RewindableObject(GameObject Object, Action Execute) : this(Object)
    {
        this.OnDeath = Execute;
    }

    public void Snap()
    {
        positions.AddFirst(Object.transform.position);
        rotations.AddFirst(Object.transform.rotation);

        if (rigidbody)
        {
            velocities.AddFirst(rigidbody.velocity);
        }

        if (positions.Count > RewindManager.Instance.TotalSnaps)
        {
            positions.RemoveLast();
            rotations.RemoveLast();
            if (rigidbody)
            {
                velocities.RemoveLast();
            }
        }
    }

    public void Pop()
    {
        positions.RemoveFirst();
        rotations.RemoveFirst();

        if (rigidbody)
        {
            velocities.RemoveFirst();
        }
    }

    public void ApplyData(float lerpPercent)
    {
        if(positions.First.Next == null && OnDeath != null)
        {
            OnDeath();
            return;
        }
        Object.transform.position = Vector3.LerpUnclamped(positions.First.Value, positions.First.Next.Value, lerpPercent);
        Object.transform.rotation = Quaternion.LerpUnclamped(rotations.First.Value, rotations.First.Next.Value, lerpPercent);

        if (rigidbody)
        {
            rigidbody.velocity = Vector3.LerpUnclamped(velocities.First.Value, velocities.First.Next.Value, lerpPercent);
        }
    }
}