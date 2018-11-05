using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using UnityStandardAssets.CrossPlatformInput;
using System.Linq;

public class RewindManager : MonoBehaviour {

    /// <summary>
    /// Singleton Instance of Rewind Manager Class
    /// </summary>
    public static RewindManager Instance { get; private set; }

    /// <summary>
    /// Total number of Snaps stored.
    /// Snaps Per Second * Seconds Saved
    /// </summary>
    public float TotalSnaps {
        get
        {
            return snapsPerSecond * secondsSaved;
        }
    }

    private bool rewinding = false;

    /// <summary>
    /// Is the Game currently rewinding?
    /// If setting to true from false, executes tasks neccessary to start rewinding.
    /// If setting to false from true, reEnables deactivated objects and fixes camera angle to account for rewinding changes.
    /// </summary>
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

                //Reverse Particles
                foreach (ParticleSystem system in particleSystems)
                {
                    ParticleSystem.Particle[] p = new ParticleSystem.Particle[system.particleCount + 1];
                    int l = system.GetParticles(p);

                    int i = 0;
                    while (i < l)
                    {
                        p[i].velocity += -2 * p[i].totalVelocity;
                        i++;
                    }

                    system.SetParticles(p, l);
                }
            }
            else if(value == false && rewinding == true)
            {
                mainCamera.transform.parent.GetComponent<RigidbodyFirstPersonController>().enabled = true;
                RigidbodyFirstPersonController controller = GameObject.Find("RigidBodyFPSController").GetComponent<RigidbodyFirstPersonController>();
                controller.mouseLook.m_CharacterTargetRot.eulerAngles = new Vector3(0, controller.gameObject.transform.rotation.eulerAngles.y, 0);
                controller.mouseLook.m_CameraTargetRot.eulerAngles = new Vector3(controller.transform.GetChild(0).transform.localRotation.eulerAngles.x, 0, 0);
                
                //Reverse Particles
                foreach (ParticleSystem system in particleSystems)
                {
                    ParticleSystem.Particle[] p = new ParticleSystem.Particle[system.particleCount + 1];
                    int l = system.GetParticles(p);

                    int i = 0;
                    while (i < l)
                    {
                        p[i].velocity += -2 * p[i].totalVelocity;
                        i++;
                    }

                    system.SetParticles(p, l);
                }
            }
            rewinding = value;
        }
    }
    
    /// <summary>
    /// The amount of Snaps taken (automatically) per second.
    /// </summary>
    public float snapsPerSecond;

    /// <summary>
    /// The amount of seconds worth of Snaps saved before they are removed.
    /// </summary>
    public float secondsSaved;

    public List<ParticleSystem> particleSystems = new List<ParticleSystem>();
    
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

    void Start(){
        rewindableObjects.AddFirst(new RewindableObject(GameObject.FindGameObjectWithTag("MainCamera")));
        mainCamera = rewindableObjects.First.Value.Object;
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("rewindableObject"))
        {
            rewindableObjects.AddFirst(new RewindableObject(obj));
        }
	}
	
	void Update()
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
            var rewindableObject = rewindableObjects.First;
            while (timeElapsed > 1 / snapsPerSecond)
            {
                timeElapsed -= 1 / snapsPerSecond;
                rewindableObject = rewindableObjects.First;
                for (; rewindableObject != null;)
                {
                    var next = rewindableObject.Next;
                    rewindableObject.Value.Pop();
                    rewindableObject = next;
                }
            }
            // Percent of the way to target time Distance
            float timeDistance = (timeElapsed)/(1 / snapsPerSecond);
            
            //Apply Lerp
            rewindableObject = rewindableObjects.First;
            for (; rewindableObject != null; )
            {
                var next = rewindableObject.Next;
                rewindableObject.Value.ApplyData(timeDistance);
                rewindableObject = next;
            }
        }
	}

    /// <summary>
    /// Registers a game object as a RewindableObject.
    /// Please keep in mind that all gameObjects with the tag "rewindableObject" at the beginning of the game do NOT require adding.
    /// Snaps once for internal reference, then adds Destroyable SnapAction, then Snaps again for continuity.
    /// </summary>
    /// <param name="gameObject">
    /// The game object to be registered as a RewindableObject.
    /// </param>
    public void RegisterGameObject(GameObject gameObject)
    {
        RewindableObject rewindableObject = new RewindableObject(gameObject);
        rewindableObject.Snap();
        rewindableObject.SnapAction(() =>
        {
            Destroy(gameObject);
            rewindableObjects.Remove(rewindableObject);
            return ActionOutput.Return;
        });
        rewindableObjects.AddFirst(rewindableObject);

        rewindableObject.Snap();
    }


    /// <summary>
    /// Snaps a SnapAction in an object already in the registered list of RewindableObjects.
    /// </summary>
    /// <param name="gameObject">
    /// The game object to find and Snap.
    /// </param>
    /// <param name="action">
    /// The SnapAction to snap onto the gameObject.
    /// </param>
    /// <param name="padNull">
    /// Would you like to pad a null Action infront of the param action?
    /// </param>
    public void SnapActionToObject(GameObject gameObject, Func<ActionOutput> action, bool padNull = false)
    {
        foreach(RewindableObject rewindableObject in rewindableObjects)
        {
            if(rewindableObject.Object == gameObject)
            {
                rewindableObject.SnapAction(action);
                if (padNull)
                {
                    rewindableObject.actions.AddFirst((Func<ActionOutput>) null);
                }
                Debug.Log("Action " + action + " snapped to gameObject " + gameObject);
            }
        }
    }
}

class RewindableObject
{
    public GameObject Object;

    public LinkedList<Vector3> positions;
    public LinkedList<Quaternion> rotations;

    private Rigidbody rigidbody;
    public LinkedList<Vector3> velocities;

    public LinkedList<Func<ActionOutput>> actions;

    /// <summary>
    /// Ensures matched synchronity between Actions and other LinkedLists by recording if an Action was added this frame. 
    /// If it was, a null Action will not be added.
    /// </summary>
    public bool addedAction;


    /// <summary>
    /// Creates a RewindableObject.
    /// </summary>
    /// <param name="Object">
    /// The GameObject on which the RewindableObject is based.
    /// </param>
    public RewindableObject(GameObject Object)
    {
        this.Object = Object;
        positions = new LinkedList<Vector3>();
        rotations = new LinkedList<Quaternion>();
        rigidbody = Object.GetComponent<Rigidbody>();
        velocities = new LinkedList<Vector3>();
        actions = new LinkedList<Func<ActionOutput>>();
        addedAction = false;


    }


    /// <summary>
    /// Snaps a SnapAction onto the Action LinkedList.
    /// </summary>
    /// <param name="action">
    /// The SnapAction to Snap.
    /// </param>
    public void SnapAction(Func<ActionOutput> action)
    {
        ///Friendly reminder
        if (addedAction) throw new Exception("Action already added this frame!!!! Fix this now you lazy pig");
        actions.AddFirst(action);
        addedAction = true;
    }

    /// <summary>
    /// Snaps a snapshot of all recorded variables. Note: Will Snap a null action if addedAction == false.
    /// </summary>
    public void Snap()
    {
        positions.AddFirst(Object.transform.position);
        rotations.AddFirst(Object.transform.rotation);

        if (addedAction)
        {
            addedAction = false;
        }
        else
        {
            actions.AddFirst((Func<ActionOutput>)null);
        }

        if (rigidbody)
        {
            velocities.AddFirst(rigidbody.velocity);
        }

        if (positions.Count > RewindManager.Instance.TotalSnaps)
        {
            if(actions.Count > RewindManager.Instance.TotalSnaps)
            {
                actions.RemoveLast();
            }
            positions.RemoveLast();
            rotations.RemoveLast();
            if (rigidbody)
            {
                velocities.RemoveLast();
            }
        }
    }


    /// <summary>
    /// Pops the first variables from all records.
    /// It will execute any action it pops off if not null.
    /// </summary>
    public void Pop()
    {
        positions.RemoveFirst();
        rotations.RemoveFirst();
        if(actions.Count > 0)
        {
            if (actions.First != null && actions.First.Value != null)
            {
                ActionOutput output = actions.First.Value();
                if (output == ActionOutput.Return)
                {
                    return;
                }
            }
            actions.RemoveFirst();
        }

        if (rigidbody)
        {
            velocities.RemoveFirst();
        }
    }

    /// <summary>
    /// Applies variables stored in LinkedLists to the GameObject.
    /// Linearly Interpolates between variables using lerpPercent as the percent.
    /// </summary>
    /// <param name="lerpPercent">
    /// Percent of the way from top to second variables
    /// </param>
    public void ApplyData(float lerpPercent)
    {
        Object.transform.position = Vector3.LerpUnclamped(positions.First.Value, positions.First.Next.Value, lerpPercent);
        Object.transform.rotation = Quaternion.LerpUnclamped(rotations.First.Value, rotations.First.Next.Value, lerpPercent);

        if (rigidbody)
        {
            rigidbody.velocity = Vector3.LerpUnclamped(velocities.First.Value, velocities.First.Next.Value, lerpPercent);
        }
    }


    /// <summary>
    /// Converts Rewindable to simple string containing only the gameObject
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return "Rewindable: " + Object.ToString();
    }
}

/// <summary>
/// Output types for Snap Action.
/// The RewindableObject will deal with the action when the SnapAction is popped off the LinkedList.
/// </summary>
public enum ActionOutput
{
    None,
    Return,
}